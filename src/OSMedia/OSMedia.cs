abstract class OSMedia{
	public OSMedia(){
		Radio.py.onSongLoad += (_, _) => {
			Song s = Song.get(Radio.py.playingSong);
			updateSong(s?.id ?? -1, s?.title ?? Song.nullTitle, s?.authors?.Select(n => (Author.get(n)?.name ?? Author.nullName)).ToArray(), Radio.py.duration);
		};
		
		Radio.py.onChangePlaystate += (_, _) => updateState(Radio.py.isPaused);
	}
	
	//To be called at the end of the construtor
	protected void init(){
		Song s = Song.get(Radio.py.playingSong);
		updateSong(s?.id ?? -1, s?.title ?? Song.nullTitle, s?.authors?.Select(n => (Author.get(n)?.name ?? Author.nullName)).ToArray(), Radio.py.duration);
		
		updateState(Radio.py.isPaused);
	}
	
	protected string authorsToString(string[] s){
		return s == null ? "" : string.Join(", ", s);
	}
	
	protected abstract void updateSong(int id, string title, string[] authors, float duration);
	protected abstract void updateState(bool paused);
	protected abstract void updateMode(SessionMode mode);
	
	//To be called
	protected void togglePause(){
		Radio.py.togglePause();
	}
	
	protected void skip(){
		Radio.py.skip();
	}
	
	protected void previous(){
		int j = Session.getPrevious(Radio.py.playingSong);
		if(j < 0){
			return;
		}
		Session.addToPrevList = false;
		Radio.py.play(j);
		Session.addToPrevList = true;
	}
	
	protected void rewind(){
		Radio.py.rewind();
	}
	
	protected void advance(){
		Radio.py.advance();
	}
	
	protected void advance(float seconds){
		Radio.py.elapsed += seconds;
	}
	
	protected void setElapsed(float seconds){
		Radio.py.elapsed = seconds;
	}
	
	protected float getElapsed(){
		return Radio.py.elapsed;
	}
	
	protected void setVolume(float volume){
		Radio.py.setVolume(volume);
	}
}