using System.IO;
using ManagedBass;

public class Player : IDisposable{
	int stream;
	int finishSync;
	
	public DeviceInfo currentDevice{get; private set;}
	
	public int volume{get; private set;} //0 to 100
	
	public float volumeExponent;
	
	public int playingSong{get; private set;}
	
	public float duration{get{
		return (float) Bass.ChannelBytes2Seconds(stream, Bass.ChannelGetLength(stream));
	}}
	
	public float elapsed{get{ //In seconds
		return (float) Bass.ChannelBytes2Seconds(stream, Bass.ChannelGetPosition(stream));
	}
	set{
		if(value >= duration){
			skip();
		}else if(value < 0f){
			Bass.ChannelSetPosition(stream, Bass.ChannelSeconds2Bytes(stream, 0f));
		}
		
		Bass.ChannelSetPosition(stream, Bass.ChannelSeconds2Bytes(stream, value));
	}}
	
	public bool isPaused{get{
		return Bass.ChannelIsActive(stream) != PlaybackState.Playing;
	}}
	
	public event EventHandler onBeforeSongLoad;
	public event EventHandler onSongLoad;
	public event EventHandler onSongFinish;
	
	public event EventHandler onChangePlaystate;
	public event EventHandler onChangeDevice;
	
	bool isStoping;
	
	public Player(int vol = 100, float volxp = 2f){
		volume = vol;
		volumeExponent = volxp;
		
		playingSong = -1;
		
		if(!Bass.Init()){
			throw new Exception("Bass failed to initialize");
		}
		
		DeviceInfo t1 = new DeviceInfo();
		Bass.GetDeviceInfo(Bass.CurrentDevice, out t1);
		currentDevice = t1;
	}
	
	public void init(int song = -1, float el = 0f){
		loadSong(song);
		elapsed = el;
	}
	
	public void loadSong(int song){
		onBeforeSongLoad?.Invoke(this, EventArgs.Empty);
		
		stop();
		
		playingSong = song;
		
		if(!Song.exists(playingSong)){
			playingSong = -1;
			
			onSongLoad?.Invoke(this, EventArgs.Empty);
			Radio.config.Set("player.song", playingSong);
			Radio.config.Save();
			return;
		}
		
		string path = Song.getAudioPath(playingSong);
		if(path == null){
			playingSong = -1;
			
			onSongLoad?.Invoke(this, EventArgs.Empty);
			Radio.config.Set("player.song", playingSong);
			Radio.config.Save();
			return;
		}
		
		stream = Bass.CreateStream(path);
		setVolume(volume);
		isStoping = false;
		
		attachFinish();
		
		onSongLoad?.Invoke(this, EventArgs.Empty);
		Radio.config.Set("player.song", playingSong);
		Radio.config.Save();
	}
	
	public void play(int song){
		loadSong(song);
		resume();
	}
	
	public void pause(){
		if(Bass.ChannelPause(stream)){
			onChangePlaystate?.Invoke(this, EventArgs.Empty);
		}
	}
	
	public void resume(){
		if(Bass.ChannelPlay(stream)){
			onChangePlaystate?.Invoke(this, EventArgs.Empty);
		}
	}
	
	public void togglePause(){
		if(isPaused){
			resume();
		}else{
			pause();
		}
	}
	
	public void skip(){
		//Console.WriteLine("Song ended");
		
		//play(Session.serveNext());
		
		onSongFinish?.Invoke(this, EventArgs.Empty);
	}
	
	/* public void prev(){
		int j = Session.getPrevious(playingSong);
		if(j < 0){
			return;
		}
		play(j);
	} */
	
	public void setVolume(int v){
		v = Math.Clamp(v, 0, 100);
		if(volume != v){
			volume = v;
			Radio.config.Set("player.volume", volume);
			Radio.config.Save();
		}
		
		Bass.ChannelSetAttribute(stream, ChannelAttribute.Volume, (float) Math.Pow((float) volume / 100f, volumeExponent));
	}
	
	void attachFinish(){
		finishSync = Bass.ChannelSetSync(stream, SyncFlags.End, 0, handleSongEnd);
	}
	
	void handleSongEnd(int handle, int channel, int data, IntPtr user){
		if(isStoping){
			return;
		}
		
		//Console.WriteLine("Song ended " + new Random().Next(10000));
		//Console.ReadKey();
		
		Task.Run(() => {
			onSongFinish?.Invoke(this, EventArgs.Empty);
		});
	}
	
	void stop(){
		isStoping = true;
		Bass.ChannelRemoveSync(stream, finishSync);
		Bass.StreamFree(stream);
		stream = 0;
	}
	
	public void Dispose(){
		stop();
		
		Bass.Free();
	}
	
	public void setDevice(int deviceIndex){
		if(deviceIndex == Bass.CurrentDevice){
			return;
		}
		
		float el = elapsed;
		
		Dispose();
		
		if(!Bass.Init(deviceIndex, 44100, DeviceInitFlags.Default, IntPtr.Zero)){
			throw new Exception("Bass failed to initialize with device: " + Bass.LastError);
		}
		
		DeviceInfo t1 = new DeviceInfo();
		Bass.GetDeviceInfo(Bass.CurrentDevice, out t1);
		currentDevice = t1;
		
		loadSong(playingSong);
		elapsed = el;
		
		onChangeDevice?.Invoke(this, EventArgs.Empty);
	}
	
	public static Dictionary<string, int> getDeviceList(){
		Dictionary<string, int> devices = new();
		
		for(int i = 0; i < 32; i++){ // max 32 devices, adjust if needed
			DeviceInfo info = new DeviceInfo();
			if(Bass.GetDeviceInfo(i, out info) && info.IsEnabled){
				devices.Add(info.Name, i);
			}
		}
		
		return devices;
	}

}