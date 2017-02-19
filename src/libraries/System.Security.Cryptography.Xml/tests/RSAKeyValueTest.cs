//
// RSAKeyValueTest.cs - Test Cases for RSAKeyValue
//
// Author:
//	Sebastien Pouliot (spouliot@motus.com)
//
// (C) 2002, 2003 Motus Technologies Inc. (http://www.motus.com)
//
// Licensed to the .NET Foundation under one or more agreements.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Xml;
using Xunit;

namespace System.Security.Cryptography.Xml.Tests
{

    public class RSAKeyValueTest
    {
        [Fact]
        public void Ctor_Empty()
        {
            RSAKeyValue rsaKeyValue = new RSAKeyValue();
            Assert.NotNull(rsaKeyValue.Key);
        }

        [Fact]
        public void Ctor_Rsa()
        {
            using (RSA rsa = RSA.Create())
            {
                RSAKeyValue rsaKeyValue = new RSAKeyValue(rsa);
                Assert.Equal(rsa, rsaKeyValue.Key);
            }
        }

        [Fact]
        public void Ctor_Rsa_Null()
        {
            RSAKeyValue rsaKeyValue = new RSAKeyValue(null);
            Assert.Null(rsaKeyValue.Key);
        }


        [Fact]
        public void GetXml()
        {
            RSAKeyValue rsa = new RSAKeyValue();
            XmlElement xmlkey = rsa.GetXml();

            // Schema check. Should not throw.
            const string schema = "http://www.w3.org/2000/09/xmldsig#";
            new[] { "Exponent", "Modulus" }
                .Select(elementName => Convert.FromBase64String(xmlkey.SelectSingleNode($"*[name()=RSAKeyValue & namespace-uri()='{schema}']/*[name()='{elementName}' & namespace-uri()='{schema}']").InnerText));
        }

        [Fact]
        public void GetXml_SameRsa()
        {
            using (RSA rsa = RSA.Create())
            {
                RSAKeyValue rsaKeyValue1 = new RSAKeyValue(rsa);
                RSAKeyValue rsaKeyValue2 = new RSAKeyValue(rsa);
                Assert.Equal(rsaKeyValue1.GetXml(), rsaKeyValue2.GetXml());
            }
        }

        [Fact]
        public void LoadXml_PlatformNotSupport()
        {
            string rsaKey = "<KeyValue xmlns=\"http://www.w3.org/2000/09/xmldsig#\"><RSAKeyValue><Modulus>ogZ1/O7iks9ncETqNxLDKoPvgrT4nFx1a3lOmpywEmgbc5+8vI5dSzReH4v0YrflY75rIJx13CYWMsaHfQ78GtXvaeshHlQ3lLTuSdYEJceKll/URlBoKQtOj5qYIVSFOIVGHv4Y/0lnLftOzIydem29KKH6lJQlJawBBssR12s=</Modulus><Exponent>AQAB</Exponent></RSAKeyValue></KeyValue>";
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(rsaKey);

            RSAKeyValue rsa1 = new RSAKeyValue();
            Assert.Throws<PlatformNotSupportedException>(() => rsa1.LoadXml(doc.DocumentElement));

            //string s = (rsa1.GetXml().OuterXml);
            //Assert.Equal(rsaKey, s);
        }

        [Fact]
        public void LoadXml_Null()
        {
            RSAKeyValue rsa1 = new RSAKeyValue();
            Assert.Throws<PlatformNotSupportedException>(() => rsa1.LoadXml(null));
        }
    }
}
