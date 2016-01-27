// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Xunit;

namespace System.ComponentModel.DataAnnotations.Schema
{
    public class InversePropertyAttributeTests
    {
        [Fact]
        public static void Property_can_be_got_and_set()
        {
            Assert.Equal("Gammer Brevis", new InversePropertyAttribute("Gammer Brevis").Property);
        }

        [Fact]
        public static void Property_cannot_be_set_to_null_or_whitespace()
        {
            Assert.Throws<ArgumentException>(() => new InversePropertyAttribute(null));
            Assert.Throws<ArgumentException>(() => new InversePropertyAttribute(string.Empty));
            Assert.Throws<ArgumentException>(() => new InversePropertyAttribute(" \t\r\n"));
        }
    }
}
