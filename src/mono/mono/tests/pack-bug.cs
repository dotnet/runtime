using System;
using System.Collections;
using System.Runtime.InteropServices;
// bug #77788

[StructLayout(LayoutKind.Sequential , Pack = 1)]
struct Bogus {
	public byte b;
	public string str;

	public Bogus (int a) {
		b = (byte)a;
		str = "hello-" + a.ToString ();
	}
}

class T {

	static void test (Bogus[] arr) {
		for (int i = 0; i < 256; ++i) {
			if (arr [i].b != (byte)i)
				throw new Exception ("wrong b at " + i);
			if (arr [i].str != "hello-" + i.ToString ())
				throw new Exception ("wrong str at " + i);
		}
	}
	static void Main () {
		Bogus[] arr = new Bogus [256];
		int i;
		for (i = 0; i < 256; ++i) {
			arr [i] = new Bogus (i);
		}
		test (arr);
		for (i = 0; i < 10000; ++i) {
			ArrayList l = new ArrayList ();
			l.Add (i);
		}
		
		GC.Collect ();
		test (arr);
	}
}

