// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text;
using Xunit;

namespace System.Tests
{
    public class ExtensionsTests
    {
        #region Deconstruct
        [Fact]
        public static void Deconstruct1()
        {
            var tuple = Tuple.Create(1);
            var h = new IntHolder();

            tuple.Deconstruct(out h.x1);
            Assert.Equal("1", h.ToString());
        }

        [Fact]
        public static void Deconstruct2()
        {
            var tuple = Tuple.Create(1, 2);
            var h = new IntHolder();

            tuple.Deconstruct(out h.x1, out h.x2);
            Assert.Equal("1 2", h.ToString());
        }

        [Fact]
        public static void Deconstruct3()
        {
            var tuple = Tuple.Create(1, 2, 3);
            var h = new IntHolder();

            tuple.Deconstruct(out h.x1, out h.x2, out h.x3);
            Assert.Equal("1 2 3", h.ToString());
        }

        [Fact]
        public static void Deconstruct4()
        {
            var tuple = Tuple.Create(1, 2, 3, 4);
            var h = new IntHolder();

            tuple.Deconstruct(out h.x1, out h.x2, out h.x3, out h.x4);
            Assert.Equal("1 2 3 4", h.ToString());
        }

        [Fact]
        public static void Deconstruct5()
        {
            var tuple = Tuple.Create(1, 2, 3, 4, 5);
            var h = new IntHolder();

            tuple.Deconstruct(out h.x1, out h.x2, out h.x3, out h.x4, out h.x5);
            Assert.Equal("1 2 3 4 5", h.ToString());
        }

        [Fact]
        public static void Deconstruct6()
        {
            var tuple = Tuple.Create(1, 2, 3, 4, 5, 6);
            var h = new IntHolder();

            tuple.Deconstruct(out h.x1, out h.x2, out h.x3, out h.x4, out h.x5, out h.x6);
            Assert.Equal("1 2 3 4 5 6", h.ToString());
        }

        [Fact]
        public static void Deconstruct7()
        {
            var tuple = Tuple.Create(1, 2, 3, 4, 5, 6, 7);
            var h = new IntHolder();

            tuple.Deconstruct(out h.x1, out h.x2, out h.x3, out h.x4, out h.x5, out h.x6, out h.x7);
            Assert.Equal("1 2 3 4 5 6 7", h.ToString());
        }

        [Fact]
        public static void Deconstruct8()
        {
            var tuple = ValueTupleTests.CreateLongRef(1, 2, 3, 4, 5, 6, 7, Tuple.Create(8));
            var h = new IntHolder();

            tuple.Deconstruct(out h.x1, out h.x2, out h.x3, out h.x4, out h.x5, out h.x6, out h.x7, out h.x8);
            Assert.Equal("1 2 3 4 5 6 7 8", h.ToString());
        }

        [Fact]
        public static void Deconstruct9()
        {
            var tuple = ValueTupleTests.CreateLongRef(1, 2, 3, 4, 5, 6, 7, Tuple.Create(8, 9));
            var h = new IntHolder();

            tuple.Deconstruct(out h.x1, out h.x2, out h.x3, out h.x4, out h.x5, out h.x6, out h.x7, out h.x8, out h.x9);
            Assert.Equal("1 2 3 4 5 6 7 8 9", h.ToString());
        }

        [Fact]
        public static void Deconstruct10()
        {
            var tuple = ValueTupleTests.CreateLongRef(1, 2, 3, 4, 5, 6, 7, Tuple.Create(8, 9, 10));
            var h = new IntHolder();

            tuple.Deconstruct(out h.x1, out h.x2, out h.x3, out h.x4, out h.x5, out h.x6, out h.x7, out h.x8, out h.x9, out h.x10);
            Assert.Equal("1 2 3 4 5 6 7 8 9 10", h.ToString());
        }

        [Fact]
        public static void Deconstruct11()
        {
            var tuple = ValueTupleTests.CreateLongRef(1, 2, 3, 4, 5, 6, 7, Tuple.Create(8, 9, 10, 11));
            var h = new IntHolder();

            tuple.Deconstruct(out h.x1, out h.x2, out h.x3, out h.x4, out h.x5, out h.x6, out h.x7, out h.x8, out h.x9, out h.x10, out h.x11);
            Assert.Equal("1 2 3 4 5 6 7 8 9 10 11", h.ToString());
        }

