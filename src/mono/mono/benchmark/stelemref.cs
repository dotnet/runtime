using System;
class T {
	static void Main () {
		int i = Environment.TickCount;
		new T ().X ();
		Console.WriteLine (Environment.TickCount - i);
	}
	
	void X () {
		object [] x = new object [1];
		object o = new object ();
		for (int i = 0; i < 10000000; i ++)
			x [0] = o;
	}
}