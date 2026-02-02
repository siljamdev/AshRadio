using AshLib.AshFiles;
using AshConsoleGraphics;

public static class Keybinds{
	//Moving around
	public static Keybind up {get; private set;}
	public static Keybind down {get; private set;}
	public static Keybind left {get; private set;}
	public static Keybind right {get; private set;}
	public static Keybind scrollUp {get; private set;}
	public static Keybind scrollDown {get; private set;}
	
	//util
	public static Keybind setSource {get; private set;}
	public static Keybind addToQueue {get; private set;}
	public static Keybind play {get; private set;}
	public static Keybind search {get; private set;}
	public static Keybind export {get; private set;}
	public static Keybind addToPlaylist {get; private set;}
	
	//lists
	public static Keybind listRemove {get; private set;}
	public static Keybind listUp {get; private set;}
	public static Keybind listDown {get; private set;}
	
	//panels
	public static Keybind selectPlaying {get; private set;}
	public static Keybind selectNavigation {get; private set;}
	public static Keybind selectSession {get; private set;}
	public static Keybind selectMiddle {get; private set;}
	public static Keybind selectQueue {get; private set;}
	
	//Player
	public static Keybind pause {get; private set;}
	public static Keybind previous {get; private set;}
	public static Keybind skip {get; private set;}
	public static Keybind rewind {get; private set;}
	public static Keybind advance {get; private set;}
	public static Keybind restart {get; private set;}
	
	//Volume
	public static Keybind volumeUp {get; private set;}
	public static Keybind volumeDown {get; private set;}
	public static Keybind volumeMute {get; private set;}
	public static Keybind volumeMax {get; private set;}
	
	//Navigation
	public static Keybind help {get; private set;}
	public static Keybind config {get; private set;}
	public static Keybind library {get; private set;}
	public static Keybind authors {get; private set;}
	public static Keybind playlists {get; private set;}
	public static Keybind import {get; private set;}
	public static Keybind stats {get; private set;}
	
	//Session
	public static Keybind changeMode {get; private set;}
	public static Keybind seeSource {get; private set;}
	public static Keybind seePlaying {get; private set;}
	public static Keybind toggleQueueEmpties {get; private set;}
	public static Keybind changeDevice {get; private set;}
	
	public static Keybind exit {get; private set;}
	
	//Unchangeable
	public static readonly Keybind enter = new Keybind((ConsoleKey.Enter, ConsoleModifiers.None), null, "Enter");
	public static readonly Keybind escape = new Keybind((ConsoleKey.Escape, ConsoleModifiers.None), null, "Escape");
	
	//Numbers
	public static readonly Keybind zero = new Keybind((ConsoleKey.D0, ConsoleModifiers.None), (ConsoleKey.NumPad0, ConsoleModifiers.None), "Zero");
	public static readonly Keybind one = new Keybind((ConsoleKey.D1, ConsoleModifiers.None), (ConsoleKey.NumPad1, ConsoleModifiers.None), "One");
	public static readonly Keybind two = new Keybind((ConsoleKey.D2, ConsoleModifiers.None), (ConsoleKey.NumPad2, ConsoleModifiers.None), "Two");
	public static readonly Keybind three = new Keybind((ConsoleKey.D3, ConsoleModifiers.None), (ConsoleKey.NumPad3, ConsoleModifiers.None), "Three");
	public static readonly Keybind four = new Keybind((ConsoleKey.D4, ConsoleModifiers.None), (ConsoleKey.NumPad4, ConsoleModifiers.None), "Four");
	public static readonly Keybind five = new Keybind((ConsoleKey.D5, ConsoleModifiers.None), (ConsoleKey.NumPad5, ConsoleModifiers.None), "Five");
	public static readonly Keybind six = new Keybind((ConsoleKey.D6, ConsoleModifiers.None), (ConsoleKey.NumPad6, ConsoleModifiers.None), "Six");
	public static readonly Keybind seven = new Keybind((ConsoleKey.D7, ConsoleModifiers.None), (ConsoleKey.NumPad7, ConsoleModifiers.None), "Seven");
	public static readonly Keybind eight = new Keybind((ConsoleKey.D8, ConsoleModifiers.None), (ConsoleKey.NumPad8, ConsoleModifiers.None), "Eight");
	public static readonly Keybind nine = new Keybind((ConsoleKey.D9, ConsoleModifiers.None), (ConsoleKey.NumPad9, ConsoleModifiers.None), "Nine");
	
