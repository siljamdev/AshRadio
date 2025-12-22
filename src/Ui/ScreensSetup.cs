using System.Diagnostics;
using System.IO;
using AshLib.Formatting;
using AshConsoleGraphics;
using AshConsoleGraphics.Interactive;

public partial class Screens{
	MultipleTuiScreenInteractive master = null!;
	TuiScreenInteractive playing = null!;
	TuiNumberPicker volume = null!;
	
	TuiScreenInteractive session = null!;
	TuiOptionPicker mode = null!;
	TuiScreenInteractive navigation = null!;
	TuiScrollingScreenInteractive queueScreen = null!;
	
	public Screens(){
		setupPlaying();
		setupSession();
		setupNavigation();
		
		TuiHorizontalLine d1 = new TuiHorizontalLine(100, 'a', Placement.BottomCenter, 0, 5);
		TuiVerticalLine d2 = new TuiVerticalLine(14, 'a', Placement.TopRight, 30, 0);
		TuiVerticalLine d3 = new TuiVerticalLine(14, 'a', Placement.TopLeft, 30, 0);
		
		d1.OnParentResize += (s, a) => {
			d1.Xsize = a.X;
			d1.OffsetY = Math.Min(a.Y, 5);
		};
		
		d2.OnParentResize += (s, a) => {
			d2.Ysize = Math.Max(a.Y - 6, 0);
			d2.OffsetX = Math.Min(a.X, 30);
		};
		
		d3.OnParentResize += (s, a) => {
			d3.Ysize = Math.Max(a.Y - 6, 0);
			d3.OffsetX = Math.Min(a.X, 30);
		};
		
		TuiConnectedLinesScreen delimiters = new TuiConnectedLinesScreen(100, 20, new ILineElement[]{d1, d2, d3}, Palette.delimiter);
		
		delimiters.OnParentResize += (s, a) => {
			delimiters.Xsize = a.X;
			delimiters.Ysize = a.Y;
		};
		
		TuiSelectable[,] temp = new TuiSelectable[,]{{
			new TuiButton("Open GitHub repo", Placement.BottomCenter, 0, 4, null, Palette.user).SetAction((s, ck) => {
				openUrl("https://github.com/siljamdev/AshRadio");
			})
		},{
			new TuiButton("Open Help menu", Placement.BottomCenter, 0, 2, null, Palette.user).SetAction((s, ck) => {
				setHelp();
			})
		}};
		
		//Initial empty screen
		MiddleScreen mid = generateMiddle(temp);
		
		mid.interactive.Elements.Add(new TuiTwoLabels("Welcome to ", "AshRadio", Placement.Center, 0, -5, null, Palette.main));
		
		middle.Add(mid);
		
		//Creating master
		master = new MultipleTuiScreenInteractive(100, 20, new TuiScreenInteractive[]{playing, session, navigation, mid.interactive}, null, delimiters);
		
		master.ScreenList.Add(queueScreen);
		
		master.AutoResize = true;
		
		Console.CursorVisible = false;
		
		master.OnResize += (s, a) => Console.CursorVisible = false;
		
		master.SubKeyEvent(ConsoleKey.Spacebar, ConsoleModifiers.Control, (s, cki) => {
			setSelectedScreen(playing);
		});
		
		master.SubKeyEvent(ConsoleKey.S, ConsoleModifiers.Control, (s, cki) => {
			setSelectedScreen(session);
		});
		
		master.SubKeyEvent(ConsoleKey.N, ConsoleModifiers.Control, (s, cki) => {
			setSelectedScreen(navigation);
		});
		
		master.SubKeyEvent(ConsoleKey.G, ConsoleModifiers.Control, (s, cki) => {
			setSelectedScreen(currentMiddleScreen);
		});
		
		master.SubKeyEvent(ConsoleKey.Escape, (s, cki) => { //Close middle
			closeMiddleScreen();
		});
		
		master.SubKeyEvent(ConsoleKey.Spacebar, ConsoleModifiers.None, (s, cki) => {
			Radio.py.togglePause();
		});
		
		master.SubKeyEvent(ConsoleKey.K, ConsoleModifiers.None, (s, cki) => {
			Radio.py.togglePause();
		});
		
		master.SubKeyEvent(ConsoleKey.N, ConsoleModifiers.None, (s, cki) => {
			int j = Session.getPrevious(Radio.py.playingSong);
			if(j < 0){
				return;
			}
			Session.addToPrevList = false;
			Radio.py.play(j);
			Session.addToPrevList = true;
		});
		
		master.SubKeyEvent(ConsoleKey.M, ConsoleModifiers.None, (s, cki) => {
			Radio.py.skip();
		});
		
		master.SubKeyEvent(ConsoleKey.J, ConsoleModifiers.None, (s, cki) => {
			Radio.py.elapsed -= Radio.config.GetValue<float>("player.advanceTime");
		});
		
		master.SubKeyEvent(ConsoleKey.J, ConsoleModifiers.Shift, (s, cki) => {
			Radio.py.elapsed = 0f;
		});
		
		master.SubKeyEvent(ConsoleKey.L, ConsoleModifiers.None, (s, cki) => {
			Radio.py.elapsed += Radio.config.GetValue<float>("player.advanceTime");
		});
		
		master.SubKeyEvent(ConsoleKey.OemMinus, (s, cki) => {
			volume.NumberDown(volume, cki);
			Radio.py.setVolume(volume.Number);
		});
		
		master.SubKeyEvent(ConsoleKey.OemPlus, (s, cki) => {
			volume.NumberUp(volume, cki);
			Radio.py.setVolume(volume.Number);
		});
		
		master.SubKeyEvent(ConsoleKey.F1, ConsoleModifiers.None, (s, cki) => {
			setHelp();
		});
		
		master.SubKeyEvent(ConsoleKey.L, ConsoleModifiers.Control, (s, cki) => {
			setLibrary();
		});
		
		master.SubKeyEvent(ConsoleKey.P, ConsoleModifiers.Control, (s, cki) => {
			setPlaylists();
		});
		
		master.SubKeyEvent(ConsoleKey.U, ConsoleModifiers.Control, (s, cki) => {
			setAuthors();
		});
		
		master.SubKeyEvent(ConsoleKey.M, ConsoleModifiers.Shift, (s, cki) => {
			if(mode.SelectedOptionIndex == 2){
				mode.SelectedOptionIndex = 0;
			}else{
				mode.SelectedOptionIndex++;
			}
			Session.setMode((SessionMode) mode.SelectedOptionIndex);
		});
		
		master.SubKeyEvent(ConsoleKey.S, ConsoleModifiers.Shift, (s, cki) => {
			switch(Session.sourceType){
				case SourceType.Library:
					setLibrary();
					break;
				
				case SourceType.Author:
					setAuthorDetails(Session.sourceIdentifier);
					break;
				
				case SourceType.Playlist:
					setPlaylistDetails(Session.sourceIdentifier);
					break;
			}
		});
		
		master.SubKeyEvent(ConsoleKey.Spacebar, ConsoleModifiers.Shift, (s, cki) => {
			if(Radio.py.playingSong >= 0){
				setSongDetails(Radio.py.playingSong);
			}
		});
		
		Stopwatch timer = Stopwatch.StartNew();
		
		int maxFps = 32;
		double dt = 1000d / maxFps;
		
		master.OnFinishPlayCycle += (s, a) => { //Wait some time to avoid enourmus cpu usage
			double st = timer.Elapsed.TotalMilliseconds;
			while(true){
				if(Console.KeyAvailable){
					ConsoleKeyInfo k = Console.ReadKey(true);
					master.HandleKey(k);
					break;
				}
				
				if(timer.Elapsed.TotalMilliseconds - st >= dt){
					break;
				}
				
				Thread.Sleep(1);
			}
		};
		
		setSelectedScreen(navigation);
	}
	
