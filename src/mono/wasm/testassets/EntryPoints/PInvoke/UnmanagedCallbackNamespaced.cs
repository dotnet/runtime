using System;
using System.Runtime.InteropServices;

public class Test
{
    public unsafe static int Main()
    {
        ((delegate* unmanaged<void>)&A.Conflict.C)();
        ((delegate* unmanaged<void>)&B.Conflict.C)();
        ((delegate* unmanaged<void>)&A.Conflict.C\u733f)();
        ((delegate* unmanaged<void>)&B.Conflict.C\u733f)();
        return 42;
    }
}

namespace A {
    public class Conflict {
        [UnmanagedCallersOnly(EntryPoint = "A_Conflict_C")]
        public static void C() {
            Console.WriteLine("TestOutput -> A.Conflict.C");
        }

        [UnmanagedCallersOnly(EntryPoint = "A_Conflict_C\u733f")]
        public static void C\u733f() {
            Console.WriteLine("TestOutput -> A.Conflict.C_\U0001F412");
        }
    }
}

namespace B {
    public class Conflict {
        [UnmanagedCallersOnly(EntryPoint = "B_Conflict_C")]
        public static void C() {
            Console.WriteLine("TestOutput -> B.Conflict.C");
        }

        [UnmanagedCallersOnly(EntryPoint = "B_Conflict_C\u733f")]
        public static void C\u733f() {
            Console.WriteLine("TestOutput -> B.Conflict.C_\U0001F412");
        }
    }
}
