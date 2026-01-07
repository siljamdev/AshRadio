using System.Globalization;

public record struct MonthDate(short month, short year){
	public MonthDate(DateTime d) : this((short) d.Month, (short) d.Year){}
	
	public static MonthDate Now => new MonthDate(DateTime.Now);
	
	public DateTime ToDate(){
		return new DateTime((int) year, (int) month, 1);
	}
	
	public MonthDate NextMonth(){
		if(month > 11){
			return new MonthDate(1, (short) (year + 1));
		}else{
			return new MonthDate((short) (month + 1), year);
		}
	}
	
	public static MonthDate[] Range(MonthDate s, MonthDate e){
		if(s > e){
			return Array.Empty<MonthDate>();
		}
		
		List<MonthDate> l = new();
		
		while(s <= e){
			l.Add(s);
			s = s.NextMonth();
		}
		
		return l.ToArray();
	}
	
	public static bool TryParse(string s, out MonthDate m){
		if(DateTime.TryParseExact(s, "MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime st)){
			m = new MonthDate(st);
			return true;
		}else{
			m = default;
			return false;
		}
	}
	
	public string ToNumbers(){
		return month.ToString("D2") + "-" + year;
	}
	
	public string ToNumbers2(){
		return month.ToString("D2") + "/" + year;
	}
	
	public override string ToString(){
		return ((Month) month).ToString() + " " + year;
	}
	
	public static bool operator > (MonthDate a, MonthDate b){
		if(a.year == b.year){
			return a.month > b.month;
		}else{
			return a.year > b.year;
		}
	}
	
	public static bool operator < (MonthDate a, MonthDate b){
		if(a.year == b.year){
			return a.month < b.month;
		}else{
			return a.year < b.year;
		}
	}
	
	public static bool operator >= (MonthDate a, MonthDate b) => (a == b) || (a > b);
	
	public static bool operator <= (MonthDate a, MonthDate b) => (a == b) || (a < b);
}

public enum Month{
	Invalid = 0,
	January = 1,
	February = 2,
	March = 3,
	April = 4,
	May = 5,
	June = 6,
	July = 7,
	August = 8,
	September = 9,
	October = 10,
	November = 11,
	December = 12,
}