	//Method to start it all
	public void play(){
		master.Play();
	}
	
	public void setupPlaying(){
		Song temp = Song.get(Radio.py.playingSong);
		TuiButton song = new TuiButton(crop(temp?.title ?? "", 38), Placement.TopLeft, 11, 1, Palette.song, Palette.user).SetAction((s, ck) => {
			if(Radio.py.playingSong >= 0){
				setSongDetails(Radio.py.playingSong);
			}
		});
		
		TuiTwoLabels authors = new TuiTwoLabels("Authors: ", temp == null ? "" : (temp.authors.Length == 0 ? "Unknown author" : (temp.authors.Length == 1 ? (Author.get(temp.authors[0])?.name ?? "Unknown author") : string.Join(", ", temp.authors.Select(n => (Author.get(n)?.name ?? "Unknown author"))))), Placement.BottomLeft, 2, 1, null, Palette.author);
		
		TuiProgressBar progress = new TuiProgressBar(70, '█', '░', Placement.Center, 0, 0, Palette.main, Palette.main);
		
		progress.OnParentResize += (s, a) => {
			progress.Xsize = Math.Max(a.X - 30, 0);
		};
		
		TuiLabel elapsedTime = new TuiLabel("0:00", Placement.Center, -39, 0, Palette.info);
		TuiLabel totalTime = new TuiLabel("0:00", Placement.Center, 39, 0, Palette.info);
		
		elapsedTime.OnParentResize += (s, a) => {
			elapsedTime.OffsetX = -(a.X - 30)/2 - 4;
		};
		
		totalTime.OnParentResize += (s, a) => {
			totalTime.OffsetX = (a.X - 30)/2 + 4;
		};
		
		int sec = Math.Max((int) Radio.py.duration, 0);
		totalTime.Text = (sec / 60) + ":" + (sec % 60).ToString("D2");
		
		TuiButton play = new TuiButton("‖", Placement.TopCenter, 0, 1, null, Palette.user); //► or ‖
		play.SetAction((s, cki) => {
			Radio.py.togglePause();
		});
		
		TuiButton prev = new TuiButton("≤", Placement.TopCenter, -6, 1, null, Palette.user).SetAction((s, cki) => {
			int j = Session.getPrevious(Radio.py.playingSong);
			if(j < 0){
				return;
			}
			Session.addToPrevList = false;
			Radio.py.play(j);
			Session.addToPrevList = true;
		});
		TuiButton next = new TuiButton("≥", Placement.TopCenter, 6, 1, null, Palette.user).SetAction((s, cki) => Radio.py.skip());
		
		TuiButton back = new TuiButton("▼", Placement.TopCenter, -12, 1, null, Palette.user).SetAction((s, cki) => Radio.py.elapsed -= Radio.config.GetValue<float>("player.advanceTime"));
		TuiButton advance = new TuiButton("▲", Placement.TopCenter, 12, 1, null, Palette.user).SetAction((s, cki) => Radio.py.elapsed += Radio.config.GetValue<float>("player.advanceTime"));
		
		volume = new TuiNumberPicker(0, 100, 2, Radio.py.volume, Placement.Center, 3, 2, Palette.info, Palette.user);
		
		volume.DeleteAllKeyEvents();
		volume.SubKeyEvent(ConsoleKey.LeftArrow, (s, cki) => {
			volume.NumberDown(s, cki);
			Radio.py.setVolume(volume.Number);
		});
		volume.SubKeyEvent(ConsoleKey.RightArrow, (s, cki) => {
			volume.NumberUp(s, cki);
			Radio.py.setVolume(volume.Number);
		});
		
		playing = new TuiScreenInteractive(100, 5, new TuiSelectable[,]{{
			song, back, prev, play, next, advance
		}, {
			volume, volume, volume, volume, volume, volume
		}}, 3, 0, Placement.BottomCenter, 0, 0, null,
			new TuiLabel("Playing:", Placement.TopLeft, 2, 1),
			new TuiLabel("Authors:", Placement.BottomLeft, 2, 1),
			new TuiLabel("v" + Radio.version, Placement.BottomRight, 0, 0, Palette.delimiter),
			new TuiLabel("Volume", Placement.Center, -2, 2),
			new TuiLabel("Ctrl+Space", Placement.TopRight, 0, 0, Palette.hint),
			authors,
			progress, elapsedTime, totalTime
		);
		
		playing.OnParentResize += (s, a) => {
			playing.Xsize = a.X;
			playing.Ysize = Math.Min(a.Y, 5);
			
			song.Text = crop(Song.get(Radio.py.playingSong)?.title ?? "", playing.Xsize / 2 - 25);
		};
		
		playing.OnFinishPlayCycle += (s, a) => {
			int n = (int) (Math.Max(Radio.py.elapsed, 0f) / Math.Max(Radio.py.duration, 0f) * 100);
			if(n != progress.Percentage){
				progress.Percentage = n;
			}
			
			int sec = Math.Max((int) Radio.py.elapsed, 0);
			string s2 = (sec / 60) + ":" + (sec % 60).ToString("D2");
			if(s2 != elapsedTime.Text){
				elapsedTime.Text = s2;
			}
		};
		
		prepareScreen(playing);
		
		Radio.py.onSongLoad += (se, e) => {
			Song s = Song.get(Radio.py.playingSong);
			
			song.Text = crop(s?.title ?? "", playing.Xsize / 2 - 25);
			authors.RightText = s == null ? "" : (s.authors.Length == 0 ? "Unknown author" : (s.authors.Length == 1 ? (Author.get(s.authors[0])?.name ?? "Unknown author") : string.Join(", ", s.authors.Select(n => (Author.get(n)?.name ?? "Unknown author")))));
			
			int sec = Math.Max((int) Radio.py.duration, 0);
			totalTime.Text = (sec / 60) + ":" + (sec % 60).ToString("D2");
		};
		
		Radio.py.onChangePlaystate += (se, e) => {
			play.Text = Radio.py.isPaused ? "‖" : "►";
		};
	}
	
