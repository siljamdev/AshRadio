using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using AshLib.Formatting;
using AshConsoleGraphics;
using AshConsoleGraphics.Interactive;

public partial class Screens{
	void setSelectedScreen(MiddleScreen s){
		if(master == null){
			return;
		}
		
		if(master.SelectedScreen != null){
			MiddleScreen ind = middle.FirstOrDefault(h => h.interactive == master.SelectedScreen);
			if(ind != null){
				ind.screen.DefFormat = null;
			}else{
				master.SelectedScreen.DefFormat = null;
			}
		}
		
		master.SelectedScreen = s.interactive;
		
		if(master.SelectedScreen != null){
			s.screen.DefFormat = Palette.selectedPanel;
		}
	}
	
	void setSelectedScreen(TuiScreenInteractive s){
		if(master == null){
			return;
		}
		
		if(master.SelectedScreen != null){
			MiddleScreen ind = middle.FirstOrDefault(h => h.interactive == master.SelectedScreen);
			if(ind != null){
				ind.screen.DefFormat = null;
			}else{
				master.SelectedScreen.DefFormat = null;
			}
		}
		
		master.SelectedScreen = s;
		
		if(master.SelectedScreen != null){
			master.SelectedScreen.DefFormat = Palette.selectedPanel;
		}
	}
	
	void prepareScreen(TuiScreenInteractive t){
		t.DeleteAllKeyEvents();
		
		Keybinds.up.subEvent(t, TuiScreenInteractive.MoveUp);
		Keybinds.down.subEvent(t, TuiScreenInteractive.MoveDown);
		Keybinds.left.subEvent(t, TuiScreenInteractive.MoveLeft);
		Keybinds.right.subEvent(t, TuiScreenInteractive.MoveRight);
		
		if(t is TuiScrollingScreenInteractive){
			Keybinds.scrollUp.subEvent(t, TuiScrollingScreenInteractive.ScrollUp);
			Keybinds.scrollDown.subEvent(t, TuiScrollingScreenInteractive.ScrollDown);
		}
	}
	
	//Generic screens
	
	void setSearchScreen(string question, Action<string> onEnter){
		MiddleScreen sc = null!;
		
		TuiMultiLineScrollingFramedTextBox input = new TuiMultiLineScrollingFramedTextBox("", 256, 34, 3, Placement.TopCenter, 0, 4, null, null, null, Palette.writing, Palette.user, Palette.user);
		
		input.OnParentResize += (s, a) => {
			input.BoxXsize = Math.Max(0, a.X - 4);
		};
		
		TuiSelectable[,] t = new TuiSelectable[,]{{
			input
		}};
		
		sc = generateMiddle(t);
		
		Keybinds.enter.subEvent(sc, "Search", (s, ck) => {
			onEnter?.Invoke(input.Text);
			removeMiddleScreen(sc);
		});
		
		sc.interactive.Elements.Add(new TuiLabel("Search", Placement.TopCenter, 0, 1, Palette.main));
		sc.interactive.Elements.Add(new TuiLabel(question, Placement.TopCenter, 0, 3));
		
		setMiddleScreen(sc);
	}
	
	void setConfirmScreen(string question, Action onEnter){
		MiddleScreen sc = null!;
		
		TuiSelectable[,] t = {{
			new TuiButton("Yes", Placement.Center, -4, 1, null, Palette.user).SetAction((s, ck) => {
				onEnter?.Invoke();
				removeMiddleScreen(sc);
			}),
			new TuiButton("No", Placement.Center, 4, 1, null, Palette.user).SetAction((s, ck) => closeMiddleScreen())
		}};
		
		sc = generateMiddle(t);
		
		sc.interactive.Elements.Add(new TuiLabel(question, Placement.Center, 0, -1));
		sc.interactive.Elements.Add(new TuiFrame(Math.Max(question.Length + 4, 20), 7, Placement.Center, 0, 0, Palette.user));
		
		sc.interactive.MatrixPointerX = 1;
		
		setMiddleScreen(sc);
	}
	
