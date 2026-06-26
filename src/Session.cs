using System.Diagnostics;
using AshLib.Lists;

public static class Session{
	public static SessionMode mode{get; private set;}
	
	public static SourceType sourceType{get; private set;} //library, plyalist, author...
	public static int sourceIdentifier{get; private set;} //WHAT playlist, WHICH author...
	public static List<int> sourceSeen{get; private set;} //Already seen songs
	
	static List<int> pool;
	
	public static ReactiveList<int> queue;
	public static bool queueEmpties {get; set{
		field = value;
		queueIndex = 0;
		onQueueChange?.Invoke();
	}} = true;
	public static int queueIndex {get; private set;} = 0;
	
	static List<int> prevPlayed = new();
	
	static Random rand;
	
	public static event Action onModeChange; //UI changes
	public static event Action onSourceChange; //UI changes
	public static event Action onQueueChange; //UI changes
	
	public static bool addToPrevList = true; //Needed for going to the previous song without reading the current one
	
	public static void init(){
		mode = (SessionMode) Radio.session.GetValue<int>("session.mode");
		sourceType = (SourceType) Radio.session.GetValue<int>("session.sourceType");
		sourceIdentifier = Radio.session.GetValue<int>("session.sourceIdentifier");
		sourceSeen = Radio.session.GetValue<int[]>("session.sourceSeen")?.ToList() ?? new List<int>();
		
		queue = new ReactiveList<int>(() => onQueueChange?.Invoke());
		rand = new Random();
		
		Radio.py.onBeforeSongLoad += () => {
			if(addToPrevList){
				addPrevPlayed(Radio.py.playingSong);
			}
		};
		
		Radio.py.onSongLoad += () => {
			if(!sourceSeen.Contains(Radio.py.playingSong)){
				sourceSeen.Add(Radio.py.playingSong);
			}
			
			if(pool != null){
				pool.RemoveAll(n => n == Radio.py.playingSong);
			}
			
			Radio.session.Set("session.sourceSeen", sourceSeen.ToArray());
		};
		
		Radio.py.onSongFinish += () => {
			Radio.py.play(serveNext());
		};
		
		Song.onLibraryUpdate += () => {
			if(sourceType == SourceType.Library){
				update();
			}
		};
		
		Song.onSongDeleted += (s) => {
			if(queue.Contains(s)){
				queue.RemoveAll(id => !Song.exists(id));
			}
		};
		
		Author.onAuthorDetailsUpdate += (a) => {
			if(sourceType == SourceType.Author && sourceIdentifier == a){
				update();
			}
		};
		
		Playlist.onPlaylistDetailsUpdate += (p) => {
			if(sourceType == SourceType.Playlist && sourceIdentifier == p){
				update();
			}
		};
		
		update(); //saves session.ash
	}
	
	public static void addToQueue(int s){
		queue.Add(s);
	}
	
	public static void addMultipleToQueue(IEnumerable<int> ids){
		queue.AddRange(ids);
	}
	
	public static void removeFromQueue(int index){
		queue.RemoveAt(index);
		queueIndex = 0;
	}
	
	public static void moveInQueue(int index, int newIndex){
		queue.Move(index, newIndex);
	}
	
	public static void clearQueue(){
		queue.Clear();
	}
	
	public static int serveNext(){
		if(queue.Count > 0){
			int s = queue[queueIndex];
			if(queueEmpties){
				queue.RemoveAt(queueIndex);
			}else{
				queueIndex++;
				if(queueIndex >= queue.Count){
					queueIndex = 0;
				}
				onQueueChange?.Invoke();
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
		
		onSourceChange?.Invoke();
	}
	
	public static void setMode(SessionMode m){
		mode = m;
		
		Radio.session.Set("session.mode", (int) mode);
		Radio.session.Save();
		
		onModeChange?.Invoke();
	}
	
	//Update pool
	static void update(){
		
		//Avoid deleted playlists
		switch(sourceType){
			case SourceType.Author:
				if(!Author.exists(sourceIdentifier)){
					sourceType = SourceType.Library;
				}
				break;
			
			case SourceType.Playlist:
				if(!Playlist.exists(sourceIdentifier)){
					sourceType = SourceType.Library;
				}
				break;
		}
		
		switch(sourceType){
			default:
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
	Library, Author, Playlist
}