using System;
using System.Runtime.InteropServices;

public class Z {
    ~Z() {
        Console.WriteLine("Hello, world!");
    }
}

[StructLayout(LayoutKind.Explicit)]
public struct X {
    [FieldOffset(0)] public short a;
    [FieldOffset(2)] public Z z; // Unaligned reference
}

class Y {
    static X test() {
        X x = new X();
        x.z = new Z();
        return x;
    }

    static void test2(X x) {
        Console.WriteLine("Object: " + x);
    }

	static void Inner () {
        X t1 = test();
    	System.GC.Collect();
        System.GC.Collect();
    	System.GC.WaitForPendingFinalizers();
        test2(t1);
	}

    static int Main() {
    	try {
			Inner ();
	} catch (TypeLoadException e) {
		Console.WriteLine ("got correct exception: {0}", e);
		return 0;
	}
	return 1;
    }
}
