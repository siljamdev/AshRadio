global using System;
global using AshLib;
global using AshLib.AshFiles;
using System.Diagnostics;
using AshLib.Folders;

public static class Radio{
	public const string version = "1.1.0";
	
	public static Dependencies dep = null!;
	public static AshFile config = null!;
	
	public static Player py;
	public static Screens sc;
	
	public static void Main(string[] args){
		try{
			File.Delete("error.log");
		}catch{}
		
		try{
			init();
			
			//debug();
			
			sc.play();
		}catch(Exception e){
			Console.WriteLine(e);
			File.AppendAllText("error.log", e.ToString());
		}
	}
	
	static void debug(){
		//importSingleVideo("https://www.youtube.com/watch?v=lT57yUqdKSk", "", new string[0], null);
		
		//Playlist.create("test");
		//Playlist p = Playlist.load(0);
		//
		//p.addSong(14);
		//p.addSong(3);
		//p.addSong(4);
		
		//Session.setSource(SourceType.Playlist, 0);
		//Session.setSource(SourceType.Library);
		//Session.setMode(SessionMode.Order);
		
		//py.askForSong();
		
		//Random rand = new Random();
		//while(true){
		//	Console.ReadLine();
		//	var p = Player.getDeviceList().ToList();
		//	py.setDevice(p[rand.Next(p.Count)].Value);
		//	py.resume();
		//}
	}
	
	//Complete init logic
	static void init(){
		string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
		dep = new Dependencies(appDataPath + "/ashproject/ashradio", true, new string[]{"songs", "songs/files", "songs/data", "import"}, null);
		config = dep.config;
		
		initConfig();
		
		Song.init(config.GetCamp<int>("songs.latestId"));
		Author.init(config.GetCamp<int>("authors.latestId"));
		Playlist.init(config.GetCamp<int>("playlists.latestId"));
		Session.init((SessionMode) config.GetCamp<int>("session.mode"), (SourceType) config.GetCamp<int>("session.sourceType"), config.GetCamp<int>("session.sourceIdentifier"), config.GetCamp<int[]>("session.sourceSeen"));
		
		py = new Player(config.GetCamp<int>("player.song"), config.GetCamp<int>("player.volume"), config.GetCamp<float>("player.volumeExponent"), config.GetCamp<float>("player.elapsed"));
		
		Palette.init();
		sc = new Screens();
		
		AppDomain.CurrentDomain.ProcessExit += onExit;
		
		string[] dirs = Directory.GetDirectories(dep.path + "/import").Select(dir => Path.GetFileName(dir)).ToArray();
		
		foreach(string d in dirs){
			importAll(d, new string[0], out string a2); //If app closed while songs where importing, import them
		}
		
		importAll("", new string[0], out string a);
	}
	
	static void initConfig(){
		AshFileModel m = new AshFileModel(
			new ModelInstance(ModelInstanceOperation.Type, "player.song", -1),
			new ModelInstance(ModelInstanceOperation.Type, "player.volume", 100),
			new ModelInstance(ModelInstanceOperation.Type, "player.volumeExponent", 2f),
			new ModelInstance(ModelInstanceOperation.Type, "player.elapsed", 0f),
			new ModelInstance(ModelInstanceOperation.Type, "player.advanceTime", 10f),
			
			new ModelInstance(ModelInstanceOperation.Type, "session.mode", 0),
			new ModelInstance(ModelInstanceOperation.Type, "session.sourceType", 0),
			new ModelInstance(ModelInstanceOperation.Type, "session.sourceIdentifier", -1),
			new ModelInstance(ModelInstanceOperation.Type, "session.sourceSeen", Array.Empty<int>()),
			
			new ModelInstance(ModelInstanceOperation.Type, "songs.latestId", -1),
			new ModelInstance(ModelInstanceOperation.Type, "authors.latestId", -1),
			new ModelInstance(ModelInstanceOperation.Type, "playlists.latestId", -1),
			
			new ModelInstance(ModelInstanceOperation.Type, "ffmpegPath", "ffmpeg"),
			new ModelInstance(ModelInstanceOperation.Type, "ytdlpPath", "yt-dlp"),
			
			new ModelInstance(ModelInstanceOperation.Type, "ui.useColors", true),
			new ModelInstance(ModelInstanceOperation.Type, "ui.palette.user", Color3.Yellow),
			new ModelInstance(ModelInstanceOperation.Type, "ui.palette.song", new Color3("3295FF")),
			new ModelInstance(ModelInstanceOperation.Type, "ui.palette.author", Color3.Green),
			new ModelInstance(ModelInstanceOperation.Type, "ui.palette.playlist", new Color3("FFA811")),
			new ModelInstance(ModelInstanceOperation.Type, "ui.palette.main", new Color3("E7484B")),
			new ModelInstance(ModelInstanceOperation.Type, "ui.palette.delimiter", new Color3("5B2D72")),
			new ModelInstance(ModelInstanceOperation.Type, "ui.palette.hint", new Color3("9F60C1")),
			new ModelInstance(ModelInstanceOperation.Type, "ui.palette.info", new Color3("849DD6")),
			new ModelInstance(ModelInstanceOperation.Type, "ui.palette.background", new Color3("101010")),
			new ModelInstance(ModelInstanceOperation.Type, "ui.palette.error", new Color3("D83F3C"))
		);
		
		m.deleteNotMentioned = true;
		
		config *= m;
		
		//Set current version and path. Might be needed by someone (maybe)
		config.SetCamp("version", version);
		try{ //Might not work on linux
			config.SetCamp("path", System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);
		}catch{}
		
		config.Save();
	}
	