        [Fact]
        public static void Deconstruct12()
        {
            var tuple = ValueTupleTests.CreateLongRef(1, 2, 3, 4, 5, 6, 7, Tuple.Create(8, 9, 10, 11, 12));
            var h = new IntHolder();

            tuple.Deconstruct(out h.x1, out h.x2, out h.x3, out h.x4, out h.x5, out h.x6, out h.x7, out h.x8, out h.x9, out h.x10, out h.x11, out h.x12);
            Assert.Equal("1 2 3 4 5 6 7 8 9 10 11 12", h.ToString());
        }

        [Fact]
        public static void Deconstruct13()
        {
            var tuple = ValueTupleTests.CreateLongRef(1, 2, 3, 4, 5, 6, 7, Tuple.Create(8, 9, 10, 11, 12, 13));
            var h = new IntHolder();

            tuple.Deconstruct(out h.x1, out h.x2, out h.x3, out h.x4, out h.x5, out h.x6, out h.x7, out h.x8, out h.x9, out h.x10, out h.x11, out h.x12, out h.x13);
            Assert.Equal("1 2 3 4 5 6 7 8 9 10 11 12 13", h.ToString());
        }

        [Fact]
        public static void Deconstruct14()
        {
            var tuple = ValueTupleTests.CreateLongRef(1, 2, 3, 4, 5, 6, 7, Tuple.Create(8, 9, 10, 11, 12, 13, 14));
            var h = new IntHolder();

            tuple.Deconstruct(out h.x1, out h.x2, out h.x3, out h.x4, out h.x5, out h.x6, out h.x7, out h.x8, out h.x9, out h.x10, out h.x11, out h.x12, out h.x13, out h.x14);
            Assert.Equal("1 2 3 4 5 6 7 8 9 10 11 12 13 14", h.ToString());
        }

        [Fact]
        public static void Deconstruct15()
        {
            var tuple = ValueTupleTests.CreateLongRef(1, 2, 3, 4, 5, 6, 7, ValueTupleTests.CreateLongRef(8, 9, 10, 11, 12, 13, 14, Tuple.Create(15)));
            var h = new IntHolder();

            tuple.Deconstruct(out h.x1, out h.x2, out h.x3, out h.x4, out h.x5, out h.x6, out h.x7, out h.x8, out h.x9, out h.x10, out h.x11, out h.x12, out h.x13, out h.x14, out h.x15);
            Assert.Equal("1 2 3 4 5 6 7 8 9 10 11 12 13 14 15", h.ToString());
        }

        [Fact]
        public static void Deconstruct16()
        {
            var tuple = ValueTupleTests.CreateLongRef(1, 2, 3, 4, 5, 6, 7, ValueTupleTests.CreateLongRef(8, 9, 10, 11, 12, 13, 14, Tuple.Create(15, 16)));
            var h = new IntHolder();

            tuple.Deconstruct(out h.x1, out h.x2, out h.x3, out h.x4, out h.x5, out h.x6, out h.x7, out h.x8, out h.x9, out h.x10, out h.x11, out h.x12, out h.x13, out h.x14, out h.x15, out h.x16);
            Assert.Equal("1 2 3 4 5 6 7 8 9 10 11 12 13 14 15 16", h.ToString());
        }

        [Fact]
        public static void Deconstruct17()
        {
            var tuple = ValueTupleTests.CreateLongRef(1, 2, 3, 4, 5, 6, 7, ValueTupleTests.CreateLongRef(8, 9, 10, 11, 12, 13, 14, Tuple.Create(15, 16, 17)));
            var h = new IntHolder();

            tuple.Deconstruct(out h.x1, out h.x2, out h.x3, out h.x4, out h.x5, out h.x6, out h.x7, out h.x8, out h.x9, out h.x10, out h.x11, out h.x12, out h.x13, out h.x14, out h.x15, out h.x16, out h.x17);
            Assert.Equal("1 2 3 4 5 6 7 8 9 10 11 12 13 14 15 16 17", h.ToString());
        }

