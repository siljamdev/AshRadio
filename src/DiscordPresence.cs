//using DiscordRPC;

public class DiscordPresence : IDisposable{
	DiscordRPC.DiscordRpcClient client;
	
	public DiscordPresence(){
		client = new DiscordRPC.DiscordRpcClient("1396153880420548720");
		
		client.Initialize();
		
		Song s = Song.get(Radio.py.playingSong);
		
		client.SetPresence(new DiscordRPC.RichPresence(){
			Details = s?.title ?? "Nothing playing yet",
            State = s == null ? "" : (s.authors.Length == 0 ? "" : string.Join(", ", s.authors.Select(n => (Author.get(n)?.name ?? Author.nullName)))),
            Assets = new DiscordRPC.Assets(){
                LargeImageKey = "icon", // uploaded image name from Dev Portal
                LargeImageText = "AshRadio"
            },
			Buttons = new[]{
				new DiscordRPC.Button {
					Label = "Get AshRadio",
					Url = "https://github.com/siljamdev/AshRadio"
				}
			},
			Type = DiscordRPC.ActivityType.Listening
        });
		
		Radio.py.onSongLoad += update;
	}
	
	void update(object sender, EventArgs a){
		Song s = Song.get(Radio.py.playingSong);
		
		client.UpdateDetails(s?.title ?? "Nothing playing yet");
		client.UpdateState(s == null ? "" : (s.authors.Length == 0 ? "" : string.Join(", ", s.authors.Select(n => (Author.get(n)?.name ?? Author.nullName)))));
	}
	
	public void Dispose(){
        client.Dispose();
		Radio.py.onSongLoad -= update;
    }
}