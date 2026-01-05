public class Author{
	
	//INSTANCE
	public string name{get; private set;}
	
	public int id{get; private set;}
	
	public void setName(string n){
		name = n?.Trim() ?? nullName;
		save();
	}
	
	public List<Song> getSongs(){
		List<Song> lib = Song.getLibrary();
		
		return lib.Where(h => h.authors.Contains(id)).ToList();
	}
	
	public List<int> getSongsIds(){
		return getSongs().Select(h => h.id).ToList();
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
	
	public const string nullName = "Unknown author";
	
	static AshFile authorsFile = null!;
	
	static int latestId;
	
	static List<Author> authors;
	
	public static event EventHandler onAuthorsUpdate;
	
	public static void init(int li){
		latestId = Math.Max(li, -1);
		authorsFile = new AshFile(Radio.dep.path + "/authors.ash");
		saveAll();
		
		loadAuthors();
	}
	
	static void loadAuthors(){
		authors = new List<Author>(authorsFile.Count);
		
		for(int i = 0; i <= latestId; i++){
			authors.Add(load(i));
		}
	}
	
	static Author load(int id2){
		if(authorsFile.TryGetValue(id2.ToString(), out string n)){
			return new Author(){
				name = n,
				id = id2
			};
		}
		return null;
	}
	
	public static bool exists(int id){
		if(id < 0 || id >= authors.Count){
			return false;
		}
		return authors[id] != null;
	}
	
	public static bool exists(string nam){
		return authors.Any(h => string.Equals(h.name, nam, StringComparison.OrdinalIgnoreCase));
	}
	
	public static int getId(string nam){
		Author a = authors.FirstOrDefault(h => string.Equals(h.name, nam, StringComparison.OrdinalIgnoreCase));
		return a == null ? -1 : a.id;
	}
	
	public static Author get(int id){
		if(exists(id)){
			return authors[id];
		}else{
			return null;
		}
	}
	
	public static Author get(string nam){
		return authors.FirstOrDefault(h => string.Equals(h.name, nam, StringComparison.OrdinalIgnoreCase));
	}
	
	public static void delete(int id){
		if(!exists(id)){
			return;
		}
		
		authorsFile.Remove(id.ToString());
		authorsFile.Save();
		
		authors[id] = null;
		
		onAuthorsUpdate?.Invoke(null, EventArgs.Empty);
	}
	
	//List of names to list of authors
	public static int[] getAuthors(string[] a){
		List<int> r = new List<int>(a.Length);
		
		List<Author> au = getAllAuthors();
		
		for(int i = 0; i < a.Length; i++){
			string name = a[i].Trim();
			if(string.IsNullOrEmpty(name)){
				continue;
			}
			
			Author d = get(name);
			if(d == null){ //Not found, create it
				latestId++;
				authorsFile.Set(latestId.ToString(), name);
				authorsFile.Save();
				
				authors.Add(new Author(){
					name = name,
					id = latestId
				});
				
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
		return getAllAuthors().Select(h => h.id).ToList();
	}
	
	public static List<Author> getAllAuthors(){
		return authors.Where(h => h != null).ToList();
	}
	
	static void saveAll(){
		Radio.data.Set("authors.latestId", latestId);
		Radio.data.Save();
	}
}