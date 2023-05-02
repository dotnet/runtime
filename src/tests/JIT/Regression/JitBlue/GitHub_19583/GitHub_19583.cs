// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Threading;
using Xunit;

// Check for proper stack spilling in the presence of assignment-like nodes.

public class Program
{
    [Fact]
    public static int TestEntryPoint()
    {
        return 100 +
            (Test1.Run() == 0 ? 0 : 1) +
            (Test2.Run() == 1 ? 0 : 2) +
            (Test3.Run() == 0 ? 0 : 4) +
            (Test4.Run() == 0 ? 0 : 8);
    }

    class Test1
    {
        static long s_1;
        static int s_3;
        public static int Run()
        {
            int vr16 = s_3;
            int vr19 = Interlocked.Exchange(ref s_3, 1);
            return vr16;
        }
    }

    class Test2
    {
        static int s_32;
        static int s_46 = 1;
        public static int Run()
        {
            s_32 = 0;
            M5();
            return s_46;
        }

        static void M5()
        {
            s_46 *= (Interlocked.Exchange(ref s_46, 0) | s_32--);
        }
    }

    class Test3
    {
        static int s_3;
        static int s_11;
        public static int Run()
        {
            return M9(s_3, Interlocked.Exchange(ref s_3, 1), s_11++);
        }

        static int M9(int arg2, int arg3, int arg4)
        {
            return arg2;
        }
    }

    class Test4
    {
        struct vec
        {
            public int x, y, z, w;
        }

        public static unsafe int Run()
        {
            if (!Sse2.IsSupported) return 0;

            vec v = new vec();
            Vector128<int> o = Vector128.Create(1);
            int vr16 = v.y;
            Sse2.Store(&v.x, o);
            return vr16;
        }
    }
}
