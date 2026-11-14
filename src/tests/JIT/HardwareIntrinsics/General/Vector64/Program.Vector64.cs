// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using Xunit;

namespace JIT.HardwareIntrinsics.General._Vector64
{
    public static partial class Program
    {
        static Program()
        {

        }

        [Theory]
        [InlineData(1, 2, 3, 4)]
        [InlineData(-7, 13, -1, int.MinValue)]
        public static void ToVector128ClearsUpperAfterCall(int lower0, int lower1, int upper0, int upper1)
        {
            Vector128<int> actual = ToVector128AfterCall(Vector128.Create(lower0, lower1, upper0, upper1));
            Assert.Equal(Vector128.Create(lower0 + lower0, lower1 + lower1, 0, 0), actual);
        }

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
        private static Vector128<int> ToVector128AfterCall(Vector128<int> value)
        {
            Vector64<int> lower = GetLowerForToVector128(value);
            Vector128<int> result = IdentityForToVector128(lower.ToVector128());
            return result + IdentityForToVector128(lower.ToVector128());
        }

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
        private static Vector64<int> GetLowerForToVector128(Vector128<int> value) => value.GetLower();

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
        private static Vector128<int> IdentityForToVector128(Vector128<int> value) => value;
    }
}
