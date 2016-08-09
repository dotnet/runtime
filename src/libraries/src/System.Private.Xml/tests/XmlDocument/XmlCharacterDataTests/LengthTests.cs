// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Xml.Tests
{
    public class LengthTests
    {
        [Fact]
        public static void LengthOfCdata()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<root><![CDATA[abcdefgh]]></root>");

            var cdataNode = (XmlCharacterData)xmlDocument.DocumentElement.FirstChild;

            Assert.Equal(8, cdataNode.Length);
        }

        [Fact]
        public static void CreateCdata()
        {
            var xmlDocument = new XmlDocument();
            var cdataNode = (XmlCharacterData)xmlDocument.CreateCDataSection("abcde");

            Assert.Equal(5, cdataNode.Length);
        }

        [Fact]
        public static void CreateEmptyCdata()
        {
            var xmlDocument = new XmlDocument();
            var cdataNode = (XmlCharacterData)xmlDocument.CreateCDataSection(String.Empty);

            Assert.Equal(0, cdataNode.Length);
        }

        [Fact]
        public static void LengthOfCdataAfterDelete()
        {
            var xmlDocument = new XmlDocument();
            var cdataNode = (XmlCharacterData)xmlDocument.CreateCDataSection("abcde");

            cdataNode.DeleteData(0, 1);
            Assert.Equal(4, cdataNode.Length);
        }
    }
}
