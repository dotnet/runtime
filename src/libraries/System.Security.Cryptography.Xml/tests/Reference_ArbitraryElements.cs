// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Xml;
using Xunit;

namespace System.Security.Cryptography.Xml.Tests
{
    public class Reference_ArbitraryElements
    {
        [Fact]
        public static void ExtraData()
        {
            string arbitraryData = @"<a:foo xmlns:a=""mynamespace"">lol</a:foo>";
            string xml = $@"<?xml version=""1.0"" encoding=""UTF-8""?><a><b xmlns:ns1=""http://www.contoso.com/"">X<Signature xmlns=""http://www.w3.org/2000/09/xmldsig#""><SignedInfo><CanonicalizationMethod Algorithm=""http://www.w3.org/TR/2001/REC-xml-c14n-20010315""/><SignatureMethod Algorithm=""http://www.w3.org/2000/09/xmldsig#rsa-sha1""/><Reference URI="""">{arbitraryData}<Transforms><Transform Algorithm=""http://www.w3.org/2000/09/xmldsig#enveloped-signature""/><Transform Algorithm=""http://www.w3.org/TR/2001/REC-xml-c14n-20010315""/></Transforms><DigestMethod Algorithm=""http://www.w3.org/2000/09/xmldsig#sha1""/><DigestValue>ZVZLYkc1BAx+YtaqeYlxanb2cGI=</DigestValue></Reference></SignedInfo><SignatureValue>Kx8xs0of766gimu5girTqiTR5xoiWjN4XMx8uzDDhG70bIqpSzlhh6IA3iI54R5mpqCCPWrJJp85ps4jpQk8RGHe4KMejstbY6YXCfs7LtRPzkNzcoZB3vDbr3ijUSrbMk+0wTaZeyeYs8Z6cOicDIVN6bN6yC/Se5fbzTTCSmg=</SignatureValue><KeyInfo><KeyValue><RSAKeyValue><Modulus>ww2w+NbXwY/GRBZfFcXqrAM2X+P1NQoU+QEvgLO1izMTB8kvx1i/bodBvHTrKMwAMGEO4kVATA1f1Vf5/lVnbqiCLMJPVRZU6rWKjOGD28T/VRaIGywTV+mC0HvMbe4DlEd3dBwJZLIMUNvOPsj5Ua+l9IS4EoszFNAg6F5Lsyk=</Modulus><Exponent>AQAB</Exponent></RSAKeyValue></KeyValue></KeyInfo></Signature></b></a>";
            Helpers.VerifyCryptoExceptionOnLoad(xml, true);
        }

        [Fact]
        public static void OutOfOrder()
        {
            string xml = $@"<?xml version=""1.0"" encoding=""UTF-8""?><a><b xmlns:ns1=""http://www.contoso.com/"">X<Signature xmlns=""http://www.w3.org/2000/09/xmldsig#""><SignedInfo><CanonicalizationMethod Algorithm=""http://www.w3.org/TR/2001/REC-xml-c14n-20010315""/><SignatureMethod Algorithm=""http://www.w3.org/2000/09/xmldsig#rsa-sha1""/><Reference URI=""""><DigestMethod Algorithm=""http://www.w3.org/2000/09/xmldsig#sha1""/><Transforms><Transform Algorithm=""http://www.w3.org/2000/09/xmldsig#enveloped-signature""/><Transform Algorithm=""http://www.w3.org/TR/2001/REC-xml-c14n-20010315""/></Transforms><DigestValue>ZVZLYkc1BAx+YtaqeYlxanb2cGI=</DigestValue></Reference></SignedInfo><SignatureValue>Kx8xs0of766gimu5girTqiTR5xoiWjN4XMx8uzDDhG70bIqpSzlhh6IA3iI54R5mpqCCPWrJJp85ps4jpQk8RGHe4KMejstbY6YXCfs7LtRPzkNzcoZB3vDbr3ijUSrbMk+0wTaZeyeYs8Z6cOicDIVN6bN6yC/Se5fbzTTCSmg=</SignatureValue><KeyInfo><KeyValue><RSAKeyValue><Modulus>ww2w+NbXwY/GRBZfFcXqrAM2X+P1NQoU+QEvgLO1izMTB8kvx1i/bodBvHTrKMwAMGEO4kVATA1f1Vf5/lVnbqiCLMJPVRZU6rWKjOGD28T/VRaIGywTV+mC0HvMbe4DlEd3dBwJZLIMUNvOPsj5Ua+l9IS4EoszFNAg6F5Lsyk=</Modulus><Exponent>AQAB</Exponent></RSAKeyValue></KeyValue></KeyInfo></Signature></b></a>";
            Helpers.VerifyCryptoExceptionOnLoad(xml, false, checkSignatureThrows: !SignatureSupport.SupportsRsaSha1Signatures, validSignature: false);
        }

