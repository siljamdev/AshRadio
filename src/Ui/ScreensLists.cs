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
	
	void setSongDetails(int s){
		setMiddleScreen(songDetails(Song.get(s)));
	}
	
	MiddleScreen songDetails(Song s){
		MiddleScreen c2 = null;
		
		TuiFramedScrollingTextBox titleInput = new TuiFramedScrollingTextBox(s?.title ?? "Untitled song", 256, 16, Placement.TopRight, 3, 5, null, null, null, Palette.user, Palette.user);
		
		titleInput.SubKeyEvent(ConsoleKey.Enter, (s2, ck) => {
			if(s != null){
				s.setTitle(titleInput.Text);
			}
		});
		
		titleInput.OnParentResize += (s, a) => {
			titleInput.BoxXsize = Math.Clamp(a.X - 32, 16, 38);
		};
		
		TuiFramedScrollingTextBox authorsInput = new TuiFramedScrollingTextBox(s == null || s.authors == null ? "" : (s.authors.Length == 0 ? "" : (s.authors.Length == 1 ? (Author.get(s.authors[0])?.name ?? "Unknown author") : string.Join(", ", s.authors.Select(n => (Author.get(n)?.name ?? "Unknown author"))))),
			64, 16, Placement.TopRight, 3, 11, null, null, null, Palette.user, Palette.user);
		
		authorsInput.SubKeyEvent(ConsoleKey.Enter, (s2, ck) => {
			if(s != null){
				string[] aps = authorsInput.Text.Split(',');
				int[] auts = Author.getAuthors(aps);
				s.setAuthors(auts);
			}
		});
		
		authorsInput.OnParentResize += (s, a) => {
			authorsInput.BoxXsize = Math.Clamp(a.X - 32, 16, 38);
		};
		
		TuiButton addPlaylist = new TuiButton("Add to playlist", Placement.TopRight, 7, 15, null, Palette.user).SetAction((s2, ck) => {
			setSelectPlaylistToAddTo(s.id);
		});
		
		TuiButton del = new TuiButton("Delete song", Placement.TopRight, 7, 17, null, Palette.user).SetAction((s2, ck) => {
			setConfirmScreen("Do you want to delete this song?", () => {
				Song.delete(s.id);
			});
		});
		
		//Selectables
		TuiSelectable[,] temp = new TuiSelectable[Math.Max((s?.authors.Length + 1) ?? 0, 4), 2];
		
		temp[0, 1] = titleInput;
		temp[1, 1] = authorsInput;
		temp[2, 1] = addPlaylist;
		temp[3, 1] = del;
		
		//Add authors
		if(s != null){
			for(int i = 0; i < s.authors.Length; i++){
				int tt3 = s.authors[i];
				TuiButton ar = new TuiButton(Author.get(tt3)?.name ?? "Unknown author", Placement.TopLeft, 4, 7 + i, Palette.author, Palette.user).SetAction((s2, ck) => {
					setAuthorDetails(tt3);
				});
				
				ar.SubKeyEvent(ConsoleKey.S, (s, ck) => {
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
		
		c.Elements.Add(new TuiLabel(s?.title ?? "Untitled song", Placement.TopLeft, 2, 2, Palette.song));
		c.Elements.Add(new TuiLabel("Song", Placement.TopLeft, 4, 3));
		c.Elements.Add(new TuiLabel("Set title:", Placement.TopRight, 11, 4));
		c.Elements.Add(new TuiLabel("Set authors:", Placement.TopRight, 9, 9));
		c.Elements.Add(new TuiLabel("(separated by commas)", Placement.TopRight, 0, 10));
		
		c.Elements.Add(new TuiTwoLabels("Q", " Add to queue", Placement.BottomRight, 0, 1, Palette.info, null));
		c.Elements.Add(new TuiTwoLabels("P", " Play", Placement.BottomRight, 0, 0, Palette.info, null));
		
		if(s != null){
			if(s.authors.Length == 1){
				c.Elements.Add(new TuiLabel("Author", Placement.TopLeft, 2, 6));
			}else if(s.authors.Length > 1){
				c.Elements.Add(new TuiLabel("Authors", Placement.TopLeft, 2, 6));
			}else{
				c.Elements.Add(new TuiLabel("Unknown authors", Placement.TopLeft, 2, 6));
			}
		}
		
		c.SubKeyEvent(ConsoleKey.Q, (s2, ck) => {
			Session.addToQueue(s.id);
		});
		
		c.SubKeyEvent(ConsoleKey.P, (s2, ck) => {
			Radio.py.play(s.id);
		});
		
		void onLibChange(object sender, LibraryEventArgs a){
			if(updateMiddleScreen(c2, () => {
				if(Song.exists(s.id)){
					return songDetails(Song.get(s.id));
				}
				
				return null;
			})){
				Song.onLibraryUpdate -= onLibChange;
				return;
			}
		}
		
		Song.onLibraryUpdate += onLibChange;
		
		return c2;
	}
	
	void setLibrary(string query = null){
		setMiddleScreen(library(query));
	}
	
	MiddleScreen library(string query = null, uint? inex = null){
		if(string.IsNullOrWhiteSpace(query)){
			query = null;
		}else{
			query = query.Trim();
		}
		
		List<Song> lib = query == null ? Song.getLibrary() : Song.getLibrary().Where(n => n != null && n.title.Contains(query, StringComparison.OrdinalIgnoreCase)).ToList();
		
		TuiButton import = new TuiButton("Import songs", Placement.TopRight, 0, 1, null, Palette.user);
		
		import.SetAction((s, ck) => {
			setImport();
		});
		
		TuiSelectable[,] t = new TuiSelectable[Math.Max(lib.Count, 1), query == null ? 2 : 1];
		
		for(int i = 0; i < lib.Count; i++){
			Song s = lib[i];
			TuiButton b = new TuiButton(s?.title ?? "Untitled song", Placement.TopLeft, 1, i, Palette.song, Palette.user);
			
			b.SetAction((s2, ck) => {
				setSongDetails(s);
			});
			
			b.SubKeyEvent(ConsoleKey.Q, (s2, ck) => {
				Session.addToQueue(s.id);
			});
			
			b.SubKeyEvent(ConsoleKey.P, (s2, ck) => {
				Radio.py.play(s.id);
			});
			
			t[i, 0] = b;
			if(query == null){
				t[i, 1] = import;
			}
		}
		
		//Static screen
		TuiScreen backg = generateMiddleStatic();
		
		backg.Elements.Add(new TuiLabel("Library", Placement.TopCenter, 0, 1, Palette.main));
		if(query != null){
			backg.Elements.Add(new TuiTwoLabels("Search results for: ", query, Placement.TopCenter, 0, 3, null, Palette.info));
		}
		
		backg.Elements.Add(new TuiTwoLabels("S", " Set source", Placement.BottomRight, 0, 0, Palette.info, null));
		backg.Elements.Add(new TuiTwoLabels("F", " Search", Placement.BottomRight, 0, 1, Palette.info, null));
		
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
		
		l.FixedElements.Add(import);
		
		l.SubKeyEvent(ConsoleKey.S, (s, ck) => { //Set source
			Session.setSource(SourceType.Library);
		});
		
		MiddleScreen c2 = new MiddleScreen(backg, l);
		
		l.SubKeyEvent(ConsoleKey.F, (s, ck) => {
			setSearchScreen("Search song in library:", s => setLibrary(s));
			
			if(query != null){
				removeMiddleScreen(c2);
			}
		});
		
		void onLibChange(object sender, LibraryEventArgs a){
			if(updateMiddleScreen(c2, () => {
				return library(query, l.MatrixPointerY);
			})){
				Song.onLibraryUpdate -= onLibChange;
				return;
			}
		}
		
		Song.onLibraryUpdate += onLibChange;
		
		return c2;
	}
	
	void setAuthorDetails(Author s){
		setMiddleScreen(authorDetails(s));
	}
	
	void setAuthorDetails(int s){
		setMiddleScreen(authorDetails(Author.get(s)));
	}
	
	MiddleScreen authorDetails(Author s, uint? inex = null){
		TuiFramedScrollingTextBox name = new TuiFramedScrollingTextBox(s?.name ?? "Unknown author", 256, 16, Placement.TopRight, 1, 0, null, null, null, Palette.user, Palette.user);
		
		name.SubKeyEvent(ConsoleKey.Enter, (s2, ck) => {
			if(s != null){
				s.setName(name.Text);
			}
		});
		
		name.OnParentResize += (s, a) => {
			name.BoxXsize = Math.Clamp(a.X - 32, 16, 38);
		};
		
		TuiButton del = new TuiButton("Delete author", Placement.TopRight, 2, 4, null, Palette.user).SetAction((s2, ck) => {
			setConfirmScreen("Do you want to delete this author?", () => {
				Author.delete(s.id);
			});
		});
		
		List<Song> songs = s?.getSongs();
		
		TuiSelectable[,] temp = new TuiSelectable[Math.Max(songs?.Count ?? 0, 2), 2];
		
		if(songs != null){
			for(int i = 0; i < songs.Count; i++){
				Song ttt3 = songs[i];
				
				TuiButton b = new TuiButton(ttt3?.title ?? "Untitled song", Placement.TopLeft, 0, i, Palette.song, Palette.user).SetAction((s, ck) => {
					setSongDetails(ttt3);
				});
				
				b.SubKeyEvent(ConsoleKey.Q, (s2, ck) => {
					Session.addToQueue(ttt3.id);
				});
				
				b.SubKeyEvent(ConsoleKey.P, (s2, ck) => {
					Radio.py.play(ttt3.id);
				});
				
				temp[i, 0] = b;
				if(i % 2 == 0){
					temp[i, 1] = name;
				}else{
					temp[i, 1] = del;
				}
			}
		}
		
		//Static screen
		TuiScreen backg = generateMiddleStatic();
		
		backg.Elements.Add(new TuiLabel(s?.name ?? "Unknown author", Placement.TopLeft, 2, 2, Palette.author));
		backg.Elements.Add(new TuiLabel("Author", Placement.TopLeft, 4, 3));
		
		backg.Elements.Add(new TuiTwoLabels("S", " Set source", Placement.BottomRight, 0, 0, Palette.info, null));
		
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
			null
		);
		
		backg.Elements.Add(l);
		
		prepareScreen(l);
		
		l.OnParentResize += (s, a) => {
			l.Xsize = Math.Max(backg.Xsize - 7, 0);
			l.Ysize = Math.Max(backg.Ysize - 8, 0);
		};
		
		l.FixedElements.Add(name);
		l.FixedElements.Add(del);
		
		l.SubKeyEvent(ConsoleKey.S, (sc, ck) => { //Set source
			Session.setSource(SourceType.Author, s.id);
		});
		
		MiddleScreen c2 = new MiddleScreen(backg, l);
		
		void onLibChange(object sender, LibraryEventArgs a){
			if(updateMiddleScreen(c2, () => {
				return authorDetails(Author.get(s.id), l.MatrixPointerY);
			})){
				Song.onLibraryUpdate -= onLibChange;
				return;
			}
		}
		
		Song.onLibraryUpdate += onLibChange;
		
		void onAuthorsChange(object sender, EventArgs a){
			if(updateMiddleScreen(c2, () => {
				if(Author.exists(s.id)){
					return authorDetails(Author.get(s.id), l.MatrixPointerY);
				}
				
				return null;
			})){
				Author.onAuthorsUpdate -= onAuthorsChange;
				return;
			}
		}
		
		Author.onAuthorsUpdate += onAuthorsChange;
		
		return c2;
	}
	
	void setAuthors(string query = null){
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
			TuiButton b = new TuiButton(s?.name ?? "Unknown author", Placement.TopLeft, 1, i, Palette.author, Palette.user);
			
			b.SetAction((s2, ck) => {
				setAuthorDetails(s);
			});
			
			b.SubKeyEvent(ConsoleKey.S, (s2, ck) => {
				Session.setSource(SourceType.Author, s.id);
			});
			
			t[i, 0] = b;
		}
		
		//Static screen
		TuiScreen backg = generateMiddleStatic();
		
		backg.Elements.Add(new TuiLabel("Authors", Placement.TopCenter, 0, 1, Palette.main));
		if(query != null){
			backg.Elements.Add(new TuiTwoLabels("Search results for: ", query, Placement.TopCenter, 0, 3, null, Palette.info));
		}
		
		backg.Elements.Add(new TuiTwoLabels("F", " Search", Placement.BottomRight, 0, 0, Palette.info, null));
		
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
		
		MiddleScreen c2 = new MiddleScreen(backg, l);
		
		l.SubKeyEvent(ConsoleKey.F, (s, ck) => {
			setSearchScreen("Search authors:", s => setAuthors(s));
			
			if(query != null){
				removeMiddleScreen(c2);
			}
		});
		
		void onAuthorsChange(object sender, EventArgs a){
			if(updateMiddleScreen(c2, () => {
				return authors(query, l.MatrixPointerY);
			})){
				Author.onAuthorsUpdate -= onAuthorsChange;
				return;
			}
		}
		
		Author.onAuthorsUpdate += onAuthorsChange;
		
		return c2;
	}
	
	void setPlaylistDetails(Playlist s){
		setMiddleScreen(playlistDetails(s));
	}
	
	void setPlaylistDetails(int s){
		setMiddleScreen(playlistDetails(Playlist.get(s)));
	}
	
	MiddleScreen playlistDetails(Playlist s, uint? inex = null){
		TuiFramedScrollingTextBox name = new TuiFramedScrollingTextBox(s?.title ?? "Untitled playlist", 256, 16, Placement.TopRight, 1, 0, null, null, null, Palette.user, Palette.user);
		
		name.SubKeyEvent(ConsoleKey.Enter, (s2, ck) => {
			if(s != null){
				s.setTitle(name.Text);
			}
		});
		
		name.OnParentResize += (s, a) => {
			name.BoxXsize = Math.Clamp(a.X - 32, 16, 38);
		};
		
		TuiButton add = new TuiButton("Add song", Placement.TopRight, 2, 4, null, Palette.user).SetAction((s2, ck) => {
			setSelectSongToAdd(s);
		});
		
		TuiButton del = new TuiButton("Delete playlist", Placement.TopRight, 2, 6, null, Palette.user).SetAction((s2, ck) => {
			setConfirmScreen("Do you want to delete this playlist?", () => {
				Playlist.delete(s.id);
			});
		});
		
		List<Song> songs = s?.getSongs().Where(n => n != null).ToList();
		
		TuiSelectable[,] temp = new TuiSelectable[Math.Max(songs?.Count ?? 0, 3), 2];
		
		TuiScrollingScreenInteractive l = null!;
		
		if(songs != null){
			for(int i = 0; i < songs.Count; i++){
				Song ttt3 = songs[i];
				
				int j = i;
				
				TuiButton b = new TuiButton(ttt3?.title ?? "Untitled song", Placement.TopLeft, 0, i, Palette.song, Palette.user).SetAction((s, ck) => {
					setSongDetails(ttt3);
				});
				
				b.SubKeyEvent(ConsoleKey.Q, (s2, ck) => {
					Session.addToQueue(ttt3.id);
				});
				
				b.SubKeyEvent(ConsoleKey.P, (s2, ck) => {
					Radio.py.play(ttt3.id);
				});
				
				b.SubKeyEvent(ConsoleKey.R, (s2, ck) => {
					s.deleteSong(j);
				});
				
				b.SubKeyEvent(ConsoleKey.N, (s2, ck) => {
					if(j != 0){
						TuiScreenInteractive.MoveUp(l, ck);
						s.moveSong(j, j - 1);
					}
				});
				
				b.SubKeyEvent(ConsoleKey.M, (s2, ck) => {
					if(j != songs.Count - 1){
						TuiScreenInteractive.MoveDown(l, ck);
						s.moveSong(j, j + 1);
					}
				});
				
				temp[i, 0] = b;
				if(i % 3 == 0){
					temp[i, 1] = name;
				}else if(i % 3 == 1){
					temp[i, 1] = add;
				}else{
					temp[i, 1] = del;
				}
			}
		}
		
		//Static screen
		TuiScreen backg = generateMiddleStatic();
		
		backg.Elements.Add(new TuiLabel(s?.title ?? "Untitled playlist", Placement.TopLeft, 2, 2, Palette.playlist));
		backg.Elements.Add(new TuiLabel("Playlist", Placement.TopLeft, 4, 3));
		
		backg.Elements.Add(new TuiTwoLabels("S", " Set source", Placement.BottomRight, 0, 0, Palette.info, null));
		
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
			null
		);
		
		backg.Elements.Add(l);
		
		prepareScreen(l);
		
		l.OnParentResize += (s, a) => {
			l.Xsize = Math.Max(backg.Xsize - 7, 0);
			l.Ysize = Math.Max(backg.Ysize - 8, 0);
		};
		
		l.FixedElements.Add(name);
		l.FixedElements.Add(add);
		l.FixedElements.Add(del);
		
		l.SubKeyEvent(ConsoleKey.S, (sc, ck) => { //Set source
			Session.setSource(SourceType.Playlist, s.id);
		});
		
		MiddleScreen c2 = new MiddleScreen(backg, l);
		
		void onLibChange(object sender, LibraryEventArgs a){
			if(updateMiddleScreen(c2, () => {
				return playlistDetails(Playlist.get(s.id), l.MatrixPointerY);
			})){
				Song.onLibraryUpdate -= onLibChange;
				return;
			}
		}
		
		Song.onLibraryUpdate += onLibChange;
		
		void onPlaylistChange(object sender, PlaylistEventArgs a){
			if(a.id != s.id){
				return;
			}
			
			if(updateMiddleScreen(c2, () => {
				if(Playlist.exists(s.id)){
					return playlistDetails(Playlist.get(s.id), l.MatrixPointerY);
				}
				
				return null;
			})){
				Playlist.onPlaylistUpdate -= onPlaylistChange;
				return;
			}
		}
		
		Playlist.onPlaylistUpdate += onPlaylistChange;
		
		return c2;
	}
	
	void setPlaylists(string query = null){
		setMiddleScreen(playlists(query));
	}
	
	MiddleScreen playlists(string query = null, uint? inex = null){
		if(string.IsNullOrWhiteSpace(query)){
			query = null;
		}else{
			query = query.Trim();
		}
		
		List<Playlist> lib = query == null ? Playlist.getAllPlaylists() : Playlist.getAllPlaylists().Where(n => n != null && n.title.Contains(query, StringComparison.OrdinalIgnoreCase)).ToList();
		
		TuiButton create = new TuiButton("Create playlist", Placement.TopRight, 3, 0, null, Palette.user);
		
		create.SetAction((s, ck) => {
			setPlaylistDetails(Playlist.create("Untitled playlist"));
		});
		
		TuiButton import = new TuiButton("Import from folder", Placement.TopRight, 3, 2, null, Palette.user);
		TuiButton importYt = new TuiButton("Import from yt", Placement.TopRight, 3, 4, null, Palette.user);
		
		import.SetAction((s, ck) => {
			setImportFolderPlaylist();
		});
		
		importYt.SetAction((s, ck) => {
			setImportPlaylist();
		});
		
		TuiSelectable[,] t = new TuiSelectable[Math.Max(lib.Count, 3), query == null ? 2 : 1];
		
		for(int i = 0; i < lib.Count; i++){
			Playlist s = lib[i];
			TuiButton b = new TuiButton(s?.title ?? "Untitled playlist", Placement.TopLeft, 1, i, Palette.playlist, Palette.user);
			
			b.SetAction((s2, ck) => {
				setPlaylistDetails(s);
			});
			
			b.SubKeyEvent(ConsoleKey.S, (s2, ck) => {
				Session.setSource(SourceType.Playlist, s.id);
			});
			
			t[i, 0] = b;
			if(query == null){
				if(i % 3 == 0){
					t[i, 1] = create;
				}else if(i % 3 == 1){
					t[i, 1] = import;
				}else{
					t[i, 1] = importYt;
				}
			}
		}
		
		//Static screen
		TuiScreen backg = generateMiddleStatic();
		
		backg.Elements.Add(new TuiLabel("Playlists", Placement.TopCenter, 0, 1, Palette.main));
		if(query != null){
			backg.Elements.Add(new TuiTwoLabels("Search results for: ", query, Placement.TopCenter, 0, 3, null, Palette.info));
		}
		
		backg.Elements.Add(new TuiTwoLabels("F", " Search", Placement.BottomRight, 0, 0, Palette.info, null));
		
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
		
		l.SubKeyEvent(ConsoleKey.F, (s, ck) => {
			setSearchScreen("Search playlists:", s => setPlaylists(s));
			
			if(query != null){
				removeMiddleScreen(c2);
			}
		});
		
		void onPlaylistChange(object sender, PlaylistEventArgs a){
			if(updateMiddleScreen(c2, () => {
				return playlists(query, l.MatrixPointerY);
			})){
				Playlist.onPlaylistUpdate -= onPlaylistChange;
				return;
			}
		}
		
		Playlist.onPlaylistUpdate += onPlaylistChange;
		
		return c2;
	}
	
	//Select to add
	
	void setSelectPlaylistToAddTo(int sindex, string query = null){
		setMiddleScreen(selectPlaylistToAddTo(sindex, query));
	}
	
	MiddleScreen selectPlaylistToAddTo(int sindex, string query = null, uint? inex = null){
		if(string.IsNullOrWhiteSpace(query)){
			query = null;
		}else{
			query = query.Trim();
		}
		
		List<Playlist> lib = query == null ? Playlist.getAllPlaylists() : Playlist.getAllPlaylists().Where(n => n != null && n.title.Contains(query, StringComparison.OrdinalIgnoreCase)).ToList();
		
		TuiSelectable[,] t = new TuiSelectable[Math.Max(lib.Count, 3), 1];
		
		for(int i = 0; i < lib.Count; i++){
			Playlist s = lib[i];
			TuiButton b = new TuiButton(s?.title ?? "Untitled playlist", Placement.TopLeft, 1, i, Palette.playlist, Palette.user);
			
			b.SetAction((s2, ck) => {
				s.addSong(sindex);
				closeMiddleScreen();
			});
			
			t[i, 0] = b;
		}
		
		//Static screen
		TuiScreen backg = generateMiddleStatic();
		
		backg.Elements.Add(new TuiTwoLabels("Select playlist where to add ", Song.get(sindex)?.title ?? "Untitled song", Placement.TopCenter, 0, 1, null, Palette.song));
		if(query != null){
			backg.Elements.Add(new TuiTwoLabels("Search results for: ", query, Placement.TopCenter, 0, 3, null, Palette.info));
		}
		
		backg.Elements.Add(new TuiTwoLabels("F", " Search", Placement.BottomRight, 0, 0, Palette.info, null));
		
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
		
		l.SubKeyEvent(ConsoleKey.F, (s, ck) => {
			setSearchScreen("Search playlists:", s => setSelectPlaylistToAddTo(sindex, s));
			
			removeMiddleScreen(c2);
		});
		
		void onPlaylistChange(object sender, PlaylistEventArgs a){
			if(updateMiddleScreen(c2, () => {
				return selectPlaylistToAddTo(sindex, query, l.MatrixPointerY);
			})){
				Playlist.onPlaylistUpdate -= onPlaylistChange;
				return;
			}
		}
		
		Playlist.onPlaylistUpdate += onPlaylistChange;
		
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
			TuiButton b = new TuiButton(s?.title ?? "Untitled song", Placement.TopLeft, 1, i, Palette.song, Palette.user);
			
			b.SetAction((s2, ck) => {
				p.addSong(s.id);
				closeMiddleScreen();
			});
			
			t[i, 0] = b;
		}
		
		//Static screen
		TuiScreen backg = generateMiddleStatic();
		
		backg.Elements.Add(new TuiTwoLabels("Select song to add to ", p?.title ?? "Untitled playlist", Placement.TopCenter, 0, 1, null, Palette.playlist));
		if(query != null){
			backg.Elements.Add(new TuiTwoLabels("Search results for: ", query, Placement.TopCenter, 0, 3, null, Palette.info));
		}
		
		backg.Elements.Add(new TuiTwoLabels("F", " Search", Placement.BottomRight, 0, 0, Palette.info, null));
		
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
		
		l.SubKeyEvent(ConsoleKey.F, (s, ck) => {
			setSearchScreen("Search song in library:", s => setSelectSongToAdd(p, s));
			
			removeMiddleScreen(c2);
		});
		
		void onLibChange(object sender, LibraryEventArgs a){
			if(updateMiddleScreen(c2, () => {
				return selectSongToAdd(p, query, l.MatrixPointerY);
			})){
				Song.onLibraryUpdate -= onLibChange;
				return;
			}
		}
		
		Song.onLibraryUpdate += onLibChange;
		
		return c2;
	}
}