using System.Diagnostics;
using AshLib.Dates;
using AshLib.Formatting;

public class Song : IDisposable, INotes{
	
	//INSTANCE
	
	public string title{get; private set;}
	public int[] authors{get; private set;}
	
	public int id{get; private set;}
	
	public float duration{get; private set;}
	
	public string notes{get; private set;}
	
	public Date added{get; private set;}
	
	private Song(){
		Author.onAuthorDeleted += onAuthorsChange;
	}
	
	//used to delete deleted authors
	void onAuthorsChange(Author a){
		if(authors.Contains(a.id)){
			authors = authors.Where(id => Author.exists(id)).ToArray();
			save();
			
			onSongDetailsUpdate?.Invoke(this);
		}
	}
	
	public void setTitle(string t){
		title = t?.Trim() ?? nullTitle;
		save();
		
		onSongDetailsUpdate?.Invoke(this);
		onSongTitleUpdate?.Invoke(this);
	}
	
	public void setAuthors(int[] auth){
		auth ??= Array.Empty<int>();
		int[] p = authors;
		authors = auth;
		save();
		
		onSongDetailsUpdate?.Invoke(this);
		
		foreach(int aid in p.Union(authors)){
			Author.get(aid)?.songsChanged(); //Notify authors their songs changed (send event)
		}
	}
	
	//Will also set if needed
	public float getDuration(){
		if(duration >= 0f){
			return duration;
		}
		
		setDuration(loadDuration(id));
		return duration;
	}
	
	void setDuration(float d){
		if(d < 0f || d == duration){
			return;
		}
		
		duration = d;
		save();
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
		return Palette.song;
	}
	
	void save(){
		AshFile s2 = new AshFile();
		s2.Set("t", title);
		s2.Set("a", authors);
		s2.Set("d", duration);
		s2.Set("c", added);
		if(notes != null){
			s2.Set("n", notes);
		}else{
			s2.Remove("n");
		}
		s2.Save(getDataPath(id));
	}
	
	public override string ToString(){
		return title + " | Id: " + id.ToString() + " | " +
			(authors.Length == 0 ? Author.nullName :
			(authors.Length == 1 ? "Author: " + (Author.get(authors[0])?.name ?? Author.nullName) : "Authors: " + string.Join(", ", authors.Select(n => (Author.get(n)?.name ?? Author.nullName)))));
	}
	
	public void Dispose(){
		Author.onAuthorDeleted -= onAuthorsChange;
	}
	
	//STATIC
	
	public const string nullTitle = "Untitled song";
	
	static int latestId;
	
	//In order of being called
	public static event Action onLibraryUpdate; //Song added or deleted
	public static event Action<Song> onSongDeleted; //Song title change
	public static event Action<Song> onSongDetailsUpdate; //Song title or song authors change or song deleted
	public static event Action<Song> onSongTitleUpdate; //Song title change
	
	static List<Song> library;
	
	static AshFileModel songModel = new AshFileModel(
		new ModelInstance(ModelInstanceOperation.Type, "t", nullTitle), //title
		new ModelInstance(ModelInstanceOperation.Type, "a", Array.Empty<int>()), //authors
		new ModelInstance(ModelInstanceOperation.Type, "d", -1f), //duration
		new ModelInstance(ModelInstanceOperation.Type, "c", (Date) DateTime.Now) //Date of creation
	);
	
	public static void init(){
		init(Radio.data.GetValue<int>("songs.latestId"));
	}
	
	public static void init(int li){
		latestId = Math.Max(li, -1);
		songModel.deleteNotMentioned = true;
		saveAll();
		
		loadLibrary();
	}
	
	public static void subEvents(){
		Radio.py.onSongLoad += () => {
			Song t = get(Radio.py.playingSong);
			if(t != null){
				t.setDuration(Radio.py.duration);
			}
		};
	}
	
	static void loadLibrary(){
		library = new List<Song>(latestId + 1);
		
		for(int i = 0; i <= latestId; i++){
			library.Add(loadFile(i));
		}
	}
	
