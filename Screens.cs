using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using AshLib.Time;
using AshLib.Formatting;
using AshConsoleGraphics;
using AshConsoleGraphics.Interactive;
using NAudio.CoreAudioApi;

public class Screens{
	MultipleTuiScreenInteractive master = null!;
	TuiScreenInteractive playing = null!;
	TuiNumberPicker volume = null!;
	
	TuiScreenInteractive session = null!;
	TuiScreenInteractive navigation = null!;
	TuiScreenInteractive queueScreen = null!;
	
	Stack<TuiScreenInteractive> middle = new();
	
	Stopwatch timer;
	double lastTime;
	
	int maxFps = 48;
	
	public Screens(){
		setupPlaying();
		setupSession();
		setupNavigation();
		
		TuiHorizontalLine d1 = new TuiHorizontalLine(100, 'a', Placement.BottomCenter, 0, 5);
		TuiVerticalLine d2 = new TuiVerticalLine(14, 'a', Placement.TopRight, 30, 0);
		TuiVerticalLine d3 = new TuiVerticalLine(14, 'a', Placement.TopLeft, 30, 0);
		
		d1.OnParentResize = s => {
			d1.Xsize = s.Xsize;
			d1.OffsetY = Math.Min((int) s.Ysize, 5);
		};
		
		d2.OnParentResize = s => {
			d2.Ysize = (uint) Math.Max((int) s.Ysize - 6, 0);
			d2.OffsetX = Math.Min((int) s.Xsize, 30);
		};
		
		d3.OnParentResize = s => {
			d3.Ysize = (uint) Math.Max((int) s.Ysize - 6, 0);
			d3.OffsetX = Math.Min((int) s.Xsize, 30);
		};
		
		TuiConnectedLinesScreen delimiters = new TuiConnectedLinesScreen(100, 20, new ILineElement[]{d1, d2, d3}, Palette.delimiter);
		
		delimiters.OnParentResize = s => {
			delimiters.Xsize = s.Xsize;
			delimiters.Ysize = s.Ysize;
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
		
		master = new MultipleTuiScreenInteractive(100, 20, new TuiScreenInteractive[]{playing, session, navigation, queueScreen, mid}, null, delimiters);
		
		master.Elements.Remove(queueScreen);
		
		master.AutoResize = true;
		
		Console.CursorVisible = false;
		
		master.OnResize = s => Console.CursorVisible = false;
		
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
			Radio.py.elapsed -= Radio.config.GetCamp<float>("player.advanceTime");
		});
		
