using System;

public class Test {

    private static IntPtr i = IntPtr.Zero;

    public static IntPtr nati {
        get {
            if (i == IntPtr.Zero) {
                i = (IntPtr) 10001;
            }
            return i;
        }
    }

    public static int Main() {
        IntPtr[] nati = new IntPtr [1];
        nati [0] = Test.nati;
        Console.WriteLine ("nati [0] " + nati [0]);
        return 0;
    }
}
