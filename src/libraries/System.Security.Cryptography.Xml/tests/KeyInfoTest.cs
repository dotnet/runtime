//
// KeyInfoTest.cs - NUnit Test Cases for KeyInfo
//
// Author:
//	Sebastien Pouliot <sebastien@ximian.com>
//
// (C) 2002, 2003 Motus Technologies Inc. (http://www.motus.com)
// Copyright (C) 2004-2005 Novell, Inc (http://www.novell.com)
//

using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.Xml;

using NUnit.Framework;

namespace MonoTests.System.Security.Cryptography.Xml {

	[TestFixture]
	public class KeyInfoTest {

		private KeyInfo info;

		[SetUp]
		public void SetUp () 
		{
			info = new KeyInfo ();
		}

		[Test]
		public void EmptyKeyInfo () 
		{
			Assert.AreEqual ("<KeyInfo xmlns=\"http://www.w3.org/2000/09/xmldsig#\" />", (info.GetXml ().OuterXml), "empty");
			Assert.AreEqual (0, info.Count, "empty count");
		}

		[Test]
		public void KeyInfoName () 
		{
			KeyInfoName name = new KeyInfoName ();
			name.Value = "Mono::";
			info.AddClause (name);
			Assert.AreEqual ("<KeyInfo xmlns=\"http://www.w3.org/2000/09/xmldsig#\"><KeyName>Mono::</KeyName></KeyInfo>", (info.GetXml ().OuterXml), "name");
			Assert.AreEqual (1, info.Count, "name count");
		}

		[Test]
		public void KeyInfoNode () 
		{
			string test = "<Test>KeyInfoNode</Test>";
			XmlDocument doc = new XmlDocument ();
			doc.LoadXml (test);

			KeyInfoNode node = new KeyInfoNode (doc.DocumentElement);
			info.AddClause (node);
			Assert.AreEqual ("<KeyInfo xmlns=\"http://www.w3.org/2000/09/xmldsig#\"><Test xmlns=\"\">KeyInfoNode</Test></KeyInfo>", (info.GetXml ().OuterXml), "node");
			Assert.AreEqual (1, info.Count, "node count");
		}

		string xmlDSA = "<DSAKeyValue><P>rjxsMU368YOCTQejWkiuO9e/vUVwkLtq1jKiU3TtJ53hBJqjFRuTa228vZe+BH2su9RPn/vYFWfQDv6zgBYe3eNdu4Afw+Ny0FatX6dl3E77Ra6Tsd3MmLXBiGSQ1mMNd5G2XQGpbt9zsGlUaexXekeMLxIufgfZLwYp67M+2WM=</P><Q>tf0K9rMyvUrU4cIkwbCrDRhQAJk=</Q><G>S8Z+1pGCed00w6DtVcqZLKjfqlCJ7JsugEFIgSy/Vxtu9YGCMclV4ijGEbPo/jU8YOSMuD7E9M7UaopMRcmKQjoKZzoJjkgVFP48Ohxl1f08lERnButsxanx3+OstFwUGQ8XNaGg3KrIoZt1FUnfxN3RHHTvVhjzNSHxMGULGaU=</G><Y>LnrxxRGLYeV2XLtK3SYz8RQHlHFZYrtznDZyMotuRfO5uC5YODhSFyLXvb1qB3WeGtF4h3Eo4KzHgMgfN2ZMlffxFRhJgTtH3ctbL8lfQoDkjeiPPnYGhspdJxr0tyZmiy0gkjJG3vwHYrLnvZWx9Wm/unqiOlGBPNuxJ+hOeP8=</Y><J>9RhE5TycDtdEIXxS3HfxFyXYgpy81zY5lVjwD6E9JP37MWEi80BlX6ab1YPm6xYSEoqReMPP9RgGiW6DuACpgI7+8vgCr4i/7VhzModJAA56PwvTu6UMt9xxKU/fT672v8ucREkMWoc7lEey</J><Seed>HxW3N4RHWVgqDQKuGg7iJTUTiCs=</Seed><PgenCounter>Asw=</PgenCounter></DSAKeyValue>";

