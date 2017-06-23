// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Xml.Tests
{
    public class CreateXmlDeclarationTests
    {
        [Fact]
        public static void CheckAllAttributes()
        {
            var xmlDocument = new XmlDocument();

            var decl = xmlDocument.CreateXmlDeclaration("1.0", "UTF-8", "yes");

            Assert.Equal("1.0", decl.Version);
            Assert.Equal("UTF-8", decl.Encoding);
            Assert.Equal("yes", decl.Standalone);
        }

        [Fact]
        public static void CheckAllAttributesOnCloneTrue()
        {
            var xmlDocument = new XmlDocument();

            var decl = xmlDocument.CreateXmlDeclaration("1.0", "UTF-8", "yes");
            var declCloned = (XmlDeclaration)decl.CloneNode(true);

            Assert.Equal("1.0", declCloned.Version);
            Assert.Equal("UTF-8", declCloned.Encoding);
            Assert.Equal("yes", declCloned.Standalone);
        }

        [Fact]
        public static void CheckAllAttributesOnCloneFalse()
        {
            var xmlDocument = new XmlDocument();

            var decl = xmlDocument.CreateXmlDeclaration("1.0", "UTF-8", "yes");
            var declCloned = (XmlDeclaration)decl.CloneNode(false);

            Assert.Equal("1.0", declCloned.Version);
            Assert.Equal("UTF-8", declCloned.Encoding);
            Assert.Equal("yes", declCloned.Standalone);
        }

        [Fact]
        public static void WrongXmlVersion()
        {
            var xmlDocument = new XmlDocument();

            AssertExtensions.Throws<ArgumentException>(null, () => xmlDocument.CreateXmlDeclaration("3.0", "UTF-8", "yes"));
        }

        [Fact]
        public static void InvalidEncoding()
        {
            var xmlDocument = new XmlDocument();
            var decl = xmlDocument.CreateXmlDeclaration("1.0", "wrong", "yes");
        }

        [Fact]
        public static void InvalidStandalone()
        {
            var xmlDocument = new XmlDocument();

            AssertExtensions.Throws<ArgumentException>(null, () => xmlDocument.CreateXmlDeclaration("1.0", "UTF-8", "Wrong"));
        }

        [Fact]
        public static void InvalidStandalone2()
        {
            var xmlDocument = new XmlDocument();

            var decl = xmlDocument.CreateXmlDeclaration("1.0", null, "yes");

            Assert.Equal(decl.Encoding, String.Empty);
        }
    }
}
