//
// XmlDsigXPathTransformTest.cs - NUnit Test Cases for XmlDsigXPathTransform
//
// Author:
//	Sebastien Pouliot <sebastien@ximian.com>
//
// (C) 2002, 2003 Motus Technologies Inc. (http://www.motus.com)
// Copyright (C) 2004-2005 Novell, Inc (http://www.novell.com)
//

using System;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Xml;
using System.Xml.Xsl;
using System.Xml.XPath;

using NUnit.Framework;

namespace MonoTests.System.Security.Cryptography.Xml {

	// Note: GetInnerXml is protected in XmlDsigXPathTransform making it
	// difficult to test properly. This class "open it up" :-)
	public class UnprotectedXmlDsigXPathTransform : XmlDsigXPathTransform {

		public XmlNodeList UnprotectedGetInnerXml () 
		{
			return base.GetInnerXml ();
		}
	}

	[TestFixture]
	public class XmlDsigXPathTransformTest {

		protected UnprotectedXmlDsigXPathTransform transform;

		[SetUp]
		protected void SetUp () 
		{
			transform = new UnprotectedXmlDsigXPathTransform ();
		}

		[Test]
		public void Properties () 
		{
			Assert.AreEqual ("http://www.w3.org/TR/1999/REC-xpath-19991116", transform.Algorithm, "Algorithm");

			Type[] input = transform.InputTypes;
			Assert.IsTrue ((input.Length == 3), "Input #");
			// check presence of every supported input types
			bool istream = false;
			bool ixmldoc = false;
			bool ixmlnl = false;
			foreach (Type t in input) {
				if (t.ToString () == "System.IO.Stream")
					istream = true;
				if (t.ToString () == "System.Xml.XmlDocument")
					ixmldoc = true;
				if (t.ToString () == "System.Xml.XmlNodeList")
					ixmlnl = true;
			}
			Assert.IsTrue (istream, "Input Stream");
			Assert.IsTrue (ixmldoc, "Input XmlDocument");
			Assert.IsTrue (ixmlnl, "Input XmlNodeList");

			Type[] output = transform.OutputTypes;
			Assert.IsTrue ((output.Length == 1), "Output #");
			// check presence of every supported output types
			bool oxmlnl = false;
			foreach (Type t in output) {
				if (t.ToString () == "System.Xml.XmlNodeList")
					oxmlnl = true;
			}
			Assert.IsTrue (oxmlnl, "Output XmlNodeList");
		}

		protected void AreEqual (string msg, XmlNodeList expected, XmlNodeList actual) 
		{
			for (int i=0; i < expected.Count; i++) {
				if (expected [i].OuterXml != actual [i].OuterXml)
					Assert.Fail (msg + " [" + i + "] expected " + expected[i].OuterXml + " bug got " + actual[i].OuterXml);
			}
			Assert.AreEqual (expected.Count, actual.Count);
		}

		[Test]
		[Ignore ("throws a NullReferenceException - but it's (kind of internal)")]
		public void GetInnerXml () 
		{
			XmlNodeList xnl = transform.UnprotectedGetInnerXml ();
			Assert.AreEqual (1, xnl.Count, "Default InnerXml.Count");
			Assert.AreEqual ("<XPath xmlns=\"http://www.w3.org/2000/09/xmldsig#\"></XPath>", xnl [0].OuterXml, "Default InnerXml.OuterXml");
		}

		[Test]
		public void OnlyInner () 
		{
			XmlNodeList inner = InnerXml (""); // empty
			transform.LoadInnerXml (inner);
			XmlNodeList xnl = (XmlNodeList) transform.GetOutput ();
			Assert.AreEqual (0, xnl.Count, "Count");
		}

