global using System;
global using AshLib;
global using AshLib.AshFiles;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.IO.Compression;
using AshLib.Folders;

public static class Radio{
	public static string errorFilePath = null;
	public static string appDataPath = null;
	
	public static Dependencies dep = null!;
	
	public static AshFile config = null!; //In appdata
	public static AshFile data = null!; //Internal data (ids...) In path
	public static AshFile session = null!; //Internal data (session, playing, init...) In appdata
	
	public static Player py;
	public static Screens sc;
	
	public static DiscordPresence dcrpc;
	
	public static AshFileModel configModel;
	
	public static bool tryInitScreens;
	
	static OSMedia osmedia;
	
	//Complete init logic
	public static void initCore(string directory = null){
		appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "/ashproject/ashradio";
		
		errorFilePath = appDataPath + "/error_" + DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss") + ".log";
		
		//Setup dep
		if(directory == null){
			dep = new Dependencies(appDataPath, true, new string[]{"songs", "songs/files", "songs/data", "import", "stats"}, null);
		}else{
			dep = new Dependencies(directory, false, new string[]{"songs", "songs/files", "songs/data", "import", "stats"}, null);
		}
		
		//Setup session
		initSession();
		
		//Setup data
		initData();
		
		//Setup config
		initConfig();
		
		//Try auto download
		if(!config.TryGetValue("internal.init", out bool b) || !b){
			downloadYtdlp(null);
			downloadFfmpeg(null);
			config.Set("internal.init", true);
			config.Save();
		}
		
		//Try clear old errors
		if(config.TryGetValue("capErrorLogs", out bool b2) && b2){
			clearOldLogs(5);
		}
		
		Song.init();
		Author.init();
		Playlist.init();
		
		py = new Player();
		Session.init();
		Stats.init();
		py.initSong();
		
		Song.subEvents();
		
		AppDomain.CurrentDomain.ProcessExit += onExit;
		
		//Try importing remaining songs
		string[] dirs = Directory.GetDirectories(dep.path + "/import").Select(dir => Path.GetFileName(dir)).ToArray();
		foreach(string d in dirs){
			importAll(d, new string[0], out string a2); //If app closed while songs where importing, import them
		}
		importAll("", new string[0], out string a);
	}
	
	static void initData(){
		//Try pass from config to data safely
		if(!File.Exists(dep.path + "/data.ash") && File.Exists(appDataPath + "/config.ash")){
			data = new AshFile(dep.path + "/data.ash");
			
			AshFile conf = new AshFile(appDataPath + "/config.ash");
			
			if(conf.TryGetValue("songs.latestId", out int i)){
				data.Set("songs.latestId", i);
			}
			if(conf.TryGetValue("authors.latestId", out i)){
				data.Set("authors.latestId", i);
			}
			if(conf.TryGetValue("playlists.latestId", out i)){
				data.Set("playlists.latestId", i);
			}
		}else{
			data = new AshFile(dep.path + "/data.ash");
		}
		
		AshFileModel m = new AshFileModel(
			new ModelInstance(ModelInstanceOperation.Type, "songs.latestId", -1),
			new ModelInstance(ModelInstanceOperation.Type, "authors.latestId", -1),
			new ModelInstance(ModelInstanceOperation.Type, "playlists.latestId", -1)
		);
		
		m.deleteNotMentioned = true;
		
		data *= m;
		
		data.Save();
	}
	
