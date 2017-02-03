//
// XmlDsigExcC14NTransformTest.cs - NUnit Test Cases for XmlDsigExcC14NTransform
//
// Author:
//  original:
//	Sebastien Pouliot <sebastien@ximian.com>
//	Aleksey Sanin (aleksey@aleksey.com)
//  this file:
//	Atsushi Enomoto <atsushi@ximian.com>
//
// (C) 2002, 2003 Motus Technologies Inc. (http://www.motus.com)
// (C) 2003 Aleksey Sanin (aleksey@aleksey.com)
// (C) 2004 Novell (http://www.novell.com)
//

//
// WARNING!!
//	This file is simply replaced C14N->ExcC14N, and then replaced expected
//	output XML. So, they are *not* from c14n specification.
//


using System;
using System.IO;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Xml;

using NUnit.Framework;

namespace MonoTests.System.Security.Cryptography.Xml {

	// Note: GetInnerXml is protected in XmlDsigExcC14NTransform making it
	// difficult to test properly. This class "open it up" :-)
	public class UnprotectedXmlDsigExcC14NTransform : XmlDsigExcC14NTransform {
		public UnprotectedXmlDsigExcC14NTransform ()
		{
		}

		public UnprotectedXmlDsigExcC14NTransform (bool includeComments)
			: base (includeComments)
		{
		}

		public UnprotectedXmlDsigExcC14NTransform (string inclusiveNamespacesPrefixList)
			: base (inclusiveNamespacesPrefixList)
		{
		}

		public UnprotectedXmlDsigExcC14NTransform (bool includeComments, string inclusiveNamespacesPrefixList)
			: base (includeComments, inclusiveNamespacesPrefixList)
		{
		}

		public XmlNodeList UnprotectedGetInnerXml () {
			return base.GetInnerXml ();
		}
	}

	[TestFixture]
	public class XmlDsigExcC14NTransformTest {

		protected UnprotectedXmlDsigExcC14NTransform transform;

		[SetUp]
		protected void SetUp () 
		{
			transform = new UnprotectedXmlDsigExcC14NTransform ();
		}

		[TearDown]
		protected void CleanUp () 
		{
			try {
				if (File.Exists ("doc.dtd"))
					File.Delete ("doc.dtd");
				if (File.Exists ("world.txt"))
					File.Delete ("world.txt");
			}
			catch {}
		}

		[Test] // ctor ()
		public void Constructor1 ()
		{
			Assert.AreEqual ("http://www.w3.org/2001/10/xml-exc-c14n#", transform.Algorithm, "Algorithm");
			Assert.IsNull (transform.InclusiveNamespacesPrefixList, "InclusiveNamespacesPrefixList");
			CheckProperties (transform);
		}

		[Test] // ctor (Boolean)
		public void Constructor2 ()
		{
			transform = new UnprotectedXmlDsigExcC14NTransform (true);
			Assert.AreEqual ("http://www.w3.org/2001/10/xml-exc-c14n#WithComments", transform.Algorithm, "Algorithm");
			Assert.IsNull (transform.InclusiveNamespacesPrefixList, "InclusiveNamespacesPrefixList");
			CheckProperties (transform);

			transform = new UnprotectedXmlDsigExcC14NTransform (false);
			Assert.AreEqual ("http://www.w3.org/2001/10/xml-exc-c14n#", transform.Algorithm, "Algorithm");
			Assert.IsNull (transform.InclusiveNamespacesPrefixList, "InclusiveNamespacesPrefixList");
			CheckProperties (transform);
		}