	public static int importSingleFile(string path, string title, string[] authors, out string err){
		if(string.IsNullOrEmpty(title.Trim())){
			title = Path.GetFileNameWithoutExtension(path);
		}
		return Song.import(path, title, Author.getAuthors(authors), out err);
	}
	
	public static async Task<int> importSingleVideo(string url, string title, string[] authors, Action<string> onErr){
		if(string.IsNullOrEmpty(title.Trim())){
			title = "%(title)s";
		}
		
		string path = dep.path + "/import/" + title + ".%(ext)s";
		
		var psi = new ProcessStartInfo{
			FileName = geYtDlpPath(),
			Arguments = "-x --audio-format mp3 --audio-quality 0 -o \"" + path + "\" --no-mtime --no-playlist --user-agent \"Mozilla/5.0 (Windows NT 10.0; Win64; x64)\" \"" + url + "\"",
			UseShellExecute = false,
			CreateNoWindow = true,
            RedirectStandardError = true,
			WindowStyle = ProcessWindowStyle.Hidden,
		};
		
		// Start the process
		var process = new Process { StartInfo = psi };
		
        process.ErrorDataReceived += (s, a) => {
			if(a.Data != null){
				onErr?.Invoke(a.Data);
			}
		};
		
		try{
			process.Start();
			//process.BeginOutputReadLine();
			process.BeginErrorReadLine();
			process.WaitForExit();
			
			int e = process.ExitCode;
			if(e != 0){
				onErr?.Invoke("yt-dlp exit code: " + e);
			}
		}catch(Exception ex){
			onErr?.Invoke(ex.Message);
		}
		
		List<int> s = importAll("", authors, out string err2);
		onErr?.Invoke(err2);
		if(s.Count > 0){
			return s[0];
		}
		
		return -1;
	}
	
	public static bool importFromFolder(string path, string[] authors, out string err){
		err = "";
		try{
			if(!Directory.Exists(path)){
				err += "Folder does not exist";
				return false;
			}
			
			
			int[] aus = Author.getAuthors(authors);
			
			string[] files = Directory.GetFiles(path, "*");
			
			foreach(string p in files){
				Song.import(p, Path.GetFileNameWithoutExtension(p), aus, out string err2);
				if(!string.IsNullOrEmpty(err2)){
					err += err2 + "\n";
				}
			}
			
			return true;
		}catch(Exception e){
			err += e.Message;
			return false;
		}
	}
	
	public static async Task<bool> importFromPlaylist(string url, string[] authors, Action<string> onErr){
		int rid = new Random().Next();
		Directory.CreateDirectory(dep.path + "/import/" + rid);
		string path = dep.path + "/import/" + rid + "/%(title)s.%(ext)s";
		
		var psi = new ProcessStartInfo{
			FileName = geYtDlpPath(),
			Arguments = "-x --audio-format mp3 --audio-quality 0 -o \"" + path + "\" --no-mtime --yes-playlist -i --user-agent \"Mozilla/5.0 (Windows NT 10.0; Win64; x64)\" \"" + url + "\"",
			UseShellExecute = false,
			CreateNoWindow = true,
            RedirectStandardError = true,
			WindowStyle = ProcessWindowStyle.Hidden,
		};
		
		// Start the process
		var process = new Process { StartInfo = psi };
		
        process.ErrorDataReceived += (s, a) => {
			if(a.Data != null){
				onErr?.Invoke(a.Data);
			}
		};
		
		try{
			process.Start();
			//process.BeginErrorReadLine();
			process.WaitForExit();
			
			int e = process.ExitCode;
			if(e != 0){
				onErr?.Invoke("yt-dlp exit code: " + e);
			}
		}catch(Exception ex){
			onErr?.Invoke(ex.Message);
		}
		
		List<int> s = importAll(rid.ToString(), authors, out string err2);
		onErr?.Invoke(err2);
		if(s.Count > 0){
			return true;
		}
		
		return false;
	}
	
