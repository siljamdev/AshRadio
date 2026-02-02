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
			new ModelInstance(ModelInstanceOperation.Exists, "ui.palette.user", toArray(new Color3("FFFF00"), null)),
			new ModelInstance(ModelInstanceOperation.Exists, "ui.palette.song", toArray(new Color3("3295FF"), null)),
			new ModelInstance(ModelInstanceOperation.Exists, "ui.palette.author", toArray(new Color3("00FF00"), null)),
			new ModelInstance(ModelInstanceOperation.Exists, "ui.palette.playlist", toArray(new Color3("FFA811"), null)),
			new ModelInstance(ModelInstanceOperation.Exists, "ui.palette.main", toArray(new Color3("E7484B"), null)),
			new ModelInstance(ModelInstanceOperation.Exists, "ui.palette.delimiter", toArray(new Color3("5B2D72"), null)),
			new ModelInstance(ModelInstanceOperation.Exists, "ui.palette.hint", toArray(new Color3("9F60C1"), null)),
			new ModelInstance(ModelInstanceOperation.Exists, "ui.palette.info", toArray(new Color3("849DD6"), null)),
			new ModelInstance(ModelInstanceOperation.Exists, "ui.palette.error", toArray(new Color3("D83F3C"), null)),
			new ModelInstance(ModelInstanceOperation.Exists, "ui.palette.writing", toArray(new Color3("FFFF66"), null)),
			
			new ModelInstance(ModelInstanceOperation.Exists, "ui.palette.selectedDefault", toArray(null, new Color3("131313"))),
			new ModelInstance(ModelInstanceOperation.Exists, "ui.palette.default", toArray(null, null))
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
		
		Radio.config.Save(); //In case any color was removed
	}
	
	//Helper method to load colors
	static CharFormat? loadColor(string colorName){
		if(Radio.config.TryGetValue("ui.palette." + colorName, out Color3 c)){
			return new CharFormat(c);
		}else if(Radio.config.TryGetValue("ui.palette." + colorName, out Color3[] ca)){
			if(ca.Length == 1){
				return new CharFormat(null, ca[0]);
			}else if(ca.Length == 2){
				return new CharFormat(ca[0], ca[1]);
			}else{
				return null;
			}
		}else{
			Radio.config.Remove("ui.palette." + colorName);
			return null;
		}
	}
	
	static object toArray(Color3? fg, Color3? bg){
		if(fg == null){
			if(bg == null){
				return new Color3[0];
			}else{
				return new Color3[]{(Color3) bg};
			}
		}else{
			if(bg == null){
				return (Color3) fg;
			}else{
				return new Color3[]{(Color3) fg, (Color3) bg};
			}
		}
	}
	
	public static void reset(){
		AshFileModel m = new AshFileModel(getPaletteModel().instances.Select(h => new ModelInstance(ModelInstanceOperation.Value, h.name, h.value)).ToArray());
		
		Radio.config.ApplyModel(m);
	}
	
	public static void setSubtle(){
		Radio.config.Set("ui.palette.user", toArray(new Color3("FFFF00"), null));
		Radio.config.Set("ui.palette.song", toArray(new Color3("BEDD58"), null));
		Radio.config.Set("ui.palette.author", toArray(new Color3("8CD0D3"), null));
		Radio.config.Set("ui.palette.playlist", toArray(new Color3("966DD3"), null));
		Radio.config.Set("ui.palette.main", toArray(new Color3("E5AA62"), null));
		Radio.config.Set("ui.palette.delimiter", toArray(new Color3("848260"), null));
		Radio.config.Set("ui.palette.hint", toArray(new Color3("918050"), null));
		Radio.config.Set("ui.palette.info", toArray(new Color3("CC7651"), null));
		Radio.config.Set("ui.palette.error", toArray(new Color3("D83F3C"), null));
		Radio.config.Set("ui.palette.writing", toArray(new Color3("FFFF66"), null));
		Radio.config.Set("ui.palette.selectedDefault", toArray(null, new Color3("131313")));
		Radio.config.Set("ui.palette.default", toArray(null, null));
		
		Radio.config.Save();
	}
	
	public static void setNeon(){ //ChatGPT made this palette bc you ran out of ideas :/
		Radio.config.Set("ui.palette.user", toArray(new Color3("39FF14"), null));
		Radio.config.Set("ui.palette.song", toArray(new Color3("00FFFF"), null));
		Radio.config.Set("ui.palette.author", toArray(new Color3("FF44CC"), null));
		Radio.config.Set("ui.palette.playlist", toArray(new Color3("FF8800"), null));
		Radio.config.Set("ui.palette.main", toArray(new Color3("AA00FF"), null));
		Radio.config.Set("ui.palette.delimiter", toArray(new Color3("00FFAA"), null));
		Radio.config.Set("ui.palette.hint", toArray(new Color3("FF66FF"), null));
		Radio.config.Set("ui.palette.info", toArray(new Color3("33CCFF"), null));
		Radio.config.Set("ui.palette.error", toArray(new Color3("FF0033"), null));
		Radio.config.Set("ui.palette.writing", toArray(new Color3("96FF82"), null));
		
		Radio.config.Set("ui.palette.selectedDefault", toArray(null, new Color3("101010")));
		Radio.config.Set("ui.palette.default", toArray(null, null));
		
		Radio.config.Save();
	}
	
	public static void setLight() { //Ewwwwww
		Radio.config.Set("ui.palette.user", toArray(new Color3("F17105"), null));
		Radio.config.Set("ui.palette.song", toArray(new Color3("2C82DD"), null));
		Radio.config.Set("ui.palette.author", toArray(new Color3("00C900"), null));
		Radio.config.Set("ui.palette.playlist", toArray(new Color3("CF5C36"), null));
		Radio.config.Set("ui.palette.main", toArray(new Color3("D8454A"), null));
		Radio.config.Set("ui.palette.delimiter", toArray(new Color3("7B4E93"), null));
		Radio.config.Set("ui.palette.hint", toArray(new Color3("8551A3"), null));
		Radio.config.Set("ui.palette.info", toArray(new Color3("4A608C"), null));
		Radio.config.Set("ui.palette.error", toArray(new Color3("D83F3C"), null));
		Radio.config.Set("ui.palette.writing", toArray(new Color3("F99C4A"), null));
		
		Radio.config.Set("ui.palette.selectedDefault", toArray(null, new Color3("D0D0D0")));
		Radio.config.Set("ui.palette.default", toArray(new Color3("0C0C0C"), new Color3("E0E0E0")));
		
		Radio.config.Save();
	}

}