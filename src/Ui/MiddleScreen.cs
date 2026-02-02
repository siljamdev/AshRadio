using System.IO;
using System.Threading.Tasks;
using AshLib.Formatting;
using AshConsoleGraphics;
using AshConsoleGraphics.Interactive;

public partial class Screens{
	List<MiddleScreen> middle = new();
	
	//Peek
	MiddleScreen currentMiddleScreen => middle.Count > 0 ? middle[middle.Count - 1] : null;
	
	MiddleScreen middlePop(){
		if(middle.Count <= 0){
			return null;
		}
		
		MiddleScreen r = currentMiddleScreen;
		middle.RemoveAt(middle.Count - 1);
		
		return r;
	}
	
	MiddleScreen generateMiddle(TuiSelectable[,] b){
		return new MiddleScreen(generateMiddleInteractive(b));
	}
	
	TuiScreenInteractive generateMiddleInteractive(TuiSelectable[,] b){
		TuiScreenInteractive te = new TuiScreenInteractive(Math.Max((master?.Xsize ?? 100) - 62, 0),
			Math.Max((master?.Ysize ?? 20) - 6, 0),
			b, 0, 0, Placement.TopCenter, 0, 0, null,
			new TuiLabel(Keybinds.selectMiddle.ToString(), Placement.BottomLeft, 0, 0, Palette.hint));
		
		te.OnParentResize += (s, a) => {
			te.Xsize = Math.Max(a.X - 62, 0);
			te.Ysize = Math.Max(a.Y - 6, 0);
		};
		
		prepareScreen(te);
		
		return te;
	}
	
	TuiScreen generateMiddleStatic(){
		TuiScreen te = new TuiScreen(Math.Max((master?.Xsize ?? 100) - 62, 0),
			Math.Max((master?.Ysize ?? 20) - 6, 0),
			Placement.TopCenter, 0, 0, null,
			new TuiLabel(Keybinds.selectMiddle.ToString(), Placement.BottomLeft, 0, 0, Palette.hint));
		
		te.OnParentResize += (s, a) => {
			te.Xsize = Math.Max(a.X - 62, 0);
			te.Ysize = Math.Max(a.Y - 6, 0);
		};
		
		return te;
	}
	
	void setMiddleScreen(MiddleScreen m){
		MiddleScreen previous = currentMiddleScreen;
		
		middle.Add(m);
		
		master.ScreenList.Remove(previous.interactive);
		master.ScreenList.Add(m.interactive);
		
		setSelectedScreen(m);
		
		master.Elements.Remove(previous.screen);
		master.Elements.Add(m.screen);
	}
	
	void closeMiddleScreen(){
		if(middle.Count < 2){ //Comfirmation to close app
			confirmExit();
			
			return;
		}
		
		MiddleScreen toDel = middlePop();
		master.ScreenList.Remove(toDel.interactive);
		master.ScreenList.Add(currentMiddleScreen.interactive);
		
		master.Elements.Remove(toDel.screen);
		master.Elements.Add(currentMiddleScreen.screen);
		
		master.Xsize = master.Xsize; //Triggers a resize to make sure the middle screen is the correct size
		
		setSelectedScreen(currentMiddleScreen);
	}
	
	//Returns true if the hook to update can be destroyed
	bool updateMiddleScreen(MiddleScreen sc, Func<MiddleScreen> func){
		if(!middle.Contains(sc)){
			return true;
		}
		
		MiddleScreen up = func?.Invoke();
		if(up == null){
			if(currentMiddleScreen == sc){
				closeMiddleScreen();
			}else{
				middle.Remove(sc);
			}
			
			return true;
		}
		
		if(currentMiddleScreen == sc){
			master.ScreenList.Remove(sc.interactive);
			master.ScreenList.Add(up.interactive);
			
			master.Elements.Remove(sc.screen);
			master.Elements.Add(up.screen);
			
			int index = middle.IndexOf(sc);
			if(index != -1){
				middle[index] = up;
			}
			
			master.Xsize = master.Xsize; //Triggers a resize to make sure the middle screen is the correct size
			
			setSelectedScreen(currentMiddleScreen);
		}else{
			int index = middle.IndexOf(sc);
			if(index != -1){
				middle[index] = up;
			}
			
			sc.screen = up.screen;
			sc.interactive = up.interactive;
		}
		
		return true;
	}
	
	//used to avoid extra work. instead of closecurrent + opennewscreen, opennewscreen + removeprevious
	void removeMiddleScreen(MiddleScreen sc){
		if(currentMiddleScreen == sc){
			closeMiddleScreen();
		}else{
			middle.Remove(sc);
		}
	}
}

public class MiddleScreen{
	public TuiScreen screen;
	public TuiScreenInteractive interactive;
	public string identifier;
	public int hintPos = 0;
	
	public MiddleScreen(TuiScreen s, TuiScreenInteractive i, string id = null){
		screen = s;
		interactive = i;
		identifier = id;
	}
	
	public MiddleScreen(TuiScreenInteractive i, string id = null){
		screen = i;
		interactive = i;
		identifier = id;
	}
}