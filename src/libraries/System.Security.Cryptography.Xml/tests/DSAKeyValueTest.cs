// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// DSAKeyValueTest.cs - Test Cases for DSAKeyValue
//
// Author:
//  Sebastien Pouliot (spouliot@motus.com)
//
// (C) 2002, 2003 Motus Technologies Inc. (http://www.motus.com)

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Xml;
using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.Xml.Tests
{
    [SkipOnPlatform(PlatformSupport.MobileAppleCrypto, "DSA is not available")]
    public class DSAKeyValueTest
    {
        [Fact]
        public void Ctor_Empty()
        {
            DSAKeyValue dsaKeyValue = new DSAKeyValue();
            Assert.NotNull(dsaKeyValue.Key);
        }

        [Fact]
        public void Ctor_Dsa()
        {
            using (DSA dsa = DSA.Create())
            {
                DSAKeyValue dsaKeyValue = new DSAKeyValue(dsa);
                Assert.Equal(dsa, dsaKeyValue.Key);
            }
        }

        [Fact]
        public void Ctor_Dsa_Null()
        {
            DSAKeyValue dsaKeyValue = new DSAKeyValue(null);

            //From https://github.com/peterwurzinger:
            //This assertion is incorrect, since the parameter value is stored unvalidated/unprocessed
            //Assert.NotNull(dsaKeyValue.Key);

            Assert.Null(dsaKeyValue.Key);
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/20575", TestPlatforms.OSX)]
        public void GetXml()
        {
            DSAKeyValue dsa = new DSAKeyValue();
            XmlElement xmlkey = dsa.GetXml();

            XmlNamespaceManager ns = new XmlNamespaceManager(xmlkey.OwnerDocument.NameTable);
            ns.AddNamespace("schema", SignedXml.XmlDsigNamespaceUrl);

            IEnumerable<XmlNode> elements =
                new[] { "P", "Q", "G", "Y", "J", "Seed", "PgenCounter" }
                .Select(elementName => xmlkey.SelectSingleNode($"/schema:DSAKeyValue/schema:{elementName}", ns))
                .Where(element => element != null);

            //There MUST be existing elements
            Assert.NotEmpty(elements);

            //Existing elements MUST include a "Y"-Element
            Assert.True(elements.SingleOrDefault(element => element.Name == "Y") != null);

            //Existing elements MUST contain InnerText
            Assert.True(elements.All(element => !string.IsNullOrEmpty(element.InnerText)));

            //Existing elements MUST be convertible from BASE64
            elements.Select(element => Convert.FromBase64String(element.InnerText));
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/20575", TestPlatforms.OSX)]
        public void GetXml_SameDsa()
        {
            using (DSA dsa = DSA.Create())
            {
                DSAKeyValue dsaKeyValue1 = new DSAKeyValue(dsa);
                DSAKeyValue dsaKeyValue2 = new DSAKeyValue(dsa);
                Assert.Equal(dsaKeyValue1.GetXml(), dsaKeyValue2.GetXml());
            }
        }

        [Fact]
        public void LoadXml()
        {
            const string pValue = "oDZlcdJA1Kf6UeNEIZqm4KDqA6zpX7CmEtAGWi9pgnBhWOUDVEfhswfsvTLR5BCbKfE6KoHvt5Hh8D1RcAko//iZkLZ+gds9y/5Oxape8tu3TUi1BnNPWu8ieXjMtdnpyudKFsCymssJked1rBeRePG23HTVwOV1DpopjRkjBEU=";
            const string qValue = "0JxsZhjbIteTbrtfWmt5Uif6il8=";
            const string gValue = "EOVCfv1saTWIc6Dgim24a07dqqyCJXmIT+5PrgrfV3M8/hfmaMfZtpvM0BUkXVv0dFScnN7txnSpnLWchBz0RfehL6c7Mofu/d2H1cp8zvwTasfiJhypQHDuC4p1aSXuQ1hnzzyYeHKzBH9r0PA78haL7/HnwrrscttXGhmU/L0=";
            const string yValue = "HBHSdiOJDoZhRpK+B4Ft5hisHvRjz6rELay+aPrya2yKRUUN7ZysNi12PltAvljexay0gEpPncg6TrRtH1+7usTxbgkuIwcQ3RPPIzM7y+XldbcyVUfyze5+zXy9ALiugT+zP8DOMRj9Yj6kR6ZsgbnSdlH2hGIn9NctXgRQ6Kg=";
            const string seedValue = "NKemrvYwT/4u8DNiXoPj9jO6LAg=";
            const string pgenCounterValue = "uA==";
            string dsaKey = $"<KeyValue xmlns=\"http://www.w3.org/2000/09/xmldsig#\"><DSAKeyValue><P>{pValue}</P><Q>{qValue}</Q><G>{gValue}</G><Y>{yValue}</Y><Seed>{seedValue}</Seed><PgenCounter>{pgenCounterValue}</PgenCounter></DSAKeyValue></KeyValue>";

            var xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(dsaKey);

            var dsaKeyValue = new DSAKeyValue();
            dsaKeyValue.LoadXml(xmlDoc.DocumentElement);

            var parameters = dsaKeyValue.Key.ExportParameters(false);
            Assert.Equal(Convert.ToBase64String(parameters.P), pValue);
            Assert.Equal(Convert.ToBase64String(parameters.Q), qValue);
            Assert.Equal(Convert.ToBase64String(parameters.G), gValue);
            Assert.Equal(Convert.ToBase64String(parameters.Y), yValue);

            // Not all providers support round-tripping the seed value.
            // Seed and PGenCounter are round-tripped together.
            if (parameters.Seed != null)
            {
                Assert.Equal(Convert.ToBase64String(parameters.Seed), seedValue);
                Assert.Equal(BitConverter.GetBytes(parameters.Counter)[0], Convert.FromBase64String(pgenCounterValue)[0]);
            }
        }

        [Fact]
        public void LoadXml_Null()
        {
            DSAKeyValue dsa1 = new DSAKeyValue();
            Assert.Throws<ArgumentNullException>(() => dsa1.LoadXml(null));
        }

        [Fact]
        public void ImportDSAKeyValue()
        {
            string p = "6zJxhRqpk5yQ7sjFSr6mPepyVwpTAXSmw1oh+5Cn/z1DjFSpW6rC6sTOkE3CMNwWOwIzrpVS3bWep7wo9CaBrOPIIVe+E4sqpPeyM2wr10mQThHEsCQAjnxBhJJindf9amaBhi6sOtVNnyETFWV6yKDptZEm9c3xdl4L7ogEbX8=";
            string q = "/0LHTzCnnmTZ0j7f4maA7NXrz60=";
            string g = "ZZefq7boKsU1pcKfRSOmsCo9QrV9N3j9aw2IP07PUoHS175vYPOBFduZIZXNmrnPEyTYpo3DaLUlTK9jsHh7TfIea4p4WC2sGF+2BgVXpioSGVFqbnZ29wIe3B6vWZtN1vSiapd9dgFklh3k65af1u3r2r9QpS2pH0BMBwCmvk8=";
            string y = "zWjWuf+4sK3p5BPpEcY8eYqcJcInFI+68HcTNjSi+0WqP+oKoQux+esVf1CfAWHcHxUFaZCWhUeqKxCN4bRUY95XZAwrMVqGFQtV3UMCzsJOYvzMtAVtOjD1Cl6d1b1i186zCkaodskgX+O0VcS/fPn+QAjIsIXWrFAuDmKfGVw=";
            string seed = "9/Y5blrHfG7EpAYQqkmgvaDzqKo=";
            string pgenCounter = "BAQ=";
            string dsaKeyXml = $@"<KeyInfo xmlns=""http://www.w3.org/2000/09/xmldsig#""><KeyValue><DSAKeyValue><P>{p}</P><Q>{q}</Q><G>{g}</G><Y>{y}</Y><Seed>{seed}</Seed><PgenCounter>{pgenCounter}</PgenCounter></DSAKeyValue></KeyValue></KeyInfo>";

            var expected = new Dictionary<string, string>()
            {
                { "P", p },
                { "Q", q },
                { "G", g },
                { "Y", y }
            };

            XmlDocument doc = new XmlDocument();
            doc.LoadXml(dsaKeyXml);

            KeyInfo info = new KeyInfo();
            info.LoadXml(doc.DocumentElement);

            XmlElement el = info.GetXml();

            foreach (var kv in expected)
            {
                XmlNode node = el.SelectSingleNode($"//*[local-name()='DSAKeyValue']/*[local-name()='{kv.Key}']");
                Assert.NotNull(node);
                Assert.Equal(kv.Value, node.InnerText);
            }

            // Either both null or both have correct values
            XmlNode seedNode = el.SelectSingleNode($"//*[local-name()='DSAKeyValue']/*[local-name()='Seed']");
            XmlNode counterNode = el.SelectSingleNode($"//*[local-name()='DSAKeyValue']/*[local-name()='PgenCounter']");

            Assert.Equal(seedNode != null, counterNode != null);
            if (seedNode != null)
            {
                Assert.Equal(seed, seedNode.InnerText);
                Assert.Equal(pgenCounter, counterNode.InnerText);
            }
        }
    }
}