	public void setupSession(){
		TuiLabel device = new TuiLabel(Radio.py.currentDevice.Name ?? "", Placement.BottomLeft, 1, 2, Palette.info);
		
		TuiButton devices = new TuiButton("Change device", Placement.BottomCenter, 0, 1, null, Palette.user).SetAction((s, cki) => {
			var devs = Player.getDeviceList().ToList();
			
			TuiSelectable[,] buttons = new TuiSelectable[devs.Count, 1];
			
			for(int i = 0; i < devs.Count; i++){
				int j = devs[i].Value;
				buttons[i, 0] = new TuiButton(devs[i].Key, Placement.TopCenter, 0, 6 + i, null, Palette.user).SetAction((s, cki) => {
					Radio.py.setDevice(j);
					closeMiddleScreen();
				});
			}
			
			MiddleScreen changeDevice = generateMiddle(buttons);
			setMiddleScreen(changeDevice);
		});
		
		string name = "";
		string name2 = "";
		CharFormat? f = null;
		switch(Session.sourceType){
			default:
				break;
			case SourceType.Library:
				name = "Library";
				f = Palette.info;
				break;
			case SourceType.Author:
				name = Author.get(Session.sourceIdentifier)?.name ?? "Unknown author";
				f = Palette.author;
				break;
			case SourceType.Playlist:
				name = Playlist.get(Session.sourceIdentifier)?.title ?? "Untitled playlist";
				f = Palette.playlist;
				break;
		}
		
		TuiTwoLabels sourceType = new TuiTwoLabels("Source: ", name, Placement.TopLeft, 1, 3, null, f);
		
		Session.onSourceChange += (s, a) => {
			string nam = "";
			string nam2 = "";
			CharFormat? f2 = null;
			switch(Session.sourceType){
				default:
					break;
				case SourceType.Library:
					nam = "Library";
					f2 = Palette.info;
					break;
				case SourceType.Author:
					nam = Author.get(Session.sourceIdentifier)?.name ?? "Unknown author";
					f2 = Palette.author;
					break;
				case SourceType.Playlist:
					nam = Playlist.get(Session.sourceIdentifier)?.title ?? "Untitled playlist";
					f2 = Palette.playlist;
					break;
			}
			
			sourceType.RightText = nam;
			sourceType.RightFormat = f2;
		};
		
		mode = new TuiOptionPicker(new string[]{"Order", "Shuffle", "Smart Shuffle"}, (uint) ((int) Session.mode), Placement.TopLeft, 7, 5, Palette.info, Palette.user);
		
		mode.DeleteAllKeyEvents();
		
		mode.SubKeyEvent(ConsoleKey.LeftArrow, (s, cki) => {
			if(mode.SelectedOptionIndex == 0){
				mode.SelectedOptionIndex = 2;
			}else{
				mode.SelectedOptionIndex--;
			}
			
			Session.setMode((SessionMode) mode.SelectedOptionIndex);
		});
		
		mode.SubKeyEvent(ConsoleKey.RightArrow, (s, cki) => {
			if(mode.SelectedOptionIndex == 2){
				mode.SelectedOptionIndex = 0;
			}else{
				mode.SelectedOptionIndex++;
			}
			Session.setMode((SessionMode) mode.SelectedOptionIndex);
		});
		
		TuiButton modifyQueue = new TuiButton("Modify queue", Placement.BottomLeft, 3, 6, null, Palette.user).SetAction((s, cki) => {
			if(Session.getQueue().Count > 0){
				setSelectedScreen(queueScreen);
			}
		});
		
		TuiFramedCheckBox emptyQueue = new TuiFramedCheckBox(' ', 'X', Session.queueEmpties, Placement.BottomLeft, 18, 3, null, null, null, Palette.user, Palette.user);
		
		emptyQueue.DeleteAllKeyEvents();
		
		emptyQueue.SubKeyEvent(ConsoleKey.Enter, (s, ck) => {
			emptyQueue.Checked = !emptyQueue.Checked;
			Session.queueEmpties = emptyQueue.Checked;
		});
		
		session = new TuiScreenInteractive(30, 14, new TuiSelectable[,]{{
			mode
		},{
			modifyQueue
		},{
			emptyQueue
		},{
			devices
		}}, 0, 0, Placement.TopRight, 0, 0, null,
			device,
			sourceType,
			new TuiLabel("Mode:", Placement.TopLeft, 1, 5),
			new TuiLabel("Queue:", Placement.TopLeft, 1, 7),
			new TuiLabel("Queue empties:", Placement.BottomLeft, 3, 4),
			new TuiLabel("Session", Placement.TopCenter, 0, 1, Palette.main),
			new TuiLabel("Ctrl+S", Placement.BottomLeft, 0, 0, Palette.hint)
		);
		
		void updateQueueScreen(){
			bool b = master?.SelectedScreen == queueScreen;
			int n = 0;
			if(b){
				n = queueScreen?.Elements.IndexOf(queueScreen.Selected) ?? 0;
			}
			
			session.Elements.Remove(queueScreen);
			master?.ScreenList.Remove(queueScreen);
			
			List<int> queue = Session.getQueue();
			TuiSelectable[,] temp = null;
			if(queue.Count != 0){
				temp = new TuiSelectable[queue.Count, 1];
				
				for(int i = 0; i < queue.Count; i++){
					Song s = Song.get(queue[i]);
					TuiButton t2 = new TuiButton(s?.title ?? "Untitled song", Placement.TopLeft, 0, i, Palette.song, Palette.user);
					
					t2.SetAction((s, cl) => {
						int myIndex = queueScreen.Elements.IndexOf(t2);
						setSongDetails(queue[myIndex]);
					});
					
					t2.SubKeyEvent(ConsoleKey.R, (s, ck) => {
						int myIndex = queueScreen.Elements.IndexOf(t2);
						Session.removeFromQueue(myIndex);
					});
					
					t2.SubKeyEvent(ConsoleKey.N, (s, ck) => {
						int myIndex = queueScreen.Elements.IndexOf(t2);
						if(myIndex != 0){
							TuiScreenInteractive.MoveUp(queueScreen, ck); //Align properly with the change
							Session.moveInQueue(myIndex, myIndex - 1);
						}
					});
					
					t2.SubKeyEvent(ConsoleKey.M, (s, ck) => {
						int myIndex = queueScreen.Elements.IndexOf(t2);
						if(myIndex != queueScreen.Elements.Count - 1){
							TuiScreenInteractive.MoveDown(queueScreen, ck); //Align properly with the change
							Session.moveInQueue(myIndex, myIndex + 1);
						}
					});
					
					temp[i, 0] = t2;
				}
			}
			
			queueScreen = new TuiScrollingScreenInteractive(Math.Max(0, session.Xsize - 4), Math.Max(0, session.Ysize - 15), temp, 0, (uint) n, Placement.TopLeft, 2, 8, null);
			
			prepareScreen(queueScreen);
			
			if(queue.Count == 0){
				queueScreen.Elements.Add(new TuiLabel("Empty", Placement.TopCenter, 0, 0, Palette.info));
			}
			
			queueScreen.OnParentResize += (s, a) => {
				queueScreen.Xsize = Math.Max(0, a.X - 4);
				queueScreen.Ysize = Math.Max(0, a.Y - 15);
			};
			
			queueScreen.SubKeyEvent(ConsoleKey.Escape, (s, ck) => {
				setSelectedScreen(session);
			});
			
			session.Elements.Add(queueScreen);
			master?.ScreenList.Add(queueScreen);
			
			if(b && master != null){
				if(queue.Count > 0){
					setSelectedScreen(queueScreen); //If the update is in real time, update
				}else{
					setSelectedScreen(session);
				}
			}
		}
		
		updateQueueScreen();
		
		Song.onLibraryUpdate += (s, a) => updateQueueScreen();
		
		Session.onQueueChange += (s, a) => updateQueueScreen();
		
		session.OnParentResize += (s, a) => {
			session.Xsize = Math.Min(30, a.X);
			session.Ysize = Math.Max(a.Y - 6, 0);
		};
		
		session.SubKeyEvent(ConsoleKey.Q, (s, ck) => {
			if(Session.getQueue().Count > 0){
				setSelectedScreen(queueScreen);
			}
		});
		
		prepareScreen(session);
		
		Radio.py.onChangeDevice += (s, a) => {
			device.Text = Radio.py.currentDevice.Name;
		};
	}
	
