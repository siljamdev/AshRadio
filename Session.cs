public static class Session{
	public static SessionMode mode{get; private set;}
	
	public static SourceType sourceType{get; private set;} //library, plyalist, author...
	public static int sourceIdentifier{get; private set;} //WHAT playlist, WHICH author...
	public static List<int> sourceSeen{get; private set;} //Already seen songs
	
	static List<int> pool;
	
	static List<int> queue;
	public static bool queueEmpties = true;
	
	static List<int> prevPlayed = new();
	
	static Random rand;
	
	public static event EventHandler onSourceChange; //UI changes
	public static event EventHandler onQueueChange; //UI changes
	
	public static void init(SessionMode m = SessionMode.Order, SourceType s = SourceType.None, int sx = -1, int[] si = null){
		mode = m;
		sourceType = s;
		sourceIdentifier = sx;
		sourceSeen = si?.ToList() ?? new List<int>();
		
		queue = new List<int>();
		rand = new Random();
		
		Song.onLibraryUpdate += (s, a) => {
			if(sourceType == SourceType.Library || (sourceType == SourceType.Author && a.auth.Contains(sourceIdentifier))){
				update();
			}
		};
		
		Playlist.onPlaylistUpdate += (s, a) => {
			if(sourceType == SourceType.Playlist && sourceIdentifier == a.id){
				update();
			}
		};
		
		update();
	}
	
	public static List<int> getQueue(){
		return queue;
	}
	
	public static void addToQueue(int s){
		queue.Add(s);
		onQueueChange?.Invoke(null, EventArgs.Empty);
	}
	
	public static void removeFromQueue(int index){
		queue.RemoveAt(index);
		onQueueChange?.Invoke(null, EventArgs.Empty);
	}
	
	public static void moveInQueue(int index, int newIndex){
		int t = queue[index];
		queue.RemoveAt(index);
		queue.Insert(newIndex, t);
		onQueueChange?.Invoke(null, EventArgs.Empty);
	}
	
	public static int serveNext(){
		if(queue.Count > 0){
			int s = queue[0];
			if(queueEmpties){
				queue.RemoveAt(0);
				onQueueChange?.Invoke(null, EventArgs.Empty);
			}
			return s;
		}
		
		int choice;
		
		switch(mode){
			default:
				choice = -1;
				break;
			case SessionMode.Order:
				if(sourceSeen == null){
					sourceSeen = new List<int>();
				}
				if(pool.Count <= 0){
					sourceSeen = new List<int>();
					update();
				}
				if(pool.Count <= 0){
					choice = -1;
					break;
				}
				choice = pool[0];
				pool.RemoveAt(0);
				sourceSeen.Add(choice);
				break;
			case SessionMode.Shuffle:
				if(pool.Count <= 0){
					choice = -1;
					break;
				}
				choice = pool[rand.Next(pool.Count)];
				break;
			case SessionMode.SmartShuffle:
				if(sourceSeen == null){
					sourceSeen = new List<int>();
				}
				if(pool.Count <= 0){
					sourceSeen = new List<int>();
					update();
				}
				if(pool.Count <= 0){
					choice = -1;
					break;
				}
				int c = rand.Next(pool.Count);
				choice = pool[c];
				pool.RemoveAt(c);
				sourceSeen.Add(choice);
				break;
		}

		save();
		
		return choice;
	}
	
	public static void addPrevPlayed(int s){
		if(s < 0){
			return;
		}
		
		prevPlayed.Insert(0, s);
	}
	
	public static int getPrevious(int c){
		if(prevPlayed.Count < 1){
			return -1;
		}
		
		int s = prevPlayed[0];
		prevPlayed.RemoveAt(0);
		if(c > -1){
			pool.Insert(0, c);
			sourceSeen.RemoveAll(n => n == c);
		}
		return s;
	}
	
	public static void setSource(SourceType s, int id = 0){
		if(s == sourceType && id == sourceIdentifier){
			return;
		}
		
		sourceSeen = new List<int>();
		prevPlayed = new List<int>();
		sourceType = s;
		sourceIdentifier = id;
		update();
		
		if(Radio.py.playingSong < 0){
			Radio.py.askForSong();
		}
		
		onSourceChange?.Invoke(null, EventArgs.Empty);
	}
	
	public static void setMode(SessionMode m){
		mode = m;
		
		save();
	}
	
	static void update(){
		switch(sourceType){
			default:
			case SourceType.None:
				sourceSeen = new List<int>();
				pool = new List<int>();
				break;
			case SourceType.Library:
				pool = Song.getLibrary();
				break;
			case SourceType.Author:
				Author a = Author.load(sourceIdentifier);
				if(a == null){
					pool = new List<int>();
					sourceSeen = new List<int>();
					break;
				}
				pool = a.getSongsIds();
				break;
			case SourceType.Playlist:
				Playlist l = Playlist.load(sourceIdentifier);
				if(l == null){
					pool = new List<int>();
					sourceSeen = new List<int>();
					break;
				}
				pool = l.getSongsIds();
				break;
		}
		
		if(sourceSeen == null){
			sourceSeen = new List<int>();
		}
		
		foreach(int s in sourceSeen){
			pool.RemoveAll(n => n == s);
		}
		
		save();
	}
	
	static void save(){
		Radio.config.SetCamp("session.mode", (int) mode);
		Radio.config.SetCamp("session.sourceType", (int) sourceType);
		Radio.config.SetCamp("session.sourceIdentifier", sourceIdentifier);
		Radio.config.SetCamp("session.sourceSeen", sourceSeen.ToArray());
		
		Radio.config.Save();
	}
	
	public static string name(this SourceType s){
		switch(s){
			default:
			case SourceType.None:
				return "None";
			case SourceType.Library:
				return "Library";
			case SourceType.Author:
				return "Author";
			case SourceType.Playlist:
				return "Playlist";
		}
	}
	
	public static string name(this SessionMode s){
		switch(s){
			default:
				return "";
			case SessionMode.Order:
				return "Order";
			case SessionMode.Shuffle:
				return "Shuffle";
			case SessionMode.SmartShuffle:
				return "Smart Shuffle";
		}
	}
}

public enum SessionMode{
	Order, Shuffle, SmartShuffle
}

public enum SourceType{
	None, Library, Author, Playlist
}