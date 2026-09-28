using System;
using System.Runtime.InteropServices;

public class Test
{
    public unsafe static int Main()
    {
        // Two nested types share the simple name "Conflict" under different enclosing types.
        // The reverse-thunk key drops the enclosing-type chain (Method#argCount:Assembly::Type),
        // so both callbacks currently map to the same key and the build fails (#130739).
        ((delegate* unmanaged<void>)&Conflicting.OuterA.Conflict.C)();
        ((delegate* unmanaged<void>)&Conflicting.OuterB.Conflict.C)();
        return 42;
    }
}

namespace Conflicting
{
    public class OuterA
    {
        public class Conflict
        {
            [UnmanagedCallersOnly]
            public static void C()
            {
                Console.WriteLine("TestOutput -> Conflicting.OuterA.Conflict.C");
            }
        }
    }

    public class OuterB
    {
        public class Conflict
        {
            [UnmanagedCallersOnly]
            public static void C()
            {
                Console.WriteLine("TestOutput -> Conflicting.OuterB.Conflict.C");
            }
        }
    }
}
