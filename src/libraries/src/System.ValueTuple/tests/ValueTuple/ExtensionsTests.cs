// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Text;
using Xunit;
using static ValueTupleTests;

public class ExtensionsTests
{
    [Fact]
    public void Deconstruct21()
    {
        var tuple = CreateLongRef(1, 2, 3, 4, 5, 6, 7, CreateLongRef(8, 9, 10, 11, 12, 13, 14, Tuple.Create(15, 16, 17, 18, 19, 20, 21)));
        var h = new IntHolder();

        tuple.Deconstruct(out h.x1, out h.x2, out h.x3, out h.x4, out h.x5, out h.x6, out h.x7, out h.x8, out h.x9, out h.x10, out h.x11, out h.x12, out h.x13, out h.x14, out h.x15, out h.x16, out h.x17, out h.x18, out h.x19, out h.x20, out h.x21);
        Assert.Equal("1 2 3 4 5 6 7 8 9 10 11 12 13 14 15 16 17 18 19 20 21", h.ToString());
    }

    [Fact]
    public void ConvertToRef21()
    {
        var refTuple = CreateLongRef(-1, -1, -1, -1, -1, -1, -1, CreateLongRef(-1, -1, -1, -1, -1, -1, -1, Tuple.Create(-1, -1, -1, -1, -1, -1, -1)));
        var valueTuple = CreateLong(1, 2, 3, 4, 5, 6, 7, CreateLong(8, 9, 10, 11, 12, 13, 14, ValueTuple.Create(15, 16, 17, 18, 19, 20, 21)));

        refTuple = valueTuple.ToRefTuple();
        Assert.Equal("(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21)", refTuple.ToString());
    }

    [Fact]
    public void ConvertToValue21()
    {
        var valueTuple = CreateLong(-1, -1, -1, -1, -1, -1, -1, CreateLong(-1, -1, -1, -1, -1, -1, -1, ValueTuple.Create(-1, -1, -1, -1, -1, -1, -1)));
        var refTuple = CreateLongRef(1, 2, 3, 4, 5, 6, 7, CreateLongRef(8, 9, 10, 11, 12, 13, 14, Tuple.Create(15, 16, 17, 18, 19, 20, 21)));

        valueTuple = refTuple.ToValueTuple();
        Assert.Equal("(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21)", valueTuple.ToString());
    }

    // Holds 21 int values
    private struct IntHolder
    {
        public int x1;
        public int x2;
        public int x3;
        public int x4;
        public int x5;
        public int x6;
        public int x7;
        public int x8;
        public int x9;
        public int x10;
        public int x11;
        public int x12;
        public int x13;
        public int x14;
        public int x15;
        public int x16;
        public int x17;
        public int x18;
        public int x19;
        public int x20;
        public int x21;

        public override string ToString()
        {
            var builder = new StringBuilder();
            if (x1 > 0) { builder.Append($"{x1}"); }
            if (x2 > 0) { builder.Append($" {x2}"); }
            if (x3 > 0) { builder.Append($" {x3}"); }
            if (x4 > 0) { builder.Append($" {x4}"); }
            if (x5 > 0) { builder.Append($" {x5}"); }
            if (x6 > 0) { builder.Append($" {x6}"); }
            if (x7 > 0) { builder.Append($" {x7}"); }
            if (x8 > 0) { builder.Append($" {x8}"); }
            if (x9 > 0) { builder.Append($" {x9}"); }
            if (x10 > 0) { builder.Append($" {x10}"); }
            if (x11 > 0) { builder.Append($" {x11}"); }
            if (x12 > 0) { builder.Append($" {x12}"); }
            if (x13 > 0) { builder.Append($" {x13}"); }
            if (x14 > 0) { builder.Append($" {x14}"); }
            if (x15 > 0) { builder.Append($" {x15}"); }
            if (x16 > 0) { builder.Append($" {x16}"); }
            if (x17 > 0) { builder.Append($" {x17}"); }
            if (x18 > 0) { builder.Append($" {x18}"); }
            if (x19 > 0) { builder.Append($" {x19}"); }
            if (x20 > 0) { builder.Append($" {x20}"); }
            if (x21 > 0) { builder.Append($" {x21}"); }

            return builder.ToString();
        }
    }
}