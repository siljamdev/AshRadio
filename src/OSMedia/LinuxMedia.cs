#if LINUX
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tmds.DBus;

[DBusInterface("org.mpris.MediaPlayer2")]
public interface IMediaPlayer2 : IDBusObject{
	//Methods
	Task QuitAsync();
	Task RaiseAsync();
	
	//Properties
	Task<object> GetAsync(string prop);
    Task<IDictionary<string, object>> GetAllAsync();
    Task SetAsync(string prop, object val);
    Task<IDisposable> WatchPropertiesAsync(Action<PropertyChanges> handler);
}

[DBusInterface("org.mpris.MediaPlayer2.Player")]
public interface IPlayer : IDBusObject{
	//Methods
	Task PlayAsync();
	Task PauseAsync();
	Task PlayPauseAsync();
	Task StopAsync();
	Task NextAsync();
	Task PreviousAsync();
	Task SeekAsync(long offset); //in microseconds
	Task SetPositionAsync(ObjectPath trackId, long position);
	Task OpenUriAsync(string uri);
	
	//Signals
	Task<IDisposable> WatchSeekedAsync(Action<long> handler, Action<Exception>? onError = null);
	
	//Properties
	Task<object> GetAsync(string prop);
    Task<IDictionary<string, object>> GetAllAsync();
    Task SetAsync(string prop, object val);
    Task<IDisposable> WatchPropertiesAsync(Action<PropertyChanges> handler);
}

class LinuxMedia : OSMedia, IMediaPlayer2, IPlayer{
	bool paused;
	bool shuffle;
	
	Connection _dbusConnection;
	
	public ObjectPath ObjectPath => new ObjectPath("/org/mpris/MediaPlayer2");
	
	public LinuxMedia() : base(){
		InitializeDBusAsync().GetAwaiter().GetResult();
		
		initMetadata();
		initProperties();
		
		base.init();
	}
	
	public async Task InitializeDBusAsync(){
		try{
			// Connect to D-Bus
			_dbusConnection = new Connection(Address.Session);
			await _dbusConnection.ConnectAsync();
			
			// Request the service name
			await _dbusConnection.RegisterServiceAsync("org.mpris.MediaPlayer2.ashradio");
			
			await _dbusConnection.RegisterObjectAsync(this);
		}catch(Exception e){
			Radio.reportError(e.ToString());
		}
	}
	
	#region MediaPlayer2
	
	public Task QuitAsync(){
		Radio.sc?.quitScreens();
		return Task.CompletedTask;
	}
	
	public Task RaiseAsync() => Task.CompletedTask;
	
	#endregion
	
	#region MediaPlayer2.Player
	
	#region Methods
	
	public Task PlayAsync(){
		base.togglePause();
		return Task.CompletedTask;
	}
	
	public Task PauseAsync(){
		base.togglePause();
		return Task.CompletedTask;
	}
	
	public Task PlayPauseAsync(){
		base.togglePause();
		return Task.CompletedTask;
	}
	
	public Task StopAsync() => Task.CompletedTask;
	
	public Task PreviousAsync(){
		base.previous();
		return Task.CompletedTask;
	}
	
	public Task NextAsync(){
		base.skip();
		return Task.CompletedTask;
	}
	
	public Task SeekAsync(long offset){
		base.advance(offset / 1000000f);
		return Task.CompletedTask;
	}
	
	public Task SetPositionAsync(ObjectPath trackId, long position){
		base.setElapsed(position / 1000000f);
		return Task.CompletedTask;
	}
	
	public Task OpenUriAsync(string uri) => Task.CompletedTask;
	
	#endregion
	
	#region signals
	private event Action<long>? _seeked;
	
	public Task<IDisposable> WatchSeekedAsync(Action<long> handler, Action<Exception>? onError = null){
		_seeked += handler;

		return Task.FromResult<IDisposable>(
			new Unsubscriber(() => _seeked -= handler)
		);
	}
	
	#endregion
	
	#region properties
	Dictionary<string, object> properties = new();
	
	public Task<object> GetAsync(string prop){
		return Task.FromResult(properties[prop]);
	}
	
    public Task<IDictionary<string, object>> GetAllAsync(){
		return Task.FromResult<IDictionary<string, object>>(properties);
	}
	
