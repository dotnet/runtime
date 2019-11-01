// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Xml.Tests
{
    public class SubstringTests
    {
        [Fact]
        public static void Substring()
        {
            var xmlDocument = new XmlDocument();
            var testString = "abcde";
            var cdataNode = (XmlCharacterData)xmlDocument.CreateCDataSection(testString);

            var subString = cdataNode.Substring(0, 2);

            Assert.Equal("ab", subString);
        }

        [Fact]
        public static void SubstringBeforeBeginning()
        {
            var xmlDocument = new XmlDocument();
            var cdataNode = (XmlCharacterData)xmlDocument.CreateCDataSection("abcde");

            Assert.Throws<ArgumentOutOfRangeException>(() => cdataNode.Substring(-1, 1));
        }

        [Fact]
        public static void SubstringLongerThanData()
        {
            var xmlDocument = new XmlDocument();
            var testString = "abcde";
            var cdataNode = (XmlCharacterData)xmlDocument.CreateCDataSection(testString);

            var subString = cdataNode.Substring(0, testString.Length + 2);

            Assert.Equal(testString, subString);
        }

        [Fact]
        public static void EmptyString()
        {
            var xmlDocument = new XmlDocument();
            var cdataNode = (XmlCharacterData)xmlDocument.CreateCDataSection(string.Empty);

            var subString = cdataNode.Substring(0, 10);

            Assert.Equal(string.Empty, subString);
        }
    }
}