	public static int importPlaylistFromFolder(string path, string title, string[] authors, out string err){
		err = "";
		try{
			if(!Directory.Exists(path)){
				err += "Folder does not exist";
				return -1;
			}
			
			if(string.IsNullOrEmpty(title.Trim())){
				title = Path.GetFileName(path);
			}
			
			int[] aus = Author.getAuthors(authors);
			
			string[] files = Directory.GetFiles(path, "*");
			
			List<int> added = new();
			
			foreach(string h in files){
				int s = Song.import(h, Path.GetFileNameWithoutExtension(h), aus, out string err2);
				if(!string.IsNullOrEmpty(err2)){
					err += err2 + "\n";
				}
				if(s > -1){
					added.Add(s);
				}
			}
			
			Playlist p = Playlist.load(Playlist.create(title));
			
			foreach(int s in added){
				p?.addSong(s);
			}
			
			return p.id;
		}catch(Exception e){
			err += e.Message;
			return -1;
		}
	}
	
	public static async Task<int> importYtPlaylist(string url, string title, string[] authors, Action<string> onErr){
		int rid = new Random().Next();
		Directory.CreateDirectory(dep.path + "/import/" + rid);
		string path = dep.path + "/import/" + rid + "/%(title)s.%(ext)s";
		
		if(string.IsNullOrEmpty(title.Trim())){
			title = "Untitled playlist";
		}
		
		var psi = new ProcessStartInfo{
			FileName = geYtDlpPath(),
			Arguments = "-x --audio-format mp3 --audio-quality 0 -o \"" + path + "\" --no-mtime --yes-playlist -i --user-agent \"Mozilla/5.0 (Windows NT 10.0; Win64; x64)\" \"" + url + "\"",
			UseShellExecute = false,
			CreateNoWindow = true,
            RedirectStandardError = true,
			WindowStyle = ProcessWindowStyle.Hidden,
		};
		
		// Start the process
		var process = new Process { StartInfo = psi };
		
        process.ErrorDataReceived += (s, a) => {
			if(a.Data != null){
				onErr?.Invoke(a.Data);
			}
		};
		
		try{
			process.Start();
			process.BeginErrorReadLine();
			process.WaitForExit();
			
			int e = process.ExitCode;
			if(e != 0){
				onErr?.Invoke("yt-dlp exit code: " + e);
			}
		}catch(Exception ex){
			onErr?.Invoke(ex.Message);
		}
		
		List<int> s = importAll(rid.ToString(), authors, out string err2);
		onErr?.Invoke(err2);
		if(s.Count > 0){
			Playlist p = Playlist.load(Playlist.create(title));
			
			foreach(int s2 in s){
				p?.addSong(s2);
			}
			
			return p.id;
		}
		
		return -1;
	}
	
	static List<int> importAll(string path, string[] authors, out string err2){
		string[] files = Directory.GetFiles(dep.path + "/import" + (string.IsNullOrEmpty(path) ? "" : "/") + path, "*.mp3");
		
		int[] aus = Author.getAuthors(authors);
		
		err2 = "";
		
		List<int> s = new();
		
		foreach(string f in files){
			int s2 = Song.import(f, Path.GetFileNameWithoutExtension(f), aus, out string err);
			if(s2 > -1){
				s.Add(s2);
			}
			err2 += err + "\n";
			try{
				File.Delete(f);
			}catch(Exception e){
				err2 += e.Message + "\n";
			}
		}
		
		if(!string.IsNullOrEmpty(path)){
			Directory.Delete(dep.path + "/import/" + path);
		}
		
		return s;
	}
	
	//Save on exit the current song and time left
	static void onExit(object sender, EventArgs e){
		config.SetCamp("player.song", py.playingSong);
		config.SetCamp("player.elapsed", py.elapsed);
		
		py.Dispose();
		
		config.Save();
	}
	
	static string geYtDlpPath(){
		return Radio.config.GetCamp<string>("ytdlpPath");
	}
}