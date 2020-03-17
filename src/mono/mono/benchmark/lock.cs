using System;
class T {
	static void Main () {
		int i = Environment.TickCount;
		new T ().X ();
		Console.WriteLine (Environment.TickCount - i);
	}
	
	void X () {
		for (int i = 0; i < 10000000; i ++)
			lock (this) {}
	}
}