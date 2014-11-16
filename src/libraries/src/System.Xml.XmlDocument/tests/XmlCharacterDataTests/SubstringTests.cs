// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Xml;

namespace XmlDocumentTests.XmlCharacterDataTests
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
            var cdataNode = (XmlCharacterData)xmlDocument.CreateCDataSection(String.Empty);

            var subString = cdataNode.Substring(0, 10);

            Assert.Equal(String.Empty, subString);
        }
    }
}
