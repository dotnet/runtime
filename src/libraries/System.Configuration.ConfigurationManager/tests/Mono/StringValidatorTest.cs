// Licensed to the .NET Foundation under one or more agreements.
// See the LICENSE file in the project root for more information.
//
// System.Configuration.StringValidatorTest.cs - Unit tests
// for System.Configuration.StringValidator.
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
    public class StringValidatorTest
    {
        [Fact]
        public void CanValidate()
        {
            StringValidator v = new StringValidator(5);

            Assert.True(v.CanValidate(typeof(string)));
            Assert.False(v.CanValidate(typeof(int)));
            Assert.False(v.CanValidate(typeof(object)));
        }

        [Fact]
        public void NullZero()
        {
            StringValidator v = new StringValidator(0);

            v.Validate(null);
        }

        [Fact]
        public void Null()
        {
            StringValidator v = new StringValidator(1);

            AssertExtensions.Throws<ArgumentException>(null, () => v.Validate(null));
        }

        [Fact]
        public void MinLenth()
        {
            StringValidator v = new StringValidator(5);

            v.Validate("123456789");
            v.Validate("1234567");
            v.Validate("12345");
        }

        [Fact]
        public void MinLength_fail()
        {
            StringValidator v = new StringValidator(5);

            AssertExtensions.Throws<ArgumentException>(null, () => v.Validate("1234"));
        }

        [Fact]
        public void Bounded()
        {
            StringValidator v = new StringValidator(5, 7);

            v.Validate("12345");
            v.Validate("123456");
            v.Validate("123457");
        }

        [Fact]
        public void Bounded_fail1()
        {
            StringValidator v = new StringValidator(5, 7);

            AssertExtensions.Throws<ArgumentException>(null, () => v.Validate("1234"));
        }

        [Fact]
        public void Bounded_fail2()
        {
            StringValidator v = new StringValidator(5, 7);

            AssertExtensions.Throws<ArgumentException>(null, () => v.Validate("12345678"));
        }

        [Fact]
        public void Invalid()
        {
            StringValidator v = new StringValidator(5, 7, "890");

            v.Validate("123456");
        }

        [Fact]
        public void Invalid_fail()
        {
            StringValidator v = new StringValidator(5, 7, "345");

            AssertExtensions.Throws<ArgumentException>(null, () => v.Validate("123456"));
        }

        [Fact]
        public void Invalid_fail_length()
        {
            StringValidator v = new StringValidator(5, 7, "890");

            AssertExtensions.Throws<ArgumentException>(null, () => v.Validate("12345678"));
        }
    }
}

