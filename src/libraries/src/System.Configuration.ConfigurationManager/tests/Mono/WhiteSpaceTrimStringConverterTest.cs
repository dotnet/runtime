// Licensed to the .NET Foundation under one or more agreements.
// See the LICENSE file in the project root for more information.
//
// System.Configuration.WhiteSpaceTrimStringConverterTest.cs - Unit tests
// for System.Configuration.WhiteSpaceTrimStringConverter.
//
// Author:
//	Chris Toshok  <toshok@ximian.com>
//
// Copyright (C) 2005 Novell, Inc (http://www.novell.com)
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

using System;
using System.Configuration;
using Xunit;

namespace MonoTests.System.Configuration
{
    public class WhiteSpaceTrimStringConverterTest
    {
        [Fact]
        public void CanConvertFrom()
        {
            WhiteSpaceTrimStringConverter cv = new WhiteSpaceTrimStringConverter();

            Assert.True(cv.CanConvertFrom(null, typeof(string)), "A1");
            Assert.False(cv.CanConvertFrom(null, typeof(TimeSpan)), "A2");
            Assert.False(cv.CanConvertFrom(null, typeof(int)), "A3");
            Assert.False(cv.CanConvertFrom(null, typeof(object)), "A4");
        }

        [Fact]
        public void CanConvertTo()
        {
            WhiteSpaceTrimStringConverter cv = new WhiteSpaceTrimStringConverter();

            Assert.True(cv.CanConvertTo(null, typeof(string)), "A1");
            Assert.False(cv.CanConvertTo(null, typeof(TimeSpan)), "A2");
            Assert.False(cv.CanConvertTo(null, typeof(int)), "A3");
            Assert.False(cv.CanConvertTo(null, typeof(object)), "A4");
        }

        [Fact]
        public void ConvertFrom()
        {
            WhiteSpaceTrimStringConverter cv = new WhiteSpaceTrimStringConverter();
            object o;

            o = cv.ConvertFrom(null, null, "hi there");
            Assert.Equal(typeof(string), o.GetType());
            Assert.Equal("hi there", (string)o);
            o = cv.ConvertFrom(null, null, " hi there ");
            Assert.Equal("hi there", (string)o);
        }

        [Fact]
        public void ConvertFrom_TypeError()
        {
            WhiteSpaceTrimStringConverter cv = new WhiteSpaceTrimStringConverter();
            object o = null;

            Assert.Throws<InvalidCastException>(() => o = cv.ConvertFrom(null, null, 59));
            Assert.Null(o);
        }

        [Fact]
        public void ConvertTo()
        {
            WhiteSpaceTrimStringConverter cv = new WhiteSpaceTrimStringConverter();

            Assert.Equal("59", cv.ConvertTo(null, null, "59", typeof(string)));
            Assert.Equal("59", cv.ConvertTo(null, null, " 59", typeof(string)));
            Assert.Equal("59", cv.ConvertTo(null, null, "59 ", typeof(string)));
            Assert.Equal("59", cv.ConvertTo(null, null, " 59 ", typeof(string)));
        }

        [Fact]
        public void ConvertTo_NullError()
        {
            WhiteSpaceTrimStringConverter cv = new WhiteSpaceTrimStringConverter();

            Assert.Equal("", cv.ConvertTo(null, null, null, typeof(string)));
        }

        [Fact]
        public void ConvertTo_TypeError1()
        {
            WhiteSpaceTrimStringConverter cv = new WhiteSpaceTrimStringConverter();

            AssertExtensions.Throws<ArgumentException>(null, () => cv.ConvertTo(null, null, 59, typeof(string)));
        }
    }
}

