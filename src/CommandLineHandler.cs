using System.Text;

public static class CommandLineHandler{
	static bool quiet = false;
	static bool onlyCli = true;
	
	public static int Main(string[] args){
		string dir = null;
		
		int n = 0;
		
		//Version and help
		if(args.Length == 1){
			switch(args[0].ToLower()){
				case "--help":
				case "-h":
					printHelp();
					return 0;
				
				case "--version":
				case "-v":
					printVersion();
					return 0;
			}
		}
		
		bool flags = true;
		
		//Flags
		while(flags && n < args.Length){
			switch(args[n].ToLower()){
				case "--directory":
				case "-d":
					if(n + 1 >= args.Length){
						report("Expected another argument: directory");
						return 1;
					}
					if(dir != null){
						report("Directory already specified");
					}else{
						n++;
						dir = args[n];	
					}
				break;
				
				case "--quiet":
				case "-q":
					if(quiet){
						report("Quiet already specified");
					}else{
						quiet = true;
					}
				break;
				
				case "--help":
				case "-h":
				case "--version":
				case "-v":
					report("Help or version flags are expected to be the first and only ones");
					return 2;
				break;
				
				default:
					n--;
					flags = false;
				break;
			}
			
			n++;
		}
		
		AppDomain.CurrentDomain.ProcessExit += onExit;
		
		//Commands
		if(n < args.Length){
			return getRoot().handle(args.Skip(n).ToArray(), report,
			() => {
				Console.OutputEncoding = Encoding.UTF8; //Russian chars didnt work
				try{
					Radio.initCore(dir);
				}catch(Exception e){
					report("An error occured while initializing! Details saved to: " + Radio.errorFilePath);
					report(e.ToString());
					Radio.reportError(e.ToString());
					Environment.Exit(4);
				}
			});
		}else{
			onlyCli = false;
			return doInteractive(dir);
		}
	}
	
	static int doInteractive(string dir){
		//Exit if not interactive
		if(!(Environment.UserInteractive && !Console.IsInputRedirected && !Console.IsOutputRedirected)){
			report("This application needs an interactive console to be run.");
			return 3;
		}
		
		try{
			Radio.initCore(dir);
			Radio.initScreens();
		}catch(Exception e){
			Screens.exitAltBuffer();
			
			report("An error occured while initializing! Details saved to: " + Radio.errorFilePath);
			report(e.ToString());
			Radio.reportError(e.ToString());
			return 4;
		}
		
		try{
			Radio.sc.play();
			return 0;
		}catch(Exception e){
			Screens.exitAltBuffer();
			
			report("An error occured! Details saved to: " + Radio.errorFilePath);
			report(e.ToString());
			Radio.reportError(e.ToString());
			return 5;
		}
	}
	
	static void printVersion(){
		Console.WriteLine("AshRadio v" + Radio.version);
		Console.WriteLine(Radio.versionDate);
		Console.WriteLine("Made by siljam");
		Console.WriteLine("Go to https://github.com/siljamdev/AshRadio for more ");
	}
	
	static void printHelp(){
		Console.WriteLine("AshRadio CLI help");
		Console.WriteLine();
		Console.WriteLine("Usage: ashradio [flags] <command>");
		Console.WriteLine();
		Console.WriteLine("Flags:");
		Console.WriteLine("  -q");
		Console.WriteLine("  --quiet       Show no error messages");
		Console.WriteLine("  -d");
		Console.WriteLine("  --directory   Specify data directory");
		Console.WriteLine("  -v");
		Console.WriteLine("  --version     Show current version");
		Console.WriteLine("  -h");
		Console.WriteLine("  --help        Show help");
		Console.WriteLine();
		Console.WriteLine("Commands:");
		Console.WriteLine(getRoot().help());
	}
	
	static void report(string e){
		if(!quiet){
			Console.Error.WriteLine(e);
		}
	}
	
