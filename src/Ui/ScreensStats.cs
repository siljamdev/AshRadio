using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using AshLib.Time;
using AshLib.Formatting;
using AshConsoleGraphics;
using AshConsoleGraphics.Interactive;

public partial class Screens{
	void setStatsSelect(){
		if(currentMiddleScreen.identifier == "statsSelect"){
			setSelectedScreen(currentMiddleScreen);
			return;
		}
		
		TuiFramedScrollingTextBox start = new TuiFramedScrollingTextBox(MonthDate.Now.ToNumbers2(), 7, 16, Placement.TopCenter, 0, 5, null, null, null, Palette.user, Palette.user);
		TuiFramedScrollingTextBox end = new TuiFramedScrollingTextBox(MonthDate.Now.ToNumbers2(), 7, 16, Placement.TopCenter, 0, 10, null, null, null, Palette.user, Palette.user);
		
		start.CanWriteChar = c => {
			if(start.Text.Length + 1 > start.Length){
				return null;
			}
			if(char.IsDigit(c) || c == '/'){
				return c.ToString();
			}
			return null;
		};
		
		end.CanWriteChar = c => {
			if(end.Text.Length + 1 > end.Length){
				return null;
			}
			if(char.IsDigit(c) || c == '/'){
				return c.ToString();
			}
			return null;
		};
		
		TuiButton wrapped = new TuiButton("Set last year", Placement.TopCenter, 0, 14, null, Palette.user).SetAction((s2, ck) => {
			MonthDate n = MonthDate.Now;
			MonthDate p = new MonthDate(n.month, (short) (n.year - 1)).NextMonth();
			start.Text = p.ToNumbers2();
			end.Text = n.ToNumbers2();
		});
		
		TuiLabel error = new TuiLabel("", Placement.BottomCenter, 0, 5, Palette.error);
		
		TuiScreenInteractive l = null;
		
		MiddleScreen c2 = null!;
		
		bool tryParseDates(out MonthDate sd, out MonthDate ed){
			sd = default;
			ed = default;
			
			if(!MonthDate.TryParse(start.Text, out MonthDate s)){
				error.Text = "Invalid start date";
				return false;
			}
			
			if(!MonthDate.TryParse(end.Text, out MonthDate e)){
				error.Text = "Invalid end date";
				return false;
			}
			
			if(s > e){
				error.Text = "Start date later than end date";
				return false;
			}
			
			error.Text = "";
			sd = s;
			ed = e;
			return true;
		}
		
		TuiButton seeSongs = new TuiButton("See top songs", Placement.BottomLeft, 3, 4, null, Palette.user).SetAction((s2, ck) => {
			if(!tryParseDates(out MonthDate s, out MonthDate e)){
				return;
			}
			
			Task.Run(() => {
				(float totalTime, Dictionary<int, (uint, float)> stt) = Stats.getStats(s, e);
				
				Dictionary<int, (uint, float, float, uint)> stats = Stats.getRealStats(stt);
				
				Dictionary<int, (float, float, uint, int)> aut = Stats.getAuthorStats(totalTime, stats);
				
				return (totalTime, stats, aut);
			}).ContinueWith(t => {
				setSongStats(s, e, t.Result.Item1, t.Result.Item2, t.Result.Item3, 0);
				removeMiddleScreen(c2);
			});
		});
		
		TuiButton seeAuthors = new TuiButton("See top authors", Placement.BottomRight, 3, 4, null, Palette.user).SetAction((s2, ck) => {
			if(!tryParseDates(out MonthDate s, out MonthDate e)){
				return;
			}
			
			Task.Run(() => {
				(float totalTime, Dictionary<int, (uint, float)> stt) = Stats.getStats(s, e);
				
				Dictionary<int, (uint, float, float, uint)> stats = Stats.getRealStats(stt);
				
				Dictionary<int, (float, float, uint, int)> aut = Stats.getAuthorStats(totalTime, stats);
				
				return (totalTime, stats, aut);
			}).ContinueWith(t => {
				setAuthorStats(s, e, t.Result.Item1, t.Result.Item2, t.Result.Item3, 0);
				removeMiddleScreen(c2);
			});
		});
		
		TuiButton flashcard = new TuiButton("See flashcard", Placement.BottomCenter, 0, 2, null, Palette.user).SetAction((s2, ck) => {
			if(!tryParseDates(out MonthDate s, out MonthDate e)){
				return;
			}
			
			Task.Run(() => {
				(float totalTime, Dictionary<int, (uint, float)> stt) = Stats.getStats(s, e);
				
				Dictionary<int, (uint, float, float, uint)> stats = Stats.getRealStats(stt);
				
				Dictionary<int, (float, float, uint, int)> aut = Stats.getAuthorStats(totalTime, stats);
				
				return (totalTime, stats, aut);
			}).ContinueWith(t => {
				setFlashCard(s, e, t.Result.Item1, t.Result.Item2, t.Result.Item3);
				removeMiddleScreen(c2);
			});
		});
		
		TuiSelectable[,] t = new TuiSelectable[,]{{
			start, start
		},{
			end, end
		},{
			wrapped, wrapped
		},{
			seeSongs, seeAuthors
		},{
			flashcard, flashcard
		}};
		
		l = generateMiddleInteractive(t);
		
		l.Elements.Add(new TuiLabel("Select stats", Placement.TopCenter, 0, 1, Palette.main));
		l.Elements.Add(new TuiLabel("Start (mm/yyyy):", Placement.TopCenter, 0, 4));
		l.Elements.Add(new TuiLabel("End (mm/yyyy):", Placement.TopCenter, 0, 9));
		l.Elements.Add(error);
		
		c2 = new MiddleScreen(l, "statsSelect");
		
		setMiddleScreen(c2);
	}
	
