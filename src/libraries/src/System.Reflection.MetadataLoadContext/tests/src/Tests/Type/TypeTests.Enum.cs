// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using SampleMetadata;
using Xunit;

namespace System.Reflection.Tests
{
    public static partial class TypeTests
    {
        [Fact]
        public static void EnumUnderlyingTypeTest()
        {
            Type t = typeof(MyColor).Project();
            Type ut = t.GetEnumUnderlyingType();
            Assert.Equal(typeof(int).Project(), ut);
        }

        [Fact]
        public static void EnumUnderlyingTypeTestNotEnum()
        {
            Type t = typeof(object).Project();
            Assert.Throws<ArgumentException>(() => t.GetEnumUnderlyingType());
        }
    }
}