	static bool existsFile(int s){
		if(s < 0){
			return false;
		}
		return File.Exists(getAudioPath(s)) && File.Exists(getDataPath(s));
	}
	
	static Song loadFile(int s){
		if(!existsFile(s)){
			return null;
		}
		
		AshFile f = new AshFile(getDataPath(s));
		string nts = f.GetValue<string>("n"); //Notes
		
		bool save = false;
		
		if(!f.ContainsKey("c") || f.GetValueType("c") != typeof(Date)){
			f.Set("c", (Date) File.GetCreationTime(getDataPath(s)));
			save = true;
		}
		
		f *= songModel;
		
		Song s2 = new Song(){
			title = f.GetValue<string>("t"),
			authors = f.GetValue<int[]>("a"),
			duration = f.GetValue<float>("d"),
			added = f.GetValue<Date>("c"),
			notes = nts,
			id = s
		};
		
		if(save){
			s2.save();
		}
		
		return s2;
	}
	
	public static bool exists(int s){
		if(s < 0 || s >= library.Count){
			return false;
		}
		return library[s] != null;
	}
	
	public static Song get(int s){
		if(exists(s)){
			return library[s];
		}else{
			return null;
		}
	}
	
	//True if succesful
	public static bool export(int id, string dirPath, int? index, out string err){
		if(!exists(id)){
			err = "Song does not exist";
			return false;
		}
		
		try{
			string path;
			if(index != null){
				path = dirPath + "/" + index + ". " + safePath(get(id)?.title ?? nullTitle) + ".mp3";
			}else{
				path = dirPath + "/" + safePath(get(id)?.title ?? nullTitle) + ".mp3";
			}
			
			File.Copy(getAudioPath(id), path);
			File.SetLastWriteTime(path, DateTime.Now);
			
			err = null;
			return true;
		}catch(Exception e){
			err = "Error exporting:\n" + e.ToString();
			return false;
		}
	}
	
	public static void delete(int id){
		Song s = get(id);
		
		if(s == null){
			return;
		}
		
		try{
			File.Delete(getAudioPath(id));
			File.Delete(getDataPath(id));
		}catch(Exception e){
			
		}
		
		s.Dispose();
		library[id] = null;
		
		onLibraryUpdate?.Invoke();
		onSongDeleted?.Invoke(s);
		onSongDetailsUpdate?.Invoke(s);
		
		foreach(int aid in s.authors){
			Author.get(aid)?.songsChanged(); //Notify authors their songs changed (send event)
		}
	}
	
	public static string getAudioPath(int s){
		if(s < 0){
			return null;
		}
		
		return Radio.dep.path + "/songs/files/" + s.ToString() + ".mp3";
	}
	
	static string getDataPath(int s){
		if(s < 0){
			return null;
		}
		
		return Radio.dep.path + "/songs/data/" + s.ToString() + ".ash";
	}
	
	public static int import(string path, string title, int[] authors, out string err){
		if(!File.Exists(path)){
			err = "File does not exist";
			return -1;
		}
		
		if(string.IsNullOrWhiteSpace(title)){
			title = Path.GetFileNameWithoutExtension(path);
		}
		
		title = title.Trim().Replace("\r\n", " ").Replace("\n", " ").Replace("\r", " ");
		
		authors ??= Array.Empty<int>();
		
		if(Path.GetExtension(path).Equals(".mp3", StringComparison.OrdinalIgnoreCase)){
			latestId++;
			try{
				File.Copy(path, getAudioPath(latestId));
			}catch(Exception e){
				err = "Error copying file:\n" + e.ToString();
				if(File.Exists(getAudioPath(latestId))){
					File.Delete(getAudioPath(latestId));
				}
				latestId--;
				return -1;
			}
			
			Song s2 = new Song(){
				title = title,
				authors = authors,
				added = (Date) DateTime.Now,
				id = latestId
			};
			
			library.Add(s2);
			
			s2.setDuration(loadDuration(latestId)); //loadDuration needs it to be added to library first
			s2.save();
			
			saveAll();
			
			onLibraryUpdate?.Invoke();
			
			foreach(int aid in authors){
				Author.get(aid)?.songsChanged(); //Notify authors their songs changed (send event)
			}
			
			err = null;
			return latestId;
		}
		
		latestId++;
		bool t = tryConvertToMp3(path, getAudioPath(latestId), out err);
		if(!t){
			try{
				if(File.Exists(getAudioPath(latestId))){
					File.Delete(getAudioPath(latestId));
				}
			}catch(Exception e){
				err = "Error deleting file:\n" + e.ToString();
			}
			latestId--;
			return -1;
		}
		
		Song s = new Song(){
			title = title,
			authors = authors,
			duration = loadDuration(latestId),
			id = latestId
		};
		s.save();
		
		library.Add(s);
		
		saveAll();
		
		onLibraryUpdate?.Invoke();
		
		foreach(int aid in authors){
			Author.get(aid)?.songsChanged(); //Notify authors their songs changed (send event)
		}
		
		return latestId;
	}
	