        [Fact]
        public static void DuplicateTransforms()
        {
            string arbitraryData = @"<a:foo xmlns:a=""mynamespace"">lol</a:foo>";
            string xml = $@"<?xml version=""1.0"" encoding=""UTF-8""?><a><b xmlns:ns1=""http://www.contoso.com/"">X<Signature xmlns=""http://www.w3.org/2000/09/xmldsig#""><SignedInfo><CanonicalizationMethod Algorithm=""http://www.w3.org/TR/2001/REC-xml-c14n-20010315""/><SignatureMethod Algorithm=""http://www.w3.org/2000/09/xmldsig#rsa-sha1""/><Reference URI=""""><Transforms><Transform Algorithm=""http://www.w3.org/2000/09/xmldsig#enveloped-signature""/><Transform Algorithm=""http://www.w3.org/TR/2001/REC-xml-c14n-20010315""/></Transforms><Transforms>{arbitraryData}</Transforms><DigestMethod Algorithm=""http://www.w3.org/2000/09/xmldsig#sha1""/><DigestValue>ZVZLYkc1BAx+YtaqeYlxanb2cGI=</DigestValue></Reference></SignedInfo><SignatureValue>Kx8xs0of766gimu5girTqiTR5xoiWjN4XMx8uzDDhG70bIqpSzlhh6IA3iI54R5mpqCCPWrJJp85ps4jpQk8RGHe4KMejstbY6YXCfs7LtRPzkNzcoZB3vDbr3ijUSrbMk+0wTaZeyeYs8Z6cOicDIVN6bN6yC/Se5fbzTTCSmg=</SignatureValue><KeyInfo><KeyValue><RSAKeyValue><Modulus>ww2w+NbXwY/GRBZfFcXqrAM2X+P1NQoU+QEvgLO1izMTB8kvx1i/bodBvHTrKMwAMGEO4kVATA1f1Vf5/lVnbqiCLMJPVRZU6rWKjOGD28T/VRaIGywTV+mC0HvMbe4DlEd3dBwJZLIMUNvOPsj5Ua+l9IS4EoszFNAg6F5Lsyk=</Modulus><Exponent>AQAB</Exponent></RSAKeyValue></KeyValue></KeyInfo></Signature></b></a>";
            Helpers.VerifyCryptoExceptionOnLoad(xml, true);
        }

        [Fact]
        public static void Transforms_ExtraData()
        {
            string arbitraryData = @"<a:foo xmlns:a=""mynamespace"">lol</a:foo>";
            string xml = $@"<?xml version=""1.0"" encoding=""UTF-8""?><a><b xmlns:ns1=""http://www.contoso.com/"">X<Signature xmlns=""http://www.w3.org/2000/09/xmldsig#""><SignedInfo><CanonicalizationMethod Algorithm=""http://www.w3.org/TR/2001/REC-xml-c14n-20010315""/><SignatureMethod Algorithm=""http://www.w3.org/2000/09/xmldsig#rsa-sha1""/><Reference URI=""""><Transforms><Transform Algorithm=""http://www.w3.org/2000/09/xmldsig#enveloped-signature""/>{arbitraryData}<Transform Algorithm=""http://www.w3.org/TR/2001/REC-xml-c14n-20010315""/></Transforms><DigestMethod Algorithm=""http://www.w3.org/2000/09/xmldsig#sha1""/><DigestValue>ZVZLYkc1BAx+YtaqeYlxanb2cGI=</DigestValue></Reference></SignedInfo><SignatureValue>Kx8xs0of766gimu5girTqiTR5xoiWjN4XMx8uzDDhG70bIqpSzlhh6IA3iI54R5mpqCCPWrJJp85ps4jpQk8RGHe4KMejstbY6YXCfs7LtRPzkNzcoZB3vDbr3ijUSrbMk+0wTaZeyeYs8Z6cOicDIVN6bN6yC/Se5fbzTTCSmg=</SignatureValue><KeyInfo><KeyValue><RSAKeyValue><Modulus>ww2w+NbXwY/GRBZfFcXqrAM2X+P1NQoU+QEvgLO1izMTB8kvx1i/bodBvHTrKMwAMGEO4kVATA1f1Vf5/lVnbqiCLMJPVRZU6rWKjOGD28T/VRaIGywTV+mC0HvMbe4DlEd3dBwJZLIMUNvOPsj5Ua+l9IS4EoszFNAg6F5Lsyk=</Modulus><Exponent>AQAB</Exponent></RSAKeyValue></KeyValue></KeyInfo></Signature></b></a>";
            Helpers.VerifyCryptoExceptionOnLoad(xml, true);
        }

