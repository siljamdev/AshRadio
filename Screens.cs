using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using AshLib.Time;
using AshLib.Formatting;
using AshConsoleGraphics;
using AshConsoleGraphics.Interactive;
using NAudio.CoreAudioApi;

public partial class Screens{
	void setSongDetails(int sindex){
		Song s = Song.load(sindex);
		setSongDetails(s);
	}
	
	void setSongDetails(Song s){
		TuiFramedScrollingTextBox title = new TuiFramedScrollingTextBox(s?.title ?? "Untitled song", 256, 16, Placement.TopRight, 3, 5, null, null, null, Palette.user, Palette.user);
		
		title.SubKeyEvent(ConsoleKey.Enter, (s2, ck) => {
			if(s != null){
				s.setTitle(title.Text);
				
				closeMiddleScreen(); //Update
				setSongDetails(s);
			}
		});
		
		title.OnParentResize += (s, a) => {
			title.BoxXsize = Math.Clamp(a.X - 32, 16, 38);
		};
		
		TuiFramedScrollingTextBox authors = new TuiFramedScrollingTextBox(s == null || s.authors == null ? "" : (s.authors.Length == 0 ? "" : (s.authors.Length == 1 ? (Author.load(s.authors[0])?.name ?? "Unknown author") : string.Join(", ", s.authors.Select(n => (Author.load(n)?.name ?? "Unknown author"))))),
			64, 16, Placement.TopRight, 3, 11, null, null, null, Palette.user, Palette.user);
		
		authors.SubKeyEvent(ConsoleKey.Enter, (s2, ck) => {
			if(s != null){
				string[] aps = authors.Text.Split(',');
				int[] auts = Author.getAuthors(aps);
				s.setAuthors(auts);
				
				closeMiddleScreen(); //Update
				setSongDetails(s);
			}
		});
		
		authors.OnParentResize += (s, a) => {
			authors.BoxXsize = Math.Clamp(a.X - 32, 16, 38);
		};
		
		TuiButton addPlaylist = new TuiButton("Add to playlist", Placement.TopRight, 7, 15, null, Palette.user).SetAction((s2, ck) => {
			selectPlaylistToAddTo(s.id);
		});
		
		TuiButton del = new TuiButton("Delete song", Placement.TopRight, 7, 17, null, Palette.user).SetAction((s2, ck) => {
			confirmDeleteSong(s.id);
		});
		
		TuiSelectable[,] temp = new TuiSelectable[Math.Max((s?.authors.Length + 1) ?? 0, 4), 2];
		
		temp[0, 1] = title;
		temp[1, 1] = authors;
		temp[2, 1] = addPlaylist;
		temp[3, 1] = del;
		
		if(s != null){
			for(int i = 0; i < s.authors.Length; i++){
				int tt3 = s.authors[i];
				TuiButton ar = new TuiButton(Author.load(tt3)?.name ?? "Unknown author", Placement.TopLeft, 4, 8 + i, Palette.author, Palette.user).SetAction((s2, ck) => {
					setAuthorDetails(tt3);
				});
				
				ar.SubKeyEvent(ConsoleKey.S, (s, ck) => {
					Session.setSource(SourceType.Author, tt3);
				});
				
				temp[i + 1, 0] = ar;
			}
		}
		
		TuiScreenInteractive c = getMiddle(temp);
		
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
				c.Elements.Add(new TuiLabel("Author", Placement.TopLeft, 2, 7));
			}else if(s.authors.Length > 1){
				c.Elements.Add(new TuiLabel("Authors", Placement.TopLeft, 2, 7));
			}else{
				c.Elements.Add(new TuiLabel("Unknown authors", Placement.TopLeft, 2, 7));
			}
		}
		
		c.SubKeyEvent(ConsoleKey.Q, (s2, ck) => {
			Session.addToQueue(s.id);
		});
		
		c.SubKeyEvent(ConsoleKey.P, (s2, ck) => {
			Radio.py.play(s.id);
		});
		
		void onLibChange(object sender, LibraryEventArgs a){
			if(!middle.Contains(c)){
				Song.onLibraryUpdate -= onLibChange;
				return;
			}
			
			Stack<TuiScreenInteractive> tempMid = new();
			while(middle.Count > 0){
				tempMid.Push(middle.Peek());
				closeMiddleScreen();
				if(tempMid.Peek() == c){
					tempMid.Pop();
					if(Song.exists(s.id)){
						setSongDetails(s.id);
					}
					while(tempMid.Count > 0){
						setMiddleScreen(tempMid.Pop());
					}
					
					Song.onLibraryUpdate -= onLibChange;
					return;
				}
			}
			
			while(tempMid.Count > 0){
				setMiddleScreen(tempMid.Pop());
			}
		}
		
		Song.onLibraryUpdate += onLibChange;
		
		setMiddleScreen(c);
	}
	
	void setLibrary(uint? inex = null){
		List<int> lib = Song.getLibrary();
		
		TuiButton import = new TuiButton("Import songs", Placement.TopRight, 3, 5, null, Palette.user);
		
		import.SetAction((s, ck) => {
			setImport();
		});
		
		TuiSelectable[,] t = new TuiSelectable[Math.Max(lib.Count, 1), 2];
		
		t[0, 1] = import;
		
		string[] titles = new string[lib.Count];
		
		for(int i = 0; i < lib.Count; i++){
			Song s = Song.load(lib[i]);
			titles[i] = s?.title ?? "Untitled song";
			TuiButton b = new TuiButton("", Placement.TopLeft, 2, i + 4, Palette.song, Palette.user);
			
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
		}
		
		TuiScreenInteractive l = getMiddle(t);
		
		l.MatrixPointerX = (uint) ((lib?.Count > 0) ? 0 : 1);
		l.MatrixPointerY = inex ?? 0;
		
		l.Elements.Add(new TuiLabel("Library", Placement.TopCenter, 0, 1, Palette.main));
		
		if(lib.Count == 0){
			l.Elements.Add(new TuiLabel("No songs", Placement.TopLeft, 2, 4));
		}
		
		l.Elements.Add(new TuiTwoLabels("S", " Set source", Placement.BottomRight, 0, 0, Palette.info, null));
		l.Elements.Add(new TuiTwoLabels("F", " Search", Placement.BottomRight, 0, 1, Palette.info, null));
		
		l.DeleteAllKeyEvents();
		
		void update(){
			int j = 0;
			for(int i = 0; i < l.Elements.Count; i++){
				TuiElement e = l.Elements[i];
				if(!(e is TuiButton b) || e == import){
					continue;
				}
				if(e.OffsetY > 3 && e.OffsetY < l.Ysize - 2){
					b.Text = crop(titles[j], l.Xsize - 12);
				}else{
					b.Text = "";
				}
				j++;
			}
		}
		
		int toChange = Math.Max(0, (l.Selected?.OffsetY ?? 0) - (int) l.Ysize + 3);
		
		foreach(TuiElement e in l){
			if(e is TuiButton && e != import){
				e.OffsetY -= toChange;	
			}
		}
		
		update();
		
		l.SubKeyEvent(ConsoleKey.UpArrow, (s, ck) => { //Scroll
			TuiScreenInteractive.MoveUp(s, ck);
			if(s.Selected?.OffsetY < 4){
				foreach(TuiElement e in s){
					if(e is TuiButton && e != import){
						e.OffsetY++;	
					}
				}
			}
			update();
		});
		
		l.SubKeyEvent(ConsoleKey.DownArrow, (s, ck) => { //Scroll
			TuiScreenInteractive.MoveDown(s, ck);
			if(s.Selected?.OffsetY >= s.Ysize - 2){
				foreach(TuiElement e in s){
					if(e is TuiButton && e != import){
						e.OffsetY--;	
					}
				}
			}
			update();
		});
		
		l.SubKeyEvent(ConsoleKey.RightArrow, (s, ck) => {
			s.MatrixPointerY = 0;
			s.MatrixPointerX = 1;
			int j = 0;
			foreach(TuiElement e in s){
				if(e is TuiButton && e != import){
					e.OffsetY = j + 4;
					j++;
				}
			}
			update();
		});
		
		l.SubKeyEvent(ConsoleKey.LeftArrow, (s, ck) => {
			TuiScreenInteractive.MoveLeft(s, ck);
		});
		
		l.OnResize += (s, a) => {
			update();
		};
		
		l.SubKeyEvent(ConsoleKey.S, (s, ck) => { //Set source
			Session.setSource(SourceType.Library);
		});
		
		l.SubKeyEvent(ConsoleKey.F, (s, ck) => {
			setSearchScreen();
		});
		
		void onLibChange(object sender, LibraryEventArgs a){
			if(!middle.Contains(l)){
				Song.onLibraryUpdate -= onLibChange;
				return;
			}
			
			Stack<TuiScreenInteractive> tempMid = new();
			while(middle.Count > 0){
				tempMid.Push(middle.Peek());
				closeMiddleScreen();
				if(tempMid.Peek() == l){
					tempMid.Pop();
					setLibrary(l.MatrixPointerY);
					while(tempMid.Count > 0){
						setMiddleScreen(tempMid.Pop());
					}
					
					Song.onLibraryUpdate -= onLibChange;
					return;
				}
			}
			
			while(tempMid.Count > 0){
				setMiddleScreen(tempMid.Pop());
			}
		}
		
		Song.onLibraryUpdate += onLibChange;
		
		setMiddleScreen(l);
	}
	
	void setSearchScreen(){
		TuiMultiLineScrollingFramedTextBox input = new TuiMultiLineScrollingFramedTextBox("", 256, 34, 3, Placement.TopCenter, 0, 4, null, null, null, Palette.user, Palette.user);
		
		input.OnParentResize += (s, a) => {
			input.BoxXsize = Math.Max(0, a.X - 4);
		};
		
		input.SubKeyEvent(ConsoleKey.Enter, (s, ck) => {
			closeMiddleScreen();
			setLibrarySearch(input.Text);
		});
		
		TuiSelectable[,] t = new TuiSelectable[,]{{
			input
		}};
		
		TuiScreenInteractive l = getMiddle(t);
		
		l.Elements.Add(new TuiLabel("Search", Placement.TopCenter, 0, 1, Palette.main));
		
		setMiddleScreen(l);
	}
	
	void setLibrarySearch(string query){
		List<Song> lib = Song.getLibrary().Select(n => Song.load(n)).Where(n => n != null && n.title.Contains(query, StringComparison.OrdinalIgnoreCase)).ToList();
		TuiSelectable[,] t = new TuiSelectable[lib.Count, 1];
		
		string[] titles = new string[lib.Count];
		
		for(int i = 0; i < lib.Count; i++){
			Song s = lib[i];
			titles[i] = s?.title ?? "Untitled song";
			TuiButton b = new TuiButton("", Placement.TopLeft, 2, i + 4, Palette.song, Palette.user);
			
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
		}
		
		TuiScreenInteractive l = getMiddle(t);
		
		l.Elements.Add(new TuiTwoLabels("Search results for: ", query, Placement.TopCenter, 0, 1, null, Palette.info));
		
		if(lib.Count == 0){
			l.Elements.Add(new TuiLabel("No songs found", Placement.TopLeft, 2, 4));
		}
		
		l.DeleteAllKeyEvents();
		
		void update(){
			for(int i = 0; i < l.Elements.Count; i++){
				TuiElement e = l.Elements[i];
				if(!(e is TuiButton b)){
					continue;
				}
				if(e.OffsetY > 3 && e.OffsetY < l.Ysize - 2){
					b.Text = titles[i - 1];
				}else{
					b.Text = "";
				}
			}
		}
		
		update();
		
		l.SubKeyEvent(ConsoleKey.UpArrow, (s, ck) => { //Scroll
			TuiScreenInteractive.MoveUp(s, ck);
			if(s.Selected?.OffsetY < 4){
				foreach(TuiElement e in s){
					if(e is TuiButton){
						e.OffsetY++;	
					}
				}
			}
			update();
		});
		
		l.SubKeyEvent(ConsoleKey.DownArrow, (s, ck) => { //Scroll
			TuiScreenInteractive.MoveDown(s, ck);
			if(s.Selected?.OffsetY >= s.Ysize - 2){
				foreach(TuiElement e in s){
					if(e is TuiButton){
						e.OffsetY--;	
					}
				}
			}
			update();
		});
		
		l.OnResize += (s, a) => {
			update();
		};
		
		void onLibChange(object sender, LibraryEventArgs a){
			if(!middle.Contains(l)){
				Song.onLibraryUpdate -= onLibChange;
				return;
			}
			
			Stack<TuiScreenInteractive> tempMid = new();
			while(middle.Count > 0){
				tempMid.Push(middle.Peek());
				closeMiddleScreen();
				if(tempMid.Peek() == l){
					tempMid.Pop();
					setLibrarySearch(query);
					while(tempMid.Count > 0){
						setMiddleScreen(tempMid.Pop());
					}
					
					Song.onLibraryUpdate -= onLibChange;
					return;
				}
			}
			
			while(tempMid.Count > 0){
				setMiddleScreen(tempMid.Pop());
			}
		}
		
		Song.onLibraryUpdate += onLibChange;
		
		setMiddleScreen(l);
	}
	
	void setAuthorDetails(int sindex){
		Author s = Author.load(sindex);
		setAuthorDetails(s);
	}
	
	void setAuthorDetails(Author s, uint? inex = null){
		TuiFramedScrollingTextBox name = new TuiFramedScrollingTextBox(s?.name ?? "Unknown author", 256, 16, Placement.TopRight, 3, 5, null, null, null, Palette.user, Palette.user);
		
		name.SubKeyEvent(ConsoleKey.Enter, (s2, ck) => {
			if(s != null){
				s.setName(name.Text);
			}
		});
		
		name.OnParentResize += (s, a) => {
			name.BoxXsize = Math.Clamp(a.X - 32, 16, 38);
		};
		
		TuiButton del = new TuiButton("Delete author", Placement.TopRight, 7, 9, null, Palette.user).SetAction((s2, ck) => {
			closeMiddleScreen();
			Author.delete(s?.id ?? -1);
		});
		
		List<Song> songs = s?.getSongs();
		
		TuiSelectable[,] temp = new TuiSelectable[Math.Max(songs?.Count ?? 0, 2), 2];
		
		temp[0, 1] = name;
		
		if(songs == null || songs.Count == 0){
			temp[1, 1] = del;
		}
		
		string[] titles = new string[songs?.Count ?? 0];
		
		if(songs != null){
			for(int i = 0; i < songs.Count; i++){
				Song ttt3 = songs[i];
				titles[i] = ttt3?.title ?? "Untitled song";
				
				TuiButton b = new TuiButton("", Placement.TopLeft, 4, 8 + i, Palette.song, Palette.user).SetAction((s, ck) => {
					setSongDetails(ttt3);
				});
				
				b.SubKeyEvent(ConsoleKey.Q, (s2, ck) => {
					Session.addToQueue(ttt3.id);
				});
				
				b.SubKeyEvent(ConsoleKey.P, (s2, ck) => {
					Radio.py.play(ttt3.id);
				});
				
				temp[i, 0] = b;
			}
		}
		
		TuiScreenInteractive c = getMiddle(temp);
		
		c.MatrixPointerX = (uint) ((songs?.Count > 0) ? 0 : 1);
		c.MatrixPointerY = inex ?? 0;
		
		c.Elements.Add(new TuiLabel(s?.name ?? "Unknown author", Placement.TopLeft, 2, 2, Palette.author));
		c.Elements.Add(new TuiLabel("Author", Placement.TopLeft, 4, 3));
		
		c.Elements.Add(new TuiTwoLabels("S", " Set source", Placement.BottomRight, 0, 0, Palette.info, null));
		
		if(songs != null){
			if(songs.Count == 1){
				c.Elements.Add(new TuiLabel(songs.Count + " song:", Placement.TopLeft, 2, 7));
			}else if(songs.Count > 0){
				c.Elements.Add(new TuiLabel(songs.Count + " songs:", Placement.TopLeft, 2, 7));
			}else{
				c.Elements.Add(new TuiLabel("No attributed songs", Placement.TopLeft, 2, 7));
			}
		}
		
		if(songs != null && songs.Count != 0){
			void update(){
				int j = 0;
				for(int i = 0; i < c.Elements.Count; i++){
					TuiElement e = c.Elements[i];
					if(!(e is TuiButton b)){
						continue;
					}
					if(e.OffsetY > 7 && e.OffsetY < c.Ysize - 2){
						b.Text = titles[j];
					}else{
						b.Text = "";
					}
					j++;
				}
			}
			
			int toChange = Math.Max(0, (c.Selected?.OffsetY ?? 0) - (int) c.Ysize + 3);
			
			foreach(TuiElement e in c){
				if(e is TuiButton){
					e.OffsetY -= toChange;	
				}
			}
			
			update();
			
			c.DeleteAllKeyEvents();
			
			c.SubKeyEvent(ConsoleKey.UpArrow, (s, ck) => { //Scroll
				TuiScreenInteractive.MoveUp(s, ck);
				if(s.Selected.OffsetY < 8){
					foreach(TuiElement e in s){
						if(e is TuiButton){
							e.OffsetY++;
						}
					}
				}
				update();
			});
			
			c.SubKeyEvent(ConsoleKey.DownArrow, (s, ck) => { //Scroll
				TuiScreenInteractive.MoveDown(s, ck);
				if(s.Selected.OffsetY >= s.Ysize - 2){
					foreach(TuiElement e in s){
						if(e is TuiButton){
							e.OffsetY--;	
						}
					}
				}
				update();
			});
			
			c.OnResize += (s, a) => {
				update();
			};
			
			c.SubKeyEvent(ConsoleKey.RightArrow, (s, ck) => {
				s.MatrixPointerY = 0;
				s.MatrixPointerX = 1;
				int j = 0;
				foreach(TuiElement e in s){
					if(e is TuiButton){
						e.OffsetY = j + 8;
						j++;
					}
				}
				update();
			});
			
			c.SubKeyEvent(ConsoleKey.LeftArrow, (s, ck) => {
				TuiScreenInteractive.MoveLeft(s, ck);
			});
			
			c.SubKeyEvent(ConsoleKey.S, (s2, ck) => {
				Session.setSource(SourceType.Author, s.id);
			});
		}
		
		void onLibChange(object sender, LibraryEventArgs a){
			if(!middle.Contains(c)){
				Song.onLibraryUpdate -= onLibChange;
				return;
			}
			
			if(!a.auth.Contains(s.id)){
				return;
			}
			
			Stack<TuiScreenInteractive> tempMid = new();
			while(middle.Count > 0){
				tempMid.Push(middle.Peek());
				closeMiddleScreen();
				if(tempMid.Peek() == c){
					tempMid.Pop();
					setAuthorDetails(s, c.MatrixPointerY);
					while(tempMid.Count > 0){
						setMiddleScreen(tempMid.Pop());
					}
					
					Song.onLibraryUpdate -= onLibChange;
					return;
				}
			}
			
			while(tempMid.Count > 0){
				setMiddleScreen(tempMid.Pop());
			}
		}
		
		Song.onLibraryUpdate += onLibChange;
		
		void onAuthorsChange(object sender, EventArgs a){
			if(!middle.Contains(c)){
				Author.onAuthorsUpdate -= onAuthorsChange;
				return;
			}
			
			Stack<TuiScreenInteractive> tempMid = new();
			while(middle.Count > 0){
				tempMid.Push(middle.Peek());
				closeMiddleScreen();
				if(tempMid.Peek() == c){
					tempMid.Pop();
					if(Author.exists(s.id)){
						setAuthorDetails(s, c.MatrixPointerY);
					}
					while(tempMid.Count > 0){
						setMiddleScreen(tempMid.Pop());
					}
					
					Author.onAuthorsUpdate -= onAuthorsChange;
					return;
				}
			}
			
			while(tempMid.Count > 0){
				setMiddleScreen(tempMid.Pop());
			}
		}
		
		Author.onAuthorsUpdate += onAuthorsChange;
		
		setMiddleScreen(c);
	}
	
	void setAuthors(){
		List<int> lib = Author.getAllIds();
		TuiSelectable[,] t = new TuiSelectable[lib.Count, 1];
		
		string[] titles = new string[lib.Count];
		
		for(int i = 0; i < lib.Count; i++){
			Author s = Author.load(lib[i]);
			titles[i] = s?.name ?? "Unknown author";
			TuiButton b = new TuiButton("", Placement.TopLeft, 2, i + 4, Palette.author, Palette.user);
			
			b.SetAction((s2, ck) => {
				setAuthorDetails(s.id);
			});
			
			b.SubKeyEvent(ConsoleKey.S, (s2, ck) => {
				Session.setSource(SourceType.Author, s.id);
			});
			
			t[i, 0] = b;
		}
		
		TuiScreenInteractive l = getMiddle(t);
		
		l.Elements.Add(new TuiLabel("Authors", Placement.TopCenter, 0, 1, Palette.main));
		
		if(lib.Count == 0){
			l.Elements.Add(new TuiLabel("No authors", Placement.TopLeft, 2, 4));
		}
		
		l.DeleteAllKeyEvents();
		
		void update(){
			for(int i = 0; i < l.Elements.Count; i++){
				TuiElement e = l.Elements[i];
				if(!(e is TuiButton b)){
					continue;
				}
				if(e.OffsetY > 3 && e.OffsetY < l.Ysize - 2){
					b.Text = titles[i - 1];
				}else{
					b.Text = "";
				}
			}
		}
		
		update();
		
		l.SubKeyEvent(ConsoleKey.UpArrow, (s, ck) => { //Scroll
			TuiScreenInteractive.MoveUp(s, ck);
			if(s.Selected?.OffsetY < 4){
				foreach(TuiElement e in s){
					if(e is TuiButton){
						e.OffsetY++;	
					}
				}
			}
			update();
		});
		
		l.SubKeyEvent(ConsoleKey.DownArrow, (s, ck) => { //Scroll
			TuiScreenInteractive.MoveDown(s, ck);
			if(s.Selected?.OffsetY >= s.Ysize - 2){
				foreach(TuiElement e in s){
					if(e is TuiButton){
						e.OffsetY--;	
					}
				}
			}
			update();
		});
		
		l.OnResize += (s, a) => {
			update();
		};
		
		void onAuthorsChange(object sender, EventArgs a){
			if(!middle.Contains(l)){
				Author.onAuthorsUpdate -= onAuthorsChange;
				return;
			}
			
			Stack<TuiScreenInteractive> tempMid = new();
			while(middle.Count > 0){
				tempMid.Push(middle.Peek());
				closeMiddleScreen();
				if(tempMid.Peek() == l){
					tempMid.Pop();
					setAuthors();
					while(tempMid.Count > 0){
						setMiddleScreen(tempMid.Pop());
					}
					
					Author.onAuthorsUpdate -= onAuthorsChange;
					return;
				}
			}
			
			while(tempMid.Count > 0){
				setMiddleScreen(tempMid.Pop());
			}
		}
		
		Author.onAuthorsUpdate += onAuthorsChange;
		
		setMiddleScreen(l);
	}
	
	void setPlaylistDetails(int s){
		Playlist p = Playlist.load(s);
		setPlaylistDetails(p);
	}
	
	void setPlaylistDetails(Playlist s, uint? inex = null){
		TuiFramedScrollingTextBox title = new TuiFramedScrollingTextBox(s?.title ?? "Untitled playlist", 256, 16, Placement.TopRight, 3, 5, null, null, null, Palette.user, Palette.user);
		
		title.SubKeyEvent(ConsoleKey.Enter, (s2, ck) => {
			if(s != null){
				s.setTitle(title.Text);
			}
		});
		
		title.OnParentResize += (s, a) => {
			title.BoxXsize = Math.Clamp(a.X - 32, 16, 38);
		};
		
		TuiButton add = new TuiButton("Add song", Placement.TopRight, 7, 9, null, Palette.user).SetAction((s2, ck) => {
			selectSongToAdd(s);
		});
		
		TuiButton del = new TuiButton("Delete playlist", Placement.TopRight, 7, 11, null, Palette.user).SetAction((s2, ck) => {
			confirmDeletePlaylist(s.id);
		});
		
		List<Song> songs = s?.getSongsIds().Select(n => Song.load(n)).Where(n => n != null).ToList();
		
		TuiSelectable[,] temp = new TuiSelectable[Math.Max(songs?.Count ?? 0, 3), 2];
		
		temp[0, 1] = title;
		temp[1, 1] = add;
		temp[2, 1] = del;
		
		string[] titles = new string[songs?.Count ?? 0];
		
		TuiScreenInteractive c = null!;
		
		if(songs != null){
			for(int i = 0; i < songs.Count; i++){
				Song ttt3 = songs[i];
				titles[i] = ttt3?.title ?? "Untitled song";
				
				int j = i;
				
				TuiButton b = new TuiButton("", Placement.TopLeft, 4, 8 + i, Palette.song, Palette.user).SetAction((s, ck) => {
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
						TuiScreenInteractive.MoveUp(c, ck);
						s.moveSong(j, j - 1);
					}
				});
				
				b.SubKeyEvent(ConsoleKey.M, (s2, ck) => {
					if(j != songs.Count - 1){
						TuiScreenInteractive.MoveDown(c, ck);
						s.moveSong(j, j + 1);
					}
				});
				
				temp[i, 0] = b;
			}
		}
		
		c = getMiddle(temp);
		
		c.MatrixPointerX = (uint) ((songs?.Count > 0) ? 0 : 1);
		c.MatrixPointerY = inex ?? (uint) ((songs?.Count > 0) ? 0 : 1);
		
		c.Elements.Add(new TuiLabel(s?.title ?? "Untitled playlist", Placement.TopLeft, 2, 2, Palette.playlist));
		c.Elements.Add(new TuiLabel("Playlist", Placement.TopLeft, 4, 3));
		
		c.Elements.Add(new TuiTwoLabels("S", " Set source", Placement.BottomRight, 0, 0, Palette.info, null));
		
		if(songs != null){
			if(songs.Count == 1){
				c.Elements.Add(new TuiLabel(songs.Count + " song:", Placement.TopLeft, 2, 7));
			}else if(songs.Count > 0){
				c.Elements.Add(new TuiLabel(songs.Count + " songs:", Placement.TopLeft, 2, 7));
			}else{
				c.Elements.Add(new TuiLabel("No songs", Placement.TopLeft, 2, 7));
			}
		}
		
		if(songs != null && songs.Count != 0){
			void update(){
				int j = 0;
				for(int i = 0; i < c.Elements.Count; i++){
					TuiElement e = c.Elements[i];
					if(!(e is TuiButton b) || e == del || e == add){
						continue;
					}
					if(e.OffsetY > 7 && e.OffsetY < c.Ysize - 2){
						b.Text = titles[j];
					}else{
						b.Text = "";
					}
					j++;
				}
			}
			
			int toChange = Math.Max(0, (c.Selected?.OffsetY ?? 0) - c.Ysize + 3);
			
			foreach(TuiElement e in c){
				if(e is TuiButton && e != del && e != add){
					e.OffsetY -= toChange;	
				}
			}
			
			update();
			
			c.DeleteAllKeyEvents();
			
			c.SubKeyEvent(ConsoleKey.UpArrow, (s, ck) => { //Scroll
				TuiScreenInteractive.MoveUp(s, ck);
				if(!(s.Selected is TuiFramedScrollingTextBox) && s.Selected.OffsetY < 8){
					foreach(TuiElement e in s){
						if(e is TuiButton && e != del && e != add){
							e.OffsetY++;
						}
					}
				}
				update();
			});
			
			c.SubKeyEvent(ConsoleKey.DownArrow, (s, ck) => { //Scroll
				TuiScreenInteractive.MoveDown(s, ck);
				if(s.Selected.OffsetY >= s.Ysize - 2){
					foreach(TuiElement e in s){
						if(e is TuiButton && e != del && e != add){
							e.OffsetY--;	
						}
					}
				}
				update();
			});
			
			c.SubKeyEvent(ConsoleKey.RightArrow, (s, ck) => {
				s.MatrixPointerY = 0;
				s.MatrixPointerX = 1;
				int j = 0;
				foreach(TuiElement e in s){
					if(e is TuiButton && e != del && e != add){
						e.OffsetY = j + 8;
						j++;
					}
				}
				update();
			});
			
			c.SubKeyEvent(ConsoleKey.LeftArrow, (s, ck) => {
				TuiScreenInteractive.MoveLeft(s, ck);
			});
			
			c.OnResize += (s, a) => {
				update();
			};
		}
		
		c.SubKeyEvent(ConsoleKey.S, (s2, ck) => {
			Session.setSource(SourceType.Playlist, s.id);
		});
		
		void onLibChange(object sender, LibraryEventArgs a){
			if(!middle.Contains(c)){
				Song.onLibraryUpdate -= onLibChange;
				return;
			}
			
			Stack<TuiScreenInteractive> tempMid = new();
			while(middle.Count > 0){
				tempMid.Push(middle.Peek());
				closeMiddleScreen();
				if(tempMid.Peek() == c){
					tempMid.Pop();
					setPlaylistDetails(s, c.MatrixPointerY);
					while(tempMid.Count > 0){
						setMiddleScreen(tempMid.Pop());
					}
					
					Song.onLibraryUpdate -= onLibChange;
					return;
				}
			}
			
			while(tempMid.Count > 0){
				setMiddleScreen(tempMid.Pop());
			}
		}
		
		Song.onLibraryUpdate += onLibChange;
		
		void onPlaylistChange(object sender, PlaylistEventArgs a){
			if(!middle.Contains(c)){
				Playlist.onPlaylistUpdate -= onPlaylistChange;
				return;
			}
			
			if(a.id != s.id){
				return;
			}
			
			Stack<TuiScreenInteractive> tempMid = new();
			while(middle.Count > 0){
				tempMid.Push(middle.Peek());
				closeMiddleScreen();
				if(tempMid.Peek() == c){
					tempMid.Pop();
					setPlaylistDetails(s, c.MatrixPointerY);
					while(tempMid.Count > 0){
						setMiddleScreen(tempMid.Pop());
					}
					
					Playlist.onPlaylistUpdate -= onPlaylistChange;
					return;
				}
			}
			
			while(tempMid.Count > 0){
				setMiddleScreen(tempMid.Pop());
			}
		}
		
		Playlist.onPlaylistUpdate += onPlaylistChange;
		
		setMiddleScreen(c);
	}
	
	void setPlaylists(){
		List<int> lib = Playlist.getAllIds();
		
		TuiButton create = new TuiButton("Create playlist", Placement.TopRight, 3, 5, null, Palette.user);
		
		create.SetAction((s, ck) => {
			setPlaylistDetails(Playlist.create("Untitled playlist"));
		});
		
		TuiButton import = new TuiButton("Import playlist from folder", Placement.TopRight, 3, 7, null, Palette.user);
		TuiButton importYt = new TuiButton("Import playlist from yt", Placement.TopRight, 3, 9, null, Palette.user);
		
		import.SetAction((s, ck) => {
			setImportFolderPlaylist();
		});
		
		importYt.SetAction((s, ck) => {
			setImportPlaylist();
		});
		
		TuiSelectable[,] t = new TuiSelectable[Math.Max(lib.Count, 3), 2];
		
		t[0, 1] = create;
		t[1, 1] = import;
		t[2, 1] = importYt;
		
		string[] titles = new string[lib.Count];
		
		for(int i = 0; i < lib.Count; i++){
			Playlist s = Playlist.load(lib[i]);
			titles[i] = s?.title ?? "Untitled playlist";
			TuiButton b = new TuiButton("", Placement.TopLeft, 2, i + 4, Palette.playlist, Palette.user);
			
			b.SetAction((s2, ck) => {
				setPlaylistDetails(s);
			});
			
			b.SubKeyEvent(ConsoleKey.S, (s2, ck) => {
				Session.setSource(SourceType.Playlist, s.id);
			});
			
			t[i, 0] = b;
		}
		
		TuiScreenInteractive l = getMiddle(t);
		
		l.MatrixPointerX = (uint) ((lib?.Count > 0) ? 0 : 1);
		
		l.Elements.Add(new TuiLabel("Playlists", Placement.TopCenter, 0, 1, Palette.main));
		
		if(lib.Count == 0){
			l.Elements.Add(new TuiLabel("No playlists", Placement.TopLeft, 2, 4));
		}
		
		l.DeleteAllKeyEvents();
		
		void update(){
			int j = 0;
			for(int i = 0; i < l.Elements.Count; i++){
				TuiElement e = l.Elements[i];
				if(!(e is TuiButton b) || e == create || e == import || e == importYt){
					continue;
				}
				if(e.OffsetY > 3 && e.OffsetY < l.Ysize - 2){
					b.Text = titles[j];
				}else{
					b.Text = "";
				}
				j++;
			}
		}
		
		update();
		
		l.SubKeyEvent(ConsoleKey.UpArrow, (s, ck) => { //Scroll
			TuiScreenInteractive.MoveUp(s, ck);
			if(s.Selected?.OffsetY < 4){
				foreach(TuiElement e in s){
					if(e is TuiButton && e != create && e != import && e != importYt){
						e.OffsetY++;	
					}
				}
			}
			update();
		});
		
		l.SubKeyEvent(ConsoleKey.DownArrow, (s, ck) => { //Scroll
			TuiScreenInteractive.MoveDown(s, ck);
			if(s.Selected?.OffsetY >= s.Ysize - 2){
				foreach(TuiElement e in s){
					if(e is TuiButton && e != create && e != import && e != importYt){
						e.OffsetY--;	
					}
				}
			}
			update();
		});
		
		l.SubKeyEvent(ConsoleKey.RightArrow, (s, ck) => {
			s.MatrixPointerY = 0;
			s.MatrixPointerX = 1;
			int j = 0;
			foreach(TuiElement e in s){
				if(e is TuiButton && e != create && e != import && e != importYt){
					e.OffsetY = j + 4;
					j++;
				}
			}
			update();
		});
		
		l.SubKeyEvent(ConsoleKey.LeftArrow, (s, ck) => {
			TuiScreenInteractive.MoveLeft(s, ck);
		});
		
		l.OnResize += (s, a) => {
			update();
		};
		
		void onPlaylistsChange(object sender, EventArgs a){
			if(!middle.Contains(l)){
				Playlist.onPlaylistUpdate -= onPlaylistsChange;
				return;
			}
			
			Stack<TuiScreenInteractive> tempMid = new();
			while(middle.Count > 0){
				tempMid.Push(middle.Peek());
				closeMiddleScreen();
				if(tempMid.Peek() == l){
					tempMid.Pop();
					setPlaylists();
					while(tempMid.Count > 0){
						setMiddleScreen(tempMid.Pop());
					}
					
					Playlist.onPlaylistUpdate -= onPlaylistsChange;
					return;
				}
			}
			
			while(tempMid.Count > 0){
				setMiddleScreen(tempMid.Pop());
			}
		}
		
		Playlist.onPlaylistUpdate += onPlaylistsChange;
		
		setMiddleScreen(l);
	}
	
	void selectSongToAdd(Playlist p){
		List<int> lib = Song.getLibrary();
		TuiSelectable[,] t = new TuiSelectable[lib.Count, 1];
		
		string[] titles = new string[lib.Count];
		
		for(int i = 0; i < lib.Count; i++){
			Song s = Song.load(lib[i]);
			titles[i] = s?.title ?? "Untitled song";
			TuiButton b = new TuiButton("", Placement.TopLeft, 2, i + 4, Palette.song, Palette.user);
			
			b.SetAction((s2, ck) => {
				p.addSong(s.id);
				closeMiddleScreen();
			});
			
			t[i, 0] = b;
		}
		
		TuiScreenInteractive l = getMiddle(t);
		
		l.Elements.Add(new TuiTwoLabels("Select song to add to ", p?.title ?? "Untitled playlist", Placement.TopCenter, 0, 1, null, Palette.playlist));
		l.Elements.Add(new TuiTwoLabels("F", " Search", Placement.BottomRight, 0, 0, Palette.info, null));
		
		l.DeleteAllKeyEvents();
		
		void update(){
			for(int i = 0; i < l.Elements.Count; i++){
				TuiElement e = l.Elements[i];
				if(!(e is TuiButton b)){
					continue;
				}
				if(e.OffsetY > 3 && e.OffsetY < l.Ysize - 2){
					b.Text = titles[i - 1];
				}else{
					b.Text = "";
				}
			}
		}
		
		update();
		
		l.SubKeyEvent(ConsoleKey.UpArrow, (s, ck) => { //Scroll
			TuiScreenInteractive.MoveUp(s, ck);
			if(s.Selected?.OffsetY < 4){
				foreach(TuiElement e in s){
					if(e is TuiButton){
						e.OffsetY++;	
					}
				}
			}
			update();
		});
		
		l.SubKeyEvent(ConsoleKey.DownArrow, (s, ck) => { //Scroll
			TuiScreenInteractive.MoveDown(s, ck);
			if(s.Selected?.OffsetY >= s.Ysize - 2){
				foreach(TuiElement e in s){
					if(e is TuiButton){
						e.OffsetY--;	
					}
				}
			}
			update();
		});
		
		l.SubKeyEvent(ConsoleKey.F, (s, ck) => {
			closeMiddleScreen();
			setSearchPlaylist(p);
		});
		
		l.OnResize += (s, a) => {
			update();
		};
		
		setMiddleScreen(l);
	}
	
	void setSearchPlaylist(Playlist p){
		TuiMultiLineScrollingFramedTextBox input = new TuiMultiLineScrollingFramedTextBox("", 256, 34, 3, Placement.TopCenter, 0, 4, null, null, null, Palette.user, Palette.user);
		
		input.OnParentResize += (s, a) => {
			input.BoxXsize = Math.Max(0, a.X - 4);
		};
		
		input.SubKeyEvent(ConsoleKey.Enter, (s, ck) => {
			closeMiddleScreen();
			setAddPlaylistSearch(input.Text, p);
		});
		
		TuiSelectable[,] t = new TuiSelectable[,]{{
			input
		}};
		
		TuiScreenInteractive l = getMiddle(t);
		
		l.Elements.Add(new TuiLabel("Search", Placement.TopCenter, 0, 1, Palette.main));
		
		setMiddleScreen(l);
	}
	
	void setAddPlaylistSearch(string query, Playlist p){
		List<Song> lib = Song.getLibrary().Select(n => Song.load(n)).Where(n => n != null && n.title.Contains(query, StringComparison.OrdinalIgnoreCase)).ToList();
		TuiSelectable[,] t = new TuiSelectable[lib.Count, 1];
		
		string[] titles = new string[lib.Count];
		
		for(int i = 0; i < lib.Count; i++){
			Song s = lib[i];
			titles[i] = s?.title ?? "Untitled song";
			TuiButton b = new TuiButton("", Placement.TopLeft, 2, i + 4, Palette.song, Palette.user);
			
			b.SetAction((s2, ck) => {
				p.addSong(s.id);
				closeMiddleScreen();
			});
			
			t[i, 0] = b;
		}
		
		TuiScreenInteractive l = getMiddle(t);
		
		l.Elements.Add(new TuiTwoLabels("Search results for: ", query, Placement.TopCenter, 0, 1, null, Palette.info));
		l.Elements.Add(new TuiTwoLabels("Select song to add to ", p?.title ?? "Untitled playlist", Placement.TopCenter, 0, 2, null, Palette.playlist));
		
		l.DeleteAllKeyEvents();
		
		void update(){
			for(int i = 0; i < l.Elements.Count; i++){
				TuiElement e = l.Elements[i];
				if(!(e is TuiButton b)){
					continue;
				}
				if(e.OffsetY > 3 && e.OffsetY < l.Ysize - 2){
					b.Text = titles[i - 1];
				}else{
					b.Text = "";
				}
			}
		}
		
		update();
		
		l.SubKeyEvent(ConsoleKey.UpArrow, (s, ck) => { //Scroll
			TuiScreenInteractive.MoveUp(s, ck);
			if(s.Selected?.OffsetY < 4){
				foreach(TuiElement e in s){
					if(e is TuiButton){
						e.OffsetY++;	
					}
				}
			}
			update();
		});
		
		l.SubKeyEvent(ConsoleKey.DownArrow, (s, ck) => { //Scroll
			TuiScreenInteractive.MoveDown(s, ck);
			if(s.Selected?.OffsetY >= s.Ysize - 2){
				foreach(TuiElement e in s){
					if(e is TuiButton){
						e.OffsetY--;	
					}
				}
			}
			update();
		});
		
		l.OnResize += (s, a) => {
			update();
		};
		
		void onLibChange(object sender, LibraryEventArgs a){
			if(!middle.Contains(l)){
				Song.onLibraryUpdate -= onLibChange;
				return;
			}
			
			Stack<TuiScreenInteractive> tempMid = new();
			while(middle.Count > 0){
				tempMid.Push(middle.Peek());
				closeMiddleScreen();
				if(tempMid.Peek() == l){
					tempMid.Pop();
					setAddPlaylistSearch(query, p);
					while(tempMid.Count > 0){
						setMiddleScreen(tempMid.Pop());
					}
					
					Song.onLibraryUpdate -= onLibChange;
					return;
				}
			}
			
			while(tempMid.Count > 0){
				setMiddleScreen(tempMid.Pop());
			}
		}
		
		Song.onLibraryUpdate += onLibChange;
		
		setMiddleScreen(l);
	}
	
	void selectPlaylistToAddTo(int sindex){
		List<int> lib = Playlist.getAllIds();
		TuiSelectable[,] t = new TuiSelectable[lib.Count, 1];
		
		string[] titles = new string[lib.Count];
		
		for(int i = 0; i < lib.Count; i++){
			Playlist s = Playlist.load(lib[i]);
			titles[i] = s?.title ?? "Untitled playlist";
			TuiButton b = new TuiButton("", Placement.TopLeft, 2, i + 4, Palette.playlist, Palette.user);
			
			b.SetAction((s2, ck) => {
				s.addSong(sindex);
				closeMiddleScreen();
			});
			
			t[i, 0] = b;
		}
		
		TuiScreenInteractive l = getMiddle(t);
		
		l.Elements.Add(new TuiTwoLabels("Select playlist where to add ", Song.load(sindex)?.title ?? "Untitled song", Placement.TopCenter, 0, 1, null, Palette.song));
		
		l.DeleteAllKeyEvents();
		
		void update(){
			for(int i = 0; i < l.Elements.Count; i++){
				TuiElement e = l.Elements[i];
				if(!(e is TuiButton b)){
					continue;
				}
				if(e.OffsetY > 3 && e.OffsetY < l.Ysize - 2){
					b.Text = titles[i - 1];
				}else{
					b.Text = "";
				}
			}
		}
		
		update();
		
		l.SubKeyEvent(ConsoleKey.UpArrow, (s, ck) => { //Scroll
			TuiScreenInteractive.MoveUp(s, ck);
			if(s.Selected?.OffsetY < 4){
				foreach(TuiElement e in s){
					if(e is TuiButton){
						e.OffsetY++;	
					}
				}
			}
			update();
		});
		
		l.SubKeyEvent(ConsoleKey.DownArrow, (s, ck) => { //Scroll
			TuiScreenInteractive.MoveDown(s, ck);
			if(s.Selected?.OffsetY >= s.Ysize - 2){
				foreach(TuiElement e in s){
					if(e is TuiButton){
						e.OffsetY--;	
					}
				}
			}
			update();
		});
		
		l.OnResize += (s, a) => {
			update();
		};
		
		setMiddleScreen(l);
	}
	
	void confirmDeleteSong(int sindex){
		TuiSelectable[,] buttons = {{
			new TuiButton("Yes", Placement.Center, -4, 1, null, Palette.user).SetAction((s, ck) => {
				closeMiddleScreen();
				closeMiddleScreen();
				
				Song.delete(sindex);
			}),
			new TuiButton("No", Placement.Center, 4, 1, null, Palette.user).SetAction((s, ck) => closeMiddleScreen())
		}};
		
		TuiScreenInteractive t = getMiddle(buttons);
		t.Elements.Add(new TuiLabel("Do you want to delete?", Placement.Center, 0, -1));
		t.Elements.Add(new TuiFrame(26, 7, Placement.Center, 0, 0, Palette.user));
		
		setMiddleScreen(t);
		
		return;
	}
	
	void confirmDeletePlaylist(int sindex){
		TuiSelectable[,] buttons = {{
			new TuiButton("Yes", Placement.Center, -4, 1, null, Palette.user).SetAction((s, ck) => {
				closeMiddleScreen();
				closeMiddleScreen();
				
				Playlist.delete(sindex);
			}),
			new TuiButton("No", Placement.Center, 4, 1, null, Palette.user).SetAction((s, ck) => closeMiddleScreen())
		}};
		
		TuiScreenInteractive t = getMiddle(buttons);
		t.Elements.Add(new TuiLabel("Do you want to delete?", Placement.Center, 0, -1));
		t.Elements.Add(new TuiFrame(26, 7, Placement.Center, 0, 0, Palette.user));
		
		setMiddleScreen(t);
		
		return;
	}
}