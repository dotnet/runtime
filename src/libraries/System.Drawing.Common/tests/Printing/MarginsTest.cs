//
// System.Drawing.Printing.Margins unit tests
//
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
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
//

using Xunit;

namespace System.Drawing.Printing.Test
{
    public class MarginsTest
    {

        [Fact]
        public void CtorDefault()
        {
            Margins m = new Margins();
            Assert.Equal(100, m.Left);
            Assert.Equal(100, m.Top);
            Assert.Equal(100, m.Right);
            Assert.Equal(100, m.Bottom);
            Assert.Equal("[Margins Left=100 Right=100 Top=100 Bottom=100]", m.ToString());
            Margins clone = (Margins)m.Clone();
            Assert.Equal(m, clone);
            Assert.True(m == clone, "==");
            Assert.False(m != clone, "!=");
        }

        [Fact]
        public void Ctor4Int()
        {
            Margins m1 = new Margins(Int32.MaxValue, Int32.MaxValue, Int32.MaxValue, Int32.MaxValue);
            Assert.Equal(Int32.MaxValue, m1.Left);
            Assert.Equal(Int32.MaxValue, m1.Top);
            Assert.Equal(Int32.MaxValue, m1.Right);
            Assert.Equal(Int32.MaxValue, m1.Bottom);
            // right smaller than left
            Margins m2 = new Margins(Int32.MaxValue, 0, 10, 20);
            // bottom smaller than top
            Margins m3 = new Margins(10, 20, Int32.MaxValue, 0);
            Assert.False(m2.GetHashCode() == m3.GetHashCode(), "GetHashCode");
            Assert.True(m1 != m2, "m1 != m2");
            Assert.False(m1 == m2, "m1 == m2");
        }

        [ConditionalTheory(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotWindowsNanoServer))]
        [InlineData(-1,0,0,0)]
        [InlineData(0,-1,0,0)]
        [InlineData(0,0,-1,0)]
        [InlineData(0,0,0,-1)]
        public void Ctor_BadValues_ThrowsAnException(int left, int right, int top, int bottom)
        {
            Assert.Throws<ArgumentException>(() => new Margins(-1, 0, 0, 0));
        }

        [Fact]
        public void Equals()
        {
            Margins m = new Margins(1,2,3,4);
            Assert.True(m.Equals(new Margins(1,2,3,4)));
            Assert.True(m.Equals(m));
            Assert.False(m.Equals(null));
        }

        [Fact]
        public void OperatorsWithNulls()
        {
            Margins m1 = null;
            Margins m2 = null;
            Assert.True(m1 == m2, "null==null");
        }
    }
}
