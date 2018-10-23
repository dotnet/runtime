// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Xunit;

namespace System.Tests
{
    public static class ObsoleteAttributeTests
    {
        [Fact]
        public static void Ctor_Default()
        {
            var attribute = new ObsoleteAttribute();
            Assert.Null(attribute.Message);
            Assert.False(attribute.IsError);
        }

        [Fact]
        public static void Ctor_String()
        {
            var attribute = new ObsoleteAttribute("this is obsolete");
            Assert.Equal("this is obsolete", attribute.Message);
            Assert.False(attribute.IsError);
        }

        [Fact]
        public static void Ctor_String_Bool()
        {
            var attribute = new ObsoleteAttribute("this is obsolete", true);
            Assert.Equal("this is obsolete", attribute.Message);
            Assert.True(attribute.IsError);
        }
    }
}