	static void initSession(){
		//Try pass from config to session safely
		if(!File.Exists(appDataPath + "/session.ash") && File.Exists(appDataPath + "/config.ash")){
			session = new AshFile(appDataPath + "/session.ash");
			
			AshFile conf = new AshFile(appDataPath + "/config.ash");
			
			if(conf.TryGetValue("player.song", out int i)){
				session.Set("player.song", i);
			}
			if(conf.TryGetValue("player.elapsed", out float f)){
				session.Set("player.elapsed", f);
			}
			if(conf.TryGetValue("session.mode", out i)){
				session.Set("session.mode", i);
			}
			if(conf.TryGetValue("session.sourceType", out i)){
				session.Set("session.sourceType", i);
			}
			if(conf.TryGetValue("session.sourceIdentifier", out i)){
				session.Set("session.sourceIdentifier", i);
			}
			if(conf.TryGetValue("session.sourceSeen", out int[] ia)){
				session.Set("session.sourceSeen", ia);
			}
		}else{
			session = new AshFile(appDataPath + "/session.ash");
		}
		
		AshFileModel m = new AshFileModel(
			new ModelInstance(ModelInstanceOperation.Type, "player.song", -1),
			new ModelInstance(ModelInstanceOperation.Type, "player.elapsed", 0f),
			
			new ModelInstance(ModelInstanceOperation.Type, "session.mode", 0),
			new ModelInstance(ModelInstanceOperation.Type, "session.sourceType", 0),
			new ModelInstance(ModelInstanceOperation.Type, "session.sourceIdentifier", -1),
			new ModelInstance(ModelInstanceOperation.Type, "session.sourceSeen", Array.Empty<int>())
		);
		
		m.deleteNotMentioned = true;
		
		session *= m;
		
		session.Save();
	}
	
	static void initConfig(){
		config = new AshFile(appDataPath + "/config.ash");
		
		if(config.TryGetValue("init", out bool b)){
			config.Set("internal.init", b);
		}
		
		if(config.TryGetValue("ui.palette.background", out Color3 c)){
			config.Set("ui.palette.selectedDefault", new Color3[]{c});
		}
		
		if(config.TryGetValue("player.volume", out int v)){
			config.Set("player.volume", v / 100f);
		}
		
		AshFileModel m = new AshFileModel(
			new ModelInstance(ModelInstanceOperation.Type, "player.volume", 1f),
			new ModelInstance(ModelInstanceOperation.Type, "player.volumeExponent", 2f),
			new ModelInstance(ModelInstanceOperation.Type, "player.advanceTime", 10f),
			
			new ModelInstance(ModelInstanceOperation.Type, "ffmpegPath", "ffmpeg"),
			new ModelInstance(ModelInstanceOperation.Type, "ffprobePath", "ffprobe"),
			new ModelInstance(ModelInstanceOperation.Type, "ytdlpPath", "yt-dlp"),
			
			new ModelInstance(ModelInstanceOperation.Type, "dcrp", true),
			new ModelInstance(ModelInstanceOperation.Type, "osmediaintegration", true),
			#if LINUX
				new ModelInstance(ModelInstanceOperation.Type, "osmediaintegration.linuxdesktop", "ashradio"),
			#endif
			
			new ModelInstance(ModelInstanceOperation.Type, "capErrorLogs", true),
			
			new ModelInstance(ModelInstanceOperation.Type, "ui.useColors", true),
			new ModelInstance(ModelInstanceOperation.Type, "ui.cursorBlinks", true),
			new ModelInstance(ModelInstanceOperation.Type, "ui.cursor", "_"),
			new ModelInstance(ModelInstanceOperation.Type, "ui.selectors", "><"),
			new ModelInstance(ModelInstanceOperation.Type, "ui.updateFrequency", 24f),
			new ModelInstance(ModelInstanceOperation.Type, "ui.cursorBlinkPeriod", 0.7f),
			new ModelInstance(ModelInstanceOperation.Type, "ui.playingChars", "►‖"),
			
			new ModelInstance(ModelInstanceOperation.Type, "internal.init", false)
		);
		
		m.Merge(Palette.getPaletteModel());
		m.Merge(Keybinds.getKeybindsModel());
		
		m.deleteNotMentioned = true;
		
		configModel = m;
		
		config *= m;
		
		//Set current version and path. Might be needed by someone (maybe)
		config.Set("version", BuildInfo.Version);
		try{ //Might not work on linux
			config.Set("path", System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);
		}catch{}
		
		config.Save();
	}
	
