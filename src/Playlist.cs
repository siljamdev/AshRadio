public class Playlist{
	
	//INSTANCE
	
	public string title{get; private set;}
	List<int> songs = new List<int>();
	
	public int id{get; private set;}
	
	public void setTitle(string n){
		title = n?.Trim() ?? "Untitled playlist";
		save();
	}
	
	public List<int> getSongsIds(){
		return new List<int>(songs);
	}
	
	public void addSong(int s){
		songs.Add(s);
		save();
	}
	
	public void deleteSong(int index){
		songs.RemoveAt(index);
		save();
	}
	
	public void moveSong(int index, int newIndex){
		int t = songs[index];
		songs.RemoveAt(index);
		songs.Insert(newIndex, t);
		save();
	}
	
	void save(){
		playlistsFile.Set(id.ToString() + ".t", title);
		playlistsFile.Set(id.ToString() + ".s", songs.ToArray());
		playlistsFile.Save();
		
		onPlaylistUpdate?.Invoke(null, new PlaylistEventArgs(id));
	}
	
	//STATIC
	
	static AshFile playlistsFile = null!;
	
	static int latestId;
	
	public static event EventHandler<PlaylistEventArgs> onPlaylistUpdate;
	
	public static void init(int li){
		latestId = Math.Max(li, -1);
		playlistsFile = new AshFile(Radio.dep.path + "/playlists.ash");
		saveAll();
	}
	
	public static bool exists(int id){
		if(id < 0){
			return false;
		}
		return playlistsFile.TryGetValue(id.ToString() + ".t", out string t) && playlistsFile.TryGetValue(id.ToString() + ".s", out int[] s);
	}
	
	public static Playlist load(int id2){
		if(playlistsFile.TryGetValue(id2.ToString() + ".t", out string t) && playlistsFile.TryGetValue(id2.ToString() + ".s", out int[] s)){
			return new Playlist(){
				title = t,
				songs = s.ToList(),
				id = id2
			};
		}
		return null;
	}
	
	public static int create(string title){
		latestId++;
		
		playlistsFile.Set(latestId.ToString() + ".t", title);
		playlistsFile.Set(latestId.ToString() + ".s", Array.Empty<int>());
		playlistsFile.Save();
		
		onPlaylistUpdate?.Invoke(null, new PlaylistEventArgs(latestId));
		
		saveAll();
		
		return latestId;
	}
	
	public static void delete(int id){
		if(!exists(id)){
			return;
		}
		
		playlistsFile.Remove(id.ToString() + ".t");
		playlistsFile.Remove(id.ToString() + ".s");
		playlistsFile.Save();
		
		onPlaylistUpdate?.Invoke(null, new PlaylistEventArgs(id));
	}
	
	public static List<int> getAllIds(){
		List<int> a = new(playlistsFile.Count / 2);
		
		foreach(var kvp in playlistsFile){
			string[] div = kvp.Key.Split(".");
			if(kvp.Value is string && div.Length == 2 && div[1] == "t" && int.TryParse(div[0], out int i) && i > -1 && playlistsFile.TryGetValue(i.ToString() + ".s", out int[] s)){
				a.Add(i);
			}
		}
		
		return a;
	}
	
	static void saveAll(){
		Radio.config.Set("playlists.latestId", latestId);
		Radio.config.Save();
	}
}

public class PlaylistEventArgs : EventArgs{
	public int id;
	
	public PlaylistEventArgs(int i){
		id = i;
	}
}