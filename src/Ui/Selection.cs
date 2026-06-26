using AshLib.Lists;
using AshLib.Formatting;
using AshConsoleGraphics;
using AshConsoleGraphics.Interactive;

public partial class Screens{
	public static ReactiveList<int> selection {get;} = new(() => onSelectionChange?.Invoke());
	
	public static event Action onSelectionChange;
	
	void initSelection(){
		selection.Clear();
		onSelectionChange = null; //Remove all subscribed
		
		Song.onSongDeleted -= onSongDel;
		Song.onSongDeleted += onSongDel;
	}
	
	//Private
	void onSongDel(int id){
		if(selection.Contains(id)){
			selection.RemoveAll(s => !Song.exists(s));
		}
	}
	
	void toggleSelected(int id){
		if(selection.Contains(id)){
			Screens.selection.Remove(id);
		}else{
			Screens.selection.Add(id);
		}
	}
	
	void setSelected(int id){
		if(!selection.Contains(id)){
			Screens.selection.Add(id);
		}
	}
	
	void clearSelection(){
		selection.Clear();
	}
	
	void addSelectionToQueue(){
		Session.addMultipleToQueue(selection);
		clearSelection();
	}
	
	void addSelectionToPlaylist(Playlist p = null){
		if(p == null){
			setSelectPlaylistToAddTo(selection.ToArray(), () => selection.Clear());
		}else{
			p.addSongs(selection);
			clearSelection();
		}
	}
	
	void exportSelection(){
		setExport(selection.ToArray(), "selection", Palette.selected, () => clearSelection());
	}
	
	void setSelectionScreen(){
		if(currentMiddleScreen.identifier == "selected"){
			setSelectedScreen(currentMiddleScreen);
			return;
		}
		
		setMiddleScreen(selectionScreen());
	}
	
	MiddleScreen selectionScreen(uint? inex = null){
		List<Song> lib = selection.Select(id => Song.get(id)).Where(s => s != null).ToList();
		
		TuiButton queue = new TuiButton("Add to queue", Placement.TopRight, 0, 0, null, Palette.user).SetAction((s2, ck) => {
			addSelectionToQueue();
		});
		
		TuiButton playlist = new TuiButton("Add to playlist", Placement.TopRight, 0, 2, null, Palette.user).SetAction((s2, ck) => {
			addSelectionToPlaylist();
		});
		
		TuiButton setAuth = new TuiButton("Set authors", Placement.TopRight, 0, 4, null, Palette.user).SetAction((s2, ck) => {
			setChangeAuthors(selection.ToArray(), () => clearSelection());
		});
		
		TuiButton exp = new TuiButton("Export", Placement.TopRight, 0, 6, null, Palette.user).SetAction((s2, ck) => {
			exportSelection();
		});
		
		TuiButton clear = new TuiButton("Clear", Placement.TopRight, 0, 8, null, Palette.user).SetAction((s2, ck) => {
			clearSelection();
		});
		
		TuiScrollingScreenInteractive l = null!;
		
		TuiSelectable[,] t = new TuiSelectable[Math.Max(lib.Count, 5), 2];
		
		t[0, 1] = queue;
		t[1, 1] = playlist;
		t[2, 1] = setAuth;
		t[3, 1] = exp;
		t[4, 1] = clear;
		
		for(int i = 0; i < lib.Count; i++){
			int j = i;
			Song s = lib[i];
			TuiButton b = new TuiSongButton(s?.id ?? -1, Placement.TopLeft, 1, i, Palette.user);
			
			b.SetAction((s2, ck) => {
				setSongDetails(s);
			});
			
			Keybinds.addToQueue.subEvent(b, (s2, ck) => {
				Session.addToQueue(s.id);
			});
			
			Keybinds.play.subEvent(b, (s2, ck) => {
				Radio.py.play(s.id);
			});
			
			Keybinds.addToPlaylist.subEvent(b, (s2, ck) => {
				setSelectPlaylistToAddTo(new int[]{s.id});
			});
			
			Keybinds.listRemove.subEvent(b, (s2, ck) => {
				selection.Remove(s.id);
			});
			
			Keybinds.listUp.subEvent(b, (s2, ck) => {
				if(j != 0){
					TuiScreenInteractive.MoveUp(l, ck);
					selection.Move(j, j - 1);
				}
			});
			
			Keybinds.listDown.subEvent(b, (s2, ck) => {
				if(j != lib.Count - 1){
					TuiScreenInteractive.MoveDown(l, ck);
					selection.Move(j, j + 1);
				}
			});
			
			t[i, 0] = b;
			t[i, 1] = (i % 5) switch{
				0 => queue,
				1 => playlist,
				2 => setAuth,
				3 => exp,
				_ => clear
			};
		}
		
		//Static screen
		TuiScreen backg = generateMiddleStatic();
		
		backg.Elements.Add(new TuiLabel("Selection", Placement.TopCenter, 0, 1, Palette.main));
		
		if(lib.Count == 1){
			backg.Elements.Add(new TuiLabel(lib.Count + " song:", Placement.TopLeft, 2, 3));
		}else if(lib.Count > 0){
			backg.Elements.Add(new TuiLabel(lib.Count + " songs:", Placement.TopLeft, 2, 3));
		}else{
			backg.Elements.Add(new TuiLabel("No songs selected", Placement.TopLeft, 2, 3));
		}
		
		//Inner screen
		l = new TuiScrollingScreenInteractive(Math.Max(backg.Xsize - 6, 0),
			Math.Max(backg.Ysize - 6, 0),
			t, (uint) ((lib?.Count > 0) ? 0 : 1), inex ?? 0,
			Placement.TopLeft, 3, 4,
			null
		);
		
		backg.Elements.Add(l);
		
		prepareScreen(l);
		
		l.OnParentResize += (s, a) => {
			l.Xsize = Math.Max(backg.Xsize - 6, 0);
			l.Ysize = Math.Max(backg.Ysize - 6, 0);
		};
		
		l.FixedElements.Add(queue);
		l.FixedElements.Add(exp);
		
		MiddleScreen c2 = new MiddleScreen(backg, l, "selected");
		
		Keybinds.export.subEvent(c2, true, (s, ck) => {
			setExport(lib.Select(s => s.id).ToArray(), "selection", Palette.selected);
		});
		
		void onSelChange(){
			updateMiddleScreen(c2, () => {
				return selectionScreen(l.MatrixPointerY);
			});
		}
		
		onSelectionChange += onSelChange;
		
		c2.OnDispose = () => {
			onSelectionChange -= onSelChange;
		};
		
		return c2;
	}
	