	static CLINode getRoot(){
		CLINode root = new CLINode("ashradio");
		
		root.chain("library").chain("list").setAction(args => {
			foreach(Song s in Song.getLibrary()){
				Console.WriteLine(s.id + ". " + s.title);
			}
			return 0;
		}).setDescription("List all songs in library and their ids");
		
		root.chain("library").chain("count").setAction(args => {
			Console.WriteLine(Song.getLibrary().Count);
			return 0;
		}).setDescription("Count number of songs in library");
		
		root.chain("library").chain("search", 1).setArgNames("query").setAction(args => {
			List<Song> lib = Song.getLibrary().Where(n => n.title.Contains(args[0], StringComparison.OrdinalIgnoreCase)).ToList();
			
			foreach(Song s in lib){
				Console.WriteLine(s.id + ". " + s.title);
			}
			return 0;
		}).setDescription("Search for songs in library");
		
		root.chain("library").chain("export", 1).setArgNames("directory").setAction(args => {
			List<Song> lib = Song.getLibrary();
			bool anyBad = false;
			
			foreach(Song s in lib){
				bool succ = Song.export(s.id, args[0], out string err);
				if(!succ){
					report(err);
					anyBad = true;
				}
			}
			return anyBad ? 10 : 0;
		}).setDescription("Export whole library to directory");
		
		root.chain("song").chain("get", 1).setArgNames("song id").setAction(args => {
			if(!int.TryParse(args[0], out int id)){
				report("A number was expected, instead was found: " + args[0]);
				return 2;
			}
			
			Song s = Song.get(id);
			if(s != null){
				Console.WriteLine("Title: " + s.title);
				Console.WriteLine("Authors: " + (s.authors.Length == 0 ? Author.nullName : string.Join(", ", s.authors.Select(n => (Author.get(n)?.name ?? Author.nullName)))));
				Console.WriteLine("Duration: " + s.duration);
				
				return 0;
			}else{
				report("Song not found");
				return 11;
			}
		}).setDescription("Get detailed info on a song");
		
		root.chain("song").chain("play", 1).setArgNames("song id").setAction(args => {
			if(!int.TryParse(args[0], out int id)){
				report("A number was expected, instead was found: " + args[0]);
				return 2;
			}
			
			if(Song.exists(id)){
				bool canExit = false;
				Radio.py.onSongFinish += (s, a) => {
					canExit = true;
				};
				
				Radio.py.play(id);
				while(!canExit){
					Thread.Sleep(1000);
				}
				return 0;
			}else{
				report("Song not found");
				return 11;
			}
		}).setDescription("Play a song");
		
		root.chain("song").chain("import", 3).setArgNames("path", "title", "authors").setAction(args => {
			int id = Radio.importSingleFile(args[0], args[1], args[2].Split(','), out string err);
			if(id == -1){
				report(err);
				
				return 10;
			}else{
				Console.WriteLine(id);
				return 0;
			}
		}).setDescription("Import song from file. Authors separated by commas");
		
		root.chain("song").chain("export", 2).setArgNames("song id", "directory").setAction(args => {
			if(!int.TryParse(args[0], out int id)){
				report("A number was expected, instead was found: " + args[0]);
				return 2;
			}
			
			if(!Song.export(id, args[1], out string err)){
				report(err);
				
				return 10;
			}else{
				return 0;
			}
		}).setDescription("Export song to directory");
		
		root.chain("author").chain("list").setAction(args => {
			foreach(Author a in Author.getAllAuthors()){
				Console.WriteLine(a.id + ". " + a.name);
			}
			return 0;
		}).setDescription("List all authors and their ids");
		
		root.chain("author").chain("count").setAction(args => {
			Console.WriteLine(Author.getAllAuthors().Count);
			return 0;
		}).setDescription("Count number of authors");
		
		root.chain("author").chain("search", 1).setArgNames("query").setAction(args => {
			List<Author> lib = Author.getAllAuthors().Where(n => n.name.Contains(args[0], StringComparison.OrdinalIgnoreCase)).ToList();
			
			foreach(Author a in lib){
				Console.WriteLine(a.id + ". " + a.name);
			}
			return 0;
		}).setDescription("Search for authors");
		
		root.chain("author").chain("get", 1).setArgNames("author id").setAction(args => {
			if(!int.TryParse(args[0], out int id)){
				report("A number was expected, instead was found: " + args[0]);
				return 2;
			}
			
			Author a = Author.get(id);
			if(a != null){
				Console.WriteLine("Name: " + a.name);
				Console.WriteLine("Songs:");
				foreach(Song s in a.getSongs()){
					Console.WriteLine(s.id + ". " + s.title);
				}
				return 0;
			}else{
				report("Author not found");
				return 11;
			}
		}).setDescription("Get detailed info on an author");
		
		root.chain("author").chain("export", 2).setArgNames("author id", "directory").setAction(args => {
			if(!int.TryParse(args[0], out int id)){
				report("A number was expected, instead was found: " + args[0]);
				return 2;
			}
			
			Author p = Author.get(id);
			if(p != null){
				List<Song> lib = p.getSongs();
				bool anyBad = false;
				
				foreach(Song s in lib){
					bool succ = Song.export(s.id, args[1], out string err);
					if(!succ){
						report(err);
						anyBad = true;
					}
				}
				return anyBad ? 10 : 0;
			}else{
				report("Author not found");
				return 11;
			}
		}).setDescription("Export all songs by an author to directory");
		
		root.chain("playlist").chain("list").setAction(args => {
			foreach(Playlist p in Playlist.getAllPlaylists()){
				Console.WriteLine(p.id + ". " + p.title);
			}
			return 0;
		}).setDescription("List all playlists and their ids");
		
		root.chain("playlist").chain("count").setAction(args => {
			Console.WriteLine(Playlist.getAllPlaylists().Count);
			return 0;
		}).setDescription("Count number of playlists");
		
		root.chain("playlist").chain("search", 1).setArgNames("query").setAction(args => {
			List<Playlist> lib = Playlist.getAllPlaylists().Where(n => n.title.Contains(args[0], StringComparison.OrdinalIgnoreCase)).ToList();
			
			foreach(Playlist p in Playlist.getAllPlaylists()){
				Console.WriteLine(p.id + ". " + p.title);
			}
			return 0;
		}).setDescription("Search for playlists");
		
		root.chain("playlist").chain("get", 1).setArgNames("playlist id").setAction(args => {
			if(!int.TryParse(args[0], out int id)){
				report("A number was expected, instead was found: " + args[0]);
				return 2;
			}
			
			Playlist p = Playlist.get(id);
			if(p != null){
				Console.WriteLine("Title: " + p.title);
				Console.WriteLine("Songs:");
				foreach(Song s in p.getSongs()){
					Console.WriteLine(s.id + ". " + s.title);
				}
				return 0;
			}else{
				report("Playlist not found");
				return 11;
			}
		}).setDescription("Get detailed info on a playlist");
		
		root.chain("playlist").chain("add", 2).setArgNames("playlist id", "song id").setAction(args => {			
			if(!int.TryParse(args[0], out int id)){
				report("A number was expected, instead was found: " + args[0]);
				return 2;
			}
			
			if(!int.TryParse(args[1], out int sid)){
				report("A number was expected, instead was found: " + args[1]);
				return 2;
			}
			
			Playlist p = Playlist.get(id);
			if(p != null){
				p.addSong(sid);
				return 0;
			}else{
				report("Playlist not found");
				return 11;
			}
		}).setDescription("Add a song to a playlist");
		
		root.chain("playlist").chain("create", 1).setArgNames("title").setAction(args => {			
			int id = Playlist.create(args[0]);
			Console.WriteLine(id);
			return 0;
		}).setDescription("Create a new playlist");
		
		root.chain("playlist").chain("import", 2).setArgNames("directory", "title").setAction(args => {			
			int id = Radio.importPlaylistFromFolder(args[0], args[1], Array.Empty<string>(), out string err);
			if(id == -1){
				report(err);
				
				return 10;
			}else{
				Console.WriteLine(id);
				return 0;
			}
		}).setDescription("Import a playlist from directory");
		
		root.chain("playlist").chain("export", 2).setArgNames("playlist id", "directory").setAction(args => {
			if(!int.TryParse(args[0], out int id)){
				report("A number was expected, instead was found: " + args[0]);
				return 2;
			}
			
			Playlist p = Playlist.get(id);
			if(p != null){
				List<Song> lib = p.getSongs();
				bool anyBad = false;
				
				foreach(Song s in lib){
					bool succ = Song.export(s.id, args[1], out string err);
					if(!succ){
						report(err);
						anyBad = true;
					}
				}
				return anyBad ? 10 : 0;
			}else{
				report("Playlist not found");
				return 11;
			}
		}).setDescription("Export a playlist to directory");
		
		root.chain("config").chain("reset").setAction(args => {
			Radio.resetConfig();
			return 0;
		}).setDescription("Reset the config");
		
		return root;
	}
	
