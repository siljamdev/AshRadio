using AshLib.AshFiles;
using AshLib.Formatting;
using AshConsoleGraphics;

public static class Palette{
	public static CharFormat? user {get; private set;}
	public static CharFormat? song {get; private set;}
	public static CharFormat? author {get; private set;}
	public static CharFormat? playlist {get; private set;}
	public static CharFormat? main {get; private set;}
	public static CharFormat? delimiter {get; private set;}
	public static CharFormat? hint {get; private set;}
	public static CharFormat? info {get; private set;}
	public static CharFormat? error {get; private set;}
	public static CharFormat? writing {get; private set;}
	
	public static CharFormat? selectedPanel {get; private set;}
	public static CharFormat? defaultPanel {get; private set;}
	
	public static AshFileModel getPaletteModel(){
		return new AshFileModel(
			new ModelInstance(ModelInstanceOperation.Exists, "ui.palette.user.fg", new Color3("FFFF00")),
			new ModelInstance(ModelInstanceOperation.Exists, "ui.palette.user.bg", false),
			
			new ModelInstance(ModelInstanceOperation.Exists, "ui.palette.song.fg", new Color3("3295FF")),
			new ModelInstance(ModelInstanceOperation.Exists, "ui.palette.song.bg", false),
			
			new ModelInstance(ModelInstanceOperation.Exists, "ui.palette.author.fg", new Color3("00FF00")),
			new ModelInstance(ModelInstanceOperation.Exists, "ui.palette.author.bg", false),
			
			new ModelInstance(ModelInstanceOperation.Exists, "ui.palette.playlist.fg", new Color3("FFA811")),
			new ModelInstance(ModelInstanceOperation.Exists, "ui.palette.playlist.bg", false),
			
			new ModelInstance(ModelInstanceOperation.Exists, "ui.palette.main.fg", new Color3("E7484B")),
			new ModelInstance(ModelInstanceOperation.Exists, "ui.palette.main.bg", false),
			
			new ModelInstance(ModelInstanceOperation.Exists, "ui.palette.delimiter.fg", new Color3("5B2D72")),
			new ModelInstance(ModelInstanceOperation.Exists, "ui.palette.delimiter.bg", false),
			
			new ModelInstance(ModelInstanceOperation.Exists, "ui.palette.hint.fg", new Color3("9F60C1")),
			new ModelInstance(ModelInstanceOperation.Exists, "ui.palette.hint.bg", false),
			
			new ModelInstance(ModelInstanceOperation.Exists, "ui.palette.info.fg", new Color3("849DD6")),
			new ModelInstance(ModelInstanceOperation.Exists, "ui.palette.info.bg", false),
			
			new ModelInstance(ModelInstanceOperation.Exists, "ui.palette.error.fg", new Color3("D83F3C")),
			new ModelInstance(ModelInstanceOperation.Exists, "ui.palette.error.bg", false),
			
			new ModelInstance(ModelInstanceOperation.Exists, "ui.palette.writing.fg", new Color3("FFFF66")),
			new ModelInstance(ModelInstanceOperation.Exists, "ui.palette.writing.bg", false),
			
			new ModelInstance(ModelInstanceOperation.Exists, "ui.palette.selectedDefault.fg", false),
			new ModelInstance(ModelInstanceOperation.Exists, "ui.palette.selectedDefault.bg", new Color3("131313")),
			
			new ModelInstance(ModelInstanceOperation.Exists, "ui.palette.default.fg", false),
			new ModelInstance(ModelInstanceOperation.Exists, "ui.palette.default.bg", false)
		);
	}
	
	public static void init(){
		if((!FormatString.usesColors) || (Radio.config.TryGetValue("ui.useColors", out bool b) && !b)){
			AshConsoleGraphics.Buffer.NoFormat = true; //Its (no longer) broken :)
		}else{
			AshConsoleGraphics.Buffer.NoFormat = false;
		}
		
		user = loadColor("user");
		song = loadColor("song");
		author = loadColor("author");
		playlist = loadColor("playlist");
		main = loadColor("main");
		delimiter = loadColor("delimiter");
		hint = loadColor("hint");
		info = loadColor("info");
		error = loadColor("error");
		writing = loadColor("writing");
		
		selectedPanel = loadColor("selectedDefault");
		defaultPanel = loadColor("default");
		
		Radio.config.Save(); //In cae any color was removed
	}
	
	//Helper method to load colors
	static CharFormat? loadColor(string colorName){
		Color3? fg = null;
		if(Radio.config.TryGetValue("ui.palette." + colorName + ".fg", out Color3 cf)){
			fg = cf;
		}else if(!Radio.config.TryGetValue("ui.palette." + colorName + ".fg", out bool _)){
			Radio.config.Remove("ui.palette." + colorName + ".fg");
		}
		
		Color3? bg = null;
		if(Radio.config.TryGetValue("ui.palette." + colorName + ".bg", out Color3 cb)){
			bg = cb;
		}else if(!Radio.config.TryGetValue("ui.palette." + colorName + ".bg", out bool _)){
			Radio.config.Remove("ui.palette." + colorName + ".bg");
		}
		
		if(fg == null && bg == null){
			return null;
		}else{
			return new CharFormat(fg, bg);
		}
	}
	
	public static void setAsh(){
		AshFileModel m = new AshFileModel(getPaletteModel().instances.Select(h => new ModelInstance(ModelInstanceOperation.Value, h.name, h.value)).ToArray());
		
		Radio.config.ApplyModel(m);
		
		init(); //save happens here
	}
	
