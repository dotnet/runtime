// Licensed to the .NET Foundation under one or more agreements.
// See the LICENSE file in the project root for more information.
//
// System.Configuration.CommaDelimitedStringCollectionConverterTest.cs - Unit tests
// for System.Configuration.CommaDelimitedStringCollectionConverter.
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
    public class CommaDelimitedStringCollectionConverterTest
    {
        [Fact]
        public void CanConvertFrom()
        {
            CommaDelimitedStringCollectionConverter cv = new CommaDelimitedStringCollectionConverter();

            Assert.True(cv.CanConvertFrom(null, typeof(string)), "A1");
            Assert.False(cv.CanConvertFrom(null, typeof(TimeSpan)), "A2");
            Assert.False(cv.CanConvertFrom(null, typeof(int)), "A3");
            Assert.False(cv.CanConvertFrom(null, typeof(object)), "A4");
        }

        [Fact]
        public void CanConvertTo()
        {
            CommaDelimitedStringCollectionConverter cv = new CommaDelimitedStringCollectionConverter();

            Assert.True(cv.CanConvertTo(null, typeof(string)), "A1");
            Assert.False(cv.CanConvertTo(null, typeof(TimeSpan)), "A2");
            Assert.False(cv.CanConvertTo(null, typeof(int)), "A3");
            Assert.False(cv.CanConvertTo(null, typeof(object)), "A4");
        }

        [Fact]
        public void ConvertFrom()
        {
            CommaDelimitedStringCollectionConverter cv = new CommaDelimitedStringCollectionConverter();
            object o;
            CommaDelimitedStringCollection col;

            o = cv.ConvertFrom(null, null, "hi,bye");
            Assert.Equal(typeof(CommaDelimitedStringCollection), o.GetType());

            col = (CommaDelimitedStringCollection)o;
            Assert.Equal(2, col.Count);
            Assert.Equal("hi", col[0]);
            Assert.Equal("bye", col[1]);

            col = (CommaDelimitedStringCollection)cv.ConvertFrom(null, null, "hi, bye");
            Assert.Equal(2, col.Count);
            Assert.Equal("hi", col[0]);
            Assert.Equal("bye", col[1]);
        }

        [Fact]
        public void ConvertFrom_TypeError()
        {
            CommaDelimitedStringCollectionConverter cv = new CommaDelimitedStringCollectionConverter();
            object o = null;

            Assert.Throws<InvalidCastException>(() => o = cv.ConvertFrom(null, null, 59));
            Assert.Null(o);
        }

        [Fact]
        public void ConvertTo()
        {
            CommaDelimitedStringCollectionConverter cv = new CommaDelimitedStringCollectionConverter();
            CommaDelimitedStringCollection col = new CommaDelimitedStringCollection();
            col.Add("hi");
            col.Add("bye");

            Assert.Equal("hi,bye", cv.ConvertTo(null, null, col, typeof(string)));
        }

        [Fact]
        public void ConvertTo_NullError()
        {
            CommaDelimitedStringCollectionConverter cv = new CommaDelimitedStringCollectionConverter();

            Assert.Equal(null, cv.ConvertTo(null, null, null, typeof(string)));
        }

        [Fact]
        public void ConvertTo_TypeError()
        {
            CommaDelimitedStringCollectionConverter cv = new CommaDelimitedStringCollectionConverter();

            AssertExtensions.Throws<ArgumentException>(null, () => cv.ConvertTo(null, null, 59, typeof(string)));
        }
    }
}

