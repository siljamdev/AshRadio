using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using AshLib.Formatting;
using AshConsoleGraphics;
using AshConsoleGraphics.Interactive;

public partial class Screens{
	void setSongDetails(Song s){
		setMiddleScreen(songDetails(s));
	}
	
	public void setSongDetails(int s){
		setMiddleScreen(songDetails(Song.get(s)));
	}
	
	MiddleScreen songDetails(Song s){
		MiddleScreen c2 = null;
		
		TuiFramedScrollingTextBox titleInput = new TuiFramedScrollingTextBox(s?.title ?? Song.nullTitle, 256, 16, Placement.TopRight, 3, 4, null, null, null, Palette.writing, Palette.user, Palette.user);
		
		Keybinds.enter.subEvent(titleInput, (s2, ck) => {
			s?.setTitle(titleInput.Text);
		});
		
		titleInput.OnParentResize += (s, a) => {
			titleInput.BoxXsize = Math.Clamp(a.X - 32, 16, 38);
		};
		
		TuiFramedScrollingTextBox authorsInput = new TuiFramedScrollingTextBox(s?.authors == null ? "" : (s.authors.Length == 0 ? "" : (s.authors.Length == 1 ? (Author.get(s.authors[0])?.name ?? Author.nullName) : string.Join(", ", s.authors.Select(n => (Author.get(n)?.name ?? Author.nullName))))),
			64, 16, Placement.TopRight, 3, 10, null, null, null, Palette.writing, Palette.user, Palette.user);
		
		Keybinds.enter.subEvent(authorsInput, (s2, ck) => {
			if(s != null){
				string[] aps = authorsInput.Text.Split(',');
				int[] auts = Author.getAuthors(aps);
				s.setAuthors(auts);
			}
		});
		
		authorsInput.OnParentResize += (s, a) => {
			authorsInput.BoxXsize = Math.Clamp(a.X - 32, 16, 38);
		};
		
		TuiButton addPlaylist = new TuiButton("Add to playlist", Placement.TopRight, 5, 14, null, Palette.user).SetAction((s2, ck) => {
			if(s == null){
				return;
			}
			
			setSelectPlaylistToAddTo(new int[]{s.id});
		});
		
		TuiButton exp = new TuiButton("Export song", Placement.TopRight, 5, 15, null, Palette.user).SetAction((s2, ck) => {
			if(s == null){
				return;
			}
			
			setExport(new int[]{s.id}, "", null);
		});
		
		TuiButton notes = new TuiButton("Edit notes", Placement.TopRight, 5, 16, null, Palette.user).SetAction((s2, ck) => {
			if(s == null){
				return;
			}
			
			setNotes(s);
		});
		
		TuiButton del = new TuiButton("Delete song", Placement.TopRight, 5, 18, null, Palette.user).SetAction((s2, ck) => {
			if(s == null){
				return;
			}
			
			setConfirmScreen("Do you want to delete this song?", () => {
				Song.delete(s.id);
			});
		});
		
		//Selectables
		TuiSelectable[,] temp = new TuiSelectable[Math.Max((s?.authors.Length ?? 0) + 1, 6), 2];
		
		temp[0, 1] = titleInput;
		temp[1, 1] = authorsInput;
		temp[2, 1] = addPlaylist;
		temp[3, 1] = exp;
		temp[4, 1] = notes;
		temp[5, 1] = del;
		
		//Add authors
		if(s?.authors != null){
			for(int i = 0; i < s.authors.Length; i++){
				int tt3 = s.authors[i];
				TuiButton ar = new TuiButton(Author.get(tt3)?.name ?? Author.nullName, Placement.TopLeft, 4, 9 + i, Palette.author, Palette.user).SetAction((s2, ck) => {
					setAuthorDetails(tt3);
				});
				
				Keybinds.setSource.subEvent(ar, (s, ck) => {
					Session.setSource(SourceType.Author, tt3);
				});
				
				temp[i + 1, 0] = ar;
			}
		}
		
		//screen stuff
		c2 = generateMiddle(temp);
		TuiScreenInteractive c = c2.interactive;
		
		c.MatrixPointerX = (uint) ((s?.authors.Length > 0) ? 0 : 1);
		c.MatrixPointerY = (uint) ((s?.authors.Length > 0) ? 1 : 2);
		
		TuiLabel title = new TuiLabel(s?.title ?? Song.nullTitle, Placement.TopLeft, 2, 2, selection.Contains(s?.id ?? -1) ? Palette.selected : Palette.song);
		
		c.Elements.Add(title);
		c.Elements.Add(new TuiLabel("Song", Placement.TopLeft, 4, 3));
		c.Elements.Add(new TuiLabel("Set title:", Placement.TopRight, 14, 3));
		c.Elements.Add(new TuiLabel("Set authors:", Placement.TopRight, 12, 8));
		c.Elements.Add(new TuiLabel("(separated by commas)", Placement.TopRight, 3, 9));
		
		TuiLabel playingNow = new TuiLabel("Playing now", Placement.TopLeft, 12, 3, Palette.main);
		
		if(Radio.py.playingSong == s?.id){
			c.Elements.Add(playingNow);
		}
		
		c.Elements.Add(new TuiLabel(secondsToMinuteTime(s.duration), Placement.TopLeft, 4, 5, Palette.info));
		c.Elements.Add(new TuiLabel(s.added.ToString(), Placement.TopLeft, 4, 6, Palette.info));
		
		if(s?.authors != null){
			if(s.authors.Length == 1){
				c.Elements.Add(new TuiLabel("Author", Placement.TopLeft, 2, 8));
			}else if(s.authors.Length > 1){
				c.Elements.Add(new TuiLabel("Authors", Placement.TopLeft, 2, 8));
			}else{
				c.Elements.Add(new TuiLabel("Unknown authors", Placement.TopLeft, 2, 8));
			}
		}
		
		Keybinds.addToQueue.subEvent(c2, true, (s2, ck) => {
			if(s == null){
				return;
			}
			
			Session.addToQueue(s.id);
		});
		
		Keybinds.play.subEvent(c2, true, (s2, ck) => {
			if(s == null){
				return;
			}
			
			Radio.py.play(s.id);
		});
		
		Keybinds.addToPlaylist.subEvent(c2, true, (s2, ck) => {
			if(s == null){
				return;
			}
			
			setSelectPlaylistToAddTo(new int[]{s.id});
		});
		
		Keybinds.export.subEvent(c2, true, (s2, ck) => {
			if(s == null){
				return;
			}
			
			setExport(new int[]{s.id}, "", null);
		});
		
		Keybinds.select.subEvent(c2, "Select/Unselect", (s2, ck) => {
			if(s == null){
				return;
			}
			
			toggleSelected(s.id);
		});
		
		void onSongChange(int sender){
			if(sender != s.id){
				return;
			}
			
			updateMiddleScreen(c2, () => {
				if(Song.exists(s.id)){
					return songDetails(Song.get(s.id));
				}
				
				return null;
			});
		}
		
		Song.onSongDetailsUpdate += onSongChange;
		
		void onAuthorChange(int sender){
			if(!(s?.authors?.Contains(sender) ?? false)){
				return;
			}
			
			updateMiddleScreen(c2, () => {
				return songDetails(Song.get(s.id));
			});
		}
		
		Author.onAuthorNameUpdate += onAuthorChange;
		Author.onAuthorDeleted += onAuthorChange;
		
		void onSongLoaded(){
			if(Radio.py.playingSong == s?.id){
				if(!c.Elements.Contains(playingNow)){
					c.Elements.Add(playingNow);
				}
			}else{
				if(c.Elements.Contains(playingNow)){
					c.Elements.Remove(playingNow);
				}
			}
		}
		
		Radio.py.onSongLoad += onSongLoaded;
		
		void onSelectionChanged(){
			bool n = Screens.selection.Contains(s?.id ?? -1);
			if(n != (title.Format == Palette.selected)){
				title.Format = n ? Palette.selected : Palette.song;
			}
		}
		
		onSelectionChange += onSelectionChanged;
		
		c2.OnDispose = () => {
			Song.onSongDetailsUpdate -= onSongChange;
			Author.onAuthorNameUpdate -= onAuthorChange;
			Author.onAuthorDeleted -= onAuthorChange;
			Radio.py.onSongLoad -= onSongLoaded;
			onSelectionChange -= onSelectionChanged;
		};
		
		return c2;
	}
	