		[Test]
		public void DSAKeyValue () 
		{
			DSA key = DSA.Create ();
			key.FromXmlString (xmlDSA);
			DSAKeyValue dsa = new DSAKeyValue (key);
			info.AddClause (dsa);
			AssertCrypto.AssertXmlEquals ("dsa", "<KeyInfo xmlns=\"http://www.w3.org/2000/09/xmldsig#\"><KeyValue xmlns=\"http://www.w3.org/2000/09/xmldsig#\">" + xmlDSA + "</KeyValue></KeyInfo>", (info.GetXml ().OuterXml));
			Assert.AreEqual (1, info.Count, "dsa count");
		}

		static string xmlRSA = "<RSAKeyValue><Modulus>9DC4XNdQJwMRnz5pP2a6U51MHCODRilaIoVXqUPhCUb0lJdGroeqVYT84ZyIVrcarzD7Tqs3aEOIa3rKox0N1bxQpZPqayVQeLAkjLLtzJW/ScRJx3uEDJdgT1JnM1FH0GZTinmEdCUXdLc7+Y/c/qqIkTfbwHbRZjW0bBJyExM=</Modulus><Exponent>AQAB</Exponent></RSAKeyValue>";

		[Test]
		public void RSAKeyValue () 
		{
			RSA key = RSA.Create ();
			key.FromXmlString (xmlRSA);
			RSAKeyValue rsa = new RSAKeyValue (key);
			info.AddClause (rsa);
			AssertCrypto.AssertXmlEquals ("rsa", "<KeyInfo xmlns=\"http://www.w3.org/2000/09/xmldsig#\"><KeyValue xmlns=\"http://www.w3.org/2000/09/xmldsig#\">" + xmlRSA + "</KeyValue></KeyInfo>", (info.GetXml ().OuterXml));
			Assert.AreEqual (1, info.Count, "rsa count");
		}

		[Test]
		public void RetrievalMethod () 
		{
			KeyInfoRetrievalMethod retrieval = new KeyInfoRetrievalMethod ();
			retrieval.Uri = "http://www.go-mono.org/";
			info.AddClause (retrieval);
			Assert.AreEqual ("<KeyInfo xmlns=\"http://www.w3.org/2000/09/xmldsig#\"><RetrievalMethod URI=\"http://www.go-mono.org/\" /></KeyInfo>", (info.GetXml ().OuterXml), "RetrievalMethod");
			Assert.AreEqual (1, info.Count, "RetrievalMethod count");
		}

