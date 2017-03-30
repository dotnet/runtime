//
// KeyInfoNameTest.cs - Test Cases for KeyInfoName
//
// Author:
//	Sebastien Pouliot (spouliot@motus.com)
//
// (C) 2002, 2003 Motus Technologies Inc. (http://www.motus.com)
//
// Licensed to the .NET Foundation under one or more agreements.
// See the LICENSE file in the project root for more information.

using System.Xml;
using Xunit;

namespace System.Security.Cryptography.Xml.Tests
{

    public class KeyInfoNameTest
    {

        [Fact]
        public void NewKeyValue()
        {
            string newKeyValue = "Mono::";
            KeyInfoName name1 = new KeyInfoName();
            name1.Value = newKeyValue;
            XmlElement xel = name1.GetXml();

            KeyInfoName name2 = new KeyInfoName();
            name2.LoadXml(xel);

            Assert.Equal(newKeyValue, name1.Value);
            Assert.Equal((name1.GetXml().OuterXml), (name2.GetXml().OuterXml));
        }

        [Fact]
        public void ImportKeyValue()
        {
            string value = "<KeyName xmlns=\"http://www.w3.org/2000/09/xmldsig#\">Mono::</KeyName>";
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(value);

            KeyInfoName name = new KeyInfoName();
            name.LoadXml(doc.DocumentElement);
            Assert.Equal("Mono::", name.Value);
            Assert.Equal(value, name.GetXml().OuterXml);
        }

        [Fact]
        public void InvalidValue1()
        {
            string bad = "<Test></Test>";
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(bad);

            KeyInfoName name = new KeyInfoName();
            Assert.Throws<ArgumentNullException>(() => name.LoadXml(null));
        }

        [Fact]
        public void InvalidValue2()
        {
            string bad = "<Test></Test>";
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(bad);

            KeyInfoName name = new KeyInfoName();
            name.LoadXml(doc.DocumentElement);
            Assert.Equal("", name.Value);
            Assert.Equal("<KeyName xmlns=\"http://www.w3.org/2000/09/xmldsig#\"></KeyName>", (name.GetXml().OuterXml));
        }
    }
}