	void setupNavigation(){
		TuiButton lib = new TuiButton("Library", Placement.TopLeft, 2, 5, null, Palette.user).SetAction((s, ck) => {
			setLibrary();
		});
		
		TuiButton plyl = new TuiButton("Playlists", Placement.TopLeft, 2, 7, null, Palette.user).SetAction((s, ck) => {
			setPlaylists();
		});
		
		TuiButton aut = new TuiButton("Authors", Placement.TopLeft, 2, 9, null, Palette.user).SetAction((s, ck) => {
			setAuthors();
		});
		
		TuiButton imp = new TuiButton("Import songs", Placement.TopLeft, 2, 13, null, Palette.user).SetAction((s, ck) => {
			setImport();
		});
		
		TuiButton help = new TuiButton("Help", Placement.BottomCenter, 0, 3, null, Palette.user).SetAction((s, ck) => {
			setHelp();
		});
		
		TuiButton config = new TuiButton("Config", Placement.BottomCenter, 0, 1, null, Palette.user).SetAction((s, ck) => {
			setConfig();
		});
		
		navigation = new TuiScreenInteractive(30, 14, new TuiSelectable[,]{{
			lib
		},{
			plyl
		},{
			aut
		},{
			imp
		},{
			help
		},{
			config
		}}, 0, 0, Placement.TopLeft, 0, 0, null,
			new TuiLabel("Navigation", Placement.TopCenter, 0, 1, Palette.main),
			new TuiLabel("Ctrl+N", Placement.BottomRight, 0, 0, Palette.hint)
		);
		
		navigation.OnParentResize += (s, a) => {
			navigation.Xsize = Math.Min(30, a.X);
			navigation.Ysize = Math.Max(a.Y - 6, 0);
		};
		
		prepareScreen(navigation);
	}
	