	public static readonly Keybind none = new Keybind(((ConsoleKey, ConsoleModifiers)?) null, null, "None");
	
	public static Keybind[] configurables => new Keybind[]{
		//movement
		up,
		down,
		left,
		right,
		scrollUp,
		scrollDown,
		
		//util
		setSource,
		addToQueue,
		play,
		search,
		export,
		addToPlaylist,
		
		//lists
		listRemove,
		listUp,
		listDown,
		
		//panels
		selectPlaying,
		selectNavigation,
		selectSession,
		selectMiddle,
		selectQueue,
		
		//player
		pause,
		previous,
		skip,
		rewind,
		advance,
		restart,
		
		//Volume
		volumeUp,
		volumeDown,
		volumeMute,
		volumeMax,
		
		//navigation
		help,
		config,
		library,
		authors,
		playlists,
		import,
		stats,
		
		//Sesssion
		changeMode,
		seeSource,
		seePlaying,
		toggleQueueEmpties,
		changeDevice,
		
		exit
	};
	
	public static AshFileModel getKeybindsModel(){
		return new AshFileModel(
			new ModelInstance(ModelInstanceOperation.Type, "keybinds.up", toArray(ConsoleKey.UpArrow)),
			new ModelInstance(ModelInstanceOperation.Type, "keybinds.down", toArray(ConsoleKey.DownArrow)),
			new ModelInstance(ModelInstanceOperation.Type, "keybinds.left", toArray(ConsoleKey.LeftArrow)),
			new ModelInstance(ModelInstanceOperation.Type, "keybinds.right", toArray(ConsoleKey.RightArrow)),
			new ModelInstance(ModelInstanceOperation.Type, "keybinds.scroll.up", toArray(ConsoleKey.PageUp)),
			new ModelInstance(ModelInstanceOperation.Type, "keybinds.scroll.down", toArray(ConsoleKey.PageDown)),
			
			new ModelInstance(ModelInstanceOperation.Type, "keybinds.setSource", toArray(ConsoleKey.S)),
			new ModelInstance(ModelInstanceOperation.Type, "keybinds.addToQueue", toArray(ConsoleKey.Q)),
			new ModelInstance(ModelInstanceOperation.Type, "keybinds.play", toArray(ConsoleKey.P)),
			new ModelInstance(ModelInstanceOperation.Type, "keybinds.search", toArray(ConsoleKey.F)),
			new ModelInstance(ModelInstanceOperation.Type, "keybinds.export", toArray()),
			new ModelInstance(ModelInstanceOperation.Type, "keybinds.addToPlaylist", toArray()),
			
			new ModelInstance(ModelInstanceOperation.Type, "keybinds.list.remove", toArray(ConsoleKey.R)),
			new ModelInstance(ModelInstanceOperation.Type, "keybinds.list.up", toArray(ConsoleKey.N)),
			new ModelInstance(ModelInstanceOperation.Type, "keybinds.list.down", toArray(ConsoleKey.M)),
			
			new ModelInstance(ModelInstanceOperation.Type, "keybinds.selectPlaying", toArray(ConsoleKey.Spacebar, ConsoleModifiers.Control)),
			new ModelInstance(ModelInstanceOperation.Type, "keybinds.selectNavigation", toArray(ConsoleKey.N, ConsoleModifiers.Control)),
			new ModelInstance(ModelInstanceOperation.Type, "keybinds.selectSession", toArray(ConsoleKey.S, ConsoleModifiers.Control)),
			new ModelInstance(ModelInstanceOperation.Type, "keybinds.selectMiddle", toArray(ConsoleKey.G, ConsoleModifiers.Control)),
			new ModelInstance(ModelInstanceOperation.Type, "keybinds.selectQueue", toArray(ConsoleKey.Q, ConsoleModifiers.Control)),
			
			new ModelInstance(ModelInstanceOperation.Type, "keybinds.player.pause", toArray(ConsoleKey.Spacebar, ConsoleKey.K)),
			new ModelInstance(ModelInstanceOperation.Type, "keybinds.player.previous", toArray(ConsoleKey.N)),
			new ModelInstance(ModelInstanceOperation.Type, "keybinds.player.skip", toArray(ConsoleKey.M)),
			new ModelInstance(ModelInstanceOperation.Type, "keybinds.player.rewind", toArray(ConsoleKey.J)),
			new ModelInstance(ModelInstanceOperation.Type, "keybinds.player.advance", toArray(ConsoleKey.L)),
			new ModelInstance(ModelInstanceOperation.Type, "keybinds.player.restart", toArray(ConsoleKey.J, ConsoleModifiers.Shift)),
			
			new ModelInstance(ModelInstanceOperation.Type, "keybinds.volume.down", toArray(ConsoleKey.Subtract, ConsoleKey.OemMinus)),
			new ModelInstance(ModelInstanceOperation.Type, "keybinds.volume.up", toArray(ConsoleKey.Add, ConsoleKey.OemPlus)),
			new ModelInstance(ModelInstanceOperation.Type, "keybinds.volume.mute", toArray(ConsoleKey.Subtract, ConsoleModifiers.Shift, ConsoleKey.OemMinus, ConsoleModifiers.Shift)),
			new ModelInstance(ModelInstanceOperation.Value, "keybinds.volume.max", toArray(ConsoleKey.Add, ConsoleModifiers.Shift, ConsoleKey.OemPlus, ConsoleModifiers.Shift)),
			
			new ModelInstance(ModelInstanceOperation.Type, "keybinds.help", toArray(ConsoleKey.F1)),
			new ModelInstance(ModelInstanceOperation.Type, "keybinds.config", toArray(ConsoleKey.OemComma, ConsoleModifiers.Control)),
			new ModelInstance(ModelInstanceOperation.Type, "keybinds.library", toArray(ConsoleKey.L, ConsoleModifiers.Control)),
			new ModelInstance(ModelInstanceOperation.Type, "keybinds.authors", toArray(ConsoleKey.U, ConsoleModifiers.Control)),
			new ModelInstance(ModelInstanceOperation.Type, "keybinds.playlists", toArray(ConsoleKey.P, ConsoleModifiers.Control)),
			new ModelInstance(ModelInstanceOperation.Type, "keybinds.import", toArray()),
			new ModelInstance(ModelInstanceOperation.Type, "keybinds.stats", toArray()),
			
			new ModelInstance(ModelInstanceOperation.Type, "keybinds.changeMode", toArray(ConsoleKey.M, ConsoleModifiers.Shift)),
			new ModelInstance(ModelInstanceOperation.Type, "keybinds.seeSource", toArray(ConsoleKey.S, ConsoleModifiers.Shift)),
			new ModelInstance(ModelInstanceOperation.Type, "keybinds.seePlaying", toArray(ConsoleKey.Spacebar, ConsoleModifiers.Shift)),
			new ModelInstance(ModelInstanceOperation.Type, "keybinds.toggleQueueEmpties", toArray(ConsoleKey.Q, ConsoleModifiers.Shift)),
			new ModelInstance(ModelInstanceOperation.Type, "keybinds.changeDevice", toArray()),
			
			new ModelInstance(ModelInstanceOperation.Type, "keybinds.exit", toArray(ConsoleKey.Escape, ConsoleModifiers.Shift))
		);
	}
	
