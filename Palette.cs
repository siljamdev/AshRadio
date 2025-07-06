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
	public static CharFormat? background {get; private set;}
	
	public static CharFormat? error {get; private set;}
	
	public static void init(){
		if((!FormatString.usesColors) || (Radio.config.CanGetCamp("ui.useColors", out bool b) && !b)){
			AshConsoleGraphics.Buffer.NoFormat = true; //Its (no longer) broken :)
			
			user = null;
			song = null;
			author = null;
			playlist = null;
			main = null;
			delimiter = null;
			hint = null;
			info = null;
			background = null;
			
			error = null;
			
			return;
		}
		
		AshConsoleGraphics.Buffer.NoFormat = false;
		
		user = new CharFormat(Radio.config.GetCamp<Color3>("ui.palette.user"));
		song = new CharFormat(Radio.config.GetCamp<Color3>("ui.palette.song"));
		author = new CharFormat(Radio.config.GetCamp<Color3>("ui.palette.author"));
		playlist = new CharFormat(Radio.config.GetCamp<Color3>("ui.palette.playlist"));
		main = new CharFormat(Radio.config.GetCamp<Color3>("ui.palette.main"));
		delimiter = new CharFormat(Radio.config.GetCamp<Color3>("ui.palette.delimiter"));
		hint = new CharFormat(Radio.config.GetCamp<Color3>("ui.palette.hint"));
		info = new CharFormat(Radio.config.GetCamp<Color3>("ui.palette.info"));
		background = new CharFormat(null, Radio.config.GetCamp<Color3>("ui.palette.background"));
		
		error = new CharFormat(Radio.config.GetCamp<Color3>("ui.palette.error"));;
	}
	
	public static void setAsh(){
		Radio.config.SetCamp("ui.palette.user", Color3.Yellow);
		Radio.config.SetCamp("ui.palette.song", new Color3("3295FF"));
		Radio.config.SetCamp("ui.palette.author", Color3.Green);
		Radio.config.SetCamp("ui.palette.playlist", new Color3("FFA811"));
		Radio.config.SetCamp("ui.palette.main", new Color3("E7484B"));
		Radio.config.SetCamp("ui.palette.delimiter", new Color3("5B2D72"));
		Radio.config.SetCamp("ui.palette.hint", new Color3("9F60C1"));
		Radio.config.SetCamp("ui.palette.info", new Color3("849DD6"));
		Radio.config.SetCamp("ui.palette.background", new Color3("101010"));
		
		Radio.config.SetCamp("ui.palette.error", new Color3("D83F3C"));
		
		Radio.config.Save();
		
		init();
	}
	
	public static void setSubtle(){
		Radio.config.SetCamp("ui.palette.user", Color3.Yellow);
		Radio.config.SetCamp("ui.palette.song", new Color3("BEDD58"));
		Radio.config.SetCamp("ui.palette.author", new Color3("8CD0D3"));
		Radio.config.SetCamp("ui.palette.playlist", new Color3("966DD3"));
		Radio.config.SetCamp("ui.palette.main", new Color3("E5AA62"));
		Radio.config.SetCamp("ui.palette.delimiter", new Color3("848260"));
		Radio.config.SetCamp("ui.palette.hint", new Color3("918050"));
		Radio.config.SetCamp("ui.palette.info", new Color3("CC7651"));
		Radio.config.SetCamp("ui.palette.background", new Color3("101010"));
		
		Radio.config.SetCamp("ui.palette.error", new Color3("D83F3C"));
		
		Radio.config.Save();
		
		init();
	}
	
	public static void setNeon(){ //Chatgpt made this palette bc i ran out of ideas :/
		Radio.config.SetCamp("ui.palette.user", new Color3("39FF14"));         // Neon green
		Radio.config.SetCamp("ui.palette.song", new Color3("00FFFF"));         // Cyan
		Radio.config.SetCamp("ui.palette.author", new Color3("FF44CC"));       // Pink
		Radio.config.SetCamp("ui.palette.playlist", new Color3("FF8800"));     // Orange
		Radio.config.SetCamp("ui.palette.main", new Color3("AA00FF"));         // Purple
		Radio.config.SetCamp("ui.palette.delimiter", new Color3("00FFAA"));    // Mint green
		Radio.config.SetCamp("ui.palette.hint", new Color3("FF66FF"));         // Light magenta
		Radio.config.SetCamp("ui.palette.info", new Color3("33CCFF"));         // Sky blue
		Radio.config.SetCamp("ui.palette.background", new Color3("0A0A0A"));   // Almost black
		
		Radio.config.SetCamp("ui.palette.error", new Color3("FF0033"));        // Bright red
		
		Radio.config.Save();
		
		init();
	}
}