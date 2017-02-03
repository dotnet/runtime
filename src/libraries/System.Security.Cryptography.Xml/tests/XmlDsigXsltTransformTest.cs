//
// XmlDsigXsltTransformTest.cs - NUnit Test Cases for XmlDsigXsltTransform
//
// Author:
//	Sebastien Pouliot <sebastien@ximian.com>
//	Atsushi Enomoto <atsushi@ximian.com>
//
// (C) 2002, 2003 Motus Technologies Inc. (http://www.motus.com)
// (C) 2004 Novell (http://www.novell.com)
//

using System;
using System.Collections;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Xml;
using System.Xml.Xsl;

using NUnit.Framework;

namespace MonoTests.System.Security.Cryptography.Xml {

	// Note: GetInnerXml is protected in XmlDsigXsltTransform making it
	// difficult to test properly. This class "open it up" :-)
	public class UnprotectedXmlDsigXsltTransform : XmlDsigXsltTransform {
		public UnprotectedXmlDsigXsltTransform ()
		{
		}

		public UnprotectedXmlDsigXsltTransform (bool includeComments)
			: base (includeComments)
		{
		}

		public XmlNodeList UnprotectedGetInnerXml () {
			return base.GetInnerXml ();
		}
	}

	[TestFixture]
	public class XmlDsigXsltTransformTest {

		protected UnprotectedXmlDsigXsltTransform transform;

		[SetUp]
		protected void SetUp () 
		{
			transform = new UnprotectedXmlDsigXsltTransform ();
		}

		[Test] // ctor ()
		public void Constructor1 ()
		{
			CheckProperties (transform);
		}

		[Test] // ctor (Boolean)
		public void Constructor2 ()
		{
			transform = new UnprotectedXmlDsigXsltTransform (true);
			CheckProperties (transform);
			transform = new UnprotectedXmlDsigXsltTransform (false);
			CheckProperties (transform);
		}

		void CheckProperties (XmlDsigXsltTransform transform)
		{
			Assert.AreEqual ("http://www.w3.org/TR/1999/REC-xslt-19991116", transform.Algorithm, "Algorithm");

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
			bool ostream = false;
			foreach (Type t in output) {
				if (t.ToString () == "System.IO.Stream")
					ostream = true;
			}
			Assert.IsTrue (ostream, "Output Stream");
		}

		[Test]
		public void GetInnerXml () 
		{
			XmlNodeList xnl = transform.UnprotectedGetInnerXml ();
			Assert.IsNull (xnl, "Default InnerXml");
		}

		private string Stream2Array (Stream s) 
		{
			StringBuilder sb = new StringBuilder ();
			int b = s.ReadByte ();
			while (b != -1) {
				sb.Append (b.ToString("X2"));
				b = s.ReadByte ();
			}
			return sb.ToString ();
		}


		[Test]
		[ExpectedException (typeof (ArgumentNullException))]
		public void EmptyXslt () 
		{
			string test = "<Test>XmlDsigXsltTransform</Test>";
			XmlDocument doc = new XmlDocument ();
			doc.LoadXml (test);

			transform.LoadInput (doc.ChildNodes);
			Stream s = (Stream) transform.GetOutput ();
		}

		[Test]
		// Note that this is _valid_ as an "embedded stylesheet".
		// (see XSLT spec 2.7)
		public void EmbeddedStylesheet () 
		{
			string test = "<Test xsl:version='1.0' xmlns:xsl='http://www.w3.org/1999/XSL/Transform'>XmlDsigXsltTransform</Test>";
			XmlDocument doc = new XmlDocument ();
			doc.LoadXml (test);

			transform.LoadInnerXml (doc.ChildNodes);
			transform.LoadInput (doc);
			Stream s = (Stream) transform.GetOutput ();
		}

		[Test]
		public void InvalidXslt () 
		{
			bool result = false;
			try {
				string test = "<xsl:element name='foo' xmlns:xsl='http://www.w3.org/1999/XSL/Transform'>XmlDsigXsltTransform</xsl:element>";
				XmlDocument doc = new XmlDocument ();
				doc.LoadXml (test);

				transform.LoadInnerXml (doc.ChildNodes);
				Stream s = (Stream) transform.GetOutput ();
			}
			catch (Exception e) {
				// we must deal with an internal exception
				result = (e.GetType ().ToString ().EndsWith ("XsltLoadException"));
				result = true;
			}
			finally {
				Assert.IsTrue (result, "Exception not thrown");
			}
		}