		static byte[] cert = { 0x30,0x82,0x02,0x1D,0x30,0x82,0x01,0x86,0x02,0x01,0x14,0x30,0x0D,0x06,0x09,0x2A,0x86,0x48,0x86,0xF7,0x0D,0x01,0x01,0x04,0x05,0x00,0x30,0x58,0x31,0x0B,0x30,0x09,0x06,0x03,0x55,0x04,0x06,0x13,0x02,0x43,0x41,0x31,0x1F,0x30,0x1D,0x06,0x03,0x55,0x04,0x03,0x13,0x16,0x4B,0x65,0x79,0x77,0x69,0x74,0x6E,0x65,0x73,0x73,0x20,0x43,0x61,0x6E,0x61,0x64,0x61,0x20,0x49,0x6E,0x63,0x2E,0x31,0x28,0x30,0x26,0x06,0x0A,0x2B,0x06,0x01,0x04,0x01,0x2A,0x02,0x0B,0x02,0x01,0x13,0x18,0x6B,0x65,0x79,0x77,0x69,0x74,0x6E,0x65,0x73,
			0x73,0x40,0x6B,0x65,0x79,0x77,0x69,0x74,0x6E,0x65,0x73,0x73,0x2E,0x63,0x61,0x30,0x1E,0x17,0x0D,0x39,0x36,0x30,0x35,0x30,0x37,0x30,0x30,0x30,0x30,0x30,0x30,0x5A,0x17,0x0D,0x39,0x39,0x30,0x35,0x30,0x37,0x30,0x30,0x30,0x30,0x30,0x30,0x5A,0x30,0x58,0x31,0x0B,0x30,0x09,0x06,0x03,0x55,0x04,0x06,0x13,0x02,0x43,0x41,0x31,0x1F,0x30,0x1D,0x06,0x03,0x55,0x04,0x03,0x13,0x16,0x4B,0x65,0x79,0x77,0x69,0x74,0x6E,0x65,0x73,0x73,0x20,0x43,0x61,0x6E,0x61,0x64,0x61,0x20,0x49,0x6E,0x63,0x2E,0x31,0x28,0x30,0x26,0x06,
			0x0A,0x2B,0x06,0x01,0x04,0x01,0x2A,0x02,0x0B,0x02,0x01,0x13,0x18,0x6B,0x65,0x79,0x77,0x69,0x74,0x6E,0x65,0x73,0x73,0x40,0x6B,0x65,0x79,0x77,0x69,0x74,0x6E,0x65,0x73,0x73,0x2E,0x63,0x61,0x30,0x81,0x9D,0x30,0x0D,0x06,0x09,0x2A,0x86,0x48,0x86,0xF7,0x0D,0x01,0x01,0x01,0x05,0x00,0x03,0x81,0x8B,0x00,0x30,0x81,0x87,0x02,0x81,0x81,0x00,0xCD,0x23,0xFA,0x2A,0xE1,0xED,0x98,0xF4,0xE9,0xD0,0x93,0x3E,0xD7,0x7A,0x80,0x02,0x4C,0xCC,0xC1,0x02,0xAF,0x5C,0xB6,0x1F,0x7F,0xFA,0x57,0x42,0x6F,0x30,0xD1,0x20,0xC5,0xB5,
			0x21,0x07,0x40,0x2C,0xA9,0x86,0xC2,0xF3,0x64,0x84,0xAE,0x3D,0x85,0x2E,0xED,0x85,0xBD,0x54,0xB0,0x18,0x28,0xEF,0x6A,0xF8,0x1B,0xE7,0x0B,0x16,0x1F,0x93,0x25,0x4F,0xC7,0xF8,0x8E,0xC3,0xB9,0xCA,0x98,0x84,0x0E,0x55,0xD0,0x2F,0xEF,0x78,0x77,0xC5,0x72,0x28,0x5F,0x60,0xBF,0x19,0x2B,0xD1,0x72,0xA2,0xB7,0xD8,0x3F,0xE0,0x97,0x34,0x5A,0x01,0xBD,0x04,0x9C,0xC8,0x78,0x45,0xCD,0x93,0x8D,0x15,0xF2,0x76,0x10,0x11,0xAB,0xB8,0x5B,0x2E,0x9E,0x52,0xDD,0x81,0x3E,0x9C,0x64,0xC8,0x29,0x93,0x02,0x01,0x03,0x30,0x0D,0x06,
			0x09,0x2A,0x86,0x48,0x86,0xF7,0x0D,0x01,0x01,0x04,0x05,0x00,0x03,0x81,0x81,0x00,0x32,0x1A,0x35,0xBA,0xBF,0x43,0x27,0xD6,0xB4,0xD4,0xB8,0x76,0xE5,0xE3,0x9B,0x4D,0x6C,0xC0,0x86,0xC9,0x77,0x35,0xBA,0x6B,0x16,0x2D,0x13,0x46,0x4A,0xB0,0x32,0x53,0xA1,0x5B,0x5A,0xE9,0x99,0xE2,0x0C,0x86,0x88,0x17,0x4E,0x0D,0xFE,0x82,0xAC,0x4E,0x47,0xEF,0xFB,0xFF,0x39,0xAC,0xEE,0x35,0xC8,0xFA,0x52,0x37,0x0A,0x49,0xAD,0x59,0xAD,0xE2,0x8A,0xA9,0x1C,0xC6,0x5F,0x1F,0xF8,0x6F,0x73,0x7E,0xCD,0xA0,0x31,0xE8,0x0C,0xBE,0xF5,0x4D,
			0xD9,0xB2,0xAB,0x8A,0x12,0xB6,0x30,0x78,0x68,0x11,0x7C,0x0D,0xF1,0x49,0x4D,0xA3,0xFD,0xB2,0xE9,0xFF,0x1D,0xF0,0x91,0xFA,0x54,0x85,0xFF,0x33,0x90,0xE8,0xC1,0xBF,0xA4,0x9B,0xA4,0x62,0x46,0xBD,0x61,0x12,0x59,0x98,0x41,0x89 };

