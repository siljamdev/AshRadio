using System.Diagnostics;

public class Song{
	
	//INSTANCE
	
	public string title{get; private set;}
	public int[] authors{get; private set;}
	
	public int id{get; private set;}
	
	public float duration{get; private set;}
	
	public void setTitle(string t){
		title = t?.Trim() ?? nullTitle;
		save();
		
		onLibraryUpdate?.Invoke(null, new LibraryEventArgs(authors));
	}
	
	public void setAuthors(int[] auth){
		auth ??= Array.Empty<int>();
		int[] p = authors;
		authors = auth;
		save();
		
		onLibraryUpdate?.Invoke(null, new LibraryEventArgs(p.Union(authors).ToArray()));
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
	
	void save(){
		AshFile s2 = new AshFile();
		s2.Set("t", title);
		s2.Set("a", authors);
		s2.Set("d", duration);
		s2.Save(getDataPath(id));
	}
	
	public override string ToString(){
		return title + " | Id: " + id.ToString() + " | " +
			(authors.Length == 0 ? Author.nullName :
			(authors.Length == 1 ? "Author: " + (Author.get(authors[0])?.name ?? Author.nullName) : "Authors: " + string.Join(", ", authors.Select(n => (Author.get(n)?.name ?? Author.nullName)))));
	}
	
	//STATIC
	
	public const string nullTitle = "Untitled song";
	
	static int latestId;
	
	public static event EventHandler<LibraryEventArgs> onLibraryUpdate;
	
	static List<Song> library;
	
	static AshFileModel songModel = new AshFileModel(
		new ModelInstance(ModelInstanceOperation.Type, "t", nullTitle), //title
		new ModelInstance(ModelInstanceOperation.Type, "a", Array.Empty<int>()), //authors
		new ModelInstance(ModelInstanceOperation.Type, "d", -1f) //duration
	);
	
	public static void init(int li){
		latestId = Math.Max(li, -1);
		songModel.deleteNotMentioned = true;
		saveAll();
		
		loadLibrary();
	}
	
	public static void subEvents(){
		Radio.py.onSongLoad += (s, a) => {
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
		f *= songModel;
		
		return new Song(){
			title = f.GetValue<string>("t"),
			authors = f.GetValue<int[]>("a"),
			duration = f.GetValue<float>("d"),
			id = s
		};
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
	public static bool export(int id, string dirPath, out string err){
		if(!exists(id)){
			err = "Song does not exist";
			return false;
		}
		
		try{
			string path = dirPath + "/" + get(id)?.title + ".mp3";
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
		
		library[id] = null;
		
		onLibraryUpdate?.Invoke(null, new LibraryEventArgs(s.authors));
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
				duration = loadDuration(latestId),
				id = latestId
			};
			s2.save();
			
			library.Add(s2);
			
			saveAll();
			
			onLibraryUpdate?.Invoke(null, new LibraryEventArgs(authors));
			
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
		
		onLibraryUpdate?.Invoke(null, new LibraryEventArgs(authors));
		
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
		if(!exists(id)){
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
}

public class LibraryEventArgs : EventArgs{
	public int[] auth;
	
	public LibraryEventArgs(int[] a){
		auth = a;
	}
}