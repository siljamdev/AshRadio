using System.Diagnostics;

public static class Session{
	public static SessionMode mode{get; private set;}
	
	public static SourceType sourceType{get; private set;} //library, plyalist, author...
	public static int sourceIdentifier{get; private set;} //WHAT playlist, WHICH author...
	public static List<int> sourceSeen{get; private set;} //Already seen songs
	
	static List<int> pool;
	
	static List<int> queue;
	public static bool queueEmpties {get; set{
		queueIndex = 0;
		field = value;
	}} = true;
	static int queueIndex = 0;
	
	static List<int> prevPlayed = new();
	
	static Random rand;
	
	public static event EventHandler onSourceChange; //UI changes
	public static event EventHandler onQueueChange; //UI changes
	
	public static bool addToPrevList = true; //Needed for going to the previous song without reading the current one
	
	public static void init(SessionMode m = SessionMode.Order, SourceType s = SourceType.None, int sx = -1, int[] si = null){
		mode = m;
		sourceType = s;
		sourceIdentifier = sx;
		sourceSeen = si?.ToList() ?? new List<int>();
		
		queue = new List<int>();
		rand = new Random();
		
		Radio.py.onBeforeSongLoad += (s, a) => {
			if(addToPrevList){
				addPrevPlayed(Radio.py.playingSong);
			}
		};
		
		Radio.py.onSongLoad += (s, a) => {
			if(!sourceSeen.Contains(Radio.py.playingSong)){
				sourceSeen.Add(Radio.py.playingSong);
			}
			
			if(pool != null){
				pool.RemoveAll(n => n == Radio.py.playingSong);
			}
			
			Radio.session.Set("session.sourceSeen", sourceSeen.ToArray());
		};
		
		Radio.py.onSongFinish += (s, a) => {
			Radio.py.play(serveNext());
		};
		
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
		return queue.ToList();
	}
	
	public static void addToQueue(int s){
		queue.Add(s);
		onQueueChange?.Invoke(null, EventArgs.Empty);
	}
	
	public static void removeFromQueue(int index){
		queue.RemoveAt(index);
		queueIndex = 0;
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
			int s = queue[queueIndex];
			if(queueEmpties){
				queue.RemoveAt(queueIndex);
				onQueueChange?.Invoke(null, EventArgs.Empty);
			}else{
				queueIndex++;
				if(queueIndex >= queue.Count){
					queueIndex = 0;
				}
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
				break;
		}
		
		return choice;
	}
	
	public static void addPrevPlayed(int s){
		if(s < 0){
			return;
		}
		
		if(prevPlayed.Count > 0 && prevPlayed[prevPlayed.Count - 1] == s){
			return;
		}
		
		prevPlayed.Add(s);
	}
	
	public static int getPrevious(int c){
		if(prevPlayed.Count < 1){
			return -1;
		}
		
		int s = prevPlayed[prevPlayed.Count - 1];
		prevPlayed.RemoveAt(prevPlayed.Count - 1);
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
			Radio.py.play(Session.serveNext());
		}
		
		onSourceChange?.Invoke(null, EventArgs.Empty);
	}
	
	public static void setMode(SessionMode m){
		mode = m;
		
		Radio.session.Set("session.mode", (int) mode);
		Radio.session.Save();
	}
	
	//Update pool
	static void update(){
		switch(sourceType){
			default:
			case SourceType.None:
				sourceSeen = new List<int>();
				pool = new List<int>();
				break;
				
			case SourceType.Library:
				pool = Song.getLibrary().Select(h => h.id).ToList();
				break;
				
			case SourceType.Author:
				Author a = Author.get(sourceIdentifier);
				if(a == null){
					pool = new List<int>();
					sourceSeen = new List<int>();
					break;
				}
				pool = a.getSongsIds();
				break;
				
			case SourceType.Playlist:
				Playlist l = Playlist.get(sourceIdentifier);
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
		
		Radio.session.Set("session.sourceType", (int) sourceType);
		Radio.session.Set("session.sourceIdentifier", sourceIdentifier);
		Radio.session.Set("session.sourceSeen", sourceSeen.ToArray());
		Radio.session.Save();
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