		[Test]
		public void X509Data () 
		{
			X509Certificate x509 = new X509Certificate (cert);
			KeyInfoX509Data x509data = new KeyInfoX509Data (x509);
			info.AddClause (x509data);
			AssertCrypto.AssertXmlEquals ("X509Data", "<KeyInfo xmlns=\"http://www.w3.org/2000/09/xmldsig#\"><X509Data xmlns=\"http://www.w3.org/2000/09/xmldsig#\"><X509Certificate>MIICHTCCAYYCARQwDQYJKoZIhvcNAQEEBQAwWDELMAkGA1UEBhMCQ0ExHzAdBgNVBAMTFktleXdpdG5lc3MgQ2FuYWRhIEluYy4xKDAmBgorBgEEASoCCwIBExhrZXl3aXRuZXNzQGtleXdpdG5lc3MuY2EwHhcNOTYwNTA3MDAwMDAwWhcNOTkwNTA3MDAwMDAwWjBYMQswCQYDVQQGEwJDQTEfMB0GA1UEAxMWS2V5d2l0bmVzcyBDYW5hZGEgSW5jLjEoMCYGCisGAQQBKgILAgETGGtleXdpdG5lc3NAa2V5d2l0bmVzcy5jYTCBnTANBgkqhkiG9w0BAQEFAAOBiwAwgYcCgYEAzSP6KuHtmPTp0JM+13qAAkzMwQKvXLYff/pXQm8w0SDFtSEHQCyphsLzZISuPYUu7YW9VLAYKO9q+BvnCxYfkyVPx/iOw7nKmIQOVdAv73h3xXIoX2C/GSvRcqK32D/glzRaAb0EnMh4Rc2TjRXydhARq7hbLp5S3YE+nGTIKZMCAQMwDQYJKoZIhvcNAQEEBQADgYEAMho1ur9DJ9a01Lh25eObTWzAhsl3NbprFi0TRkqwMlOhW1rpmeIMhogXTg3+gqxOR+/7/zms7jXI+lI3CkmtWa3iiqkcxl8f+G9zfs2gMegMvvVN2bKrihK2MHhoEXwN8UlNo/2y6f8d8JH6VIX/M5Dowb+km6RiRr1hElmYQYk=</X509Certificate></X509Data></KeyInfo>", (info.GetXml ().OuterXml));
			Assert.AreEqual (1, info.Count, "X509Data count");
		}

