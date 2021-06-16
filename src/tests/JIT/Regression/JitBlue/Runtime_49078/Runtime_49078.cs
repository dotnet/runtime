// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

// Regression test for GitHub issue 49078: https://github.com/dotnet/runtime/issues/49078
//
// The problem was when a VSD interface call returning a multi-byte struct in registers was initially considered
// to be a tailcall, but the tailcall was abandoned in morph due to not enough stack space to store outgoing
// arguments, in which case we create a new call return local to store the return struct, and re-morph the
// call. In doing so, we forget that we had already added VSD non-standard args, and re-add them, leaving
// the originally added arg as a "normal" arg that shouldn't be there.
//
// So, in summary, for a call A->B, to see this failure, we need:
// 
// 1. The call is considered a potential tailcall (by the importer)
// 2. The call requires non-standard arguments that add call argument IR in fgInitArgInfo()
//    (e.g., VSD call -- in this case, a generic interface call)
// 3. We reject the tailcall in fgMorphPotentialTailCall() (e.g., not enough incoming arg stack space in A
//    to store B's outgoing args), in this case because the first arg is a large struct. We can't reject
//    it earlier, due to things like address exposed locals -- we must get far enough through the checks
//    to have called fgInitArgInfo() to add the extra non-standard arg.
// 4. B returns a struct in multiple registers (e.g., a 16-byte struct in Linux x64 ABI)

namespace GitHub_49078
{
    public struct S16
    {
        public IntPtr a;
        public uint b;
    }

    public struct BigStruct
    {
        public IntPtr a, b, c, d, e, f, g, h, j, k, l, m;

        public BigStruct(IntPtr a1)
        {
            a = b = c = d = e = f = g = h = j = k = l = m = a1;
        }
    }

    public interface IFoo<T>
    {
        public S16 Foo(BigStruct b, int i, int j);
    }

    public class CFoo<T> : IFoo<T>
    {
        public S16 Foo(BigStruct b, int i, int j)
        {
            S16 s16;
            s16.a = (IntPtr)i;
            s16.b = (uint)j;
            return s16;
        }
    }

    class Test
    {
        IFoo<int> m_if = new CFoo<int>();
        BigStruct m_bs = new BigStruct((IntPtr)1);

        public S16 Caller(int a)
        {
            // Add some computation so this is not inlineable (but don't mark it noinline,
            // which would prevent the tailcall consideration).
            int i = 7;
            try
            {
                for (int j = 0; j < a; j++)
                {
                    i += j;
                }
            }
            finally
            {
                i += 2;
            }

            return m_if.Foo(m_bs, i, a);
        }
    }

    class Program
    {
        static int Main(string[] args)
        {
            Test t = new Test();
            S16 s = t.Caller(4);
            long l = (long)s.a + s.b;
            if (l == 19)
            {
                Console.WriteLine("Passed");
                return 100;
            }
            else
            {
                Console.WriteLine($"{s.a}, {s.b}, {l}");
                Console.WriteLine("Failed");
                return 101;
            }
        }
    }
}
