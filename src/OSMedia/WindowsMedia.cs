#if WINDOWS
using Windows.Media;
using Windows.Media.Control;
using Windows.Media.Playback;
using Windows.Storage;

class WindowsMedia : OSMedia{
	MediaPlayer mp;
	SystemMediaTransportControls smtc;
	
	public WindowsMedia() : base(){
		mp = new MediaPlayer();
		smtc = mp.SystemMediaTransportControls;
		
		smtc.IsEnabled = true;
		smtc.IsPlayEnabled = true;
		smtc.IsPauseEnabled = true;
		smtc.IsPreviousEnabled = true;
		smtc.IsNextEnabled = true;
		smtc.IsRewindEnabled = true;
		smtc.IsFastForwardEnabled = true;
		
		smtc.ButtonPressed += buttonPressed;
		
		base.init();
	}
	
	void buttonPressed(SystemMediaTransportControls sender, SystemMediaTransportControlsButtonPressedEventArgs args){
        switch (args.Button){
            case SystemMediaTransportControlsButton.Play:
                base.togglePause();
                break;
			
            case SystemMediaTransportControlsButton.Pause:
                base.togglePause();
                break;
			
            case SystemMediaTransportControlsButton.Next:
                base.skip();
                break;
			
            case SystemMediaTransportControlsButton.Previous:
                base.previous();
                break;
			
			case SystemMediaTransportControlsButton.Rewind:
				base.rewind();
				break;
			
			case SystemMediaTransportControlsButton.FastForward:
				base.advance();
				break;
        }
    }
	
	protected override void updateSong(int id, string title, string[] authors, float duration){
		smtc.DisplayUpdater.Type = MediaPlaybackType.Music;
		smtc.DisplayUpdater.MusicProperties.Title = title;
		smtc.DisplayUpdater.MusicProperties.Artist = base.authorsToString(authors);
		smtc.DisplayUpdater.Update();
	}
	
	protected override void updateState(bool paused){
		smtc.PlaybackStatus = paused ? MediaPlaybackStatus.Paused : MediaPlaybackStatus.Playing;
	}
	
	protected override void updateMode(SessionMode mode){
		
	}
}
#endif
