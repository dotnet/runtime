// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Xunit;

namespace System.Security.Cryptography.Xml.Tests
{
    public class KeyInfoTests
    {
        [Fact]
        public void Constructor()
        {
            KeyInfo keyInfo = new KeyInfo();

            Assert.Equal(0, keyInfo.Count);
            Assert.Null(keyInfo.Id);

            XmlElement xmlElement = keyInfo.GetXml();
            Assert.NotNull(xmlElement);
            Assert.Equal("<KeyInfo xmlns=\"http://www.w3.org/2000/09/xmldsig#\" />", xmlElement.OuterXml);

            IEnumerator enumerator = keyInfo.GetEnumerator();
            Assert.NotNull(enumerator);
            Assert.False(enumerator.MoveNext());
        }

        [Fact]
        public void AddClause()
        {
            KeyInfo keyInfo = new KeyInfo();
            Assert.Equal(0, keyInfo.Count);

            KeyInfoName name1 = new KeyInfoName("key1");
            keyInfo.AddClause(name1);
            Assert.Equal(1, keyInfo.Count);

            KeyInfoName name2 = new KeyInfoName("key2");
            keyInfo.AddClause(name2);
            Assert.Equal(2, keyInfo.Count);
        }

        [Fact]
        public void GetEnumerator()
        {
            KeyInfo keyInfo = new KeyInfo();
            keyInfo.AddClause(new KeyInfoName("key1"));
            keyInfo.AddClause(new KeyInfoName("key2"));

            int count = 0;
            IEnumerator enumerator = keyInfo.GetEnumerator();
            while (enumerator.MoveNext())
            {
                Assert.NotNull(enumerator.Current);
                count++;
            }
            Assert.Equal(2, count);
        }

        [Fact]
        public void GetXml_WithId()
        {
            KeyInfo keyInfo = new KeyInfo();
            keyInfo.Id = "KeyInfo-1";
            keyInfo.AddClause(new KeyInfoName("TestKey"));

            XmlElement xml = keyInfo.GetXml();
            Assert.Equal("KeyInfo-1", xml.GetAttribute("Id"));
        }

        [Fact]
        public void LoadXml()
        {
            string xml = @"<KeyInfo Id=""info1"" xmlns=""http://www.w3.org/2000/09/xmldsig#"">
                <KeyName>key1</KeyName>
                <KeyName>key2</KeyName>
            </KeyInfo>";
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xml);

            KeyInfo keyInfo = new KeyInfo();
            keyInfo.LoadXml(doc.DocumentElement);
            Assert.Equal("info1", keyInfo.Id);
            Assert.Equal(2, keyInfo.Count);
        }

        [Fact]
        public void LoadXml_Null()
        {
            KeyInfo keyInfo = new KeyInfo();
            Assert.Throws<ArgumentNullException>(() => keyInfo.LoadXml(null));
        }

        [Fact]
        public void GenericEnumerator()
        {
            KeyInfo keyInfo = new KeyInfo();
            keyInfo.AddClause(new KeyInfoName("key1"));
            keyInfo.AddClause(new KeyInfoName("key2"));

            int count = 0;
            foreach (KeyInfoClause clause in keyInfo)
            {
                Assert.NotNull(clause);
                count++;
            }
            Assert.Equal(2, count);
        }
    }
}
