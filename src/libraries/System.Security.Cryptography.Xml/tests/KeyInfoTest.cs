// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// KeyInfoTest.cs - Test Cases for KeyInfo
//
// Author:
//  Sebastien Pouliot <sebastien@ximian.com>
//
// (C) 2002, 2003 Motus Technologies Inc. (http://www.motus.com)
// Copyright (C) 2004-2005 Novell, Inc (http://www.novell.com)

using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Xml;
using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.Xml.Tests
{

    public class KeyInfoTest
    {

        private KeyInfo info;

        public KeyInfoTest()
        {
            info = new KeyInfo();
        }

        [Fact]
        public void EmptyKeyInfo()
        {
            Assert.Equal("<KeyInfo xmlns=\"http://www.w3.org/2000/09/xmldsig#\" />", (info.GetXml().OuterXml));
            Assert.Equal(0, info.Count);
        }

        [Fact]
        public void KeyInfoName()
        {
            KeyInfoName name = new KeyInfoName();
            name.Value = "Mono::";
            info.AddClause(name);
            Assert.Equal("<KeyInfo xmlns=\"http://www.w3.org/2000/09/xmldsig#\"><KeyName>Mono::</KeyName></KeyInfo>", (info.GetXml().OuterXml));
            Assert.Equal(1, info.Count);
        }

        [Fact]
        public void KeyInfoNode()
        {
            string test = "<Test>KeyInfoNode</Test>";
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(test);

            KeyInfoNode node = new KeyInfoNode(doc.DocumentElement);
            info.AddClause(node);
            Assert.Equal("<KeyInfo xmlns=\"http://www.w3.org/2000/09/xmldsig#\"><Test xmlns=\"\">KeyInfoNode</Test></KeyInfo>", (info.GetXml().OuterXml));
            Assert.Equal(1, info.Count);
        }

        private static string dsaP = "rjxsMU368YOCTQejWkiuO9e/vUVwkLtq1jKiU3TtJ53hBJqjFRuTa228vZe+BH2su9RPn/vYFWfQDv6zgBYe3eNdu4Afw+Ny0FatX6dl3E77Ra6Tsd3MmLXBiGSQ1mMNd5G2XQGpbt9zsGlUaexXekeMLxIufgfZLwYp67M+2WM=";
        private static string dsaQ = "tf0K9rMyvUrU4cIkwbCrDRhQAJk=";
        private static string dsaG = "S8Z+1pGCed00w6DtVcqZLKjfqlCJ7JsugEFIgSy/Vxtu9YGCMclV4ijGEbPo/jU8YOSMuD7E9M7UaopMRcmKQjoKZzoJjkgVFP48Ohxl1f08lERnButsxanx3+OstFwUGQ8XNaGg3KrIoZt1FUnfxN3RHHTvVhjzNSHxMGULGaU=";
        private static string dsaY = "LnrxxRGLYeV2XLtK3SYz8RQHlHFZYrtznDZyMotuRfO5uC5YODhSFyLXvb1qB3WeGtF4h3Eo4KzHgMgfN2ZMlffxFRhJgTtH3ctbL8lfQoDkjeiPPnYGhspdJxr0tyZmiy0gkjJG3vwHYrLnvZWx9Wm/unqiOlGBPNuxJ+hOeP8=";
        //private static string dsaJ = "9RhE5TycDtdEIXxS3HfxFyXYgpy81zY5lVjwD6E9JP37MWEi80BlX6ab1YPm6xYSEoqReMPP9RgGiW6DuACpgI7+8vgCr4i/7VhzModJAA56PwvTu6UMt9xxKU/fT672v8ucREkMWoc7lEey";
        //private static string dsaSeed = "HxW3N4RHWVgqDQKuGg7iJTUTiCs=";
        //private static string dsaPgenCounter = "Asw=";
        // private static string xmlDSA = "<DSAKeyValue><P>" + dsaP + "</P><Q>" + dsaQ + "</Q><G>" + dsaG + "</G><Y>" + dsaY + "</Y><J>" + dsaJ + "</J><Seed>" + dsaSeed + "</Seed><PgenCounter>" + dsaPgenCounter + "</PgenCounter></DSAKeyValue>";
        private static string xmlDSA = "<DSAKeyValue><P>" + dsaP + "</P><Q>" + dsaQ + "</Q><G>" + dsaG + "</G><Y>" + dsaY + "</Y></DSAKeyValue>";

        [Fact]
        [SkipOnPlatform(PlatformSupport.MobileAppleCrypto, "DSA is not available")]
        public void DSAKeyValue()
        {
            using (DSA key = DSA.Create())
            {
                key.ImportParameters(new DSAParameters {
                    P = Convert.FromBase64String(dsaP),
                    Q = Convert.FromBase64String(dsaQ),
                    G = Convert.FromBase64String(dsaG),
                    Y = Convert.FromBase64String(dsaY),
                    //J = Convert.FromBase64String(dsaJ),
                    //Seed = Convert.FromBase64String(dsaSeed),
                    //Counter = BitConverter.ToUInt16(Convert.FromBase64String(dsaPgenCounter), 0)
                });
                DSAKeyValue dsa = new DSAKeyValue(key);
                info.AddClause(dsa);
                AssertCrypto.AssertXmlEquals("dsa",
                    "<KeyInfo xmlns=\"http://www.w3.org/2000/09/xmldsig#\"><KeyValue xmlns=\"http://www.w3.org/2000/09/xmldsig#\">" +
                    xmlDSA + "</KeyValue></KeyInfo>", (info.GetXml().OuterXml));
                Assert.Equal(1, info.Count);
            }
        }

        private static string rsaModulus = "9DC4XNdQJwMRnz5pP2a6U51MHCODRilaIoVXqUPhCUb0lJdGroeqVYT84ZyIVrcarzD7Tqs3aEOIa3rKox0N1bxQpZPqayVQeLAkjLLtzJW/ScRJx3uEDJdgT1JnM1FH0GZTinmEdCUXdLc7+Y/c/qqIkTfbwHbRZjW0bBJyExM=";
        private static string rsaExponent = "AQAB";
        private static string xmlRSA = "<RSAKeyValue><Modulus>" + rsaModulus + "</Modulus><Exponent>" + rsaExponent + "</Exponent></RSAKeyValue>";

        [Fact]
        public void RSAKeyValue()
        {
            using (RSA key = RSA.Create())
            {
                key.ImportParameters(new RSAParameters()
                {
                    Modulus = Convert.FromBase64String(rsaModulus),
                    Exponent = Convert.FromBase64String(rsaExponent)
                });
                RSAKeyValue rsa = new RSAKeyValue(key);
                info.AddClause(rsa);
                AssertCrypto.AssertXmlEquals("rsa",
                    "<KeyInfo xmlns=\"http://www.w3.org/2000/09/xmldsig#\"><KeyValue xmlns=\"http://www.w3.org/2000/09/xmldsig#\">" +
                    xmlRSA + "</KeyValue></KeyInfo>", (info.GetXml().OuterXml));
                Assert.Equal(1, info.Count);
            }
        }

        [Fact]
        public void RetrievalMethod()
        {
            KeyInfoRetrievalMethod retrieval = new KeyInfoRetrievalMethod();
            retrieval.Uri = "http://www.go-mono.org/";
            info.AddClause(retrieval);
            Assert.Equal("<KeyInfo xmlns=\"http://www.w3.org/2000/09/xmldsig#\"><RetrievalMethod URI=\"http://www.go-mono.org/\" /></KeyInfo>", (info.GetXml().OuterXml));
            Assert.Equal(1, info.Count);
        }

        static byte[] cert = { 0x30,0x82,0x02,0x1D,0x30,0x82,0x01,0x86,0x02,0x01,0x14,0x30,0x0D,0x06,0x09,0x2A,0x86,0x48,0x86,0xF7,0x0D,0x01,0x01,0x04,0x05,0x00,0x30,0x58,0x31,0x0B,0x30,0x09,0x06,0x03,0x55,0x04,0x06,0x13,0x02,0x43,0x41,0x31,0x1F,0x30,0x1D,0x06,0x03,0x55,0x04,0x03,0x13,0x16,0x4B,0x65,0x79,0x77,0x69,0x74,0x6E,0x65,0x73,0x73,0x20,0x43,0x61,0x6E,0x61,0x64,0x61,0x20,0x49,0x6E,0x63,0x2E,0x31,0x28,0x30,0x26,0x06,0x0A,0x2B,0x06,0x01,0x04,0x01,0x2A,0x02,0x0B,0x02,0x01,0x13,0x18,0x6B,0x65,0x79,0x77,0x69,0x74,0x6E,0x65,0x73,
            0x73,0x40,0x6B,0x65,0x79,0x77,0x69,0x74,0x6E,0x65,0x73,0x73,0x2E,0x63,0x61,0x30,0x1E,0x17,0x0D,0x39,0x36,0x30,0x35,0x30,0x37,0x30,0x30,0x30,0x30,0x30,0x30,0x5A,0x17,0x0D,0x39,0x39,0x30,0x35,0x30,0x37,0x30,0x30,0x30,0x30,0x30,0x30,0x5A,0x30,0x58,0x31,0x0B,0x30,0x09,0x06,0x03,0x55,0x04,0x06,0x13,0x02,0x43,0x41,0x31,0x1F,0x30,0x1D,0x06,0x03,0x55,0x04,0x03,0x13,0x16,0x4B,0x65,0x79,0x77,0x69,0x74,0x6E,0x65,0x73,0x73,0x20,0x43,0x61,0x6E,0x61,0x64,0x61,0x20,0x49,0x6E,0x63,0x2E,0x31,0x28,0x30,0x26,0x06,
            0x0A,0x2B,0x06,0x01,0x04,0x01,0x2A,0x02,0x0B,0x02,0x01,0x13,0x18,0x6B,0x65,0x79,0x77,0x69,0x74,0x6E,0x65,0x73,0x73,0x40,0x6B,0x65,0x79,0x77,0x69,0x74,0x6E,0x65,0x73,0x73,0x2E,0x63,0x61,0x30,0x81,0x9D,0x30,0x0D,0x06,0x09,0x2A,0x86,0x48,0x86,0xF7,0x0D,0x01,0x01,0x01,0x05,0x00,0x03,0x81,0x8B,0x00,0x30,0x81,0x87,0x02,0x81,0x81,0x00,0xCD,0x23,0xFA,0x2A,0xE1,0xED,0x98,0xF4,0xE9,0xD0,0x93,0x3E,0xD7,0x7A,0x80,0x02,0x4C,0xCC,0xC1,0x02,0xAF,0x5C,0xB6,0x1F,0x7F,0xFA,0x57,0x42,0x6F,0x30,0xD1,0x20,0xC5,0xB5,
            0x21,0x07,0x40,0x2C,0xA9,0x86,0xC2,0xF3,0x64,0x84,0xAE,0x3D,0x85,0x2E,0xED,0x85,0xBD,0x54,0xB0,0x18,0x28,0xEF,0x6A,0xF8,0x1B,0xE7,0x0B,0x16,0x1F,0x93,0x25,0x4F,0xC7,0xF8,0x8E,0xC3,0xB9,0xCA,0x98,0x84,0x0E,0x55,0xD0,0x2F,0xEF,0x78,0x77,0xC5,0x72,0x28,0x5F,0x60,0xBF,0x19,0x2B,0xD1,0x72,0xA2,0xB7,0xD8,0x3F,0xE0,0x97,0x34,0x5A,0x01,0xBD,0x04,0x9C,0xC8,0x78,0x45,0xCD,0x93,0x8D,0x15,0xF2,0x76,0x10,0x11,0xAB,0xB8,0x5B,0x2E,0x9E,0x52,0xDD,0x81,0x3E,0x9C,0x64,0xC8,0x29,0x93,0x02,0x01,0x03,0x30,0x0D,0x06,
            0x09,0x2A,0x86,0x48,0x86,0xF7,0x0D,0x01,0x01,0x04,0x05,0x00,0x03,0x81,0x81,0x00,0x32,0x1A,0x35,0xBA,0xBF,0x43,0x27,0xD6,0xB4,0xD4,0xB8,0x76,0xE5,0xE3,0x9B,0x4D,0x6C,0xC0,0x86,0xC9,0x77,0x35,0xBA,0x6B,0x16,0x2D,0x13,0x46,0x4A,0xB0,0x32,0x53,0xA1,0x5B,0x5A,0xE9,0x99,0xE2,0x0C,0x86,0x88,0x17,0x4E,0x0D,0xFE,0x82,0xAC,0x4E,0x47,0xEF,0xFB,0xFF,0x39,0xAC,0xEE,0x35,0xC8,0xFA,0x52,0x37,0x0A,0x49,0xAD,0x59,0xAD,0xE2,0x8A,0xA9,0x1C,0xC6,0x5F,0x1F,0xF8,0x6F,0x73,0x7E,0xCD,0xA0,0x31,0xE8,0x0C,0xBE,0xF5,0x4D,
            0xD9,0xB2,0xAB,0x8A,0x12,0xB6,0x30,0x78,0x68,0x11,0x7C,0x0D,0xF1,0x49,0x4D,0xA3,0xFD,0xB2,0xE9,0xFF,0x1D,0xF0,0x91,0xFA,0x54,0x85,0xFF,0x33,0x90,0xE8,0xC1,0xBF,0xA4,0x9B,0xA4,0x62,0x46,0xBD,0x61,0x12,0x59,0x98,0x41,0x89 };

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/51370", TestPlatforms.iOS | TestPlatforms.tvOS | TestPlatforms.MacCatalyst)]
        public void X509Data()
        {
            using (X509Certificate x509 = new X509Certificate(cert))
            {
                KeyInfoX509Data x509data = new KeyInfoX509Data(x509);
                info.AddClause(x509data);
                AssertCrypto.AssertXmlEquals("X509Data",
                    "<KeyInfo xmlns=\"http://www.w3.org/2000/09/xmldsig#\"><X509Data xmlns=\"http://www.w3.org/2000/09/xmldsig#\"><X509Certificate>MIICHTCCAYYCARQwDQYJKoZIhvcNAQEEBQAwWDELMAkGA1UEBhMCQ0ExHzAdBgNVBAMTFktleXdpdG5lc3MgQ2FuYWRhIEluYy4xKDAmBgorBgEEASoCCwIBExhrZXl3aXRuZXNzQGtleXdpdG5lc3MuY2EwHhcNOTYwNTA3MDAwMDAwWhcNOTkwNTA3MDAwMDAwWjBYMQswCQYDVQQGEwJDQTEfMB0GA1UEAxMWS2V5d2l0bmVzcyBDYW5hZGEgSW5jLjEoMCYGCisGAQQBKgILAgETGGtleXdpdG5lc3NAa2V5d2l0bmVzcy5jYTCBnTANBgkqhkiG9w0BAQEFAAOBiwAwgYcCgYEAzSP6KuHtmPTp0JM+13qAAkzMwQKvXLYff/pXQm8w0SDFtSEHQCyphsLzZISuPYUu7YW9VLAYKO9q+BvnCxYfkyVPx/iOw7nKmIQOVdAv73h3xXIoX2C/GSvRcqK32D/glzRaAb0EnMh4Rc2TjRXydhARq7hbLp5S3YE+nGTIKZMCAQMwDQYJKoZIhvcNAQEEBQADgYEAMho1ur9DJ9a01Lh25eObTWzAhsl3NbprFi0TRkqwMlOhW1rpmeIMhogXTg3+gqxOR+/7/zms7jXI+lI3CkmtWa3iiqkcxl8f+G9zfs2gMegMvvVN2bKrihK2MHhoEXwN8UlNo/2y6f8d8JH6VIX/M5Dowb+km6RiRr1hElmYQYk=</X509Certificate></X509Data></KeyInfo>",
                    (info.GetXml().OuterXml));
                Assert.Equal(1, info.Count);
            }
        }

        [Fact]
        [SkipOnPlatform(PlatformSupport.MobileAppleCrypto, "DSA is not available")]
        public void Complex()
        {
            KeyInfoName name = new KeyInfoName();
            name.Value = "CoreFx::";
            info.AddClause(name);

            using (DSA keyDSA = DSA.Create())
            {
                keyDSA.ImportParameters(new DSAParameters
                {
                    P = Convert.FromBase64String(dsaP),
                    Q = Convert.FromBase64String(dsaQ),
                    G = Convert.FromBase64String(dsaG),
                    Y = Convert.FromBase64String(dsaY),
                });
                DSAKeyValue dsa = new DSAKeyValue(keyDSA);
                info.AddClause(dsa);

                using (RSA keyRSA = RSA.Create())
                {
                    keyRSA.ImportParameters(new RSAParameters()
                    {
                        Modulus = Convert.FromBase64String(rsaModulus),
                        Exponent = Convert.FromBase64String(rsaExponent)
                    });
                    RSAKeyValue rsa = new RSAKeyValue(keyRSA);
                    info.AddClause(rsa);

                    KeyInfoRetrievalMethod retrieval = new KeyInfoRetrievalMethod();
                    retrieval.Uri = "https://github.com/dotnet/corefx";
                    info.AddClause(retrieval);

                    using (X509Certificate x509 = new X509Certificate(cert))
                    {
                        KeyInfoX509Data x509data = new KeyInfoX509Data(x509);
                        info.AddClause(x509data);

                        string s = "<KeyInfo xmlns=\"http://www.w3.org/2000/09/xmldsig#\"><KeyName>CoreFx::</KeyName><KeyValue xmlns=\"http://www.w3.org/2000/09/xmldsig#\"><DSAKeyValue><P>rjxsMU368YOCTQejWkiuO9e/vUVwkLtq1jKiU3TtJ53hBJqjFRuTa228vZe+BH2su9RPn/vYFWfQDv6zgBYe3eNdu4Afw+Ny0FatX6dl3E77Ra6Tsd3MmLXBiGSQ1mMNd5G2XQGpbt9zsGlUaexXekeMLxIufgfZLwYp67M+2WM=</P><Q>tf0K9rMyvUrU4cIkwbCrDRhQAJk=</Q><G>S8Z+1pGCed00w6DtVcqZLKjfqlCJ7JsugEFIgSy/Vxtu9YGCMclV4ijGEbPo/jU8YOSMuD7E9M7UaopMRcmKQjoKZzoJjkgVFP48Ohxl1f08lERnButsxanx3+OstFwUGQ8XNaGg3KrIoZt1FUnfxN3RHHTvVhjzNSHxMGULGaU=</G><Y>LnrxxRGLYeV2XLtK3SYz8RQHlHFZYrtznDZyMotuRfO5uC5YODhSFyLXvb1qB3WeGtF4h3Eo4KzHgMgfN2ZMlffxFRhJgTtH3ctbL8lfQoDkjeiPPnYGhspdJxr0tyZmiy0gkjJG3vwHYrLnvZWx9Wm/unqiOlGBPNuxJ+hOeP8=</Y></DSAKeyValue></KeyValue>";
                        s += "<KeyValue xmlns=\"http://www.w3.org/2000/09/xmldsig#\"><RSAKeyValue><Modulus>9DC4XNdQJwMRnz5pP2a6U51MHCODRilaIoVXqUPhCUb0lJdGroeqVYT84ZyIVrcarzD7Tqs3aEOIa3rKox0N1bxQpZPqayVQeLAkjLLtzJW/ScRJx3uEDJdgT1JnM1FH0GZTinmEdCUXdLc7+Y/c/qqIkTfbwHbRZjW0bBJyExM=</Modulus><Exponent>AQAB</Exponent></RSAKeyValue></KeyValue>";
                        s += "<RetrievalMethod URI=\"https://github.com/dotnet/corefx\" />";
                        s += "<X509Data xmlns=\"http://www.w3.org/2000/09/xmldsig#\">";
                        s += "<X509Certificate>MIICHTCCAYYCARQwDQYJKoZIhvcNAQEEBQAwWDELMAkGA1UEBhMCQ0ExHzAdBgNVBAMTFktleXdpdG5lc3MgQ2FuYWRhIEluYy4xKDAmBgorBgEEASoCCwIBExhrZXl3aXRuZXNzQGtleXdpdG5lc3MuY2EwHhcNOTYwNTA3MDAwMDAwWhcNOTkwNTA3MDAwMDAwWjBYMQswCQYDVQQGEwJDQTEfMB0GA1UEAxMWS2V5d2l0bmVzcyBDYW5hZGEgSW5jLjEoMCYGCisGAQQBKgILAgETGGtleXdpdG5lc3NAa2V5d2l0bmVzcy5jYTCBnTANBgkqhkiG9w0BAQEFAAOBiwAwgYcCgYEAzSP6KuHtmPTp0JM+13qAAkzMwQKvXLYff/pXQm8w0SDFtSEHQCyphsLzZISuPYUu7YW9VLAYKO9q+BvnCxYfkyVPx/iOw7nKmIQOVdAv73h3xXIoX2C/GSvRcqK32D/glzRaAb0EnMh4Rc2TjRXydhARq7hbLp5S3YE+nGTIKZMCAQMwDQYJKoZIhvcNAQEEBQADgYEAMho1ur9DJ9a01Lh25eObTWzAhsl3NbprFi0TRkqwMlOhW1rpmeIMhogXTg3+gqxOR+/7/zms7jXI+lI3CkmtWa3iiqkcxl8f+G9zfs2gMegMvvVN2bKrihK2MHhoEXwN8UlNo/2y6f8d8JH6VIX/M5Dowb+km6RiRr1hElmYQYk=</X509Certificate></X509Data></KeyInfo>";
                        AssertCrypto.AssertXmlEquals("Complex", s, (info.GetXml().OuterXml));
                        Assert.Equal(5, info.Count);
                    }
                }
            }
        }

        [Fact]
        [SkipOnPlatform(PlatformSupport.MobileAppleCrypto, "DSA is not available")]
        public void ImportKeyNode()
        {
            string keyName = "Mono::";
            string dsaP = "rjxsMU368YOCTQejWkiuO9e/vUVwkLtq1jKiU3TtJ53hBJqjFRuTa228vZe+BH2su9RPn/vYFWfQDv6zgBYe3eNdu4Afw+Ny0FatX6dl3E77Ra6Tsd3MmLXBiGSQ1mMNd5G2XQGpbt9zsGlUaexXekeMLxIufgfZLwYp67M+2WM=";
            string dsaQ = "tf0K9rMyvUrU4cIkwbCrDRhQAJk=";
            string dsaG = "S8Z+1pGCed00w6DtVcqZLKjfqlCJ7JsugEFIgSy/Vxtu9YGCMclV4ijGEbPo/jU8YOSMuD7E9M7UaopMRcmKQjoKZzoJjkgVFP48Ohxl1f08lERnButsxanx3+OstFwUGQ8XNaGg3KrIoZt1FUnfxN3RHHTvVhjzNSHxMGULGaU=";
            string dsaY = "LnrxxRGLYeV2XLtK3SYz8RQHlHFZYrtznDZyMotuRfO5uC5YODhSFyLXvb1qB3WeGtF4h3Eo4KzHgMgfN2ZMlffxFRhJgTtH3ctbL8lfQoDkjeiPPnYGhspdJxr0tyZmiy0gkjJG3vwHYrLnvZWx9Wm/unqiOlGBPNuxJ+hOeP8=";
            string dsaJ = "9RhE5TycDtdEIXxS3HfxFyXYgpy81zY5lVjwD6E9JP37MWEi80BlX6ab1YPm6xYSEoqReMPP9RgGiW6DuACpgI7+8vgCr4i/7VhzModJAA56PwvTu6UMt9xxKU/fT672v8ucREkMWoc7lEey";
            string dsaSeed = "HxW3N4RHWVgqDQKuGg7iJTUTiCs=";
            string dsaPgenCounter = "Asw=";
            string rsaModulus = "9DC4XNdQJwMRnz5pP2a6U51MHCODRilaIoVXqUPhCUb0lJdGroeqVYT84ZyIVrcarzD7Tqs3aEOIa3rKox0N1bxQpZPqayVQeLAkjLLtzJW/ScRJx3uEDJdgT1JnM1FH0GZTinmEdCUXdLc7+Y/c/qqIkTfbwHbRZjW0bBJyExM=";
            string rsaExponent = "AQAB";
            string x509cert = "MIICHTCCAYYCARQwDQYJKoZIhvcNAQEEBQAwWDELMAkGA1UEBhMCQ0ExHzAdBgNVBAMTFktleXdpdG5lc3MgQ2FuYWRhIEluYy4xKDAmBgorBgEEASoCCwIBExhrZXl3aXRuZXNzQGtleXdpdG5lc3MuY2EwHhcNOTYwNTA3MDAwMDAwWhcNOTkwNTA3MDAwMDAwWjBYMQswCQYDVQQGEwJDQTEfMB0GA1UEAxMWS2V5d2l0bmVzcyBDYW5hZGEgSW5jLjEoMCYGCisGAQQBKgILAgETGGtleXdpdG5lc3NAa2V5d2l0bmVzcy5jYTCBnTANBgkqhkiG9w0BAQEFAAOBiwAwgYcCgYEAzSP6KuHtmPTp0JM+13qAAkzMwQKvXLYff/pXQm8w0SDFtSEHQCyphsLzZISuPYUu7YW9VLAYKO9q+BvnCxYfkyVPx/iOw7nKmIQOVdAv73h3xXIoX2C/GSvRcqK32D/glzRaAb0EnMh4Rc2TjRXydhARq7hbLp5S3YE+nGTIKZMCAQMwDQYJKoZIhvcNAQEEBQADgYEAMho1ur9DJ9a01Lh25eObTWzAhsl3NbprFi0TRkqwMlOhW1rpmeIMhogXTg3+gqxOR+/7/zms7jXI+lI3CkmtWa3iiqkcxl8f+G9zfs2gMegMvvVN2bKrihK2MHhoEXwN8UlNo/2y6f8d8JH6VIX/M5Dowb+km6RiRr1hElmYQYk=";
            string retrievalElementUri = @"http://www.go-mono.org/";

            string value = $@"<KeyInfo xmlns=""http://www.w3.org/2000/09/xmldsig#"">
                <KeyName>{keyName}</KeyName>
                <KeyValue xmlns=""http://www.w3.org/2000/09/xmldsig#"">
                    <DSAKeyValue>
                        <P>{dsaP}</P>
                        <Q>{dsaQ}</Q>
                        <G>{dsaG}</G>
                        <Y>{dsaY}</Y>
                        <J>{dsaJ}</J>
                        <Seed>{dsaSeed}</Seed>
                        <PgenCounter>{dsaPgenCounter}</PgenCounter>
                    </DSAKeyValue>
                </KeyValue>
                <KeyValue xmlns=""http://www.w3.org/2000/09/xmldsig#"">
                    <RSAKeyValue>
                        <Modulus>{rsaModulus}</Modulus>
                        <Exponent>{rsaExponent}</Exponent>
                    </RSAKeyValue>
                </KeyValue>
                <RetrievalElement URI=""{retrievalElementUri}"" />
                <X509Data xmlns=""http://www.w3.org/2000/09/xmldsig#"">
                    <X509Certificate>{x509cert}</X509Certificate>
                </X509Data>
            </KeyInfo>";

            XmlDocument doc = new XmlDocument();
            doc.LoadXml(value);
            info.LoadXml(doc.DocumentElement);

            Assert.Equal(5, info.Count);
            int i = 0;
            int pathsCovered = 0;
            foreach (var clause in info)
            {
                i++;

                if (clause is KeyInfoName)
                {
                    pathsCovered |= 1 << 0;

                    var name = clause as KeyInfoName;
                    Assert.Equal(keyName, name.Value);
                }
                else if (clause is DSAKeyValue)
                {
                    pathsCovered |= 1 << 1;

                    var dsaKV = clause as DSAKeyValue;
                    DSA dsaKey = dsaKV.Key;
                    DSAParameters dsaParams = dsaKey.ExportParameters(false);

                    Assert.Equal(Convert.FromBase64String(dsaP), dsaParams.P);
                    Assert.Equal(Convert.FromBase64String(dsaQ), dsaParams.Q);
                    Assert.Equal(Convert.FromBase64String(dsaG), dsaParams.G);
                    Assert.Equal(Convert.FromBase64String(dsaY), dsaParams.Y);

                    // J is an optimization it should either be null or correct value
                    if (dsaParams.J != null)
                    {
                        Assert.Equal(Convert.FromBase64String(dsaJ), dsaParams.J);
                    }

                    // Seed and Counter are not guaranteed to roundtrip
                    // they should either both be non-null or both null
                    if (dsaParams.Seed != null)
                    {
                        Assert.Equal(Convert.FromBase64String(dsaSeed), dsaParams.Seed);

                        byte[] counter = Convert.FromBase64String(dsaPgenCounter);
                        Assert.InRange(counter.Length, 1, 4);
                        int counterVal = 0;
                        for (int j = 0; j < counter.Length; j++)
                        {
                            counterVal <<= 8;
                            counterVal |= counter[j];
                        }

                        Assert.Equal(counterVal, dsaParams.Counter);
                    }
                    else
                    {
                        Assert.Null(dsaParams.Seed);
                        Assert.Equal(default(int), dsaParams.Counter);
                    }
                }
                else if (clause is RSAKeyValue)
                {
                    pathsCovered |= 1 << 2;

                    var rsaKV = clause as RSAKeyValue;
                    RSA rsaKey = rsaKV.Key;
                    RSAParameters rsaParameters = rsaKey.ExportParameters(false);

                    Assert.Equal(Convert.FromBase64String(rsaModulus), rsaParameters.Modulus);
                    Assert.Equal(Convert.FromBase64String(rsaExponent), rsaParameters.Exponent);
                }
                else if (clause is KeyInfoNode)
                {
                    pathsCovered |= 1 << 3;

                    var keyInfo = clause as KeyInfoNode;
                    XmlElement keyInfoEl = keyInfo.GetXml();
                    Assert.Equal("RetrievalElement", keyInfoEl.LocalName);
                    Assert.Equal("http://www.w3.org/2000/09/xmldsig#", keyInfoEl.NamespaceURI);
                    Assert.Equal(1, keyInfoEl.Attributes.Count);
                    Assert.Equal("URI", keyInfoEl.Attributes[0].Name);
                    Assert.Equal(retrievalElementUri, keyInfoEl.GetAttribute("URI"));
                }
                else if (clause is KeyInfoX509Data)
                {
                    pathsCovered |= 1 << 4;

                    var x509data = clause as KeyInfoX509Data;
                    Assert.Equal(1, x509data.Certificates.Count);
                    X509Certificate cert = x509data.Certificates[0] as X509Certificate;
                    Assert.NotNull(cert);
                    Assert.Equal(Convert.FromBase64String(x509cert), cert.GetRawCertData());
                }
                else
                {
                    Assert.Fail($"Unexpected clause type: {clause.GetType().FullName}");
                }
            }

            // 0x1f = b11111, number of ones = 5
            Assert.Equal(0x1f, pathsCovered);
            Assert.Equal(5, i);
        }

        [Fact]
        public void NullClause()
        {
            Assert.Equal(0, info.Count);
            // null is accepted...
            info.AddClause(null);
            Assert.Equal(1, info.Count);
            // but can't get XML out if it!
            Assert.Throws<NullReferenceException>(() => info.GetXml());
        }

        [Fact]
        public void NullXml()
        {
            Assert.Throws<ArgumentNullException>(() => info.LoadXml(null));
        }

        [Fact]
        public void InvalidXml()
        {
            string bad = "<Test></Test>";
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(bad);
            info.LoadXml(doc.DocumentElement);
            // no exception but Xml isn't loaded
            Assert.Equal("<KeyInfo xmlns=\"http://www.w3.org/2000/09/xmldsig#\" />", (info.GetXml().OuterXml));
            Assert.Equal(0, info.Count);
        }
    }
}
