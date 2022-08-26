// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using Xunit;

namespace GitHub_43569
{
    class Program
    {
        public static int Main()
        {
            if ((int)Vector64_Create_short(100) != 100)
                return 1;
            if ((int)Vector128_Create_float(100) != 100)
                return 2;
            if ((int)Vector128_Create_byte(100) != 100)
                return 3;
            if ((int)Vector256_Create_float(100) != 100)
                return 4;
            if ((int)Vector256_Create_double(100) != 100)
                return 5;
            return 100;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Inline<T>(T t) => t;

        [Theory]
        [InlineData(100, 100)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void Vector64_Create_short(short a, int expectedResult)
        {
            Vector64<short> x = default;
            for (int i = 0; i < 1; i++)
                x = Vector64.Create(1, 2, 3, Inline(a));
            Assert.Equal(expectedResult, (int)x.GetElement(3));
        }

        [Theory]
        [InlineData(100, 100)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void Vector128_Create_float(float a, int expectedResult)
        {
            Vector128<float> x = default;
            for (int i = 0; i < 1; i++)
                x = Vector128.Create(1, 2, 3, Inline(a));
            Assert.Equal(expectedResult, (int)x.GetElement(3));
        }

        [Theory]
        [InlineData(100, 100)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void Vector128_Create_byte(byte a, int expectedResult)
        {
            Vector128<byte> x = default;
            for (int i = 0; i < 1; i++)
                x = Vector128.Create(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, Inline(a));
            Assert.Equal(expectedResult, (int)x.GetElement(15));
        }

        [Theory]
        [InlineData(100, 100)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void Vector256_Create_float(float a, int expectedResult)
        {
            Vector256<float> x = default;
            for (int i = 0; i < 1; i++)
                x = Vector256.Create(1, 2, 3, 4, 5, 6, 7, Inline(a));
            Assert.Equal(expectedResult, (int)x.GetElement(7));
        }

        [Theory]
        [InlineData(100, 100)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void Vector256_Create_double(double a, int expectedResult)
        {
            Vector256<double> x = default;
            for (int i = 0; i < 1; i++)
                x = Vector256.Create(1, 2, 3, Inline(a));
            Assert.Equal(expectedResult, (int)x.GetElement(3));
        }
    }
}