	void setLibrary(string query = null){
		if(currentMiddleScreen.identifier == "library"){
			setSelectedScreen(currentMiddleScreen);
			return;
		}
		
		setMiddleScreen(library(query));
	}
	
	MiddleScreen library(string query = null, uint? inex = null){
		if(string.IsNullOrWhiteSpace(query)){
			query = null;
		}else{
			query = query.Trim();
		}
		
		List<Song> lib = query == null ? Song.getLibrary() : Song.getLibrary().Where(n => n.title.Contains(query, StringComparison.OrdinalIgnoreCase)).ToList();
		
		TuiButton import = new TuiButton("Import songs", Placement.TopRight, 0, 0, null, Palette.user);
		
		import.SetAction((s, ck) => {
			setImport();
		});
		
		TuiButton exp = new TuiButton("Export library", Placement.TopRight, 0, 2, null, Palette.user).SetAction((s2, ck) => {
			setExport(lib.Select(s => s.id).ToArray(), "library", Palette.info);
		});
		
		TuiSelectable[,] t = new TuiSelectable[Math.Max(lib.Count, 2), query == null ? 2 : 1];
		
		if(query == null){
			t[0, 1] = import;
			t[1, 1] = exp;
		}
		
		for(int i = 0; i < lib.Count; i++){
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
			
			t[i, 0] = b;
			if(query == null){
				t[i, 1] = (i % 2) switch{
					0 => import,
					_ => exp
				};
			}
		}
		
		//Static screen
		TuiScreen backg = generateMiddleStatic();
		
		backg.Elements.Add(new TuiLabel("Library", Placement.TopCenter, 0, 1, Palette.main));
		if(query != null){
			backg.Elements.Add(new TuiTwoLabels("Search results for: ", query, Placement.TopCenter, 0, 2, null, Palette.info));
		}
		
		if(lib.Count == 1){
			backg.Elements.Add(new TuiLabel(lib.Count + " song:", Placement.TopLeft, 2, 3));
		}else if(lib.Count > 0){
			backg.Elements.Add(new TuiLabel(lib.Count + " songs:", Placement.TopLeft, 2, 3));
		}else{
			backg.Elements.Add(new TuiLabel("No songs found", Placement.TopLeft, 2, 3));
		}
		
		//Inner screen
		TuiScrollingScreenInteractive l = new TuiScrollingScreenInteractive(Math.Max(backg.Xsize - 6, 0),
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
		
		if(query == null){
			l.FixedElements.Add(import);
			l.FixedElements.Add(exp);
		}
		
		MiddleScreen c2 = new MiddleScreen(backg, l, "library");
		
		if(query == null){
			Keybinds.setSource.subEvent(c2, true, (s, ck) => {
				Session.setSource(SourceType.Library);
			});
			
			Keybinds.export.subEvent(c2, true, (s, ck) => {
				setExport(lib.Select(s => s.id).ToArray(), "library", Palette.info);
			});
		}
		
		Keybinds.search.subEvent(c2, true, (s, ck) => {
			setSearchScreen("Search song in library:", s => setLibrary(s));
			
			if(query != null){
				removeMiddleScreen(c2);
			}
		});
		
		Keybinds.selectAll.subEvent(c2, "Select all", (s, ck) => {
			lib.ForEach(s => setSelected(s.id));
		});
		
		void onLibChange(){
			updateMiddleScreen(c2, () => {
				return library(query, l.MatrixPointerY);
			});
		}
		
		Song.onLibraryUpdate += onLibChange;
		
		c2.OnDispose = () => {
			Song.onLibraryUpdate -= onLibChange;
		};
		
		return c2;
	}
	
	void setAuthorDetails(Author s){
		setMiddleScreen(authorDetails(s));
	}
	
	void setAuthorDetails(int s){
		setMiddleScreen(authorDetails(Author.get(s)));
	}
	
	MiddleScreen authorDetails(Author s, uint? inex = null){
		TuiFramedScrollingTextBox name = new TuiFramedScrollingTextBox(s?.name ?? Author.nullName, 256, 16, Placement.TopRight, 1, 1, null, null, null, Palette.writing, Palette.user, Palette.user);
		
		Keybinds.enter.subEvent(name, (s2, ck) => {
			s?.setName(name.Text);
		});
		
		name.OnParentResize += (s, a) => {
			name.BoxXsize = Math.Clamp(a.X - 32, 16, 38);
		};
		
		List<Song> songs = s?.getSongs();
		
		TuiButton export = new TuiButton("Export songs", Placement.TopRight, 2, 5, null, Palette.user).SetAction((s2, ck) => {
			if(s == null){
				return;
			}
			
			setExport(songs.Select(s => s.id).ToArray(), s.name, Palette.author);
		});
		
		TuiButton notes = new TuiButton("Edit notes", Placement.TopRight, 2, 6, null, Palette.user).SetAction((s2, ck) => {
			if(s == null){
				return;
			}
			
			setNotes(s);
		});
		
		TuiButton del = new TuiButton("Delete author", Placement.TopRight, 2, 8, null, Palette.user).SetAction((s2, ck) => {
			if(s == null){
				return;
			}
			
			setConfirmScreen("Do you want to delete this author?", () => {
				Author.delete(s.id);
			});
		});
		
		TuiLabel lab = new TuiLabel("Set name:", Placement.TopRight, 12, 0);
		
		TuiSelectable[,] temp = new TuiSelectable[Math.Max(songs?.Count ?? 0, 4), 2];
		
		temp[0, 1] = name;
		temp[1, 1] = export;
		temp[2, 1] = notes;
		temp[3, 1] = del;
		
		if(songs != null){
			for(int i = 0; i < songs.Count; i++){
				Song ttt3 = songs[i];
				
				TuiButton b = new TuiSongButton(ttt3?.id ?? -1, Placement.TopLeft, 0, i, Palette.user);
				
				b.SetAction((s, ck) => {
					setSongDetails(ttt3);
				});
				
				Keybinds.addToQueue.subEvent(b, (s2, ck) => {
					Session.addToQueue(ttt3.id);
				});
				
				Keybinds.play.subEvent(b, (s2, ck) => {
					Radio.py.play(ttt3.id);
				});
				
				Keybinds.addToPlaylist.subEvent(b, (s2, ck) => {
					setSelectPlaylistToAddTo(new int[]{ttt3.id});
				});
				
				temp[i, 0] = b;
				temp[i, 1] = (i % 4) switch{
					0 => name,
					1 => export,
					2 => notes,
					_ => del
				};
			}
		}
		
		//Static screen
		TuiScreen backg = generateMiddleStatic();
		
		backg.Elements.Add(new TuiLabel(s?.name ?? Author.nullName, Placement.TopLeft, 2, 2, Palette.author));
		backg.Elements.Add(new TuiLabel("Author", Placement.TopLeft, 4, 3));
		
		if(songs != null){
			if(songs.Count == 1){
				backg.Elements.Add(new TuiLabel(songs.Count + " song:", Placement.TopLeft, 2, 5));
			}else if(songs.Count > 0){
				backg.Elements.Add(new TuiLabel(songs.Count + " songs:", Placement.TopLeft, 2, 5));
			}else{
				backg.Elements.Add(new TuiLabel("No attributed songs", Placement.TopLeft, 2, 5));
			}
		}else{
			backg.Elements.Add(new TuiLabel("No attributed songs", Placement.TopLeft, 2, 5));
		}
		
		//Inner screen
		TuiScrollingScreenInteractive l = new TuiScrollingScreenInteractive(Math.Max(backg.Xsize - 7, 0),
			Math.Max(backg.Ysize - 8, 0),
			temp, (uint) ((songs?.Count > 0) ? 0 : 1), inex ?? 0,
			Placement.TopLeft, 4, 6,
			null,
			lab
		);
		
		backg.Elements.Add(l);
		
		prepareScreen(l);
		
		l.OnParentResize += (s, a) => {
			l.Xsize = Math.Max(backg.Xsize - 7, 0);
			l.Ysize = Math.Max(backg.Ysize - 8, 0);
		};
		
		l.FixedElements.Add(lab);
		l.FixedElements.Add(name);
		l.FixedElements.Add(export);
		l.FixedElements.Add(notes);
		l.FixedElements.Add(del);
		
		MiddleScreen c2 = new MiddleScreen(backg, l);
		
		Keybinds.setSource.subEvent(c2, true, (sc, ck) => { //Set source
			if(s == null){
				return;
			}
			
			Session.setSource(SourceType.Author, s.id);
		});
		
		Keybinds.export.subEvent(c2, true, (sc, ck) => {
			if(s == null){
				return;
			}
			
			setExport(songs.Select(s => s.id).ToArray(), s.name, Palette.author);
		});
		
		Keybinds.selectAll.subEvent(c2, "Select all", (s, ck) => {
			songs?.ForEach(s => setSelected(s.id));
		});
		
		void onAuthorChange(int sender){
			if(s.id != sender){
				return;
			}
			
			updateMiddleScreen(c2, () => {
				if(Author.exists(s.id)){
					return authorDetails(Author.get(s?.id ?? -1), l.MatrixPointerY);
				}
				
				return null;
			});
		}
		
		Author.onAuthorDetailsUpdate += onAuthorChange;
		
		c2.OnDispose = () => {
			Author.onAuthorDetailsUpdate -= onAuthorChange;
		};
		
		return c2;
	}
	
	void setAuthors(string query = null){
		if(currentMiddleScreen.identifier == "authors"){
			setSelectedScreen(currentMiddleScreen);
			return;
		}
		
		setMiddleScreen(authors(query));
	}
	
	MiddleScreen authors(string query = null, uint? inex = null){
		if(string.IsNullOrWhiteSpace(query)){
			query = null;
		}else{
			query = query.Trim();
		}
		
		List<Author> lib = query == null ? Author.getAllAuthors() : Author.getAllAuthors().Where(n => n != null && n.name.Contains(query, StringComparison.OrdinalIgnoreCase)).ToList();
		
		TuiSelectable[,] t = new TuiSelectable[Math.Max(lib.Count, 1), 1];
		
		for(int i = 0; i < lib.Count; i++){
			Author s = lib[i];
			TuiButton b = new TuiButton(s?.name ?? Author.nullName, Placement.TopLeft, 1, i, Palette.author, Palette.user);
			
			b.SetAction((s2, ck) => {
				setAuthorDetails(s);
			});
			
			Keybinds.setSource.subEvent(b, (s2, ck) => {
				Session.setSource(SourceType.Author, s.id);
			});
			
			Keybinds.export.subEvent(b, (s2, ck) => {
				setExport(s?.getSongsIds().ToArray(), s?.name ?? Author.nullName, Palette.author);
			});
			
			t[i, 0] = b;
		}
		
		//Static screen
		TuiScreen backg = generateMiddleStatic();
		
		backg.Elements.Add(new TuiLabel("Authors", Placement.TopCenter, 0, 1, Palette.main));
		if(query != null){
			backg.Elements.Add(new TuiTwoLabels("Search results for: ", query, Placement.TopCenter, 0, 2, null, Palette.info));
		}
		
		if(lib.Count == 1){
			backg.Elements.Add(new TuiLabel(lib.Count + " author:", Placement.TopLeft, 2, 3));
		}else if(lib.Count > 0){
			backg.Elements.Add(new TuiLabel(lib.Count + " authors:", Placement.TopLeft, 2, 3));
		}else{
			backg.Elements.Add(new TuiLabel("No authors found", Placement.TopLeft, 2, 3));
		}
		
		//Inner screen
		TuiScrollingScreenInteractive l = new TuiScrollingScreenInteractive(Math.Max(backg.Xsize - 6, 0),
			Math.Max(backg.Ysize - 6, 0),
			t, 0, inex ?? 0,
			Placement.TopLeft, 3, 4,
			null
		);
		
		backg.Elements.Add(l);
		
		prepareScreen(l);
		
		l.OnParentResize += (s, a) => {
			l.Xsize = Math.Max(backg.Xsize - 6, 0);
			l.Ysize = Math.Max(backg.Ysize - 6, 0);
		};
		
		MiddleScreen c2 = new MiddleScreen(backg, l, "authors");
		
		Keybinds.search.subEvent(c2, true, (s, ck) => {
			setSearchScreen("Search authors:", s => setAuthors(s));
			
			if(query != null){
				removeMiddleScreen(c2);
			}
		});
		
		void onAuthorsChange(){
			updateMiddleScreen(c2, () => {
				return authors(query, l.MatrixPointerY);
			});
		}
		
		Author.onAuthorsUpdate += onAuthorsChange;
		
		void onAuthorChange(int sender){
			if(!lib.Any(a => a.id == sender)){
				return;
			}
			
			updateMiddleScreen(c2, () => {
				return authors(query, l.MatrixPointerY);
			});
		}
		
		Author.onAuthorNameUpdate += onAuthorChange;
		
		c2.OnDispose = () => {
			Author.onAuthorsUpdate -= onAuthorsChange;
			Author.onAuthorNameUpdate -= onAuthorChange;
		};
		
		return c2;
	}
	
	void setPlaylistDetails(Playlist s){
		setMiddleScreen(playlistDetails(s));
	}
	
	void setPlaylistDetails(int s){
		setMiddleScreen(playlistDetails(Playlist.get(s)));
	}
	
	MiddleScreen playlistDetails(Playlist s, uint? inex = null){
		TuiFramedScrollingTextBox name = new TuiFramedScrollingTextBox(s?.title ?? Playlist.nullTitle, 256, 16, Placement.TopRight, 1, 1, null, null, null, Palette.writing, Palette.user, Palette.user);
		
		Keybinds.enter.subEvent(name, (s2, ck) => {
			s?.setTitle(name.Text);
		});
		
		name.OnParentResize += (s, a) => {
			name.BoxXsize = Math.Clamp(a.X - 32, 16, 38);
		};
		
		TuiButton add = new TuiButton("Add song", Placement.TopRight, 2, 5, null, Palette.user).SetAction((s2, ck) => {
			if(s == null){
				return;
			}
			
			setSelectSongToAdd(s);
		});
		
		TuiButton removeSel = new TuiButton("Remove selected", Placement.TopRight, 2, 6, null, Palette.user).SetAction((s2, ck) => {
			if(s == null){
				return;
			}
			
			if(s.deleteSongs(selection)){
				clearSelection();
			}
		});
		
		List<Song> songs = s?.getSongs();
		
		TuiButton exp = new TuiButton("Export", Placement.TopRight, 2, 7, null, Palette.user).SetAction((s2, ck) => {
			if(s == null){
				return;
			}
			
			setExport(songs.Select(s => s.id).ToArray(), s.title, Palette.playlist);
		});
		
		TuiButton notes = new TuiButton("Edit notes", Placement.TopRight, 2, 8, null, Palette.user).SetAction((s2, ck) => {
			if(s == null){
				return;
			}
			
			setNotes(s);
		});
		
		TuiButton del = new TuiButton("Delete playlist", Placement.TopRight, 2, 10, null, Palette.user).SetAction((s2, ck) => {
			if(s == null){
				return;
			}
			
			setConfirmScreen("Do you want to delete this playlist?", () => {
				Playlist.delete(s.id);
			});
		});
		
		TuiLabel lab = new TuiLabel("Set title:", Placement.TopRight, 11, 0);
		
		TuiScrollingScreenInteractive l = null!;
		
		TuiSelectable[,] temp = new TuiSelectable[Math.Max(songs?.Count ?? 0, 6), 2];
		
		temp[0, 1] = name;
		temp[1, 1] = add;
		temp[2, 1] = removeSel;
		temp[3, 1] = exp;
		temp[4, 1] = notes;
		temp[5, 1] = del;
		
		if(songs != null){
			for(int i = 0; i < songs.Count; i++){
				Song ttt3 = songs[i];
				
				int j = i;
				
				TuiButton b = new TuiSongButton(ttt3?.id ?? -1, Placement.TopLeft, 0, i, Palette.user);
				
				b.SetAction((s, ck) => {
					setSongDetails(ttt3);
				});
				
				Keybinds.addToQueue.subEvent(b, (s2, ck) => {
					Session.addToQueue(ttt3.id);
				});
				
				Keybinds.play.subEvent(b, (s2, ck) => {
					Radio.py.play(ttt3.id);
				});
				
				Keybinds.addToPlaylist.subEvent(b, (s2, ck) => {
					setSelectPlaylistToAddTo(new int[]{ttt3.id});
				});
				
				Keybinds.listRemove.subEvent(b, (s2, ck) => {
					s?.deleteSongAt(j);
				});
				
				Keybinds.listUp.subEvent(b, (s2, ck) => {
					if(j != 0){
						TuiScreenInteractive.MoveUp(l, ck);
						s?.moveSong(j, j - 1);
					}
				});
				
				Keybinds.listDown.subEvent(b, (s2, ck) => {
					if(j != songs.Count - 1){
						TuiScreenInteractive.MoveDown(l, ck);
						s?.moveSong(j, j + 1);
					}
				});
				
				temp[i, 0] = b;
				temp[i, 1] = (i % 6) switch{
					0 => name,
					1 => add,
					2 => removeSel,
					3 => exp,
					4 => notes,
					_ => del
				};
			}
		}
		
		//Static screen
		TuiScreen backg = generateMiddleStatic();
		
		backg.Elements.Add(new TuiLabel(s?.title ?? Playlist.nullTitle, Placement.TopLeft, 2, 2, Palette.playlist));
		backg.Elements.Add(new TuiLabel("Playlist", Placement.TopLeft, 4, 3));
		
		if(songs != null){
			if(songs.Count == 1){
				backg.Elements.Add(new TuiLabel(songs.Count + " song:", Placement.TopLeft, 2, 5));
			}else if(songs.Count > 0){
				backg.Elements.Add(new TuiLabel(songs.Count + " songs:", Placement.TopLeft, 2, 5));
			}else{
				backg.Elements.Add(new TuiLabel("No songs", Placement.TopLeft, 2, 5));
			}
		}else{
			backg.Elements.Add(new TuiLabel("No songs", Placement.TopLeft, 2, 5));
		}
		
		//Inner screen
		l = new TuiScrollingScreenInteractive(Math.Max(backg.Xsize - 7, 0),
			Math.Max(backg.Ysize - 8, 0),
			temp, (uint) ((songs?.Count > 0) ? 0 : 1), inex ?? 0,
			Placement.TopLeft, 4, 6,
			null,
			lab
		);
		
		backg.Elements.Add(l);
		
		prepareScreen(l);
		
		l.OnParentResize += (s, a) => {
			l.Xsize = Math.Max(backg.Xsize - 7, 0);
			l.Ysize = Math.Max(backg.Ysize - 8, 0);
		};
		
		l.FixedElements.Add(lab);
		l.FixedElements.Add(name);
		l.FixedElements.Add(add);
		l.FixedElements.Add(removeSel);
		l.FixedElements.Add(del);
		l.FixedElements.Add(notes);
		l.FixedElements.Add(exp);
		
		MiddleScreen c2 = new MiddleScreen(backg, l);
		
		Keybinds.setSource.subEvent(c2, true, (sc, ck) => {
			if(s == null){
				return;
			}
			
			Session.setSource(SourceType.Playlist, s.id);
		});
		
		Keybinds.export.subEvent(c2, true, (sc, ck) => {
			if(s == null){
				return;
			}
			
			setExport(songs.Select(s => s.id).ToArray(), s.title, Palette.playlist);
		});
		
		Keybinds.selectionAddToPlaylist.subEvent(c2, "Add selection", (sc, ck) => {
			if(s == null){
				return;
			}
			
			addSelectionToPlaylist(s);
		});
		
		Keybinds.selectAll.subEvent(c2, "Select all", (s, ck) => {
			songs?.ForEach(s => setSelected(s.id));
		});
		
		void onPlaylistChange(int sender){
			if(sender != s.id){
				return;
			}
			
			updateMiddleScreen(c2, () => {
				if(Playlist.exists(s.id)){
					return playlistDetails(Playlist.get(s?.id ?? -1), l.MatrixPointerY);
				}
				
				return null;
			});
		}
		
		Playlist.onPlaylistDetailsUpdate += onPlaylistChange;
		
		c2.OnDispose = () => {
			Playlist.onPlaylistDetailsUpdate -= onPlaylistChange;
		};
		
		return c2;
	}
	
	void setPlaylists(string query = null){
		if(currentMiddleScreen.identifier == "playlists"){
			setSelectedScreen(currentMiddleScreen);
			return;
		}
		
		setMiddleScreen(playlists(query));
	}
	
	MiddleScreen playlists(string query = null, uint? inex = null){
		if(string.IsNullOrWhiteSpace(query)){
			query = null;
		}else{
			query = query.Trim();
		}
		
		List<Playlist> lib = query == null ? Playlist.getAllPlaylists() : Playlist.getAllPlaylists().Where(n => n != null && n.title.Contains(query, StringComparison.OrdinalIgnoreCase)).ToList();
		
		TuiButton create = new TuiButton("Create playlist", Placement.TopRight, 0, 0, null, Palette.user);
		
		create.SetAction((s, ck) => {
			setPlaylistDetails(Playlist.create("New playlist #${id}"));
		});
		
		TuiButton import = new TuiButton("Import from folder", Placement.TopRight, 0, 2, null, Palette.user);
		TuiButton importYt = new TuiButton("Import from yt", Placement.TopRight, 0, 4, null, Palette.user);
		
		import.SetAction((s, ck) => {
			setImportFolderPlaylist();
		});
		
		importYt.SetAction((s, ck) => {
			setImportPlaylist();
		});
		
		TuiSelectable[,] t = new TuiSelectable[Math.Max(lib.Count, 3), query == null ? 2 : 1];
		
		if(query == null){
			t[0, 1] = create;
			t[1, 1] = import;
			t[2, 1] = importYt;
		}
		
		for(int i = 0; i < lib.Count; i++){
			Playlist s = lib[i];
			TuiButton b = new TuiButton(s?.title ?? Playlist.nullTitle, Placement.TopLeft, 1, i, Palette.playlist, Palette.user);
			
			b.SetAction((s2, ck) => {
				setPlaylistDetails(s);
			});
			
			Keybinds.setSource.subEvent(b, (s2, ck) => {
				Session.setSource(SourceType.Playlist, s.id);
			});
			
			Keybinds.export.subEvent(b, (s2, ck) => {
				setExport(s?.getSongsIds().ToArray(), s?.title ?? Playlist.nullTitle, Palette.playlist);
			});
			
			t[i, 0] = b;
			if(query == null){
				t[i, 1] = (i % 3) switch{
					0 => create,
					1 => import,
					_ => importYt
				};
			}
		}
		
		//Static screen
		TuiScreen backg = generateMiddleStatic();
		
		backg.Elements.Add(new TuiLabel("Playlists", Placement.TopCenter, 0, 1, Palette.main));
		if(query != null){
			backg.Elements.Add(new TuiTwoLabels("Search results for: ", query, Placement.TopCenter, 0, 2, null, Palette.info));
		}
		
		if(lib.Count == 1){
			backg.Elements.Add(new TuiLabel(lib.Count + " playlist:", Placement.TopLeft, 2, 3));
		}else if(lib.Count > 0){
			backg.Elements.Add(new TuiLabel(lib.Count + " playlists:", Placement.TopLeft, 2, 3));
		}else{
			backg.Elements.Add(new TuiLabel("No playlists found", Placement.TopLeft, 2, 3));
		}
		
		//Inner screen
		TuiScrollingScreenInteractive l = new TuiScrollingScreenInteractive(Math.Max(backg.Xsize - 6, 0),
			Math.Max(backg.Ysize - 6, 0),
			t, 0, inex ?? 0,
			Placement.TopLeft, 3, 4,
			null
		);
		
		backg.Elements.Add(l);
		
		prepareScreen(l);
		
		l.OnParentResize += (s, a) => {
			l.Xsize = Math.Max(backg.Xsize - 6, 0);
			l.Ysize = Math.Max(backg.Ysize - 6, 0);
		};
		
		if(query == null){
			l.FixedElements.Add(create);
			l.FixedElements.Add(import);
			l.FixedElements.Add(importYt);
		}
		
		MiddleScreen c2 = new MiddleScreen(backg, l, "playlists");
		
		Keybinds.search.subEvent(c2, true, (s, ck) => {
			setSearchScreen("Search playlists:", s => setPlaylists(s));
			
			if(query != null){
				removeMiddleScreen(c2);
			}
		});
		
		void onPlaylistsChange(){
			updateMiddleScreen(c2, () => {
				return playlists(query, l.MatrixPointerY);
			});
		}
		
		Playlist.onPlaylistsUpdate += onPlaylistsChange;
		
		void onPlaylistChange(int sender){
			if(!lib.Any(p => p.id == sender)){
				return;
			}
			
			updateMiddleScreen(c2, () => {
				return playlists(query, l.MatrixPointerY);
			});
		}
		
		Playlist.onPlaylistTitleUpdate += onPlaylistChange;
		
		c2.OnDispose = () => {
			Playlist.onPlaylistsUpdate -= onPlaylistsChange;
			Playlist.onPlaylistTitleUpdate -= onPlaylistChange;
		};
		
		return c2;
	}
	
	//Select to add
	
	void setSelectPlaylistToAddTo(int[] sindexes, Action? onSuccess = null, string query = null){
		if(sindexes == null || sindexes.Length == 0){
			return;
		}
		
		setMiddleScreen(selectPlaylistToAddTo(sindexes, onSuccess, query));
	}
	
	MiddleScreen selectPlaylistToAddTo(int[] sindexes, Action? onSuccess = null, string query = null, uint? inex = null){
		if(sindexes == null || sindexes.Length == 0){
			return null;
		}
		
		if(string.IsNullOrWhiteSpace(query)){
			query = null;
		}else{
			query = query.Trim();
		}
		
		List<Playlist> lib = query == null ? Playlist.getAllPlaylists() : Playlist.getAllPlaylists().Where(n => n != null && n.title.Contains(query, StringComparison.OrdinalIgnoreCase)).ToList();
		
		TuiSelectable[,] t = new TuiSelectable[Math.Max(lib.Count, 3), 1];
		
		for(int i = 0; i < lib.Count; i++){
			Playlist s = lib[i];
			TuiButton b = new TuiButton(s?.title ?? Playlist.nullTitle, Placement.TopLeft, 1, i, Palette.playlist, Palette.user);
			
			b.SetAction((s2, ck) => {
				s.addSongs(sindexes);
				onSuccess?.Invoke();
				closeMiddleScreen();
			});
			
			t[i, 0] = b;
		}
		
		//Static screen
		TuiScreen backg = generateMiddleStatic();
		
		if(sindexes.Length == 1){
			backg.Elements.Add(new TuiTwoLabels("Select playlist where to add ", Song.get(sindexes[0])?.title ?? Song.nullTitle, Placement.TopCenter, 0, 1, null, Palette.song));
		}else{
			backg.Elements.Add(new TuiTwoLabels("Select playlist where to add ", sindexes.Length + " songs", Placement.TopCenter, 0, 1, null, Palette.info));
		}
		
		if(query != null){
			backg.Elements.Add(new TuiTwoLabels("Search results for: ", query, Placement.TopCenter, 0, 2, null, Palette.info));
		}
		
		if(lib.Count == 1){
			backg.Elements.Add(new TuiLabel(lib.Count + " playlist:", Placement.TopLeft, 2, 3));
		}else if(lib.Count > 0){
			backg.Elements.Add(new TuiLabel(lib.Count + " playlists:", Placement.TopLeft, 2, 3));
		}else{
			backg.Elements.Add(new TuiLabel("No playlists found", Placement.TopLeft, 2, 3));
		}
		
		//Inner screen
		TuiScrollingScreenInteractive l = new TuiScrollingScreenInteractive(Math.Max(backg.Xsize - 6, 0),
			Math.Max(backg.Ysize - 6, 0),
			t, 0, inex ?? 0,
			Placement.TopLeft, 3, 4,
			null
		);
		
		backg.Elements.Add(l);
		
		prepareScreen(l);
		
		l.OnParentResize += (s, a) => {
			l.Xsize = Math.Max(backg.Xsize - 6, 0);
			l.Ysize = Math.Max(backg.Ysize - 6, 0);
		};
		
		MiddleScreen c2 = new MiddleScreen(backg, l);
		
		Keybinds.search.subEvent(c2, true, (s, ck) => {
			setSearchScreen("Search playlists:", s => setSelectPlaylistToAddTo(sindexes, onSuccess, s));
			
			removeMiddleScreen(c2);
		});
		
		void onPlaylistsChange(){
			updateMiddleScreen(c2, () => {
				return selectPlaylistToAddTo(sindexes, onSuccess, query, l.MatrixPointerY);
			});
		}
		
		Playlist.onPlaylistsUpdate += onPlaylistsChange;
		
		void onPlaylistChange(int sender){
			if(!lib.Any(p => p.id == sender)){
				return;
			}
			
			updateMiddleScreen(c2, () => {
				return selectPlaylistToAddTo(sindexes, onSuccess, query, l.MatrixPointerY);
			});
		}
		
		Playlist.onPlaylistTitleUpdate += onPlaylistChange;
		
		c2.OnDispose = () => {
			Playlist.onPlaylistsUpdate -= onPlaylistsChange;
			Playlist.onPlaylistTitleUpdate -= onPlaylistChange;
		};
		
		return c2;
	}
	
	void setSelectSongToAdd(Playlist p, string query = null){
		setMiddleScreen(selectSongToAdd(p, query));
	}
	
	MiddleScreen selectSongToAdd(Playlist p, string query = null, uint? inex = null){
		if(string.IsNullOrWhiteSpace(query)){
			query = null;
		}else{
			query = query.Trim();
		}
		
		List<Song> lib = query == null ? Song.getLibrary() : Song.getLibrary().Where(n => n != null && n.title.Contains(query, StringComparison.OrdinalIgnoreCase)).ToList();
		
		TuiSelectable[,] t = new TuiSelectable[Math.Max(lib.Count, 1), 1];
		
		for(int i = 0; i < lib.Count; i++){
			Song s = lib[i];
			TuiButton b = new TuiSongButton(s?.id ?? -1, Placement.TopLeft, 1, i, Palette.user);
			
			b.SetAction((s2, ck) => {
				p?.addSong(s.id);
				closeMiddleScreen();
			});
			
			Keybinds.play.subEvent(b, (s2, ck) => {
				Radio.py.play(s.id);
			});
			
			t[i, 0] = b;
		}
		
		//Static screen
		TuiScreen backg = generateMiddleStatic();
		
		backg.Elements.Add(new TuiTwoLabels("Select song to add to ", p?.title ?? Playlist.nullTitle, Placement.TopCenter, 0, 1, null, Palette.playlist));
		if(query != null){
			backg.Elements.Add(new TuiTwoLabels("Search results for: ", query, Placement.TopCenter, 0, 2, null, Palette.info));
		}
		
		if(lib.Count == 1){
			backg.Elements.Add(new TuiLabel(lib.Count + " song:", Placement.TopLeft, 2, 3));
		}else if(lib.Count > 0){
			backg.Elements.Add(new TuiLabel(lib.Count + " songs:", Placement.TopLeft, 2, 3));
		}else{
			backg.Elements.Add(new TuiLabel("No songs found", Placement.TopLeft, 2, 3));
		}
		
		//Inner screen
		TuiScrollingScreenInteractive l = new TuiScrollingScreenInteractive(Math.Max(backg.Xsize - 6, 0),
			Math.Max(backg.Ysize - 6, 0),
			t, 0, inex ?? 0,
			Placement.TopLeft, 3, 4,
			null
		);
		
		backg.Elements.Add(l);
		
		prepareScreen(l);
		
		l.OnParentResize += (s, a) => {
			l.Xsize = Math.Max(backg.Xsize - 6, 0);
			l.Ysize = Math.Max(backg.Ysize - 6, 0);
		};
		
		MiddleScreen c2 = new MiddleScreen(backg, l);
		
		Keybinds.search.subEvent(c2, true, (s, ck) => {
			setSearchScreen("Search song in library:", s => setSelectSongToAdd(p, s));
			
			removeMiddleScreen(c2);
		});
		
		void onLibChange(){
			updateMiddleScreen(c2, () => {
				return selectSongToAdd(p, query, l.MatrixPointerY);
			});
		}
		
		Song.onLibraryUpdate += onLibChange;
		
		c2.OnDispose = () => {
			Song.onLibraryUpdate -= onLibChange;
		};
		
		return c2;
	}
}