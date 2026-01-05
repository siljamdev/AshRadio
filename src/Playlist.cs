public class Playlist{
	
	//INSTANCE
	
	public string title{get; private set;}
	List<int> songs = new List<int>();
	
	public int id{get; private set;}
	
	public void setTitle(string n){
		title = n?.Trim() ?? nullTitle;
		save();
	}
	
	public List<int> getSongsIds(){
		return new List<int>(songs);
	}
	
	public List<Song> getSongs(){
		return songs.Select(h => Song.get(h)).Where(h => h != null).ToList();
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
	
	public const string nullTitle = "Untitled playlist";
	
	static AshFile playlistsFile = null!;
	
	static int latestId;
	
	static List<Playlist> playlists;
	
	public static event EventHandler<PlaylistEventArgs> onPlaylistUpdate;
	
	public static void init(int li){
		latestId = Math.Max(li, -1);
		playlistsFile = new AshFile(Radio.dep.path + "/playlists.ash");
		saveAll();
		
		loadPlaylists();
	}
	
	static void loadPlaylists(){
		playlists = new List<Playlist>(playlistsFile.Count / 2);
		
		for(int i = 0; i <= latestId; i++){
			playlists.Add(load(i));
		}
	}
	
	static Playlist load(int id2){
		if(playlistsFile.TryGetValue(id2.ToString() + ".t", out string t) && playlistsFile.TryGetValue(id2.ToString() + ".s", out int[] s)){
			return new Playlist(){
				title = t,
				songs = s.ToList(),
				id = id2
			};
		}
		return null;
	}
	
	public static bool exists(int id){
		if(id < 0 || id >= playlists.Count){
			return false;
		}
		return playlists[id] != null;
	}
	
	public static Playlist get(int id){
		if(exists(id)){
			return playlists[id];
		}else{
			return null;
		}
	}
	
	public static int create(string title){
		latestId++;
		
		playlistsFile.Set(latestId.ToString() + ".t", title);
		playlistsFile.Set(latestId.ToString() + ".s", Array.Empty<int>());
		playlistsFile.Save();
		
		playlists.Add(new Playlist(){
			title = title,
			songs = Array.Empty<int>().ToList(),
			id = latestId
		});
		
		saveAll();
		
		onPlaylistUpdate?.Invoke(null, new PlaylistEventArgs(latestId));
		
		return latestId;
	}
	
	public static void delete(int id){
		if(!exists(id)){
			return;
		}
		
		playlistsFile.Remove(id.ToString() + ".t");
		playlistsFile.Remove(id.ToString() + ".s");
		playlistsFile.Save();
		
		playlists[id] = null;
		
		onPlaylistUpdate?.Invoke(null, new PlaylistEventArgs(id));
	}
	
	public static List<Playlist> getAllPlaylists(){
		return playlists.Where(h => h != null).ToList();
	}
	
	public static List<int> getAllIds(){
		return getAllPlaylists().Select(h => h.id).ToList();
	}
	
	static void saveAll(){
		Radio.data.Set("playlists.latestId", latestId);
		Radio.data.Save();
	}
}

public class PlaylistEventArgs : EventArgs{
	public int id;
	
	public PlaylistEventArgs(int i){
		id = i;
	}
}