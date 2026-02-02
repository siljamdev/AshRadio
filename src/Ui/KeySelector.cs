using AshLib.Formatting;
using AshConsoleGraphics;
using AshConsoleGraphics.Interactive;

public class TuiKeySelector : TuiSelectable{
	
	public CharFormat? TextFormat {get;
	set{
		field = value;
		needToGenBuffer = true;
	}}
	
	public CharFormat? SelectedTextFormat {get;
	set{
		field = value;
		needToGenBuffer = true;
	}}
	
	public CharFormat? ListeningFormat {get;
	set{
		field = value;
		needToGenBuffer = true;
	}}
	
	public CharFormat? SelectorFormat {get;
	set{
		field = value;
		needToGenBuffer = true;
	}}
	
	bool isListening {get;
	set{
		field = value;
		needToGenBuffer = true;
	}}
	
	public (ConsoleKey, ConsoleModifiers)? key {get;
	private set{
		field = value;
		needToGenBuffer = true;
	}}
	
	public TuiKeySelector((ConsoleKey, ConsoleModifiers)? k, Placement p, int x, int y, CharFormat? tf, CharFormat? stf, CharFormat? lf, CharFormat? pf) : base(p, x, y){
		TextFormat = tf;
		SelectedTextFormat = stf;
		ListeningFormat = lf;
		SelectorFormat = pf;
		key = k;
	}
	
	override protected AshConsoleGraphics.Buffer GenerateBuffer(){
		AshConsoleGraphics.Buffer b;
		
		string Text = isListening ? "Listening…" : Keybind.keybindToString(key);
		
		if(Selected){
			b = new AshConsoleGraphics.Buffer(Text.Length + 2, 1);
			b.SetChar(0, 0, LeftSelector, SelectorFormat);
			b.SetChar(Text.Length + 1, 0, RightSelector, SelectorFormat);
			for(int i = 0; i < Text.Length; i++){
				b.SetChar(1 + i, 0, Text[i], isListening ? ListeningFormat : SelectedTextFormat);
			}
		}else{
			b = new AshConsoleGraphics.Buffer(Text.Length, 1);
			for(int i = 0; i < Text.Length; i++){
				b.SetChar(i, 0, Text[i], isListening ? ListeningFormat : TextFormat);
			}
		}
		return b;
	}
	
	public override bool HandleKey(ConsoleKeyInfo keyInfo){
		if(!isListening){
			if(keyInfo.Key == ConsoleKey.Enter && keyInfo.Modifiers == ConsoleModifiers.None){
				isListening = true;
				return true;
			}
			return false;
		}else{
			if(keyInfo.Key == ConsoleKey.Escape && keyInfo.Modifiers == ConsoleModifiers.None){
				isListening = false;
				key = null;
				return true;
			}
			isListening = false;
			key = (keyInfo.Key, keyInfo.Modifiers);
			return true;
		}
	}
}