	void setChangeAuthors(int[] songs, Action? onSuccess = null){
		if(songs == null || songs.Length == 0){
			return;
		}
		
		MiddleScreen sc = null!;
		
		HashSet<int> f = new();
		Song first = Song.get(songs[0]);
		if(first != null){
			foreach(int aid in first.authors){
				f.Add(aid);
			}
		}
		
		bool wrong = false;
		for(int i = 0; i < songs.Length; i++){
			Song g = Song.get(songs[i]);
			if(g == null){
				continue;
			}
			foreach(int aid in g.authors){
				if(f.Add(aid)){
					wrong = true;
					break;
				}
			}
		}
		
		string st = wrong ? "" : string.Join(", ", f.Select(aid => Author.get(aid)?.name ?? Author.nullName));
		
		TuiMultiLineScrollingFramedTextBox input = new TuiMultiLineScrollingFramedTextBox(st, 64, 34, 3, Placement.TopCenter, 0, 4, null, null, null, Palette.writing, Palette.user, Palette.user);
		
		input.OnParentResize += (s, a) => {
			input.BoxXsize = Math.Max(0, a.X - 4);
		};
		
		TuiSelectable[,] t = new TuiSelectable[,]{{
			input
		}};
		
		sc = generateMiddle(t);
		
		Keybinds.enter.subEvent(sc, "Set authors", (_, _) => {
			string[] aps = input.Text.Split(',');
			int[] auts = Author.getAuthors(aps);
			
			foreach(int sid in songs){
				Song s = Song.get(sid);
				if(s == null){
					continue;
				}
				
				s.setAuthors(auts);
			}
			onSuccess?.Invoke();
			closeMiddleScreen();
		});
		
		if(songs.Length == 1){
			sc.interactive.Elements.Add(new TuiTwoLabels("Set authors of ", Song.get(songs[0])?.title ?? Song.nullTitle, Placement.TopCenter, 0, 1, null, Palette.song));
		}else{
			sc.interactive.Elements.Add(new TuiTwoLabels("Set authors of ", songs.Length + " songs", Placement.TopCenter, 0, 1, null, Palette.info));
		}
		
		sc.interactive.Elements.Add(new TuiLabel("Separated by commas", Placement.TopLeft, 2, 9, null));
		
		setMiddleScreen(sc);
	}
}