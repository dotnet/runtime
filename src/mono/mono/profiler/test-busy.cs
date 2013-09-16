using System;
using System.Threading;

class T {

	static int count = 10;
	static void test2 () {
		//Thread.Sleep (500);
		//return;
		int v = 0;
		for (int i = 0; i < count; ++i) {
			v += test3 ();
		}
	}

	static int test3 () {
		int v = 33;
		for (int i = 0; i < 10000000; ++i) {
			v += i * 1000;
			v /=  (1 + i) * 2;
		}
		return v > 0? 0: 1;
	}
	static int test () {
		int v = 33;
		for (int i = 0; i < 10000000; ++i) {
			v += i * 1000;
			v /=  (1 + i) * 2;
		}
		return v > 0? 0: 1;
	}
	static int Main (string[] args) {
		if (args.Length > 0)
			count = int.Parse (args [0]);
		Thread t = new Thread (test2);
		t.Name = "BusyHelper";
		t.Start ();
		int v = 0;
		for (int i = 0; i < count; ++i) {
			v += test ();
		}
		t.Join ();
		return v > 0? 0: 1;
	}
}

