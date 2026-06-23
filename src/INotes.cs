using AshLib.Formatting;

interface INotes{
	string notes{get;}
	
	void setNotes(string notes);
	string getTitle();
	CharFormat? getStyle();
}