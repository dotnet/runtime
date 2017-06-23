// Licensed to the .NET Foundation under one or more agreements.
// See the LICENSE file in the project root for more information.
//
// System.Configuration.InfiniteTimeSpanConverterTest.cs - Unit tests
// for System.Configuration.InfiniteTimeSpanConverter.
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
    public class InfiniteTimeSpanConverterTest
    {
        [Fact]
        public void CanConvertFrom()
        {
            InfiniteTimeSpanConverter cv = new InfiniteTimeSpanConverter();

            Assert.True(cv.CanConvertFrom(null, typeof(string)), "A1");
            Assert.False(cv.CanConvertFrom(null, typeof(TimeSpan)), "A2");
            Assert.False(cv.CanConvertFrom(null, typeof(int)), "A3");
            Assert.False(cv.CanConvertFrom(null, typeof(object)), "A4");
        }

        [Fact]
        public void CanConvertTo()
        {
            InfiniteTimeSpanConverter cv = new InfiniteTimeSpanConverter();

            Assert.True(cv.CanConvertTo(null, typeof(string)), "A1");
            Assert.False(cv.CanConvertTo(null, typeof(TimeSpan)), "A2");
            Assert.False(cv.CanConvertTo(null, typeof(int)), "A3");
            Assert.False(cv.CanConvertTo(null, typeof(object)), "A4");
        }

        [Fact]
        public void ConvertFrom()
        {
            InfiniteTimeSpanConverter cv = new InfiniteTimeSpanConverter();
            object o;

            o = cv.ConvertFrom(null, null, "00:00:59");
            Assert.Equal(typeof(TimeSpan), o.GetType());
            Assert.Equal(TimeSpan.FromSeconds(59), o);

            /* and now test infinity */
            o = cv.ConvertFrom(null, null, "Infinite");
            Assert.Equal(TimeSpan.MaxValue, o);
        }

        [Fact]
        public void ConvertFrom_TypeError()
        {
            InfiniteTimeSpanConverter cv = new InfiniteTimeSpanConverter();
            object o = null;

            Assert.Throws<InvalidCastException>(() => o = cv.ConvertFrom(null, null, 59));
            Assert.Null(o);
        }

        [Fact]
        public void ConvertTo()
        {
            InfiniteTimeSpanConverter cv = new InfiniteTimeSpanConverter();

            Assert.Equal("00:00:59", cv.ConvertTo(null, null, TimeSpan.FromSeconds(59), typeof(string)));

            Assert.Equal("00:02:24", cv.ConvertTo(null, null, TimeSpan.FromSeconds(144), typeof(string)));

            /* infinity tests */
            Assert.Equal("Infinite", cv.ConvertTo(null, null, TimeSpan.MaxValue, typeof(string)));
            Assert.Equal("10675199.02:48:04.4775807", cv.ConvertTo(null, null, TimeSpan.MaxValue - TimeSpan.FromSeconds(1), typeof(string)));
        }

        [Fact]
        public void ConvertTo_NullError()
        {
            InfiniteTimeSpanConverter cv = new InfiniteTimeSpanConverter();

            Assert.Throws<NullReferenceException>(() => cv.ConvertTo(null, null, null, typeof(string)));
        }

        [Fact]
        public void ConvertTo_TypeError1()
        {
            InfiniteTimeSpanConverter cv = new InfiniteTimeSpanConverter();

            AssertExtensions.Throws<ArgumentException>(null, () => cv.ConvertTo(null, null, "hi", typeof(string)));
        }

        [Fact]
        public void ConvertTo_TypeError2()
        {
            InfiniteTimeSpanConverter cv = new InfiniteTimeSpanConverter();

            AssertExtensions.Throws<ArgumentException>(null, () => cv.ConvertTo(null, null, 59, typeof(int)));
        }
    }
}

