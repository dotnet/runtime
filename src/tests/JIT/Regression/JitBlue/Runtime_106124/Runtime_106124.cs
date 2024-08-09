// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;
using Xunit;

namespace JIT.HardwareIntrinsics.Arm._Sve
{
    public static partial class Program
    {
        [Fact]
        public static void TestEntryPoint()
        {
            var vr19 = Vector128.CreateScalar(0L).AsVector();
            var vr25 = Sve.CreateBreakPropagateMask(vr19, vr19);
            Consume(vr25);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void Consume(Vector<long> v)
        {
            ;
        }

    }
}