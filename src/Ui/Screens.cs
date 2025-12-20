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
			s.screen.DefFormat = Palette.background;
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
			master.SelectedScreen.DefFormat = Palette.background;
		}
	}
	
	void prepareScreen(TuiScreenInteractive t){
		t.DeleteAllKeyEvents();
		
		t.SubKeyEvent(ConsoleKey.UpArrow, TuiScreenInteractive.MoveUp);
		t.SubKeyEvent(ConsoleKey.DownArrow, TuiScreenInteractive.MoveDown);
		t.SubKeyEvent(ConsoleKey.LeftArrow, TuiScreenInteractive.MoveLeft);
		t.SubKeyEvent(ConsoleKey.RightArrow, TuiScreenInteractive.MoveRight);
	}
	
	//Utils
	
	void setSearchScreen(string question, Action<string> onEnter){
		MiddleScreen sc = null!;
		
		TuiMultiLineScrollingFramedTextBox input = new TuiMultiLineScrollingFramedTextBox("", 256, 34, 3, Placement.TopCenter, 0, 4, null, null, null, Palette.user, Palette.user);
		
		input.OnParentResize += (s, a) => {
			input.BoxXsize = Math.Max(0, a.X - 4);
		};
		
		input.SubKeyEvent(ConsoleKey.Enter, (s, ck) => {
			onEnter?.Invoke(input.Text);
			removeMiddleScreen(sc);
		});
		
		TuiSelectable[,] t = new TuiSelectable[,]{{
			input
		}};
		
		sc = generateMiddle(t);
		
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
		
		setMiddleScreen(sc);
	}
	
	
	//Utilities
	
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
			}
			else if(OperatingSystem.IsLinux()){
				Process.Start("xdg-open", url);
			}
			else if(OperatingSystem.IsMacOS()){
				Process.Start("open", url);
			}
		}
		catch(Exception e){}
	}
	
	//Crop length of string
	static string crop(string s, int len){
		if(s.Length > len){
			return s.Substring(0, len - 1) + "…";
		}
		return s;
	}
}