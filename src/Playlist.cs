using AshLib.Formatting;

public class Playlist : IDisposable, INotes{
	
	//INSTANCE
	
	public string title{get; private set;}
	List<int> songs = new List<int>();
	
	public int id{get; private set;}
	
	public string notes{get; private set;}
	
	private Playlist(){
		Song.onSongDeleted += onLibChange;
	}
	
	//used to delete deleted songs
	void onLibChange(int s){
		if(songs.Contains(s)){
			songs = songs.Where(id => Song.exists(id)).ToList();
			save();
			
			onPlaylistDetailsUpdate?.Invoke(this.id);
		}
	}
	
	public void setTitle(string n){
		title = n?.Trim() ?? nullTitle;
		save();
		
		onPlaylistDetailsUpdate?.Invoke(this.id);
		onPlaylistTitleUpdate?.Invoke(this.id);
	}
	
	//INOTES interface
	public void setNotes(string n){
		notes = n.Trim();
		if(string.IsNullOrEmpty(notes)){
			notes = null;
		}
		save();
	}
	
	//INOTES interface
	public string getTitle(){
		return title;
	}
	
	//INOTES interface
	public CharFormat? getStyle(){
		return Palette.playlist;
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
		
		onPlaylistDetailsUpdate?.Invoke(this.id);
	}
	
	public void addSongs(IEnumerable<int> ids){
		songs.AddRange(ids);
		save();
		
		onPlaylistDetailsUpdate?.Invoke(this.id);
	}
	
	public void deleteSongAt(int index){
		songs.RemoveAt(index);
		save();
		
		onPlaylistDetailsUpdate?.Invoke(this.id);
	}
	
	public bool deleteSongs(IEnumerable<int> sids){
		int r = songs.RemoveAll(id => sids.Contains(id));
		if(r > 0){
			save();
			
			onPlaylistDetailsUpdate?.Invoke(this.id);
			return true;
		}
		return false;
	}
	
	public void moveSong(int index, int newIndex){
		int t = songs[index];
		songs.RemoveAt(index);
		songs.Insert(newIndex, t);
		save();
		
		onPlaylistDetailsUpdate?.Invoke(this.id);
	}
	
	void save(){
		playlistsFile.Set(id.ToString() + ".t", title);
		playlistsFile.Set(id.ToString() + ".s", songs.ToArray());
		if(notes != null){
			playlistsFile.Set(id.ToString() + ".n", notes);
		}else{
			playlistsFile.Remove(id.ToString() + ".n");
		}
		playlistsFile.Save();
	}
	
	public void Dispose(){
		Song.onSongDeleted -= onLibChange;
	}
	
	//STATIC
	
	public const string nullTitle = "Untitled playlist";
	
	static AshFile playlistsFile = null!;
	
	static int latestId;
	
	static List<Playlist> playlists;
	
	public static event Action onPlaylistsUpdate; //Created, deleted
	public static event Action<int> onPlaylistDeleted; //deleted
	public static event Action<int> onPlaylistDetailsUpdate; //Deletion, Name change, song contents
	public static event Action<int> onPlaylistTitleUpdate; //Name change
	
	public static void init(){
		init(Radio.data.GetValue<int>("playlists.latestId"));
	}
	
	static void init(int li){
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
		
		title = title.Trim().Replace("${id}", latestId.ToString());
		
		playlistsFile.Set(latestId.ToString() + ".t", title);
		playlistsFile.Set(latestId.ToString() + ".s", Array.Empty<int>());
		playlistsFile.Save();
		
		playlists.Add(new Playlist(){
			title = title,
			songs = Array.Empty<int>().ToList(),
			id = latestId
		});
		
		saveAll();
		
		onPlaylistsUpdate?.Invoke();
		
		return latestId;
	}
	
	public static void delete(int id){
		if(!exists(id)){
			return;
		}
		
		Playlist p = get(id);
		
		playlistsFile.Remove(id.ToString() + ".t");
		playlistsFile.Remove(id.ToString() + ".s");
		playlistsFile.Save();
		
		playlists[id]?.Dispose();
		playlists[id] = null;
		
		onPlaylistsUpdate?.Invoke();
		onPlaylistDeleted?.Invoke(id);
		onPlaylistDetailsUpdate?.Invoke(p.id);
	}
	
	public static List<Playlist> getAllPlaylists(){
		return playlists.Where(h => h != null).ToList();
	}
	
	public static List<int> getAllIds(){
		return getAllPlaylists().Select(h => h.id).ToList();
	}
	
	public static void repairLatestId(){
		int b = -1;
		foreach(string s in playlistsFile.Keys){
			string[] a = s.Split(".");
			if(a.Length == 2 && int.TryParse(a[0], out int i) && playlistsFile.ContainsKey(a[0] + ".t") && playlistsFile.ContainsKey(a[0] + ".s") && i > b){
				b = i;
			}
		}
		
		init(b);
	}
	
	static void saveAll(){
		Radio.data.Set("playlists.latestId", latestId);
		Radio.data.Save();
	}
}