        [Fact]
        public static void Deconstruct18()
        {
            var tuple = ValueTupleTests.CreateLongRef(1, 2, 3, 4, 5, 6, 7, ValueTupleTests.CreateLongRef(8, 9, 10, 11, 12, 13, 14, Tuple.Create(15, 16, 17, 18)));
            var h = new IntHolder();

            tuple.Deconstruct(out h.x1, out h.x2, out h.x3, out h.x4, out h.x5, out h.x6, out h.x7, out h.x8, out h.x9, out h.x10, out h.x11, out h.x12, out h.x13, out h.x14, out h.x15, out h.x16, out h.x17, out h.x18);
            Assert.Equal("1 2 3 4 5 6 7 8 9 10 11 12 13 14 15 16 17 18", h.ToString());
        }

        [Fact]
        public static void Deconstruct19()
        {
            var tuple = ValueTupleTests.CreateLongRef(1, 2, 3, 4, 5, 6, 7, ValueTupleTests.CreateLongRef(8, 9, 10, 11, 12, 13, 14, Tuple.Create(15, 16, 17, 18, 19)));
            var h = new IntHolder();

            tuple.Deconstruct(out h.x1, out h.x2, out h.x3, out h.x4, out h.x5, out h.x6, out h.x7, out h.x8, out h.x9, out h.x10, out h.x11, out h.x12, out h.x13, out h.x14, out h.x15, out h.x16, out h.x17, out h.x18, out h.x19);
            Assert.Equal("1 2 3 4 5 6 7 8 9 10 11 12 13 14 15 16 17 18 19", h.ToString());
        }

        [Fact]
        public static void Deconstruct20()
        {
            var tuple = ValueTupleTests.CreateLongRef(1, 2, 3, 4, 5, 6, 7, ValueTupleTests.CreateLongRef(8, 9, 10, 11, 12, 13, 14, Tuple.Create(15, 16, 17, 18, 19, 20)));
            var h = new IntHolder();

            tuple.Deconstruct(out h.x1, out h.x2, out h.x3, out h.x4, out h.x5, out h.x6, out h.x7, out h.x8, out h.x9, out h.x10, out h.x11, out h.x12, out h.x13, out h.x14, out h.x15, out h.x16, out h.x17, out h.x18, out h.x19, out h.x20);
            Assert.Equal("1 2 3 4 5 6 7 8 9 10 11 12 13 14 15 16 17 18 19 20", h.ToString());
        }

        [Fact]
        public static void Deconstruct21()
        {
            var tuple = ValueTupleTests.CreateLongRef(1, 2, 3, 4, 5, 6, 7, ValueTupleTests.CreateLongRef(8, 9, 10, 11, 12, 13, 14, Tuple.Create(15, 16, 17, 18, 19, 20, 21)));
            var h = new IntHolder();

            tuple.Deconstruct(out h.x1, out h.x2, out h.x3, out h.x4, out h.x5, out h.x6, out h.x7, out h.x8, out h.x9, out h.x10, out h.x11, out h.x12, out h.x13, out h.x14, out h.x15, out h.x16, out h.x17, out h.x18, out h.x19, out h.x20, out h.x21);
            Assert.Equal("1 2 3 4 5 6 7 8 9 10 11 12 13 14 15 16 17 18 19 20 21", h.ToString());
        }
        #endregion

        #region ToTuple
        [Fact]
        public static void ConvertToRef1()
        {
            var refTuple = Tuple.Create(-1);
            var valueTuple = ValueTuple.Create(1);

            refTuple = valueTuple.ToTuple();
            Assert.Equal("(1)", refTuple.ToString());
        }

        [Fact]
        public static void ConvertToRef2()
        {
            var refTuple = Tuple.Create(-1, -1);
            var valueTuple = ValueTuple.Create(1, 2);

            refTuple = valueTuple.ToTuple();
            Assert.Equal("(1, 2)", refTuple.ToString());
        }

        [Fact]
        public static void ConvertToRef3()
        {
            var refTuple = Tuple.Create(-1, -1, -1);
            var valueTuple = ValueTuple.Create(1, 2, 3);

            refTuple = valueTuple.ToTuple();
            Assert.Equal("(1, 2, 3)", refTuple.ToString());
        }

