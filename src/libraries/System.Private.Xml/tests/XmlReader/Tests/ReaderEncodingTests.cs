// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using Xunit;

namespace System.Xml.Tests
{
    /// <summary>
    /// This class is not completely testing XmlReader Encoding, it has a regression tests for the fix of the issue: https://github.com/dotnet/runtime/issues/28615
    /// which reported due to fuzzy testing. Defect happening while encoding byte array, which includes a surrogate char and an invalid char.
    /// </summary>
    public class ReaderEncodingTests
    {
        private static string _invalidCharMessageStart = "Data at the root level is invalid";
        private static string _badStartNameChar = "Name cannot begin with the";
        private static string _invalidCharInThisEncoding = "Invalid character in the given encoding";

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/50945", TestPlatforms.Android)]
        public static void ReadWithSurrogateCharAndInvalidChar()
        {
            // {60, 0, 0, 0} is a normal char, {0, 34, 1, 0}  is a surrogate char {62, 100, 60, 47} is an invalid char
            var bytes = new byte[] { 60, 0, 0, 0, 0, 34, 1, 0, 62, 100, 60, 47, 97, 62, 10 };
            var reader = XmlReader.Create(new MemoryStream(bytes));

            XmlException ex = Assert.Throws<XmlException>(() => reader.Read());
            Assert.Contains(_invalidCharInThisEncoding, ex.Message);
            Assert.Equal(4, ex.LinePosition);
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/50945", TestPlatforms.Android)]
        public static void ReadWithNormalCharAndInvalidChar()
        {
            // {60, 0, 0, 0, 65, 0, 0, 0} are normal chars, {62, 100, 60, 47} is an invalid char, similar bytes used below tests
            var bytes = new byte[] { 60, 0, 0, 0, 65, 0, 0, 0, 62, 100, 60, 47, 97, 62, 10 };
            var reader = XmlReader.Create(new MemoryStream(bytes));

            XmlException ex = Assert.Throws<XmlException>(() => reader.Read());
            Assert.Contains(_invalidCharInThisEncoding, ex.Message);
            Assert.Equal(3, ex.LinePosition);
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/50945", TestPlatforms.Android)]
        public static void ReadWithSurrogateCharAndInvalidChar_ValidXmlStructure()
        {
            var bytes = new byte[] { 60, 0, 0, 0, 97, 0, 0, 0, 62, 0, 0, 0, 0, 34, 1, 0, 62, 100, 60, 47, 60, 0, 0, 0, 47, 0, 0, 0, 97, 0, 0, 0, 62, 0, 0, 0 };
            var reader = XmlReader.Create(new MemoryStream(bytes));

            XmlException ex = Assert.Throws<XmlException>(() => reader.Read());
            Assert.Contains(_invalidCharInThisEncoding, ex.Message);
            Assert.Equal(6, ex.LinePosition);
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/50945", TestPlatforms.Android)]
        public static void ReadWithSurrogateCharAsElementName()
        {
            var bytes = new byte[] { 60, 0, 0, 0, 0, 34, 1, 0, 65, 0, 0, 0, 97, 0, 0, 0 };
            var reader = XmlReader.Create(new MemoryStream(bytes));

            XmlException ex = Assert.Throws<XmlException>(() => reader.Read());
            Assert.Contains(_badStartNameChar, ex.Message);
            Assert.Equal(2, ex.LinePosition);
        }

        [Fact]
        public static void BytesStartingWithSurrogateChar()
        {
            var bytes = new byte[] { 0, 34, 1, 0, 60, 0, 0, 0, 65, 0, 0, 0, 97, 0, 0, 0 };
            var reader = XmlReader.Create(new MemoryStream(bytes));

            XmlException ex = Assert.Throws<XmlException>(() => reader.Read());
            Assert.Equal(1, ex.LinePosition);
        }

        [Fact]
        public static void BytesEndingWithSurrogateChar()
        {
            var bytes = new byte[] { 60, 0, 0, 0, 65, 0, 0, 0, 97, 0, 0, 0, 62, 0, 0, 0, 98, 0, 0, 0, 97, 0, 0, 0, 0, 34, 1, 0 };
            var reader = XmlReader.Create(new MemoryStream(bytes));

            Assert.True(reader.Read());
            Assert.True(reader.Read());
            XmlException ex = Assert.Throws<XmlException>(() => reader.Read());
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/50945", TestPlatforms.Android)]
        public static void BytesStartingWithInvalidChar()
        {
            var bytes = new byte[] { 62, 100, 60, 47, 60, 0, 0, 0, 0, 34, 1, 0, 65, 0, 0, 0, 97, 0, 0, 0 };
            var reader = XmlReader.Create(new MemoryStream(bytes));

            XmlException ex = Assert.Throws<XmlException>(() => reader.Read());
            Assert.Contains(_invalidCharMessageStart, ex.Message);
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/50945", TestPlatforms.Android)]
        public static void BytesEndingWithInvalidChar()
        {
            var bytes = new byte[] { 60, 0, 0, 0, 97, 0, 0, 0, 62, 0, 0, 0, 65, 0, 0, 0, 65, 0, 0, 0, 97, 0, 0, 0, 62, 100, 60, 47};
            var reader = XmlReader.Create(new MemoryStream(bytes));

            Assert.True(reader.Read());
            XmlException ex = Assert.Throws<XmlException>(() => reader.ReadElementContentAsString());
            Assert.Contains(_invalidCharInThisEncoding, ex.Message);
        }

        [Fact]
        public static void ReadWithSurrogateChar_ValidXmlStructure()
        {
            var bytes = new byte[] { 60, 0, 0, 0, 97, 0, 0, 0, 62, 0, 0, 0, 0, 34, 1, 0, 0, 35, 1, 0, 60, 0, 0, 0, 47, 0, 0, 0, 97, 0, 0, 0, 62, 0, 0, 0 };
            var reader = XmlReader.Create(new MemoryStream(bytes));

            Assert.True(reader.Read());
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/50945", TestPlatforms.Android)]
        public static void ReadWithIncompleteBytes()
        {
            var bytes = new byte[] { 60, 0, 0, 0, 97, 0, 0, 0, 65, 0, 0, 0, 97, 62, 10};
            var reader = XmlReader.Create(new MemoryStream(bytes));

            XmlException ex = Assert.Throws<XmlException>(() => reader.Read());
            Assert.Contains(_invalidCharMessageStart, ex.Message);
        }
    }
}