        [Fact]
        public static void Transforms_ExtraData_CData_Text()
        {
            string arbitraryData = @"text";

            string xml = $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<a><b xmlns:ns1=""http://www.contoso.com/"">X<Signature xmlns=""http://www.w3.org/2000/09/xmldsig#""><SignedInfo><CanonicalizationMethod Algorithm=""http://www.w3.org/TR/2001/REC-xml-c14n-20010315""/><SignatureMethod Algorithm=""http://www.w3.org/2000/09/xmldsig#rsa-sha1""/><Reference URI=""""><Transforms><Transform Algorithm=""http://www.w3.org/2000/09/xmldsig#enveloped-signature""/>{arbitraryData}<Transform Algorithm=""http://www.w3.org/TR/2001/REC-xml-c14n-20010315""/></Transforms><DigestMethod Algorithm=""http://www.w3.org/2000/09/xmldsig#sha1""/><DigestValue>ZVZLYkc1BAx+YtaqeYlxanb2cGI=</DigestValue></Reference></SignedInfo><SignatureValue>Kx8xs0of766gimu5girTqiTR5xoiWjN4XMx8uzDDhG70bIqpSzlhh6IA3iI54R5mpqCCPWrJJp85ps4jpQk8RGHe4KMejstbY6YXCfs7LtRPzkNzcoZB3vDbr3ijUSrbMk+0wTaZeyeYs8Z6cOicDIVN6bN6yC/Se5fbzTTCSmg=</SignatureValue><KeyInfo><KeyValue><RSAKeyValue><Modulus>ww2w+NbXwY/GRBZfFcXqrAM2X+P1NQoU+QEvgLO1izMTB8kvx1i/bodBvHTrKMwAMGEO4kVATA1f1Vf5/lVnbqiCLMJPVRZU6rWKjOGD28T/VRaIGywTV+mC0HvMbe4DlEd3dBwJZLIMUNvOPsj5Ua+l9IS4EoszFNAg6F5Lsyk=</Modulus><Exponent>AQAB</Exponent></RSAKeyValue></KeyValue></KeyInfo></Signature></b></a>";

            Helpers.VerifyCryptoExceptionOnLoad(xml, false, checkSignatureThrows: !SignatureSupport.SupportsRsaSha1Signatures, validSignature: false);
        }

        [Fact]
        public static void Transforms_ExtraData_XmlNotation()
        {
            string xml = $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<a><b xmlns:ns1=""http://www.contoso.com/"">X<Signature xmlns=""http://www.w3.org/2000/09/xmldsig#""><SignedInfo><CanonicalizationMethod Algorithm=""http://www.w3.org/TR/2001/REC-xml-c14n-20010315""/><SignatureMethod Algorithm=""http://www.w3.org/2000/09/xmldsig#rsa-sha1""/><Reference URI=""""><notation id='Transforms' name='Transforms' public='Transforms' system='Transforms' /><DigestMethod Algorithm=""http://www.w3.org/2000/09/xmldsig#sha1""/><DigestValue>ZVZLYkc1BAx+YtaqeYlxanb2cGI=</DigestValue></Reference></SignedInfo><SignatureValue>Kx8xs0of766gimu5girTqiTR5xoiWjN4XMx8uzDDhG70bIqpSzlhh6IA3iI54R5mpqCCPWrJJp85ps4jpQk8RGHe4KMejstbY6YXCfs7LtRPzkNzcoZB3vDbr3ijUSrbMk+0wTaZeyeYs8Z6cOicDIVN6bN6yC/Se5fbzTTCSmg=</SignatureValue><KeyInfo><KeyValue><RSAKeyValue><Modulus>ww2w+NbXwY/GRBZfFcXqrAM2X+P1NQoU+QEvgLO1izMTB8kvx1i/bodBvHTrKMwAMGEO4kVATA1f1Vf5/lVnbqiCLMJPVRZU6rWKjOGD28T/VRaIGywTV+mC0HvMbe4DlEd3dBwJZLIMUNvOPsj5Ua+l9IS4EoszFNAg6F5Lsyk=</Modulus><Exponent>AQAB</Exponent></RSAKeyValue></KeyValue></KeyInfo></Signature></b></a>";

            Helpers.VerifyCryptoExceptionOnLoad(xml, true);
        }