        [Fact]
        public static void ConvertToRef4()
        {
            var refTuple = Tuple.Create(-1, -1, -1, -1);
            var valueTuple = ValueTuple.Create(1, 2, 3, 4);

            refTuple = valueTuple.ToTuple();
            Assert.Equal("(1, 2, 3, 4)", refTuple.ToString());
        }

        [Fact]
        public static void ConvertToRef5()
        {
            var refTuple = Tuple.Create(-1, -1, -1, -1, -1);
            var valueTuple = ValueTuple.Create(1, 2, 3, 4, 5);

            refTuple = valueTuple.ToTuple();
            Assert.Equal("(1, 2, 3, 4, 5)", refTuple.ToString());
        }

        [Fact]
        public static void ConvertToRef6()
        {
            var refTuple = Tuple.Create(-1, -1, -1, -1, -1, -1);
            var valueTuple = ValueTuple.Create(1, 2, 3, 4, 5, 6);

            refTuple = valueTuple.ToTuple();
            Assert.Equal("(1, 2, 3, 4, 5, 6)", refTuple.ToString());
        }

        [Fact]
        public static void ConvertToRef7()
        {
            var refTuple = Tuple.Create(-1, -1, -1, -1, -1, -1, -1);
            var valueTuple = ValueTuple.Create(1, 2, 3, 4, 5, 6, 7);

            refTuple = valueTuple.ToTuple();
            Assert.Equal("(1, 2, 3, 4, 5, 6, 7)", refTuple.ToString());
        }

        [Fact]
        public static void ConvertToRef8()
        {
            var refTuple = ValueTupleTests.CreateLongRef(-1, -1, -1, -1, -1, -1, -1, Tuple.Create(-1));
            var valueTuple = ValueTupleTests.CreateLong(1, 2, 3, 4, 5, 6, 7, ValueTuple.Create(8));

            refTuple = valueTuple.ToTuple();
            Assert.Equal("(1, 2, 3, 4, 5, 6, 7, 8)", refTuple.ToString());
        }

        [Fact]
        public static void ConvertToRef9()
        {
            var refTuple = ValueTupleTests.CreateLongRef(-1, -1, -1, -1, -1, -1, -1, Tuple.Create(-1, -1));
            var valueTuple = ValueTupleTests.CreateLong(1, 2, 3, 4, 5, 6, 7, ValueTuple.Create(8, 9));

            refTuple = valueTuple.ToTuple();
            Assert.Equal("(1, 2, 3, 4, 5, 6, 7, 8, 9)", refTuple.ToString());
        }

        [Fact]
        public static void ConvertToRef10()
        {
            var refTuple = ValueTupleTests.CreateLongRef(-1, -1, -1, -1, -1, -1, -1, Tuple.Create(-1, -1, -1));
            var valueTuple = ValueTupleTests.CreateLong(1, 2, 3, 4, 5, 6, 7, ValueTuple.Create(8, 9, 10));

            refTuple = valueTuple.ToTuple();
            Assert.Equal("(1, 2, 3, 4, 5, 6, 7, 8, 9, 10)", refTuple.ToString());
        }

        [Fact]
        public static void ConvertToRef11()
        {
            var refTuple = ValueTupleTests.CreateLongRef(-1, -1, -1, -1, -1, -1, -1, Tuple.Create(-1, -1, -1, -1));
            var valueTuple = ValueTupleTests.CreateLong(1, 2, 3, 4, 5, 6, 7, ValueTuple.Create(8, 9, 10, 11));

            refTuple = valueTuple.ToTuple();
            Assert.Equal("(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11)", refTuple.ToString());
        }

        [Fact]
        public static void ConvertToRef12()
        {
            var refTuple = ValueTupleTests.CreateLongRef(-1, -1, -1, -1, -1, -1, -1, Tuple.Create(-1, -1, -1, -1, -1));
            var valueTuple = ValueTupleTests.CreateLong(1, 2, 3, 4, 5, 6, 7, ValueTuple.Create(8, 9, 10, 11, 12));

            refTuple = valueTuple.ToTuple();
            Assert.Equal("(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12)", refTuple.ToString());
        }

