// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Xml.Linq;
using Xunit;

namespace CoreXml.Test.XLinq
{
    public partial class ImplicitConversionsElem
    {
        // these tests are duplication of the 
        // valid cases
        //   - direct value
        //   - concat value
        //          - concat of text
        //          - text & CData
        //          - concat from children
        //   - removing, adding and modifying nodes sanity

        // invalid cases (sanity, pri2)

        [Theory]
        [InlineData("<A>text</A>", "text")]
        [InlineData("<A>text1<B/>text2</A>", "text1text2")]
        [InlineData("<A>text1<B/><![CDATA[text2]]></A>", "text1text2")]
        [InlineData("<A><B>text1<D><![CDATA[text2]]></D></B><C>text3</C></A>", "text1text2text3")]
        [InlineData("<A><B><D></D></B><C></C></A>", "")]
        [InlineData("<A><B><D><![CDATA[]]></D></B><C></C></A>", "")]
        public void StringConvert(string xml, string expected)
        {
            XElement elem = XElement.Parse(xml);
            Assert.Equal(expected, (string)elem);
        }

        [Theory]
        [InlineData("<A>true</A>", true)]
        [InlineData("<A>1</A>", true)]
        [InlineData("<A>false</A>", false)]
        [InlineData("<A>0</A>", false)]
        [InlineData("<A>tr<B/>ue</A>", true)]
        [InlineData("<A>f<B/>alse</A>", false)]
        [InlineData("<A>tru<B/><![CDATA[e]]></A>", true)]
        [InlineData("<A>fal<B/><![CDATA[se]]></A>", false)]
        [InlineData("<A><B>t<D><![CDATA[ru]]></D></B><C>e</C></A>", true)]
        [InlineData("<A> <B><D><![CDATA[1]]></D></B><C> </C></A>", true)]
        [InlineData("<A><B>fa<D><![CDATA[l]]></D></B><C>se</C></A>", false)]
        [InlineData("<A><B> <D><![CDATA[ ]]></D></B><C>0</C></A>", false)]
        public void BoolConvert(string xml, bool expected)
        {
            XElement elem = XElement.Parse(xml);
            Assert.Equal(expected, (bool)elem);
        }

        [Theory]
        [InlineData("<A></A>")]
        [InlineData("<A>2</A>")]
        public void BoolConvertInvalid(string xml)
        {
            XElement elem = XElement.Parse(xml);
            Assert.Throws<FormatException>(() => (bool)elem);
        }

        [Theory]
        [InlineData("<A>true</A>", true)]
        [InlineData("<A>1</A>", true)]
        [InlineData("<A>false</A>", false)]
        [InlineData("<A>0</A>", false)]
        [InlineData("<A>tr<B/>ue</A>", true)]
        [InlineData("<A>f<B/>alse</A>", false)]
        [InlineData("<A>tru<B/><![CDATA[e]]></A>", true)]
        [InlineData("<A>fal<B/><![CDATA[se]]></A>", false)]
        [InlineData("<A><B>t<D><![CDATA[ru]]></D></B><C>e</C></A>", true)]
        [InlineData("<A> <B><D><![CDATA[1]]></D></B><C> </C></A>", true)]
        [InlineData("<A><B>fa<D><![CDATA[l]]></D></B><C>se</C></A>", false)]
        [InlineData("<A><B> <D><![CDATA[ ]]></D></B><C>0</C></A>", false)]
        public void BoolQConvert(string xml, bool? expected)
        {
            XElement elem = XElement.Parse(xml);
            Assert.Equal(expected, (bool?)elem);
        }

        [Theory]
        [InlineData("<A a='1'></A>")]
        [InlineData("<A a='1'>tr<B/> ue</A>")]
        [InlineData("<A a='1'>2</A>")]
        public void BoolQConvertInvalid(string xml)
        {
            XElement elem = XElement.Parse(xml);
            Assert.Throws<FormatException>(() => (bool?)elem);
        }

        [Fact]
        public void BoolQConvertNull()
        {
            XElement elem = null;
            Assert.Null((bool?)elem);
        }

