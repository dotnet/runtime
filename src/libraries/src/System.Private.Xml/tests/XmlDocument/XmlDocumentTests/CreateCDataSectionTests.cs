// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Xml.Tests
{
    public class CreateCDataSectionTests
    {
        [Fact]
        public static void CreateCDataTest1()
        {
            var xmlDocument = new XmlDocument();
            var cdata = xmlDocument.CreateCDataSection(string.Empty);

            Assert.Equal("<![CDATA[]]>", cdata.OuterXml);
            Assert.Equal(string.Empty, cdata.InnerXml);
            Assert.Equal(string.Empty, cdata.InnerText);
            Assert.Equal(XmlNodeType.CDATA, cdata.NodeType);
        }

        [Fact]
        public static void CreateCDataTest2()
        {
            var xmlDocument = new XmlDocument();
            var cdata = xmlDocument.CreateCDataSection("test data");

            Assert.Equal("<![CDATA[test data]]>", cdata.OuterXml);
            Assert.Equal(string.Empty, cdata.InnerXml);
            Assert.Equal("test data", cdata.InnerText);
            Assert.Equal(XmlNodeType.CDATA, cdata.NodeType);
        }

        [Fact]
        public static void CreateCDataTest3()
        {
            var xmlDocument = new XmlDocument();
            var cdata = xmlDocument.CreateCDataSection("]]>");

            AssertExtensions.Throws<ArgumentException>(null, () => { var test = cdata.OuterXml; });
            Assert.Equal(string.Empty, cdata.InnerXml);
            Assert.Equal("]]>", cdata.InnerText);
            Assert.Equal(XmlNodeType.CDATA, cdata.NodeType);
        }

        [Fact]
        public static void CreateCDataTest4()
        {
            var xmlDocument = new XmlDocument();
            var cdata = xmlDocument.CreateCDataSection("=\"&<> : ;; @#$%^&*()\"~\"' ");

            Assert.Equal("<![CDATA[=\"&<> : ;; @#$%^&*()\"~\"' ]]>", cdata.OuterXml);
            Assert.Equal(string.Empty, cdata.InnerXml);
            Assert.Equal("=\"&<> : ;; @#$%^&*()\"~\"' ", cdata.InnerText);
            Assert.Equal(XmlNodeType.CDATA, cdata.NodeType);
        }

        [Fact]
        public static void CreateCDataTest5()
        {
            var xmlDocument = new XmlDocument();
            var text = "X    X";
            var cdata = xmlDocument.CreateCDataSection(text);

            Assert.Equal("<![CDATA[" + text + "]]>", cdata.OuterXml);
            Assert.Equal(string.Empty, cdata.InnerXml);
            Assert.Equal(text, cdata.InnerText);
            Assert.Equal(XmlNodeType.CDATA, cdata.NodeType);
        }
    }
}