        [Fact]
        public static void ConvertToRef13()
        {
            var refTuple = ValueTupleTests.CreateLongRef(-1, -1, -1, -1, -1, -1, -1, Tuple.Create(-1, -1, -1, -1, -1, -1));
            var valueTuple = ValueTupleTests.CreateLong(1, 2, 3, 4, 5, 6, 7, ValueTuple.Create(8, 9, 10, 11, 12, 13));

            refTuple = valueTuple.ToTuple();
            Assert.Equal("(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13)", refTuple.ToString());
        }

        [Fact]
        public static void ConvertToRef14()
        {
            var refTuple = ValueTupleTests.CreateLongRef(-1, -1, -1, -1, -1, -1, -1, Tuple.Create(-1, -1, -1, -1, -1, -1, -1));
            var valueTuple = ValueTupleTests.CreateLong(1, 2, 3, 4, 5, 6, 7, ValueTuple.Create(8, 9, 10, 11, 12, 13, 14));

            refTuple = valueTuple.ToTuple();
            Assert.Equal("(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14)", refTuple.ToString());
        }

        [Fact]
        public static void ConvertToRef15()
        {
            var refTuple = ValueTupleTests.CreateLongRef(-1, -1, -1, -1, -1, -1, -1, ValueTupleTests.CreateLongRef(-1, -1, -1, -1, -1, -1, -1, Tuple.Create(-1)));
            var valueTuple = ValueTupleTests.CreateLong(1, 2, 3, 4, 5, 6, 7, ValueTupleTests.CreateLong(8, 9, 10, 11, 12, 13, 14, ValueTuple.Create(15)));

            refTuple = valueTuple.ToTuple();
            Assert.Equal("(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15)", refTuple.ToString());
        }

        [Fact]
        public static void ConvertToRef16()
        {
            var refTuple = ValueTupleTests.CreateLongRef(-1, -1, -1, -1, -1, -1, -1, ValueTupleTests.CreateLongRef(-1, -1, -1, -1, -1, -1, -1, Tuple.Create(-1, -1)));
            var valueTuple = ValueTupleTests.CreateLong(1, 2, 3, 4, 5, 6, 7, ValueTupleTests.CreateLong(8, 9, 10, 11, 12, 13, 14, ValueTuple.Create(15, 16)));

            refTuple = valueTuple.ToTuple();
            Assert.Equal("(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16)", refTuple.ToString());
        }

        [Fact]
        public static void ConvertToRef17()
        {
            var refTuple = ValueTupleTests.CreateLongRef(-1, -1, -1, -1, -1, -1, -1, ValueTupleTests.CreateLongRef(-1, -1, -1, -1, -1, -1, -1, Tuple.Create(-1, -1, -1)));
            var valueTuple = ValueTupleTests.CreateLong(1, 2, 3, 4, 5, 6, 7, ValueTupleTests.CreateLong(8, 9, 10, 11, 12, 13, 14, ValueTuple.Create(15, 16, 17)));

            refTuple = valueTuple.ToTuple();
            Assert.Equal("(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17)", refTuple.ToString());
        }

        [Fact]
        public static void ConvertToRef18()
        {
            var refTuple = ValueTupleTests.CreateLongRef(-1, -1, -1, -1, -1, -1, -1, ValueTupleTests.CreateLongRef(-1, -1, -1, -1, -1, -1, -1, Tuple.Create(-1, -1, -1, -1)));
            var valueTuple = ValueTupleTests.CreateLong(1, 2, 3, 4, 5, 6, 7, ValueTupleTests.CreateLong(8, 9, 10, 11, 12, 13, 14, ValueTuple.Create(15, 16, 17, 18)));

            refTuple = valueTuple.ToTuple();
            Assert.Equal("(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18)", refTuple.ToString());
        }

        [Fact]
        public static void ConvertToRef19()
        {
            var refTuple = ValueTupleTests.CreateLongRef(-1, -1, -1, -1, -1, -1, -1, ValueTupleTests.CreateLongRef(-1, -1, -1, -1, -1, -1, -1, Tuple.Create(-1, -1, -1, -1, -1)));
            var valueTuple = ValueTupleTests.CreateLong(1, 2, 3, 4, 5, 6, 7, ValueTupleTests.CreateLong(8, 9, 10, 11, 12, 13, 14, ValueTuple.Create(15, 16, 17, 18, 19)));

            refTuple = valueTuple.ToTuple();
            Assert.Equal("(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19)", refTuple.ToString());
        }

