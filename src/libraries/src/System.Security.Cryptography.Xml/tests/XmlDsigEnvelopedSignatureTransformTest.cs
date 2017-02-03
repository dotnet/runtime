//
// XmlDsigEnvelopedSignatureTransformTest.cs
//
// Author:
//	Atsushi Enomoto <atsushi@ximian.com>
//
// (C) 2004 Novell Inc.
//

using System;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Xml;
using System.Xml.Xsl;

using NUnit.Framework;

namespace MonoTests.System.Security.Cryptography.Xml {

	// Note: GetInnerXml is protected in XmlDsigEnvelopedSignatureTransform making it
	// difficult to test properly. This class "open it up" :-)
	public class UnprotectedXmlDsigEnvelopedSignatureTransform : XmlDsigEnvelopedSignatureTransform {
		public UnprotectedXmlDsigEnvelopedSignatureTransform ()
		{
		}

		public UnprotectedXmlDsigEnvelopedSignatureTransform (bool includeComments)
			: base (includeComments)
		{
		}

		public XmlNodeList UnprotectedGetInnerXml () 
		{
			return base.GetInnerXml ();
		}
	}

	[TestFixture]
	public class XmlDsigEnvelopedSignatureTransformTest {
		private UnprotectedXmlDsigEnvelopedSignatureTransform transform;

		[SetUp]
		public void SetUp ()
		{
			transform = new UnprotectedXmlDsigEnvelopedSignatureTransform ();
		}

		[Test] // ctor ()
		public void Constructor1 ()
		{
			CheckProperties (transform);
		}

		[Test] // ctor (Boolean)
		public void Constructor2 ()
		{
			transform = new UnprotectedXmlDsigEnvelopedSignatureTransform (true);
			CheckProperties (transform);
			transform = new UnprotectedXmlDsigEnvelopedSignatureTransform (false);
			CheckProperties (transform);
		}

		void CheckProperties (XmlDsigEnvelopedSignatureTransform transform)
		{
			Assert.AreEqual ("http://www.w3.org/2000/09/xmldsig#enveloped-signature",
				transform.Algorithm, "Algorithm");

			Type [] input = transform.InputTypes;
			Assert.AreEqual (3, input.Length, "Input Length");
			// check presence of every supported input types
			bool istream = false;
			bool ixmldoc = false;
			bool ixmlnl = false;
			foreach (Type t in input) {
				if (t == typeof (XmlDocument))
					ixmldoc = true;
				if (t == typeof (XmlNodeList))
					ixmlnl = true;
				if (t == typeof (Stream))
					istream = true;
			}
			Assert.IsTrue (istream, "Input Stream");
			Assert.IsTrue (ixmldoc, "Input XmlDocument");
			Assert.IsTrue (ixmlnl, "Input XmlNodeList");

			Type [] output = transform.OutputTypes;
			Assert.AreEqual (2, output.Length, "Output Length");
			// check presence of every supported output types
			bool oxmlnl = false;
			bool oxmldoc = false;
			foreach (Type t in output) {
				if (t == typeof (XmlNodeList))
					oxmlnl = true;
				if (t == typeof (XmlDocument))
					oxmldoc = true;
			}
			Assert.IsTrue (oxmlnl, "Output XmlNodeList");
			Assert.IsTrue (oxmldoc, "Output XmlDocument");
		}

		void AssertEquals (XmlNodeList expected, XmlNodeList actual, string msg)
		{
			for (int i = 0; i < expected.Count; i++) {
				if (expected [i].OuterXml != actual [i].OuterXml)
					Assert.Fail (msg + " [" + i + "] expected " + expected [i].OuterXml + " bug got " + actual [i].OuterXml);
			}
		}

		[Test]
		public void GetInnerXml () 
		{
			// Always returns null
			Assert.IsNull (transform.UnprotectedGetInnerXml ());
		}

		private XmlDocument GetDoc () 
		{
			string dsig = "<Signature xmlns=\"http://www.w3.org/2000/09/xmldsig#\"><CanonicalizationMethod Algorithm=\"http://www.w3.org/TR/2001/REC-xml-c14n-20010315\" /><SignatureMethod Algorithm=\"http://www.w3.org/2000/09/xmldsig#dsa-sha1\" /><Reference URI=\"\"><Transforms><Transform Algorithm=\"http://www.w3.org/2000/09/xmldsig#enveloped-signature\" /></Transforms><DigestMethod Algorithm=\"http://www.w3.org/2000/09/xmldsig#sha1\" /><DigestValue>fdy6S2NLpnT4fMdokUHSHsmpcvo=</DigestValue></Reference></Signature>";
			string test = "<Envelope> " + dsig + " </Envelope>";
			XmlDocument doc = new XmlDocument ();
			doc.LoadXml (test);
			return doc;
		}

		[Test]
		public void LoadInputAsXmlDocument () 
		{
			XmlDocument doc = GetDoc ();
			transform.LoadInput (doc);
			object o = transform.GetOutput ();
			Assert.AreEqual (doc, o, "EnvelopedSignature result");
		}

		[Test]
		public void LoadInputAsXmlNodeList () 
		{
			XmlDocument doc = GetDoc ();
			transform.LoadInput (doc.ChildNodes);
			XmlNodeList xnl = (XmlNodeList) transform.GetOutput ();
			AssertEquals (doc.ChildNodes, xnl, "EnvelopedSignature result");
		}
	}
}
