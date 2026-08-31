using System;
using System.Threading;

public class IntClass {
	public int i;

	public IntClass (int val) { i = val; }

	public int get () { return i; }
}

public class Gen<T> {
	[ThreadStaticAttribute]
	static T field;

	public static void setField (T t) { field = t; }
	public static T getField () { return field; }
}

public class main {
	static int i1;
	static int i2;

	public static void otherThread () {
		Gen<IntClass>.setField (new IntClass (2));
		i2 = Gen<IntClass>.getField ().get ();
	}

	public static int Main () {
		Gen<IntClass>.setField (new IntClass (1));

		Thread thread = new Thread (main.otherThread);
		thread.Start ();
		thread.Join ();

		i1 = Gen<IntClass>.getField ().get ();

		if (i1 != 1 || i2 != 2)
			return 1;
		return 0;
	}
}