	public static void initScreens(){		
		Palette.init();
		Keybinds.init();
		sc = new Screens();
		
		if(config.TryGetValue("dcrp", out bool b) && b){
			dcrpc = new DiscordPresence();
		}
		
		if(config.TryGetValue("osmediaintegration", out bool b2) && b2){
			#if WINDOWS
				if(OperatingSystem.IsWindowsVersionAtLeast(10, 0, 19041)){
					osmedia = new WindowsMedia();
				}
			#elif LINUX
				osmedia = new LinuxMedia();
			#elif MACOS
				//cannot get it to fucking work, sorry
			#endif
		}
		
		#if WINDOWS
			Application.SetHighDpiMode(HighDpiMode.PerMonitorV2); //WinForms will have better visual quality
		#endif
	}
	
	public static void reinitScreens(){
		Palette.init();
		Keybinds.init();
		sc = new Screens(true);
	}
	
	public static void resetConfig(){
		string[] keysToReset = new string[]{
			"player.volume", "player.volumeExponent", "player.advanceTime",
			"ffmpegPath", "ffmpegPath", "ytdlpPath",
			"dcrp", "osmediaintegration", "osmediaintegration.linuxdesktop", "capErrorLogs",
			"ui.useColors", "ui.cursorBlinks", "ui.cursor", "ui.selectors", "ui.updateFrequency", "ui.cursorBlinkPeriod", "ui.playingChars"
		};
		
		AshFileModel m = new AshFileModel(configModel.instances.Where(h => keysToReset.Contains(h.name)).Select(h => new ModelInstance(ModelInstanceOperation.Value, h.name, h.value)).ToArray());
		
		config *= m;
		
		py.init();
		
		Palette.reset();
		Keybinds.reset();
		
		config.Save();
	}
	
	//Auto-downloading
	
	static bool downloadingYtdlp = false;
	
	//Returns if it is being downloaded
	public static bool downloadYtdlp(Action onComplete = null){
		if(downloadingYtdlp){
			return false;
		}
		
		try{
			if(OperatingSystem.IsWindows()){
				downloadingYtdlp = true;
				
				downloadFile("https://github.com/yt-dlp/yt-dlp/releases/latest/download/yt-dlp.exe",
				appDataPath + "/yt-dlp.exe", async () => {
					config.Set("ytdlpPath", appDataPath + "/yt-dlp.exe");
					onComplete?.Invoke();
					config.Save();
					
					downloadingYtdlp = false;
				});
				
				return true;
			}else if(OperatingSystem.IsLinux()){
				string arch;
				bool downloadZip = false;
				bool isMusl = File.Exists("/lib/ld-musl-x86_64.so.1") || File.Exists("/lib/ld-musl-aarch64.so.1");
				
				switch(RuntimeInformation.OSArchitecture){
					case Architecture.X64:
						arch = isMusl ? "yt-dlp_musllinux" : "yt-dlp_linux";
						break;
					
					case Architecture.Arm64:
						arch = isMusl ? "yt-dlp_musllinux_aarch64" : "yt-dlp_linux_aarch64";
						break;
					
					case Architecture.Arm:
						// Only unpackaged armv7l exists
						arch = "yt-dlp_linux_armv7l.zip";
						downloadZip = true;
						break;
					
					default:
						return false;
				}
				
				downloadingYtdlp = true;
				
				if(downloadZip){
					downloadFile("https://github.com/yt-dlp/yt-dlp/releases/latest/download/" + arch,
					appDataPath + "/ytdlptemp.zip", async () => {
						try{
							ZipFile.ExtractToDirectory(appDataPath + "/ytdlptemp.zip", appDataPath + "/ytdlptemp", true);
							
							string p = Directory.GetFiles(appDataPath + "/ytdlptemp", "yt-dlp_linux_armv7l", SearchOption.AllDirectories).FirstOrDefault();
							File.Copy(p, appDataPath + "/yt-dlp", true);
							
							Directory.Delete(appDataPath + "/ytdlptemp", true);
							File.Delete(appDataPath + "/ytdlptemp.zip");
						}catch(Exception e){
							reportError(e.ToString());
						}
						
						try{
							Process.Start("chmod", "+x \"" + appDataPath + "/yt-dlp\"");
						}catch(Exception e){
							reportError(e.ToString());
						}
						
						config.Set("ytdlpPath", appDataPath + "/yt-dlp");
						onComplete?.Invoke();
						config.Save();
						
						downloadingYtdlp = false;
					});
				}else{
					downloadFile("https://github.com/yt-dlp/yt-dlp/releases/latest/download/" + arch,
					appDataPath + "/yt-dlp", async () => {
						try{
							Process.Start("chmod", "+x \"" + appDataPath + "/yt-dlp\"");
						}catch(Exception e){
							reportError(e.ToString());
						}
						
						config.Set("ytdlpPath", appDataPath + "/yt-dlp");
						onComplete?.Invoke();
						config.Save();
						
						downloadingYtdlp = false;
					});
				}
				
				return true;
			}else if(OperatingSystem.IsMacOS()){
				downloadingYtdlp = false;
				
				downloadFile("https://github.com/yt-dlp/yt-dlp/releases/latest/download/yt-dlp_macos",
				appDataPath + "/yt-dlp", async () => {
					try{
						Process.Start("chmod", "+x \"" + appDataPath + "/yt-dlp\"");
					}catch(Exception e){
						reportError(e.ToString());
					}
					
					config.Set("ytdlpPath", appDataPath + "/yt-dlp");
					onComplete?.Invoke();
					config.Save();
					
					downloadingYtdlp = false;
				});
				
				return true; 
			}else{
				return false;
			}
		}catch(Exception e){
			reportError(e.ToString());
			
			return false;
		}
	}
	