	static void onExit(object sender, EventArgs e){
		if(!onlyCli){
			Screens.exitAltBuffer();
			Screens.showCursor();
		}
	}
}

//Lil helper class
class CLINode{
	public string command {get;}
	public int extraArgs {get;}
	public string[] extraArgsNames;
	public string description {get; private set;} 
	
	public List<CLINode> children {get;} = new();
	public Func<string[], int>? action;
	
	public CLINode(string n, int x = 0){
		command = n;
		extraArgs = x;
		extraArgsNames = Enumerable.Repeat("arg", x).ToArray();
	}
	
	public CLINode setArgNames(params string[] n){
		if(n.Length == extraArgs){
			extraArgsNames = n;
		}
		
		return this;
	}
	
	public CLINode chain(string n, int x = 0){
		if(children.Any(h => h.command == n)){
			return children.First(h => h.command == n);
		}
		
		CLINode c = new CLINode(n, x);
		children.Add(c);
		return c;
	}
	
	public CLINode setAction(Func<string[], int>? f){
		action = f;
		
		return this;
	}
	
	public CLINode setDescription(string d){
		description = d;
		
		return this;
	}
	
	public string help(int indent = 0, int len = 0){
		string tab = new string(' ', indent);
		StringBuilder sb = new();
		sb.Append(tab);
		sb.Append(command);
		if(action != null){
			foreach(string n in extraArgsNames){
				sb.Append(" <" + n + ">");
			}
		}
		
		if(description != null){
			sb.Append(new string(' ', Math.Max(0, len - helpLen())) + "    " + description);
		}
		
		sb.Append(Environment.NewLine);
		
		int l = 0;
		foreach(CLINode c in children){
			l = Math.Max(l, c.helpLen());
		}
		
		foreach(CLINode c in children){
			sb.Append(c.help(indent + 3, l));
		}
		
		return sb.ToString();
	}
	
