//
// SignedInfoTest.cs - NUnit Test Cases for SignedInfo
//
// Author:
//	Sebastien Pouliot <sebastien@ximian.com>
//
// (C) 2002, 2003 Motus Technologies Inc. (http://www.motus.com)
// Copyright (C) 2005, 2009 Novell, Inc (http://www.novell.com)
//

using System;
using System.Security.Cryptography;
using System.Security.Cryptography.Xml;
using System.Xml;

using NUnit.Framework;

namespace MonoTests.System.Security.Cryptography.Xml {

	[TestFixture]
	public class SignedInfoTest {

		protected SignedInfo info;

		[SetUp]
		protected void SetUp () 
		{
			info = new SignedInfo ();
		}

		[Test]
		public void Empty () 
		{
			Assert.AreEqual ("http://www.w3.org/TR/2001/REC-xml-c14n-20010315", info.CanonicalizationMethod, "CanonicalizationMethod");
			Assert.IsNull (info.Id, "Id");
			Assert.IsNotNull (info.References, "References");
			Assert.AreEqual (0, info.References.Count, "References.Count");
			Assert.IsNull (info.SignatureLength, "SignatureLength");
			Assert.IsNull (info.SignatureMethod, "SignatureMethod");
			Assert.AreEqual ("System.Security.Cryptography.Xml.SignedInfo", info.ToString (), "ToString()");
		}

		[Test]
		[ExpectedException (typeof (CryptographicException))]
		public void EmptyException () 
		{
			string xml = info.GetXml ().OuterXml;
		}

		[Test]
		public void Properties () 
		{
			info.CanonicalizationMethod = "http://www.go-mono.com/";
			Assert.AreEqual ("http://www.go-mono.com/", info.CanonicalizationMethod, "CanonicalizationMethod");
			info.Id = "Mono::";
			Assert.AreEqual ("Mono::", info.Id, "Id");
		}

		[Test]
		public void References () 
		{
			Reference r1 = new Reference ();
			r1.Uri = "http://www.go-mono.com/";
			r1.AddTransform (new XmlDsigBase64Transform ());
			info.AddReference (r1);
			Assert.AreEqual (1, info.References.Count, "References.Count 1");

			Reference r2 = new Reference ("http://www.motus.com/");
			r2.AddTransform (new XmlDsigBase64Transform ());
			info.AddReference (r2);
			Assert.AreEqual (2, info.References.Count, "References.Count 2");

			info.SignatureMethod = "http://www.w3.org/2000/09/xmldsig#dsa-sha1";
		}

		[Test]
		public void Load () 
		{
			string xml = "<SignedInfo xmlns=\"http://www.w3.org/2000/09/xmldsig#\"><CanonicalizationMethod Algorithm=\"http://www.w3.org/TR/2001/REC-xml-c14n-20010315\" /><SignatureMethod Algorithm=\"http://www.w3.org/2000/09/xmldsig#rsa-sha1\" /><Reference URI=\"#MyObjectId\"><DigestMethod Algorithm=\"http://www.w3.org/2000/09/xmldsig#sha1\" /><DigestValue>/Vvq6sXEVbtZC8GwNtLQnGOy/VI=</DigestValue></Reference></SignedInfo>";
			XmlDocument doc = new XmlDocument ();
			doc.LoadXml (xml);
			info.LoadXml (doc.DocumentElement);
			Assert.AreEqual (xml, (info.GetXml ().OuterXml), "LoadXml");
			Assert.AreEqual ("http://www.w3.org/TR/2001/REC-xml-c14n-20010315", info.CanonicalizationMethod, "LoadXml-C14N");
			Assert.AreEqual ("http://www.w3.org/2000/09/xmldsig#rsa-sha1", info.SignatureMethod, "LoadXml-Algo");
			Assert.AreEqual (1, info.References.Count, "LoadXml-Ref1");
		}

		// there are many (documented) not supported methods in SignedInfo

		[Test]
		[ExpectedException (typeof (NotSupportedException))]
		public void NotSupportedCount () 
		{
			int n = info.Count;
		}

		[Test]
		[ExpectedException (typeof (NotSupportedException))]
		public void NotSupportedIsReadOnly () 
		{
			bool b = info.IsReadOnly;
		}

		[Test]
		[ExpectedException (typeof (NotSupportedException))]
		public void NotSupportedIsSynchronized () 
		{
			bool b = info.IsSynchronized;
		}

		[Test]
		[ExpectedException (typeof (NotSupportedException))]
		public void NotSupportedSyncRoot () 
		{
			object o = info.SyncRoot;
		}

		[Test]
		[ExpectedException (typeof (NotSupportedException))]
		public void NotSupportedCopyTo () 
		{
			info.CopyTo (null, 0);
		}
		
