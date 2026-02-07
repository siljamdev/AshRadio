#if MACOS_WORKS
using System;
using System.Linq;
using Foundation;
using MediaPlayer;

class MacosMedia : OSMedia{
    private MPNowPlayingInfoCenter nowPlaying => MPNowPlayingInfoCenter.DefaultCenter;
    private MPRemoteCommandCenter commandCenter => MPRemoteCommandCenter.Shared;

    private bool isPlaying = false;

    public MacosMedia() : base(){
        RegisterCommands();
        init();
    }

    protected override void updateSong(int id, string title, string[] authors, float duration){
        MPNowPlayingInfo info = new MPNowPlayingInfo{
            Title = title,
            Artist = base.authorsToString(authors),
            PlaybackDuration = duration,
            ElapsedPlaybackTime = getElapsed(),
            PlaybackRate = isPlaying ? 1.0 : 0.0
        };

        nowPlaying.NowPlaying = info;
    }

    protected override void updateState(bool paused){
        isPlaying = !paused;

        MPNowPlayingInfo info = nowPlaying.NowPlaying;
        if(info != null){
            info.PlaybackRate = isPlaying ? 1.0 : 0.0;
            info.ElapsedPlaybackTime = getElapsed();
            nowPlaying.NowPlaying = info;
        }
    }

    protected override void updateMode(SessionMode mode){}

    private void RegisterCommands(){
        commandCenter.PlayCommand.Enabled = true;
        commandCenter.PlayCommand.AddTarget(_ => {
            togglePause();
            return MPRemoteCommandHandlerStatus.Success;
        });

        commandCenter.PauseCommand.Enabled = true;
        commandCenter.PauseCommand.AddTarget(_ => {
            togglePause();
            return MPRemoteCommandHandlerStatus.Success;
        });

        commandCenter.NextTrackCommand.Enabled = true;
        commandCenter.NextTrackCommand.AddTarget(_ => {
            skip();
            return MPRemoteCommandHandlerStatus.Success;
        });

        commandCenter.PreviousTrackCommand.Enabled = true;
        commandCenter.PreviousTrackCommand.AddTarget(_ => {
            previous();
            return MPRemoteCommandHandlerStatus.Success;
        });

        commandCenter.SeekBackwardCommand.Enabled = true;
        commandCenter.SeekBackwardCommand.AddTarget(_ => {
            rewind();
            return MPRemoteCommandHandlerStatus.Success;
        });

        commandCenter.SeekForwardCommand.Enabled = true;
        commandCenter.SeekForwardCommand.AddTarget(_ => {
            advance();
            return MPRemoteCommandHandlerStatus.Success;
        });
    }
}
#endif