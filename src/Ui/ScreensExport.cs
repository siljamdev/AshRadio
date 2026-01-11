using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using AshLib.Time;
using AshLib.Formatting;
using AshConsoleGraphics;
using AshConsoleGraphics.Interactive;

public partial class Screens{	
	void setExportSong(Song s){
		TuiFramedScrollingTextBox path = new TuiFramedScrollingTextBox("", 256, 34, Placement.TopCenter, 0, 5, null, null, null, Palette.user, Palette.user);
		
		path.OnParentResize += (s, a) => {
			path.BoxXsize = Math.Max(0, a.X - 4);
		};
		
		List<TuiLabel> error = new();
		
		TuiScreenInteractive l = null;
		
		TuiButton export = new TuiButton("Export", Placement.BottomCenter, 0, 2, Palette.info, Palette.user).SetAction((s2, ck) => {
			bool succ = Song.export(s.id, removeQuotesSingle(path.Text), out string err);
			if(!succ){
				foreach(TuiLabel a in error){
					l.Elements.Remove(a);
				}
				error.Clear();
				
				string[] r = err.Split(new string[]{"\r\n", "\n", "\r"}, StringSplitOptions.None);
				
				int j = 17;
				foreach(string e in r){
					TuiLabel a = new TuiLabel(e, Placement.TopLeft, 3, j, Palette.error);
					j++;
					l.Elements.Insert(0, a);
					error.Add(a);
				}
			}else{
				closeMiddleScreen();
			}
		});
		
		#if WINDOWS
			TuiButton search = new TuiButton("Search folder", Placement.TopCenter, 0, 8, null, Palette.user).SetAction((s, ck) => {
				Thread thread = new Thread(() => {
				using(FolderBrowserDialog openFileDialog = new FolderBrowserDialog()){
					openFileDialog.Description = "Select a folder";
					openFileDialog.ShowNewFolderButton  = true;
					
					if(openFileDialog.ShowDialog() == DialogResult.OK){
						path.Text = openFileDialog.SelectedPath;
					}
				}});
				
				thread.SetApartmentState(ApartmentState.STA); // Required for OpenFileDialog
				thread.Start();
			});
			
			TuiSelectable[,] t = new TuiSelectable[,]{{
				path
			},{
				search
			},{
				export
			}};
		#else
			TuiSelectable[,] t = new TuiSelectable[,]{{
				path
			},{
				export
			}};
		#endif
		
		l = generateMiddleInteractive(t);
		
		l.Elements.Add(new TuiTwoLabels("Export ", s.title, Placement.TopCenter, 0, 1, null, Palette.song));
		l.Elements.Add(new TuiLabel("Folder path:", Placement.TopLeft, 2, 4));
		
		setMiddleScreen(new MiddleScreen(l));
	}
	
	void setExportPlaylist(Playlist p){
		TuiFramedScrollingTextBox path = new TuiFramedScrollingTextBox("", 256, 34, Placement.TopCenter, 0, 5, null, null, null, Palette.user, Palette.user);
		
		path.OnParentResize += (s, a) => {
			path.BoxXsize = Math.Max(0, a.X - 4);
		};
		
		List<TuiLabel> error = new();
		
		TuiScreenInteractive l = null;
		
		bool b = false;
		
		TuiButton export = null!;
		export = new TuiButton("Export", Placement.BottomCenter, 0, 2, Palette.info, Palette.user).SetAction((s2, ck) => {
			if(b){
				return;
			}
			
			export.Text = "Exporting…";
			b = true;
			foreach(TuiLabel a in error){
				l.Elements.Remove(a);
			}
			error.Clear();
			
			List<Song> lib = p.getSongs();
			
			bool anyBad = false;
			int j = 10;
			
			Task task = Task.Run(() => {
				foreach(Song s in lib){
					bool succ = Song.export(s.id, removeQuotesSingle(path.Text), out string err);
					if(!succ){
						string[] r = err.Split(new string[]{"\r\n", "\n", "\r"}, StringSplitOptions.None);
						
						foreach(string e in r){
							TuiLabel a = new TuiLabel(e, Placement.TopLeft, 3, j, Palette.error);
							j++;
							l.Elements.Insert(0, a);
							error.Add(a);
						}
						
						anyBad = true;
					}
				}
			});
			
			task.ContinueWith(t => {
				if(!anyBad){
					closeMiddleScreen();
				}
				export.Text = "Export";
				b = false;
			});
		});
		
		#if WINDOWS
			TuiButton search = new TuiButton("Search folder", Placement.TopCenter, 0, 8, null, Palette.user).SetAction((s, ck) => {
				Thread thread = new Thread(() => {
				using(FolderBrowserDialog openFileDialog = new FolderBrowserDialog()){
					openFileDialog.Description = "Select a folder";
					openFileDialog.ShowNewFolderButton  = true;
					
					if(openFileDialog.ShowDialog() == DialogResult.OK){
						path.Text = openFileDialog.SelectedPath;
					}
				}});
				
				thread.SetApartmentState(ApartmentState.STA); // Required for OpenFileDialog
				thread.Start();
			});
			
			TuiSelectable[,] t = new TuiSelectable[,]{{
				path
			},{
				search
			},{
				export
			}};
		#else
			TuiSelectable[,] t = new TuiSelectable[,]{{
				path
			},{
				export
			}};
		#endif
		
		l = generateMiddleInteractive(t);
		
		l.Elements.Add(new TuiTwoLabels("Export ", p.title, Placement.TopCenter, 0, 1, null, Palette.playlist));
		l.Elements.Add(new TuiLabel("Folder path:", Placement.TopLeft, 2, 4));
		
		setMiddleScreen(new MiddleScreen(l));
	}
	