		private XmlDocument GetDoc () 
		{
			string test = "<catalog><cd><title>Empire Burlesque</title><artist>Bob Dylan</artist><price>10.90</price>";
			test += "<year>1985</year></cd><cd><title>Hide your heart</title><artist>Bonnie Tyler</artist><price>9.90</price>";
			test += "<year>1988</year></cd><cd><title>Greatest Hits</title><artist>Dolly Parton</artist><price>9.90</price>";
			test += "<year>1982</year></cd><cd><title>Still got the blues</title><artist>Gary Moore</artist><price>10.20</price>";
			test += "<year>1990</year></cd><cd><title>Eros</title><artist>Eros Ramazzotti</artist><price>9.90</price>";
			test += "<year>1997</year></cd></catalog>";
			XmlDocument doc = new XmlDocument ();
			doc.LoadXml (test);
			return doc;
		}

		private XmlNodeList InnerXml (string xpathExpr) 
		{
			string xpath = "<XPath xmlns=\"http://www.w3.org/2000/09/xmldsig#\">" + xpathExpr + "</XPath>";
			XmlDocument doc = new XmlDocument ();
			doc.LoadXml (xpath);
			return doc.ChildNodes;
		}

		[Test]
		[Category ("NotWorking")]
		public void LoadInputAsXmlDocument () 
		{
			XmlDocument doc = GetDoc ();
			transform.LoadInput (doc);
			XmlNodeList inner = InnerXml ("//*/title");
			transform.LoadInnerXml (inner);
			XmlNodeList xnl = (XmlNodeList) transform.GetOutput ();
			Assert.AreEqual (73, xnl.Count);
		}

		[Test]
		public void LoadInputAsXmlDocument_EmptyXPath () 
		{
			XmlDocument doc = GetDoc ();
			transform.LoadInput (doc);
			// empty means no LoadInnerXml
			XmlNodeList xnl = (XmlNodeList) transform.GetOutput ();
			Assert.AreEqual (0, xnl.Count, "Empy Result");
		}

		[Test]
		[Category ("NotWorking")]
		public void LoadInputAsXmlNodeList () 
		{
			XmlDocument doc = GetDoc ();
			transform.LoadInput (doc.ChildNodes);
			XmlNodeList inner = InnerXml ("//*/title");
			transform.LoadInnerXml (inner);
			XmlNodeList xnl = (XmlNodeList) transform.GetOutput ();
			Assert.AreEqual (1, xnl.Count);
		}

		[Test]
		public void LoadInputAsXmlNodeList_EmptyXPath () 
		{
			XmlDocument doc = GetDoc ();
			transform.LoadInput (doc.ChildNodes);
			// empty means no LoadInnerXml
			XmlNodeList xnl = (XmlNodeList) transform.GetOutput ();
			Assert.AreEqual (0, xnl.Count, "Empy Result");
		}

		[Test]
		[Category ("NotWorking")]
		public void LoadInputAsStream () 
		{
			XmlDocument doc = GetDoc ();
			doc.PreserveWhitespace = true;
			MemoryStream ms = new MemoryStream ();
			doc.Save (ms);
			ms.Position = 0;
			transform.LoadInput (ms);
			XmlNodeList inner = InnerXml ("//*/title");
			transform.LoadInnerXml (inner);
			XmlNodeList xnl = (XmlNodeList) transform.GetOutput ();
			Assert.AreEqual (73, xnl.Count);
		}

		[Test]
		public void LoadInputAsStream_EmptyXPath () 
		{
			XmlDocument doc = GetDoc ();
			MemoryStream ms = new MemoryStream ();
			doc.Save (ms);
			ms.Position = 0;
			transform.LoadInput (ms);
			// empty means no LoadInnerXml
			XmlNodeList xnl = (XmlNodeList) transform.GetOutput ();
			Assert.AreEqual (0, xnl.Count, "Empy Result");
		}

		[Test]
		public void LoadInnerXml () 
		{
			XmlNodeList inner = InnerXml ("//*");
			transform.LoadInnerXml (inner);
			XmlNodeList xnl = transform.UnprotectedGetInnerXml ();
			Assert.AreEqual (inner, xnl, "LoadInnerXml");
		}

		[Test]
		public void UnsupportedInput () 
		{
			byte[] bad = { 0xBA, 0xD };
			// LAMESPEC: input MUST be one of InputType - but no exception is thrown (not documented)
			transform.LoadInput (bad);
		}