	public static void setSubtle(){
		Radio.config.Set("ui.palette.user.fg", new Color3("FFFF00"));
		Radio.config.Set("ui.palette.user.bg", false);
		
		Radio.config.Set("ui.palette.song.fg", new Color3("BEDD58"));
		Radio.config.Set("ui.palette.song.bg", false);
		
		Radio.config.Set("ui.palette.author.fg", new Color3("8CD0D3"));
		Radio.config.Set("ui.palette.author.bg", false);
		
		Radio.config.Set("ui.palette.playlist.fg", new Color3("966DD3"));
		Radio.config.Set("ui.palette.playlist.bg", false);
		
		Radio.config.Set("ui.palette.main.fg", new Color3("E5AA62"));
		Radio.config.Set("ui.palette.main.bg", false);
		
		Radio.config.Set("ui.palette.delimiter.fg", new Color3("848260"));
		Radio.config.Set("ui.palette.delimiter.bg", false);
		
		Radio.config.Set("ui.palette.hint.fg", new Color3("918050"));
		Radio.config.Set("ui.palette.hint.bg", false);
		
		Radio.config.Set("ui.palette.info.fg", new Color3("CC7651"));
		Radio.config.Set("ui.palette.info.bg", false);
		
		Radio.config.Set("ui.palette.error.fg", new Color3("D83F3C"));
		Radio.config.Set("ui.palette.error.bg", false);
		
		Radio.config.Set("ui.palette.writing.fg", new Color3("FFFF66"));
		Radio.config.Set("ui.palette.writing.bg", false);
		
		Radio.config.Set("ui.palette.selectedDefault.fg", false);
		Radio.config.Set("ui.palette.selectedDefault.bg", new Color3("131313"));
		
		Radio.config.Set("ui.palette.default.fg", false);
		Radio.config.Set("ui.palette.default.bg", false);
		
		init(); //save happens here
	}
	
	public static void setNeon(){ //Chatgpt made this palette bc i ran out of ideas :/
		Radio.config.Set("ui.palette.user.fg", new Color3("39FF14"));
		Radio.config.Set("ui.palette.user.bg", false);
		
		Radio.config.Set("ui.palette.song.fg", new Color3("00FFFF"));
		Radio.config.Set("ui.palette.song.bg", false);
		
		Radio.config.Set("ui.palette.author.fg", new Color3("FF44CC"));
		Radio.config.Set("ui.palette.author.bg", false);
		
		Radio.config.Set("ui.palette.playlist.fg", new Color3("FF8800"));
		Radio.config.Set("ui.palette.playlist.bg", false);
		
		Radio.config.Set("ui.palette.main.fg", new Color3("AA00FF"));
		Radio.config.Set("ui.palette.main.bg", false);
		
		Radio.config.Set("ui.palette.delimiter.fg", new Color3("00FFAA"));
		Radio.config.Set("ui.palette.delimiter.bg", false);
		
		Radio.config.Set("ui.palette.hint.fg", new Color3("FF66FF"));
		Radio.config.Set("ui.palette.hint.bg", false);
		
		Radio.config.Set("ui.palette.info.fg", new Color3("33CCFF"));
		Radio.config.Set("ui.palette.info.bg", false);
		
		Radio.config.Set("ui.palette.error.fg", new Color3("FF0033"));
		Radio.config.Set("ui.palette.error.bg", false);
		
		Radio.config.Set("ui.palette.writing.fg", new Color3("96FF82"));
		Radio.config.Set("ui.palette.writing.bg", false);
		
		Radio.config.Set("ui.palette.selectedDefault.fg", false);
		Radio.config.Set("ui.palette.selectedDefault.bg", new Color3("101010"));
		
		Radio.config.Set("ui.palette.default.fg", false);
		Radio.config.Set("ui.palette.default.bg", false);
		
		init(); //save happens here
	}
	
	public static void setLight(){ //Ewwwwww
		Radio.config.Set("ui.palette.user.fg", new Color3("F17105"));
		Radio.config.Set("ui.palette.user.bg", false);
		
		Radio.config.Set("ui.palette.song.fg", new Color3("2C82DD"));
		Radio.config.Set("ui.palette.song.bg", false);
		
		Radio.config.Set("ui.palette.author.fg", new Color3("00C900"));
		Radio.config.Set("ui.palette.author.bg", false);
		
		Radio.config.Set("ui.palette.playlist.fg", new Color3("CF5C36"));
		Radio.config.Set("ui.palette.playlist.bg", false);
		
		Radio.config.Set("ui.palette.main.fg", new Color3("D8454A"));
		Radio.config.Set("ui.palette.main.bg", false);
		
		Radio.config.Set("ui.palette.delimiter.fg", new Color3("7B4E93"));
		Radio.config.Set("ui.palette.delimiter.bg", false);
		
		Radio.config.Set("ui.palette.hint.fg", new Color3("8551A3"));
		Radio.config.Set("ui.palette.hint.bg", false);
		
		Radio.config.Set("ui.palette.info.fg", new Color3("4A608C"));
		Radio.config.Set("ui.palette.info.bg", false);
		
		Radio.config.Set("ui.palette.error.fg", new Color3("D83F3C"));
		Radio.config.Set("ui.palette.error.bg", false);
		
		Radio.config.Set("ui.palette.writing.fg", new Color3("F99C4A"));
		Radio.config.Set("ui.palette.writing.bg", false);
		
		Radio.config.Set("ui.palette.selectedDefault.fg", false);
		Radio.config.Set("ui.palette.selectedDefault.bg", new Color3("D0D0D0"));
		
		Radio.config.Set("ui.palette.default.fg", new Color3("0C0C0C"));
		Radio.config.Set("ui.palette.default.bg", new Color3("E0E0E0"));
		
		init(); //save happens here
	}
}