	static bool tryConvertToMp3(string inputFile, string outputFile, out string err){
		err = null;
		try{
			var startInfo = new ProcessStartInfo{
				FileName = getFfmpegPath(),
				Arguments = "-y -i \"" + inputFile + "\" -codec:a libmp3lame -qscale:a 0 \"" + outputFile + "\"",
				RedirectStandardError = true,
				RedirectStandardOutput = true,
				UseShellExecute = false,
				CreateNoWindow = true
			};
			
			using var process = Process.Start(startInfo);
			
			string stdErr = process.StandardError.ReadToEnd();
			string stdOut = process.StandardOutput.ReadToEnd();
			
			process.WaitForExit();
			
			if(process.ExitCode != 0){
				err = $"FFmpeg exited with code {process.ExitCode}:\n{stdErr}";
				return false;
			}
			
			return true;
		}catch(Exception ex){
			err = ex.Message;
			return false;
		}
	}
	
	static float loadDuration(int id){
		if(!File.Exists(getAudioPath(id))){
			return -1f;
		}
		
		try{
			var startInfo = new ProcessStartInfo{
				FileName = getFfprobePath(),
				Arguments = "-v error -show_entries format=duration -of default=noprint_wrappers=1:nokey=1 \"" + getAudioPath(id) + "\"",
				RedirectStandardError = true,
				RedirectStandardOutput = true,
				UseShellExecute = false,
				CreateNoWindow = true
			};
			
			using var process = Process.Start(startInfo);
			
			string stdOut = process.StandardOutput.ReadToEnd();
			
			process.WaitForExit();
			
			if(process.ExitCode != 0){
				return 0f;
			}
			
			if(float.TryParse(stdOut, out float f)){
				return f;
			}else{
				return 0f;
			}
		}catch(Exception ex){
			return 0f;
		}
	}
	
	public static async Task<float[]> getDurationsAsync(int[] ids){
		Task<float>[] tasks = ids.Select(id => Task.Run(() => get(id)?.getDuration() ?? -1f)).ToArray();
		
		return await Task.WhenAll(tasks);
	}
	
	public static List<Song> getLibrary(){
		return library.Where(h => h != null).ToList();
	}
	
	public static void repairLatestId(){
		string[] mp3Files = Directory.GetFiles(Radio.dep.path + "/songs/files", "*.mp3");
		mp3Files = mp3Files.Select(f => Path.GetFileNameWithoutExtension(f)).ToArray();
		
		int b = -1;
		foreach(string s in mp3Files){
			if(int.TryParse(s, out int i) && File.Exists(getDataPath(i)) && i > b){
				b = i;
			}
		}
		
		init(b);
	}
	
	static void saveAll(){
		Radio.data.Set("songs.latestId", latestId);
		Radio.data.Save();
	}
	
	static string getFfmpegPath(){
		return Radio.config.GetValue<string>("ffmpegPath");
	}
	
	static string getFfprobePath(){
		return Radio.config.GetValue<string>("ffprobePath");
	}
	
	static string safePath(string p){
		char[] invalid = Path.GetInvalidFileNameChars();
		
		return new string(p.Select(c => invalid.Contains(c) ? '_' : c).ToArray());
	}
}