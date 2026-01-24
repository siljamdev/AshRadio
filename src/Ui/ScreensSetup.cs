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
		
		master.OnResize += (s, a) => hideCursor();
		
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
		enterAltBuffer();
		hideCursor();
		
		master.Play();
	}
	
	public void setupPlaying(){
		Song temp = Song.get(Radio.py.playingSong);
		TuiButton song = new TuiButton(crop(temp?.title ?? "", 38), Placement.TopLeft, 11, 1, Palette.song, Palette.user).SetAction((s, ck) => {
			if(Radio.py.playingSong >= 0){
				setSongDetails(Radio.py.playingSong);
			}
		});
		
		TuiTwoLabels authors = new TuiTwoLabels("Authors: ", temp == null ? "" : (temp.authors.Length == 0 ? Author.nullName : string.Join(", ", temp.authors.Select(n => (Author.get(n)?.name ?? Author.nullName)))), Placement.BottomLeft, 2, 1, null, Palette.author);
		
		TuiProgressBar progress = new TuiProgressBar(70, '█', '░', Placement.Center, 0, 0, Palette.main, Palette.main);
		
		progress.OnParentResize += (s, a) => {
			progress.Xsize = Math.Max(a.X - 30, 0);
		};
		
		TuiLabel elapsedTime = new TuiLabel("00:00", Placement.Center, -39, 0, Palette.info);
		TuiLabel totalTime = new TuiLabel("00:00", Placement.Center, 39, 0, Palette.info);
		
		elapsedTime.OnParentResize += (s, a) => {
			elapsedTime.OffsetX = -(a.X - 30)/2 - 4;
		};
		
		totalTime.OnParentResize += (s, a) => {
			totalTime.OffsetX = (a.X - 30)/2 + 4;
		};
		
		totalTime.Text = secondsToMinuteTime(Radio.py.duration);
		
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
			
			string s2 = secondsToMinuteTime(Radio.py.elapsed);
			if(s2 != elapsedTime.Text){
				elapsedTime.Text = s2;
			}
		};
		
		prepareScreen(playing);
		
		Radio.py.onSongLoad += (se, e) => {
			Song s = Song.get(Radio.py.playingSong);
			
			song.Text = crop(s?.title ?? "", playing.Xsize / 2 - 25);
			authors.RightText = s == null ? "" : (s.authors.Length == 0 ? Author.nullName : string.Join(", ", s.authors.Select(n => (Author.get(n)?.name ?? Author.nullName))));
			
			totalTime.Text = secondsToMinuteTime(Radio.py.duration);
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
				name = Author.get(Session.sourceIdentifier)?.name ?? Author.nullName;
				f = Palette.author;
				break;
			case SourceType.Playlist:
				name = Playlist.get(Session.sourceIdentifier)?.title ?? Playlist.nullTitle;
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
					nam = Author.get(Session.sourceIdentifier)?.name ?? Author.nullName;
					f2 = Palette.author;
					break;
				case SourceType.Playlist:
					nam = Playlist.get(Session.sourceIdentifier)?.title ?? Playlist.nullTitle;
					f2 = Palette.playlist;
					break;
			}
			
			sourceType.RightText = nam;
			sourceType.RightFormat = f2;
		};
		
		mode = new TuiOptionPicker(new string[]{"Order", "Shuffle", "Smart Shuffle"}, (uint) ((int) Session.mode), Placement.TopLeft, 7, 5, Palette.info, Palette.user);
		
		setLooping(mode);
		
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
					TuiButton t2 = new TuiButton(s?.title ?? Song.nullTitle, Placement.TopLeft, Session.queueIndex == i ? 1 : 0, i, Palette.song, Palette.user);
					
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
		
		session.SubKeyEvent(ConsoleKey.Q, (s, ck) => {
			if(Session.getQueue().Count > 0){
				setSelectedScreen(queueScreen);
			}
		});
		
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
		
		TuiButton sta = new TuiButton("Stats", Placement.TopLeft, 2, 15, null, Palette.user).SetAction((s, ck) => {
			setStatsSelect();
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
			sta
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
	
	void setHelp(int page = 0, bool ignore = false){
		if(!ignore && currentMiddleScreen.identifier == "help"){
			setSelectedScreen(currentMiddleScreen);
			return;
		}
		const int maxPage = 8;
		
		MiddleScreen l3 = generateMiddle(null);
		l3.identifier = "help";
		
		//Juto to avoid rewriting a lot of code
		TuiScreenInteractive l = l3.interactive;
		
		TuiFormatLog content = new TuiFormatLog(l.Xsize - 4, l.Ysize - 7, Placement.TopLeft, 2, 5);
		
		content.OnParentResize += (s, a) => {
			content.Xsize = l.Xsize - 4;
			content.Ysize = l.Ysize - 7;
		};
		
		l.Elements.Add(content);
		
		l.Elements.Add(new TuiLabel("Help - Page " + (page + 1), Placement.TopCenter, 0, 1, Palette.main));
		
		if(page > 0){
			l.Elements.Add(new TuiTwoLabels("N", " Previous page", Placement.BottomRight, 0, 1, Palette.hint, null));
		}
		
		if(page < maxPage){
			l.Elements.Add(new TuiTwoLabels("M", " Next page", Placement.BottomRight, 0, 0, Palette.hint, null));
		}
		
		//Helper method
		void setPage(int n){
			if(n != page && n > -1 && n <= maxPage){
				setHelp(n, true);
				removeMiddleScreen(l3);
			}
		}
		
		l.SubKeyEvent(ConsoleKey.N, (s, ck) => {
			setPage(page - 1);
		});
		l.SubKeyEvent(ConsoleKey.M, (s, ck) => {
			setPage(page + 1);
		});
		l.SubKeyEvent(ConsoleKey.D1, (s, ck) => {
			setPage(0);
		});
		l.SubKeyEvent(ConsoleKey.D2, (s, ck) => {
			setPage(1);
		});
		l.SubKeyEvent(ConsoleKey.D3, (s, ck) => {
			setPage(2);
		});
		l.SubKeyEvent(ConsoleKey.D4, (s, ck) => {
			setPage(3);
		});
		l.SubKeyEvent(ConsoleKey.D5, (s, ck) => {
			setPage(4);
		});
		l.SubKeyEvent(ConsoleKey.D6, (s, ck) => {
			setPage(5);
		});
		l.SubKeyEvent(ConsoleKey.D7, (s, ck) => {
			setPage(6);
		});
		l.SubKeyEvent(ConsoleKey.D8, (s, ck) => {
			setPage(7);
		});
		l.SubKeyEvent(ConsoleKey.D9, (s, ck) => {
			setPage(8);
		});
		
		switch(page){
			case 0:
				content.AppendLine("Concepts", Palette.info);
				
				content.Append(" Session", Palette.info);
				content.AppendLine(": the current options for source, order and queue", null);
				content.Append(" Source", Palette.info);
				content.AppendLine(": the 'pool' from where the next song will be chosen", null);
				content.Append(" Library", Palette.info);
				content.AppendLine(": the whole collection of songs", null);
				content.Append(" Order", Palette.info);
				content.AppendLine(": the order in which the next song will be chosen: order, shuffle, smart shuffle", null);
				content.Append(" Queue", Palette.info);
				content.AppendLine(": if not empty, next song will be chosen from here instead of source. There is an additional option to not empty it (this allows for repetition)", null);
				break;
			case 1:
				content.AppendLine("Importing", Palette.info);
				
				content.AppendLine(" AshRadio uses ffmpeg to import non .mp3 files, transforming them. Therefore, you can import almost any audio format from files.", null);
				content.AppendLine(" To download from youtube and other websites, AshRadio uses yt-dlp. This program downloads audio files from multiple web pages, allowing for easier importing.", null);
				content.AppendLine(" Go to the config to change the paths of these executables or auto-download them.", null);
				break;
			case 2:
				content.AppendLine("Keybinds", Palette.info);
				
				content.AppendLine(" Wherever songs appear:", null);
				content.Append("  Q", Palette.hint);
				content.AppendLine(" add song to the queue", null);
				content.Append("  P", Palette.hint);
				content.AppendLine(" play song", null);
				content.Append("  R", Palette.hint);
				content.AppendLine(" delete song (in lists)", null);
				content.Append("  N", Palette.hint);
				content.AppendLine(" move song up (in lists)", null);
				content.Append("  M", Palette.hint);
				content.AppendLine(" move song down (in lists)", null);
				content.AppendLine("", null);
				
				content.AppendLine(" For authors or playlists:", null);
				content.Append("  S", Palette.hint);
				content.AppendLine(" set as source", null);
				content.AppendLine("", null);
				
				content.AppendLine(" Volume (available everywhere):", null);
				content.Append("  -", Palette.hint);
				content.AppendLine(" decrease by 2", null);
				content.Append("  +", Palette.hint);
				content.AppendLine(" increase by 2", null);
				break;
			case 3:
				content.AppendLine("Keybinds", Palette.info);
				
				content.AppendLine(" Player (available everywhere):", null);
				content.Append("  Space", Palette.hint);
				content.AppendLine(" play/pause music", null);
				content.Append("  K", Palette.hint);
				content.AppendLine(" play/pause music", null);
				content.Append("  Shift+J", Palette.hint);
				content.AppendLine(" restart song", null);
				content.Append("  J", Palette.hint);
				content.AppendLine(" go back X seconds (advance time)", null);
				content.Append("  L", Palette.hint);
				content.AppendLine(" go forward X seconds (advance time)", null);
				content.Append("  N", Palette.hint);
				content.AppendLine(" previous song", null);
				content.Append("  M", Palette.hint);
				content.AppendLine(" next song", null);
				content.Append("  Shift+Space", Palette.hint);
				content.AppendLine(" see playing song", null);
				break;
			case 4:
				content.AppendLine("Keybinds", Palette.info);
				
				content.AppendLine(" Navigation (available everywhere):", null);
				content.Append("  Ctrl+L", Palette.hint);
				content.AppendLine(" see Library", null);
				content.Append("  Ctrl+P", Palette.hint);
				content.AppendLine(" see Playlists", null);
				content.Append("  Ctrl+U", Palette.hint);
				content.AppendLine(" see Authors", null);
				content.AppendLine("", null);
				
				content.AppendLine(" Session (available everywhere):", null);
				content.Append("  Shift+M", Palette.hint);
				content.AppendLine(" change mode", null);
				content.Append("  Shift+S", Palette.hint);
				content.AppendLine(" see source", null);
				break;
			case 5:
				content.AppendLine("Stats", Palette.info);
				content.AppendLine(" You can check the stats divided into months.", null);
				content.AppendLine(" Every time a song is loaded into the player, it counts as 'song laoded'.", null);
				content.AppendLine(" Then, the time listening to that song while it is playing is tracked.", null);
				content.AppendLine(" Also, dividing the time lime listened by the duration gives a much more accurate number of times the song was listened to. This is what is called 'song listened'.", null);
				content.AppendLine(" When seeing the stats you can filter between these numbers.", null);
				content.AppendLine(" Additionally, you can see top authors and their top tracks.", null);
				break;
			case 6:
				content.AppendLine("Exporting", Palette.info);
				content.AppendLine(" You can export songs or whole playlists to folders.", null);
				content.AppendLine(" This allows you to have the mp3 files of your songs wherever you want, or share them.", null);
				break;
			case 7:
				content.AppendLine("Internal operation", Palette.info);
				
				content.AppendLine(" AshRadio uses numerical ids for songs, authors and playlists.", null);
				content.AppendLine(" 2147483647 is the maximum id. Try not importing that many songs!", null);
				content.AppendLine(" For the audio playing, ManagedBass is used. This .net wrapper of the c BASS library makes it really easy to play audio files.", null);
				content.AppendLine(" For data storage and many other tasks, AshLib is used. This .net library (made by me!) handles AshFiles.", null);
				content.AppendLine(" The UI in the console is made using AshConsoleGraphics, a .net library also made by me.", null);
				break;
			case 8:
				content.AppendLine("About", Palette.info);
				content.AppendLine(" AshRadio v" + Radio.version, null);
				content.AppendLine(" " + Radio.versionDate, null);
				content.AppendLine(" Made by siljam", null);
				content.AppendLine(" This software is under the MIT license", Palette.hint);
				break;
		}
		
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