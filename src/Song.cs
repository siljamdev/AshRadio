using System.Diagnostics;

public class Song{
	
	//INSTANCE
	
	public string title{get; private set;}
	public int[] authors{get; private set;}
	
	public int id{get; private set;}
	
	public void setTitle(string t){
		title = t?.Trim() ?? "Untitled song";
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
	
	void save(){
		AshFile s2 = new AshFile();
		s2.Set("t", title);
		s2.Set("a", authors);
		s2.Save(getDataPath(id));
	}
	
	public override string ToString(){
		return title + " | Id: " + id.ToString() + " | " +
			(authors.Length == 0 ? "Unknown author" :
			(authors.Length == 1 ? "Author: " + (Author.get(authors[0])?.name ?? "Unknown author") : "Authors: " + string.Join(", ", authors.Select(n => (Author.get(n)?.name ?? "Unknown author")))));
	}
	
	//STATIC
	
	static int latestId;
	
	public static event EventHandler<LibraryEventArgs> onLibraryUpdate;
	
	static List<Song> library;
	
	static AshFileModel songModel = new AshFileModel(
		new ModelInstance(ModelInstanceOperation.Type, "t", "Untitled song"), //title
		new ModelInstance(ModelInstanceOperation.Type, "a", Array.Empty<int>()) //authors
	);
	
	public static void init(int li){
		latestId = Math.Max(li, -1);
		songModel.deleteNotMentioned = true;
		saveAll();
		
		loadLibrary();
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
	
	public static void delete(int id){
		Song s = get(id);
		
		if(s == null){
			return;
		}
		
		File.Delete(getAudioPath(id));
		File.Delete(getDataPath(id));
		
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
		
		title = title.Replace("\r\n", " ").Replace("\n", " ").Replace("\r", " ");
		
		authors ??= Array.Empty<int>();
		
		if(Path.GetExtension(path).Equals(".mp3", StringComparison.OrdinalIgnoreCase)){
			latestId++;
			try{
				File.Copy(path, getAudioPath(latestId));
			}catch(Exception e){
				err = $"Error copying file:\n{e.ToString}";
				if(File.Exists(getAudioPath(latestId))){
					File.Delete(getAudioPath(latestId));
				}
				latestId--;
				return -1;
			}
			
			AshFile s2 = new AshFile();
			s2.Set("t", title);
			s2.Set("a", authors);
			s2.Save(getDataPath(latestId));
			
			library.Add(new Song(){
				title = title,
				authors = authors,
				id = latestId
			});
			
			saveAll();
			
			onLibraryUpdate?.Invoke(null, new LibraryEventArgs(authors));
			
			err = null;
			return latestId;
		}
		
		latestId++;
		bool t = tryConvertToMp3(path, getAudioPath(latestId), out err);
		if(!t){
			if(File.Exists(getAudioPath(latestId))){
				File.Delete(getAudioPath(latestId));
			}
			latestId--;
			return -1;
		}
		
		AshFile s = new AshFile();
		s.Set("t", title);
		s.Set("a", authors);
		s.Save(getDataPath(latestId));
		
		library.Add(new Song(){
			title = title,
			authors = authors,
			id = latestId
		});
		
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
	
	public static List<Song> getLibrary(){
		return library.Where(h => h != null).ToList();
		//string[] mp3Files = Directory.GetFiles(Radio.dep.path + "/songs/files", "*.mp3");
		//mp3Files = mp3Files.Select(f => Path.GetFileNameWithoutExtension(f)).ToArray();
		//
		//List<int> f = new(mp3Files.Length);
		//foreach(string s in mp3Files){
		//	if(int.TryParse(s, out int i) && File.Exists(getDataPath(i))){
		//		f.Add(i);
		//	}
		//}
		//f.Sort();
		//return f;
	}
	
	static void saveAll(){
		Radio.config.Set("songs.latestId", latestId);
		Radio.config.Save();
	}
	
	static string getFfmpegPath(){
		return Radio.config.GetValue<string>("ffmpegPath");
	}
}

public class LibraryEventArgs : EventArgs{
	public int[] auth;
	
	public LibraryEventArgs(int[] a){
		auth = a;
	}
}