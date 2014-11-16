// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Xml;

namespace XmlDocumentTests.XmlDocumentTests
{
    public class CreateCDataSectionTests
    {
        [Fact]
        public static void CreateCDataTest1()
        {
            var xmlDocument = new XmlDocument();
            var cdata = xmlDocument.CreateCDataSection(String.Empty);

            Assert.Equal("<![CDATA[]]>", cdata.OuterXml);
            Assert.Equal(String.Empty, cdata.InnerXml);
            Assert.Equal(String.Empty, cdata.InnerText);
            Assert.Equal(XmlNodeType.CDATA, cdata.NodeType);
        }

        [Fact]
        public static void CreateCDataTest2()
        {
            var xmlDocument = new XmlDocument();
            var cdata = xmlDocument.CreateCDataSection("test data");

            Assert.Equal("<![CDATA[test data]]>", cdata.OuterXml);
            Assert.Equal(String.Empty, cdata.InnerXml);
            Assert.Equal("test data", cdata.InnerText);
            Assert.Equal(XmlNodeType.CDATA, cdata.NodeType);
        }

        [Fact]
        public static void CreateCDataTest3()
        {
            var xmlDocument = new XmlDocument();
            var cdata = xmlDocument.CreateCDataSection("]]>");

            Assert.Throws<ArgumentException>(() => { var test = cdata.OuterXml; });
            Assert.Equal(String.Empty, cdata.InnerXml);
            Assert.Equal("]]>", cdata.InnerText);
            Assert.Equal(XmlNodeType.CDATA, cdata.NodeType);
        }

        [Fact]
        public static void CreateCDataTest4()
        {
            var xmlDocument = new XmlDocument();
            var cdata = xmlDocument.CreateCDataSection("=\"&<> : ;; @#$%^&*()\"~\"' ");

            Assert.Equal("<![CDATA[=\"&<> : ;; @#$%^&*()\"~\"' ]]>", cdata.OuterXml);
            Assert.Equal(String.Empty, cdata.InnerXml);
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
            Assert.Equal(String.Empty, cdata.InnerXml);
            Assert.Equal(text, cdata.InnerText);
            Assert.Equal(XmlNodeType.CDATA, cdata.NodeType);
        }
    }
}