		[Test] // ctor (String)
		public void Constructor3 ()
		{
			transform = new UnprotectedXmlDsigExcC14NTransform (null);
			Assert.AreEqual ("http://www.w3.org/2001/10/xml-exc-c14n#", transform.Algorithm, "Algorithm");
			Assert.IsNull (transform.InclusiveNamespacesPrefixList, "InclusiveNamespacesPrefixList");
			CheckProperties (transform);

			transform = new UnprotectedXmlDsigExcC14NTransform (string.Empty);
			Assert.AreEqual ("http://www.w3.org/2001/10/xml-exc-c14n#", transform.Algorithm, "Algorithm");
			Assert.AreEqual (string.Empty, transform.InclusiveNamespacesPrefixList, "InclusiveNamespacesPrefixList");
			CheckProperties (transform);

			transform = new UnprotectedXmlDsigExcC14NTransform ("#default xsd");
			Assert.AreEqual ("http://www.w3.org/2001/10/xml-exc-c14n#", transform.Algorithm, "Algorithm");
			Assert.AreEqual ("#default xsd", transform.InclusiveNamespacesPrefixList, "InclusiveNamespacesPrefixList");
			CheckProperties (transform);
		}

		[Test] // ctor (Boolean, String)
		public void Constructor4 ()
		{
			transform = new UnprotectedXmlDsigExcC14NTransform (true, null);
			Assert.AreEqual ("http://www.w3.org/2001/10/xml-exc-c14n#WithComments", transform.Algorithm, "Algorithm");
			Assert.IsNull (transform.InclusiveNamespacesPrefixList, "InclusiveNamespacesPrefixList");
			CheckProperties (transform);

			transform = new UnprotectedXmlDsigExcC14NTransform (true, string.Empty);
			Assert.AreEqual ("http://www.w3.org/2001/10/xml-exc-c14n#WithComments", transform.Algorithm, "Algorithm");
			Assert.AreEqual (string.Empty, transform.InclusiveNamespacesPrefixList, "InclusiveNamespacesPrefixList");
			CheckProperties (transform);

			transform = new UnprotectedXmlDsigExcC14NTransform (true, "#default xsd");
			Assert.AreEqual ("http://www.w3.org/2001/10/xml-exc-c14n#WithComments", transform.Algorithm, "Algorithm");
			Assert.AreEqual ("#default xsd", transform.InclusiveNamespacesPrefixList, "InclusiveNamespacesPrefixList");
			CheckProperties (transform);

			transform = new UnprotectedXmlDsigExcC14NTransform (false, null);
			Assert.AreEqual ("http://www.w3.org/2001/10/xml-exc-c14n#", transform.Algorithm, "Algorithm");
			Assert.IsNull (transform.InclusiveNamespacesPrefixList, "InclusiveNamespacesPrefixList");
			CheckProperties (transform);

			transform = new UnprotectedXmlDsigExcC14NTransform (false, string.Empty);
			Assert.AreEqual ("http://www.w3.org/2001/10/xml-exc-c14n#", transform.Algorithm, "Algorithm");
			Assert.AreEqual (string.Empty, transform.InclusiveNamespacesPrefixList, "InclusiveNamespacesPrefixList");
			CheckProperties (transform);

			transform = new UnprotectedXmlDsigExcC14NTransform (false, "#default xsd");
			Assert.AreEqual ("http://www.w3.org/2001/10/xml-exc-c14n#", transform.Algorithm, "Algorithm");
			Assert.AreEqual ("#default xsd", transform.InclusiveNamespacesPrefixList, "InclusiveNamespacesPrefixList");
			CheckProperties (transform);
		}

