// Licensed to the .NET Foundation under one or more agreements.
// See the LICENSE file in the project root for more information.
//
// Authors:
//	Sebastien Pouliot  <sebastien@ximian.com>
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
            Margins m = new Margins();
            Assert.Equal(100, m.Left);
            Assert.Equal(100, m.Top);
            Assert.Equal(100, m.Right);
            Assert.Equal(100, m.Bottom);
        }

        [Theory]
        [InlineData(int.MaxValue, int.MaxValue, int.MaxValue, int.MaxValue)]
        [InlineData(0, 1, 2, 3)]
        public void Ctor_Bounds(int left, int right, int top, int bottom)
        {
            Margins m = new Margins(left, right, top, bottom);
            Assert.Equal(left, m.Left);
            Assert.Equal(right, m.Right);
            Assert.Equal(top, m.Top);
            Assert.Equal(bottom, m.Bottom);
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [Theory]
        [InlineData(-1, 0, 0, 0)]
        [InlineData(0, -1, 0, 0)]
        [InlineData(0, 0, -1, 0)]
        [InlineData(0, 0, 0, -1)]
        public void Ctor_BoundsBadValues_ThrowsArgumentException(int left, int right, int top, int bottom)
        {
            AssertExtensions.Throws<ArgumentException>(null, () => new Margins(left, right, top, bottom));
        }

        public static IEnumerable<object[]> Equals_TestData()
        {
            yield return new object[] { new Margins(), null, false };
            yield return new object[] { new Margins(1, 2, 3, 4), new Margins(1, 2, 3, 4), true };
            yield return new object[] { new Margins(int.MaxValue, 2, 3, 4), new Margins(1, 2, 3, 4), false };
            yield return new object[] { new Margins(1, int.MaxValue, 3, 4), new Margins(1, 2, 3, 4), false };
            yield return new object[] { new Margins(1, 2, int.MaxValue, 4), new Margins(1, 2, 3, 4), false };
            yield return new object[] { new Margins(1, 2, 3, int.MaxValue), new Margins(1, 2, 3, 4), false };
        }

        [Theory]
        [MemberData(nameof(Equals_TestData))]
        public void Equals_Margin_Success(Margins margin1, Margins margin2, bool expectedResult)
        {
            Assert.Equal(expectedResult, margin1.Equals(margin2));
            Assert.Equal(expectedResult, margin1 == margin2);
            Assert.NotEqual(expectedResult, margin1 != margin2);
        }

        [Fact]
        public void ToString_Success()
        {
            Margins m = new Margins();
            Assert.Equal("[Margins Left=100 Right=100 Top=100 Bottom=100]", m.ToString());
        }

        [Fact]
        public void Clone_Margin_Success()
        {
            Margins m = new Margins(1, 2, 3, 4);
            Margins clone = (Margins)m.Clone();
            Assert.Equal(m, clone);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(10)]
        [InlineData(int.MaxValue)]
        public void Bounds_Values_ReturnsExpected(int boundValue)
        {
            Margins m = new Margins();
            m.Bottom = boundValue;
            m.Left = boundValue;
            m.Right = boundValue;
            m.Top = boundValue;

            Assert.Equal(boundValue, m.Bottom);
            Assert.Equal(boundValue, m.Left);
            Assert.Equal(boundValue, m.Right);
            Assert.Equal(boundValue, m.Top);
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [Fact]
        public void Bounds_BadValues_ThrowsArgumentException()
        {
            Margins m = new Margins();
            AssertExtensions.Throws<ArgumentException>(null, () => m.Bottom = -1);
            AssertExtensions.Throws<ArgumentException>(null, () => m.Left = -1);
            AssertExtensions.Throws<ArgumentException>(null, () => m.Right = -1);
            AssertExtensions.Throws<ArgumentException>(null, () => m.Top = -1);
        }
    }
}
