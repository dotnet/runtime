// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using Xunit;

namespace System.Runtime.InteropServices.JavaScript.Tests
{
    public class MemoryTests
    {
        [Theory]
        [InlineData(-1L)]
        [InlineData(-42L)]
        [InlineData(int.MinValue)]
        [InlineData(-9007199254740991L)]//MIN_SAFE_INTEGER
        [InlineData(1L)]
        [InlineData(0L)]
        [InlineData(42L)]
        [InlineData(int.MaxValue)]
        [InlineData(0xF_FFFF_FFFFL)]
        [InlineData(9007199254740991L)]//MAX_SAFE_INTEGER 
        public static unsafe void Int52TestOK(long value)
        {
            long expected = value;
            long actual2 = value;
            var bagFn = new Function("ptr", "ptr2", @"
                const value=globalThis.App.MONO.getI52(ptr);
                globalThis.App.MONO.setI52(ptr2, value);
                return value;");

            uint ptr = (uint)Unsafe.AsPointer(ref expected);
            uint ptr2 = (uint)Unsafe.AsPointer(ref actual2);

            object o = bagFn.Call(null, ptr, ptr2);
            if (value < int.MaxValue && value > int.MinValue)
            {
                Assert.IsType<int>(o);
                long actual = (int)o;
                Assert.Equal(expected, actual);
            }
            Assert.Equal(expected, actual2);
        }

        [Theory]
        [InlineData(uint.MinValue)]
        [InlineData(1L)]
        [InlineData(0L)]
        [InlineData(42L)]
        [InlineData(uint.MaxValue)]
        [InlineData(0xF_FFFF_FFFFL)]
        [InlineData(9007199254740991L)]//MAX_SAFE_INTEGER 
        public static unsafe void UInt52TestOK(ulong value)
        {
            ulong expected = value;
            ulong actual2 = value;
            var bagFn = new Function("ptr", "ptr2", @"
                const value=globalThis.App.MONO.getI52(ptr);
                globalThis.App.MONO.setU52(ptr2, value);
                return value;");

            uint ptr = (uint)Unsafe.AsPointer(ref expected);
            uint ptr2 = (uint)Unsafe.AsPointer(ref actual2);

            object o = bagFn.Call(null, ptr, ptr2);
            if (value < int.MaxValue)
            {
                Assert.IsType<int>(o);
                ulong actual = (ulong)(long)(int)o;
                Assert.Equal(expected, actual);
            }
            Assert.Equal(expected, actual2);
        }

        [Theory]
        [InlineData(double.NegativeInfinity)]
        [InlineData(double.PositiveInfinity)]
        [InlineData(double.MinValue)]
        [InlineData(double.MaxValue)]
        [InlineData(double.Pi)]
        [InlineData(9007199254740993.0)]//MAX_SAFE_INTEGER +2
        public static unsafe void Int52TestRange(double value)
        {
            long actual = 0;
            uint ptr = (uint)Unsafe.AsPointer(ref actual);
            var bagFn = new Function("ptr", "value", @"
                globalThis.App.MONO.setI52(ptr, value);");
            var ex=Assert.Throws<JSException>(() => bagFn.Call(null, ptr, value));
            Assert.Contains("Overflow: value out of Number.isSafeInteger range", ex.Message);

            double expectedD = value;
            uint ptrD = (uint)Unsafe.AsPointer(ref expectedD);
            var bagFnD = new Function("ptr", "value", @"
                globalThis.App.MONO.getI52(ptr);");
            var exD = Assert.Throws<JSException>(() => bagFn.Call(null, ptr, value));
            Assert.Contains("Overflow: value out of Number.isSafeInteger range", ex.Message);
        }

        [Theory]
        [InlineData(-1.0)]
        public static unsafe void UInt52TestRange(double value)
        {
            long actual = 0;
            uint ptr = (uint)Unsafe.AsPointer(ref actual);
            var bagFn = new Function("ptr", "value", @"
                globalThis.App.MONO.setU52(ptr, value);");
            var ex=Assert.Throws<JSException>(() => bagFn.Call(null, ptr, value));
            Assert.Contains("Can't convert negative Number into UInt64", ex.Message);

            double expectedD = value;
            uint ptrD = (uint)Unsafe.AsPointer(ref expectedD);
            var bagFnD = new Function("ptr", "value", @"
                globalThis.App.MONO.getU52(ptr);");
            var exD = Assert.Throws<JSException>(() => bagFn.Call(null, ptr, value));
            Assert.Contains("Can't convert negative Number into UInt64", ex.Message);
        }

        [Fact]
        public static unsafe void Int52TestNaN()
        {
            long actual = 0;
            uint ptr = (uint)Unsafe.AsPointer(ref actual);
            var bagFn = new Function("ptr", "value", @"
                globalThis.App.MONO.setI52(ptr, value);");
            var ex=Assert.Throws<JSException>(() => bagFn.Call(null, ptr, double.NaN));
            Assert.Contains("Can't convert Number.Nan into Int64", ex.Message);
        }
    }
}
