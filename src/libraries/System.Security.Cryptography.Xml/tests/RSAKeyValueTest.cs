//
// RSAKeyValueTest.cs - NUnit Test Cases for RSAKeyValue
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

    public class RSAKeyValueTest
    {

        [Fact]
        public void GeneratedKey()
        {
            RSAKeyValue rsa1 = new RSAKeyValue();
            Assert.NotNull(rsa1.Key);
            XmlElement xmlkey = rsa1.GetXml();

            RSAKeyValue rsa2 = new RSAKeyValue();
            rsa2.LoadXml(xmlkey);

            Assert.True((rsa1.GetXml().OuterXml) == (rsa2.GetXml().OuterXml), "rsa1==rsa2");

            RSA key = rsa1.Key;
            RSAKeyValue rsa3 = new RSAKeyValue(key);
            Assert.True((rsa3.GetXml().OuterXml) == (rsa1.GetXml().OuterXml), "rsa3==rsa1");
            Assert.True((rsa3.GetXml().OuterXml) == (rsa2.GetXml().OuterXml), "rsa3==rsa2");
        }

        [Fact]
        public void ImportKey()
        {
            string rsaKey = "<KeyValue xmlns=\"http://www.w3.org/2000/09/xmldsig#\"><RSAKeyValue><Modulus>ogZ1/O7iks9ncETqNxLDKoPvgrT4nFx1a3lOmpywEmgbc5+8vI5dSzReH4v0YrflY75rIJx13CYWMsaHfQ78GtXvaeshHlQ3lLTuSdYEJceKll/URlBoKQtOj5qYIVSFOIVGHv4Y/0lnLftOzIydem29KKH6lJQlJawBBssR12s=</Modulus><Exponent>AQAB</Exponent></RSAKeyValue></KeyValue>";
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(rsaKey);

            RSAKeyValue rsa1 = new RSAKeyValue();
            rsa1.LoadXml(doc.DocumentElement);

            string s = (rsa1.GetXml().OuterXml);
            Assert.Equal(rsaKey, s);
        }

        [Fact]
        public void InvalidValue1()
        {
            RSAKeyValue rsa = new RSAKeyValue();
            Assert.Throws<ArgumentNullException>(() => rsa.LoadXml(null));
        }

        [Fact]
        public void InvalidValue2()
        {
            string badKey = "<Test></Test>";
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(badKey);

            RSAKeyValue rsa = new RSAKeyValue();
            Assert.Throws<CryptographicException>(() => rsa.LoadXml(doc.DocumentElement));
        }
    }
}
