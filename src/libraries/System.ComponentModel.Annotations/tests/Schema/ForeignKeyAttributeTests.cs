// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Xunit;

namespace System.ComponentModel.DataAnnotations.Schema
{
    public class ForeignKeyAttributeTests
    {
        [Fact]
        public static void Name_can_be_got_and_set()
        {
            Assert.Equal("Old Mother Dismass", new ForeignKeyAttribute("Old Mother Dismass").Name);
        }

        [Fact]
        public static void Name_cannot_be_set_to_null_or_whitespace()
        {
            Assert.Throws<ArgumentException>(() => new ForeignKeyAttribute(null));
            Assert.Throws<ArgumentException>(() => new ForeignKeyAttribute(string.Empty));
            Assert.Throws<ArgumentException>(() => new ForeignKeyAttribute(" \t\r\n"));
        }
    }
}
