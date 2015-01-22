// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Xunit;

namespace System.ComponentModel.DataAnnotations.Schema
{
    public class ColumnAttributeTests
    {
        [Fact]
        public static void Default_values_are_null_or_negative_one()
        {
            Assert.Null(new ColumnAttribute().Name);
            Assert.Equal(-1, new ColumnAttribute().Order);
            Assert.Null(new ColumnAttribute().TypeName);
        }

        [Fact]
        public static void Name_can_be_got_and_set()
        {
            Assert.Equal("Granny Weatherwax", new ColumnAttribute("Granny Weatherwax").Name);
        }

        [Fact]
        public static void Name_cannot_be_set_to_null_or_whitespace()
        {
            Assert.Throws<ArgumentException>(() => new ColumnAttribute(null));
            Assert.Throws<ArgumentException>(() => new ColumnAttribute(string.Empty));
            Assert.Throws<ArgumentException>(() => new ColumnAttribute(" \t\r\n"));
        }

        [Fact]
        public static void Order_can_be_got_and_set()
        {
            Assert.Equal(
                0,
                new ColumnAttribute
                {
                    Order = 0
                }.Order);
        }

        [Fact]
        public static void Order_cannot_be_set_to_negative_value()
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new ColumnAttribute
                {
                    Order = -1
                });
        }

        [Fact]
        public static void TypeName_can_be_got_and_set()
        {
            Assert.Equal(
                "Nanny Ogg",
                new ColumnAttribute
                {
                    TypeName = "Nanny Ogg"
                }.TypeName);
        }

        [Fact]
        public static void TypeName_cannot_be_set_to_null_or_whitespace()
        {
            Assert.Throws<ArgumentException>(
                () => new ColumnAttribute
                {
                    TypeName = null
                });

            Assert.Throws<ArgumentException>(
                () => new ColumnAttribute
                {
                    TypeName = string.Empty
                });

            Assert.Throws<ArgumentException>(
                () => new ColumnAttribute
                {
                    TypeName = " \t\r\n"
                });
        }
    }
}
