//using DiscordRPC;

public class DiscordPresence : IDisposable{
	DiscordRPC.DiscordRpcClient client;
	
	public DiscordPresence(){
		client = new DiscordRPC.DiscordRpcClient("1396153880420548720");
		
		client.Initialize();
		
		Song s = Song.get(Radio.py.playingSong);
		
		client.SetPresence(new DiscordRPC.RichPresence(){
			Details = s?.title ?? "Nothing playing yet",
            State = s == null ? "" : (s.authors.Length == 0 ? "" : (s.authors.Length == 1 ? (Author.get(s.authors[0])?.name ?? "Unknown author") : string.Join(", ", s.authors.Select(n => (Author.get(n)?.name ?? "Unknown author"))))),
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
		
		Radio.py.onSongLoad += (se, e) => {
			Song s = Song.get(Radio.py.playingSong);
			
			client.UpdateDetails(s?.title ?? "Nothing playing yet");
			client.UpdateState(s == null ? "" : (s.authors.Length == 0 ? "" : (s.authors.Length == 1 ? (Author.get(s.authors[0])?.name ?? "Unknown author") : string.Join(", ", s.authors.Select(n => (Author.get(n)?.name ?? "Unknown author"))))));
		};
	}
	
	public void Dispose(){
        client.Dispose();
    }
}