	void setExportLibrary(){
		TuiFramedScrollingTextBox path = new TuiFramedScrollingTextBox("", 256, 34, Placement.TopCenter, 0, 5, null, null, null, Palette.user, Palette.user);
		
		path.OnParentResize += (s, a) => {
			path.BoxXsize = Math.Max(0, a.X - 4);
		};
		
		List<TuiLabel> error = new();
		
		TuiScreenInteractive l = null;
		
		bool b = false;
		
		TuiButton export = null!;
		export = new TuiButton("Export", Placement.BottomCenter, 0, 2, Palette.info, Palette.user).SetAction((s2, ck) => {
			if(b){
				return;
			}
			
			export.Text = "Exporting…";
			b = true;
			foreach(TuiLabel a in error){
				l.Elements.Remove(a);
			}
			error.Clear();
			
			List<Song> lib = Song.getLibrary();
			
			bool anyBad = false;
			int j = 10;
			
			Task task = Task.Run(() => {
				foreach(Song s in lib){
					bool succ = Song.export(s.id, removeQuotesSingle(path.Text), out string err);
					if(!succ){
						string[] r = err.Split(new string[]{"\r\n", "\n", "\r"}, StringSplitOptions.None);
						
						foreach(string e in r){
							TuiLabel a = new TuiLabel(e, Placement.TopLeft, 3, j, Palette.error);
							j++;
							l.Elements.Insert(0, a);
							error.Add(a);
						}
						
						anyBad = true;
					}
				}
			});
			
			task.ContinueWith(t => {
				if(!anyBad){
					closeMiddleScreen();
				}
				export.Text = "Export";
				b = false;
			});
		});
		
		#if WINDOWS
			TuiButton search = new TuiButton("Search folder", Placement.TopCenter, 0, 8, null, Palette.user).SetAction((s, ck) => {
				Thread thread = new Thread(() => {
				using(FolderBrowserDialog openFileDialog = new FolderBrowserDialog()){
					openFileDialog.Description = "Select a folder";
					openFileDialog.ShowNewFolderButton  = true;
					
					if(openFileDialog.ShowDialog() == DialogResult.OK){
						path.Text = openFileDialog.SelectedPath;
					}
				}});
				
				thread.SetApartmentState(ApartmentState.STA); // Required for OpenFileDialog
				thread.Start();
			});
			
			TuiSelectable[,] t = new TuiSelectable[,]{{
				path
			},{
				search
			},{
				export
			}};
		#else
			TuiSelectable[,] t = new TuiSelectable[,]{{
				path
			},{
				export
			}};
		#endif
		
		l = generateMiddleInteractive(t);
		
		l.Elements.Add(new TuiLabel("Export library", Placement.TopCenter, 0, 1, Palette.info));
		l.Elements.Add(new TuiLabel("Folder path:", Placement.TopLeft, 2, 4));
		
		setMiddleScreen(new MiddleScreen(l));
	}
}