	void setHelp(int page = 0){
		const int maxPage = 7;
		MiddleScreen l3 = generateMiddle(null);
		
		//Juto to avoid rewriting a lot of code
		TuiScreenInteractive l = l3.interactive;
		
		l.Elements.Add(new TuiLabel("Help - Page " + (page + 1), Placement.TopCenter, 0, 1, Palette.main));
		
		if(page > 0){
			l.Elements.Add(new TuiTwoLabels("N", " Previous page", Placement.BottomRight, 0, 1, Palette.info, null));
		}
		
		if(page < maxPage){
			l.Elements.Add(new TuiTwoLabels("M", " Next page", Placement.BottomRight, 0, 0, Palette.info, null));
		}
		
		l.SubKeyEvent(ConsoleKey.N, (s, ck) => {
			if(page != 0){
				setHelp(page - 1);
				removeMiddleScreen(l3);
			}
		});
		
		l.SubKeyEvent(ConsoleKey.M, (s, ck) => {
			if(page != maxPage){
				setHelp(page + 1);
				removeMiddleScreen(l3);
			}
		});
		
		List<TuiElement> pageSpecific = new();
		
		void update(){
			l.Elements.RemoveAll(h => pageSpecific.Contains(h));
			
			pageSpecific = page switch{
				0 =>
					generateHelpPageElements(
						"Concepts",
						new (string, CharFormat?)[][]{
							new (string, CharFormat?)[]{("Session", Palette.info), (": the current options for source, order and queue", null)},
							new (string, CharFormat?)[]{("Source", Palette.info), (": the 'pool' from where the next song will be chosen", null)},
							new (string, CharFormat?)[]{("Library", Palette.info), (": the whole collection of songs", null)},
							new (string, CharFormat?)[]{("Order", Palette.info), (": the order in which the next song will be chosen: order, shuffle, smart shuffle", null)},
							new (string, CharFormat?)[]{("Queue", Palette.info), (": if not empty, next song will be chosen from here instead of source. There is an additional option to not empty it (this allows repetition).", null)},
						},
						l.Xsize
					),
				1 =>
					generateHelpPageElements(
						"Importing",
						new (string, CharFormat?)[][]{
							new (string, CharFormat?)[]{("AshRadio uses ffmpeg to import non .mp3 files, transforming them. Therefore, you can import almost any audio format from files.", null)},
							new (string, CharFormat?)[]{("To download from youtube and other websites, AshRadio uses yt-dlp. This program downloads audio files from multiple web pages, allowing for easier importing.", null)},
							new (string, CharFormat?)[]{("Go to the config to change the paths of these executables or auto-download them.", null)},
						},
						l.Xsize
					),
				2 =>
					generateHelpPageElements(
						"Keybinds",
						new (string, CharFormat?)[][]{
							new (string, CharFormat?)[]{("Wherever songs appear:", null)},
							new (string, CharFormat?)[]{("Q", Palette.info), (" add song to the queue", null)},
							new (string, CharFormat?)[]{("P", Palette.info), (" play song", null)},
							new (string, CharFormat?)[]{("R", Palette.info), (" delete song (in lists)", null)},
							new (string, CharFormat?)[]{("N", Palette.info), (" move song up (in lists)", null)},
							new (string, CharFormat?)[]{("M", Palette.info), (" move song down (in lists)", null)},
							new (string, CharFormat?)[]{},
							new (string, CharFormat?)[]{("For authors or playlists:", null)},
							new (string, CharFormat?)[]{("S", Palette.info), (" set as source", null)},
							new (string, CharFormat?)[]{},
							new (string, CharFormat?)[]{("Volume (available everywhere):", null)},
							new (string, CharFormat?)[]{("-", Palette.info), (" decrease by 2", null)},
							new (string, CharFormat?)[]{("+", Palette.info), (" increase by 2", null)},
						},
						l.Xsize
					),
				3 =>
					generateHelpPageElements(
						"Keybinds",
						new (string, CharFormat?)[][]{
							new (string, CharFormat?)[]{("Player (available everywhere):", null)},
							new (string, CharFormat?)[]{("Space", Palette.info), (" play/pause music", null)},
							new (string, CharFormat?)[]{("K", Palette.info), (" play/pause music", null)},
							new (string, CharFormat?)[]{("Shift + J", Palette.info), (" restart song", null)},
							new (string, CharFormat?)[]{("J", Palette.info), (" go back X seconds (advance time)", null)},
							new (string, CharFormat?)[]{("L", Palette.info), (" go forward X seconds (advance time)", null)},
							new (string, CharFormat?)[]{("N", Palette.info), (" previous song", null)},
							new (string, CharFormat?)[]{("M", Palette.info), (" next song", null)},
							new (string, CharFormat?)[]{("Shift + Space", Palette.info), (" see playing song", null)},
						},
						l.Xsize
					),
				4 =>
					generateHelpPageElements(
						"Keybinds",
						new (string, CharFormat?)[][]{
							new (string, CharFormat?)[]{("Navigation (available everywhere):", null)},
							new (string, CharFormat?)[]{("Ctrl + L", Palette.info), (" see Library", null)},
							new (string, CharFormat?)[]{("Ctrl + P", Palette.info), (" see Playlists", null)},
							new (string, CharFormat?)[]{("Ctrl + U", Palette.info), (" see Authors", null)},
							new (string, CharFormat?)[]{},
							new (string, CharFormat?)[]{("Session (available everywhere):", null)},
							new (string, CharFormat?)[]{("Shift + M", Palette.info), (" change mode", null)},
							new (string, CharFormat?)[]{("Shift + S", Palette.info), (" see source", null)},
						},
						l.Xsize
					),
				5 =>
					generateHelpPageElements(
						"Exporting",
						new (string, CharFormat?)[][]{
							new (string, CharFormat?)[]{("You can export songs or whole playlists to folders.", null)},
							new (string, CharFormat?)[]{("This allows you to have the mp3 files of your songs wherever you want, or share them.", null)},
						},
						l.Xsize
					),
				6 =>
					generateHelpPageElements(
						"Internal operation",
						new (string, CharFormat?)[][]{
							new (string, CharFormat?)[]{("AshRadio uses numerical ids for songs, authors and playlists.", null)},
							new (string, CharFormat?)[]{("2147483647 is the maximum id. Try not importing that many songs!", null)},
							new (string, CharFormat?)[]{("For the audio playing, ManagedBass is used. This .net wrapper of the c BASS library makes it really easy to play audio files.", null)},
							new (string, CharFormat?)[]{("For data storage and many other tasks, AshLib is used. This .net library (made by me!) handles AshFiles.", null)},
							new (string, CharFormat?)[]{("The UI in the console is made using AshConsoleGraphics, a .net library also made by me.", null)},
						},
						l.Xsize
					),
				7 =>
					generateHelpPageElements(
						"About the app",
						new (string, CharFormat?)[][]{
							new (string, CharFormat?)[]{("AshRadio v" + Radio.version, null)},
							new (string, CharFormat?)[]{(Radio.versionDate, null)},
							new (string, CharFormat?)[]{("Made by siljam", null)},
							new (string, CharFormat?)[]{("This software is under the MIT license.", Palette.hint)},
						},
						l.Xsize
					),	
				_ =>
					new List<TuiElement>()
			};
			
			l.Elements.AddRange(pageSpecific);
		}
		
		update();
		
		l.OnResize += (s, a) => {
			update();
		};
		
		setMiddleScreen(l3);
	}
	
