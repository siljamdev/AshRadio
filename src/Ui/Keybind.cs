using System.Text;
using AshConsoleGraphics;
using AshConsoleGraphics.Interactive;

public class Keybind{
	public (ConsoleKey, ConsoleModifiers)? primary {get; private set;}
	public (ConsoleKey, ConsoleModifiers)? secondary {get; private set;}
	
	public readonly string key;
	public readonly string description;
	
	public Keybind((ConsoleKey, ConsoleModifiers)? p, (ConsoleKey, ConsoleModifiers)? s, string desc){
		primary = p;
		secondary = s;
		key = "";
		description = desc;
	}
	
	public Keybind(string k, byte[] a, string desc){
		if(a.Length == 2){
			primary = ((ConsoleKey) a[0], (ConsoleModifiers) a[1]);
		}else if(a.Length == 4){
			primary = ((ConsoleKey) a[0], (ConsoleModifiers) a[1]);
			secondary = ((ConsoleKey) a[2], (ConsoleModifiers) a[3]);
		}
		key = k;
		description = desc;
	}
	
	public void subEvent(MiddleScreen s, bool addHint, Action<TuiScreenInteractive, ConsoleKeyInfo> act){
		if(primary is (ConsoleKey k, ConsoleModifiers m)){
			s.interactive.SubKeyEvent(k, m, act);
			
			if(addHint){
				s.screen.Elements.Add(new TuiTwoLabels(keybindToString(primary), " " + description, Placement.BottomRight, 0, s.hintPos, Palette.hint, null));
				s.hintPos++;
			}
		}
		
		if(secondary is (ConsoleKey k2, ConsoleModifiers m2)){
			s.interactive.SubKeyEvent(k2, m2, act);
		}
	}
	
	public void subEvent(MiddleScreen s, string hint, Action<TuiScreenInteractive, ConsoleKeyInfo> act){
		if(primary is (ConsoleKey k, ConsoleModifiers m)){
			s.interactive.SubKeyEvent(k, m, act);
			
			s.screen.Elements.Add(new TuiTwoLabels(keybindToString(primary), " " + hint, Placement.BottomRight, 0, s.hintPos, Palette.hint, null));
			s.hintPos++;
		}
		
		if(secondary is (ConsoleKey k2, ConsoleModifiers m2)){
			s.interactive.SubKeyEvent(k2, m2, act);
		}
	}
	
	public void subEvent(TuiScreenInteractive s, Action<TuiScreenInteractive, ConsoleKeyInfo> act){
		if(primary is (ConsoleKey k, ConsoleModifiers m)){
			s.SubKeyEvent(k, m, act);
		}
		
		if(secondary is (ConsoleKey k2, ConsoleModifiers m2)){
			s.SubKeyEvent(k2, m2, act);
		}
	}
	
	public void subEvent(MultipleTuiScreenInteractive s, Action<MultipleTuiScreenInteractive, ConsoleKeyInfo> act){
		if(primary is (ConsoleKey k, ConsoleModifiers m)){
			s.SubKeyEvent(k, m, act);
		}
		
		if(secondary is (ConsoleKey k2, ConsoleModifiers m2)){
			s.SubKeyEvent(k2, m2, act);
		}
	}
	
	public void subEvent(TuiSelectable s, Action<TuiSelectable, ConsoleKeyInfo> act){
		if(primary is (ConsoleKey k, ConsoleModifiers m)){
			s.SubKeyEvent(k, m, act);
		}
		
		if(secondary is (ConsoleKey k2, ConsoleModifiers m2)){
			s.SubKeyEvent(k2, m2, act);
		}
	}
	
	public override string ToString(){
		return keybindToString(primary);
	}
	
	public string ToStringFull(){
		return keybindToString(primary) + (secondary != null ? (", " + keybindToString(secondary)) : "");
	}
	
	//Static
	
	public static string keybindToString((ConsoleKey, ConsoleModifiers)? b){
		if(b is (ConsoleKey k, ConsoleModifiers m)){
			StringBuilder sb = new();
			
			if((m & ConsoleModifiers.Control) != 0){
				sb.Append("Ctrl+");
			}
			
			if((m & ConsoleModifiers.Shift) != 0){
				sb.Append("Shift+");
			}
			
			if((m & ConsoleModifiers.Alt) != 0){
				sb.Append("Alt+");
			}
			
			sb.Append(keyToString(k));
			
			return sb.ToString();
		}else{
			return "None";
		}
	}
	
	static string keyToString(ConsoleKey k){
		switch(k){
			case ConsoleKey.Escape:
				return "Esc";
			
			case ConsoleKey.Spacebar:
				return "Space";
			
			case ConsoleKey.LeftArrow:
				return "←";
			
			case ConsoleKey.UpArrow:
				return "↑";
			
			case ConsoleKey.RightArrow:
				return "→";
			
			case ConsoleKey.DownArrow:
				return "↓";
			
			case ConsoleKey.Insert:
				return "Ins";
			
			case ConsoleKey.Select:
				return "Sel";
			
			case ConsoleKey.Delete:
				return "Del";
			
			case ConsoleKey.LeftWindows:
				return "LWin";
			
			case ConsoleKey.RightWindows:
				return "RWin";
			
			case ConsoleKey.D0:
				return "0";
			
			case ConsoleKey.D1:
				return "1";
			
			case ConsoleKey.D2:
				return "2";
			
			case ConsoleKey.D3:
				return "3";
			
			case ConsoleKey.D4:
				return "4";
			
			case ConsoleKey.D5:
				return "5";
			
			case ConsoleKey.D6:
				return "6";
			
			case ConsoleKey.D7:
				return "7";
			
			case ConsoleKey.D8:
				return "8";
			
			case ConsoleKey.D9:
				return "9";
			
			case ConsoleKey.NumPad0:
				return "NP0";
			
			case ConsoleKey.NumPad1:
				return "NP1";
			
			case ConsoleKey.NumPad2:
				return "NP2";
			
			case ConsoleKey.NumPad3:
				return "NP3";
			
			case ConsoleKey.NumPad4:
				return "NP4";
			
			case ConsoleKey.NumPad5:
				return "NP5";
			
			case ConsoleKey.NumPad6:
				return "NP6";
			
			case ConsoleKey.NumPad7:
				return "NP7";
			
			case ConsoleKey.NumPad8:
				return "NP8";
			
			case ConsoleKey.NumPad9:
				return "NP9";
			
			case ConsoleKey.Add:
				return "NP+";
			
			case ConsoleKey.Subtract:
				return "NP-";
			
			case ConsoleKey.Multiply:
				return "NP*";
			
			case ConsoleKey.Divide:
				return "NP/";
			
			case ConsoleKey.OemPlus:
				return "+";
			
			case ConsoleKey.OemComma:
				return ",";
			
			case ConsoleKey.OemMinus:
				return "-";
			
			case ConsoleKey.OemPeriod:
				return ".";
			
			default:
				return k.ToString();
		}
	}
}