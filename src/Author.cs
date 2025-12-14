public class Author{
	
	//INSTANCE
	public string name{get; private set;}
	
	public int id{get; private set;}
	
	public void setName(string n){
		name = n?.Trim() ?? "Unknown author";
		save();
	}
	
	public List<Song> getSongs(){
		List<int> lib = Song.getLibrary();
		
		List<Song> a = new();
		foreach(int s in lib){
			Song g = Song.load(s);
			if(g == null){
				continue;
			}
			if(g.authors.Contains(id)){
				a.Add(g);
			}
		}
		
		return a;
	}
	
	public List<int> getSongsIds(){
		List<int> lib = Song.getLibrary();
		
		List<int> a = new();
		foreach(int s in lib){
			Song g = Song.load(s);
			if(g == null){
				continue;
			}
			if(g.authors.Contains(id)){
				a.Add(s);
			}
		}
		
		return a;
	}
	
	void save(){
		authorsFile.Set(id.ToString(), name);
		authorsFile.Save();
		
		onAuthorsUpdate?.Invoke(null, EventArgs.Empty);
	}
	
	public override string ToString(){
		return name + " | Id: " + id.ToString();
	}
	
	//STATIC
	static AshFile authorsFile = null!;
	
	static int latestId;
	
	public static event EventHandler onAuthorsUpdate;
	
	public static void init(int li){
		latestId = Math.Max(li, -1);
		authorsFile = new AshFile(Radio.dep.path + "/authors.ash");
		saveAll();
	}
	
	public static bool exists(int id){
		if(id < 0){
			return false;
		}
		return authorsFile.TryGetValue(id.ToString(), out string n);
	}
	
	public static bool exists(string nam){
		foreach(var kvp in authorsFile){
			if(kvp.Value is string s && string.Equals(s, nam, StringComparison.OrdinalIgnoreCase) && int.TryParse(kvp.Key, out int i) && i > -1){
				return true;
			}
		}
		return false;
	}
	
	public static int getId(string nam){
		foreach(var kvp in authorsFile){
			if(kvp.Value is string s && string.Equals(s, nam, StringComparison.OrdinalIgnoreCase) && int.TryParse(kvp.Key, out int i) && i > -1){
				return i;
			}
		}
		return -1;
	}
	
	public static Author load(int id2){
		if(authorsFile.TryGetValue(id2.ToString(), out string n)){
			return new Author(){
				name = n,
				id = id2
			};
		}
		return null;
	}
	
	public static void delete(int id){
		if(!exists(id)){
			return;
		}
		
		authorsFile.Remove(id.ToString());
		authorsFile.Save();
		
		onAuthorsUpdate?.Invoke(null, EventArgs.Empty);
	}
	
	public static int[] getAuthors(string[] a){
		List<int> r = new List<int>(a.Length);
		
		List<Author> au = getAllAuthors();
		
		for(int i = 0; i < a.Length; i++){
			if(a[i].Trim() == ""){
				continue;
			}
			Author d = au.FirstOrDefault(t => string.Equals(t.name, a[i].Trim(), StringComparison.OrdinalIgnoreCase));
			if(d == null){ //Not found, create it
				latestId++;
				authorsFile.Set(latestId.ToString(), a[i].Trim());
				authorsFile.Save();
				
				saveAll();
				
				onAuthorsUpdate?.Invoke(null, EventArgs.Empty);
				
				r.Add(latestId);
			}else{
				r.Add(d.id);
			}
		}
		
		return r.ToArray();
	}
	
	public static List<int> getAllIds(){
		List<int> a = new(authorsFile.Count);
		
		foreach(var kvp in authorsFile){
			if(kvp.Value is string && int.TryParse(kvp.Key, out int i) && i > -1){
				a.Add(i);
			}
		}
		
		return a;
	}
	
	public static List<Author> getAllAuthors(){
		List<Author> a = new(authorsFile.Count);
		
		foreach(var kvp in authorsFile){
			if(kvp.Value is string && int.TryParse(kvp.Key, out int i) && i > -1){
				a.Add(load(i));
			}
		}
		
		return a;
	}
	
	static void saveAll(){
		Radio.config.Set("authors.latestId", latestId);
		Radio.config.Save();
	}
}