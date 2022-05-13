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
    }
}
