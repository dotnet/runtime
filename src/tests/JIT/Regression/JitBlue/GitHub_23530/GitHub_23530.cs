// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using Xunit;

namespace GitHub_23530
{
    public class Program
    {
        struct vec
        {
            public float f1;
            public float f2;
            public float f3;
            public float f4;
        }

        static unsafe float fmaTest()
        {
            vec a;
            var b = Vector128.Create(1f);
            var c = Vector128.Create(2f);
            var d = Vector128.Create(3f);

            c = Fma.MultiplyAdd(Sse.LoadVector128((float*)&a), b, c);

            return Sse.Add(c, d).ToScalar();
        }

        [Fact]
        public static int TestEntryPoint()
        {
            if (Fma.IsSupported)
            {
                float result = fmaTest();
                if (Math.Abs(result - 5.0F) > System.Single.Epsilon)
                {
                    return -1;
                }
            }
            return 100;
        }
    }
}