        [Fact]
        public static void Transforms_ExtraData_XmlProcessingInstruction()
        {
            string xml = $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<a><b xmlns:ns1=""http://www.contoso.com/"">X<Signature xmlns=""http://www.w3.org/2000/09/xmldsig#""><SignedInfo><CanonicalizationMethod Algorithm=""http://www.w3.org/TR/2001/REC-xml-c14n-20010315""/><SignatureMethod Algorithm=""http://www.w3.org/2000/09/xmldsig#rsa-sha1""/><Reference URI=""""><?Transforms name='Transforms' ?><DigestMethod Algorithm=""http://www.w3.org/2000/09/xmldsig#sha1""/><DigestValue>ZVZLYkc1BAx+YtaqeYlxanb2cGI=</DigestValue></Reference></SignedInfo><SignatureValue>Kx8xs0of766gimu5girTqiTR5xoiWjN4XMx8uzDDhG70bIqpSzlhh6IA3iI54R5mpqCCPWrJJp85ps4jpQk8RGHe4KMejstbY6YXCfs7LtRPzkNzcoZB3vDbr3ijUSrbMk+0wTaZeyeYs8Z6cOicDIVN6bN6yC/Se5fbzTTCSmg=</SignatureValue><KeyInfo><KeyValue><RSAKeyValue><Modulus>ww2w+NbXwY/GRBZfFcXqrAM2X+P1NQoU+QEvgLO1izMTB8kvx1i/bodBvHTrKMwAMGEO4kVATA1f1Vf5/lVnbqiCLMJPVRZU6rWKjOGD28T/VRaIGywTV+mC0HvMbe4DlEd3dBwJZLIMUNvOPsj5Ua+l9IS4EoszFNAg6F5Lsyk=</Modulus><Exponent>AQAB</Exponent></RSAKeyValue></KeyValue></KeyInfo></Signature></b></a>";

            Helpers.VerifyCryptoExceptionOnLoad(xml, false, checkSignatureThrows: !SignatureSupport.SupportsRsaSha1Signatures, validSignature: false);
        }

        [Fact]
        public static void Transforms_ExtraAttributes()
        {
            string xml = $@"<?xml version=""1.0"" encoding=""UTF-8""?><a><b xmlns:ns1=""http://www.contoso.com/"">X<Signature xmlns=""http://www.w3.org/2000/09/xmldsig#""><SignedInfo><CanonicalizationMethod Algorithm=""http://www.w3.org/TR/2001/REC-xml-c14n-20010315""/><SignatureMethod Algorithm=""http://www.w3.org/2000/09/xmldsig#rsa-sha1""/><Reference URI=""""><Transforms extraAttr=""""><Transform Algorithm=""http://www.w3.org/2000/09/xmldsig#enveloped-signature""/><Transform Algorithm=""http://www.w3.org/TR/2001/REC-xml-c14n-20010315""/></Transforms><DigestMethod Algorithm=""http://www.w3.org/2000/09/xmldsig#sha1""/><DigestValue>ZVZLYkc1BAx+YtaqeYlxanb2cGI=</DigestValue></Reference></SignedInfo><SignatureValue>Kx8xs0of766gimu5girTqiTR5xoiWjN4XMx8uzDDhG70bIqpSzlhh6IA3iI54R5mpqCCPWrJJp85ps4jpQk8RGHe4KMejstbY6YXCfs7LtRPzkNzcoZB3vDbr3ijUSrbMk+0wTaZeyeYs8Z6cOicDIVN6bN6yC/Se5fbzTTCSmg=</SignatureValue><KeyInfo><KeyValue><RSAKeyValue><Modulus>ww2w+NbXwY/GRBZfFcXqrAM2X+P1NQoU+QEvgLO1izMTB8kvx1i/bodBvHTrKMwAMGEO4kVATA1f1Vf5/lVnbqiCLMJPVRZU6rWKjOGD28T/VRaIGywTV+mC0HvMbe4DlEd3dBwJZLIMUNvOPsj5Ua+l9IS4EoszFNAg6F5Lsyk=</Modulus><Exponent>AQAB</Exponent></RSAKeyValue></KeyValue></KeyInfo></Signature></b></a>";
            Helpers.VerifyCryptoExceptionOnLoad(xml, true);
        }

