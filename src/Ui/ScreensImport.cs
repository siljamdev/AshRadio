using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using AshLib.Time;
using AshLib.Formatting;
using AshConsoleGraphics;
using AshConsoleGraphics.Interactive;

public partial class Screens{
	void setImport(){
		TuiSelectable[,] t = new TuiSelectable[,]{{
			new TuiButton("Import song", Placement.TopCenter, 0, 6, null, Palette.user).SetAction((s, ck) => {
				setImportSingleFile();
			})
		},{
			new TuiButton("Import all songs from folder", Placement.TopCenter, 0, 7, null, Palette.user).SetAction((s, ck) => {
				setImportFolder();
			})
		},{
			new TuiButton("Import folder as playlist", Placement.TopCenter, 0, 8, null, Palette.user).SetAction((s, ck) => {
				setImportFolderPlaylist();
			})
		},{
			new TuiButton("Import song", Placement.TopCenter, 0, 13, null, Palette.user).SetAction((s, ck) => {
				setImportSingleVideo();
			})
		},{
			new TuiButton("Import songs from yt playlist", Placement.TopCenter, 0, 14, null, Palette.user).SetAction((s, ck) => {
				setImportFromPlaylist();
			})
		},{
			new TuiButton("Import playlist from yt playlist", Placement.TopCenter, 0, 15, null, Palette.user).SetAction((s, ck) => {
				setImportPlaylist();
			})
		}};
		
		MiddleScreen l = generateMiddle(t);
		
		l.interactive.Elements.Add(new TuiLabel("Import songs", Placement.TopCenter, 0, 1, Palette.main));
		l.interactive.Elements.Add(new TuiLabel("From file", Placement.TopCenter, 0, 4, Palette.info));
		l.interactive.Elements.Add(new TuiLabel("From YouTube", Placement.TopCenter, 0, 10, Palette.info));
		l.interactive.Elements.Add(new TuiLabel("Importing from yt might take a while", Placement.TopCenter, 0, 11, Palette.info));
		
		setMiddleScreen(l);
	}
	
