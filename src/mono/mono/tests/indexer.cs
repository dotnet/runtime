namespace Test {
public class Patient {
	int id;
	double age;
	bool dead;
	public object this[string name] {
		get {
			switch (name) {
			case "id": return id;
			case "age": return age;
			case "dead": return dead;
			default: return null;
			}
		}
		set {
			switch (name) {
			case "id":    id = (int)value;    break;
			case "age": age = (double)value; break;
			case "dead": dead = (bool)value; break;
			}
		}
	}
	public static int Main() {
		Patient bob = new Patient();
		bob["age"] = 32.0;
		if ((bool)bob["dead"])
			return 1;
		return 0;
	}
}
}