		[Test]
		public void Complex () 
		{
			KeyInfoName name = new KeyInfoName ();
			name.Value = "Mono::";
			info.AddClause (name);

			DSA keyDSA = DSA.Create ();
			keyDSA.FromXmlString (xmlDSA);
			DSAKeyValue dsa = new DSAKeyValue (keyDSA);
			info.AddClause (dsa);

			RSA keyRSA = RSA.Create ();
			keyRSA.FromXmlString (xmlRSA);
			RSAKeyValue rsa = new RSAKeyValue (keyRSA);
			info.AddClause (rsa);

			KeyInfoRetrievalMethod retrieval = new KeyInfoRetrievalMethod ();
			retrieval.Uri = "http://www.go-mono.org/";
			info.AddClause (retrieval);

			X509Certificate x509 = new X509Certificate (cert);
			KeyInfoX509Data x509data = new KeyInfoX509Data (x509);
			info.AddClause (x509data);

			string s = "<KeyInfo xmlns=\"http://www.w3.org/2000/09/xmldsig#\"><KeyName>Mono::</KeyName><KeyValue xmlns=\"http://www.w3.org/2000/09/xmldsig#\"><DSAKeyValue><P>rjxsMU368YOCTQejWkiuO9e/vUVwkLtq1jKiU3TtJ53hBJqjFRuTa228vZe+BH2su9RPn/vYFWfQDv6zgBYe3eNdu4Afw+Ny0FatX6dl3E77Ra6Tsd3MmLXBiGSQ1mMNd5G2XQGpbt9zsGlUaexXekeMLxIufgfZLwYp67M+2WM=</P><Q>tf0K9rMyvUrU4cIkwbCrDRhQAJk=</Q><G>S8Z+1pGCed00w6DtVcqZLKjfqlCJ7JsugEFIgSy/Vxtu9YGCMclV4ijGEbPo/jU8YOSMuD7E9M7UaopMRcmKQjoKZzoJjkgVFP48Ohxl1f08lERnButsxanx3+OstFwUGQ8XNaGg3KrIoZt1FUnfxN3RHHTvVhjzNSHxMGULGaU=</G><Y>LnrxxRGLYeV2XLtK3SYz8RQHlHFZYrtznDZyMotuRfO5uC5YODhSFyLXvb1qB3WeGtF4h3Eo4KzHgMgfN2ZMlffxFRhJgTtH3ctbL8lfQoDkjeiPPnYGhspdJxr0tyZmiy0gkjJG3vwHYrLnvZWx9Wm/unqiOlGBPNuxJ+hOeP8=</Y><J>9RhE5TycDtdEIXxS3HfxFyXYgpy81zY5lVjwD6E9JP37MWEi80BlX6ab1YPm6xYSEoqReMPP9RgGiW6DuACpgI7+8vgCr4i/7VhzModJAA56PwvTu6UMt9xxKU/fT672v8ucREkMWoc7lEey</J><Seed>HxW3N4RHWVgqDQKuGg7iJTUTiCs=</Seed><PgenCounter>Asw=</PgenCounter></DSAKeyValue></KeyValue>";
			s += "<KeyValue xmlns=\"http://www.w3.org/2000/09/xmldsig#\"><RSAKeyValue><Modulus>9DC4XNdQJwMRnz5pP2a6U51MHCODRilaIoVXqUPhCUb0lJdGroeqVYT84ZyIVrcarzD7Tqs3aEOIa3rKox0N1bxQpZPqayVQeLAkjLLtzJW/ScRJx3uEDJdgT1JnM1FH0GZTinmEdCUXdLc7+Y/c/qqIkTfbwHbRZjW0bBJyExM=</Modulus><Exponent>AQAB</Exponent></RSAKeyValue></KeyValue>";
			s += "<RetrievalMethod URI=\"http://www.go-mono.org/\" />";
			s += "<X509Data xmlns=\"http://www.w3.org/2000/09/xmldsig#\">";
			s += "<X509Certificate>MIICHTCCAYYCARQwDQYJKoZIhvcNAQEEBQAwWDELMAkGA1UEBhMCQ0ExHzAdBgNVBAMTFktleXdpdG5lc3MgQ2FuYWRhIEluYy4xKDAmBgorBgEEASoCCwIBExhrZXl3aXRuZXNzQGtleXdpdG5lc3MuY2EwHhcNOTYwNTA3MDAwMDAwWhcNOTkwNTA3MDAwMDAwWjBYMQswCQYDVQQGEwJDQTEfMB0GA1UEAxMWS2V5d2l0bmVzcyBDYW5hZGEgSW5jLjEoMCYGCisGAQQBKgILAgETGGtleXdpdG5lc3NAa2V5d2l0bmVzcy5jYTCBnTANBgkqhkiG9w0BAQEFAAOBiwAwgYcCgYEAzSP6KuHtmPTp0JM+13qAAkzMwQKvXLYff/pXQm8w0SDFtSEHQCyphsLzZISuPYUu7YW9VLAYKO9q+BvnCxYfkyVPx/iOw7nKmIQOVdAv73h3xXIoX2C/GSvRcqK32D/glzRaAb0EnMh4Rc2TjRXydhARq7hbLp5S3YE+nGTIKZMCAQMwDQYJKoZIhvcNAQEEBQADgYEAMho1ur9DJ9a01Lh25eObTWzAhsl3NbprFi0TRkqwMlOhW1rpmeIMhogXTg3+gqxOR+/7/zms7jXI+lI3CkmtWa3iiqkcxl8f+G9zfs2gMegMvvVN2bKrihK2MHhoEXwN8UlNo/2y6f8d8JH6VIX/M5Dowb+km6RiRr1hElmYQYk=</X509Certificate></X509Data></KeyInfo>";
			AssertCrypto.AssertXmlEquals ("Complex", s, (info.GetXml ().OuterXml));
			Assert.AreEqual (5, info.Count, "RetrievalMethod count");
		}

