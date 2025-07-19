using DiscordRPC;

public class DiscordPresence : IDisposable{
	DiscordRpcClient client;
	
	public DiscordPresence(){
		client = new DiscordRpcClient("1396153880420548720");
		
		client.Initialize();
		
		Song s = Song.load(Radio.py.playingSong);
		
		client.SetPresence(new RichPresence(){
			Details = s?.title ?? "Nothing playing yet",
            State = s == null ? "" : (s.authors.Length == 0 ? "" : (s.authors.Length == 1 ? (Author.load(s.authors[0])?.name ?? "Unknown author") : string.Join(", ", s.authors.Select(n => (Author.load(n)?.name ?? "Unknown author"))))),
            Assets = new Assets(){
                LargeImageKey = "icon", // uploaded image name from Dev Portal
                LargeImageText = "AshRadio"
            },
			Buttons = new[]{
				new DiscordRPC.Button {
					Label = "Get AshRadio",
					Url = "https://github.com/siljamdev/AshRadio"
				}
			},
			Type = ActivityType.Listening
        });
		
		Radio.py.onSongLoad += (se, e) => {
			Song s = Song.load(Radio.py.playingSong);
			
			client.UpdateDetails(s?.title ?? "Nothing playing yet");
			client.UpdateState(s == null ? "" : (s.authors.Length == 0 ? "" : (s.authors.Length == 1 ? (Author.load(s.authors[0])?.name ?? "Unknown author") : string.Join(", ", s.authors.Select(n => (Author.load(n)?.name ?? "Unknown author"))))));
		};
	}
	
	public void Dispose(){
        client.Dispose();
    }
}