		[Test]
		[ExpectedException (typeof (ArgumentNullException))]
		public void OnlyInner () 
		{
			string test = "<xsl:stylesheet xmlns:xsl=\"http://www.w3.org/1999/XSL/Transform\" xmlns=\"http://www.w3.org/TR/xhtml1/strict\" version=\"1.0\">";
			test += "<xsl:output encoding=\"UTF-8\" indent=\"no\" method=\"xml\" />";
			test += "<xsl:template match=\"/\"><html><head><title>Notaries</title>";
			test += "</head><body><table><xsl:for-each select=\"Notaries/Notary\">";
			test += "<tr><th><xsl:value-of select=\"@name\" /></th></tr></xsl:for-each>";
			test += "</table></body></html></xsl:template></xsl:stylesheet>";
			XmlDocument doc = new XmlDocument ();
			doc.LoadXml (test);

			transform.LoadInnerXml (doc.ChildNodes);
			Stream s = (Stream) transform.GetOutput ();
			string output = Stream2Array (s);
		}

		private XmlDocument GetXslDoc () 
		{
			string test = "<Transform Algorithm=\"http://www.w3.org/TR/1999/REC-xslt-19991116\" xmlns='http://www.w3.org/2000/09/xmldsig#'>";
			test += "<xsl:stylesheet xmlns:xsl=\"http://www.w3.org/1999/XSL/Transform\" xmlns=\"http://www.w3.org/TR/xhtml1/strict\" version=\"1.0\">";
			test += "<xsl:output encoding=\"UTF-8\" indent=\"no\" method=\"xml\" />";
			test += "<xsl:template match=\"/\"><html><head><title>Notaries</title>";
			test += "</head><body><table><xsl:for-each select=\"Notaries/Notary\">";
			test += "<tr><th><xsl:value-of select=\"@name\" /></th></tr></xsl:for-each>";
			test += "</table></body></html></xsl:template></xsl:stylesheet></Transform>";
			XmlDocument doc = new XmlDocument ();
			doc.LoadXml (test);
			return doc;
		}

		[Test]
		public void LoadInputAsXmlDocument () 
		{
			XmlDocument doc = GetXslDoc ();
			transform.LoadInnerXml (doc.DocumentElement.ChildNodes);
			transform.LoadInput (doc);
			Stream s = (Stream) transform.GetOutput ();
			string output = Stream2Array (s);
		}

		[Test]
		public void LoadInputAsXmlNodeList () 
		{
			XmlDocument doc = GetXslDoc ();
			transform.LoadInnerXml (doc.DocumentElement.ChildNodes);
			transform.LoadInput (doc.ChildNodes);
			Stream s = (Stream) transform.GetOutput ();
			string output = Stream2Array (s);
		}

		[Test]
		public void LoadInputAsStream () 
		{
			XmlDocument doc = GetXslDoc ();
			transform.LoadInnerXml (doc.DocumentElement.ChildNodes);
			MemoryStream ms = new MemoryStream ();
			doc.Save (ms);
			ms.Position = 0;
			transform.LoadInput (ms);
			Stream s = (Stream) transform.GetOutput ();
			string output = Stream2Array (s);
		}

		protected void AreEqual (string msg, XmlNodeList expected, XmlNodeList actual) 
		{
			for (int i=0; i < expected.Count; i++) {
				if (expected[i].OuterXml != actual[i].OuterXml)
					Assert.Fail (msg + " [" + i + "] expected " + expected[i].OuterXml + " bug got " + actual[i].OuterXml);
			}
		}

		[Test]
		public void LoadInnerXml () 
		{
			XmlDocument doc = GetXslDoc ();
			transform.LoadInnerXml (doc.DocumentElement.ChildNodes);
			XmlNodeList xnl = transform.UnprotectedGetInnerXml ();
			AssertNodeListEqual (doc.DocumentElement.ChildNodes, xnl, "LoadInnerXml");
		}
		
		void AssertNodeListEqual (XmlNodeList nl1, XmlNodeList nl2, string label)
		{
			Assert.AreEqual (nl1.Count, nl2.Count, label + ": node count");
			IEnumerator e1, e2;
			int i;
			for (i = 0, e1 = nl1.GetEnumerator (), e2 = nl2.GetEnumerator (); e1.MoveNext (); i++) {
				Assert.IsTrue (e2.MoveNext (), label + " : nl2.MoveNext");
				Assert.AreEqual (e1.Current, e2.Current, label + " : node at " + i);
			}
			Assert.IsFalse (e2.MoveNext (), label + " : nl2 has extras");
		}

		[Test]
		public void Load2 () 
		{
			XmlDocument doc = GetXslDoc ();
			transform.LoadInnerXml (doc.DocumentElement.ChildNodes);
			transform.LoadInput (doc);
			Stream s = (Stream) transform.GetOutput ();
			string output = Stream2Array (s);
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
			XmlDocument doc = new XmlDocument();
			object o = transform.GetOutput (doc.GetType ());
		}
	}
}
