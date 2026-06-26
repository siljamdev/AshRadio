using AshLib.Formatting;
using AshConsoleGraphics;
using AshConsoleGraphics.Interactive;

public class TuiSongButton : TuiButton, IDisposable{
	public int id {get; private init;}
	
	bool selected => this.TextFormat == Palette.selected;
	
	public TuiSongButton(int id, Placement p, int x, int y, CharFormat? pf) : base("", p, x, y, Palette.song, pf){
		this.id = id;
		
		Keybinds.select.subEvent(this, (s2, ck) => {
			if(selected){
				Screens.selection.Remove(id);
			}else{
				Screens.selection.Add(id);
			}
		});
		
		Song.onSongTitleUpdate += onSongUpdate;
		Screens.onSelectionChange += onSelectionChanged;
		
		onSongUpdate(id);
		onSelectionChanged();
	}
	
	void onSongUpdate(int songid){
		if(songid == id){
			this.Text = Song.get(id)?.title ?? Song.nullTitle;
		}
	}
	
	void onSelectionChanged(){
		bool n = Screens.selection.Contains(id);
		if(n != selected){
			this.TextFormat = n ? Palette.selected : Palette.song;
			this.SelectedTextFormat = this.TextFormat;
		}
	}
	
	public void Dispose(){
		Song.onSongTitleUpdate -= onSongUpdate;
		Screens.onSelectionChange -= onSelectionChanged;
	}
}