		[Test]
		[ExpectedException (typeof (ArgumentException))]
		public void UnsupportedOutput () 
		{
			XmlDocument doc = new XmlDocument ();
			object o = transform.GetOutput (doc.GetType ());
		}

		[Test]
		public void TransformSimple ()
		{
			XmlDsigXPathTransform t = new XmlDsigXPathTransform ();
			XmlDocument xpdoc = new XmlDocument ();
			string ns = "http://www.w3.org/2000/09/xmldsig#";
			string xpath = "<XPath xmlns='" + ns + "' xmlns:x='urn:foo'>*|@*|namespace::*</XPath>"; // not absolute path.. so @* and namespace::* does not make sense.
			xpdoc.LoadXml (xpath);
			t.LoadInnerXml (xpdoc.ChildNodes);
			XmlDocument doc = new XmlDocument ();
			doc.LoadXml ("<element xmlns='urn:foo'><foo><bar>test</bar></foo></element>");
			t.LoadInput (doc);
			XmlNodeList nl = (XmlNodeList) t.GetOutput ();
			Assert.AreEqual (XmlNodeType.Document, nl [0].NodeType);
			Assert.AreEqual (XmlNodeType.Element, nl [1].NodeType);
			Assert.AreEqual ("element", nl [1].LocalName);
			Assert.AreEqual (XmlNodeType.Element, nl [2].NodeType);
			Assert.AreEqual ("foo", nl [2].LocalName);
			Assert.AreEqual (XmlNodeType.Element, nl [3].NodeType);
			Assert.AreEqual ("bar", nl [3].LocalName);
			// MS.NET bug - ms.net returns ns node even when the
			// current node is ns node (it is like returning
			// attribute from attribute nodes).
//			Assert.AreEqual (XmlNodeType.Attribute, nl [4].NodeType);
//			Assert.AreEqual ("xmlns", nl [4].LocalName);
		}

		[Test]
		[Category ("NotWorking")]
		// MS.NET looks incorrect, or something incorrect in this test code; It turned out nothing to do with function here()
		public void FunctionHereObsolete ()
		{
			XmlDsigXPathTransform t = new XmlDsigXPathTransform ();
			XmlDocument xpdoc = new XmlDocument ();
			string ns = "http://www.w3.org/2000/09/xmldsig#";
//			string xpath = "<XPath xmlns='" + ns + "' xmlns:x='urn:foo'>here()</XPath>";
			string xpath = "<XPath xmlns='" + ns + "' xmlns:x='urn:foo'></XPath>";
			xpdoc.LoadXml (xpath);
			t.LoadInnerXml (xpdoc.ChildNodes);
			XmlDocument doc = new XmlDocument ();

			doc.LoadXml ("<element a='b'><foo><bar>test</bar></foo></element>");
			t.LoadInput (doc);

			XmlNodeList nl = (XmlNodeList) t.GetOutput ();
			Assert.AreEqual (0, nl.Count, "0");

			doc.LoadXml ("<element xmlns='urn:foo'><foo><bar>test</bar></foo></element>");
			t.LoadInput (doc);
			nl = (XmlNodeList) t.GetOutput ();
			Assert.AreEqual (0, nl.Count, "1");

			doc.LoadXml ("<element xmlns='urn:foo'><foo xmlns='urn:bar'><bar>test</bar></foo></element>");
			t.LoadInput (doc);
			nl = (XmlNodeList) t.GetOutput ();
			Assert.AreEqual (0, nl.Count, "2");

			doc.LoadXml ("<element xmlns='urn:foo' xmlns:x='urn:x'><foo xmlns='urn:bar'><bar>test</bar></foo></element>");
			t.LoadInput (doc);
			nl = (XmlNodeList) t.GetOutput ();
			Assert.AreEqual (0, nl.Count, "3");

			doc.LoadXml ("<envelope><Signature xmlns='http://www.w3.org/2000/09/xmldsig#'><XPath>blah</XPath></Signature></envelope>");
			t.LoadInput (doc);
			nl = (XmlNodeList) t.GetOutput ();
			Assert.AreEqual (0, nl.Count, "4");
		}
	}
}
