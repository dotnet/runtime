// Licensed to the .NET Foundation under one or more agreements.
// See the LICENSE file in the project root for more information.
//
// System.Configuration.ConfigurationElementTest.cs - Unit tests
// for System.Configuration.ConfigurationElement.
//
// Author:
//	Konstantin Triger <kostat@mainsoft.com>
//
// Copyright (C) 2006 Mainsoft, Inc (http://www.mainsoft.com)
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
    public class ConfigurationPropertyTest
    {
        [Fact]
        public void CostructorTest()
        {
            Assert.Throws<ConfigurationErrorsException>(() => new ConfigurationProperty("Name", typeof(char), 5));
        }

        [Fact]
        public void CostructorTest1()
        {
            ConfigurationProperty poker = new ConfigurationProperty("Name", typeof(string));
            Assert.NotNull(poker.Validator);
            Assert.NotNull(poker.Converter);
        }

        [Fact]
        public void DefaultValueTest()
        {
            ConfigurationProperty poker = new ConfigurationProperty("Name", typeof(char));
            Assert.Equal(typeof(char), poker.DefaultValue.GetType());

            ConfigurationProperty poker1 = new ConfigurationProperty("Name", typeof(ConfigurationProperty));
            Assert.Equal(null, poker1.DefaultValue);

            ConfigurationProperty poker2 = new ConfigurationProperty("Name", typeof(string));
            Assert.Equal(string.Empty, poker2.DefaultValue);
        }
    }
}