	public static void init(){
		up = loadKeybind("up", "Move up");
		down = loadKeybind("down", "Move down");
		left = loadKeybind("left", "Move left");
		right = loadKeybind("right", "Move right");
		scrollUp = loadKeybind("scroll.up", "Scroll upwards");
		scrollDown = loadKeybind("scroll.down", "Scroll downwards");
		
		setSource = loadKeybind("setSource", "Set source");
		addToQueue = loadKeybind("addToQueue", "Add song to queue");
		play = loadKeybind("play", "Play song");
		search = loadKeybind("search", "Search");
		export = loadKeybind("export", "Export");
		addToPlaylist = loadKeybind("addToPlaylist", "Add song to playlist");
		
		listRemove = loadKeybind("list.remove", "Remove from list");
		listUp = loadKeybind("list.up", "Move up in list");
		listDown = loadKeybind("list.down", "Move down in list");
		
		selectPlaying = loadKeybind("selectPlaying", "Focus playing panel");
		selectNavigation = loadKeybind("selectNavigation", "Focus navigation panel");
		selectSession = loadKeybind("selectSession", "Focus session panel");
		selectMiddle = loadKeybind("selectMiddle", "Focus middle panel");
		selectQueue = loadKeybind("selectQueue", "Focus queue panel");
		
		pause = loadKeybind("player.pause", "Play/Pause song");
		previous = loadKeybind("player.previous", "Previous song");
		skip = loadKeybind("player.skip", "Next song");
		rewind = loadKeybind("player.rewind", "Rewind X seconds (advance time)");
		advance = loadKeybind("player.advance", "Advance X seconds (advance time)");
		restart = loadKeybind("player.restart", "Restart song");
		
		volumeDown = loadKeybind("volume.down", "Decrease volume");
		volumeUp = loadKeybind("volume.up", "Increase volume");
		volumeMute = loadKeybind("volume.mute", "Mute volume");
		volumeMax = loadKeybind("volume.max", "Maximum volume");
		
		help = loadKeybind("help", "Open help");
		config = loadKeybind("config", "Open config");
		library = loadKeybind("library", "Open library");
		authors = loadKeybind("authors", "Open authors");
		playlists = loadKeybind("playlists", "Open playlists");
		import = loadKeybind("import", "Open import menu");
		stats = loadKeybind("stats", "Open stats");
		
		changeMode = loadKeybind("changeMode", "Change mode");
		seeSource = loadKeybind("seeSource", "See source");
		seePlaying = loadKeybind("seePlaying", "See playing song");
		toggleQueueEmpties = loadKeybind("toggleQueueEmpties", "Toggle queue empties");
		changeDevice = loadKeybind("changeDevice", "Change device");
		
		exit = loadKeybind("exit", "Exit instantly");
	}
	