		void CheckProperties (XmlDsigExcC14NTransform transform)
		{
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
		public void Types ()
		{
			Type [] input = transform.InputTypes;
			input [0] = null;
			input [1] = null;
			input [2] = null;
			// property does not return a clone
			foreach (Type t in transform.InputTypes) {
				Assert.IsNull (t);
			}
			// it's not a static array
			XmlDsigExcC14NTransform t2 = new XmlDsigExcC14NTransform ();
			foreach (Type t in t2.InputTypes) {
				Assert.IsNotNull (t);
			}
		}

		[Test]
		public void GetInnerXml () 
		{
			XmlNodeList xnl = transform.UnprotectedGetInnerXml ();
			Assert.IsNull (xnl, "Default InnerXml");
		}

		private string Stream2String (Stream s) 
		{
			StringBuilder sb = new StringBuilder ();
			int b = s.ReadByte ();
			while (b != -1) {
				sb.Append (Convert.ToChar (b));
				b = s.ReadByte ();
			}
			return sb.ToString ();
		}

		static string xml = "<Test  attrib='at ' xmlns=\"http://www.go-mono.com/\" > \r\n &#xD; <Toto/> text &amp; </Test   >";
		// BAD for XmlDocument input (framework 1.0 result)
		static string c14xml1 = "<Test xmlns=\"http://www.go-mono.com/\" attrib=\"at \"> \r\n \r <Toto></Toto> text &amp; </Test>";
		// GOOD for Stream input
		static string c14xml2 = "<Test xmlns=\"http://www.go-mono.com/\" attrib=\"at \"> \n &#xD; <Toto></Toto> text &amp; </Test>";
		// GOOD for XmlDocument input. The difference is because once
		// xml string is loaded to XmlDocument, there is no difference
		// between \r and &#xD;, so every \r must be handled as &#xD;.
		static string c14xml3 = "<Test xmlns=\"http://www.go-mono.com/\" attrib=\"at \"> &#xD;\n &#xD; <Toto></Toto> text &amp; </Test>";

		private XmlDocument GetDoc () 
		{
			XmlDocument doc = new XmlDocument ();
			doc.PreserveWhitespace = true;
			doc.LoadXml (xml);
			return doc;
		}

		[Test]
		public void LoadInputAsXmlDocument () 
		{
			XmlDocument doc = GetDoc ();
			transform.LoadInput (doc);
			Stream s = (Stream) transform.GetOutput ();
			string output = Stream2String (s);
			Assert.AreEqual (c14xml3, output, "XmlDocument");
		}

		[Test]
		[Category ("NotDotNet")]
		// see LoadInputAsXmlNodeList2 description
		public void LoadInputAsXmlNodeList () 
		{
			XmlDocument doc = GetDoc ();
			// Argument list just contains element Test.
			transform.LoadInput (doc.ChildNodes);
			Stream s = (Stream) transform.GetOutput ();
			string output = Stream2String (s);
			Assert.AreEqual ("<Test></Test>", output, "XmlChildNodes");
		}

		[Test]
		[Category ("NotDotNet")]
		// MS has a bug that those namespace declaration nodes in
		// the node-set are written to output. Related spec section is:
		// http://www.w3.org/TR/2001/REC-xml-c14n-20010315#ProcessingModel
		public void LoadInputAsXmlNodeList2 () 
		{
			XmlDocument doc = GetDoc ();
			transform.LoadInput (doc.SelectNodes ("//*"));
			Stream s = (Stream) transform.GetOutput ();
			string output = Stream2String (s);
			string expected = @"<Test><Toto></Toto></Test>";
			Assert.AreEqual (expected, output, "XmlChildNodes");
		}

		[Test]
		public void LoadInputAsStream () 
		{
			MemoryStream ms = new MemoryStream ();
			byte[] x = Encoding.ASCII.GetBytes (xml);
			ms.Write (x, 0, x.Length);
			ms.Position = 0;
			transform.LoadInput (ms);
			Stream s = (Stream) transform.GetOutput ();
			string output = Stream2String (s);
			Assert.AreEqual (c14xml2, output, "MemoryStream");
		}

		[Test]
		[Ignore ("LAMESPEC: input MUST be one of InputType - but no exception is thrown (not documented)")]
		public void LoadInputWithUnsupportedType () 
		{
			byte[] bad = { 0xBA, 0xD };
			transform.LoadInput (bad);
		}

		[Test]
		[ExpectedException (typeof (ArgumentException))]
		public void UnsupportedOutput () 
		{
			XmlDocument doc = new XmlDocument();
			object o = transform.GetOutput (doc.GetType ());
		}

	        [Test]
	        public void ExcC14NSpecExample1 ()
	        {
			using (StreamWriter sw = new StreamWriter ("doc.dtd", false, Encoding.ASCII)) {
				sw.Write ("<!-- presence, not content, required -->");
				sw.Close ();
			}
			string res = ExecuteXmlDSigExcC14NTransform (ExcC14NSpecExample1Input);
			Assert.AreEqual (ExcC14NSpecExample1Output, res, "Example 1 from c14n spec - PIs, Comments, and Outside of Document Element (without comments)");
	        }

	        [Test]
	        public void ExcC14NSpecExample2 ()
	        {
			string res = ExecuteXmlDSigExcC14NTransform (ExcC14NSpecExample2Input);
			Assert.AreEqual (ExcC14NSpecExample2Output, res, "Example 2 from c14n spec - Whitespace in Document Content (without comments)");
		}

	        [Test]
	        public void ExcC14NSpecExample3 ()
	        {
	    		string res = ExecuteXmlDSigExcC14NTransform (ExcC14NSpecExample3Input);
	    		Assert.AreEqual (ExcC14NSpecExample3Output, res, "Example 3 from c14n spec - Start and End Tags (without comments)");
	        }
	    
	        [Test]
//		[Ignore ("This test should be fine, but it does not pass under MS.NET")]
	        public void ExcC14NSpecExample4 ()
	        {
	    		string res = ExecuteXmlDSigExcC14NTransform (ExcC14NSpecExample4Input);
	    		Assert.AreEqual (ExcC14NSpecExample4Output, res, "Example 4 from c14n spec - Character Modifications and Character References (without comments)");
	        }
	    
	        [Test]
	        public void ExcC14NSpecExample5 ()
	        {
			using (StreamWriter sw = new StreamWriter ("world.txt", false, Encoding.ASCII)) {
				sw.Write ("world");
				sw.Close ();
			}
	    	    	string res = ExecuteXmlDSigExcC14NTransform (ExcC14NSpecExample5Input);
	    	    	Assert.AreEqual (ExcC14NSpecExample5Output, res, "Example 5 from c14n spec - Entity References (without comments)");
	        }
    
		[Test]
	        public void ExcC14NSpecExample6 ()
	        {
	    	    	string res = ExecuteXmlDSigExcC14NTransform (ExcC14NSpecExample6Input);
	    	    	Assert.AreEqual (ExcC14NSpecExample6Output, res, "Example 6 from c14n spec - UTF-8 Encoding (without comments)");
	        }

		private string ExecuteXmlDSigExcC14NTransform (string InputXml)
		{
			XmlDocument doc = new XmlDocument ();
			doc.PreserveWhitespace = true;
			doc.LoadXml (InputXml);
		
			// Testing default attribute support with
			// vreader.ValidationType = ValidationType.None.
			//
			UTF8Encoding utf8 = new UTF8Encoding ();
			byte[] data = utf8.GetBytes (InputXml.ToString ());
			Stream stream = new MemoryStream (data);
			XmlTextReader reader = new XmlTextReader (stream);
			XmlValidatingReader vreader = new XmlValidatingReader (reader);
			vreader.ValidationType = ValidationType.None;
			vreader.EntityHandling = EntityHandling.ExpandCharEntities;
			doc.Load (vreader);

			transform.LoadInput (doc);
			return Stream2String ((Stream)transform.GetOutput ());
		}

	        //
	        // Example 1 from ExcC14N spec - PIs, Comments, and Outside of Document Element: 
	        // http://www.w3.org/TR/xml-c14n#Example-OutsideDoc
	        // 
	        // Aleksey: 
	        // removed reference to an empty external DTD
	        //
	        static string ExcC14NSpecExample1Input =  
	    	        "<?xml version=\"1.0\"?>\n" +
	    	        "\n" +
	    	        "<?xml-stylesheet   href=\"doc.xsl\"\n" +
	    	        "   type=\"text/xsl\"   ?>\n" +
	    	        "\n" +
	    	        // "<!DOCTYPE doc SYSTEM \"doc.dtd\">\n" +
	    	        "\n" +
	    	        "<doc>Hello, world!<!-- Comment 1 --></doc>\n" +
	    	        "\n" +
	    	        "<?pi-without-data     ?>\n\n" +
	    	        "<!-- Comment 2 -->\n\n" +
	    	        "<!-- Comment 3 -->\n";
	        static string ExcC14NSpecExample1Output =   
	    	        "<?xml-stylesheet href=\"doc.xsl\"\n" +
	    	        "   type=\"text/xsl\"   ?>\n" +
	    	        "<doc>Hello, world!</doc>\n" +
	    	        "<?pi-without-data?>";
	        
	        //
	        // Example 2 from ExcC14N spec - Whitespace in Document Content: 
	        // http://www.w3.org/TR/xml-c14n#Example-WhitespaceInContent
	        // 
	        static string ExcC14NSpecExample2Input =  
	    	        "<doc>\n" +
	    	        "  <clean>   </clean>\n" +
	    	        "   <dirty>   A   B   </dirty>\n" +
	    	        "   <mixed>\n" +
	    	        "      A\n" +
	    	        "      <clean>   </clean>\n" +
	    	        "      B\n" +
	    	        "      <dirty>   A   B   </dirty>\n" +
	    	        "      C\n" +
	    	        "   </mixed>\n" +
	    	        "</doc>\n";
	        static string ExcC14NSpecExample2Output =   
	    	        "<doc>\n" +
	    	        "  <clean>   </clean>\n" +
	    	        "   <dirty>   A   B   </dirty>\n" +
	    	        "   <mixed>\n" +
	    	        "      A\n" +
	    	        "      <clean>   </clean>\n" +
	    	        "      B\n" +
	    	        "      <dirty>   A   B   </dirty>\n" +
	    	        "      C\n" +
	    	        "   </mixed>\n" +
	    	        "</doc>";
	    
	        //
	        // Example 3 from ExcC14N spec - Start and End Tags: 
	        // http://www.w3.org/TR/xml-c14n#Example-SETags
	        //
	        static string ExcC14NSpecExample3Input =  
	    	        "<!DOCTYPE doc [<!ATTLIST e9 attr CDATA \"default\">]>\n" +
	    	        "<doc>\n" +
	    	        "   <e1   />\n" +
	    	        "   <e2   ></e2>\n" +
	    	        "   <e3    name = \"elem3\"   id=\"elem3\"    />\n" +
	    	        "   <e4    name=\"elem4\"   id=\"elem4\"    ></e4>\n" +
	    	        "   <e5 a:attr=\"out\" b:attr=\"sorted\" attr2=\"all\" attr=\"I\'m\"\n" +
	    	        "       xmlns:b=\"http://www.ietf.org\" \n" +
	    	        "       xmlns:a=\"http://www.w3.org\"\n" +
	    	        "       xmlns=\"http://www.uvic.ca\"/>\n" +
	    	        "   <e6 xmlns=\"\" xmlns:a=\"http://www.w3.org\">\n" +
	    	        "       <e7 xmlns=\"http://www.ietf.org\">\n" +
	    	        "           <e8 xmlns=\"\" xmlns:a=\"http://www.w3.org\">\n" +
	    	        "               <e9 xmlns=\"\" xmlns:a=\"http://www.ietf.org\"/>\n" +
	    	        "           </e8>\n" +
	    	        "       </e7>\n" +
	    	        "   </e6>\n" +
	    	        "</doc>\n";
	        static string ExcC14NSpecExample3Output =   
	    	        "<doc>\n" +
	    	        "   <e1></e1>\n" +
	    	        "   <e2></e2>\n" +
	    	        "   <e3 id=\"elem3\" name=\"elem3\"></e3>\n" +
	    	        "   <e4 id=\"elem4\" name=\"elem4\"></e4>\n" +
	    	        "   <e5 xmlns=\"http://www.uvic.ca\" xmlns:a=\"http://www.w3.org\" xmlns:b=\"http://www.ietf.org\" attr=\"I\'m\" attr2=\"all\" b:attr=\"sorted\" a:attr=\"out\"></e5>\n" +
    	    	        "   <e6>\n" +
	    	        "       <e7 xmlns=\"http://www.ietf.org\">\n" +
	    	        "           <e8 xmlns=\"\">\n" +
	    	        "               <e9 attr=\"default\"></e9>\n" +
//	    	        "               <e9 xmlns:a=\"http://www.ietf.org\"></e9>\n" +
	    	        "           </e8>\n" +
	    	        "       </e7>\n" +
	    	        "   </e6>\n" +
	    	        "</doc>";
	    
	    
	        //
	        // Example 4 from ExcC14N spec - Character Modifications and Character References: 
	        // http://www.w3.org/TR/xml-c14n#Example-Chars
	        //
	        // Aleksey: 
	        // This test does not include "normId" element
	        // because it has an invalid ID attribute "id" which
	        // should be normalized by XML parser. Currently Mono
	        // does not support this (see comment after this example
	        // in the spec).
	        static string ExcC14NSpecExample4Input =  
	    	        "<!DOCTYPE doc [<!ATTLIST normId id ID #IMPLIED>]>\n" +
	    	        "<doc>\n" +
	    	        "   <text>First line&#x0d;&#10;Second line</text>\n" +
	    	        "   <value>&#x32;</value>\n" +
	    	        "   <compute><![CDATA[value>\"0\" && value<\"10\" ?\"valid\":\"error\"]]></compute>\n" +
	    	        "   <compute expr=\'value>\"0\" &amp;&amp; value&lt;\"10\" ?\"valid\":\"error\"\'>valid</compute>\n" +
	    	        "   <norm attr=\' &apos;   &#x20;&#13;&#xa;&#9;   &apos; \'/>\n" +
	    	        // "   <normId id=\' &apos;   &#x20;&#13;&#xa;&#9;   &apos; \'/>\n" +
	    	        "</doc>\n";
	        static string ExcC14NSpecExample4Output =   
	    	        "<doc>\n" +
	    	        "   <text>First line&#xD;\n" +
	    	        "Second line</text>\n" +
	    	        "   <value>2</value>\n" +
	    	        "   <compute>value&gt;\"0\" &amp;&amp; value&lt;\"10\" ?\"valid\":\"error\"</compute>\n" +
	    	        "   <compute expr=\"value>&quot;0&quot; &amp;&amp; value&lt;&quot;10&quot; ?&quot;valid&quot;:&quot;error&quot;\">valid</compute>\n" +
	    	        "   <norm attr=\" \'    &#xD;&#xA;&#x9;   \' \"></norm>\n" +
	    	        // "   <normId id=\"\' &#xD;&#xA;&#x9; \'\"></normId>\n" +
	    	        "</doc>";
	    
	        //
	        // Example 5 from ExcC14N spec - Entity References: 
	        // http://www.w3.org/TR/xml-c14n#Example-Entities
	        //
	        static string ExcC14NSpecExample5Input =  
	    	        "<!DOCTYPE doc [\n" +
	    	        "<!ATTLIST doc attrExtEnt ENTITY #IMPLIED>\n" +
	    	        "<!ENTITY ent1 \"Hello\">\n" +
	    	        "<!ENTITY ent2 SYSTEM \"world.txt\">\n" +
	    	        "<!ENTITY entExt SYSTEM \"earth.gif\" NDATA gif>\n" +
	    	        "<!NOTATION gif SYSTEM \"viewgif.exe\">\n" +
	    	        "]>\n" +
	    	        "<doc attrExtEnt=\"entExt\">\n" +
	    	        "   &ent1;, &ent2;!\n" +
	    	        "</doc>\n" +
	    	        "\n" +
	    	        "<!-- Let world.txt contain \"world\" (excluding the quotes) -->\n";
	        static string ExcC14NSpecExample5Output =  
	    	        "<doc attrExtEnt=\"entExt\">\n" +
	    	        "   Hello, world!\n" +
	    	        "</doc>";	    
    
    	        //
	        // Example 6 from ExcC14N spec - UTF-8 Encoding: 
	        // http://www.w3.org/TR/xml-c14n#Example-UTF8
	        // 
	        static string ExcC14NSpecExample6Input =  
    	    	        "<?xml version=\"1.0\" encoding=\"ISO-8859-1\"?>\n" +
    	    	        "<doc>&#169;</doc>\n";
	        static string ExcC14NSpecExample6Output =  
	    	        "<doc>\xC2\xA9</doc>";
	    

	        //
	        // Example 7 from ExcC14N spec - Document Subsets: 
	        // http://www.w3.org/TR/xml-c14n#Example-DocSubsets
	        // 
		// Aleksey:
		// Well, XPath support in Mono is far from complete....
		// I was not able to simplify the xpath expression from this test
		// so it runs on Mono and still makes sense for testing this feature.
		// Thus this test is not included in the suite now.
	        static string ExcC14NSpecExample7Input =  
	    	        "<!DOCTYPE doc [\n" +
	    	        "<!ATTLIST e2 xml:space (default|preserve) \'preserve\'>\n" +
	    	        "<!ATTLIST e3 id ID #IMPLIED>\n" +
	    	        "]>\n" +
	    	        "<doc xmlns=\"http://www.ietf.org\" xmlns:w3c=\"http://www.w3.org\">\n" +
	    	        "   <e1>\n" +
	    	        "      <e2 xmlns=\"\">\n" +
	    	        "         <e3 id=\"E3\"/>\n" +
	    	        "      </e2>\n" +
	    	        "   </e1>\n" +
	    	        "</doc>\n";

	        static string ExcC14NSpecExample7Xpath =  
    	    	        "(//.|//@*|//namespace::*)\n" +
	    	        "[\n" +
	    		"self::ietf:e1\n" +
	    		"    or\n" +
	    	        "(parent::ietf:e1 and not(self::text() or self::e2))\n" +
	    	        "    or\n" +
	    	        "count(id(\"E3\")|ancestor-or-self::node()) = count(ancestor-or-self::node())\n" +
	    	        "]";
	        static string ExcC14NSpecExample7Output =  
	    	        "<e1 xmlns=\"http://www.ietf.org\" xmlns:w3c=\"http://www.w3.org\"><e3 xmlns=\"\" id=\"E3\" xml:space=\"preserve\"></e3></e1>";

		[Test]
		public void SimpleNamespacePrefixes ()
		{
			string input = "<a:Action xmlns:a='urn:foo'>http://tempuri.org/IFoo/Echo</a:Action>";
			string expected = @"<a:Action xmlns:a=""urn:foo"">http://tempuri.org/IFoo/Echo</a:Action>";

			XmlDocument doc = new XmlDocument ();
			doc.LoadXml (input);
			XmlDsigExcC14NTransform t = new XmlDsigExcC14NTransform ();
			t.LoadInput (doc);
			Stream s = t.GetOutput () as Stream;
			Assert.AreEqual (new StreamReader (s, Encoding.UTF8).ReadToEnd (), expected);
		}

		[Test]
		[ExpectedException (typeof (NullReferenceException))]
		public void GetDigestedOutput_Null ()
		{
			new XmlDsigExcC14NTransform ().GetDigestedOutput (null);
		}
	}    
}