        [Fact]
        public static void DuplicateDigestMethod()
        {
            string arbitraryData = @"<a:foo xmlns:a=""mynamespace"">lol</a:foo>";

            string xml = $@"<?xml version=""1.0"" encoding=""UTF-8""?><a><b xmlns:ns1=""http://www.contoso.com/"">X<Signature xmlns=""http://www.w3.org/2000/09/xmldsig#""><SignedInfo><CanonicalizationMethod Algorithm=""http://www.w3.org/TR/2001/REC-xml-c14n-20010315""/><SignatureMethod Algorithm=""http://www.w3.org/2000/09/xmldsig#rsa-sha1""/><Reference URI="""">{arbitraryData}<Transforms><Transform Algorithm=""http://www.w3.org/2000/09/xmldsig#enveloped-signature""/><Transform Algorithm=""http://www.w3.org/TR/2001/REC-xml-c14n-20010315""/></Transforms><DigestMethod Algorithm=""http://www.w3.org/2000/09/xmldsig#sha1""/><DigestMethod>{arbitraryData}</DigestMethod><DigestValue>ZVZLYkc1BAx+YtaqeYlxanb2cGI=</DigestValue></Reference></SignedInfo><SignatureValue>Kx8xs0of766gimu5girTqiTR5xoiWjN4XMx8uzDDhG70bIqpSzlhh6IA3iI54R5mpqCCPWrJJp85ps4jpQk8RGHe4KMejstbY6YXCfs7LtRPzkNzcoZB3vDbr3ijUSrbMk+0wTaZeyeYs8Z6cOicDIVN6bN6yC/Se5fbzTTCSmg=</SignatureValue><KeyInfo><KeyValue><RSAKeyValue><Modulus>ww2w+NbXwY/GRBZfFcXqrAM2X+P1NQoU+QEvgLO1izMTB8kvx1i/bodBvHTrKMwAMGEO4kVATA1f1Vf5/lVnbqiCLMJPVRZU6rWKjOGD28T/VRaIGywTV+mC0HvMbe4DlEd3dBwJZLIMUNvOPsj5Ua+l9IS4EoszFNAg6F5Lsyk=</Modulus><Exponent>AQAB</Exponent></RSAKeyValue></KeyValue></KeyInfo></Signature></b></a>";
            Helpers.VerifyCryptoExceptionOnLoad(xml, true);
        }

        [Fact]
        public static void DuplicateDigestValue()
        {
            string arbitraryData = @"<a:foo xmlns:a=""mynamespace"">lol</a:foo>";
            string xml = $@"<?xml version=""1.0"" encoding=""UTF-8""?><a><b xmlns:ns1=""http://www.contoso.com/"">X<Signature xmlns=""http://www.w3.org/2000/09/xmldsig#""><SignedInfo><CanonicalizationMethod Algorithm=""http://www.w3.org/TR/2001/REC-xml-c14n-20010315""/><SignatureMethod Algorithm=""http://www.w3.org/2000/09/xmldsig#rsa-sha1""/><Reference URI="""">{arbitraryData}<Transforms><Transform Algorithm=""http://www.w3.org/2000/09/xmldsig#enveloped-signature""/><Transform Algorithm=""http://www.w3.org/TR/2001/REC-xml-c14n-20010315""/></Transforms><DigestMethod Algorithm=""http://www.w3.org/2000/09/xmldsig#sha1""/><DigestValue>ZVZLYkc1BAx+YtaqeYlxanb2cGI=</DigestValue><DigestValue>{arbitraryData}</DigestValue></Reference></SignedInfo><SignatureValue>Kx8xs0of766gimu5girTqiTR5xoiWjN4XMx8uzDDhG70bIqpSzlhh6IA3iI54R5mpqCCPWrJJp85ps4jpQk8RGHe4KMejstbY6YXCfs7LtRPzkNzcoZB3vDbr3ijUSrbMk+0wTaZeyeYs8Z6cOicDIVN6bN6yC/Se5fbzTTCSmg=</SignatureValue><KeyInfo><KeyValue><RSAKeyValue><Modulus>ww2w+NbXwY/GRBZfFcXqrAM2X+P1NQoU+QEvgLO1izMTB8kvx1i/bodBvHTrKMwAMGEO4kVATA1f1Vf5/lVnbqiCLMJPVRZU6rWKjOGD28T/VRaIGywTV+mC0HvMbe4DlEd3dBwJZLIMUNvOPsj5Ua+l9IS4EoszFNAg6F5Lsyk=</Modulus><Exponent>AQAB</Exponent></RSAKeyValue></KeyValue></KeyInfo></Signature></b></a>";
            Helpers.VerifyCryptoExceptionOnLoad(xml, true);
        }