        [Fact]
        public static void ConvertToRef20()
        {
            var refTuple = ValueTupleTests.CreateLongRef(-1, -1, -1, -1, -1, -1, -1, ValueTupleTests.CreateLongRef(-1, -1, -1, -1, -1, -1, -1, Tuple.Create(-1, -1, -1, -1, -1, -1)));
            var valueTuple = ValueTupleTests.CreateLong(1, 2, 3, 4, 5, 6, 7, ValueTupleTests.CreateLong(8, 9, 10, 11, 12, 13, 14, ValueTuple.Create(15, 16, 17, 18, 19, 20)));

            refTuple = valueTuple.ToTuple();
            Assert.Equal("(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20)", refTuple.ToString());
        }

        [Fact]
        public static void ConvertToRef21()
        {
            var refTuple = ValueTupleTests.CreateLongRef(-1, -1, -1, -1, -1, -1, -1, ValueTupleTests.CreateLongRef(-1, -1, -1, -1, -1, -1, -1, Tuple.Create(-1, -1, -1, -1, -1, -1, -1)));
            var valueTuple = ValueTupleTests.CreateLong(1, 2, 3, 4, 5, 6, 7, ValueTupleTests.CreateLong(8, 9, 10, 11, 12, 13, 14, ValueTuple.Create(15, 16, 17, 18, 19, 20, 21)));

            refTuple = valueTuple.ToTuple();
            Assert.Equal("(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21)", refTuple.ToString());
        }
        #endregion

        #region ToValue
        [Fact]
        public static void ConvertToValue1()
        {
            var valueTuple = ValueTuple.Create(-1);
            var refTuple = Tuple.Create(1);

            valueTuple = refTuple.ToValueTuple();
            Assert.Equal("(1)", valueTuple.ToString());
        }

        [Fact]
        public static void ConvertToValue2()
        {
            var valueTuple = ValueTuple.Create(-1, -1);
            var refTuple = Tuple.Create(1, 2);

            valueTuple = refTuple.ToValueTuple();
            Assert.Equal("(1, 2)", valueTuple.ToString());
        }

        [Fact]
        public static void ConvertToValue3()
        {
            var valueTuple = ValueTuple.Create(-1, -1, -1);
            var refTuple = Tuple.Create(1, 2, 3);

            valueTuple = refTuple.ToValueTuple();
            Assert.Equal("(1, 2, 3)", valueTuple.ToString());
        }

        [Fact]
        public static void ConvertToValue4()
        {
            var valueTuple = ValueTuple.Create(-1, -1, -1, -1);
            var refTuple = Tuple.Create(1, 2, 3, 4);

            valueTuple = refTuple.ToValueTuple();
            Assert.Equal("(1, 2, 3, 4)", valueTuple.ToString());
        }

        [Fact]
        public static void ConvertToValue5()
        {
            var valueTuple = ValueTuple.Create(-1, -1, -1, -1, -1);
            var refTuple = Tuple.Create(1, 2, 3, 4, 5);

            valueTuple = refTuple.ToValueTuple();
            Assert.Equal("(1, 2, 3, 4, 5)", valueTuple.ToString());
        }

        [Fact]
        public static void ConvertToValue6()
        {
            var valueTuple = ValueTuple.Create(-1, -1, -1, -1, -1, -1);
            var refTuple = Tuple.Create(1, 2, 3, 4, 5, 6);

            valueTuple = refTuple.ToValueTuple();
            Assert.Equal("(1, 2, 3, 4, 5, 6)", valueTuple.ToString());
        }

        [Fact]
        public static void ConvertToValue7()
        {
            var valueTuple = ValueTuple.Create(-1, -1, -1, -1, -1, -1, -1);
            var refTuple = Tuple.Create(1, 2, 3, 4, 5, 6, 7);

            valueTuple = refTuple.ToValueTuple();
            Assert.Equal("(1, 2, 3, 4, 5, 6, 7)", valueTuple.ToString());
        }

        [Fact]
        public static void ConvertToValue8()
        {
            var valueTuple = ValueTupleTests.CreateLong(-1, -1, -1, -1, -1, -1, -1, ValueTuple.Create(-1));
            var refTuple = ValueTupleTests.CreateLongRef(1, 2, 3, 4, 5, 6, 7, Tuple.Create(8));

            valueTuple = refTuple.ToValueTuple();
            Assert.Equal("(1, 2, 3, 4, 5, 6, 7, 8)", valueTuple.ToString());
        }