	public static void reset(){
		AshFileModel m = new AshFileModel(getKeybindsModel().instances.Select(h => new ModelInstance(ModelInstanceOperation.Value, h.name, h.value)).ToArray());
		
		Radio.config.ApplyModel(m);
		Radio.config.Save();
	}
	
	//Helper method to load colors
	static Keybind loadKeybind(string name, string desc){
		if(!Radio.config.TryGetValue("keybinds." + name, out byte[] a)){
			return new Keybind(((ConsoleKey, ConsoleModifiers)?) null, null, desc);
		}else{
			return new Keybind("keybinds." + name, a, desc);
		}
	}
	
	static byte[] toArray(){
		return new byte[0];
	}
	
	static byte[] toArray(ConsoleKey pk){
		return new byte[]{(byte) pk, 0};
	}
	
	static byte[] toArray(ConsoleKey pk, ConsoleModifiers pm){
		return new byte[]{(byte) pk, (byte) pm};
	}
	
	static byte[] toArray(ConsoleKey pk, ConsoleModifiers pm, ConsoleKey sk, ConsoleModifiers sm){
		return new byte[]{(byte) pk, (byte) pm, (byte) sk, (byte) sm};
	}
	
	static byte[] toArray(ConsoleKey pk, ConsoleKey sk){
		return new byte[]{(byte) pk, 0, (byte) sk, 0};
	}
	
	public static Keybind getNumber(int n){
		switch(n){
			case 0:
				return zero;
			
			case 1:
				return one;
			
			case 2:
				return two;
			
			case 3:
				return three;
			
			case 4:
				return four;
			
			case 5:
				return five;
			
			case 6:
				return six;
			
			case 7:
				return seven;
			
			case 8:
				return eight;
			
			case 9:
				return nine;
			
			default:
				return none;
		}
	}
}