		master.SubKeyEvent(ConsoleKey.L, ConsoleModifiers.None, (s, cki) => {
			Radio.py.elapsed += Radio.config.GetCamp<float>("player.advanceTime");
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
		
		double wantedDeltaTime = 1000d/(double)maxFps; //In ms
		
		master.FinishPlayCycleEvent = s => { //Wait some time to avoid enourmus cpu usage
			double realDeltaTime = (timer.Elapsed.TotalMilliseconds - lastTime); //In ms
			double waitTime = wantedDeltaTime - realDeltaTime;
			
			if(waitTime > 0){
				Thread.Sleep((int) waitTime); //Not precise but at least it aint busywait
			}
			
			lastTime = timer.Elapsed.TotalMilliseconds;
		};
		
		setSelectedScreen(navigation);
	}
	
	public void play(){
		timer = Stopwatch.StartNew();
		
		master.Play();
	}
	
	public void setupPlaying(){
		Song temp = Song.load(Radio.py.playingSong);
		TuiButton song = new TuiButton(temp?.title ?? "", Placement.TopLeft, 11, 1, Palette.song, Palette.user).SetAction((s, ck) => {
			setSongDetails(temp.id);
		});
		TuiTwoLabels authors = new TuiTwoLabels("Authors: ", temp == null ? "" : (temp.authors.Length == 0 ? "Unknown author" : (temp.authors.Length == 1 ? (Author.load(temp.authors[0])?.name ?? "Unknown author") : string.Join(", ", temp.authors.Select(n => (Author.load(n)?.name ?? "Unknown author"))))), Placement.BottomLeft, 2, 1, null, Palette.author);
		
		TuiProgressBar progress = new TuiProgressBar(70, '█', '░', Placement.Center, 0, 0, Palette.main, Palette.main);
		
		progress.OnParentResize = t => {
			progress.Xsize = (uint) Math.Max((int) t.Xsize - 30, 0);
		};
		
		TuiLabel elapsedTime = new TuiLabel("0:00", Placement.Center, -39, 0, Palette.info);
		TuiLabel totalTime = new TuiLabel("0:00", Placement.Center, 39, 0, Palette.info);
		
		elapsedTime.OnParentResize = t => {
			elapsedTime.OffsetX = -((int) t.Xsize - 30)/2 - 4;
		};
		
		totalTime.OnParentResize = t => {
			totalTime.OffsetX = ((int) t.Xsize - 30)/2 + 4;
		};
		
		int sec = (int) Radio.py.duration;
		totalTime.Text = (sec / 60) + ":" + (sec % 60).ToString("D2");
		
		TuiButton play = new TuiButton("‖", Placement.TopCenter, 0, 1, null, Palette.user); //► or ‖
		play.SetAction((s, cki) => {
			Radio.py.togglePause();
		});
		
		TuiButton prev = new TuiButton("≤", Placement.TopCenter, -6, 1, null, Palette.user).SetAction((s, cki) => Radio.py.prev());
		TuiButton next = new TuiButton("≥", Placement.TopCenter, 6, 1, null, Palette.user).SetAction((s, cki) => Radio.py.skip());
		
		TuiButton back = new TuiButton("▼", Placement.TopCenter, -12, 1, null, Palette.user).SetAction((s, cki) => Radio.py.elapsed -= Radio.config.GetCamp<float>("player.advanceTime"));
		TuiButton advance = new TuiButton("▲", Placement.TopCenter, 12, 1, null, Palette.user).SetAction((s, cki) => Radio.py.elapsed += Radio.config.GetCamp<float>("player.advanceTime"));
		
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
		
		playing.OnParentResize = t => {
			playing.Xsize = t.Xsize;
			playing.Ysize = Math.Min(t.Ysize, 5);
		};
		
		playing.FinishPlayCycleEvent = s => {
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
			Song s = Song.load(Radio.py.playingSong);
			
			song.Text = s?.title ?? "";
			authors.RightText = s == null ? "" : (s.authors.Length == 0 ? "Unknown author" : (s.authors.Length == 1 ? (Author.load(s.authors[0])?.name ?? "Unknown author") : string.Join(", ", s.authors.Select(n => (Author.load(n)?.name ?? "Unknown author")))));
			
			int sec = (int) Radio.py.duration;
			totalTime.Text = (sec / 60) + ":" + (sec % 60).ToString("D2");
		};
		
		Radio.py.onChangePlaystate += (se, e) => {
			play.Text = Radio.py.isPaused ? "‖" : "►";
		};
	}
	
	public void setupSession(){
		TuiLabel device = new TuiLabel(Radio.py.getCurrentDevice().FriendlyName, Placement.BottomLeft, 1, 2, Palette.info);
		
		TuiButton devices = new TuiButton("Change device", Placement.BottomCenter, 0, 1, null, Palette.user).SetAction((s, cki) => {
			var devs = Player.getDeviceList().ToList();
			
			TuiSelectable[,] buttons = new TuiSelectable[devs.Count, 1];
			
			for(int i = 0; i < devs.Count; i++){
				MMDevice d = devs[i].Value;
				int j = i;
				buttons[i, 0] = new TuiButton(devs[i].Key, Placement.TopCenter, 0, 6 + i, null, Palette.user).SetAction((s, cki) => {
					Radio.py.setDevice(d);
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
				name2 = Author.load(Session.sourceIdentifier)?.name ?? "Unknown author";
				f = Palette.author;
				break;
			case SourceType.Playlist:
				name = "Playlist";
				name2 = Playlist.load(Session.sourceIdentifier)?.title ?? "Untitled playlist";
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
					nam2 = Author.load(Session.sourceIdentifier)?.name ?? "Unknown author";
					f2 = Palette.author;
					break;
				case SourceType.Playlist:
					nam = "Playlist";
					nam2 = Playlist.load(Session.sourceIdentifier)?.title ?? "Untitled playlist";
					f2 = Palette.playlist;
					break;
			}
			
			sourceType.RightText = nam;
			source.Text = nam2;
			source.Format = f2;
		};
		
		TuiOptionPicker mode = new TuiOptionPicker(new string[]{"Order", "Shuffle", "Smart Shuffle"}, (uint) ((int) Session.mode), Placement.TopLeft, 3, 6, Palette.info, Palette.user);
		
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
					Song s = Song.load(queue[i]);
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
			
			queueScreen = new TuiScreenInteractive((uint) Math.Max(0, (int)session.Xsize - 4), (uint) Math.Max(0, (int)session.Ysize - 16), temp, 0, (uint) n, Placement.TopLeft, 2, 9, null);
			
			if(queue.Count == 0){
				queueScreen.Elements.Add(new TuiLabel("Empty", Placement.TopCenter, 0, 0));
			}
			
			if(queueScreen.Selected?.OffsetY >= queueScreen.Ysize){
				int dif = queueScreen.Selected.OffsetY - (int) queueScreen.Ysize + 1;
				foreach(TuiElement e in queueScreen){
					e.OffsetY -= dif;
				}
			}
			
			queueScreen.OnParentResize = s => {
				queueScreen.Xsize = (uint) Math.Max(0, (int)s.Xsize - 4);
				queueScreen.Ysize = (uint) Math.Max(0, (int)s.Ysize - 16);
				
				for(int i = 0; i < queueScreen.Elements.Count; i++){ //Reset scroll position on resize
					queueScreen.Elements[i].OffsetY = i;
				}
				
				if(queueScreen.Selected?.OffsetY >= queueScreen.Ysize){
					int dif = queueScreen.Selected.OffsetY - (int) queueScreen.Ysize + 1;
					foreach(TuiElement e in queueScreen){
						e.OffsetY -= dif;
					}
				}
			};
			
			queueScreen.DeleteAllKeyEvents();
			
			queueScreen.SubKeyEvent(ConsoleKey.UpArrow, (s, ck) => { //Scroll
				TuiScreenInteractive.MoveUp(s, ck);
				if(s.Selected.OffsetY < 0){
					foreach(TuiElement e in s){
						e.OffsetY++;
					}
				}
			});
			
			queueScreen.SubKeyEvent(ConsoleKey.DownArrow, (s, ck) => { //Scroll
				TuiScreenInteractive.MoveDown(s, ck);
				if(s.Selected.OffsetY >= s.Ysize){
					foreach(TuiElement e in s){
						e.OffsetY--;
					}
				}
			});
			
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
			}else if(master != null){
				TuiScreenInteractive temp2 = master.SelectedScreen;
				master.SelectedScreen = queueScreen;
				master.SelectedScreen = temp2;
			}
		}
		
		updateQueueScreen();
		
		Song.onLibraryUpdate += (s, a) => updateQueueScreen();
		
		Session.onQueueChange += (s, a) => updateQueueScreen();
		
		session.OnParentResize = t => {
			session.Xsize = Math.Min(30, t.Xsize);
			session.Ysize = (uint) Math.Max((int) t.Ysize - 6, 0);
		};
		
		prepareScreen(session);
		
		Radio.py.onChangeDevice += (s, a) => {
			device.Text = Radio.py.getCurrentDevice().FriendlyName;
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
		
		navigation.OnParentResize = t => {
			navigation.Xsize = Math.Min(30, t.Xsize);
			navigation.Ysize = (uint) Math.Max((int) t.Ysize - 6, 0);
		};
		
		prepareScreen(navigation);
	}
	
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
		
		title.OnParentResize = s => {
			title.BoxXsize = (uint) Math.Clamp((int) s.Xsize - 32, 16, 38);
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
		
		authors.OnParentResize = s => {
			authors.BoxXsize = (uint) Math.Clamp((int) s.Xsize - 32, 16, 38);
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
						setSongDetails(s);
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
				if(e.OffsetY > 3 && e.OffsetY < l.Ysize - 3){
					b.Text = titles[j];
				}else{
					b.Text = "";
				}
				j++;
			}
		}
		
		int toChange = Math.Max(0, (l.Selected?.OffsetY ?? 0) - (int) l.Ysize + 4);
		
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
			if(s.Selected?.OffsetY >= s.Ysize - 3){
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
		
		l.OnResize = s => {
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
		
		input.OnParentResize = s => {
			input.BoxXsize = (uint) Math.Max(0, (int) s.Xsize - 4);
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
				if(e.OffsetY > 3 && e.OffsetY < l.Ysize - 3){
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
			if(s.Selected?.OffsetY >= s.Ysize - 3){
				foreach(TuiElement e in s){
					if(e is TuiButton){
						e.OffsetY--;	
					}
				}
			}
			update();
		});
		
		l.OnResize = s => {
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
		
		name.OnParentResize = s => {
			name.BoxXsize = (uint) Math.Clamp((int) s.Xsize - 32, 16, 38);
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
					if(e.OffsetY > 7 && e.OffsetY < c.Ysize - 3){
						b.Text = titles[j];
					}else{
						b.Text = "";
					}
					j++;
				}
			}
			
			int toChange = Math.Max(0, (c.Selected?.OffsetY ?? 0) - (int) c.Ysize + 4);
			
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
				if(s.Selected.OffsetY >= s.Ysize - 3){
					foreach(TuiElement e in s){
						if(e is TuiButton){
							e.OffsetY--;	
						}
					}
				}
				update();
			});
			
			c.OnResize = s => {
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
				if(e.OffsetY > 3 && e.OffsetY < l.Ysize - 3){
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
			if(s.Selected?.OffsetY >= s.Ysize - 3){
				foreach(TuiElement e in s){
					if(e is TuiButton){
						e.OffsetY--;	
					}
				}
			}
			update();
		});
		
		l.OnResize = s => {
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
		
		title.OnParentResize = s => {
			title.BoxXsize = (uint) Math.Clamp((int) s.Xsize - 32, 16, 38);
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
					if(e.OffsetY > 7 && e.OffsetY < c.Ysize - 3){
						b.Text = titles[j];
					}else{
						b.Text = "";
					}
					j++;
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
				if(s.Selected.OffsetY >= s.Ysize - 3){
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
			
			c.OnResize = s => {
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
				if(e.OffsetY > 3 && e.OffsetY < l.Ysize - 3){
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
			if(s.Selected?.OffsetY >= s.Ysize - 3){
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
		
		l.OnResize = s => {
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
				if(e.OffsetY > 3 && e.OffsetY < l.Ysize - 3){
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
			if(s.Selected?.OffsetY >= s.Ysize - 3){
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
		
		l.OnResize = s => {
			update();
		};
		
		setMiddleScreen(l);
	}
	
	void setSearchPlaylist(Playlist p){
		TuiMultiLineScrollingFramedTextBox input = new TuiMultiLineScrollingFramedTextBox("", 256, 34, 3, Placement.TopCenter, 0, 4, null, null, null, Palette.user, Palette.user);
		
		input.OnParentResize = s => {
			input.BoxXsize = (uint) Math.Max(0, (int) s.Xsize - 4);
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
				if(e.OffsetY > 3 && e.OffsetY < l.Ysize - 3){
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
			if(s.Selected?.OffsetY >= s.Ysize - 3){
				foreach(TuiElement e in s){
					if(e is TuiButton){
						e.OffsetY--;	
					}
				}
			}
			update();
		});
		
		l.OnResize = s => {
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
				if(e.OffsetY > 3 && e.OffsetY < l.Ysize - 3){
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
			if(s.Selected?.OffsetY >= s.Ysize - 3){
				foreach(TuiElement e in s){
					if(e is TuiButton){
						e.OffsetY--;	
					}
				}
			}
			update();
		});
		
		l.OnResize = s => {
			update();
		};
		
		setMiddleScreen(l);
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
		TuiFramedScrollingTextBox title = new TuiFramedScrollingTextBox("", 256, 34, Placement.TopCenter, 0, 9, null, null, null, Palette.user, Palette.user);
		TuiFramedScrollingTextBox authors = new TuiFramedScrollingTextBox("", 256, 34, Placement.TopCenter, 0, 13, null, null, null, Palette.user, Palette.user);
		
		List<TuiLabel> error = new();
		
		TuiScreenInteractive l = null;
		
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
		
		path.OnParentResize = s => {
			path.BoxXsize = (uint) Math.Max(0, (int) s.Xsize - 4);
		};
		
		title.OnParentResize = s => {
			title.BoxXsize = (uint) Math.Max(0, (int) s.Xsize - 4);
		};
		
		authors.OnParentResize = s => {
			authors.BoxXsize = (uint) Math.Max(0, (int) s.Xsize - 4);
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
		
		l.Elements.Add(new TuiLabel("Import song from file", Placement.TopCenter, 0, 1, Palette.main));
		l.Elements.Add(new TuiLabel("Path:", Placement.TopLeft, 2, 4));
		l.Elements.Add(new TuiLabel("Title:", Placement.TopLeft, 2, 8));
		l.Elements.Add(new TuiLabel("Authors (separated by commas):", Placement.TopLeft, 1, 12));
		
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
		
		path.OnParentResize = s => {
			path.BoxXsize = (uint) Math.Max(0, (int) s.Xsize - 4);
		};
		
		title.OnParentResize = s => {
			title.BoxXsize = (uint) Math.Max(0, (int) s.Xsize - 4);
		};
		
		authors.OnParentResize = s => {
			authors.BoxXsize = (uint) Math.Max(0, (int) s.Xsize - 4);
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
		TuiFramedScrollingTextBox authors = new TuiFramedScrollingTextBox("", 256, 34, Placement.TopCenter, 0, 9, null, null, null, Palette.user, Palette.user);
		
		List<TuiLabel> error = new();
		
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
		
		path.OnParentResize = s => {
			path.BoxXsize = (uint) Math.Max(0, (int) s.Xsize - 4);
		};
		
		authors.OnParentResize = s => {
			authors.BoxXsize = (uint) Math.Max(0, (int) s.Xsize - 4);
		};
		
		TuiSelectable[,] t = new TuiSelectable[,]{{
			path
		},{
			authors
		},{
			import
		}};
		
		l = getMiddle(t);
		
		l.Elements.Add(new TuiLabel("Import songs from folder", Placement.TopCenter, 0, 1, Palette.main));
		l.Elements.Add(new TuiLabel("Folder path:", Placement.TopLeft, 2, 4));
		l.Elements.Add(new TuiLabel("Authors (separated by commas):", Placement.TopLeft, 1, 8));
		
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
		
		path.OnParentResize = s => {
			path.BoxXsize = (uint) Math.Max(0, (int) s.Xsize - 4);
		};
		
		authors.OnParentResize = s => {
			authors.BoxXsize = (uint) Math.Max(0, (int) s.Xsize - 4);
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
		TuiFramedScrollingTextBox title = new TuiFramedScrollingTextBox("", 256, 34, Placement.TopCenter, 0, 9, null, null, null, Palette.user, Palette.user);
		TuiFramedScrollingTextBox authors = new TuiFramedScrollingTextBox("", 256, 34, Placement.TopCenter, 0, 13, null, null, null, Palette.user, Palette.user);
		
		List<TuiLabel> error = new();
		
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
		
		path.OnParentResize = s => {
			path.BoxXsize = (uint) Math.Max(0, (int) s.Xsize - 4);
		};
		
		title.OnParentResize = s => {
			title.BoxXsize = (uint) Math.Max(0, (int) s.Xsize - 4);
		};
		
		authors.OnParentResize = s => {
			authors.BoxXsize = (uint) Math.Max(0, (int) s.Xsize - 4);
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
		
		l.Elements.Add(new TuiLabel("Import playlist from folder", Placement.TopCenter, 0, 1, Palette.main));
		l.Elements.Add(new TuiLabel("Folder path:", Placement.TopLeft, 2, 4));
		l.Elements.Add(new TuiLabel("Playlist title:", Placement.TopLeft, 2, 8));
		l.Elements.Add(new TuiLabel("Authors (separated by commas):", Placement.TopLeft, 1, 12));
		
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
		
		path.OnParentResize = s => {
			path.BoxXsize = (uint) Math.Max(0, (int) s.Xsize - 4);
		};
		
		title.OnParentResize = s => {
			title.BoxXsize = (uint) Math.Max(0, (int) s.Xsize - 4);
		};
		
		authors.OnParentResize = s => {
			authors.BoxXsize = (uint) Math.Max(0, (int) s.Xsize - 4);
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
		}};
		
		TuiScreenInteractive l = getMiddle(t);
		
		l.Elements.Add(new TuiLabel("Config", Placement.TopCenter, 0, 1, Palette.main));
		l.Elements.Add(new TuiTwoLabels("AshRadio v" + Radio.version, " made by siljam", Placement.BottomRight, 0, 0, Palette.hint, null));
		
		setMiddleScreen(l);
	}
	
	void setPaletteConfig(){
		TuiFramedCheckBox useCols = new TuiFramedCheckBox(' ', 'X', Radio.config.GetCamp<bool>("ui.useColors"), Placement.TopCenter, 4, 4, null, null, null, Palette.user, Palette.user);
		
		TuiFramedScrollingTextBoxColor3 user = new TuiFramedScrollingTextBoxColor3(Palette.user.foreground.ToString(), 8, Placement.TopLeft, 8, 7, null, Palette.user, Palette.user);
		TuiFramedScrollingTextBoxColor3 main = new TuiFramedScrollingTextBoxColor3(Palette.main.foreground.ToString(), 8, Placement.TopCenter, 4, 7, null, Palette.main, Palette.user);
		TuiFramedScrollingTextBoxColor3 background = new TuiFramedScrollingTextBoxColor3(Palette.background.background.ToString(), 8, Placement.TopRight, 1, 7, null, null, Palette.user);
		
		TuiFramedScrollingTextBoxColor3 song = new TuiFramedScrollingTextBoxColor3(Palette.song.foreground.ToString(), 8, Placement.TopLeft, 8, 10, null, Palette.song, Palette.user);
		TuiFramedScrollingTextBoxColor3 author = new TuiFramedScrollingTextBoxColor3(Palette.author.foreground.ToString(), 8, Placement.TopCenter, 4, 10, null, Palette.author, Palette.user);
		TuiFramedScrollingTextBoxColor3 playlist = new TuiFramedScrollingTextBoxColor3(Palette.playlist.foreground.ToString(), 8, Placement.TopRight, 1, 10, null, Palette.playlist, Palette.user);
		
		TuiFramedScrollingTextBoxColor3 hint = new TuiFramedScrollingTextBoxColor3(Palette.hint.foreground.ToString(), 8, Placement.TopLeft, 8, 13, null, Palette.hint, Palette.user);
		TuiFramedScrollingTextBoxColor3 info = new TuiFramedScrollingTextBoxColor3(Palette.info.foreground.ToString(), 8, Placement.TopCenter, 4, 13, null, Palette.info, Palette.user);
		TuiFramedScrollingTextBoxColor3 delimiter = new TuiFramedScrollingTextBoxColor3(Palette.delimiter.foreground.ToString(), 8, Placement.TopRight, 1, 13, null, Palette.delimiter, Palette.user);
		
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
			
			Radio.config.SetCamp("ui.palette.user", cUser);
			Radio.config.SetCamp("ui.palette.song", cSong);
			Radio.config.SetCamp("ui.palette.author", cAuthor);
			Radio.config.SetCamp("ui.palette.playlist", cPlaylist);
			Radio.config.SetCamp("ui.palette.main", cMain);
			Radio.config.SetCamp("ui.palette.delimiter", cDelimiter);
			Radio.config.SetCamp("ui.palette.hint", cHint);
			Radio.config.SetCamp("ui.palette.info", cInfo);
			Radio.config.SetCamp("ui.palette.background", cBackground);
			
			Radio.config.SetCamp("ui.useColors", useCols.Checked);
			
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
		TuiFramedTextBoxFloat volumeExponent = new TuiFramedTextBoxFloat(Radio.py.volumeExponent.ToString(), 8, Placement.TopCenter, 8, 4, null, null, null, Palette.user, Palette.user);
		TuiFramedTextBoxFloat advanceTime = new TuiFramedTextBoxFloat(Radio.config.GetCamp<float>("player.advanceTime").ToString(), 8, Placement.TopCenter, 8, 7, null, null, null, Palette.user, Palette.user);
		
		TuiLabel errorLabel = new TuiLabel("", Placement.BottomCenter, 0, 4, Palette.error);
		
		TuiButton reset = new TuiButton("Reset", Placement.BottomCenter, 0, 6, null, Palette.user).SetAction((s, ck) => {
			Radio.py.volumeExponent = 2f;
			Radio.config.SetCamp("player.advanceTime", 5f);
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
			Radio.config.SetCamp("player.advanceTime", f2);
			
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
		TuiFramedScrollingTextBox ffmpeg = new TuiFramedScrollingTextBox(Radio.config.GetCamp<string>("ffmpegPath"), 256, 16, Placement.TopCenter, 7, 4, null, null, null, Palette.user, Palette.user);
		TuiFramedScrollingTextBox ytdlp = new TuiFramedScrollingTextBox(Radio.config.GetCamp<string>("ytdlpPath"), 256, 16, Placement.TopCenter, 7, 7, null, null, null, Palette.user, Palette.user);
		
		TuiButton reset = new TuiButton("Reset", Placement.BottomCenter, 0, 6, null, Palette.user).SetAction((s, ck) => {
			Radio.config.SetCamp("ffmpegPath", "ffmpeg");
			Radio.config.SetCamp("ytdlpPath", "yt-dlp");
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
		
		TuiButton auto = new TuiButton("Auto download yt-dlp", Placement.BottomCenter, 0, 5, null, Palette.user).SetAction((s, ck) => {
			downloadFile("https://github.com/yt-dlp/yt-dlp/releases/latest/download/yt-dlp.exe",
			Radio.dep.path + "/yt-dlp.exe", () => {
				ytdlp.Text = Radio.dep.path + "/yt-dlp.exe";
				Radio.config.SetCamp("ytdlpPath", removeQuotesSingle(ytdlp.Text));
				Radio.config.Save();
			});
		});
		
		void save(){	
			Radio.config.SetCamp("ffmpegPath", removeQuotesSingle(ffmpeg.Text));
			Radio.config.SetCamp("ytdlpPath", removeQuotesSingle(ytdlp.Text));
			
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
	
	void setHelp(int page = 0){
		const int maxPage = 4;
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
				l.Elements.Add(new TuiTwoLabels("J", " go back X seconds (adavance time)", Placement.TopLeft, 4, 8, Palette.info, null));
				l.Elements.Add(new TuiTwoLabels("L", " go forward X seconds (adavance time)", Placement.TopLeft, 4, 9, Palette.info, null));
				l.Elements.Add(new TuiTwoLabels("N", " previous song", Placement.TopLeft, 4, 10, Palette.info, null));
				l.Elements.Add(new TuiTwoLabels("M", " next song", Placement.TopLeft, 4, 11, Palette.info, null));
				break;
			case 4:
				l.Elements.Add(new TuiLabel("Internal operation", Placement.TopLeft, 2, 4, Palette.info));
				l.Elements.Add(new TuiLabel("AshRadio uses numerical ids for songs, authors and", Placement.TopLeft, 3, 5));
				l.Elements.Add(new TuiLabel("playlists.", Placement.TopLeft, 3, 6));
				l.Elements.Add(new TuiLabel("2147483647 is the maximum id. Try not importing that many songs!", Placement.TopLeft, 3, 7));
				l.Elements.Add(new TuiLabel("For the audio playing, CsCore is used. This .net library", Placement.TopLeft, 3, 8));
				l.Elements.Add(new TuiLabel("makes it really easy to play audio files.", Placement.TopLeft, 3, 9));
				l.Elements.Add(new TuiLabel("For data storage and many other tasks, AshLib is used.", Placement.TopLeft, 3, 9));
				l.Elements.Add(new TuiLabel("This .net library (made by me!) handles AshFiles.", Placement.TopLeft, 3, 9));
				l.Elements.Add(new TuiLabel("The UI in the console is made using AshConsoleGraphics.", Placement.TopLeft, 3, 9));
				l.Elements.Add(new TuiLabel(".net library also made by me.", Placement.TopLeft, 3, 9));
				break;
		}
		
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
		TuiScreenInteractive te = new TuiScreenInteractive((uint) Math.Max((int) (master?.Xsize ?? 100) - 62, 0),
			(uint) Math.Max((int) (master?.Ysize ?? 20) - 6, 0),
			b, 0, 0, Placement.TopCenter, 0, 0, null,
			new TuiLabel("Ctrl+G", Placement.BottomLeft, 0, 0, Palette.hint));
		
		te.OnParentResize = t => {
			te.Xsize = (uint) Math.Max((int) t.Xsize - 62, 0);
			te.Ysize = (uint) Math.Max((int) t.Ysize - 6, 0);
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
	
	static async Task downloadFile(string url, string outputPath, Action onComplete){
		using HttpClient client = new HttpClient();
		using HttpResponseMessage response = await client.GetAsync(url);
		response.EnsureSuccessStatusCode();
		
		Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
		
		await using FileStream fs = new FileStream(outputPath, FileMode.Create);
		await response.Content.CopyToAsync(fs);
		
		onComplete(); // Lambda executed after file is saved
	}
}