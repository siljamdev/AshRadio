using System.IO;
using CSCore;
using CSCore.Codecs;
using CSCore.CoreAudioAPI;
using CSCore.SoundOut;
using CSCore.Streams;

public class Player : IDisposable{
	ISoundOut soundOut = new WasapiOut();
	IWaveSource waveSource;
	
	public int volume{get; private set;} //0 to 100
	
	public float volumeExponent;
	
	public int playingSong{get; private set;}
	
	public float duration{get{
		return (float) (waveSource?.GetLength().TotalSeconds ?? 0d);
	}}
	
	public float elapsed{get{ //In seconds
		return (float) (waveSource?.GetPosition().TotalSeconds ?? 0d);
	}
	set{
		if(value > duration){
			waveSource?.SetPosition(TimeSpan.FromSeconds(duration));
		}else{
			waveSource?.SetPosition(TimeSpan.FromSeconds(value));
		}
	}}
	
	public bool isPaused{get{
		return soundOut?.PlaybackState != PlaybackState.Playing;
	}}
	
	public MMDevice currentDevice;
	
	public event EventHandler onSongLoad;
	public event EventHandler onChangePlaystate;
	public event EventHandler onChangeDevice;
	
	public Player(int song = -1, int vol = 100, float volxp = 2f, float el = 0f){
		volume = vol;
		volumeExponent = volxp;
		playingSong = song + 1; //Cheap trick so it loads...
		loadSong(song);
		elapsed = el;
		
		currentDevice = new MMDeviceEnumerator().GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
		
		soundOut.Stopped += onFinish; //Only once!!!
	}
	
	public void loadSong(int song){
		if(song == playingSong){ //No reloading
			return;
		}
		
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
		
		waveSource?.Dispose();
		waveSource = CodecFactory.Instance.GetCodec(path);

		soundOut.Initialize(waveSource);
		soundOut.Volume = (float) Math.Pow((float) volume / 100f, volumeExponent);
		
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
		
		if(soundOut?.PlaybackState == PlaybackState.Playing){
			soundOut.Pause();
			onChangePlaystate?.Invoke(this, EventArgs.Empty);
		}
	}
	
	public void resume(){
		if(playingSong < 0){
			return;
		}
		
		if(soundOut?.PlaybackState != PlaybackState.Playing){
			soundOut.Play();
			onChangePlaystate?.Invoke(this, EventArgs.Empty);
		}
	}
	
	public void togglePause(){
		if(soundOut == null){
			return;
		}
		
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
		
		if(playingSong < 0){
			return;
		}
		
		soundOut.Volume = (float) Math.Pow((float) volume / 100f, volumeExponent);
	}
	
	void onFinish(object sender, PlaybackStoppedEventArgs a){
		if(a.Exception != null){
			Console.WriteLine("The audio stopped because of an error:");
			Console.WriteLine(a.Exception);
		}
		
		Session.addPrevPlayed(playingSong);
		
		//Console.WriteLine("Song ended " + new Random().Next(10000));
		//Console.ReadKey();
		
		Task.Run(() => play(Session.serveNext()));
	}
	
	void stop(){
		if(soundOut.PlaybackState != PlaybackState.Stopped){
			soundOut.Stopped -= onFinish;
			soundOut.Stop();
		}
		
		waveSource?.Dispose();
		waveSource = null;
	}
	
	public void Dispose(){
		stop();
		soundOut?.Dispose();
	}
	
	public MMDevice getCurrentDevice(){
		return currentDevice;
	}
	
	public void setDevice(MMDevice dev){
		pause();
		soundOut.Stopped -= onFinish;
		soundOut.Stop();
		soundOut.Dispose();
		
		currentDevice = dev;
		
		//There is no need to regenerate wavesource. tested it.
		
		soundOut = new WasapiOut(){Device = dev}; //Everything should be right!
		soundOut.Stopped += onFinish;
		if(waveSource != null){
			soundOut.Initialize(waveSource);
			soundOut.Volume = (float) Math.Pow((float) volume / 100f, volumeExponent);
		}
		
		onChangeDevice?.Invoke(this, EventArgs.Empty);
	}
	
	public static Dictionary<string, MMDevice> getDeviceList(){
		using var enumerator = new MMDeviceEnumerator();
		var devices = enumerator.EnumAudioEndpoints(DataFlow.Render, DeviceState.Active);
		
		return devices.ToDictionary(n => n.FriendlyName, n => n);
	}
}