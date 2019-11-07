// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Runtime.CompilerServices.Tests
{
    public static class StrongBoxTests
    {
        [Fact]
        public static void Ctor()
        {
            StrongBox<int> boxedInt32 = new StrongBox<int>();
            Assert.Equal(default(int), boxedInt32.Value);
            boxedInt32 = new StrongBox<int>(42);
            Assert.Equal(42, boxedInt32.Value);

            StrongBox<string> boxedString = new StrongBox<string>();
            Assert.Equal(default(string), boxedString.Value);
            boxedString = new StrongBox<string>("test");
            Assert.Equal("test", boxedString.Value);
        }

        [Fact]
        public static void Value()
        {
            StrongBox<int> sb = new StrongBox<int>();
            Assert.Equal(0, sb.Value);
            sb.Value = 42;
            Assert.Equal(42, sb.Value);

            IStrongBox isb = sb;
            Assert.Equal(42, (int)isb.Value);
            isb.Value = 84;
            Assert.Equal(84, sb.Value);
            Assert.Equal(84, (int)isb.Value);

            Assert.Throws<InvalidCastException>(() => isb.Value = "test");
        }
    }
}