	static bool downloadingFfmpeg = false;
	
	//Returns if it is being downloaded
	public static bool downloadFfmpeg(Action onComplete = null){
		if(downloadingFfmpeg){
			return false;
		}
		
		try{
			if(OperatingSystem.IsWindows()){
				string dlurl = "https://github.com/yt-dlp/FFmpeg-Builds/releases/latest/download/";
				switch(RuntimeInformation.OSArchitecture){
					case Architecture.X64:
						dlurl += "ffmpeg-master-latest-win64-gpl.zip";
						break;
					
					case Architecture.X86:
						dlurl += "ffmpeg-master-latest-win32-gpl.zip";
						break;
					
					case Architecture.Arm64:
						dlurl += "ffmpeg-master-latest-winarm64-gpl.zip";
						break;
					
					default:
						return false;
				};
				
				downloadingFfmpeg = true;
				
				downloadFile(dlurl,
				appDataPath + "/ffmpegtemp.zip", async () => {
					try{
						ZipFile.ExtractToDirectory(appDataPath + "/ffmpegtemp.zip", appDataPath + "/ffmpegtemp", true);
						
						string p = Directory.GetFiles(appDataPath + "/ffmpegtemp", "ffmpeg.exe", SearchOption.AllDirectories).FirstOrDefault();
						File.Copy(p, appDataPath + "/ffmpeg.exe", true);
						
						p = Directory.GetFiles(appDataPath + "/ffmpegtemp", "ffprobe.exe", SearchOption.AllDirectories).FirstOrDefault();
						File.Copy(p, appDataPath + "/ffprobe.exe", true);
						
						Directory.Delete(appDataPath + "/ffmpegtemp", true);
						File.Delete(appDataPath + "/ffmpegtemp.zip");
					}catch(Exception e){
						reportError(e.ToString());
					}
					config.Set("ffmpegPath", appDataPath + "/ffmpeg.exe");
					config.Set("ffprobePath", appDataPath + "/ffprobe.exe");
					onComplete?.Invoke();
					config.Save();
					
					downloadingFfmpeg = false;
				});
				
				return true;
			}else if(OperatingSystem.IsLinux()){
				string dlurl = "https://github.com/yt-dlp/FFmpeg-Builds/releases/latest/download/";
				switch(RuntimeInformation.OSArchitecture){
					case Architecture.X64:
						dlurl += "ffmpeg-master-latest-linux64-gpl.tar.xz";
						break;
					
					case Architecture.Arm64:
						dlurl += "ffmpeg-master-latest-linuxarm64-gpl.tar.xz";
						break;
					
					default:
						return false;
				};
				
				downloadingFfmpeg = true;
				
				downloadFile(dlurl,
				appDataPath + "/ffmpegtemp.tar.xz", async () => {
					try{
						Directory.CreateDirectory(appDataPath + "/ffmpegtemp");
						
						ProcessStartInfo psi = new ProcessStartInfo{
							FileName = "tar",
							Arguments = "-xf \"" + appDataPath + "/ffmpegtemp.tar.xz\" -C \"" + appDataPath + "/ffmpegtemp\"",
							UseShellExecute = false,
							CreateNoWindow = true,
							RedirectStandardOutput = true,
							RedirectStandardError = true,
							WindowStyle = ProcessWindowStyle.Hidden,
						};
						
						using Process proc = Process.Start(psi);
						proc.WaitForExit();
						
						if(proc.ExitCode != 0){
							string err = proc.StandardError.ReadToEnd();
							throw new Exception("tar failed: " + err);
						}
						
						string p = Directory.GetFiles(appDataPath + "/ffmpegtemp", "ffmpeg", SearchOption.AllDirectories).FirstOrDefault();
						File.Copy(p, appDataPath + "/ffmpeg", true);
						
						p = Directory.GetFiles(appDataPath + "/ffmpegtemp", "ffprobe", SearchOption.AllDirectories).FirstOrDefault();
						File.Copy(p, appDataPath + "/ffprobe", true);
						
						Directory.Delete(appDataPath + "/ffmpegtemp", true);
						File.Delete(appDataPath + "/ffmpegtemp.tar.xz");
					}catch(Exception e){
						reportError(e.ToString());
					}
					
					try{
						Process.Start("chmod", "+x \"" + appDataPath + "/ffmpeg\"");
					}catch(Exception e){
						reportError(e.ToString());
					}
					
					try{
						Process.Start("chmod", "+x \"" + appDataPath + "/ffprobe\"");
					}catch(Exception e){
						reportError(e.ToString());
					}
					
					config.Set("ffmpegPath", appDataPath + "/ffmpeg");
					config.Set("ffprobePath", appDataPath + "/ffprobe");
					onComplete?.Invoke();
					config.Save();
					
					downloadingFfmpeg = false;
				});
				
				return true;
			}else{
				return false;
			}
		}catch(Exception e){
			reportError(e.ToString());
			
			return false;
		}
	}
	
