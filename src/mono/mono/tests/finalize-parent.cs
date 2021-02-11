using System;

class P {

	static public int count = 0;
	~P () {
		count++;
	}
}

class T : P {

	static int Main () {
		for (int i = 0; i < 100; ++i) {
			T t = new T ();
		}
		GC.Collect ();
		GC.WaitForPendingFinalizers ();
		Console.WriteLine (P.count);
		if (P.count > 0)
			return 0;
		return 1;
	}
}
