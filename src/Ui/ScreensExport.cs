using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using AshLib.Time;
using AshLib.Formatting;
using AshConsoleGraphics;
using AshConsoleGraphics.Interactive;

public partial class Screens{	
	void setExport(int[] songs, string source, CharFormat? sourceStyle, Action? onSuccess = null){
		if(songs == null || songs.Length == 0){
			return;
		}
		
		TuiFramedScrollingTextBox path = new TuiFramedScrollingTextBox(Radio.session.GetValue<string>("preferences.exportPath"), 256, 34, Placement.TopCenter, 0, 5, null, null, null, Palette.writing, Palette.user, Palette.user);
		
		path.OnParentResize += (s, a) => {
			path.BoxXsize = Math.Max(0, a.X - 4);
		};
		
		List<TuiLabel> error = new();
		
		TuiScreenInteractive l = null!;
		MiddleScreen midSc = null!;
		
		TuiFramedCheckBox includeIndex = new TuiFramedCheckBox(' ', 'X', Radio.session.GetValue<bool>("preferences.exportIndex"), Placement.TopCenter, 4, 10, null, null, null, Palette.writing, Palette.user);
		
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
			
			Radio.session.Set("preferences.exportIndex", includeIndex.Checked);
			Radio.session.Set("preferences.exportPath", removeQuotesSingle(path.Text));
			Radio.session.Save();
			
			bool anyBad = false;
			int j = 10;
			
			Task task = Task.Run(() => {
				int i = 0;
				foreach(int s in songs){
					bool succ = Song.export(s, removeQuotesSingle(path.Text), includeIndex.Checked ? (i + 1) : null, out string err);
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
					i++;
				}
			});
			
			task.ContinueWith(t => {
				if(!anyBad){
					onSuccess?.Invoke();
					removeMiddleScreen(midSc);
				}else{
					export.Text = "Export";
					b = false;
				}
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
				includeIndex
			},{
				export
			}};
		#else
			TuiSelectable[,] t = new TuiSelectable[,]{{
				path
			},{
				includeIndex
			},{
				export
			}};
		#endif
		
		l = generateMiddleInteractive(t);
		
		if(songs.Length == 1){
			l.Elements.Add(new TuiTwoLabels("Export ", Song.get(songs[0])?.title ?? Song.nullTitle, Placement.TopCenter, 0, 1, null, Palette.song));
		}else{
			l.Elements.Add(new TuiMultipleLabels(new string[]{"Export ", songs.Length + " songs", " from ", source}, Placement.TopCenter, 0, 1, new CharFormat?[]{null, Palette.info, null, sourceStyle}));
		}
		
		l.Elements.Add(new TuiLabel("Folder path:", Placement.TopLeft, 2, 4));
		l.Elements.Add(new TuiLabel("Add index:", Placement.TopCenter, -3, 11));
		
		midSc = new MiddleScreen(l);
		
		setMiddleScreen(midSc);
	}
}