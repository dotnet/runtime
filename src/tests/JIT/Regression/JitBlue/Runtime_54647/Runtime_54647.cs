// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using Xunit;

namespace Runtime_54647
{
    struct Vector64x2
    {
        Vector64<int> _fld1;
        Vector64<int> _fld2;
    }

    public class Program
    {
        [Fact]
        public static int TestEntryPoint()
        {
            var val1 = new Vector64x2();
            var val2 = new Vector64x2();

            Copy(ref val1, val2);

            return 100;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static void Copy(ref Vector64x2 dst, Vector64x2 src)
        {
            dst = src;
        }
    }
}
