// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
