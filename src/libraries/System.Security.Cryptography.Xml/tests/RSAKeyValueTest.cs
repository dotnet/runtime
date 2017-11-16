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
                .Select(elementName => Convert.FromBase64String(xmlkey.SelectSingleNode($"*[local-name()='RSAKeyValue' and namespace-uri()='{schema}']/*[local-name()='{elementName}' and namespace-uri()='{schema}']").InnerText))
                .ToArray();
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
        public void LoadXml_LoadXml_GetXml()
        {
            string rsaKey = "<KeyValue xmlns=\"http://www.w3.org/2000/09/xmldsig#\"><RSAKeyValue><Modulus>ogZ1/O7iks9ncETqNxLDKoPvgrT4nFx1a3lOmpywEmgbc5+8vI5dSzReH4v0YrflY75rIJx13CYWMsaHfQ78GtXvaeshHlQ3lLTuSdYEJceKll/URlBoKQtOj5qYIVSFOIVGHv4Y/0lnLftOzIydem29KKH6lJQlJawBBssR12s=</Modulus><Exponent>AQAB</Exponent></RSAKeyValue></KeyValue>";
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(rsaKey);

            RSAKeyValue rsa1 = new RSAKeyValue();
            rsa1.LoadXml(doc.DocumentElement);

            string s = rsa1.GetXml().OuterXml;
            Assert.Equal(rsaKey, s);
        }

        [Fact]
        public void LoadXml_GetXml_With_NS_Prefix()
        {
            string rsaKeyWithPrefix = "<ds:KeyValue xmlns:ds=\"http://www.w3.org/2000/09/xmldsig#\"><ds:RSAKeyValue><ds:Modulus>ogZ1/O7iks9ncETqNxLDKoPvgrT4nFx1a3lOmpywEmgbc5+8vI5dSzReH4v0YrflY75rIJx13CYWMsaHfQ78GtXvaeshHlQ3lLTuSdYEJceKll/URlBoKQtOj5qYIVSFOIVGHv4Y/0lnLftOzIydem29KKH6lJQlJawBBssR12s=</ds:Modulus><ds:Exponent>AQAB</ds:Exponent></ds:RSAKeyValue></ds:KeyValue>";
            string rsaKeyWithoutPrefix = "<KeyValue xmlns=\"http://www.w3.org/2000/09/xmldsig#\"><RSAKeyValue><Modulus>ogZ1/O7iks9ncETqNxLDKoPvgrT4nFx1a3lOmpywEmgbc5+8vI5dSzReH4v0YrflY75rIJx13CYWMsaHfQ78GtXvaeshHlQ3lLTuSdYEJceKll/URlBoKQtOj5qYIVSFOIVGHv4Y/0lnLftOzIydem29KKH6lJQlJawBBssR12s=</Modulus><Exponent>AQAB</Exponent></RSAKeyValue></KeyValue>";
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(rsaKeyWithPrefix);

            RSAKeyValue rsa1 = new RSAKeyValue();
            rsa1.LoadXml(doc.DocumentElement);

            string s = rsa1.GetXml().OuterXml;
            //Comparing with rsaKeyWithoutPrefix because RSAKeyValue.GetXml().OuterXml returns the markup without the namespace prefixes
            Assert.Equal(rsaKeyWithoutPrefix, s);
        }

        [Fact]
        public void LoadXml_Null()
        {
            RSAKeyValue rsa = new RSAKeyValue();
            Assert.Throws<ArgumentNullException>(() => rsa.LoadXml(null));
        }

        [Theory]
        [MemberData(nameof(LoadXml_InvalidXml_Source))]
        public void LoadXml_InvalidXml(string xml)
        {
            XmlDocument xmlDocument = new XmlDocument();
            xmlDocument.LoadXml(xml);

            RSAKeyValue rsa = new RSAKeyValue();

            // FormatException exception because desktop does not
            // check if Convert.FromBase64String throws
            // Related to: https://github.com/dotnet/corefx/issues/18690
            try
            {
                rsa.LoadXml(xmlDocument.DocumentElement);
            }
            catch (CryptographicException) { }
            catch (FormatException) when (PlatformDetection.IsFullFramework) { }
        }

        private static object[][] LoadXml_InvalidXml_Source()
        {
            return new object[][]
            {
                // Missing elements
                new [] { "<KeyValue/>" },
                new [] { "<KeyValue><RSAKeyValue/></KeyValue>" },
                new [] { "<RSAKeyValue/>" },
                new [] { "<KeyValue><RSAKeyValue></RSAKeyValue></KeyValue>" },
                new [] { "<KeyValue><RSAKeyValue><Exponent>AQAB</Exponent></RSAKeyValue></KeyValue>" },
                new [] { "<KeyValue><RSAKeyValue><Modulus>ogZ1/O7iks9ncETqNxLDKoPvgrT4nFx1a3lOmpywEmgbc5+8vI5dSzReH4v0YrflY75rIJx13CYWMsaHfQ78GtXvaeshHlQ3lLTuSdYEJceKll/URlBoKQtOj5qYIVSFOIVGHv4Y/0lnLftOzIydem29KKH6lJQlJawBBssR12s=</Modulus></RSAKeyValue></KeyValue>" },

                // Invalid length
                new [] { "<KeyValue><RSAKeyValue><Modulus>gZ1/O7iks9ncETqNxLDKoPvgrT4nFx1a3lOmpywEmgbc5+8vI5dSzReH4v0YrflY75rIJx13CYWMsaHfQ78GtXvaeshHlQ3lLTuSdYEJceKll/URlBoKQtOj5qYIVSFOIVGHv4Y/0lnLftOzIydem29KKH6lJQlJawBBssR12s=</Modulus></RSAKeyValue></KeyValue>" },
                new [] { "<KeyValue><RSAKeyValue><Exponent>1AQAB</Exponent></RSAKeyValue></KeyValue>" },

                // Invalid namespace
                new [] { "<KeyValue><RSAKeyValue><Modulus>ogZ1/O7iks9ncETqNxLDKoPvgrT4nFx1a3lOmpywEmgbc5+8vI5dSzReH4v0YrflY75rIJx13CYWMsaHfQ78GtXvaeshHlQ3lLTuSdYEJceKll/URlBoKQtOj5qYIVSFOIVGHv4Y/0lnLftOzIydem29KKH6lJQlJawBBssR12s=</Modulus><Exponent>AQAB</Exponent></RSAKeyValue></KeyValue>" },
                new [] { "<KeyValue xlmns=\"http://randomnamespace.org\"><RSAKeyValue><Modulus>ogZ1/O7iks9ncETqNxLDKoPvgrT4nFx1a3lOmpywEmgbc5+8vI5dSzReH4v0YrflY75rIJx13CYWMsaHfQ78GtXvaeshHlQ3lLTuSdYEJceKll/URlBoKQtOj5qYIVSFOIVGHv4Y/0lnLftOzIydem29KKH6lJQlJawBBssR12s=</Modulus><Exponent>AQAB</Exponent></RSAKeyValue></KeyValue>" },
            };
        }

        [Theory]
        [MemberData(nameof(LoadXml_ValidXml_Source))]
        public void LoadXml_ValidXml(string xml, byte[] expectedModulus, byte[] expectedExponent)
        {
            XmlDocument xmlDocument = new XmlDocument();
            xmlDocument.LoadXml(xml);

            RSAKeyValue rsa = new RSAKeyValue();
            rsa.LoadXml(xmlDocument.DocumentElement);

            RSAParameters rsaParameters = rsa.Key.ExportParameters(false);
            Assert.Equal(expectedModulus, rsaParameters.Modulus);
            Assert.Equal(expectedExponent, rsaParameters.Exponent);
        }

        private static object[][] LoadXml_ValidXml_Source()
        {
            const string modulus1 =
                "ogZ1/O7iks9ncETqNxLDKoPvgrT4nFx1a3lOmpywEmgbc5+8vI5dSzReH4v0YrflY75rIJx13CYWMsaHfQ78GtXvaeshHlQ3lLTuSdYEJceKll/URlBoKQtOj5qYIVSFOIVGHv4Y/0lnLftOzIydem29KKH6lJQlJawBBssR12s=";
            const string modulus2 =
                "xA7SEU+e0yQH5rm9kbCDN9o3aPIo7HbP7tX6WOocLZAtNfyxSZDU16ksL6WjubafOqNEpcwR3RdFsT7bCqnXPBe5ELh5u4VEy19MzxkXRgrMvavzyBpVRgBUwUlV5foK5hhmbktQhyNdy/6LpQRhDUDsTvK+g9Ucj47es9AQJ3U=";
            const string exponent = "AQAB";

            return new []
            {
                new object[]
                {
                    $"<KeyValue xmlns=\"http://www.w3.org/2000/09/xmldsig#\"><RSAKeyValue><Modulus>{modulus1}</Modulus><Exponent>{exponent}</Exponent></RSAKeyValue></KeyValue>",
                    Convert.FromBase64String(modulus1),
                    Convert.FromBase64String(exponent)
                },
                new object[]
                {
                    $"<KeyValue xmlns:ds=\"http://www.w3.org/2000/09/xmldsig#\" xmlns=\"http://www.w3.org/2000/09/xmldsig#\"><ds:RSAKeyValue><ds:Modulus>{modulus2}</ds:Modulus><ds:Exponent>{exponent}</ds:Exponent></ds:RSAKeyValue></KeyValue>",
                    Convert.FromBase64String(modulus2),
                    Convert.FromBase64String(exponent)
                },
            };
        }
    }
}