        [Theory]
        [InlineData("<A a='1'>10</A>", 10)]
        [InlineData("<A a='1'>1<B/>7</A>", 17)]
        [InlineData("<A a='1'>-<B/><![CDATA[21]]></A>", -21)]
        [InlineData("<A a='1'><B>-<D><![CDATA[12]]></D></B><C>0</C></A>", -120)]
        public void IntConvert(string xml, int expected)
        {
            XElement elem = XElement.Parse(xml);
            Assert.Equal(expected, (int)elem);
        }

        [Theory]
        [InlineData("<A a='1'></A>")]
        [InlineData("<A a='1'>2<B/> 1</A>")]
        [InlineData("<A a='1'>X</A>")]
        public void IntConvertInvalid(string xml)
        {
            XElement elem = XElement.Parse(xml);
            Assert.Throws<FormatException>(() => (int)elem);
        }

        [Theory]
        [InlineData("<A a='1'>10</A>", 10)]
        [InlineData("<A a='1'>1<B/>7</A>", 17)]
        [InlineData("<A a='1'>-<B/><![CDATA[21]]></A>", -21)]
        [InlineData("<A a='1'><B>-<D><![CDATA[12]]></D></B><C>0</C></A>", -120)]
        public void IntQConvert(string xml, int? expected)
        {
            XElement elem = XElement.Parse(xml);
            Assert.Equal(expected, (int?)elem);
        }

        [Theory]
        [InlineData("<A a='1'></A>")]
        [InlineData("<A a='1'>2<B/> 1</A>")]
        [InlineData("<A a='1'>X</A>")]
        public void IntQConvertInvalid(string xml)
        {
            XElement elem = XElement.Parse(xml);
            Assert.Throws<FormatException>(() => (int?)elem);
        }

        [Fact]
        public void IntQConvertNull()
        {
            XElement elem = null;
            Assert.Null((int?)elem);
        }

        [Theory]
        [InlineData("<A a='1'>10</A>", 10)]
        [InlineData("<A a='1'>1<B/>7</A>", 17)]
        [InlineData("<A a='1'><B/><![CDATA[21]]></A>", 21)]
        [InlineData("<A a='1'><B><D><![CDATA[12]]></D></B><C>0</C></A>", 120)]
        public void UIntConvert(string xml, uint expected)
        {
            XElement elem = XElement.Parse(xml);
            Assert.Equal(expected, (uint)elem);
        }

        [Theory]
        [InlineData("<A a='1'></A>")]
        [InlineData("<A a='1'>2<B/> 1</A>")]
        [InlineData("<A a='1'>-<B/>1</A>")]
        [InlineData("<A a='1'>X</A>")]
        public void UIntConvertInvalid(string xml)
        {
            XElement elem = XElement.Parse(xml);
            Assert.Throws<FormatException>(() => (uint)elem);
        }

        [Theory]
        [InlineData("<A a='1'>10</A>", 10u)]
        [InlineData("<A a='1'>1<B/>7</A>", 17u)]
        [InlineData("<A a='1'><B/><![CDATA[21]]></A>", 21u)]
        [InlineData("<A a='1'><B><D><![CDATA[12]]></D></B><C>0</C></A>", 120u)]
        public void UIntQConvert(string xml, uint? expected)
        {
            XElement elem = XElement.Parse(xml);
            Assert.Equal(expected, (uint?)elem);
        }

        [Theory]
        [InlineData("<A a='1'></A>")]
        [InlineData("<A a='1'>2<B/> 1</A>")]
        [InlineData("<A a='1'>-<B/>1</A>")]
        [InlineData("<A a='1'>X</A>")]
        public void UIntQConvertInvalid(string xml)
        {
            XElement elem = XElement.Parse(xml);
            Assert.Throws<FormatException>(() => (uint?)elem);
        }

        [Fact]
        public void UIntQConvertNull()
        {
            XElement elem = null;
            Assert.Null((uint?)elem);
        }
    }
}