		// from phaos testcase
		const string xmlForGetXml = @"<player bats=""left"" id=""10012"" throws=""right"">
	<!-- Here&apos;s a comment -->
	<name>Alfonso Soriano</name>
	<position>2B</position>
	<team>New York Yankees</team>
<dsig:Signature xmlns=""http://www.w3.org/2000/09/xmldsig#"" xmlns:dsig=""http://www.w3.org/2000/09/xmldsig#"">"
+ @"<dsig:SignedInfo><dsig:CanonicalizationMethod Algorithm=""http://www.w3.org/TR/2001/REC-xml-c14n-withcomments-20010315""/><dsig:SignatureMethod Algorithm=""http://www.w3.org/2000/09/xmldsig#rsa-sha1""/>"
+ @"<dsig:Reference URI=""""><dsig:Transforms><dsig:Transform Algorithm=""http://www.w3.org/2000/09/xmldsig#enveloped-signature""/></dsig:Transforms><dsig:DigestMethod Algorithm=""http://www.w3.org/2000/09/xmldsig#sha1""/><dsig:DigestValue>nDF2V/bzRd0VE3EwShWtsBzTEDc=</dsig:DigestValue></dsig:Reference></dsig:SignedInfo><dsig:SignatureValue>fbye4Xm//RPUTsLd1dwJPo0gPZYX6gVYCEB/gz2348EARNk/nCCch1fFfpuqAGMKg4ayVC0yWkUyE5V4QB33jaGlh9wuNQSjxs6TIvFwSsT+0ioDgVgFv0gVeasbyNL4rFEHuAWL8QKwDT9L6b2wUvJC90DmpBs9GMR2jTZIWlM=</dsig:SignatureValue><dsig:KeyInfo><dsig:X509Data><dsig:X509Certificate>MIIC0DCCAjmgAwIBAgIDD0JBMA0GCSqGSIb3DQEBBAUAMHwxCzAJBgNVBAYTAlVTMREwDwYDVQQIEwhOZXcgWW9yazERMA8GA1UEBxMITmV3IFlvcmsxGTAXBgNVBAoTEFBoYW9zIFRlY2hub2xvZ3kxFDASBgNVBAsTC0VuZ2luZWVyaW5nMRYwFAYDVQQDEw1UZXN0IENBIChSU0EpMB4XDTAyMDQyOTE5MTY0MFoXDTEyMDQyNjE5MTY0MFowgYAxCzAJBgNVBAYTAlVTMREwDwYDVQQIEwhOZXcgWW9yazERMA8GA1UEBxMITmV3IFlvcmsxGTAXBgNVBAoTEFBoYW9zIFRlY2hub2xvZ3kxFDASBgNVBAsTC0VuZ2luZWVyaW5nMRowGAYDVQQDExFUZXN0IENsaWVudCAoUlNBKTCBnzANBgkqhkiG9w0BAQEFAAOBjQAwgYkCgYEAgIb6nAB9oS/AI5jIj6WymvQhRxiMlE07G4abmMliYi5zWzvaFE2tnU+RZIBgtoXcgDEIU/vsLQut7nzCn9mHxC8JEaV4D4U91j64AyZakShqJw7qjJfqUxxPL0yJv2oFiouPDjGuJ9JPi0NrsZq+yfWfM54s4b9SNkcOIVMybZUCAwEAAaNbMFkwDAYDVR0TAQH/BAIwADAPBgNVHQ8BAf8EBQMDB9gAMBkGA1UdEQQSMBCBDnRlY2hAcGhhb3MuY29tMB0GA1UdDgQWBBQT58rBCxPmVLeZaYGRqVROnQlFbzANBgkqhkiG9w0BAQQFAAOBgQCxbCovFST25t+ryN1RipqozxJQcguKfeCwbfgBNobzcRvoW0kSIf7zi4mtQajDM0NfslFF51/dex5Rn64HmFFshSwSvQQMyf5Cfaqv2XQ60OXq6nAFG6WbHoge6RqfIez2MWDLoSB6plsjKtMmL3mcybBhROtX5GGuLx1NtfhNFQ==</dsig:X509Certificate><dsig:X509IssuerSerial><dsig:X509IssuerName>CN=Test CA (RSA),OU=Engineering,O=Phaos Technology,L=New York,ST=New York,C=US</dsig:X509IssuerName><dsig:X509SerialNumber>1000001</dsig:X509SerialNumber></dsig:X509IssuerSerial><dsig:X509SubjectName>CN=Test Client (RSA),OU=Engineering,O=Phaos Technology,L=New York,ST=New York,C=US</dsig:X509SubjectName><dsig:X509SKI>E+fKwQsT5lS3mWmBkalUTp0JRW8=</dsig:X509SKI></dsig:X509Data></dsig:KeyInfo></dsig:Signature></player>";

		[Test]
		public void GetXmlWithoutSetProperty ()
		{
			string result = @"<dsig:SignedInfo xmlns:dsig=""http://www.w3.org/2000/09/xmldsig#""><dsig:CanonicalizationMethod Algorithm=""http://www.w3.org/TR/2001/REC-xml-c14n-withcomments-20010315"" /><dsig:SignatureMethod Algorithm=""http://www.w3.org/2000/09/xmldsig#rsa-sha1"" /><dsig:Reference URI=""""><dsig:Transforms><dsig:Transform Algorithm=""http://www.w3.org/2000/09/xmldsig#enveloped-signature"" /></dsig:Transforms><dsig:DigestMethod Algorithm=""http://www.w3.org/2000/09/xmldsig#sha1"" /><dsig:DigestValue>nDF2V/bzRd0VE3EwShWtsBzTEDc=</dsig:DigestValue></dsig:Reference></dsig:SignedInfo>";
			XmlDocument doc = new XmlDocument ();
			doc.LoadXml (xmlForGetXml);
			SignedInfo sig = new SignedInfo ();
			sig.LoadXml ((XmlElement) doc.SelectSingleNode ("//*[local-name()='SignedInfo']"));
			XmlElement el = sig.GetXml ();
			Assert.AreEqual (doc, el.OwnerDocument, "#GetXmlWOSetProperty.document");
			Assert.AreEqual (result, el.OuterXml, "#GetXmlWOSetProperty.outerxml");
		}

