using System.IO;
using ManagedBass;

public class Player : IDisposable{
	int stream;
	int finishSync;
	
	int currentDeviceIndex;
	public DeviceInfo currentDevice;
	
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
	
	public event EventHandler onSongLoad;
	public event EventHandler onChangePlaystate;
	public event EventHandler onChangeDevice;
	
	bool isStoping;
	
	public Player(int song = -1, int vol = 100, float volxp = 2f, float el = 0f){
		volume = vol;
		volumeExponent = volxp;
		playingSong = song + 1; //Cheap trick so it loads...
		
		if(!Bass.Init()){
			throw new Exception("Bass failed to initialize");
		}
		
		currentDeviceIndex = Bass.CurrentDevice;
		currentDevice = new DeviceInfo();
		Bass.GetDeviceInfo(currentDeviceIndex, out currentDevice);
		
		loadSong(song);
		elapsed = el;
	}
	
	public void loadSong(int song){
		stop();
		
		playingSong = song;
		
		if(!Song.exists(playingSong)){
			playingSong = -1;
			
			onSongLoad?.Invoke(this, EventArgs.Empty);
			return;
		}
		
		string path = Song.getAudioPath(playingSong);
		if(path == null){
			playingSong = -1;
			
			onSongLoad?.Invoke(this, EventArgs.Empty);
			return;
		}
		
		stream = Bass.CreateStream(path);
		setVolume(volume);
		isStoping = false;
		
		attachFinish();
		
		onSongLoad?.Invoke(this, EventArgs.Empty);
	}
	
	public void play(int song){
		loadSong(song);
		resume();
	}
	
	public void askForSong(){
		play(Session.serveNext());
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
		Session.addPrevPlayed(playingSong);
		
		//Console.WriteLine("Song ended");
		
		play(Session.serveNext());
	}
	
	public void prev(){
		int j = Session.getPrevious(playingSong);
		if(j < 0){
			return;
		}
		play(j);
	}
	
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
		finishSync = Bass.ChannelSetSync(stream, SyncFlags.End, 0, onFinish);
	}
	
	void onFinish(int handle, int channel, int data, IntPtr user){		
		if(isStoping){
			return;
		}
		
		//Console.WriteLine("Song ended " + new Random().Next(10000));
		//Console.ReadKey();
		
		Task.Run(() => {
			play(Session.serveNext());
			Session.addPrevPlayed(playingSong);
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
	
	public DeviceInfo getCurrentDevice(){
		return currentDevice;
	}
	
	public void setDevice(int deviceIndex){		
		float el = elapsed;
		
		Dispose();
		
		if(!Bass.Init(deviceIndex, 44100, DeviceInitFlags.Default, IntPtr.Zero)){
			throw new Exception("Bass failed to initialize with device: " + Bass.LastError);
		}
		
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