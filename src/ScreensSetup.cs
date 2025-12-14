using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Threading.Tasks;
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
	
	Stack<TuiScreenInteractive> middle = new();
	
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
		
		TuiScreenInteractive mid = getMiddle(temp);
		
		mid.Elements.Add(new TuiTwoLabels("Welcome to ", "AshRadio", Placement.Center, 0, -5, null, Palette.main));
		
		middle.Push(mid); //Initial empty middle screen
		
		master = new MultipleTuiScreenInteractive(100, 20, new TuiScreenInteractive[]{playing, session, navigation, mid}, null, delimiters);
		
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
			setSelectedScreen(middle.Peek());
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
			Radio.py.prev();
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
		
		Stopwatch timer = Stopwatch.StartNew();
		
		int maxFps = 48;
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
		
		TuiButton prev = new TuiButton("≤", Placement.TopCenter, -6, 1, null, Palette.user).SetAction((s, cki) => Radio.py.prev());
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
		TuiLabel device = new TuiLabel(Radio.py.getCurrentDevice().Name ?? "", Placement.BottomLeft, 1, 2, Palette.info);
		
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
			
			TuiScreenInteractive changeDevice = getMiddle(buttons);
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
				break;
			case SourceType.Author:
				name = "Author";
				name2 = Author.get(Session.sourceIdentifier)?.name ?? "Unknown author";
				f = Palette.author;
				break;
			case SourceType.Playlist:
				name = "Playlist";
				name2 = Playlist.get(Session.sourceIdentifier)?.title ?? "Untitled playlist";
				f = Palette.playlist;
				break;
		}
		
		TuiTwoLabels sourceType = new TuiTwoLabels("Source: ", name, Placement.TopLeft, 1, 2, null, Palette.info);
		
		TuiLabel source = new TuiLabel(name2, Placement.TopLeft, 3, 3, f);
		
		Session.onSourceChange += (s, a) => {
			string nam = "";
			string nam2 = "";
			CharFormat? f2 = null;
			switch(Session.sourceType){
				default:
					break;
				case SourceType.Library:
					nam = "Library";
					break;
				case SourceType.Author:
					nam = "Author";
					nam2 = Author.get(Session.sourceIdentifier)?.name ?? "Unknown author";
					f2 = Palette.author;
					break;
				case SourceType.Playlist:
					nam = "Playlist";
					nam2 = Playlist.get(Session.sourceIdentifier)?.title ?? "Untitled playlist";
					f2 = Palette.playlist;
					break;
			}
			
			sourceType.RightText = nam;
			source.Text = nam2;
			source.Format = f2;
		};
		
		mode = new TuiOptionPicker(new string[]{"Order", "Shuffle", "Smart Shuffle"}, (uint) ((int) Session.mode), Placement.TopLeft, 3, 6, Palette.info, Palette.user);
		
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
			device, source,
			sourceType,
			new TuiLabel("Mode:", Placement.TopLeft, 1, 5),
			new TuiLabel("Queue:", Placement.TopLeft, 1, 8),
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
			
			queueScreen = new TuiScrollingScreenInteractive(Math.Max(0, session.Xsize - 4), Math.Max(0, session.Ysize - 16), temp, 0, (uint) n, Placement.TopLeft, 2, 9, null);
			
			if(queue.Count == 0){
				queueScreen.Elements.Add(new TuiLabel("Empty", Placement.TopCenter, 0, 0));
			}
			
			if(queueScreen.Selected?.OffsetY >= queueScreen.Ysize){
				int dif = queueScreen.Selected.OffsetY - (int) queueScreen.Ysize + 1;
				foreach(TuiElement e in queueScreen){
					e.OffsetY -= dif;
				}
			}
			
			queueScreen.OnParentResize += (s, a) => {
				queueScreen.Xsize = Math.Max(0, a.X - 4);
				queueScreen.Ysize = Math.Max(0, a.Y - 16);
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
			device.Text = Radio.py.getCurrentDevice().Name;
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
		
		TuiScreenInteractive l = getMiddle(t);
		
		l.Elements.Add(new TuiLabel("Import songs", Placement.TopCenter, 0, 1, Palette.main));
		l.Elements.Add(new TuiLabel("From file", Placement.TopCenter, 0, 4, Palette.info));
		l.Elements.Add(new TuiLabel("From YouTube", Placement.TopCenter, 0, 10, Palette.info));
		l.Elements.Add(new TuiLabel("Importing from yt might take a while", Placement.TopCenter, 0, 11, Palette.info));
		
		setMiddleScreen(l);
	}
	
	void setImportSingleFile(){
		TuiFramedScrollingTextBox path = new TuiFramedScrollingTextBox("", 256, 34, Placement.TopCenter, 0, 5, null, null, null, Palette.user, Palette.user);
		TuiFramedScrollingTextBox title = new TuiFramedScrollingTextBox("", 256, 34, Placement.TopCenter, 0, 10, null, null, null, Palette.user, Palette.user);
		TuiFramedScrollingTextBox authors = new TuiFramedScrollingTextBox("", 256, 34, Placement.TopCenter, 0, 14, null, null, null, Palette.user, Palette.user);
		
		List<TuiLabel> error = new();
		
		TuiScreenInteractive l = null;
		
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
		#else
			TuiButton search = null;
		#endif
		
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
					l.Elements.Add(a);
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
		
		TuiSelectable[,] t = OperatingSystem.IsWindows() ? new TuiSelectable[,]{{
			path
		},{
			search
		},{
			title
		},{
			authors
		},{
			import
		}} : new TuiSelectable[,]{{
			path
		},{
			title
		},{
			authors
		},{
			import
		}};
		
		l = getMiddle(t);
		
		l.Elements.Add(new TuiLabel("Import song from file", Placement.TopCenter, 0, 1, Palette.main));
		l.Elements.Add(new TuiLabel("Path:", Placement.TopLeft, 2, 4));
		l.Elements.Add(new TuiLabel("Title:", Placement.TopLeft, 2, 9));
		l.Elements.Add(new TuiLabel("Authors (separated by commas):", Placement.TopLeft, 1, 13));
		
		setMiddleScreen(l);
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
					l.Elements.Add(a);
					error.Add(a);
				}
			};
			
			Task<int> task = Task.Run(() => Radio.importSingleVideo(path.Text, title.Text, authors.Text.Split(','), r2d2));
			
			task.ContinueWith(t => {
				if(middle.Peek() == l && t.Result > -1){
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
		
		l = getMiddle(t);
		
		l.Elements.Add(new TuiLabel("Import song from youtube", Placement.TopCenter, 0, 1, Palette.main));
		l.Elements.Add(new TuiLabel("Url:", Placement.TopLeft, 2, 4));
		l.Elements.Add(new TuiLabel("Title:", Placement.TopLeft, 2, 8));
		l.Elements.Add(new TuiLabel("Authors (separated by commas):", Placement.TopLeft, 1, 12));
		
		setMiddleScreen(l);
	}
	
	void setImportFolder(){
		TuiFramedScrollingTextBox path = new TuiFramedScrollingTextBox("", 256, 34, Placement.TopCenter, 0, 5, null, null, null, Palette.user, Palette.user);
		TuiFramedScrollingTextBox authors = new TuiFramedScrollingTextBox("", 256, 34, Placement.TopCenter, 0, 10, null, null, null, Palette.user, Palette.user);
		
		List<TuiLabel> error = new();
		
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
		#else
			TuiButton search = null;
		#endif
		
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
					l.Elements.Add(a);
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
		
		TuiSelectable[,] t = OperatingSystem.IsWindows() ? new TuiSelectable[,]{{
			path
		},{
			search
		},{
			authors
		},{
			import
		}} : new TuiSelectable[,]{{
			path
		},{
			authors
		},{
			import
		}};
		
		l = getMiddle(t);
		
		l.Elements.Add(new TuiLabel("Import songs from folder", Placement.TopCenter, 0, 1, Palette.main));
		l.Elements.Add(new TuiLabel("Folder path:", Placement.TopLeft, 2, 4));
		l.Elements.Add(new TuiLabel("Authors (separated by commas):", Placement.TopLeft, 1, 9));
		
		setMiddleScreen(l);
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
					l.Elements.Add(a);
					error.Add(a);
				}
			};
			
			Task<bool> task = Task.Run(() => Radio.importFromPlaylist(path.Text, authors.Text.Split(','), r2d2));
			
			task.ContinueWith(t => {
				if(middle.Peek() == l && t.Result){
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
		
		l = getMiddle(t);
		
		l.Elements.Add(new TuiLabel("Import songs from youtube playlist", Placement.TopCenter, 0, 1, Palette.main));
		l.Elements.Add(new TuiLabel("Url:", Placement.TopLeft, 2, 4));
		l.Elements.Add(new TuiLabel("Authors (separated by commas):", Placement.TopLeft, 1, 8));
		
		setMiddleScreen(l);
	}
	
	void setImportFolderPlaylist(){
		TuiFramedScrollingTextBox path = new TuiFramedScrollingTextBox("", 256, 34, Placement.TopCenter, 0, 5, null, null, null, Palette.user, Palette.user);
		TuiFramedScrollingTextBox title = new TuiFramedScrollingTextBox("", 256, 34, Placement.TopCenter, 0, 10, null, null, null, Palette.user, Palette.user);
		TuiFramedScrollingTextBox authors = new TuiFramedScrollingTextBox("", 256, 34, Placement.TopCenter, 0, 14, null, null, null, Palette.user, Palette.user);
		
		List<TuiLabel> error = new();
		
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
		#else
			TuiButton search = null;
		#endif
		
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
					l.Elements.Add(a);
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
		
		TuiSelectable[,] t = OperatingSystem.IsWindows() ? new TuiSelectable[,]{{
			path
		},{
			search
		},{
			title
		},{
			authors
		},{
			import
		}} : new TuiSelectable[,]{{
			path
		},{
			title
		},{
			authors
		},{
			import
		}};
		
		l = getMiddle(t);
		
		l.Elements.Add(new TuiLabel("Import playlist from folder", Placement.TopCenter, 0, 1, Palette.main));
		l.Elements.Add(new TuiLabel("Folder path:", Placement.TopLeft, 2, 4));
		l.Elements.Add(new TuiLabel("Playlist title:", Placement.TopLeft, 2, 9));
		l.Elements.Add(new TuiLabel("Authors (separated by commas):", Placement.TopLeft, 1, 13));
		
		setMiddleScreen(l);
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
					l.Elements.Add(a);
					error.Add(a);
				}
			};
			
			Task<int> task = Task.Run(() => Radio.importYtPlaylist(path.Text, title.Text, authors.Text.Split(','), r2d2));
			
			task.ContinueWith(t => {
				if(middle.Peek() == l && t.Result > -1){
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
		
		l = getMiddle(t);
		
		l.Elements.Add(new TuiLabel("Import playlist from youtube playlist", Placement.TopCenter, 0, 1, Palette.main));
		l.Elements.Add(new TuiLabel("Yt playlist url:", Placement.TopLeft, 2, 4));
		l.Elements.Add(new TuiLabel("Playlist title:", Placement.TopLeft, 2, 8));
		l.Elements.Add(new TuiLabel("Authors (separated by commas):", Placement.TopLeft, 1, 12));
		
		setMiddleScreen(l);
	}
	
	void setConfig(){
		TuiSelectable[,] t = new TuiSelectable[,]{{
			new TuiButton("Palette", Placement.TopCenter, 0, 4, null, Palette.user).SetAction((s, ck) => {
				setPaletteConfig();
			})
		},{
			new TuiButton("Player", Placement.TopCenter, 0, 6, null, Palette.user).SetAction((s, ck) => {
				setPlayerConfig();
			})
		},{
			new TuiButton("Paths", Placement.TopCenter, 0, 8, null, Palette.user).SetAction((s, ck) => {
				setPathConfig();
			})
		},{
			new TuiButton("Miscellaneous", Placement.TopCenter, 0, 10, null, Palette.user).SetAction((s, ck) => {
				setMiscConfig();
			})
		}};
		
		TuiScreenInteractive l = getMiddle(t);
		
		l.Elements.Add(new TuiLabel("Config", Placement.TopCenter, 0, 1, Palette.main));
		l.Elements.Add(new TuiTwoLabels("AshRadio v" + Radio.version, " made by siljam", Placement.BottomRight, 0, 0, Palette.hint, null));
		
		setMiddleScreen(l);
	}
	
	void setPaletteConfig(){
		TuiFramedCheckBox useCols = new TuiFramedCheckBox(' ', 'X', Radio.config.GetValue<bool>("ui.useColors"), Placement.TopCenter, 4, 4, null, null, null, Palette.user, Palette.user);
		
		TuiFramedTextBox user = setColor3(new TuiFramedTextBox(Palette.user.foreground.ToString(), 7, Placement.TopLeft, 8, 7, null, Palette.user, Palette.user));
		TuiFramedTextBox main = setColor3(new TuiFramedTextBox(Palette.main.foreground.ToString(), 7, Placement.TopCenter, 4, 7, null, Palette.main, Palette.user));
		TuiFramedTextBox background = setColor3(new TuiFramedTextBox(Palette.background.background.ToString(), 7, Placement.TopRight, 1, 7, null, null, Palette.user));
		
		TuiFramedTextBox song = setColor3(new TuiFramedTextBox(Palette.song.foreground.ToString(), 7, Placement.TopLeft, 8, 10, null, Palette.song, Palette.user));
		TuiFramedTextBox author = setColor3(new TuiFramedTextBox(Palette.author.foreground.ToString(), 7, Placement.TopCenter, 4, 10, null, Palette.author, Palette.user));
		TuiFramedTextBox playlist = setColor3(new TuiFramedTextBox(Palette.playlist.foreground.ToString(), 7, Placement.TopRight, 1, 10, null, Palette.playlist, Palette.user));
		
		TuiFramedTextBox hint = setColor3(new TuiFramedTextBox(Palette.hint.foreground.ToString(), 7, Placement.TopLeft, 8, 13, null, Palette.hint, Palette.user));
		TuiFramedTextBox info = setColor3(new TuiFramedTextBox(Palette.info.foreground.ToString(), 7, Placement.TopCenter, 4, 13, null, Palette.info, Palette.user));
		TuiFramedTextBox delimiter = setColor3(new TuiFramedTextBox(Palette.delimiter.foreground.ToString(), 7, Placement.TopRight, 1, 13, null, Palette.delimiter, Palette.user));
		
		TuiLabel errorLabel = new TuiLabel("", Placement.BottomCenter, 0, 4, Palette.error);
		
		TuiButton setAshTheme = new TuiButton("Ash Theme", Placement.BottomLeft, 3, 5, null, Palette.user).SetAction((s, ck) => {
			Palette.setAsh();
			closeMiddleScreen(); //update
			setPaletteConfig();
		});
		
		TuiButton setSubtleTheme = new TuiButton("Subtle Theme", Placement.BottomCenter, 0, 5, null, Palette.user).SetAction((s, ck) => {
			Palette.setSubtle();
			closeMiddleScreen(); //update
			setPaletteConfig();
		});
		
		TuiButton setNeonTheme = new TuiButton("Neon Theme", Placement.BottomRight, 3, 5, null, Palette.user).SetAction((s, ck) => {
			Palette.setNeon();
			closeMiddleScreen(); //update
			setPaletteConfig();
		});
		
		bool save(){
			if(!Color3.TryParse(user.Text, out Color3 cUser)){
				errorLabel.Text = "Invalid user color. Try again";
				return false;
			}
			if(!Color3.TryParse(main.Text, out Color3 cMain)){
				errorLabel.Text = "Invalid main color. Try again";
				return false;
			}
			if(!Color3.TryParse(background.Text, out Color3 cBackground)){
				errorLabel.Text = "Invalid background color. Try again";
				return false;
			}
			if(!Color3.TryParse(song.Text, out Color3 cSong)){
				errorLabel.Text = "Invalid song color. Try again";
				return false;
			}
			if(!Color3.TryParse(author.Text, out Color3 cAuthor)){
				errorLabel.Text = "Invalid author color. Try again";
				return false;
			}
			if(!Color3.TryParse(playlist.Text, out Color3 cPlaylist)){
				errorLabel.Text = "Invalid playlist color. Try again";
				return false;
			}
			if(!Color3.TryParse(hint.Text, out Color3 cHint)){
				errorLabel.Text = "Invalid hint color. Try again";
				return false;
			}
			if(!Color3.TryParse(info.Text, out Color3 cInfo)){
				errorLabel.Text = "Invalid info color. Try again";
				return false;
			}
			if(!Color3.TryParse(delimiter.Text, out Color3 cDelimiter)){
				errorLabel.Text = "Invalid delimiter color. Try again";
				return false;
			}
			
			Radio.config.Set("ui.palette.user", cUser);
			Radio.config.Set("ui.palette.song", cSong);
			Radio.config.Set("ui.palette.author", cAuthor);
			Radio.config.Set("ui.palette.playlist", cPlaylist);
			Radio.config.Set("ui.palette.main", cMain);
			Radio.config.Set("ui.palette.delimiter", cDelimiter);
			Radio.config.Set("ui.palette.hint", cHint);
			Radio.config.Set("ui.palette.info", cInfo);
			Radio.config.Set("ui.palette.background", cBackground);
			
			Radio.config.Set("ui.useColors", useCols.Checked);
			
			errorLabel.Text = "";
			
			Radio.config.Save();
			
			Palette.init();
			
			return true;
		}
		
		TuiButton done = new TuiButton("Done", Placement.BottomCenter, 0, 2, null, Palette.user).SetAction((s, ck) => {
			if(save()){
				closeMiddleScreen(); //update
				setPaletteConfig();
			}
		});
		
		TuiSelectable[,] t = new TuiSelectable[,]{{
			useCols, useCols, useCols
		},{
			user, main, background
		},{
			song, author, playlist
		},{
			hint, info, delimiter
		},{
			setAshTheme, setSubtleTheme, setNeonTheme
		},{
			done, done, done
		}};
		
		TuiScreenInteractive l = getMiddle(t);
		
		l.Elements.Add(new TuiLabel("Palette config", Placement.TopCenter, 0, 1, Palette.main));
		l.Elements.Add(new TuiLabel("Use colors:", Placement.TopCenter, -4, 5));
		
		l.Elements.Add(new TuiLabel("User:", Placement.TopLeft, 2, 8));
		l.Elements.Add(new TuiLabel("Main:", Placement.TopCenter, -5, 8));
		l.Elements.Add(new TuiLabel("Background:", Placement.TopRight, 13, 8));
		
		l.Elements.Add(new TuiLabel("Song:", Placement.TopLeft, 2, 11));
		l.Elements.Add(new TuiLabel("Author:", Placement.TopCenter, -6, 11));
		l.Elements.Add(new TuiLabel("Playlist:", Placement.TopRight, 13, 11));
		
		l.Elements.Add(new TuiLabel("Hint:", Placement.TopLeft, 2, 14));
		l.Elements.Add(new TuiLabel("Info:", Placement.TopCenter, -5, 14));
		l.Elements.Add(new TuiLabel("Delimiter:", Placement.TopRight, 13, 14));
		
		l.Elements.Add(errorLabel);
		
		l.SubKeyEvent(ConsoleKey.Escape, (s, ck) => {
			if(save()){
				closeMiddleScreen();
			}
		});
		
		setMiddleScreen(l);
	}
	
	void setPlayerConfig(){
		TuiFramedTextBox volumeExponent = setUFloat(new TuiFramedTextBox(Radio.py.volumeExponent.ToString(), 8, Placement.TopCenter, 8, 4, null, null, null, Palette.user, Palette.user));
		TuiFramedTextBox advanceTime = setUFloat(new TuiFramedTextBox(Radio.config.GetValue<float>("player.advanceTime").ToString(), 8, Placement.TopCenter, 8, 7, null, null, null, Palette.user, Palette.user));
		
		TuiLabel errorLabel = new TuiLabel("", Placement.BottomCenter, 0, 4, Palette.error);
		
		TuiButton reset = new TuiButton("Reset", Placement.BottomCenter, 0, 6, null, Palette.user).SetAction((s, ck) => {
			Radio.py.volumeExponent = 2f;
			Radio.config.Set("player.advanceTime", 5f);
			Radio.config.Save();
			
			closeMiddleScreen(); //update
			setPlayerConfig();
		});
		
		bool save(){
			if(!float.TryParse(volumeExponent.Text, out float f1)){
				errorLabel.Text = "Invalid volume exponent. Try again";
				return false;
			}
			if(!float.TryParse(advanceTime.Text, out float f2)){
				errorLabel.Text = "Invalid advance time. Try again";
				return false;
			}
			
			Radio.py.volumeExponent = f1;
			Radio.config.Set("player.advanceTime", f2);
			
			Radio.config.Save();
			
			errorLabel.Text = "";			
			return true;
		}
		
		TuiButton done = new TuiButton("Done", Placement.BottomCenter, 0, 2, null, Palette.user).SetAction((s, ck) => {
			save();
		});
		
		TuiSelectable[,] t = new TuiSelectable[,]{{
			volumeExponent
		},{
			advanceTime
		},{
			reset
		},{
			done
		}};
		
		TuiScreenInteractive l = getMiddle(t);
		
		l.Elements.Add(new TuiLabel("Player config", Placement.TopCenter, 0, 1, Palette.main));
		l.Elements.Add(new TuiLabel("Volume correction exponent:", Placement.TopCenter, -12, 5));
		l.Elements.Add(new TuiLabel("Advance time:", Placement.TopCenter, -5, 8));
		
		l.Elements.Add(errorLabel);
		
		l.SubKeyEvent(ConsoleKey.Escape, (s, ck) => {
			if(save()){
				closeMiddleScreen();
			}
		});
		
		setMiddleScreen(l);
	}
	
	void setPathConfig(){
		TuiFramedScrollingTextBox ffmpeg = new TuiFramedScrollingTextBox(Radio.config.GetValue<string>("ffmpegPath"), 256, 16, Placement.TopCenter, 7, 4, null, null, null, Palette.user, Palette.user);
		TuiFramedScrollingTextBox ytdlp = new TuiFramedScrollingTextBox(Radio.config.GetValue<string>("ytdlpPath"), 256, 16, Placement.TopCenter, 7, 7, null, null, null, Palette.user, Palette.user);
		
		TuiButton reset = new TuiButton("Reset", Placement.BottomCenter, 0, 6, null, Palette.user).SetAction((s, ck) => {
			Radio.config.Set("ffmpegPath", "ffmpeg");
			Radio.config.Set("ytdlpPath", "yt-dlp");
			Radio.config.Save();
			
			closeMiddleScreen(); //update
			setPathConfig();
		});
		
		TuiButton open1 = new TuiButton("Open ffmpeg.org", Placement.BottomLeft, 3, 3, null, Palette.user).SetAction((s, ck) => {
			openUrl("https://ffmpeg.org/");
		});
		
		TuiButton open2 = new TuiButton("Open yt-dlp downloads", Placement.BottomRight, 3, 3, null, Palette.user).SetAction((s, ck) => {
			openUrl("https://github.com/yt-dlp/yt-dlp/releases");
		});
		
		TuiButton auto = new TuiButton("Auto download all", Placement.BottomCenter, 0, 5, null, Palette.user).SetAction((s, ck) => {
			Radio.downloadFile("https://github.com/yt-dlp/yt-dlp/releases/latest/download/yt-dlp.exe",
			Radio.dep.path + "/yt-dlp.exe", async () => {
				ytdlp.Text = Radio.dep.path + "/yt-dlp.exe";
				Radio.config.Set("ytdlpPath", removeQuotesSingle(ytdlp.Text));
				Radio.config.Save();
			});
			
			Radio.downloadFile("https://www.gyan.dev/ffmpeg/builds/packages/ffmpeg-7.1.1-essentials_build.zip",
			Radio.dep.path + "/temp.zip", async () => {
				try{
					ZipFile.ExtractToDirectory(Radio.dep.path + "/temp.zip", Radio.dep.path + "/temp", true);
					
					string p = Directory.GetFiles(Radio.dep.path + "/temp", "ffmpeg.exe", SearchOption.AllDirectories).FirstOrDefault();
					File.Copy(p, Radio.dep.path + "/ffmpeg.exe");
					
					Directory.Delete(Radio.dep.path + "/temp", true);
					File.Delete(Radio.dep.path + "/temp.zip");
				}catch(Exception e){
					File.AppendAllText("error.log", e.ToString());
				}
				ffmpeg.Text = Radio.dep.path + "/ffmpeg.exe";
				Radio.config.Set("ffmpegPath", removeQuotesSingle(ffmpeg.Text));
				Radio.config.Save();
			});
		});
		
		void save(){	
			Radio.config.Set("ffmpegPath", removeQuotesSingle(ffmpeg.Text));
			Radio.config.Set("ytdlpPath", removeQuotesSingle(ytdlp.Text));
			
			Radio.config.Save();
		}
		
		TuiButton done = new TuiButton("Done", Placement.BottomCenter, 0, 2, null, Palette.user).SetAction((s, ck) => {
			save();
		});
		
		TuiSelectable[,] t = OperatingSystem.IsWindows() ? new TuiSelectable[,]{{
			ffmpeg, ffmpeg
		},{
			ytdlp, ytdlp
		},{
			auto, auto
		},{
			open1, open2
		},{
			done, done
		}} : new TuiSelectable[,]{{
			ffmpeg, ffmpeg
		},{
			ytdlp, ytdlp
		},{
			open1, open2
		},{
			done, done
		}};
		
		TuiScreenInteractive l = getMiddle(t);
		
		l.Elements.Add(new TuiLabel("Paths config", Placement.TopCenter, 0, 1, Palette.main));
		l.Elements.Add(new TuiLabel("FFMPEG path:", Placement.TopCenter, -10, 5));
		l.Elements.Add(new TuiLabel("YT-DLP path:", Placement.TopCenter, -10, 8));
		
		l.SubKeyEvent(ConsoleKey.Escape, (s, ck) => {
			save();
			closeMiddleScreen();
		});
		
		setMiddleScreen(l);
	}
	
	void setMiscConfig(){
		TuiFramedCheckBox usercp = new TuiFramedCheckBox(' ', 'X', !Radio.config.TryGetValue("dcrcp", out bool b) || b, Placement.TopCenter, 8, 4, null, null, null, Palette.user, Palette.user);
		
		TuiButton reset = new TuiButton("Reset", Placement.BottomCenter, 0, 6, null, Palette.user).SetAction((s, ck) => {
			Radio.config.Set("dcrcp", true);
			Radio.config.Save();
			
			usercp.Checked = true;
		});
		
		bool save(){
			Radio.config.Set("dcrcp", usercp.Checked);
			Radio.config.Save();
			
			if(usercp.Checked){
				if(Radio.dcrcp == null){
					Radio.dcrcp = new DiscordPresence();
				}
			}else{
				Radio.dcrcp?.Dispose();
				Radio.dcrcp = null;
			}
			
			return true;
		}
		
		TuiButton done = new TuiButton("Done", Placement.BottomCenter, 0, 2, null, Palette.user).SetAction((s, ck) => {
			save();
		});
		
		TuiSelectable[,] t = new TuiSelectable[,]{{
			usercp
		},{
			reset
		},{
			done
		}};
		
		TuiScreenInteractive l = getMiddle(t);
		
		l.Elements.Add(new TuiLabel("Miscellaneous config", Placement.TopCenter, 0, 1, Palette.main));
		l.Elements.Add(new TuiLabel("Use Discord RCP:", Placement.TopCenter, -12, 5));
		
		l.SubKeyEvent(ConsoleKey.Escape, (s, ck) => {
			if(save()){
				closeMiddleScreen();
			}
		});
		
		setMiddleScreen(l);
	}
	
	void setHelp(int page = 0){
		const int maxPage = 6;
		TuiScreenInteractive l = getMiddle(null);
		
		l.Elements.Add(new TuiLabel("Help - Page " + (page + 1), Placement.TopCenter, 0, 1, Palette.main));
		
		if(page > 0){
			l.Elements.Add(new TuiTwoLabels("N", " Previous page", Placement.BottomRight, 0, 1, Palette.info, null));
		}
		
		if(page < maxPage){
			l.Elements.Add(new TuiTwoLabels("M", " Next page", Placement.BottomRight, 0, 0, Palette.info, null));
		}
		
		l.SubKeyEvent(ConsoleKey.N, (s, ck) => {
			if(page != 0){
				closeMiddleScreen();
				setHelp(page - 1);
			}
		});
		
		l.SubKeyEvent(ConsoleKey.M, (s, ck) => {
			if(page != maxPage){
				closeMiddleScreen();
				setHelp(page + 1);
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
		
		setMiddleScreen(l);
	}
	
	void setMiddleScreen(TuiScreenInteractive m){
		TuiScreenInteractive previous = middle.Peek();
		middle.Push(m);
		master.ScreenList.Remove(previous);
		master.ScreenList.Add(m);
		setSelectedScreen(m);
		master.Elements.Remove(previous);
		master.Elements.Add(m);
	}
	
	void closeMiddleScreen(){
		if(middle.Count < 2){ //Comfirmation to close
			TuiSelectable[,] buttons = {{
				new TuiButton("Exit", Placement.Center, -4, 1, null, Palette.user).SetAction((s, ck) => {
					MultipleTuiScreenInteractive.StopPlaying(master, default);
					Environment.Exit(0);
				}),
				new TuiButton("Cancel", Placement.Center, 4, 1, null, Palette.user).SetAction((s, ck) => closeMiddleScreen())
			}};
			
			TuiScreenInteractive t = getMiddle(buttons);
			t.Elements.Add(new TuiLabel("Do you want to exit?", Placement.Center, 0, -1));
			t.Elements.Add(new TuiFrame(24, 7, Placement.Center, 0, 0, Palette.user));
			
			t.SubKeyEvent(ConsoleKey.Escape, (s, ck) => {
				MultipleTuiScreenInteractive.StopPlaying(master, default);
				Environment.Exit(0);
			}); //Escape + escape will close
			
			setMiddleScreen(t);
			
			return;
		}
		
		TuiScreenInteractive toDel = middle.Pop();
		master.ScreenList.Remove(toDel);
		master.ScreenList.Add(middle.Peek());
		
		master.Elements.Remove(toDel);
		master.Elements.Add(middle.Peek());
		
		master.Xsize = master.Xsize; //Triggers a resize to make sure the middle screen is the correct size
		
		setSelectedScreen(middle.Peek());
	}
	
	void prepareScreen(TuiScreenInteractive t){
		t.DeleteAllKeyEvents();
		
		t.SubKeyEvent(ConsoleKey.UpArrow, TuiScreenInteractive.MoveUp);
		t.SubKeyEvent(ConsoleKey.DownArrow, TuiScreenInteractive.MoveDown);
		t.SubKeyEvent(ConsoleKey.LeftArrow, TuiScreenInteractive.MoveLeft);
		t.SubKeyEvent(ConsoleKey.RightArrow, TuiScreenInteractive.MoveRight);
	}
	
	void setSelectedScreen(TuiScreenInteractive s){
		if(master == null){
			return;
		}
		if(master.SelectedScreen != null){
			master.SelectedScreen.DefFormat = null;
		}
		
		master.SelectedScreen = s;
		
		if(master.SelectedScreen != null){
			master.SelectedScreen.DefFormat = Palette.background;
		}
	}
	
	TuiScreenInteractive getMiddle(TuiSelectable[,] b){
		TuiScreenInteractive te = new TuiScreenInteractive(Math.Max((master?.Xsize ?? 100) - 62, 0),
			Math.Max((master?.Ysize ?? 20) - 6, 0),
			b, 0, 0, Placement.TopCenter, 0, 0, null,
			new TuiLabel("Ctrl+G", Placement.BottomLeft, 0, 0, Palette.hint));
		
		te.OnParentResize += (s, a) => {
			te.Xsize = Math.Max(a.X - 62, 0);
			te.Ysize = Math.Max(a.Y - 6, 0);
		};
		
		prepareScreen(te);
		
		return te;
	}
	
	//remove surrounding quotes
	static string removeQuotesSingle(string p){
		p = p.Trim();
		
		if(p.Length < 1){
			return p;
		}
		char[] c = p.ToCharArray();
		if(c[0] == '\"' && c[c.Length - 1] == '\"'){
			if(c.Length < 2){
				return "";
			}
			return p.Substring(1, p.Length - 2);
		}
		return p;
	}
	
	static void openUrl(string url){
		try{
			if(OperatingSystem.IsWindows()){
				Process.Start(new ProcessStartInfo{
					FileName = url,
					UseShellExecute = true
				});
			}
			else if(OperatingSystem.IsLinux()){
				Process.Start("xdg-open", url);
			}
			else if(OperatingSystem.IsMacOS()){
				Process.Start("open", url);
			}
		}
		catch(Exception e){}
	}
	
	static string crop(string s, int len){
		if(s.Length > len){
			return s.Substring(0, len - 1) + "…";
		}
		return s;
	}
	
	static TuiFramedTextBox setUFloat(TuiFramedTextBox b){
		b.CanWriteChar = c => {
			if(b.Text.Length + 1 > b.Length){
				return null;
			}
			if(char.IsDigit(c) || c == '.'){
				return c.ToString();
			}
			return null;
		};
		
		return b;
	}
	
	static TuiFramedTextBox setColor3(TuiFramedTextBox b){
		b.CanWriteChar = c => {
			if(b.Text.Length + 1 > b.Length){
				return null;
			}
			if(Uri.IsHexDigit(c) || c == '#'){
				return c.ToString();
			}
			return null;
		};
		
		return b;
	}
}