        [Fact]
        public static void ConvertToValue9()
        {
            var valueTuple = ValueTupleTests.CreateLong(-1, -1, -1, -1, -1, -1, -1, ValueTuple.Create(-1, -1));
            var refTuple = ValueTupleTests.CreateLongRef(1, 2, 3, 4, 5, 6, 7, Tuple.Create(8, 9));

            valueTuple = refTuple.ToValueTuple();
            Assert.Equal("(1, 2, 3, 4, 5, 6, 7, 8, 9)", valueTuple.ToString());
        }

        [Fact]
        public static void ConvertToValue10()
        {
            var valueTuple = ValueTupleTests.CreateLong(-1, -1, -1, -1, -1, -1, -1, ValueTuple.Create(-1, -1, -1));
            var refTuple = ValueTupleTests.CreateLongRef(1, 2, 3, 4, 5, 6, 7, Tuple.Create(8, 9, 10));

            valueTuple = refTuple.ToValueTuple();
            Assert.Equal("(1, 2, 3, 4, 5, 6, 7, 8, 9, 10)", valueTuple.ToString());
        }

        [Fact]
        public static void ConvertToValue11()
        {
            var valueTuple = ValueTupleTests.CreateLong(-1, -1, -1, -1, -1, -1, -1, ValueTuple.Create(-1, -1, -1, -1));
            var refTuple = ValueTupleTests.CreateLongRef(1, 2, 3, 4, 5, 6, 7, Tuple.Create(8, 9, 10, 11));

            valueTuple = refTuple.ToValueTuple();
            Assert.Equal("(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11)", valueTuple.ToString());
        }

        [Fact]
        public static void ConvertToValue12()
        {
            var valueTuple = ValueTupleTests.CreateLong(-1, -1, -1, -1, -1, -1, -1, ValueTuple.Create(-1, -1, -1, -1, -1));
            var refTuple = ValueTupleTests.CreateLongRef(1, 2, 3, 4, 5, 6, 7, Tuple.Create(8, 9, 10, 11, 12));

            valueTuple = refTuple.ToValueTuple();
            Assert.Equal("(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12)", valueTuple.ToString());
        }

        [Fact]
        public static void ConvertToValue13()
        {
            var valueTuple = ValueTupleTests.CreateLong(-1, -1, -1, -1, -1, -1, -1, ValueTuple.Create(-1, -1, -1, -1, -1, -1));
            var refTuple = ValueTupleTests.CreateLongRef(1, 2, 3, 4, 5, 6, 7, Tuple.Create(8, 9, 10, 11, 12, 13));

            valueTuple = refTuple.ToValueTuple();
            Assert.Equal("(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13)", valueTuple.ToString());
        }

        [Fact]
        public static void ConvertToValue14()
        {
            var valueTuple = ValueTupleTests.CreateLong(-1, -1, -1, -1, -1, -1, -1, ValueTuple.Create(-1, -1, -1, -1, -1, -1, -1));
            var refTuple = ValueTupleTests.CreateLongRef(1, 2, 3, 4, 5, 6, 7, Tuple.Create(8, 9, 10, 11, 12, 13, 14));

            valueTuple = refTuple.ToValueTuple();
            Assert.Equal("(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14)", valueTuple.ToString());
        }

        [Fact]
        public static void ConvertToValue15()
        {
            var valueTuple = ValueTupleTests.CreateLong(-1, -1, -1, -1, -1, -1, -1, ValueTupleTests.CreateLong(-1, -1, -1, -1, -1, -1, -1, ValueTuple.Create(-1)));
            var refTuple = ValueTupleTests.CreateLongRef(1, 2, 3, 4, 5, 6, 7, ValueTupleTests.CreateLongRef(8, 9, 10, 11, 12, 13, 14, Tuple.Create(15)));

            valueTuple = refTuple.ToValueTuple();
            Assert.Equal("(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15)", valueTuple.ToString());
        }