        [Fact]
        public static void ExtraAttributes()
        {
            foreach (string includeID in new string[] { "", $@" Id=""""" })
                foreach (string includeURI in new string[] { "", $@" URI=""""" })
                    foreach (string includeType in new string[] { "", $@" Type=""""" })
                        foreach (string includeExtra in new string[] { "", $@" extraattr=""cat""" })
                        {
                            string xml = $@"<?xml version=""1.0"" encoding=""UTF-8""?><a><b xmlns:ns1=""http://www.contoso.com/"">X<Signature xmlns=""http://www.w3.org/2000/09/xmldsig#""><SignedInfo><CanonicalizationMethod Algorithm=""http://www.w3.org/TR/2001/REC-xml-c14n-20010315""/><SignatureMethod Algorithm=""http://www.w3.org/2000/09/xmldsig#rsa-sha1""/><Reference{includeID}{includeURI}{includeExtra}{includeType}><Transforms><Transform Algorithm=""http://www.w3.org/2000/09/xmldsig#enveloped-signature""/><Transform Algorithm=""http://www.w3.org/TR/2001/REC-xml-c14n-20010315""/></Transforms><DigestMethod Algorithm=""http://www.w3.org/2000/09/xmldsig#sha1""/><DigestValue>ZVZLYkc1BAx+YtaqeYlxanb2cGI=</DigestValue></Reference></SignedInfo><SignatureValue>Kx8xs0of766gimu5girTqiTR5xoiWjN4XMx8uzDDhG70bIqpSzlhh6IA3iI54R5mpqCCPWrJJp85ps4jpQk8RGHe4KMejstbY6YXCfs7LtRPzkNzcoZB3vDbr3ijUSrbMk+0wTaZeyeYs8Z6cOicDIVN6bN6yC/Se5fbzTTCSmg=</SignatureValue><KeyInfo><KeyValue><RSAKeyValue><Modulus>ww2w+NbXwY/GRBZfFcXqrAM2X+P1NQoU+QEvgLO1izMTB8kvx1i/bodBvHTrKMwAMGEO4kVATA1f1Vf5/lVnbqiCLMJPVRZU6rWKjOGD28T/VRaIGywTV+mC0HvMbe4DlEd3dBwJZLIMUNvOPsj5Ua+l9IS4EoszFNAg6F5Lsyk=</Modulus><Exponent>AQAB</Exponent></RSAKeyValue></KeyValue></KeyInfo></Signature></b></a>";
                            bool validSignature = includeExtra == "" && includeID == "" && includeURI != "" && includeType == "";
                            Helpers.VerifyCryptoExceptionOnLoad(xml, includeExtra != "", validSignature, checkSignatureThrows: !SignatureSupport.SupportsRsaSha1Signatures);
                        }
        }

