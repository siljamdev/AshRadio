using System.IO;
using NAudio.Wave;
using NAudio.CoreAudioApi;

public class Player : IDisposable{
	IWavePlayer soundOut;
    AudioFileReader reader;
	
	public int volume{get; private set;} //0 to 100
	
	public float volumeExponent;
	
	public int playingSong{get; private set;}
	
	public float duration{get{
		return (float) (reader?.TotalTime.TotalSeconds ?? 0d);
	}}
	
	public float elapsed{get{ //In seconds
		return (float) (reader?.CurrentTime.TotalSeconds ?? 0d);
	}
	set{
		if(reader == null){
			return;
		}
		if(value >= duration){
			onFinish(null, null);
		}
		reader.CurrentTime = TimeSpan.FromSeconds(Math.Clamp(value, 0, duration));
	}}
	
	public bool isPaused{get{
		return soundOut.PlaybackState != PlaybackState.Playing;
	}}
	
	public MMDevice currentDevice;
	
	public event EventHandler onSongLoad;
	public event EventHandler onChangePlaystate;
	public event EventHandler onChangeDevice;
	
	bool isStoping;
	
	public Player(int song = -1, int vol = 100, float volxp = 2f, float el = 0f){
		volume = vol;
		volumeExponent = volxp;
		playingSong = song + 1; //Cheap trick so it loads...
		
		currentDevice = new MMDeviceEnumerator().GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
		soundOut = new WasapiOut(currentDevice, AudioClientShareMode.Shared, false, 100);
		
		loadSong(song);
		elapsed = el;
		
		soundOut.PlaybackStopped += onFinish; //Only once!!!
	}
	
	public void loadSong(int song){
		stop();
		soundOut.Dispose();
		
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
		
		reader?.Dispose();
		
		reader = new AudioFileReader(path);
		setVolume(volume);
		
		isStoping = false;
		soundOut = new WasapiOut(currentDevice, AudioClientShareMode.Shared, false, 100);
		soundOut.PlaybackStopped += onFinish;
		
		soundOut.Init(reader);
		
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
		if(playingSong < 0){
			return;
		}
		
		if(soundOut.PlaybackState == PlaybackState.Playing){
			soundOut.Pause();
			onChangePlaystate?.Invoke(this, EventArgs.Empty);
		}
	}
	
	public void resume(){
		if(playingSong < 0){
			return;
		}
		
		if(soundOut.PlaybackState != PlaybackState.Playing){
			soundOut.Play();
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
		//soundOut.Stop();
		//return;
		
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
		volume = Math.Clamp(v, 0, 100);
		
		Radio.config.SetCamp("player.volume", volume);
		Radio.config.Save();
		
		if(reader != null){
			reader.Volume = (float) Math.Pow((float) volume / 100f, volumeExponent);
		}
	}
	
	void onFinish(object sender, StoppedEventArgs a){
		if(a?.Exception != null){
			Console.WriteLine("The audio stopped because of an error:");
			Console.WriteLine(a.Exception);
			File.AppendAllText("error.log", a.Exception.ToString());
		}
		
		if(isStoping){
			return;
		}
		
		Session.addPrevPlayed(playingSong);
		
		//Console.WriteLine("Song ended " + new Random().Next(10000));
		//Console.ReadKey();
		
		Task.Run(() => play(Session.serveNext()));
	}
	
	void stop(){
		if(soundOut.PlaybackState != PlaybackState.Stopped){
			isStoping = true;
			soundOut.PlaybackStopped -= onFinish;
			soundOut.Stop();
		}
		
		reader?.Dispose();
		reader = null;
	}
	
	public void Dispose(){
		stop();
		soundOut.Dispose();
	}
	
	public MMDevice getCurrentDevice(){
		return currentDevice;
	}
	
	public void setDevice(MMDevice dev){
		pause();
		soundOut.PlaybackStopped -= onFinish;
		soundOut.Stop();
		soundOut.Dispose();
		
		currentDevice = dev;
		
		soundOut = new WasapiOut(currentDevice, AudioClientShareMode.Shared, false, 100);
		soundOut.PlaybackStopped += onFinish;
		if(reader != null){
			soundOut.Init(reader);
			setVolume(volume);
		}
		
		onChangeDevice?.Invoke(this, EventArgs.Empty);
	}
	
	public static Dictionary<string, MMDevice> getDeviceList(){
		using var enumerator = new MMDeviceEnumerator();
		var devices = enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active);
		
		return devices.ToDictionary(n => n.FriendlyName, n => n);
	}
}