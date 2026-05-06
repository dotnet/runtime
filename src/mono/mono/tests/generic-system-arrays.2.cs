public class Test {
	public static object genArr<T> () {
		return new T[3,3];
	}

	public static int Main () {
		if (genArr<int> ().GetType () != typeof (int [,]))
			return 1;
		if (genArr<object> ().GetType () != typeof (object [,]))
			return 1;
		if (genArr<string> ().GetType () != typeof (string [,]))
			return 1;
		if (genArr<Test> ().GetType () != typeof (Test [,]))
			return 1;
		return 0;
	}
}
