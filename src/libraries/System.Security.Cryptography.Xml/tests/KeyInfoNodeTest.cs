//
// KeyInfoNodeTest.cs - NUnit Test Cases for KeyInfoNode
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
	public class KeyInfoNodeTest {

		[Test]
		public void NewKeyNode () 
		{
			string test = "<Test></Test>";
			XmlDocument doc = new XmlDocument ();
			doc.LoadXml (test);

			KeyInfoNode node1 = new KeyInfoNode ();
			node1.Value = doc.DocumentElement;
			XmlElement xel = node1.GetXml ();

			KeyInfoNode node2 = new KeyInfoNode (node1.Value);
			node2.LoadXml (xel);

			Assert.AreEqual ((node1.GetXml ().OuterXml), (node2.GetXml ().OuterXml), "node1==node2");
		}

		[Test]
		public void ImportKeyNode () 
		{
			// Note: KeyValue is a valid KeyNode
			string value = "<KeyName xmlns=\"http://www.w3.org/2000/09/xmldsig#\">Mono::</KeyName>";
			XmlDocument doc = new XmlDocument ();
			doc.LoadXml (value);

			KeyInfoNode node1 = new KeyInfoNode ();
			node1.LoadXml (doc.DocumentElement);

			string s = (node1.GetXml ().OuterXml);
			Assert.AreEqual (value, s, "Node");
		}

		// well there's no invalid value - unless you read the doc ;-)
		[Test]
		public void InvalidKeyNode () 
		{
			string bad = "<Test></Test>";
			XmlDocument doc = new XmlDocument ();
			doc.LoadXml (bad);

			KeyInfoNode node1 = new KeyInfoNode ();
			// LAMESPEC: No ArgumentNullException is thrown if value == null
			node1.LoadXml (null);
			Assert.IsNull (node1.Value, "Value==null");
		}
	}
}
