// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Runtime;
using Xunit;

namespace System.Runtime.Tests
{
    public static class RuntimeHelpersTests
    {
        [Fact]
        public static void MemoryFailPointTestNoThrow()
        {
            MemoryFailPoint memFailPoint = null;

            memFailPoint = new MemoryFailPoint(1);
            memFailPoint.Dispose();
            memFailPoint = new MemoryFailPoint(2);
            memFailPoint.Dispose();
        }

        [Fact]
        public static void MemoryFailPointTestThrow()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new MemoryFailPoint(Int32.MinValue));
            Assert.Throws<ArgumentOutOfRangeException>(() => new MemoryFailPoint(0));
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)] //https://github.com/dotnet/coreclr/issues/7807
        public static void MemoryFailPointMaxMemoryThrow()
        {
            Assert.Throws<InsufficientMemoryException>(() => new MemoryFailPoint(Int32.MaxValue));
        }
    }
}