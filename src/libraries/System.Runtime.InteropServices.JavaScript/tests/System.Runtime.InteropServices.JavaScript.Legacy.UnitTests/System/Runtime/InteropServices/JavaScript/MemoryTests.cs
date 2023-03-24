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
        [InlineData(-9007199254740990L)]//MIN_SAFE_INTEGER+1
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
            long dummy = 0xA6A6A6A6L;
            long actual2 = dummy;
            var bagFn = new Function("ptr", "ptr2", @"
                const value=globalThis.App.runtime.getHeapI52(ptr);
                globalThis.App.runtime.setHeapI52(ptr2, value);
                return ''+value;");

            uint ptr = (uint)Unsafe.AsPointer(ref expected);
            uint ptr2 = (uint)Unsafe.AsPointer(ref actual2);

            object actual = (string)bagFn.Call(null, ptr, ptr2);
            Assert.Equal(""+ value, actual);
            Assert.Equal(value, actual2);
            Assert.Equal(0xA6A6A6A6L, dummy);
        }

        [Theory]
        [InlineData(uint.MinValue)]
        [InlineData(1UL)]
        [InlineData(0UL)]
        [InlineData(42UL)]
        [InlineData(uint.MaxValue)]
        [InlineData(0xF_FFFF_FFFFUL)]
        [InlineData(9007199254740991UL)]//MAX_SAFE_INTEGER
        public static unsafe void UInt52TestOK(ulong value)
        {
            ulong expected = value;
            ulong dummy = 0xA6A6A6A6UL;
            ulong actual2 = dummy;
            var bagFn = new Function("ptr", "ptr2", @"
                const value=globalThis.App.runtime.getHeapI52(ptr);
                globalThis.App.runtime.setHeapU52(ptr2, value);
                return ''+value;");

            uint ptr = (uint)Unsafe.AsPointer(ref expected);
            uint ptr2 = (uint)Unsafe.AsPointer(ref actual2);

            string actual = (string)bagFn.Call(null, ptr, ptr2);
            Assert.Equal(""+value, actual);
            Assert.Equal(value, actual2);
            Assert.Equal(0xA6A6A6A6UL, dummy);
        }

        [Fact]
        public static unsafe void UInt52TestRandom()
        {
            for(int i = 0; i < 1000; i++)
            {
                var value = (ulong)Random.Shared.NextInt64();
                value&= 0x1F_FFFF_FFFF_FFFFUL;// only safe range
                UInt52TestOK(value);
            }
        }

        [Fact]
        public static unsafe void Int52TestRandom()
        {
            for(int i = 0; i < 1000; i++)
            {
                var value = Random.Shared.NextInt64(-9007199254740991L, 9007199254740991L);
                Int52TestOK(value);
            }
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
                globalThis.App.runtime.setHeapI52(ptr, value);");
            var ex=Assert.Throws<JSException>(() => bagFn.Call(null, ptr, value));
            Assert.Contains("Value is not a safe integer", ex.Message);

            double expectedD = value;
            uint ptrD = (uint)Unsafe.AsPointer(ref expectedD);
            var bagFnD = new Function("ptr", "value", @"
                globalThis.App.runtime.getHeapI52(ptr);");
            var exD = Assert.Throws<JSException>(() => bagFn.Call(null, ptr, value));
            Assert.Contains("Value is not a safe integer", ex.Message);
        }

        [Theory]
        [InlineData(-1.0)]
        public static unsafe void UInt52TestRange(double value)
        {
            long actual = 0;
            uint ptr = (uint)Unsafe.AsPointer(ref actual);
            var bagFn = new Function("ptr", "value", @"
                globalThis.App.runtime.setHeapU52(ptr, value);");
            var ex=Assert.Throws<JSException>(() => bagFn.Call(null, ptr, value));
            Assert.Contains("Can't convert negative Number into UInt64", ex.Message);

            double expectedD = value;
            uint ptrD = (uint)Unsafe.AsPointer(ref expectedD);
            var bagFnD = new Function("ptr", "value", @"
                globalThis.App.runtime.getHeapU52(ptr);");
            var exD = Assert.Throws<JSException>(() => bagFn.Call(null, ptr, value));
            Assert.Contains("Can't convert negative Number into UInt64", ex.Message);
        }

        [Fact]
        public static unsafe void Int52TestNaN()
        {
            long actual = 0;
            uint ptr = (uint)Unsafe.AsPointer(ref actual);
            var bagFn = new Function("ptr", "value", @"
                globalThis.App.runtime.setHeapI52(ptr, value);");
            var ex=Assert.Throws<JSException>(() => bagFn.Call(null, ptr, double.NaN));
            Assert.Contains("Value is not a safe integer: NaN (number)", ex.Message);
        }
    }
}
