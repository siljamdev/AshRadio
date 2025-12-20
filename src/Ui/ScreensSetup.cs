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
			Radio.py.play(j);
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
		
		#region DEBUG
		master.SubKeyEvent(ConsoleKey.A, ConsoleModifiers.None, (s, cki) => {
			Console.Clear();
			
			Console.WriteLine("Pool");
			foreach(int s2 in Session.pool){
				Console.WriteLine("\t" + (Song.get(s2)?.title ?? "Untitled song"));
			}
			
			Console.WriteLine("Seen");
			foreach(int s2 in Session.sourceSeen){
				Console.WriteLine("\t" + (Song.get(s2)?.title ?? "Untitled song"));
			}
			
			Console.WriteLine("Prev");
			foreach(int s2 in Session.prevPlayed){
				Console.WriteLine("\t" + (Song.get(s2)?.title ?? "Untitled song"));
			}
			
			Console.ReadKey();
		});
		#endregion
		
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
			setSongDetails(Radio.py.playingSong);
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
		
		int sec = (int) Radio.py.duration;
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
			Radio.py.play(j);
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
			int n = (int) (Radio.py.elapsed / Radio.py.duration * 100);
			if(n != progress.Percentage){
				progress.Percentage = n;
			}
			
			int sec = (int) Radio.py.elapsed;
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
			
			int sec = (int) Radio.py.duration;
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
		const int maxPage = 6;
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
		
		switch(page){
			case 0:
				l.Elements.Add(new TuiLabel("Concepts", Placement.TopLeft, 2, 4, Palette.info));
				l.Elements.Add(new TuiTwoLabels("Session", ": the current options for source, order and queue", Placement.TopLeft, 3, 5, Palette.info, null));
				l.Elements.Add(new TuiTwoLabels("Source", ": the 'pool' from where the next song will be chosen", Placement.TopLeft, 3, 6, Palette.info, null));
				l.Elements.Add(new TuiTwoLabels("Library", ": the whole collection of all songs", Placement.TopLeft, 3, 7, Palette.info, null));
				l.Elements.Add(new TuiTwoLabels("Order", ": the order in which the next song will be chosen: order, shuffle, smart shuffle", Placement.TopLeft, 3, 8, Palette.info, null));
				l.Elements.Add(new TuiTwoLabels("Queue", ": if not empty, next song will be chosen from here instead of source.", Placement.TopLeft, 3, 8, Palette.info, null));
				l.Elements.Add(new TuiLabel("Additional option to not empty it (this allows repetition).", Placement.TopLeft, 10, 9));
				break;
			case 1:
				l.Elements.Add(new TuiLabel("Importing", Placement.TopLeft, 2, 4, Palette.info));
				l.Elements.Add(new TuiLabel("AshRadio uses ffmpeg to import non .mp3 files, transforming them.", Placement.TopLeft, 3, 5));
				l.Elements.Add(new TuiLabel("Therefore, you can import almost any audio format from files.", Placement.TopLeft, 3, 6));
				l.Elements.Add(new TuiLabel("To import from youtube and other websites, AshRadio uses yt-dlp.", Placement.TopLeft, 3, 7));
				l.Elements.Add(new TuiLabel("This program downloads audio files from multiple web pages,", Placement.TopLeft, 3, 7));
				l.Elements.Add(new TuiLabel("allowing easier importing.", Placement.TopLeft, 3, 8));
				l.Elements.Add(new TuiLabel("Go to the config to change these paths.", Placement.TopLeft, 3, 9));
				break;
			case 2:
				l.Elements.Add(new TuiLabel("Keybinds", Placement.TopLeft, 2, 4, Palette.info));
				l.Elements.Add(new TuiLabel("Wherever songs appear, you can use:", Placement.TopLeft, 3, 5));
				l.Elements.Add(new TuiTwoLabels("Q", " to add it to the queue", Placement.TopLeft, 4, 6, Palette.info, null));
				l.Elements.Add(new TuiTwoLabels("P", " to play it", Placement.TopLeft, 4, 7, Palette.info, null));
				l.Elements.Add(new TuiTwoLabels("R", " to delete it (in lists)", Placement.TopLeft, 4, 8, Palette.info, null));
				l.Elements.Add(new TuiTwoLabels("N", " to move it up (in lists)", Placement.TopLeft, 4, 9, Palette.info, null));
				l.Elements.Add(new TuiTwoLabels("M", " to move it down (in lists)", Placement.TopLeft, 4, 10, Palette.info, null));
				l.Elements.Add(new TuiLabel("For authors or playlists:", Placement.TopLeft, 3, 11));
				l.Elements.Add(new TuiTwoLabels("S", " to set it as source", Placement.TopLeft, 4, 12, Palette.info, null));
				l.Elements.Add(new TuiLabel("Volume (available everywhere):", Placement.TopLeft, 3, 13));
				l.Elements.Add(new TuiTwoLabels("-", " decrease by 2", Placement.TopLeft, 4, 14, Palette.info, null));
				l.Elements.Add(new TuiTwoLabels("+", " increase by 2", Placement.TopLeft, 4, 15, Palette.info, null));
				break;
			case 3:
				l.Elements.Add(new TuiLabel("Keybinds", Placement.TopLeft, 2, 4, Palette.info));
				l.Elements.Add(new TuiLabel("Player (available everywhere):", Placement.TopLeft, 3, 5));
				l.Elements.Add(new TuiTwoLabels("Space", " play/pause music", Placement.TopLeft, 4, 6, Palette.info, null));
				l.Elements.Add(new TuiTwoLabels("K", " play/pause music", Placement.TopLeft, 4, 7, Palette.info, null));
				l.Elements.Add(new TuiTwoLabels("Shift + J", " restart song", Placement.TopLeft, 4, 8, Palette.info, null));
				l.Elements.Add(new TuiTwoLabels("J", " go back X seconds (advance time)", Placement.TopLeft, 4, 9, Palette.info, null));
				l.Elements.Add(new TuiTwoLabels("L", " go forward X seconds (adavance time)", Placement.TopLeft, 4, 10, Palette.info, null));
				l.Elements.Add(new TuiTwoLabels("N", " previous song", Placement.TopLeft, 4, 11, Palette.info, null));
				l.Elements.Add(new TuiTwoLabels("M", " next song", Placement.TopLeft, 4, 12, Palette.info, null));
				break;
			case 4:
				l.Elements.Add(new TuiLabel("Keybinds", Placement.TopLeft, 2, 4, Palette.info));
				l.Elements.Add(new TuiLabel("Navigation (available everywhere):", Placement.TopLeft, 3, 5));
				l.Elements.Add(new TuiTwoLabels("Ctrl + L", " see Library", Placement.TopLeft, 4, 6, Palette.info, null));
				l.Elements.Add(new TuiTwoLabels("Ctrl + P", " see Playlists", Placement.TopLeft, 4, 7, Palette.info, null));
				l.Elements.Add(new TuiTwoLabels("Ctrl + U", " see Authors", Placement.TopLeft, 4, 8, Palette.info, null));
				l.Elements.Add(new TuiLabel("Session (available everywhere)", Placement.TopLeft, 3, 9));
				l.Elements.Add(new TuiTwoLabels("Shift + M", " change mode", Placement.TopLeft, 4, 10, Palette.info, null));
				l.Elements.Add(new TuiTwoLabels("Shift + S", " see source", Placement.TopLeft, 4, 11, Palette.info, null));
				break;
			case 5:
				l.Elements.Add(new TuiLabel("Internal operation", Placement.TopLeft, 2, 4, Palette.info));
				l.Elements.Add(new TuiLabel("AshRadio uses numerical ids for songs, authors and", Placement.TopLeft, 3, 5));
				l.Elements.Add(new TuiLabel("playlists.", Placement.TopLeft, 3, 6));
				l.Elements.Add(new TuiLabel("2147483647 is the maximum id. Try not importing that many songs!", Placement.TopLeft, 3, 7));
				l.Elements.Add(new TuiLabel("For the audio playing, NAudio is used. This .net library", Placement.TopLeft, 3, 8));
				l.Elements.Add(new TuiLabel("makes it really easy to play audio files.", Placement.TopLeft, 3, 9));
				l.Elements.Add(new TuiLabel("For data storage and many other tasks, AshLib is used.", Placement.TopLeft, 3, 10));
				l.Elements.Add(new TuiLabel("This .net library (made by me!) handles AshFiles.", Placement.TopLeft, 3, 11));
				l.Elements.Add(new TuiLabel("The UI in the console is made using AshConsoleGraphics.", Placement.TopLeft, 3, 12));
				l.Elements.Add(new TuiLabel(".net library also made by me.", Placement.TopLeft, 3, 13));
				break;
			case 6:
				l.Elements.Add(new TuiLabel("About the app", Placement.TopLeft, 2, 4, Palette.info));
				l.Elements.Add(new TuiLabel("AshRadio v" + Radio.version, Placement.TopLeft, 3, 5));
				l.Elements.Add(new TuiLabel("Made by Siljam", Placement.TopLeft, 3, 6));
				l.Elements.Add(new TuiLabel("This software is under the MIT license.", Placement.TopLeft, 3, 8, Palette.hint));
				break;
		}
		
		setMiddleScreen(l3);
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