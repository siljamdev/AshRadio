using AshLib;
using AshLib.Formatting;
using AshConsoleGraphics;
using AshConsoleGraphics.Interactive;

public class TuiFramedTextBoxFloat : TuiFramedTextBox{
	
	public TuiFramedTextBoxFloat(string chars, string t, uint bl, Placement p, int x, int y, CharFormat? ff, CharFormat? sff, CharFormat? tf, CharFormat? stf, CharFormat? pf)
		: base(chars, t, bl, p, x, y, ff, sff, tf, stf, pf){}
	
	public TuiFramedTextBoxFloat(string chars, string t, uint bl, Placement p, int x, int y, CharFormat? ff = null, CharFormat? tf = null, CharFormat? pf = null)
		: this(chars, t, bl, p, x, y, ff, ff, tf, tf, pf){}
	
	public TuiFramedTextBoxFloat(string t, uint bl, Placement p, int x, int y, CharFormat? ff, CharFormat? sff, CharFormat? tf, CharFormat? stf, CharFormat? pf)
		: this(null, t, bl, p, x, y, ff, sff, tf, stf, pf){}
	
	public TuiFramedTextBoxFloat(string t, uint bl, Placement p, int x, int y, CharFormat? ff = null, CharFormat? tf = null, CharFormat? pf = null)
		: this(t, bl, p, x, y, ff, ff, tf, tf, pf){}
	
	public override bool WriteChar(char c){
		if(c == '\n' || Text.Length + 1 > Length){
			return false;
		}
		if(char.IsDigit(c) || c == '-' || c == '.'){
			Text = Text + c;
			return true;
		}
		return false;
	}
}

public class TuiFramedTextBoxUInt : TuiFramedTextBox{
	
	public TuiFramedTextBoxUInt(string chars, string t, uint bl, Placement p, int x, int y, CharFormat? ff, CharFormat? sff, CharFormat? tf, CharFormat? stf, CharFormat? pf)
		: base(chars, t, bl, p, x, y, ff, sff, tf, stf, pf){}
	
	public TuiFramedTextBoxUInt(string chars, string t, uint bl, Placement p, int x, int y, CharFormat? ff = null, CharFormat? tf = null, CharFormat? pf = null)
		: this(chars, t, bl, p, x, y, ff, ff, tf, tf, pf){}
	
	public TuiFramedTextBoxUInt(string t, uint bl, Placement p, int x, int y, CharFormat? ff, CharFormat? sff, CharFormat? tf, CharFormat? stf, CharFormat? pf)
		: this(null, t, bl, p, x, y, ff, sff, tf, stf, pf){}
	
	public TuiFramedTextBoxUInt(string t, uint bl, Placement p, int x, int y, CharFormat? ff = null, CharFormat? tf = null, CharFormat? pf = null)
		: this(t, bl, p, x, y, ff, ff, tf, tf, pf){}
	
	public override bool WriteChar(char c){
		if(c == '\n' || Text.Length + 1 > Length){
			return false;
		}
		if(char.IsDigit(c)){
			Text = Text + c;
			return true;
		}
		return false;
	}
}

public class TuiFramedScrollingTextBoxColor3 : TuiFramedScrollingTextBox{
	
	public TuiFramedScrollingTextBoxColor3(string chars, string t, uint bl, Placement p, int x, int y, CharFormat? ff, CharFormat? sff, CharFormat? tf, CharFormat? stf, CharFormat? pf)
		: base(chars, t, 7, bl, p, x, y, ff, sff, tf, stf, pf){}
	
	public TuiFramedScrollingTextBoxColor3(string chars, string t, uint bl, Placement p, int x, int y, CharFormat? ff = null, CharFormat? tf = null, CharFormat? pf = null)
		: this(chars, t, bl, p, x, y, ff, ff, tf, tf, pf){}
	
	public TuiFramedScrollingTextBoxColor3(string t, uint bl, Placement p, int x, int y, CharFormat? ff, CharFormat? sff, CharFormat? tf, CharFormat? stf, CharFormat? pf)
		: this(null, t, bl, p, x, y, ff, sff, tf, stf, pf){}
	
	public TuiFramedScrollingTextBoxColor3(string t, uint bl, Placement p, int x, int y, CharFormat? ff = null, CharFormat? tf = null, CharFormat? pf = null)
		: this(t, bl, p, x, y, ff, ff, tf, tf, pf){}
	
	public override bool WriteChar(char c){
		if(c == '\n' || Text.Length + 1 > Length){
			return false;
		}
		if(Uri.IsHexDigit(c) || c == '#'){
			Text = Text + c;
			return true;
		}
		return false;
	}
}
