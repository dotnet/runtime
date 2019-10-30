// Licensed to the .NET Foundation under one or more agreements.
// See the LICENSE file in the project root for more information.
//
// Authors:
//  Sebastien Pouliot  <sebastien@ximian.com>
//
// Copyright (C) 2007 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using System.Collections.Generic;
using Xunit;

namespace System.Drawing.Printing.Tests
{
    public class MarginsTests
    {
        [Fact]
        public void Ctor_Default()
        {
            var margins = new Margins();
            Assert.Equal(100, margins.Left);
            Assert.Equal(100, margins.Top);
            Assert.Equal(100, margins.Right);
            Assert.Equal(100, margins.Bottom);
        }

        [Theory]
        [InlineData(int.MaxValue, int.MaxValue, int.MaxValue, int.MaxValue)]
        [InlineData(0, 1, 2, 3)]
        [InlineData(0, 0, 0, 0)]
        public void Ctor_Bounds(int left, int right, int top, int bottom)
        {
            var margins = new Margins(left, right, top, bottom);
            Assert.Equal(left, margins.Left);
            Assert.Equal(right, margins.Right);
            Assert.Equal(top, margins.Top);
            Assert.Equal(bottom, margins.Bottom);
        }

        [Fact]
        public void Ctor_NegativeLeft_ThrowsArgumentOutOfRangeException()
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException, ArgumentException>("left", null, () => new Margins(-1, 2, 3, 4));
        }