	void confirmExit(){
		TuiSelectable[,] buttons = {{
			new TuiButton("Exit", Placement.Center, -4, 1, null, Palette.user).SetAction((s, ck) => {
				MultipleTuiScreenInteractive.StopPlaying(master, default);
			}),
			new TuiButton("Cancel", Placement.Center, 4, 1, null, Palette.user).SetAction((s, ck) => closeMiddleScreen())
		}};
		
		MiddleScreen t = generateMiddle(buttons);
		t.interactive.Elements.Add(new TuiLabel("Do you want to exit?", Placement.Center, 0, -1));
		t.interactive.Elements.Add(new TuiFrame(24, 7, Placement.Center, 0, 0, Palette.user));
		
		Keybinds.escape.subEvent(t, false, (s, ck) => {
			MultipleTuiScreenInteractive.StopPlaying(master, default);
		}); //Escape + escape will close
		
		setMiddleScreen(t);
	}
	
	void reinitScreens(){
		Radio.tryInitScreens = true;
		MultipleTuiScreenInteractive.StopPlaying(master, default);
	}
	
	//Utilities
	
	static void setLooping(TuiOptionPicker top){
		top.DeleteAllKeyEvents();
		
		Keybinds.left.subEvent(top, (s, cki) => {
			if(top.SelectedOptionIndex == 0){
				top.SelectedOptionIndex = (uint) (top.Options.Length - 1);
			}else{
				top.SelectedOptionIndex--;
			}
		});
		
		Keybinds.right.subEvent(top, (s, cki) => {
			if(top.SelectedOptionIndex == top.Options.Length - 1){
				top.SelectedOptionIndex = 0;
			}else{
				top.SelectedOptionIndex++;
			}
		});
	}
	
	//remove surrounding quotes
	static string removeQuotesSingle(string p){
		p = p.Trim();
		
		if(p.Length < 1){
			return p;
		}
		char[] c = p.ToCharArray();
		if(c[0] == '\"' && c[c.Length - 1] == '\"'){
			if(c.Length < 2){
				return "";
			}
			return p.Substring(1, p.Length - 2);
		}
		return p;
	}
	
	static void openUrl(string url){
		try{
			if(OperatingSystem.IsWindows()){
				Process.Start(new ProcessStartInfo{
					FileName = url,
					UseShellExecute = true
				});
			}else if(OperatingSystem.IsLinux()){
				Process.Start("xdg-open", url);
			}else if(OperatingSystem.IsMacOS()){
				Process.Start("open", url);
			}
		}
		catch(Exception e){
			Radio.reportError(e.ToString());
		}
	}
	
	static void openFolder(string path){
		try{
			if(OperatingSystem.IsWindows()){
				Process.Start(new ProcessStartInfo{
					FileName = path,
					UseShellExecute = true
				});
			}else if(OperatingSystem.IsLinux()){
				Process.Start("xdg-open", path);
			}else if(OperatingSystem.IsMacOS()){
				Process.Start("open", path);
			}
		}
		catch(Exception e){
			Radio.reportError(e.ToString());
		}
	}
	
	//Crop length of string
	static string crop(string s, int len){
		if(s.Length > len){
			return s.Substring(0, len - 1) + "…";
		}
		return s;
	}
	
	static string secondsToReadable(float seconds){
		TimeSpan ts = TimeSpan.FromSeconds(seconds);
		
		int days = ts.Days;
		int hours = ts.Hours;
		int minutes = ts.Minutes;
		int secs = ts.Seconds;
		
		List<string> parts = new();
		
		if(days > 0){
			parts.Add($"{days}d");
		}
		if(hours > 0){
			parts.Add($"{hours}h");
		}
		if(minutes > 0){
			parts.Add($"{minutes}m");
		}
		parts.Add($"{secs}s");
		
		return string.Join(" ", parts);
	}
	
	static string secondsToHourTime(float seconds){
		int sec = Math.Max(0, (int) Math.Round(seconds));
		
		return string.Format("{0}:{1:D2}:{2:D2}", sec / 3600, (sec % 3600) / 60, sec % 60);
	}
	
	static string secondsToMinuteTime(float seconds){
		int sec = Math.Max(0, (int) Math.Round(seconds));
		
		return string.Format("{0}:{1:D2}", sec / 60, sec % 60);
	}
	
	static void hideCursor(){
		Console.Write("\x1b[?25l");
	}
	
	public static void showCursor(){
		Console.Write("\x1b[?25h");
	}
	
	static void enterAltBuffer(){
		Console.Write("\x1b[?1049h");
	}
	
	public static void exitAltBuffer(){
		Console.Write("\x1b[?1049l");
	}
}