    public Task SetAsync(string prop, object val){
		if(prop == "Volume"){
			properties[prop] = val;
			base.setVolume(Convert.ToSingle(val));
		}
		
		return Task.CompletedTask;
	}
	
	private event Action<PropertyChanges>? _properties;
	
    public Task<IDisposable> WatchPropertiesAsync(Action<PropertyChanges> handler){
		_properties += handler;

		return Task.FromResult<IDisposable>(
			new Unsubscriber(() => _properties -= handler)
		);
	}
	
	void initProperties(){
		//MediaPlayer2
		properties["CanQuit"] = true;
		properties["CanSetFullscreen"] = true;
		properties["CanRaise"] = false;
		properties["HasTrackList"] = false;
		properties["Identity"] = "AshRadio";
		properties["DesktopEntry"] = Radio.config.GetValue<string>("osmediaintegration.linuxdesktop") ?? "ashradio";
		
		//MediaPlayer2.Player
		properties["PlaybackStatus"] = "Stopped";
		properties["LoopStatus"] = "None";
		properties["Rate"] = 1d;
		properties["Shuffle"] = false;
		properties["Volume"] = 1d;
		properties["Position"] = (long) 0;
		properties["MinimumRate"] = 1d;
		properties["MaximumRate"] = 1d;
		properties["CanGoNext"] = true;
		properties["CanGoPrevious"] = true;
		properties["CanPlay"] = true;
		properties["CanPause"] = true;
		properties["CanSeek"] = true;
		properties["CanControl"] = true;
		
		properties["Metadata"] = metadata;
	}
	
	void changeProperties(params (string, object)[] props){
		foreach((string p, object v) in props){
			properties[p] = v;
		}
		
		_properties?.Invoke(new PropertyChanges(props.Select(t => new KeyValuePair<string, object>(t.Item1, t.Item2)).ToArray()));
	}
	#endregion
	
	#region Metadata
	Dictionary<string, object> metadata = new();
	
	void initMetadata(){
		metadata["mpris:trackid"] = new ObjectPath("/org/mpris/MediaPlayer2/Track/" + getId(-1));
		metadata["mpris:length"] = Time_In_Us(0f);
		metadata["xesam:artist"] = Array.Empty<string>();
		metadata["xesam:title"] = Song.nullTitle;
	}
	
	void changeMetadata(params (string, object)[] mets){
		foreach((string m, object v) in mets){
			metadata[m] = v;
		}
		
		_properties?.Invoke(PropertyChanges.ForProperty("Metadata", metadata));
	}
	#endregion
	
	#endregion
	
	#region OSMedia
	protected override void updateSong(int id, string title, string[] authors, float duration){
		changeMetadata(
			("mpris:trackid", new ObjectPath("/org/mpris/MediaPlayer2/Track/" + getId(id))),
			("mpris:length", Time_In_Us(duration)),
			("xesam:artist", authors),
			("xesam:title", title)
		);
		
		changeProperties(("Position", (long) 0));
	}
	
	protected override void updateState(bool isPaused){
		changeProperties(("PlaybackStatus", Playback_Status(isPaused)));
	}
	
	protected override void updateMode(SessionMode mode){
		changeProperties(("Shuffle", mode == SessionMode.Shuffle || mode == SessionMode.SmartShuffle));
	}
	
	protected override void updateElapsed(float seconds){
		changeProperties(("Position", Time_In_Us(seconds)));
		
		_seeked?.Invoke(Time_In_Us(seconds));
	}
	
	protected override void updateVolume(float volume){
		changeProperties(("Volume", (double) volume));
	}
	#endregion
	
	//Helper type
	static string Playback_Status(bool isPaused){
		return isPaused ? "Paused" : "Playing";
	}
	
	//Helper type
	static long Time_In_Us(float seconds){
		return (long) (seconds * 1000000);
	}
	
	//Helper
	static string getId(int id){
		return id > 0 ? id.ToString() : ("n" + (-id));
	}
}

sealed class Unsubscriber : IDisposable{
    private readonly Action _dispose;
    public Unsubscriber(Action dispose) => _dispose = dispose;
    public void Dispose() => _dispose();
}

#endif
