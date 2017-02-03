//
// TransformChainTest.cs - NUnit Test Cases for TransformChain
//
// Author:
//	Sebastien Pouliot (spouliot@motus.com)
//
// (C) 2002, 2003 Motus Technologies Inc. (http://www.motus.com)
//

using System;
using System.Security.Cryptography;
using System.Security.Cryptography.Xml;
using System.Xml;

using NUnit.Framework;

namespace MonoTests.System.Security.Cryptography.Xml {

	[TestFixture]
	public class TransformChainTest {

		[Test]
		public void EmptyChain () 
		{
			TransformChain chain = new TransformChain ();
			Assert.AreEqual (0, chain.Count, "empty count");
			Assert.IsNotNull (chain.GetEnumerator (), "IEnumerator");
			Assert.AreEqual ("System.Security.Cryptography.Xml.TransformChain", chain.ToString (), "ToString()");
		}

		[Test]
		public void FullChain () 
		{
			TransformChain chain = new TransformChain ();

			XmlDsigBase64Transform base64 = new XmlDsigBase64Transform ();
			chain.Add (base64);
			Assert.AreEqual (base64, chain[0], "XmlDsigBase64Transform");
			Assert.AreEqual (1, chain.Count, "count 1");

			XmlDsigC14NTransform c14n = new XmlDsigC14NTransform ();
			chain.Add (c14n);
			Assert.AreEqual (c14n, chain[1], "XmlDsigC14NTransform");
			Assert.AreEqual (2, chain.Count, "count 2");

			XmlDsigC14NWithCommentsTransform c14nc = new XmlDsigC14NWithCommentsTransform ();
			chain.Add (c14nc);
			Assert.AreEqual (c14nc, chain[2], "XmlDsigC14NWithCommentsTransform");
			Assert.AreEqual (3, chain.Count, "count 3");

			XmlDsigEnvelopedSignatureTransform esign = new XmlDsigEnvelopedSignatureTransform ();
			chain.Add (esign);
			Assert.AreEqual (esign, chain[3], "XmlDsigEnvelopedSignatureTransform");
			Assert.AreEqual (4, chain.Count, "count 4");

			XmlDsigXPathTransform xpath = new XmlDsigXPathTransform ();
			chain.Add (xpath);
			Assert.AreEqual (xpath, chain[4], "XmlDsigXPathTransform");
			Assert.AreEqual (5, chain.Count, "count 5");

			XmlDsigXsltTransform xslt = new XmlDsigXsltTransform ();
			chain.Add (xslt);
			Assert.AreEqual (xslt, chain[5], "XmlDsigXsltTransform");
			Assert.AreEqual (6, chain.Count, "count 6");
		}
	}
}