        [Fact]
        public static void ConvertToValue16()
        {
            var valueTuple = ValueTupleTests.CreateLong(-1, -1, -1, -1, -1, -1, -1, ValueTupleTests.CreateLong(-1, -1, -1, -1, -1, -1, -1, ValueTuple.Create(-1, -1)));
            var refTuple = ValueTupleTests.CreateLongRef(1, 2, 3, 4, 5, 6, 7, ValueTupleTests.CreateLongRef(8, 9, 10, 11, 12, 13, 14, Tuple.Create(15, 16)));

            valueTuple = refTuple.ToValueTuple();
            Assert.Equal("(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16)", valueTuple.ToString());
        }

        [Fact]
        public static void ConvertToValue17()
        {
            var valueTuple = ValueTupleTests.CreateLong(-1, -1, -1, -1, -1, -1, -1, ValueTupleTests.CreateLong(-1, -1, -1, -1, -1, -1, -1, ValueTuple.Create(-1, -1, -1)));
            var refTuple = ValueTupleTests.CreateLongRef(1, 2, 3, 4, 5, 6, 7, ValueTupleTests.CreateLongRef(8, 9, 10, 11, 12, 13, 14, Tuple.Create(15, 16, 17)));

            valueTuple = refTuple.ToValueTuple();
            Assert.Equal("(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17)", valueTuple.ToString());
        }

        [Fact]
        public static void ConvertToValue18()
        {
            var valueTuple = ValueTupleTests.CreateLong(-1, -1, -1, -1, -1, -1, -1, ValueTupleTests.CreateLong(-1, -1, -1, -1, -1, -1, -1, ValueTuple.Create(-1, -1, -1, -1)));
            var refTuple = ValueTupleTests.CreateLongRef(1, 2, 3, 4, 5, 6, 7, ValueTupleTests.CreateLongRef(8, 9, 10, 11, 12, 13, 14, Tuple.Create(15, 16, 17, 18)));

            valueTuple = refTuple.ToValueTuple();
            Assert.Equal("(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18)", valueTuple.ToString());
        }

        [Fact]
        public static void ConvertToValue19()
        {
            var valueTuple = ValueTupleTests.CreateLong(-1, -1, -1, -1, -1, -1, -1, ValueTupleTests.CreateLong(-1, -1, -1, -1, -1, -1, -1, ValueTuple.Create(-1, -1, -1, -1, -1)));
            var refTuple = ValueTupleTests.CreateLongRef(1, 2, 3, 4, 5, 6, 7, ValueTupleTests.CreateLongRef(8, 9, 10, 11, 12, 13, 14, Tuple.Create(15, 16, 17, 18, 19)));

            valueTuple = refTuple.ToValueTuple();
            Assert.Equal("(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19)", valueTuple.ToString());
        }

        [Fact]
        public static void ConvertToValue20()
        {
            var valueTuple = ValueTupleTests.CreateLong(-1, -1, -1, -1, -1, -1, -1, ValueTupleTests.CreateLong(-1, -1, -1, -1, -1, -1, -1, ValueTuple.Create(-1, -1, -1, -1, -1, -1)));
            var refTuple = ValueTupleTests.CreateLongRef(1, 2, 3, 4, 5, 6, 7, ValueTupleTests.CreateLongRef(8, 9, 10, 11, 12, 13, 14, Tuple.Create(15, 16, 17, 18, 19, 20)));

            valueTuple = refTuple.ToValueTuple();
            Assert.Equal("(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20)", valueTuple.ToString());
        }

        [Fact]
        public static void ConvertToValue21()
        {
            var valueTuple = ValueTupleTests.CreateLong(-1, -1, -1, -1, -1, -1, -1, ValueTupleTests.CreateLong(-1, -1, -1, -1, -1, -1, -1, ValueTuple.Create(-1, -1, -1, -1, -1, -1, -1)));
            var refTuple = ValueTupleTests.CreateLongRef(1, 2, 3, 4, 5, 6, 7, ValueTupleTests.CreateLongRef(8, 9, 10, 11, 12, 13, 14, Tuple.Create(15, 16, 17, 18, 19, 20, 21)));

            valueTuple = refTuple.ToValueTuple();
            Assert.Equal("(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21)", valueTuple.ToString());
        }
        #endregion

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
                if (x1 > 0) { builder.Append(x1); }
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
}
