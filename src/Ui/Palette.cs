using AshLib.AshFiles;
using AshLib.Formatting;
using AshConsoleGraphics;

public static class Palette{
	public static CharFormat? user {get; private set;}
	public static CharFormat? song {get; private set;}
	public static CharFormat? selected {get; private set;} //Selected song
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
			new ModelInstance(ModelInstanceOperation.Exists, "ui.palette.selected", toArray(new Color3("110FFF"), null)),
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
	
	public static void attemptMigration(){
		string[] keys = getPaletteModel().instances.Select(i => i.name).ToArray();
		
		foreach(string k in keys){
			if(Radio.config.TryGetValue(k, out Color3 c)){
				Radio.config.Set(k, new byte[]{c.R, c.G, c.B});
			}else if(Radio.config.TryGetValue(k, out Color3[] ca)){
				if(ca.Length == 1){
					Radio.config.Set(k, new byte[]{0, ca[0].R, ca[0].G, ca[0].B});
				}else if(ca.Length == 2){
					Radio.config.Set(k, new byte[]{ca[0].R, ca[0].G, ca[0].B, ca[1].R, ca[1].G, ca[1].B});
				}else{
					Radio.config.Set(k, new byte[0]);
				}
			}
		}
	}
	
	public static void init(){
		if((!FormatString.usesColors) || (Radio.config.TryGetValue("ui.useColors", out bool b) && !b)){
			AshConsoleGraphics.Buffer.NoFormat = true; //Its (no longer) broken :)
		}else{
			AshConsoleGraphics.Buffer.NoFormat = false;
		}
		
		user = loadColor("user");
		song = loadColor("song");
		selected = loadColor("selected");
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
		if(Radio.config.TryGetValue("ui.palette." + colorName, out byte[] ca)){
			if(ca.Length == 3){
				return new CharFormat(new Color3(ca[0], ca[1], ca[2]), null);
			}else if(ca.Length == 4){
				return new CharFormat(null, new Color3(ca[1], ca[2], ca[3]));
			}else if(ca.Length == 6){
				return new CharFormat(new Color3(ca[0], ca[1], ca[2]), new Color3(ca[3], ca[4], ca[5]));
			}else{
				return null;
			}
		}else{
			Radio.config.Remove("ui.palette." + colorName);
			return null;
		}
	}
	
	static byte[] toArray(Color3? fg, Color3? bg){
		if(fg == null){
			if(bg == null){
				return new byte[0];
			}else{
				Color3 b = (Color3) bg;
				return new byte[]{0, b.R, b.G, b.B};
			}
		}else{
			if(bg == null){
				Color3 f = (Color3) fg;
				return new byte[]{f.R, f.G, f.B};
			}else{
				Color3 f = (Color3) fg;
				Color3 b = (Color3) bg;
				return new byte[]{f.R, f.G, f.B, b.R, b.G, b.B};
			}
		}
	}
	
	public static void reset(){
		AshFileModel m = new AshFileModel(getPaletteModel().instances.Select(h => new ModelInstance(ModelInstanceOperation.Value, h.name, h.value)).ToArray());
		
		Radio.config.ApplyModel(m);
		Radio.config.Save();
	}
	
	public static AshFile export(){
		AshFile exp = AshFile.Clone(Radio.config);
		
		AshFileModel m = getPaletteModel();
		m.deleteNotMentioned = true;
		
		exp.ApplyModel(m);
		exp.path = null;
		
		return exp;
	}
	
	public static void import(AshFile source){
		AshFileModel m = getPaletteModel();
		m.deleteNotMentioned = true;
		
		source.ApplyModel(m);
		
		foreach(KeyValuePair<string, object> kvp in source){
			Radio.config.Set(kvp.Key, (Color3[]) kvp.Value);
		}
		
		Radio.config.Save();
	}
	
	public static void setSubtle(){
		Radio.config.Set("ui.palette.user", toArray(new Color3("FFFF00"), null));
		Radio.config.Set("ui.palette.song", toArray(new Color3("BEDD58"), null));
		Radio.config.Set("ui.palette.selected", toArray(new Color3("80F100"), null));
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
		Radio.config.Set("ui.palette.selected", toArray(new Color3("007DFF"), null));
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
		Radio.config.Set("ui.palette.selected", toArray(new Color3("1726DD"), null));
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