	void setSongStats(MonthDate start, MonthDate end, float totalTime, Dictionary<int, (uint, float, float, uint)> stats, Dictionary<int, (float, float, uint, int)> aut, byte order, Song selected = null){
		List<Song> songs = stats.Keys.Select(h => Song.get(h)).Where(h => h != null).ToList();
		
		//Reorder songs
		switch(order){
			case 0: //Number of times listened
				songs = songs.OrderByDescending(h => stats[h.id].Item4).ToList();
				break;
			
			case 1: //Total time
				songs = songs.OrderByDescending(h => stats[h.id].Item2).ToList();
				break;
			
			case 2: //Number of times loaded
				songs = songs.OrderByDescending(h => stats[h.id].Item1).ToList();
				break;
		}
		
		//Build buttons
		TuiSelectable[,] t = new TuiSelectable[songs.Count, 1];
		for(int i = 0; i < songs.Count; i++){
			Song s = songs[i];
			TuiButton b = new TuiButton("", Placement.TopLeft, 2 + (i + 1).ToString().Length, 3 * i, Palette.song, Palette.user);
			
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
		
		TuiScrollingScreenInteractive l = null!;
		
		void updateTitles(){
			for(int i = 0; i < songs.Count; i++){
				((TuiButton) t[i, 0]).Text = crop(songs[i]?.title ?? Song.nullTitle, l.Xsize - 10);
			}
		}
		
		//Static screen
		TuiScreen backg = generateMiddleStatic();
		
		backg.Elements.Add(new TuiLabel("Stats", Placement.TopCenter, 0, 1, Palette.main));
		
		backg.Elements.Add(new TuiMultipleLabels(new string[]{start.ToString(), " - ", end.ToString()}, Placement.TopCenter, 0, 3, new CharFormat?[]{Palette.info, null, Palette.info}));
		backg.Elements.Add(new TuiTwoLabels("Total time listened: ", secondsToReadable(totalTime) + " (" + (totalTime / 60f).ToString("F0") + " mins)", Placement.TopCenter, 0, 4, null, Palette.info));
		
		backg.Elements.Add(new TuiLabel("Top songs:", Placement.TopLeft, 2, 5));
		
		//Inner screen
		l = new TuiScrollingScreenInteractive(Math.Max(backg.Xsize - 6, 0),
			Math.Max(backg.Ysize - 9, 0),
			t, 0, (uint) Math.Max(0, songs.IndexOf(selected)),
			Placement.TopLeft, 3, 6,
			null
		);
		
		backg.Elements.Add(l);
		
		prepareScreen(l);
		
		//Other info
		for(int i = 0; i < songs.Count; i++){
			l.Elements.Add(new TuiLabel((i + 1) + ".", Placement.TopLeft, 0, i * 3));
			
			l.Elements.Add(new TuiLabel(secondsToMinuteTime(stats[songs[i].id].Item3), Placement.TopRight, 0, i * 3, Palette.info));
			
			l.Elements.Add(new TuiTwoLabels("Listened for: ", secondsToHourTime(stats[songs[i].id].Item2), Placement.TopLeft, 5, i * 3 + 1, null, Palette.info));
			
			l.Elements.Add(new TuiMultipleLabels(new string[]{stats[songs[i].id].Item1.ToString(), " times loaded   ", stats[songs[i].id].Item4.ToString(), " times listened"}, Placement.TopLeft, 5, i * 3 + 2, new CharFormat?[]{Palette.info, null, Palette.info, null}));
		}
		
		l.OnParentResize += (s, a) => {
			l.Xsize = Math.Max(backg.Xsize - 6, 0);
			l.Ysize = Math.Max(backg.Ysize - 9, 0);
			updateTitles();
		};
		
		//Trigger first update
		updateTitles();
		
		MiddleScreen c2 = new MiddleScreen(backg, l);
		
		//To switch to other types
		switch(order){
			case 0: //Number of times listened
				backg.Elements.Add(new TuiTwoLabels("2", " Order by total time", Placement.BottomRight, 0, 1, Palette.hint, null));
				backg.Elements.Add(new TuiTwoLabels("3", " Order by times loaded", Placement.BottomRight, 0, 2, Palette.hint, null));
				l.SubKeyEvent(ConsoleKey.D2, (s, ck) => {
					setSongStats(start, end, totalTime, stats, aut, 1, songs.Count > 0 ? songs[(int) l.MatrixPointerY] : null);
					
					removeMiddleScreen(c2);
				});
				l.SubKeyEvent(ConsoleKey.D3, (s, ck) => {
					setSongStats(start, end, totalTime, stats, aut, 2, songs.Count > 0 ? songs[(int) l.MatrixPointerY] : null);
					
					removeMiddleScreen(c2);
				});
				break;
			
			case 1: //Total time
				backg.Elements.Add(new TuiTwoLabels("1", " Order by times listened", Placement.BottomRight, 0, 1, Palette.hint, null));
				backg.Elements.Add(new TuiTwoLabels("3", " Order by times loaded", Placement.BottomRight, 0, 2, Palette.hint, null));
				l.SubKeyEvent(ConsoleKey.D1, (s, ck) => {
					setSongStats(start, end, totalTime, stats, aut, 0, songs.Count > 0 ? songs[(int) l.MatrixPointerY] : null);
					
					removeMiddleScreen(c2);
				});
				l.SubKeyEvent(ConsoleKey.D3, (s, ck) => {
					setSongStats(start, end, totalTime, stats, aut, 2, songs.Count > 0 ? songs[(int) l.MatrixPointerY] : null);
					
					removeMiddleScreen(c2);
				});
				break;
			
			case 2: //Number of times loaded
				backg.Elements.Add(new TuiTwoLabels("1", " Order by times listened", Placement.BottomRight, 0, 1, Palette.hint, null));
				backg.Elements.Add(new TuiTwoLabels("2", " Order by total time", Placement.BottomRight, 0, 2, Palette.hint, null));
				l.SubKeyEvent(ConsoleKey.D1, (s, ck) => {
					setSongStats(start, end, totalTime, stats, aut, 0, songs.Count > 0 ? songs[(int) l.MatrixPointerY] : null);
					
					removeMiddleScreen(c2);
				});
				l.SubKeyEvent(ConsoleKey.D2, (s, ck) => {
					setSongStats(start, end, totalTime, stats, aut, 1, songs.Count > 0 ? songs[(int) l.MatrixPointerY] : null);
					
					removeMiddleScreen(c2);
				});
				break;
		}
		
		backg.Elements.Add(new TuiTwoLabels("U", " See top authors", Placement.BottomRight, 0, 0, Palette.hint, null));
		l.SubKeyEvent(ConsoleKey.U, (s, ck) => {
			setAuthorStats(start, end, totalTime, stats, aut, 0);
			
			removeMiddleScreen(c2);
		});
		
		setMiddleScreen(c2);
	}
	
	void setAuthorStats(MonthDate start, MonthDate end, float totalTime, Dictionary<int, (uint, float, float, uint)> stats, Dictionary<int, (float, float, uint, int)> aut, byte order, Author selected = null){
		List<Author> authors = aut.Keys.Select(h => Author.get(h)).Where(h => h != null).ToList();
		
		//Reorder authors
		switch(order){
			case 0: //Total time
				authors = authors.OrderByDescending(h => aut[h.id].Item1).ToList();
				break;
			
			case 1: //Total number of songs
				authors = authors.OrderByDescending(h => aut[h.id].Item3).ToList();
				break;
		}
		
		//Build buttons
		TuiSelectable[,] t = new TuiSelectable[authors.Count, 1];
		for(int i = 0; i < authors.Count; i++){
			Author a = authors[i];
			TuiButton b = new TuiButton("", Placement.TopLeft, 2 + (i + 1).ToString().Length, 3 * i, Palette.author, Palette.user);
			
			b.SetAction((s2, ck) => {
				setAuthorDetails(a);
			});
			
			b.SubKeyEvent(ConsoleKey.S, (s2, ck) => {
				Session.setSource(SourceType.Author, a.id);
			});
			
			t[i, 0] = b;
		}
		
		TuiScrollingScreenInteractive l = null!;
		
		void updateTitles(){
			for(int i = 0; i < authors.Count; i++){
				((TuiButton) t[i, 0]).Text = crop(authors[i]?.name ?? Author.nullName, l.Xsize - 11);
			}
		}
		
		//Static screen
		TuiScreen backg = generateMiddleStatic();
		
		backg.Elements.Add(new TuiLabel("Stats", Placement.TopCenter, 0, 1, Palette.main));
		
		backg.Elements.Add(new TuiMultipleLabels(new string[]{start.ToString(), " - ", end.ToString()}, Placement.TopCenter, 0, 3, new CharFormat?[]{Palette.info, null, Palette.info}));
		backg.Elements.Add(new TuiTwoLabels("Total time listened: ", secondsToReadable(totalTime) + " (" + (totalTime / 60f).ToString("F0") + " mins)", Placement.TopCenter, 0, 4, null, Palette.info));
		
		backg.Elements.Add(new TuiLabel("Top authors:", Placement.TopLeft, 2, 5));
		
		//Inner screen
		l = new TuiScrollingScreenInteractive(Math.Max(backg.Xsize - 6, 0),
			Math.Max(backg.Ysize - 9, 0),
			t, 0, (uint) Math.Max(0, authors.IndexOf(selected)),
			Placement.TopLeft, 3, 6,
			null
		);
		
		backg.Elements.Add(l);
		
		prepareScreen(l);
		
		//Other info
		for(int i = 0; i < authors.Count; i++){
			l.Elements.Add(new TuiLabel((i + 1) + ".", Placement.TopLeft, 0, i * 3));
			
			l.Elements.Add(new TuiLabel(aut[authors[i].id].Item2.ToString("F2") + "%", Placement.TopRight, 0, i * 3, Palette.info));
			
			l.Elements.Add(new TuiMultipleLabels(new string[]{"Listened for: ", secondsToHourTime(aut[authors[i].id].Item1), "   ", aut[authors[i].id].Item3.ToString(), " songs listened"}, Placement.TopLeft, 5, i * 3 + 1, new CharFormat?[]{null, Palette.info, null, Palette.info, null}));
			
			l.Elements.Add(new TuiTwoLabels("Top song: ", (Song.get(aut[authors[i].id].Item4)?.title ?? Song.nullTitle), Placement.TopLeft, 5, i * 3 + 2, null, Palette.song));
		}
		
		l.OnParentResize += (s, a) => {
			l.Xsize = Math.Max(backg.Xsize - 6, 0);
			l.Ysize = Math.Max(backg.Ysize - 9, 0);
			updateTitles();
		};
		
		//Trigger first update
		updateTitles();
		
		MiddleScreen c2 = new MiddleScreen(backg, l);
		
		//To switch to other types
		switch(order){
			case 0: //Total time
				backg.Elements.Add(new TuiTwoLabels("2", " Order by songs listened", Placement.BottomRight, 0, 1, Palette.hint, null));
				l.SubKeyEvent(ConsoleKey.D2, (s, ck) => {
					setAuthorStats(start, end, totalTime, stats, aut, 1, authors.Count > 0 ? authors[(int) l.MatrixPointerY] : null);
					
					removeMiddleScreen(c2);
				});
				break;
			
			case 1: //Number of songs listened
				backg.Elements.Add(new TuiTwoLabels("1", " Order by total time", Placement.BottomRight, 0, 1, Palette.hint, null));
				l.SubKeyEvent(ConsoleKey.D1, (s, ck) => {
					setAuthorStats(start, end, totalTime, stats, aut, 0, authors.Count > 0 ? authors[(int) l.MatrixPointerY] : null);
					
					removeMiddleScreen(c2);
				});
				break;
		}
		
		backg.Elements.Add(new TuiTwoLabels("L", " See top songs", Placement.BottomRight, 0, 0, Palette.hint, null));
		l.SubKeyEvent(ConsoleKey.L, (s, ck) => {
			setSongStats(start, end, totalTime, stats, aut, 0);
			
			removeMiddleScreen(c2);
		});
		
		setMiddleScreen(c2);
	}
	
	void setFlashCard(MonthDate start, MonthDate end, float totalTime, Dictionary<int, (uint, float, float, uint)> stats, Dictionary<int, (float, float, uint, int)> aut){
		List<Song> songs = stats.Keys.Select(h => Song.get(h)).Where(h => h != null).ToList();
		List<Author> authors = aut.Keys.Select(h => Author.get(h)).Where(h => h != null).ToList();
		
		songs = songs.OrderByDescending(h => stats[h.id].Item4).ToList();
		authors = authors.OrderByDescending(h => aut[h.id].Item1).ToList();
		
		MiddleScreen l3 = generateMiddle(null);
		TuiScreenInteractive l = l3.interactive;
		
		TuiFrame frame = new TuiFrame(Math.Max(0, l.Xsize - 4), 15, Placement.TopLeft, 0, 0);
		frame.OnParentResize += (s, a) => {
			frame.Xsize = Math.Max(0, a.X);
		};
		
		TuiHorizontalLine hor = new TuiHorizontalLine(Math.Max(0, l.Xsize - 6), 'a', Placement.TopLeft, 1, 2);
		hor.OnParentResize += (s, a) => {
			hor.Xsize = Math.Max(0, a.X - 2);
		};
		
		TuiVerticalLine ver = new TuiVerticalLine(11, 'a', Placement.TopCenter, 0, 3);
		
		TuiConnectedLinesScreen fram = new TuiConnectedLinesScreen(Math.Max(0, l.Xsize - 4), 15, new ILineElement[]{frame, hor, ver}, Placement.Center, 0, 0, Palette.user);
		fram.OnParentResize += (s, a) => {
			fram.Xsize = Math.Max(0, a.X - 4);
		};
		
		TuiScreen ins = new TuiScreen(Math.Max(0, l.Xsize - 6), 13, Placement.Center, 0, 0, null);
		ins.OnParentResize += (s, a) => {
			ins.Xsize = Math.Max(0, a.X - 6);
		};
		
		l.Elements.Add(fram);
		l.Elements.Add(ins);
		
		TuiScreen left = new TuiScreen(Math.Max(0, ins.Xsize/2), 11, Placement.TopLeft, 0, 2, null);
		left.OnParentResize += (s, a) => {
			left.Xsize = Math.Max(0, a.X/2);
		};
		
		TuiScreen right = new TuiScreen(Math.Max(0, ins.Xsize/2 - 1), 11, Placement.TopRight, 0, 2, null);
		right.OnParentResize += (s, a) => {
			right.Xsize = Math.Max(0, a.X/2 - 1);
		};
		
		ins.Elements.Add(left);
		ins.Elements.Add(right);
		ins.Elements.Add(new TuiMultipleLabels(new string[]{start.ToString(), " - ", end.ToString()}, Placement.TopLeft, 0, 0, new CharFormat?[]{Palette.info, null, Palette.info}));
		ins.Elements.Add(new TuiTwoLabels("", secondsToReadable(totalTime) + " (" + (totalTime / 60f).ToString("F0") + " mins)", Placement.TopRight, 0, 0, null, Palette.info));
		
		l.Elements.Add(new TuiLabel("FlashCard", Placement.TopCenter, 0, 1, Palette.main));
		
		TuiLabel lab = new TuiLabel("AshRadio", Placement.Center, (l.Xsize - 12)/2, -8, Palette.hint);
		lab.OnParentResize += (s, a) => {
			lab.OffsetX = (a.X - 12)/2;
		};
		
		l.Elements.Add(lab);
		
		left.Elements.Add(new TuiLabel("Top songs:", Placement.TopLeft, 0, 0));
		
		for(int i = 0; i < Math.Min(5, songs.Count); i++){
			Song s = songs[i];
			
			left.Elements.Add(new TuiLabel((i + 1) + ".", Placement.TopLeft, 0, i * 2 + 1));
			left.Elements.Add(new TuiLabel(s?.title ?? Song.nullTitle, Placement.TopLeft, 3, i * 2 + 1, Palette.song)); //Song title
			left.Elements.Add(new TuiLabel((s?.authors?.Length ?? 0) == 0 ? Author.nullName : string.Join(", ", s.authors.Select(n => (Author.get(n)?.name ?? Author.nullName))), Placement.TopLeft, 6, i * 2 + 2, Palette.author)); //Song author
		}
		
		right.Elements.Add(new TuiLabel("Top authors:", Placement.TopLeft, 0, 0));
		
		for(int i = 0; i < Math.Min(5, authors.Count); i++){
			Author a = authors[i];
			
			right.Elements.Add(new TuiLabel((i + 1) + ".", Placement.TopLeft, 0, i * 2 + 1));
			right.Elements.Add(new TuiLabel(a?.name ?? Author.nullName, Placement.TopLeft, 3, i * 2 + 1, Palette.author)); //Author name
			right.Elements.Add(new TuiLabel(aut[a.id].Item2.ToString("F2") + "%", Placement.TopRight, 0, i * 2 + 1, Palette.info)); //Percentage
			right.Elements.Add(new TuiLabel(secondsToHourTime(aut[a.id].Item1), Placement.TopLeft, 6, i * 2 + 2, Palette.info)); //Total time
		}
		
		setMiddleScreen(l3);
	}
}