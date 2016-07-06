// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Xml.Tests
{
    public class ReplaceTests
    {
        [Fact]
        public static void Replace0CharactersFromCdataNode()
        {
            var xmlDocument = new XmlDocument();
            var cdataNode = (XmlCharacterData)xmlDocument.CreateCDataSection("abcde");

            cdataNode.ReplaceData(0, 0, "test");

            Assert.Equal("testabcde", cdataNode.Data);
        }

        [Fact]
        public static void Replace4CharactersFromCdataNodeFromBeginning()
        {
            var xmlDocument = new XmlDocument();
            var cdataNode = (XmlCharacterData)xmlDocument.CreateCDataSection("abcde");

            cdataNode.ReplaceData(0, 4, "test");

            Assert.Equal("teste", cdataNode.Data);
        }

        [Fact]
        public static void Replace4CharactersFromCdataNode()
        {
            var xmlDocument = new XmlDocument();
            var cdataNode = (XmlCharacterData)xmlDocument.CreateCDataSection("abcde");

            cdataNode.ReplaceData(1, 4, "test");

            Assert.Equal("atest", cdataNode.Data);
        }

        [Fact]
        public static void Replace1CharactersFromCdataNodeBeginning()
        {
            var xmlDocument = new XmlDocument();
            var cdataNode = (XmlCharacterData)xmlDocument.CreateCDataSection("abcde");

            cdataNode.ReplaceData(0, 1, "&");

            Assert.Equal("&bcde", cdataNode.Data);
        }

        [Fact]
        public static void ReplaceAllCharactersFromCdataNodeBeginning()
        {
            var xmlDocument = new XmlDocument();
            var cdataNode = (XmlCharacterData)xmlDocument.CreateCDataSection("abcdefgh");

            cdataNode.ReplaceData(0, cdataNode.Length, "new string");

            Assert.Equal("new string", cdataNode.Data);
        }

        [Fact]
        public static void ReplaceCharactersFromCdataNodeWhenStringIsShorter()
        {
            var xmlDocument = new XmlDocument();
            var cdataNode = (XmlCharacterData)xmlDocument.CreateCDataSection("abcdefgh");

            var newString = "new string";
            cdataNode.ReplaceData(0, newString.Length + 1, newString);

            Assert.Equal(newString, cdataNode.Data);
        }
    }
}