        [Fact]
        public void Ctor_NegativeRight_ThrowsArgumentOutOfRangeException()
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException, ArgumentException>("right", null, () => new Margins(1, -1, 3, 4));
        }

        [Fact]
        public void Ctor_NegativeTop_ThrowsArgumentOutOfRangeException()
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException, ArgumentException>("top", null, () => new Margins(1, 2, -1, 4));
        }

        [Fact]
        public void Ctor_NegativeBottom_ThrowsArgumentOutOfRangeException()
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException, ArgumentException>("bottom", null, () => new Margins(1, 2, 3, -1));
        }

        public static IEnumerable<object[]> Equals_Object_TestData()
        {
            var margins = new Margins(1, 2, 3, 4);
            yield return new object[] { margins, margins, true };
            yield return new object[] { margins, new Margins(1, 2, 3, 4), true };
            yield return new object[] { margins, new Margins(2, 2, 3, 4), false };
            yield return new object[] { margins, new Margins(1, 3, 3, 4), false };
            yield return new object[] { margins, new Margins(1, 2, 4, 4), false };
            yield return new object[] { margins, new Margins(1, 2, 3, 5), false };

            yield return new object[] { margins, new object(), false };
            yield return new object[] { margins, null, false };
        }

        [Theory]
        [MemberData(nameof(Equals_Object_TestData))]
        public void Equals_InvokeObject_ReturnsExpected(Margins margins, object obj, bool expected)
        {
            Assert.Equal(expected, margins.Equals(obj));
            if (obj is Margins)
            {
                Assert.Equal(expected, margins.GetHashCode().Equals(obj.GetHashCode()));
            }
        }

        public static IEnumerable<object[]> Equals_Margin_TestData()
        {
            var margins = new Margins(1, 2, 3, 4);
            yield return new object[] { margins, margins, true };
            yield return new object[] { margins, new Margins(1, 2, 3, 4), true };
            yield return new object[] { margins, new Margins(2, 2, 3, 4), false };
            yield return new object[] { margins, new Margins(1, 3, 3, 4), false };
            yield return new object[] { margins, new Margins(1, 2, 4, 4), false };
            yield return new object[] { margins, new Margins(1, 2, 3, 5), false };

            yield return new object[] { null, null, true };
            yield return new object[] { null, new Margins(1, 2, 3, 4), false };
            yield return new object[] { new Margins(1, 2, 3, 4), null, false };
        }

        [Theory]
        [MemberData(nameof(Equals_Margin_TestData))]
        public void Equals_InvokeMargin_ReturnsExpected(Margins margins1, Margins margins2, bool expected)
        {
            Assert.Equal(expected, margins1 == margins2);
            Assert.Equal(!expected, margins1 != margins2);
        }

        public static IEnumerable<object[]> ToString_TestData()
        {
            yield return new object[] { new Margins(), "[Margins Left=100 Right=100 Top=100 Bottom=100]" };
            yield return new object[] { new Margins(1, 2, 3, 4), "[Margins Left=1 Right=2 Top=3 Bottom=4]" };
        }

        [Theory]
        [MemberData(nameof(ToString_TestData))]
        public void ToString_Invoke_ReturnsExpected(Margins margins, string expected)
        {
            Assert.Equal(expected, margins.ToString());
        }

        [Fact]
        public void Clone_Invoke_ReturnsExpected()
        {
            var margins = new Margins(1, 2, 3, 4);
            Margins clonedMargins = Assert.IsType<Margins>(margins.Clone());
            Assert.NotSame(margins, clonedMargins);
            Assert.Equal(1, clonedMargins.Left);
            Assert.Equal(2, clonedMargins.Right);
            Assert.Equal(3, clonedMargins.Top);
            Assert.Equal(4, clonedMargins.Bottom);
        }

        public static IEnumerable<object[]> Bounds_Set_TestData()
        {
            yield return new object[] { 0 };
            yield return new object[] { 10 };
            yield return new object[] { int.MaxValue };
        }

        [Theory]
        [MemberData(nameof(Bounds_Set_TestData))]
        public void Left_Set_GetReturnsExpected(int value)
        {
            var margins = new Margins
            {
                Left = value
            };
            Assert.Equal(value, margins.Left);

            // Set same.
            margins.Left = value;
            Assert.Equal(value, margins.Left);
        }

        [Fact]
        public void Left_SetNegative_ThrowsArgumentOutOfRangeException()
        {
            var margins = new Margins();
            AssertExtensions.Throws<ArgumentOutOfRangeException, ArgumentException>("value", null, () => margins.Left = -1);
        }

        [Theory]
        [MemberData(nameof(Bounds_Set_TestData))]
        public void Right_Set_GetReturnsExpected(int value)
        {
            var margins = new Margins
            {
                Right = value
            };
            Assert.Equal(value, margins.Right);

            // Set same.
            margins.Right = value;
            Assert.Equal(value, margins.Right);
        }

        [Fact]
        public void Right_SetNegative_ThrowsArgumentOutOfRangeException()
        {
            var margins = new Margins();
            AssertExtensions.Throws<ArgumentOutOfRangeException, ArgumentException>("value", null, () => margins.Right = -1);
        }

        [Theory]
        [MemberData(nameof(Bounds_Set_TestData))]
        public void Top_Set_GetReturnsExpected(int value)
        {
            var margins = new Margins
            {
                Top = value
            };
            Assert.Equal(value, margins.Top);

            // Set same.
            margins.Top = value;
            Assert.Equal(value, margins.Top);
        }

        [Fact]
        public void Top_SetNegative_ThrowsArgumentOutOfRangeException()
        {
            var margins = new Margins();
            AssertExtensions.Throws<ArgumentOutOfRangeException, ArgumentException>("value", null, () => margins.Top = -1);
        }

        [Theory]
        [MemberData(nameof(Bounds_Set_TestData))]
        public void Bottom_Set_GetReturnsExpected(int value)
        {
            var margins = new Margins
            {
                Bottom = value
            };
            Assert.Equal(value, margins.Bottom);

            // Set same.
            margins.Bottom = value;
            Assert.Equal(value, margins.Bottom);
        }

        [Fact]
        public void Bottom_SetNegative_ThrowsArgumentOutOfRangeException()
        {
            var margins = new Margins();
            AssertExtensions.Throws<ArgumentOutOfRangeException, ArgumentException>("value", null, () => margins.Bottom = -1);
        }
    }
}