	void setImportSingleFile(){
		TuiFramedScrollingTextBox path = new TuiFramedScrollingTextBox("", 256, 34, Placement.TopCenter, 0, 5, null, null, null, Palette.user, Palette.user);
		TuiFramedScrollingTextBox title = new TuiFramedScrollingTextBox("", 256, 34, Placement.TopCenter, 0, 10, null, null, null, Palette.user, Palette.user);
		TuiFramedScrollingTextBox authors = new TuiFramedScrollingTextBox("", 256, 34, Placement.TopCenter, 0, 14, null, null, null, Palette.user, Palette.user);
		
		List<TuiLabel> error = new();
		
		TuiScreenInteractive l = null;
		
		TuiButton import = new TuiButton("Import", Placement.BottomCenter, 0, 2, null, Palette.user).SetAction((s, ck) => {
			int s2 = Radio.importSingleFile(removeQuotesSingle(path.Text), title.Text, authors.Text.Split(','), out string err);
			if(s2 < 0){
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
				setSongDetails(s2);
			}
		});
		
		path.OnParentResize += (s, a) => {
			path.BoxXsize = Math.Max(0, a.X - 4);
		};
		
		title.OnParentResize += (s, a) => {
			title.BoxXsize = Math.Max(0, a.X - 4);
		};
		
		authors.OnParentResize += (s, a) => {
			authors.BoxXsize = Math.Max(0, a.X - 4);
		};
		
		#if WINDOWS
			TuiButton search = new TuiButton("Search file", Placement.TopCenter, 0, 8, null, Palette.user).SetAction((s, ck) => {
				Thread thread = new Thread(() => {
				using(OpenFileDialog openFileDialog = new OpenFileDialog()){
					openFileDialog.Title = "Select a file";
					openFileDialog.Filter = "Audio Files|*.*";
					
					if(openFileDialog.ShowDialog() == DialogResult.OK){
						path.Text = openFileDialog.FileName;
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
				title
			},{
				authors
			},{
				import
			}};
		#else
			TuiSelectable[,] t = new TuiSelectable[,]{{
				path
			},{
				title
			},{
				authors
			},{
				import
			}};
		#endif
		
		l = generateMiddleInteractive(t);
		
		l.Elements.Add(new TuiLabel("Import song from file", Placement.TopCenter, 0, 1, Palette.main));
		l.Elements.Add(new TuiLabel("Path:", Placement.TopLeft, 2, 4));
		l.Elements.Add(new TuiLabel("Title:", Placement.TopLeft, 2, 9));
		l.Elements.Add(new TuiLabel("Authors (separated by commas):", Placement.TopLeft, 1, 13));
		
		setMiddleScreen(new MiddleScreen(l));
	}
	
	void setImportSingleVideo(){
		TuiFramedScrollingTextBox path = new TuiFramedScrollingTextBox("", 256, 34, Placement.TopCenter, 0, 5, null, null, null, Palette.user, Palette.user);
		TuiFramedScrollingTextBox title = new TuiFramedScrollingTextBox("", 256, 34, Placement.TopCenter, 0, 9, null, null, null, Palette.user, Palette.user);
		TuiFramedScrollingTextBox authors = new TuiFramedScrollingTextBox("", 256, 34, Placement.TopCenter, 0, 13, null, null, null, Palette.user, Palette.user);
		
		int j = 17;
		List<TuiLabel> error = new();
		
		TuiScreenInteractive l = null;
		
		bool b = false;
		
		TuiButton import = new TuiButton("Import", Placement.BottomCenter, 0, 2, null, Palette.user).SetAction((s, ck) => {
			if(b){
				return;
			}
			b = true;
			j = 17;
			
			foreach(TuiLabel a in error){
				l.Elements.Remove(a);
			}
			error.Clear();
			
			Action<string> r2d2 = err => {
				string[] r = err.Split(new string[]{"\r\n", "\n", "\r"}, StringSplitOptions.None);
				
				foreach(string e in r){
					TuiLabel a = new TuiLabel(e, Placement.TopLeft, 3, j, Palette.error);
					j++;
					l.Elements.Insert(0, a);
					error.Add(a);
				}
			};
			
			Task<int> task = Task.Run(() => Radio.importSingleVideo(path.Text, title.Text, authors.Text.Split(','), r2d2));
			
			task.ContinueWith(t => {
				if(currentMiddleScreen.interactive == l && t.Result > -1){
					closeMiddleScreen();
					setSongDetails(t.Result);
				}
				b = false;
			});
		});
		
		path.OnParentResize += (s, a) => {
			path.BoxXsize = Math.Max(0, a.X - 4);
		};
		
		title.OnParentResize += (s, a) => {
			title.BoxXsize = Math.Max(0, a.X - 4);
		};
		
		authors.OnParentResize += (s, a) => {
			authors.BoxXsize = Math.Max(0, a.X - 4);
		};
		
		TuiSelectable[,] t = new TuiSelectable[,]{{
			path
		},{
			title
		},{
			authors
		},{
			import
		}};
		
		l = generateMiddleInteractive(t);
		
		l.Elements.Add(new TuiLabel("Import song from youtube", Placement.TopCenter, 0, 1, Palette.main));
		l.Elements.Add(new TuiLabel("Url:", Placement.TopLeft, 2, 4));
		l.Elements.Add(new TuiLabel("Title:", Placement.TopLeft, 2, 8));
		l.Elements.Add(new TuiLabel("Authors (separated by commas):", Placement.TopLeft, 1, 12));
		
		setMiddleScreen(new MiddleScreen(l));
	}
	
	void setImportFolder(){
		TuiFramedScrollingTextBox path = new TuiFramedScrollingTextBox("", 256, 34, Placement.TopCenter, 0, 5, null, null, null, Palette.user, Palette.user);
		TuiFramedScrollingTextBox authors = new TuiFramedScrollingTextBox("", 256, 34, Placement.TopCenter, 0, 10, null, null, null, Palette.user, Palette.user);
		
		List<TuiLabel> error = new();
		
		TuiScreenInteractive l = null;
		
		TuiButton import = new TuiButton("Import", Placement.BottomCenter, 0, 2, null, Palette.user).SetAction((s, ck) => {
			bool s2 = Radio.importFromFolder(removeQuotesSingle(path.Text), authors.Text.Split(','), out string err);
			if(!s2){
				foreach(TuiLabel a in error){
					l.Elements.Remove(a);
				}
				error.Clear();
				
				string[] r = err.Split(new string[]{"\r\n", "\n", "\r"}, StringSplitOptions.None);
				
				int j = 13;
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
		
		path.OnParentResize += (s, a) => {
			path.BoxXsize = Math.Max(0, a.X - 4);
		};
		
		authors.OnParentResize += (s, a) => {
			authors.BoxXsize = Math.Max(0, a.X - 4);
		};
		
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
				authors
			},{
				import
			}};
		#else
			TuiSelectable[,] t = new TuiSelectable[,]{{
				path
			},{
				authors
			},{
				import
			}};
		#endif
		
		l = generateMiddleInteractive(t);
		
		l.Elements.Add(new TuiLabel("Import songs from folder", Placement.TopCenter, 0, 1, Palette.main));
		l.Elements.Add(new TuiLabel("Folder path:", Placement.TopLeft, 2, 4));
		l.Elements.Add(new TuiLabel("Authors (separated by commas):", Placement.TopLeft, 1, 9));
		
		setMiddleScreen(new MiddleScreen(l));
	}
	
	void setImportFromPlaylist(){
		TuiFramedScrollingTextBox path = new TuiFramedScrollingTextBox("", 256, 34, Placement.TopCenter, 0, 5, null, null, null, Palette.user, Palette.user);
		TuiFramedScrollingTextBox authors = new TuiFramedScrollingTextBox("", 256, 34, Placement.TopCenter, 0, 9, null, null, null, Palette.user, Palette.user);
		
		int j = 13;
		List<TuiLabel> error = new();
		
		TuiScreenInteractive l = null;
		
		bool b = false;
		
		TuiButton import = new TuiButton("Import", Placement.BottomCenter, 0, 2, null, Palette.user).SetAction((s, ck) => {
			if(b){
				return;
			}
			b = true;
			j = 13;
			
			foreach(TuiLabel a in error){
				l.Elements.Remove(a);
			}
			error.Clear();
			
			Action<string> r2d2 = err => {
				string[] r = err.Split(new string[]{"\r\n", "\n", "\r"}, StringSplitOptions.None);
				
				foreach(string e in r){
					TuiLabel a = new TuiLabel(e, Placement.TopLeft, 3, j, Palette.error);
					j++;
					l.Elements.Insert(0, a);
					error.Add(a);
				}
			};
			
			Task<bool> task = Task.Run(() => Radio.importFromPlaylist(path.Text, authors.Text.Split(','), r2d2));
			
			task.ContinueWith(t => {
				if(currentMiddleScreen.interactive == l && t.Result){
					closeMiddleScreen();
				}
				b = false;
			});
		});
		
		path.OnParentResize += (s, a) => {
			path.BoxXsize = Math.Max(0, a.X - 4);
		};
		
		authors.OnParentResize += (s, a) => {
			authors.BoxXsize = Math.Max(0, a.X - 4);
		};
		
		TuiSelectable[,] t = new TuiSelectable[,]{{
			path
		},{
			authors
		},{
			import
		}};
		
		l = generateMiddleInteractive(t);
		
		l.Elements.Add(new TuiLabel("Import songs from youtube playlist", Placement.TopCenter, 0, 1, Palette.main));
		l.Elements.Add(new TuiLabel("Url:", Placement.TopLeft, 2, 4));
		l.Elements.Add(new TuiLabel("Authors (separated by commas):", Placement.TopLeft, 1, 8));
		
		setMiddleScreen(new MiddleScreen(l));
	}
	
	void setImportFolderPlaylist(){
		TuiFramedScrollingTextBox path = new TuiFramedScrollingTextBox("", 256, 34, Placement.TopCenter, 0, 5, null, null, null, Palette.user, Palette.user);
		TuiFramedScrollingTextBox title = new TuiFramedScrollingTextBox("", 256, 34, Placement.TopCenter, 0, 10, null, null, null, Palette.user, Palette.user);
		TuiFramedScrollingTextBox authors = new TuiFramedScrollingTextBox("", 256, 34, Placement.TopCenter, 0, 14, null, null, null, Palette.user, Palette.user);
		
		List<TuiLabel> error = new();
		
		TuiScreenInteractive l = null;
		
		TuiButton import = new TuiButton("Import", Placement.BottomCenter, 0, 2, null, Palette.user).SetAction((s, ck) => {
			int s2 = Radio.importPlaylistFromFolder(removeQuotesSingle(path.Text), title.Text, authors.Text.Split(','), out string err);
			if(s2 < 0){
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
				setPlaylistDetails(s2);
			}
		});
		
		path.OnParentResize += (s, a) => {
			path.BoxXsize = Math.Max(0, a.X - 4);
		};
		
		title.OnParentResize += (s, a) => {
			title.BoxXsize = Math.Max(0, a.X - 4);
		};
		
		authors.OnParentResize += (s, a) => {
			authors.BoxXsize = Math.Max(0, a.X - 4);
		};
		
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
				title
			},{
				authors
			},{
				import
			}};
		#else
			TuiSelectable[,] t = new TuiSelectable[,]{{
				path
			},{
				title
			},{
				authors
			},{
				import
			}};
		#endif
		
		l = generateMiddleInteractive(t);
		
		l.Elements.Add(new TuiLabel("Import playlist from folder", Placement.TopCenter, 0, 1, Palette.main));
		l.Elements.Add(new TuiLabel("Folder path:", Placement.TopLeft, 2, 4));
		l.Elements.Add(new TuiLabel("Playlist title:", Placement.TopLeft, 2, 9));
		l.Elements.Add(new TuiLabel("Authors (separated by commas):", Placement.TopLeft, 1, 13));
		
		setMiddleScreen(new MiddleScreen(l));
	}
	
	void setImportPlaylist(){
		TuiFramedScrollingTextBox path = new TuiFramedScrollingTextBox("", 256, 34, Placement.TopCenter, 0, 5, null, null, null, Palette.user, Palette.user);
		TuiFramedScrollingTextBox title = new TuiFramedScrollingTextBox("", 256, 34, Placement.TopCenter, 0, 9, null, null, null, Palette.user, Palette.user);
		TuiFramedScrollingTextBox authors = new TuiFramedScrollingTextBox("", 256, 34, Placement.TopCenter, 0, 13, null, null, null, Palette.user, Palette.user);
		
		int j = 17;
		List<TuiLabel> error = new();
		
		TuiScreenInteractive l = null;
		
		bool b = false;
		
		TuiButton import = new TuiButton("Import", Placement.BottomCenter, 0, 2, null, Palette.user).SetAction((s, ck) => {
			if(b){
				return;
			}
			b = true;
			j = 17;
			
			foreach(TuiLabel a in error){
				l.Elements.Remove(a);
			}
			error.Clear();
			
			Action<string> r2d2 = err => {
				string[] r = err.Split(new string[]{"\r\n", "\n", "\r"}, StringSplitOptions.None);
				
				foreach(string e in r){
					TuiLabel a = new TuiLabel(e, Placement.TopLeft, 3, j, Palette.error);
					j++;
					l.Elements.Insert(0, a);
					error.Add(a);
				}
			};
			
			Task<int> task = Task.Run(() => Radio.importYtPlaylist(path.Text, title.Text, authors.Text.Split(','), r2d2));
			
			task.ContinueWith(t => {
				if(currentMiddleScreen.interactive == l && t.Result > -1){
					closeMiddleScreen();
					setPlaylistDetails(t.Result);
				}
				b = false;
			});
		});
		
		path.OnParentResize += (s, a) => {
			path.BoxXsize = Math.Max(0, a.X - 4);
		};
		
		title.OnParentResize += (s, a) => {
			title.BoxXsize = Math.Max(0, a.X - 4);
		};
		
		authors.OnParentResize += (s, a) => {
			authors.BoxXsize = Math.Max(0, a.X - 4);
		};
		
		TuiSelectable[,] t = new TuiSelectable[,]{{
			path
		},{
			title
		},{
			authors
		},{
			import
		}};
		
		l = generateMiddleInteractive(t);
		
		l.Elements.Add(new TuiLabel("Import playlist from youtube playlist", Placement.TopCenter, 0, 1, Palette.main));
		l.Elements.Add(new TuiLabel("Yt playlist url:", Placement.TopLeft, 2, 4));
		l.Elements.Add(new TuiLabel("Playlist title:", Placement.TopLeft, 2, 8));
		l.Elements.Add(new TuiLabel("Authors (separated by commas):", Placement.TopLeft, 1, 12));
		
		setMiddleScreen(new MiddleScreen(l));
	}
}