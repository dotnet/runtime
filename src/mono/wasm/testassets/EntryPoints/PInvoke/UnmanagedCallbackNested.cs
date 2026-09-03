using System;
using System.Runtime.InteropServices;

public class Test
{
    public unsafe static int Main()
    {
        // Cold ldftn of a nested [UnmanagedCallersOnly] callback. The reverse-thunk key
        // must use the metadata namespace (empty for nested types) to match the runtime,
        // otherwise the lookup misses and the interpreter asserts at method-compile time.
        ((delegate* unmanaged<void>)&Namespaced.Outer.Nested.C)();
        ((delegate* unmanaged<void>)&Namespaced.Outer.Nested.Deeper.D)();
        return 42;
    }
}

namespace Namespaced
{
    public class Outer
    {
        public class Nested
        {
            [UnmanagedCallersOnly]
            public static void C()
            {
                Console.WriteLine("TestOutput -> Namespaced.Outer.Nested.C");
            }

            public class Deeper
            {
                [UnmanagedCallersOnly]
                public static void D()
                {
                    Console.WriteLine("TestOutput -> Namespaced.Outer.Nested.Deeper.D");
                }
            }
        }
    }
}
