// Licensed to the .NET Foundation under one or more agreements.
// See the LICENSE file in the project root for more information.
//
// System.Configuration.IntegerValidatorTest.cs - Unit tests
// for System.Configuration.IntegerValidator.
//
// Author:
//	Chris Toshok  <toshok@ximian.com>
//	Andres G. Aragoneses  <andres@7digital.com>
//
// Copyright (C) 2005 Novell, Inc (http://www.novell.com)
// Copyright (C) 2012 7digital Media, Ltd (http://www.7digital.com)
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
using System.Xml;
using System.IO;
using System.Configuration;
using Xunit;

namespace MonoTests.System.Configuration
{
    public class IntegerValidatorTest
    {
        [Fact]
        public void CanValidate()
        {
            IntegerValidator v = new IntegerValidator(1, 1);

            Assert.False(v.CanValidate(typeof(TimeSpan)), "A1");
            Assert.True(v.CanValidate(typeof(int)), "A2");
            Assert.False(v.CanValidate(typeof(long)), "A3");
        }

        [Fact]
        public void Validate_inRange()
        {
            IntegerValidator v = new IntegerValidator(5000, 10000);
            v.Validate(7000);
        }

        [Fact]
        public void Validate_Inclusive()
        {
            IntegerValidator v = new IntegerValidator(5000, 10000, false);
            v.Validate(5000);
            v.Validate(10000);
        }

        [Fact]
        public void Validate_Exclusive()
        {
            IntegerValidator v = new IntegerValidator(5000, 10000, true);
            v.Validate(1000);
            v.Validate(15000);
        }

        [Fact]
        public void Validate_Exclusive_fail1()
        {
            IntegerValidator v = new IntegerValidator(5000, 10000, true);
            AssertExtensions.Throws<ArgumentException>(null, () => v.Validate(5000));
        }

        [Fact]
        public void Validate_Exclusive_fail2()
        {
            IntegerValidator v = new IntegerValidator(5000, 10000, true);
            AssertExtensions.Throws<ArgumentException>(null, () => v.Validate(10000));
        }

        [Fact]
        public void Validate_Exclusive_fail3()
        {
            IntegerValidator v = new IntegerValidator(5000, 10000, true);
            AssertExtensions.Throws<ArgumentException>(null, () => v.Validate(7000));
        }

        [Fact]
        public void Validate_Resolution()
        {
            IntegerValidator v = new IntegerValidator(20000,
                                   50000,
                                   false,
                                   3000);

            AssertExtensions.Throws<ArgumentException>(null, () => v.Validate(40000));
        }

        #region BNC654721 https://bugzilla.novell.com/show_bug.cgi?id=654721
        public sealed class TestSection : ConfigurationSection
        {
            public void Load(string xml)
            {
                Init();
                using (var sr = new StringReader(xml))
                using (var reader = new XmlTextReader(sr))
                {
                    DeserializeSection(reader);
                }
            }

            [ConfigurationProperty("integerValidatorMinValue")]
            public IntegerValidatorMinValueChildElement IntegerValidatorMinValue
            {
                get { return (IntegerValidatorMinValueChildElement)base["integerValidatorMinValue"]; }
                set { base["integerValidatorMinValue"] = value; }
            }

            [ConfigurationProperty("integerValidatorMaxValue")]
            public IntegerValidatorMaxValueChildElement IntegerValidatorMaxValue
            {
                get { return (IntegerValidatorMaxValueChildElement)base["integerValidatorMaxValue"]; }
                set { base["integerValidatorMaxValue"] = value; }
            }
        }

        public sealed class IntegerValidatorMaxValueChildElement : ConfigurationElement
        {
            [ConfigurationProperty("theProperty"), IntegerValidator(MaxValue = 100)]
            public int TheProperty
            {
                get { return (int)base["theProperty"]; }
                set { base["theProperty"] = value; }
            }
        }

        public sealed class IntegerValidatorMinValueChildElement : ConfigurationElement
        {
            [ConfigurationProperty("theProperty"), IntegerValidator(MinValue = 0)]
            public int TheProperty
            {
                get { return (int)base["theProperty"]; }
                set { base["theProperty"] = value; }
            }
        }

        [Fact]
        public void IntegerValidatorMinValueTest()
        {
            var section = new TestSection();
            section.Load(@"<someSection><integerValidatorMinValue theProperty=""15"" /></someSection>");
            Assert.Equal(15, section.IntegerValidatorMinValue.TheProperty);
        }

        [Fact]
        public void IntegerValidatorMaxValueTest()
        {
            var section = new TestSection();
            section.Load(@"<someSection><integerValidatorMaxValue theProperty=""25"" /></someSection>");
            Assert.Equal(25, section.IntegerValidatorMaxValue.TheProperty);
        }
        #endregion
    }
}

