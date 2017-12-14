// Licensed to the .NET Foundation under one or more agreements.
// See the LICENSE file in the project root for more information.
//
// System.Configuration.GenericEnumConverterTest.cs - Unit tests
// for System.Configuration.GenericEnumConverter.
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
    public class GenericEnumConverterTest
    {
        enum FooEnum
        {
            Foo = 1,
            Bar = 2
        }

        [Fact]
        public void Ctor_Null()
        {
            Assert.Throws<ArgumentNullException>(() => new GenericEnumConverter(null));
        }

        [Fact]
        public void Ctor_TypeError()
        {
            GenericEnumConverter cv = new GenericEnumConverter(typeof(object));
        }

        [Fact]
        public void CanConvertFrom()
        {
            GenericEnumConverter cv = new GenericEnumConverter(typeof(FooEnum));

            Assert.True(cv.CanConvertFrom(null, typeof(string)), "A1");
            Assert.False(cv.CanConvertFrom(null, typeof(TimeSpan)), "A2");
            Assert.False(cv.CanConvertFrom(null, typeof(int)), "A3");
            Assert.False(cv.CanConvertFrom(null, typeof(object)), "A4");
        }

        [Fact]
        public void CanConvertTo()
        {
            GenericEnumConverter cv = new GenericEnumConverter(typeof(FooEnum));

            Assert.True(cv.CanConvertTo(null, typeof(string)), "A1");
            Assert.False(cv.CanConvertTo(null, typeof(TimeSpan)), "A2");
            Assert.False(cv.CanConvertTo(null, typeof(int)), "A3");
            Assert.False(cv.CanConvertTo(null, typeof(object)), "A4");
        }

        [Fact]
        public void ConvertFrom()
        {
            GenericEnumConverter cv = new GenericEnumConverter(typeof(FooEnum));
            Assert.Equal(FooEnum.Foo, cv.ConvertFrom(null, null, "Foo"));
            Assert.Equal(FooEnum.Bar, cv.ConvertFrom(null, null, "Bar"));
        }

        [Fact]
        public void ConvertFrom_Case()
        {
            GenericEnumConverter cv = new GenericEnumConverter(typeof(FooEnum));
            AssertExtensions.Throws<ArgumentException>(null, () => cv.ConvertFrom(null, null, "foo"));
        }

        [Fact]
        public void ConvertFrom_InvalidString()
        {
            GenericEnumConverter cv = new GenericEnumConverter(typeof(FooEnum));
            object o = null;

            AssertExtensions.Throws<ArgumentException>(null, () => o = cv.ConvertFrom(null, null, "baz"));
            Assert.Null(o);
        }

        [Fact]
        public void ConvertFrom_InvalidString_WhiteSpaceAtTheBeginning()
        {
            GenericEnumConverter cv = new GenericEnumConverter(typeof(FooEnum));
            object o = null;

            AssertExtensions.Throws<ArgumentException>(null, () => o = cv.ConvertFrom(null, null, " Foo"));
            Assert.Null(o);
        }

        [Fact]
        public void ConvertFrom_InvalidString_WhiteSpaceAtTheEnd()
        {
            GenericEnumConverter cv = new GenericEnumConverter(typeof(FooEnum));
            object o = null;

            AssertExtensions.Throws<ArgumentException>(null, () => o = cv.ConvertFrom(null, null, "Foo "));
            Assert.Null(o);
        }

        [Fact]
        public void ConvertFrom_InvalidString_DigitAtTheBeginning()
        {
            GenericEnumConverter cv = new GenericEnumConverter(typeof(FooEnum));
            object o = null;

            AssertExtensions.Throws<ArgumentException>(null, () => o = cv.ConvertFrom(null, null, "1Foo"));
            Assert.Null(o);
        }

        [Fact]
        public void ConvertFrom_InvalidString_PlusSignAtTheBeginning()
        {
            GenericEnumConverter cv = new GenericEnumConverter(typeof(FooEnum));
            object o = null;

            AssertExtensions.Throws<ArgumentException>(null, () => o = cv.ConvertFrom(null, null, "+Foo"));
            Assert.Null(o);
        }

        [Fact]
        public void ConvertFrom_InvalidString_MinusSignAtTheBeginning()
        {
            GenericEnumConverter cv = new GenericEnumConverter(typeof(FooEnum));
            object o = null;

            AssertExtensions.Throws<ArgumentException>(null, () => o = cv.ConvertFrom(null, null, "-Foo"));
            Assert.Null(o);
        }

        [Fact]
        public void ConvertFrom_Null()
        {
            GenericEnumConverter cv = new GenericEnumConverter(typeof(FooEnum));
            object o = null;

            AssertExtensions.Throws<ArgumentException>(null, () => o = cv.ConvertFrom(null, null, null));
            Assert.Null(o);
        }

        [Fact]
        public void ConvertFrom_EmptyString()
        {
            GenericEnumConverter cv = new GenericEnumConverter(typeof(FooEnum));
            object o = null;

            AssertExtensions.Throws<ArgumentException>(null, () => o = cv.ConvertFrom(null, null, string.Empty));
            Assert.Null(o);
        }

        [Fact]
        public void ConvertTo()
        {
            GenericEnumConverter cv = new GenericEnumConverter(typeof(FooEnum));

            Assert.Equal("Foo", cv.ConvertTo(null, null, FooEnum.Foo, typeof(string)));
            Assert.Equal("Bar", cv.ConvertTo(null, null, FooEnum.Bar, typeof(string)));
        }

        [Fact]
        public void ConvertTo_NullError()
        {
            GenericEnumConverter cv = new GenericEnumConverter(typeof(FooEnum));

            Assert.Throws<NullReferenceException>(() => cv.ConvertTo(null, null, null, typeof(string)));
        }

        [Fact]
        public void ConvertTo_TypeError1()
        {
            GenericEnumConverter cv = new GenericEnumConverter(typeof(FooEnum));

            Assert.Equal("0", cv.ConvertTo(null, null, 0, typeof(string)));
        }

        [Fact]
        public void ConvertTo_TypeError2()
        {
            GenericEnumConverter cv = new GenericEnumConverter(typeof(FooEnum));

            Assert.Equal("", cv.ConvertTo(null, null, "", typeof(string)));
        }
    }
}

