// Licensed to the .NET Foundation under one or more agreements.
// See the LICENSE file in the project root for more information.
//
// System.Configuration.KeyValueConfigurationElementTest.cs - Unit tests
// for System.Configuration.KeyValueConfigurationElement.
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
using System.ComponentModel;
using Xunit;

namespace MonoTests.System.Configuration
{
    public class KeyValueConfigurationElementTest
    {
        class Poker : KeyValueConfigurationElement
        {
            public Poker(string name, string value)
                : base(name, value)
            {
            }

            protected override void Init()
            {
                Console.WriteLine(Environment.StackTrace);
                base.Init();
            }

            public ConfigurationPropertyCollection GetProperties()
            {
                return base.Properties;
            }
        }

        [Fact]
        // [NUnit.Framework.Category("NotWorking")]
        public void Properties()
        {
            Poker p = new Poker("name", "value");
            ConfigurationPropertyCollection props = p.GetProperties();

            Assert.NotNull(props);
            Assert.Equal(2, props.Count);

            ConfigurationProperty prop;

            prop = props["key"];
            Assert.Equal("key", prop.Name);
            Assert.Null(prop.Description);
            Assert.Equal(typeof(string), prop.Type);
            Assert.Equal(typeof(StringConverter), prop.Converter.GetType());
            Assert.NotNull(prop.Validator);
            Assert.Equal(typeof(DefaultValidator), prop.Validator.GetType());
            Assert.Equal("", prop.DefaultValue);
            Assert.True(prop.IsKey, "A9");
            Assert.True(prop.IsRequired, "A10");

            Assert.False(prop.IsDefaultCollection, "A11");

            prop = props["value"];
            Assert.Equal("value", prop.Name);
            Assert.Null(prop.Description);
            Assert.Equal(typeof(string), prop.Type);
            Assert.Equal(typeof(StringConverter), prop.Converter.GetType());
            Assert.Equal(typeof(DefaultValidator), prop.Validator.GetType());
            Assert.Equal("", prop.DefaultValue);
            Assert.False(prop.IsKey, "A18");
            Assert.False(prop.IsRequired, "A19");

            Assert.False(prop.IsDefaultCollection, "A20");
        }
    }
}