	List<TuiElement> generateHelpPageElements(string title, (string, CharFormat?)[][] parts, int xsize){
		xsize -= 2; //Right margin
		
		if(xsize <= 0){
			return new List<TuiElement>();
		}
		
		int x = 3; //Left margin
		int y = 5;
		
		List<TuiElement> elems = new();
		
		elems.Add(new TuiLabel(title, Placement.TopLeft, 2, 4, Palette.info));
		
		foreach((string, CharFormat?)[] line in parts){
			foreach((string t, CharFormat? format) in line){
				string text = t;
				int size = text.Length;
				
				while(x + size > xsize){
					int av = xsize - x;
					if(av <= 0){
						x = 3;
						y++;
						continue;
					}
					
					elems.Add(new TuiLabel(text.Substring(0, xsize - x), Placement.TopLeft, x, y, format));
					text = text.Substring(xsize - x);
					size = text.Length;
					
					x = 3;
					y++;
				}
				
				if(text.Length > 0){
					elems.Add(new TuiLabel(text, Placement.TopLeft, x, y, format));
					
					x += text.Length;
				}
			}
			
			y++;
			x = 3;
		}
		
		return elems;
	}
	
	void confirmExit(){
		TuiSelectable[,] buttons = {{
			new TuiButton("Exit", Placement.Center, -4, 1, null, Palette.user).SetAction((s, ck) => {
				MultipleTuiScreenInteractive.StopPlaying(master, default);
				Environment.Exit(0);
			}),
			new TuiButton("Cancel", Placement.Center, 4, 1, null, Palette.user).SetAction((s, ck) => closeMiddleScreen())
		}};
		
		MiddleScreen t = generateMiddle(buttons);
		t.interactive.Elements.Add(new TuiLabel("Do you want to exit?", Placement.Center, 0, -1));
		t.interactive.Elements.Add(new TuiFrame(24, 7, Placement.Center, 0, 0, Palette.user));
		
		t.interactive.SubKeyEvent(ConsoleKey.Escape, (s, ck) => {
			MultipleTuiScreenInteractive.StopPlaying(master, default);
			Environment.Exit(0);
		}); //Escape + escape will close
		
		setMiddleScreen(t);
	}
}