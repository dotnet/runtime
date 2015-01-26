// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Xunit;

namespace System.ComponentModel.DataAnnotations.Schema
{
    public class TableAttributeTests
    {
        [Fact]
        public static void Name_can_be_got_and_set()
        {
            Assert.Equal("Black Aliss", new TableAttribute("Black Aliss").Name);
        }

        [Fact]
        public static void Name_cannot_be_set_to_null_or_whitespace()
        {
            Assert.Throws<ArgumentException>(() => new TableAttribute(null));
            Assert.Throws<ArgumentException>(() => new TableAttribute(string.Empty));
            Assert.Throws<ArgumentException>(() => new TableAttribute(" \t\r\n"));
        }

        [Fact]
        public static void Default_value_for_schema_is_null()
        {
            Assert.Null(new TableAttribute("Perspicacia Tick").Schema);
        }

        [Fact]
        public static void Schema_can_be_got_and_set()
        {
            Assert.Equal(
                "Mrs Letice Earwig", new TableAttribute("Perspicacia Tick")
                {
                    Schema = "Mrs Letice Earwig"
                }.Schema);
        }

        [Fact]
        public static void Schema_cannot_be_set_to_null_or_whitespace()
        {
            Assert.Throws<ArgumentException>(
                () => new TableAttribute("Perspicacia Tick")
                {
                    Schema = null
                });
            Assert.Throws<ArgumentException>(
                () => new TableAttribute("Perspicacia Tick")
                {
                    Schema = string.Empty
                });
            Assert.Throws<ArgumentException>(
                () => new TableAttribute("Perspicacia Tick")
                {
                    Schema = " \t\r\n"
                });
        }
    }
}
