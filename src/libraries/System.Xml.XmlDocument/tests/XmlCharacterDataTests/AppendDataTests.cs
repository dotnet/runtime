// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System.Xml;

namespace XmlDocumentTests.XmlCharacterDataTests
{
    public class AppendDataTests
    {
        private static void AppendNullToXmlCharacterData(XmlCharacterData xmlCharacterData)
        {
            var dataBefore = xmlCharacterData.Data;
            xmlCharacterData.AppendData(null);
            Assert.Equal(dataBefore, xmlCharacterData.Data);
        }

        [Fact]
        public static void AppendNullToTextNodeTest()
        {
            var xmlDocument = new XmlDocument();
            AppendNullToXmlCharacterData(xmlDocument.CreateTextNode("a"));
        }

        [Fact]
        public static void AppendNullToCommentNodeTest()
        {
            var xmlDocument = new XmlDocument();
            AppendNullToXmlCharacterData(xmlDocument.CreateComment("a"));
        }

        [Fact]
        public static void AppendNullToCDataNodeTest()
        {
            var xmlDocument = new XmlDocument();
            AppendNullToXmlCharacterData(xmlDocument.CreateCDataSection("a"));
        }

        [Fact]
        public static void AppendDataToAnEmptyCdataNode()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<root><![CDATA[]]></root>");

            var cdataNode = (XmlCharacterData)xmlDocument.DocumentElement.FirstChild;

            cdataNode.AppendData("hello");

            Assert.Equal("hello", cdataNode.Data);
        }

        [Fact]
        public static void AppendDataToACdataNodeWithSomeContent()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<root><![CDATA[already here]]></root>");

            var cdataNode = (XmlCharacterData)xmlDocument.DocumentElement.FirstChild;

            cdataNode.AppendData(" hello");

            Assert.Equal("already here hello", cdataNode.Data);
        }
    }
}
