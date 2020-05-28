using System;
class T {
	static void Main () {
		int i = Environment.TickCount;
		new T ().X ();
		Console.WriteLine (Environment.TickCount - i);
	}
	
	int [] window = new int [9];
	
	void X () {
		int scan = 0, match = 0;
		for (int i = 0; i < 5000000; i ++) {
			if (window[++scan] == window[++match] && 
				window[++scan] == window[++match] && 
				window[++scan] == window[++match] && 
				window[++scan] == window[++match] && 
				window[++scan] == window[++match] && 
				window[++scan] == window[++match] && 
				window[++scan] == window[++match] && 
				window[++scan] == window[++match]) {
					scan = match = 0;
			}	
		}
	}
}