	int helpLen(){
		return command.Length + (action == null ? 0 : extraArgsNames.Select(h => h.Length + 3).Sum());
	}
	
	public int handle(string[] args, Action<string> report, Action onMatch){
		if(args.Length == 0){
			if(action != null){
				if(extraArgs == 0){
					onMatch?.Invoke();
					return action?.Invoke(new string[0]) ?? 0;
				}else{
					report(extraArgs + " extra arguments(" + string.Join(", ", extraArgsNames.Select(h => "'" + h + "'")) + ") were expected after '" + command + "'");
					return 1;
				}
			}else{
				report("Expected another command after '" + command + "'");
				return 1;
			}
		}
		
		CLINode c = children.FirstOrDefault(h => h.command == args[0]);
		if(c != null){
			return c.handle(args.Skip(1).ToArray(), report, onMatch);
		}else if(action != null){
			if(args.Length == extraArgs){
				onMatch?.Invoke();
				return action?.Invoke(args) ?? 0;
			}else if(args.Length < extraArgs){
				report("Too few arguments. " + (extraArgs - args.Length) + " extra (" + string.Join(", ", extraArgsNames.Skip(args.Length).Select(h => "'" + h + "'")) + ") were expected after '" + command + "'");
				return 1;
			}else{
				if(extraArgs == 0){
					report("Too many arguments. No more were expected after '" + command + "'");
				}else{
					report("Too many arguments. Only " + extraArgs + " were expected after '" + command + "'");
				}
				return 1;
			}
		}else{
			report("Unknown command: '" + args[0] + "'");
			return 2;
		}
	}
}