		[Test]
		// urn:foo is'nt accepted when calling GetXml
		[ExpectedException (typeof (CryptographicException))]
		[Category ("NotWorking")]
		public void GetXmlWithSetProperty ()
		{
			XmlDocument doc = new XmlDocument ();
			doc.LoadXml (xmlForGetXml);
			SignedInfo sig = new SignedInfo ();
			sig.LoadXml ((XmlElement) doc.SelectSingleNode ("//*[local-name()='SignedInfo']"));
			sig.CanonicalizationMethod = "urn:foo";
			XmlElement el = sig.GetXml ();
			Assert.IsTrue (doc != el.OwnerDocument, "#GetXmlWithSetProperty.document");
		}

		[Test] // never fails
		public void EmptyReferenceWithoutSetProperty ()
		{
			XmlDocument doc = new XmlDocument ();
			doc.LoadXml (xmlForGetXml);
			XmlNode n = doc.SelectSingleNode ("//*[local-name()='Reference']");
			n.ParentNode.RemoveChild (n);

			SignedInfo sig = new SignedInfo ();
			sig.LoadXml ((XmlElement) doc.SelectSingleNode ("//*[local-name()='SignedInfo']"));
			XmlElement el = sig.GetXml ();
		}

		[Test]
		[ExpectedException (typeof (CryptographicException))]
		public void EmptyReferenceWithSetProperty ()
		{
			XmlDocument doc = new XmlDocument ();
			doc.LoadXml (xmlForGetXml);
			XmlNode n = doc.SelectSingleNode ("//*[local-name()='Reference']");
			n.ParentNode.RemoveChild (n);

			SignedInfo sig = new SignedInfo ();
			sig.LoadXml ((XmlElement) doc.SelectSingleNode ("//*[local-name()='SignedInfo']"));
			sig.CanonicalizationMethod = "urn:foo";
			XmlElement el = sig.GetXml ();
		}

		[Test]
		public void SignatureLength ()
		{
			// we can set the length before the algorithm
			SignedInfo si = new SignedInfo ();
			si.SignatureLength = "128";
			Assert.AreEqual ("128", si.SignatureLength, "SignatureLength-1");
			Assert.IsNull (si.SignatureMethod, "SignatureMethod-1");

			// zero
			si.SignatureMethod = "http://www.w3.org/2000/09/xmldsig#rsa-sha1";
			si.SignatureLength = "0";
			Assert.AreEqual ("0", si.SignatureLength, "SignatureLength-2");
			Assert.AreEqual ("http://www.w3.org/2000/09/xmldsig#rsa-sha1", si.SignatureMethod, "SignatureMethod-2");

			// mixup length and method
			si.SignatureLength = "http://www.w3.org/2000/09/xmldsig#rsa-sha1";
			si.SignatureMethod = "0";
			Assert.AreEqual ("http://www.w3.org/2000/09/xmldsig#rsa-sha1", si.SignatureLength, "SignatureLength-3");
			Assert.AreEqual ("0", si.SignatureMethod, "SignatureMethod-3");
		}
	}
}