	static bool updatingYtdlp = false;
	
	public static void updateYtdlp(){
		if(updatingYtdlp){
			return;
		}
		
		updatingYtdlp = true;
		
		var psi = new ProcessStartInfo{
			FileName = getYtdlpPath(),
			Arguments = "--update",
			UseShellExecute = false,
			CreateNoWindow = true,
			RedirectStandardOutput = true,
            RedirectStandardError = true,
			WindowStyle = ProcessWindowStyle.Hidden,
		};
		
		using Process proc = Process.Start(psi);
		
		proc.EnableRaisingEvents = true;
		
		proc.Exited += (s, e) => {
			updatingYtdlp = false;
			
			if(proc.ExitCode != 0){
				reportError(proc.StandardError.ReadToEnd());
			}
		};
	}
	
	#region Importing
	public static int importSingleFile(string path, string title, string[] authors, out string err){
		return Song.import(path, title, Author.getAuthors(authors), out err);
	}
	
	public static async Task<int> importSingleVideo(string url, string title, string[] authors, Action<string> onErr){
		if(string.IsNullOrEmpty(title.Trim())){
			title = "%(title)s";
		}
		
		string path = dep.path + "/import/" + title + ".%(ext)s";
		
		var psi = new ProcessStartInfo{
			FileName = getYtdlpPath(),
			Arguments = "-x --audio-format mp3 --audio-quality 0 -o \"" + path + "\" --no-mtime --no-playlist --user-agent \"Mozilla/5.0 (Windows NT 10.0; Win64; x64)\" \"" + url + "\"",
			UseShellExecute = false,
			CreateNoWindow = true,
			RedirectStandardOutput = true,
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
				Song.import(p, null, aus, out string err2);
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
			FileName = getYtdlpPath(),
			Arguments = "-x --audio-format mp3 --audio-quality 0 -o \"" + path + "\" --no-mtime --yes-playlist -i --user-agent \"Mozilla/5.0 (Windows NT 10.0; Win64; x64)\" \"" + url + "\"",
			UseShellExecute = false,
			CreateNoWindow = true,
			RedirectStandardOutput = true,
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
			
			if(string.IsNullOrWhiteSpace(title)){
				title = Path.GetFileName(path);
			}
			
			title = title.Trim();
			
			int[] aus = Author.getAuthors(authors);
			
			string[] files = Directory.GetFiles(path, "*");
			
			List<int> added = new();
			
			foreach(string h in files){
				int s = Song.import(h, null, aus, out string err2);
				if(!string.IsNullOrEmpty(err2)){
					err += err2 + "\n";
				}
				if(s > -1){
					added.Add(s);
				}
			}
			
			Playlist p = Playlist.get(Playlist.create(title));
			
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
			title = Playlist.nullTitle;
		}
		
		var psi = new ProcessStartInfo{
			FileName = getYtdlpPath(),
			Arguments = "-x --audio-format mp3 --audio-quality 0 -o \"" + path + "\" --no-mtime --yes-playlist -i --user-agent \"Mozilla/5.0 (Windows NT 10.0; Win64; x64)\" \"" + url + "\"",
			UseShellExecute = false,
			CreateNoWindow = true,
			RedirectStandardOutput = true,
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
			Playlist p = Playlist.get(Playlist.create(title));
			
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
			int s2 = Song.import(f, null, aus, out string err);
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
	#endregion
	
	static void clearOldLogs(int n){
		string[] filesToDelete = Directory.GetFiles(appDataPath, "error_*.log").OrderByDescending(Path.GetFileName).Skip(n).ToArray();
		
		foreach(string file in filesToDelete){
			try{
				File.Delete(file);
			}catch(Exception e){
				reportError(e.ToString());
			}
		}
	}
	
	//Save on exit the current song and time left
	static void onExit(object sender, EventArgs e){
		session.Set("player.elapsed", py.elapsed);
		
		Stats.setTime();
		
		py.Dispose();
		
		dcrpc?.Dispose();
		
		session.Save();
	}
	
	public static async Task downloadFile(string url, string outputPath, Func<Task> onComplete){
		using HttpClient client = new HttpClient();
		using HttpResponseMessage response = await client.GetAsync(url);
		response.EnsureSuccessStatusCode();
		
		Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
		
		await using(FileStream fs = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, true)){
			await response.Content.CopyToAsync(fs);
			await fs.FlushAsync();
		}
		
		if(onComplete != null){
			await onComplete(); //Lambda executed after file is saved
		}
	}
	
	public static async Task fetchUpdate(Func<string, Task> onComplete){
		using var client = new HttpClient();
		client.DefaultRequestHeaders.UserAgent.ParseAdd("AshRadioUpdateCheck");
	
		try{
			string url = $"https://api.github.com/repos/siljamdev/AshRadio/releases/latest";
			string json = await client.GetStringAsync(url);
			var doc = JsonDocument.Parse(json);
			string latestTag = doc.RootElement.GetProperty("tag_name").GetString()!;
			
			if(onComplete != null){
				await onComplete(latestTag);
			}
		}catch(Exception e){
			reportError(e.ToString());
			
			if(onComplete != null){
				await onComplete(null);
			}
		}
	}
	
	static string getYtdlpPath(){
		return Radio.config.GetValue<string>("ytdlpPath");
	}
	
	public static void reportError(string e){
		File.AppendAllText(errorFilePath, e.ToString() + "\n");
	}
}
