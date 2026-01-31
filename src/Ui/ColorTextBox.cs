using AshLib.Formatting;
using AshConsoleGraphics;
using AshConsoleGraphics.Interactive;

public class TuiColor3TextBox : TuiFramedScrollingTextBox{
	
	bool isBackGround;
	
	public TuiColor3TextBox(string t, int bl, Placement p, int x, int y, bool bg, CharFormat? ff, CharFormat? curfor, CharFormat? pf)
		: base(t, 7, bl, p, x, y, ff, ff, null, null, curfor, pf){
		
		isBackGround = bg;
		
		if(Color3.TryParse(this.Text, out Color3 col)){
			if(isBackGround){
				SelectedTextFormat = new CharFormat(null, col);
				TextFormat = SelectedTextFormat;
			}else{
				SelectedTextFormat = new CharFormat(col);
				TextFormat = SelectedTextFormat;
			}
		}else{
			SelectedTextFormat = null;
			TextFormat = SelectedTextFormat;
		}
		
		this.CanWriteChar = c => {
			if(Text.Length + 1 > Length){
				return null;
			}
			if(Uri.IsHexDigit(c) || c == '#'){
				if(Color3.TryParse(this.Text + c, out Color3 col)){
					if(isBackGround){
						SelectedTextFormat = new CharFormat(null, col);
						TextFormat = SelectedTextFormat;
					}else{
						SelectedTextFormat = new CharFormat(col);
						TextFormat = SelectedTextFormat;
					}
				}else{
					SelectedTextFormat = null;
					TextFormat = SelectedTextFormat;
				}
				
				return c.ToString();
			}
			return null;
		};
	}
	
	public override bool DelChar(){
		if(!base.DelChar()){
			return false;
		}
		
		if(Color3.TryParse(this.Text, out Color3 col)){
			if(isBackGround){
				SelectedTextFormat = new CharFormat(null, col);
				TextFormat = SelectedTextFormat;
			}else{
				SelectedTextFormat = new CharFormat(col);
				TextFormat = SelectedTextFormat;
			}
		}else{
			SelectedTextFormat = null;
			TextFormat = SelectedTextFormat;
		}
		
		return true;
	}
}