#if LINUX
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tmds.DBus;

[DBusInterface("org.mpris.MediaPlayer2")]
interface IMediaPlayer2 : IDBusObject{
	Task QuitAsync();
	Task RaiseAsync();
	
	Task<string> IdentityAsync{get;}
	Task<string> DesktopEntryAsync{get;}
	
	Task<bool> CanQuitAsync{get;}
	Task<bool> CanRaiseAsync{get;}
	Task<bool> HasTrackListAsync{get;}
}

[DBusInterface("org.mpris.MediaPlayer2.Player")]
interface IPlayer : IDBusObject{
	Task PlayAsync();
	Task PauseAsync();
	Task PlayPauseAsync();
	Task NextAsync();
	Task PreviousAsync();
	Task SeekAsync(long offset); //in microseconds
	Task SetPositionAsync(ObjectPath trackId, long position);

	Task<string> PlaybackStatusAsync { get; }
	Task<long> PositionAsync { get; }
	Task<IDictionary<string, object>> MetadataAsync { get; }
	Task<double> MinimumRateAsync { get; }
	Task<double> MaximumRateAsync { get; }
	
	Task<bool> ShuffleAsync { get; }
	Task<double> VolumeAsync { get;}
	Task SetVolumeAsync(double value);
	Task<double> RateAsync { get; } //Speed
	Task<string> LoopStatusAsync { get; }
	
	Task<bool> CanGoNextAsync { get; }
	Task<bool> CanGoPreviousAsync { get; }
	Task<bool> CanPlayAsync { get; }
	Task<bool> CanPauseAsync { get; }
	Task<bool> CanSeekAsync { get; }
	Task<bool> CanControlAsync { get; }
}

class MediaPlayer2Impl : IMediaPlayer2{
	string desktopName;
	
	public MediaPlayer2Impl(){
		desktopName = Radio.config.GetValue<string>("osmediaintegration.linuxdesktop") ?? "ashradio";
	}
	
	public ObjectPath ObjectPath => new ObjectPath("/org/mpris/MediaPlayer2");
	
	public Task QuitAsync(){
		Radio.sc?.quitScreens();
		return Task.CompletedTask;
	}
	
	public Task RaiseAsync() => Task.CompletedTask;
	
	public Task<string> IdentityAsync => Task.FromResult("AshRadio");
	public Task<string> DesktopEntryAsync => Task.FromResult(desktopName);
	
	public Task<bool> CanQuitAsync => Task.FromResult(true);
	public Task<bool> CanRaiseAsync => Task.FromResult(true);
	public Task<bool> HasTrackListAsync => Task.FromResult(false);
}

class LinuxMedia : OSMedia, IPlayer{
	bool paused;
	bool shuffle;
	
	Connection _dbusConnection;
	MediaPlayer2Impl _mediaPlayer2;
	
	public LinuxMedia() : base(){
		InitializeDBusAsync().GetAwaiter().GetResult();
		
		base.init();
	}
	
	public async Task InitializeDBusAsync(){
		try{
			// Connect to D-Bus
			_dbusConnection = new Connection(Address.Session);
			await _dbusConnection.ConnectAsync();
			
			// Create and register implementations
			_mediaPlayer2 = new MediaPlayer2Impl();
			
			await _dbusConnection.RegisterObjectAsync(_mediaPlayer2);
			await _dbusConnection.RegisterObjectAsync(this);
			
			// Request the service name
			await _dbusConnection.RegisterServiceAsync("org.mpris.MediaPlayer2.ashradio");
		}catch(Exception e){
			Radio.reportError(e.ToString());
		}
	}
	
	public ObjectPath ObjectPath => new ObjectPath("/org/mpris/MediaPlayer2/Player");
	
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
	
	public Task<string> PlaybackStatusAsync => Task.FromResult(paused ? "Paused" : "Playing");
	public Task<long> PositionAsync => Task.FromResult((long) (base.getElapsed() * 1000000));
	
	Dictionary<string, object> Metadata = new();
	public Task<IDictionary<string, object>> MetadataAsync {get => Task.FromResult((IDictionary<string, object>) Metadata);}
	
	public Task<bool> ShuffleAsync => Task.FromResult(shuffle);
	
	public Task<double> VolumeAsync {get => Task.FromResult(Radio.py.volume / 100d);}
	public async Task SetVolumeAsync(double value){
		base.setVolume((float) value);
	}
	
	public Task<double> RateAsync => Task.FromResult(1d);
	public Task<double> MinimumRateAsync => Task.FromResult(1d);
	public Task<double> MaximumRateAsync => Task.FromResult(1d);
	
	public Task<string> LoopStatusAsync => Task.FromResult("None");
	
	public Task<bool> CanGoNextAsync => Task.FromResult(true);
	public Task<bool> CanGoPreviousAsync => Task.FromResult(true);
	public Task<bool> CanPlayAsync => Task.FromResult(true);
	public Task<bool> CanPauseAsync => Task.FromResult(true);
	public Task<bool> CanSeekAsync => Task.FromResult(true);
	public Task<bool> CanControlAsync => Task.FromResult(true);
	
	protected override void updateSong(int id, string title, string[] authors, float duration){
		Metadata["xesam:title"] = title;
		Metadata["xesam:artist"] = authors;
		Metadata["xesam:length"] = (long) (duration * 1000000d);
		Metadata["xesam:trackid"] = new ObjectPath("/org/mpris/MediaPlayer2/Track/" + id);
	}
	
	protected override void updateState(bool isPaused){
		paused = isPaused;
	}
	
	protected override void updateMode(SessionMode mode){
		shuffle = mode == SessionMode.Shuffle || mode == SessionMode.SmartShuffle;
	}
}
#endif