        [Fact]
        public static void DuplicateLegalAttributes()
        {
            foreach (string includeID in new string[] { "", $@" Id=""""", $@" Id="""" Id=""""" })
                foreach (string includeURI in new string[] { "", $@" URI=""""", $@" URI="""" URI=""""" })
                    foreach (string includeType in new string[] { "", $@" Type=""""", $@" Type="""" Type=""""" })
                    {
                        string xml = $@"<?xml version=""1.0"" encoding=""UTF-8""?><a><b xmlns:ns1=""http://www.contoso.com/"">X<Signature xmlns=""http://www.w3.org/2000/09/xmldsig#""><SignedInfo><CanonicalizationMethod Algorithm=""http://www.w3.org/TR/2001/REC-xml-c14n-20010315""/><SignatureMethod Algorithm=""http://www.w3.org/2000/09/xmldsig#rsa-sha1""/><Reference{includeID}{includeURI}{includeType}><Transforms><Transform Algorithm=""http://www.w3.org/2000/09/xmldsig#enveloped-signature""/><Transform Algorithm=""http://www.w3.org/TR/2001/REC-xml-c14n-20010315""/></Transforms><DigestMethod Algorithm=""http://www.w3.org/2000/09/xmldsig#sha1""/><DigestValue>ZVZLYkc1BAx+YtaqeYlxanb2cGI=</DigestValue></Reference></SignedInfo><SignatureValue>Kx8xs0of766gimu5girTqiTR5xoiWjN4XMx8uzDDhG70bIqpSzlhh6IA3iI54R5mpqCCPWrJJp85ps4jpQk8RGHe4KMejstbY6YXCfs7LtRPzkNzcoZB3vDbr3ijUSrbMk+0wTaZeyeYs8Z6cOicDIVN6bN6yC/Se5fbzTTCSmg=</SignatureValue><KeyInfo><KeyValue><RSAKeyValue><Modulus>ww2w+NbXwY/GRBZfFcXqrAM2X+P1NQoU+QEvgLO1izMTB8kvx1i/bodBvHTrKMwAMGEO4kVATA1f1Vf5/lVnbqiCLMJPVRZU6rWKjOGD28T/VRaIGywTV+mC0HvMbe4DlEd3dBwJZLIMUNvOPsj5Ua+l9IS4EoszFNAg6F5Lsyk=</Modulus><Exponent>AQAB</Exponent></RSAKeyValue></KeyValue></KeyInfo></Signature></b></a>";
                        bool throwsXmlException = includeID.LastIndexOf("I") != includeID.IndexOf("I") || includeURI.LastIndexOf("U") != includeURI.IndexOf("U") || includeType.LastIndexOf("T") != includeType.IndexOf("T");
                        if (throwsXmlException)
                            Assert.Throws<XmlException>(() => Helpers.VerifyCryptoExceptionOnLoad(xml, false, checkSignatureThrows: !SignatureSupport.SupportsRsaSha1Signatures));
                        else
                        {
                            bool validSignature = includeID == "" && includeURI != "" && includeType == "";
                            Helpers.VerifyCryptoExceptionOnLoad(xml, false, validSignature, checkSignatureThrows: !SignatureSupport.SupportsRsaSha1Signatures);
                        }
                    }
        }

        [Fact]
        public static void MissingAttribute_Transform()
        {
            string arbitraryData = @"<a:foo xmlns:a=""mynamespace"">lol</a:foo>";
            string xml = $@"<?xml version=""1.0"" encoding=""UTF-8""?><a><b xmlns:ns1=""http://www.contoso.com/"">X<Signature xmlns=""http://www.w3.org/2000/09/xmldsig#""><SignedInfo><CanonicalizationMethod Algorithm=""http://www.w3.org/TR/2001/REC-xml-c14n-20010315""/><SignatureMethod Algorithm=""http://www.w3.org/2000/09/xmldsig#rsa-sha1""/><Reference URI=""""><Transforms><Transform/><Transform Algorithm=""http://www.w3.org/TR/2001/REC-xml-c14n-20010315""/></Transforms><DigestMethod Algorithm=""http://www.w3.org/2000/09/xmldsig#sha1""/><DigestValue>ZVZLYkc1BAx+YtaqeYlxanb2cGI=</DigestValue><DigestValue>{arbitraryData}</DigestValue></Reference></SignedInfo><SignatureValue>Kx8xs0of766gimu5girTqiTR5xoiWjN4XMx8uzDDhG70bIqpSzlhh6IA3iI54R5mpqCCPWrJJp85ps4jpQk8RGHe4KMejstbY6YXCfs7LtRPzkNzcoZB3vDbr3ijUSrbMk+0wTaZeyeYs8Z6cOicDIVN6bN6yC/Se5fbzTTCSmg=</SignatureValue><KeyInfo><KeyValue><RSAKeyValue><Modulus>ww2w+NbXwY/GRBZfFcXqrAM2X+P1NQoU+QEvgLO1izMTB8kvx1i/bodBvHTrKMwAMGEO4kVATA1f1Vf5/lVnbqiCLMJPVRZU6rWKjOGD28T/VRaIGywTV+mC0HvMbe4DlEd3dBwJZLIMUNvOPsj5Ua+l9IS4EoszFNAg6F5Lsyk=</Modulus><Exponent>AQAB</Exponent></RSAKeyValue></KeyValue></KeyInfo></Signature></b></a>";
            Helpers.VerifyCryptoExceptionOnLoad(xml, true);
        }

        [Fact]
        public static void ExtraAttribute_Transform()
        {
            string arbitraryData = @"<a:foo xmlns:a=""mynamespace"">lol</a:foo>";
            string xml = $@"<?xml version=""1.0"" encoding=""UTF-8""?><a><b xmlns:ns1=""http://www.contoso.com/"">X<Signature xmlns=""http://www.w3.org/2000/09/xmldsig#""><SignedInfo><CanonicalizationMethod Algorithm=""http://www.w3.org/TR/2001/REC-xml-c14n-20010315""/><SignatureMethod Algorithm=""http://www.w3.org/2000/09/xmldsig#rsa-sha1""/><Reference URI=""""><Transforms  Algorithm=""http://www.w3.org/TR/2001/REC-xml-c14n-20010315""><Transform/><Transform Algorithm=""http://www.w3.org/TR/2001/REC-xml-c14n-20010315"" extraattr=""""/></Transforms><DigestMethod Algorithm=""http://www.w3.org/2000/09/xmldsig#sha1""/><DigestValue>ZVZLYkc1BAx+YtaqeYlxanb2cGI=</DigestValue><DigestValue>{arbitraryData}</DigestValue></Reference></SignedInfo><SignatureValue>Kx8xs0of766gimu5girTqiTR5xoiWjN4XMx8uzDDhG70bIqpSzlhh6IA3iI54R5mpqCCPWrJJp85ps4jpQk8RGHe4KMejstbY6YXCfs7LtRPzkNzcoZB3vDbr3ijUSrbMk+0wTaZeyeYs8Z6cOicDIVN6bN6yC/Se5fbzTTCSmg=</SignatureValue><KeyInfo><KeyValue><RSAKeyValue><Modulus>ww2w+NbXwY/GRBZfFcXqrAM2X+P1NQoU+QEvgLO1izMTB8kvx1i/bodBvHTrKMwAMGEO4kVATA1f1Vf5/lVnbqiCLMJPVRZU6rWKjOGD28T/VRaIGywTV+mC0HvMbe4DlEd3dBwJZLIMUNvOPsj5Ua+l9IS4EoszFNAg6F5Lsyk=</Modulus><Exponent>AQAB</Exponent></RSAKeyValue></KeyValue></KeyInfo></Signature></b></a>";
            Helpers.VerifyCryptoExceptionOnLoad(xml, true);
        }

        [Fact]
        public static void MissingAttribute_DigestMethod()
        {
            string arbitraryData = @"<a:foo xmlns:a=""mynamespace"">lol</a:foo>";
            string xml = $@"<?xml version=""1.0"" encoding=""UTF-8""?><a><b xmlns:ns1=""http://www.contoso.com/"">X<Signature xmlns=""http://www.w3.org/2000/09/xmldsig#""><SignedInfo><CanonicalizationMethod Algorithm=""http://www.w3.org/TR/2001/REC-xml-c14n-20010315""/><SignatureMethod Algorithm=""http://www.w3.org/2000/09/xmldsig#rsa-sha1""/><Reference URI=""""><Transforms><Transform Algorithm=""http://www.w3.org/TR/2001/REC-xml-c14n-20010315""/></Transforms><DigestMethod /><DigestValue>ZVZLYkc1BAx+YtaqeYlxanb2cGI=</DigestValue><DigestValue>{arbitraryData}</DigestValue></Reference></SignedInfo><SignatureValue>Kx8xs0of766gimu5girTqiTR5xoiWjN4XMx8uzDDhG70bIqpSzlhh6IA3iI54R5mpqCCPWrJJp85ps4jpQk8RGHe4KMejstbY6YXCfs7LtRPzkNzcoZB3vDbr3ijUSrbMk+0wTaZeyeYs8Z6cOicDIVN6bN6yC/Se5fbzTTCSmg=</SignatureValue><KeyInfo><KeyValue><RSAKeyValue><Modulus>ww2w+NbXwY/GRBZfFcXqrAM2X+P1NQoU+QEvgLO1izMTB8kvx1i/bodBvHTrKMwAMGEO4kVATA1f1Vf5/lVnbqiCLMJPVRZU6rWKjOGD28T/VRaIGywTV+mC0HvMbe4DlEd3dBwJZLIMUNvOPsj5Ua+l9IS4EoszFNAg6F5Lsyk=</Modulus><Exponent>AQAB</Exponent></RSAKeyValue></KeyValue></KeyInfo></Signature></b></a>";
            Helpers.VerifyCryptoExceptionOnLoad(xml, true);
        }

        [Fact]
        public static void ExtraAttribute_DigestMethod()
        {
            string arbitraryData = @"<a:foo xmlns:a=""mynamespace"">lol</a:foo>";
            string xml = $@"<?xml version=""1.0"" encoding=""UTF-8""?><a><b xmlns:ns1=""http://www.contoso.com/"">X<Signature xmlns=""http://www.w3.org/2000/09/xmldsig#""><SignedInfo><CanonicalizationMethod Algorithm=""http://www.w3.org/TR/2001/REC-xml-c14n-20010315""/><SignatureMethod Algorithm=""http://www.w3.org/2000/09/xmldsig#rsa-sha1""/><Reference URI=""""><Transforms Algorithm=""http://www.w3.org/TR/2001/REC-xml-c14n-20010315""><Transform/><Transform Algorithm=""http://www.w3.org/TR/2001/REC-xml-c14n-20010315""/></Transforms><DigestMethod Algorithm=""http://www.w3.org/2000/09/xmldsig#sha1"" extraattr=""""/><DigestValue>ZVZLYkc1BAx+YtaqeYlxanb2cGI=</DigestValue><DigestValue>{arbitraryData}</DigestValue></Reference></SignedInfo><SignatureValue>Kx8xs0of766gimu5girTqiTR5xoiWjN4XMx8uzDDhG70bIqpSzlhh6IA3iI54R5mpqCCPWrJJp85ps4jpQk8RGHe4KMejstbY6YXCfs7LtRPzkNzcoZB3vDbr3ijUSrbMk+0wTaZeyeYs8Z6cOicDIVN6bN6yC/Se5fbzTTCSmg=</SignatureValue><KeyInfo><KeyValue><RSAKeyValue><Modulus>ww2w+NbXwY/GRBZfFcXqrAM2X+P1NQoU+QEvgLO1izMTB8kvx1i/bodBvHTrKMwAMGEO4kVATA1f1Vf5/lVnbqiCLMJPVRZU6rWKjOGD28T/VRaIGywTV+mC0HvMbe4DlEd3dBwJZLIMUNvOPsj5Ua+l9IS4EoszFNAg6F5Lsyk=</Modulus><Exponent>AQAB</Exponent></RSAKeyValue></KeyValue></KeyInfo></Signature></b></a>";
            Helpers.VerifyCryptoExceptionOnLoad(xml, true);
        }
    }
}
