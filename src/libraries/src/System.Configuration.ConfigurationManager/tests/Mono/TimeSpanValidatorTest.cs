// Licensed to the .NET Foundation under one or more agreements.
// See the LICENSE file in the project root for more information.
//
// System.Configuration.TimeSpanValidatorTest.cs - Unit tests
// for System.Configuration.TimeSpanValidator.
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
    public class TimeSpanValidatorTest
    {
        [Fact]
        public void CanValidate()
        {
            TimeSpan t = new TimeSpan(1000);
            TimeSpanValidator v = new TimeSpanValidator(t, t);

            Assert.True(v.CanValidate(typeof(TimeSpan)), "A1");
            Assert.False(v.CanValidate(typeof(int)), "A2");
            Assert.False(v.CanValidate(typeof(long)), "A3");
        }

        [Fact]
        public void Validate_inRange()
        {
            TimeSpanValidator v = new TimeSpanValidator(new TimeSpan(5000), new TimeSpan(10000));
            v.Validate(new TimeSpan(7000));
        }

        [Fact]
        public void Validate_Inclusive()
        {
            TimeSpanValidator v = new TimeSpanValidator(new TimeSpan(5000), new TimeSpan(10000), false);
            v.Validate(new TimeSpan(5000));
            v.Validate(new TimeSpan(10000));
        }

        [Fact]
        public void Validate_Exclusive()
        {
            TimeSpanValidator v = new TimeSpanValidator(new TimeSpan(5000), new TimeSpan(10000), true);
            v.Validate(new TimeSpan(1000));
            v.Validate(new TimeSpan(15000));
        }

        [Fact]
        public void Validate_Exclusive_fail1()
        {
            TimeSpanValidator v = new TimeSpanValidator(new TimeSpan(5000), new TimeSpan(10000), true);
            AssertExtensions.Throws<ArgumentException>(null, () => v.Validate(new TimeSpan(5000)));
        }

        [Fact]
        public void Validate_Exclusive_fail2()
        {
            TimeSpanValidator v = new TimeSpanValidator(new TimeSpan(5000), new TimeSpan(10000), true);
            AssertExtensions.Throws<ArgumentException>(null, () => v.Validate(new TimeSpan(10000)));
        }

        [Fact]
        public void Validate_Exclusive_fail3()
        {
            TimeSpanValidator v = new TimeSpanValidator(new TimeSpan(5000), new TimeSpan(10000), true);
            AssertExtensions.Throws<ArgumentException>(null, () => v.Validate(new TimeSpan(7000)));
        }

        [Fact]
        public void Validate_Resolution()
        {
            TimeSpanValidator v = new TimeSpanValidator(new TimeSpan(20000),
                                     new TimeSpan(50000),
                                     false,
                                     2);

            AssertExtensions.Throws<ArgumentException>(null, () => v.Validate(TimeSpan.FromTicks(40000)));
        }
    }
}