		[Test]
		public void ImportKeyNode () 
		{
			string value = "<KeyInfo xmlns=\"http://www.w3.org/2000/09/xmldsig#\"><KeyName>Mono::</KeyName><KeyValue xmlns=\"http://www.w3.org/2000/09/xmldsig#\"><DSAKeyValue><P>rjxsMU368YOCTQejWkiuO9e/vUVwkLtq1jKiU3TtJ53hBJqjFRuTa228vZe+BH2su9RPn/vYFWfQDv6zgBYe3eNdu4Afw+Ny0FatX6dl3E77Ra6Tsd3MmLXBiGSQ1mMNd5G2XQGpbt9zsGlUaexXekeMLxIufgfZLwYp67M+2WM=</P><Q>tf0K9rMyvUrU4cIkwbCrDRhQAJk=</Q><G>S8Z+1pGCed00w6DtVcqZLKjfqlCJ7JsugEFIgSy/Vxtu9YGCMclV4ijGEbPo/jU8YOSMuD7E9M7UaopMRcmKQjoKZzoJjkgVFP48Ohxl1f08lERnButsxanx3+OstFwUGQ8XNaGg3KrIoZt1FUnfxN3RHHTvVhjzNSHxMGULGaU=</G><Y>LnrxxRGLYeV2XLtK3SYz8RQHlHFZYrtznDZyMotuRfO5uC5YODhSFyLXvb1qB3WeGtF4h3Eo4KzHgMgfN2ZMlffxFRhJgTtH3ctbL8lfQoDkjeiPPnYGhspdJxr0tyZmiy0gkjJG3vwHYrLnvZWx9Wm/unqiOlGBPNuxJ+hOeP8=</Y><J>9RhE5TycDtdEIXxS3HfxFyXYgpy81zY5lVjwD6E9JP37MWEi80BlX6ab1YPm6xYSEoqReMPP9RgGiW6DuACpgI7+8vgCr4i/7VhzModJAA56PwvTu6UMt9xxKU/fT672v8ucREkMWoc7lEey</J><Seed>HxW3N4RHWVgqDQKuGg7iJTUTiCs=</Seed><PgenCounter>Asw=</PgenCounter></DSAKeyValue></KeyValue>";
			value += "<KeyValue xmlns=\"http://www.w3.org/2000/09/xmldsig#\"><RSAKeyValue><Modulus>9DC4XNdQJwMRnz5pP2a6U51MHCODRilaIoVXqUPhCUb0lJdGroeqVYT84ZyIVrcarzD7Tqs3aEOIa3rKox0N1bxQpZPqayVQeLAkjLLtzJW/ScRJx3uEDJdgT1JnM1FH0GZTinmEdCUXdLc7+Y/c/qqIkTfbwHbRZjW0bBJyExM=</Modulus><Exponent>AQAB</Exponent></RSAKeyValue></KeyValue><RetrievalElement URI=\"http://www.go-mono.org/\" /><X509Data xmlns=\"http://www.w3.org/2000/09/xmldsig#\">";
			value += "<X509Certificate>MIICHTCCAYYCARQwDQYJKoZIhvcNAQEEBQAwWDELMAkGA1UEBhMCQ0ExHzAdBgNVBAMTFktleXdpdG5lc3MgQ2FuYWRhIEluYy4xKDAmBgorBgEEASoCCwIBExhrZXl3aXRuZXNzQGtleXdpdG5lc3MuY2EwHhcNOTYwNTA3MDAwMDAwWhcNOTkwNTA3MDAwMDAwWjBYMQswCQYDVQQGEwJDQTEfMB0GA1UEAxMWS2V5d2l0bmVzcyBDYW5hZGEgSW5jLjEoMCYGCisGAQQBKgILAgETGGtleXdpdG5lc3NAa2V5d2l0bmVzcy5jYTCBnTANBgkqhkiG9w0BAQEFAAOBiwAwgYcCgYEAzSP6KuHtmPTp0JM+13qAAkzMwQKvXLYff/pXQm8w0SDFtSEHQCyphsLzZISuPYUu7YW9VLAYKO9q+BvnCxYfkyVPx/iOw7nKmIQOVdAv73h3xXIoX2C/GSvRcqK32D/glzRaAb0EnMh4Rc2TjRXydhARq7hbLp5S3YE+nGTIKZMCAQMwDQYJKoZIhvcNAQEEBQADgYEAMho1ur9DJ9a01Lh25eObTWzAhsl3NbprFi0TRkqwMlOhW1rpmeIMhogXTg3+gqxOR+/7/zms7jXI+lI3CkmtWa3iiqkcxl8f+G9zfs2gMegMvvVN2bKrihK2MHhoEXwN8UlNo/2y6f8d8JH6VIX/M5Dowb+km6RiRr1hElmYQYk=</X509Certificate></X509Data></KeyInfo>";
			XmlDocument doc = new XmlDocument ();
			doc.LoadXml (value);
			info.LoadXml (doc.DocumentElement);

			AssertCrypto.AssertXmlEquals ("Import", value, (info.GetXml ().OuterXml));
			Assert.AreEqual (5, info.Count, "Import count");
		}

		[Test]
		[ExpectedException (typeof (NullReferenceException))]
		public void NullClause () 
		{
			Assert.AreEqual (0, info.Count, "empty count");
			// null is accepted...
			info.AddClause (null);
			Assert.AreEqual (1, info.Count, "null count");
			// but can't get XML out if it!
			XmlElement xel = info.GetXml ();
		}

		[Test]
		[ExpectedException (typeof (ArgumentNullException))]
		public void NullXml () 
		{
			info.LoadXml (null);
		}

		[Test]
		public void InvalidXml () 
		{
			string bad = "<Test></Test>";
			XmlDocument doc = new XmlDocument ();
			doc.LoadXml (bad);
			info.LoadXml (doc.DocumentElement);
			// LAMESPEC: no expection but Xml isn't loaded
			Assert.AreEqual ("<KeyInfo xmlns=\"http://www.w3.org/2000/09/xmldsig#\" />", (info.GetXml ().OuterXml), "invalid");
			Assert.AreEqual (0, info.Count, "invalid count");
		}
	}
}
