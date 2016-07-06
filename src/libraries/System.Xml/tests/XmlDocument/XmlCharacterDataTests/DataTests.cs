// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Xml.Tests
{
    public class Character_DataTests
    {
        [Fact]
        public static void GetDataFromEmptyCdataNode()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<root><![CDATA[]]></root>");

            var cdataNode = (XmlCharacterData)xmlDocument.DocumentElement.FirstChild;

            Assert.Equal(String.Empty, cdataNode.Data);
        }

        [Fact]
        public static void SetDataFromEmptyCdataNode()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<root><![CDATA[]]></root>");

            var cdataNode = (XmlCharacterData)xmlDocument.DocumentElement.FirstChild;
            cdataNode.Data = "   !   <>&& very strange data 0x3000 &234   ";

            Assert.Equal("   !   <>&& very strange data 0x3000 &234   ", cdataNode.Data);
        }

        [Fact]
        public static void GetDataFromCdataNode()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<root><![CDATA[   !   <>&& very strange data 0x3000 &234   ]]></root>");

            var cdataNode = (XmlCharacterData)xmlDocument.DocumentElement.FirstChild;

            Assert.Equal("   !   <>&& very strange data 0x3000 &234   ", cdataNode.Data);
        }

        [Fact]
        public static void SetDataFromCdataNode()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<root><![CDATA[abcdefgh]]></root>");

            var cdataNode = (XmlCharacterData)xmlDocument.DocumentElement.FirstChild;
            cdataNode.Data = "   !   <>&& very strange data 0x3000 &234   ";

            Assert.Equal("   !   <>&& very strange data 0x3000 &234   ", cdataNode.Data);
        }

        [Fact]
        public static void MoveTextNodeWithMuchWhiteSpace()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml(" <elem1 xml:space='preserve'>       content   with         spaces  </elem1>");

            var cdataNode = (XmlCharacterData)xmlDocument.FirstChild.FirstChild;

            Assert.Equal("       content   with         spaces  ", cdataNode.Data);
        }
    }
}
