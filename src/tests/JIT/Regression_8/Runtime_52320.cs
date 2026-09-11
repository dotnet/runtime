// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Diagnostics;
using Xunit;

namespace Runtime_52320
{
    public class Program
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static bool UseAsgOpt(int a)
        {
            Unsafe.InitBlock(ref Unsafe.As<int, byte>(ref a), 0, 2);
            return a == 1 << 20;
        }
        [Fact]
        public static int TestEntryPoint()
        {
            bool res = UseAsgOpt(1 << 20);
            Debug.Assert(res);
            if (!res)
            {
                return 101;
            }
            return 100;
        }
    }
}
