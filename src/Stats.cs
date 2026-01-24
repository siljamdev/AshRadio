public static class Stats{
	static MonthDate currentDate;
	static AshFile currentFile;
	
	public static void init(){
		currentDate = MonthDate.Now;
		currentFile = new AshFile(getPath(currentDate));
		
		Radio.py.onSongLoad += (s, a) => {
			tryUpdateFile();
			
			if(Radio.py.playingSong < 0){
				return;
			}
			
			if(currentFile.TryGetValue(Radio.py.playingSong + ".n", out uint n)){
				currentFile.Set(Radio.py.playingSong + ".n", n + 1);
			}else{
				currentFile.Set(Radio.py.playingSong + ".n", (uint) 1);
			}
			
			currentFile.Save();
		};
		
		Radio.py.onBeforeSongLoad += (s, a) => setTime();
	}
	
	public static void setTime(){
		tryUpdateFile();
		
		if(Radio.py.playingSong < 0){
			return;
		}
		
		if(currentFile.TryGetValue(Radio.py.playingSong + ".t", out float t)){
			currentFile.Set(Radio.py.playingSong + ".t", t + (float) Radio.py.timer.Elapsed.TotalSeconds);
		}else{
			currentFile.Set(Radio.py.playingSong + ".t", (float) Radio.py.timer.Elapsed.TotalSeconds);
		}
		
		if(currentFile.TryGetValue("totalTime", out float tt)){
			currentFile.Set("totalTime", tt + (float) Radio.py.timer.Elapsed.TotalSeconds);
		}else{
			currentFile.Set("totalTime", (float) Radio.py.timer.Elapsed.TotalSeconds);
		}
		
		currentFile.Save();
	}
	
	//totalTime, Song id, number of times, time
	public static (float, Dictionary<int, (uint, float)>) getStats(MonthDate d){
		if(!File.Exists(getPath(d))){
			return (0f, new Dictionary<int, (uint, float)>());
		}
		
		AshFile stats = new AshFile(getPath(d));
		
		Dictionary<int, (uint, float)> ind = new();
		
		HashSet<int> existing = new();
		
		foreach(string k in stats.Keys){
			string[] a = k.Split(".");
			if(a.Length == 2 && (a[1] == "n" || a[1] == "t") && int.TryParse(a[0], out int id) && id > -1){
				existing.Add(id);
			}
		}
		
		foreach(int id in existing){
			ind[id] = (stats.GetOrDefault(id + ".n", (uint) 0), stats.GetOrDefault(id + ".t", 0f));
		}
		
		return (stats.GetOrDefault("totalTime", 0f), ind);
	}
	
	public static (float, Dictionary<int, (uint, float)>) getStats(MonthDate s, MonthDate e){
		return merge(MonthDate.Range(s, e).Select(h => getStats(h)).ToArray());
	}
	
	static (float, Dictionary<int, (uint, float)>) merge(params (float, Dictionary<int, (uint, float)>)[] s){
		Dictionary<int, (uint, float)> tot = new();
		
		foreach(Dictionary<int, (uint, float)> ind in s.Select(h => h.Item2)){
			foreach(KeyValuePair<int, (uint, float)> kvp in ind){
				int id = kvp.Key;
				(uint n, float t) = kvp.Value;
				if(tot.ContainsKey(id)){
					(uint n2, float t2) = tot[id];
					tot[id] = (n + n2, t + t2);
				}else{
					tot[id] = (n, t);
				}
			}
		}
		
		return (s.Select(h => h.Item1).Sum(), tot);
	}
	
	//id, number of times loaded, total time, duration, real number of times
	public static Dictionary<int, (uint, float, float, uint)> getRealStats(Dictionary<int, (uint, float)> s){
		Dictionary<int, (uint, float, float, uint)> ind = new();
		
		int[] ids = s.Keys.ToArray();
		float[] drs = Song.getDurationsAsync(ids).Result;
		//float[] drs = Song.getDurations(ids);
		Dictionary<int, float> durations = ids.Zip(drs, (id, dur) => (id, dur)).ToDictionary(x => x.Item1, x => x.Item2);
		
		foreach(KeyValuePair<int, (uint, float)> kvp in s){
			int id = kvp.Key;
			float dur = durations[id];
			//float dur = Song.getDuration(id);
			
			if(dur > 0f){
				ind[id] = (kvp.Value.Item1, kvp.Value.Item2, dur, (uint) (Math.Round(kvp.Value.Item2 / dur)));
			}else{
				ind[id] = (kvp.Value.Item1, kvp.Value.Item2, 0f, 0);
			}
		}
		
		return ind;
	}
	
	//id, total listen time, percentage of total time, total number of songs listened, most listened song
	public static Dictionary<int, (float, float, uint, int)> getAuthorStats(float totalTime, Dictionary<int, (uint, float, float, uint)> stats){
		//Author id, total listened time, total number of songs listened, most listened song id, most listened song listen time
		Dictionary<int, (float, uint, int, float)> aut = new();
		
		foreach(KeyValuePair<int, (uint, float, float, uint)> kvp in stats){
			int id = kvp.Key;
			
			Song s = Song.get(id);
			
			if(s?.authors == null){
				continue;
			}
			
			float sTime = kvp.Value.Item2;
			uint sNum = kvp.Value.Item4;
			
			foreach(int aid in s.authors){
				if(aut.ContainsKey(aid)){
					(float lt, uint num, int sid, float slt) = aut[aid];
					
					aut[aid] = (lt + sTime, num + sNum, slt > sTime ? sid : id, Math.Max(slt, sTime));
				}else{
					aut[aid] = (sTime, sNum, id, sTime);
				}
			}
		}
		
		return aut.ToDictionary(kvp => kvp.Key, kvp => {
			float percentage = 0f;
			if(totalTime != 0f){
				percentage = kvp.Value.Item1 / totalTime * 100f;
			}
			return (kvp.Value.Item1, percentage, kvp.Value.Item2, kvp.Value.Item3);
		});
	}
	
	static void tryUpdateFile(){
		if(MonthDate.Now != currentDate){
			currentDate = MonthDate.Now;
			currentFile = new AshFile(getPath(currentDate));
		}
	}
	
	static string getPath(MonthDate d){
		return Radio.dep.path + "/stats/" + d.ToNumbers() + ".ash";
	}
}