// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Xml.Tests
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
