using System.Diagnostics;
using System.IO;
using AshLib.Time;
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
	
	bool cursorBlinks = true;
	float cursorPeriod = 1.2f;
	char cursor = '_';
	
	string playingChar = "►";
	string paused = "‖";
	
	float fps = 24f;
	double dt;
	
	DeltaHelper dh;
	
	public Screens(bool openConfig = false){
		init();
		
		//Init setup
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
		
		TuiConnectedLinesScreen delimiters = new TuiConnectedLinesScreen(100, 20, Palette.delimiter, d1, d2, d3);
		
		delimiters.OnParentResize += (s, a) => {
			delimiters.Xsize = a.X;
			delimiters.Ysize = a.Y;
		};
		
		TuiSelectable[,] temp = new TuiSelectable[,]{{
			new TuiButton("Open Help menu", Placement.BottomCenter, 0, 4, null, Palette.user).SetAction((s, ck) => {
				setHelp();
			})
		},{
			new TuiButton("Open GitHub repo", Placement.BottomCenter, 0, 2, null, Palette.user).SetAction((s, ck) => {
				openUrl("https://github.com/siljamdev/AshRadio");
			})
		}};
		
		//Initial empty screen
		MiddleScreen mid = generateMiddle(temp);
		
		mid.interactive.Elements.Add(new TuiTwoLabels("Welcome to ", "AshRadio", Placement.Center, 0, -5, null, Palette.main));
		
		middle.Add(mid);
		
		//Creating master
		master = new MultipleTuiScreenInteractive(100, 20, new TuiScreenInteractive[]{playing, session, navigation, mid.interactive}, Palette.defaultPanel, delimiters);
		
		master.ScreenList.Add(queueScreen);
		
		master.AutoResize = true;
		
		master.OnResize += (s, a) => hideCursor();
		
		//Panels
		Keybinds.selectPlaying.subEvent(master, (s, cki) => {
			setSelectedScreen(playing);
		});
		
		Keybinds.selectSession.subEvent(master, (s, cki) => {
			setSelectedScreen(session);
		});
		
		Keybinds.selectNavigation.subEvent(master, (s, cki) => {
			setSelectedScreen(navigation);
		});
		
		Keybinds.selectMiddle.subEvent(master, (s, cki) => {
			setSelectedScreen(currentMiddleScreen);
		});
		
		Keybinds.selectQueue.subEvent(master, (s, cki) => {
			if(Session.getQueue().Count > 0){
				setSelectedScreen(queueScreen);
			}
		});
		
		Keybinds.escape.subEvent(master, (s, cki) => { //Close or focus middle
			if(master?.SelectedScreen == currentMiddleScreen.interactive){
				closeMiddleScreen();
			}else{
				setSelectedScreen(currentMiddleScreen);
			}
		});
		
		Keybinds.exit.subEvent(master, (s, cki) => { //Exit immediately
			MultipleTuiScreenInteractive.StopPlaying(master, default);
		});
		
		//Player
		Keybinds.pause.subEvent(master, (s, cki) => {
			Radio.py.togglePause();
		});
		
		Keybinds.previous.subEvent(master, (s, cki) => {
			int j = Session.getPrevious(Radio.py.playingSong);
			if(j < 0){
				return;
			}
			Session.addToPrevList = false;
			Radio.py.play(j);
			Session.addToPrevList = true;
		});
		
		Keybinds.skip.subEvent(master, (s, cki) => {
			Radio.py.skip();
		});
		
		Keybinds.rewind.subEvent(master, (s, cki) => {
			Radio.py.rewind();
		});
		
		Keybinds.restart.subEvent(master, (s, cki) => {
			Radio.py.elapsed = 0f;
		});
		
		Keybinds.advance.subEvent(master, (s, cki) => {
			Radio.py.advance();
		});
		
		//Volume
		Keybinds.volumeDown.subEvent(master, (s, cki) => {
			volume.NumberDown(volume, cki);
			Radio.py.setVolume(volume.Number / 100f);
		});
		
		Keybinds.volumeUp.subEvent(master, (s, cki) => {
			volume.NumberUp(volume, cki);
			Radio.py.setVolume(volume.Number / 100f);
		});
		
		Keybinds.volumeMute.subEvent(master, (s, cki) => {
			Radio.py.setVolume(0f);
		});
		
		Keybinds.volumeMax.subEvent(master, (s, cki) => {
			Radio.py.setVolume(1f);
		});
		
		//Navigation
		Keybinds.help.subEvent(master, (s, cki) => {
			setHelp();
		});
		
		Keybinds.config.subEvent(master, (s, cki) => {
			setConfig();
		});
		
		Keybinds.library.subEvent(master, (s, cki) => {
			setLibrary();
		});
		
		Keybinds.playlists.subEvent(master, (s, cki) => {
			setPlaylists();
		});
		
		Keybinds.authors.subEvent(master, (s, cki) => {
			setAuthors();
		});
		
		Keybinds.import.subEvent(master, (s, cki) => {
			setImport();
		});
		
		Keybinds.stats.subEvent(master, (s, cki) => {
			setStatsSelect();
		});
		
		Keybinds.errlog.subEvent(master, (s, cki) => {
			setErrorLog();
		});
		
		//session
		Keybinds.changeMode.subEvent(master, (s, cki) => {
			if(mode.SelectedOptionIndex == 2){
				mode.SelectedOptionIndex = 0;
			}else{
				mode.SelectedOptionIndex++;
			}
			Session.setMode((SessionMode) mode.SelectedOptionIndex);
		});
		
		Keybinds.seeSource.subEvent(master, (s, cki) => {
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
		
		Keybinds.seePlaying.subEvent(master, (s, cki) => {
			if(Radio.py.playingSong >= 0){
				setSongDetails(Radio.py.playingSong);
			}
		});
		
		Keybinds.toggleQueueEmpties.subEvent(master, (s, cki) => {
			Session.queueEmpties = !Session.queueEmpties;
		});
		
		Keybinds.changeDevice.subEvent(master, (s, cki) => {
			setChangeDevice();
		});
		
		Stopwatch timer = Stopwatch.StartNew();
		double st = 0d;
		
		double consoleTime = 0d;
		
		double cursorTime = 0d;
		
		master.OnFinishPlayCycle += (s, a) => { //Wait some time to avoid enourmus cpu usage
			
			//Update cursor if neccessary
			cursorTime += dh.deltaTime;
			if(cursorBlinks && cursorTime >= cursorPeriod){
				cursorTime = 0d;
				if(cursorBlinks){
					if(TuiWritable.Cursor != ' '){
						TuiWritable.Cursor = ' ';
					}else{
						TuiWritable.Cursor = cursor;
					}
				}
			}
			
			consoleTime += dh.deltaTime;
			if(consoleTime >= 30000){
				consoleTime = 0d;
				
				//Actual console cursor, not this ui cursor
				hideCursor(); //In conhost.exe, sometimes cursor appears out of the blue
			}
			
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
			
			st = timer.Elapsed.TotalMilliseconds;
			dh.Frame();
		};
		
		if(openConfig){
			setConfig();
		}else{
			setSelectedScreen(navigation);
		}
	}
	
	public void init(){
		//Load selector chars
		string sels = Radio.config.GetValue<string>("ui.selectors") ?? "><";
		TuiSelectable.LeftSelector = sels.Length > 1 ? sels[0] : '>';
		TuiSelectable.RightSelector = sels.Length > 1 ? sels[1] : '<';
		
		//Cursor
		cursorBlinks = Radio.config.GetValue<bool>("ui.cursorBlinks");
		cursorPeriod = Radio.config.GetValue<float>("ui.cursorBlinkPeriod");
		
		string cur = Radio.config.GetValue<string>("ui.cursor") ?? "_";
		cursor = cur.Length > 0 ? cur[0] : '_';
		TuiWritable.Cursor = cursor;
		
		//Load playing chars
		sels = Radio.config.GetValue<string>("ui.playingChars") ?? "►‖";
		playingChar = sels.Length > 1 ? new string(sels[0], 1) : "►";
		paused = sels.Length > 1 ? new string(sels[1], 1) : "‖";
		
		//fps
		fps = Radio.config.GetValue<float>("ui.updateFrequency");
		dt = 1000d / fps;
		
		dh = new DeltaHelper();
		dh.Start();
	}
	
	//Method to start it all
	public void play(){
		enterAltBuffer();
		hideCursor();
		
		master.Play();
	}
	
	void setupPlaying(){
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
		
		TuiLabel elapsedTime = new TuiLabel("0:00", Placement.Center, -39, 0, Palette.info);
		TuiLabel totalTime = new TuiLabel("0:00", Placement.Center, 39, 0, Palette.info);
		
		elapsedTime.OnParentResize += (s, a) => {
			elapsedTime.OffsetX = -(a.X - 30)/2 - 4;
		};
		
		totalTime.OnParentResize += (s, a) => {
			totalTime.OffsetX = (a.X - 30)/2 + 4;
		};
		
		totalTime.Text = secondsToMinuteTime(Radio.py.duration);
		
		TuiButton play = new TuiButton(playingChar, Placement.TopCenter, 0, 1, null, Palette.user); //► or ‖
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
		
		volume = new TuiNumberPicker(0, 100, 2, (int) (Radio.py.volume * 100f), Placement.Center, 3, 2, Palette.info, Palette.user);
		
		volume.DeleteAllKeyEvents();
		Keybinds.left.subEvent(volume, (s, cki) => {
			volume.NumberDown(s, cki);
			Radio.py.setVolume(volume.Number);
		});
		Keybinds.right.subEvent(volume, (s, cki) => {
			volume.NumberUp(s, cki);
			Radio.py.setVolume(volume.Number);
		});
		
		Radio.py.onChangeVolume += (s, a) => {
			volume.Number = (int) (Radio.py.volume * 100f);
		};
		
		playing = new TuiScreenInteractive(100, 5, new TuiSelectable[,]{{
			song, back, prev, play, next, advance
		}, {
			volume, volume, volume, volume, volume, volume
		}}, 3, 0, Placement.BottomCenter, 0, 0, null,
			new TuiLabel("Playing:", Placement.TopLeft, 2, 1),
			new TuiLabel("Authors:", Placement.BottomLeft, 2, 1),
			new TuiLabel("v" + BuildInfo.Version, Placement.BottomRight, 0, 0, Palette.delimiter),
			new TuiLabel("Volume", Placement.Center, -2, 2),
			new TuiLabel(Keybinds.selectPlaying.ToString(), Placement.TopRight, 0, 0, Palette.hint),
			authors,
			progress, elapsedTime, totalTime
		);
		
		playing.OnParentResize += (s, a) => {
			playing.Xsize = a.X;
			playing.Ysize = Math.Min(a.Y, 5);
			
			song.Text = crop(Song.get(Radio.py.playingSong)?.title ?? "", playing.Xsize / 2 - 25);
		};
		
		playing.OnFinishPlayCycle += (s, a) => {
			progress.Filled = Math.Max(Radio.py.elapsed, 0f) / Math.Max(Radio.py.duration, 0f); //Is optimized
			
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
			play.Text = Radio.py.isPaused ? playingChar : paused;
		};
	}
	
	void setupSession(){
		TuiLabel device = new TuiLabel(Radio.py.currentDevice.Name ?? "", Placement.BottomLeft, 1, 2, Palette.info);
		
		TuiButton devices = new TuiButton("Change device", Placement.BottomCenter, 0, 1, null, Palette.user).SetAction((s, cki) => {
			setChangeDevice();
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
		
		TuiFramedCheckBox emptyQueue = new TuiFramedCheckBox(' ', 'X', Session.queueEmpties, Placement.BottomLeft, 18, 3, null, null, null, Palette.writing, Palette.user);
		
		emptyQueue.DeleteAllKeyEvents();
		Keybinds.enter.subEvent(emptyQueue, (s, ck) => {
			Session.queueEmpties = !Session.queueEmpties;
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
			new TuiLabel(Keybinds.selectSession.ToString(), Placement.BottomLeft, 0, 0, Palette.hint)
		);
		
		void updateQueueScreen(){
			bool b = master?.SelectedScreen == queueScreen;
			int n = 0;
			if(b){
				n = queueScreen?.Elements.IndexOf(queueScreen.Selected) ?? 0;
			}
			
			emptyQueue.Checked = Session.queueEmpties;
			
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
					
					Keybinds.listRemove.subEvent(t2, (s, ck) => {
						int myIndex = queueScreen.Elements.IndexOf(t2);
						Session.removeFromQueue(myIndex);
					});
					
					Keybinds.listUp.subEvent(t2, (s, ck) => {
						int myIndex = queueScreen.Elements.IndexOf(t2);
						if(myIndex != 0){
							TuiScreenInteractive.MoveUp(queueScreen, ck); //Align properly with the change
							Session.moveInQueue(myIndex, myIndex - 1);
						}
					});
					
					Keybinds.listDown.subEvent(t2, (s, ck) => {
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
			
			Keybinds.escape.subEvent(queueScreen, (s, ck) => {
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
			new TuiLabel(Keybinds.selectNavigation.ToString(), Placement.BottomRight, 0, 0, Palette.hint)
		);
		
		navigation.OnParentResize += (s, a) => {
			navigation.Xsize = Math.Min(30, a.X);
			navigation.Ysize = Math.Max(a.Y - 6, 0);
		};
		
		prepareScreen(navigation);
	}
	
	void setChangeDevice(){
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
		changeDevice.interactive.Elements.Add(new TuiLabel("Select Device", Placement.TopCenter, 0, 1, Palette.main));
		setMiddleScreen(changeDevice);
	}
}