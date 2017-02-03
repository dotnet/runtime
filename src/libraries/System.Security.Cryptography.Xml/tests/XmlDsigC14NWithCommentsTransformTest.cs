//
// XmlDsigC14NWithCommentsTransformTest.cs 
//	- NUnit Test Cases for XmlDsigC14NWithCommentsTransform
//
// Author:
//	Sebastien Pouliot <sebastien@ximian.com>
//
// (C) 2002, 2003 Motus Technologies Inc. (http://www.motus.com)
// Copyright (C) 2004-2005 Novell, Inc (http://www.novell.com)
//

using System;
using System.IO;
using System.Security;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Xml;

using NUnit.Framework;

namespace MonoTests.System.Security.Cryptography.Xml {

	// Note: GetInnerXml is protected in XmlDsigC14NWithCommentsTransform
	// making it difficult to test properly. This class "open it up" :-)
	public class UnprotectedXmlDsigC14NWithCommentsTransform : XmlDsigC14NWithCommentsTransform {

		public XmlNodeList UnprotectedGetInnerXml () {
			return base.GetInnerXml ();
		}
	}

	[TestFixture]
	public class XmlDsigC14NWithCommentsTransformTest {

		protected UnprotectedXmlDsigC14NWithCommentsTransform transform;

		[SetUp]
		protected void SetUp () 
		{
			transform = new UnprotectedXmlDsigC14NWithCommentsTransform ();
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
			if (File.Exists ("doc.dtd"))
				throw new Exception ("File.Delete() is not working.");
			if (File.Exists ("world.txt"))
				throw new Exception ("File.Delete() is not working.");
		}

		[Test]
		public void Properties () 
		{
			Assert.AreEqual ("http://www.w3.org/TR/2001/REC-xml-c14n-20010315#WithComments", transform.Algorithm, "Algorithm");

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
			Assert.AreEqual (null, xnl, "Default InnerXml");
		}

		[ExpectedException (typeof (ArgumentException))]
		public void LoadInputWithUnsupportedType () 
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

	        [Test]
	        public void C14NSpecExample1 ()
	        {
			using (StreamWriter sw = new StreamWriter ("doc.dtd", false, Encoding.ASCII)) {
				sw.Write ("<!-- presence, not content, required -->");
				sw.Close ();
			}
			string res = ExecuteXmlDSigC14NTransform (C14NSpecExample1Input, true);
			Assert.AreEqual (C14NSpecExample1Output, res, "Example 1 from c14n spec - PIs, Comments, and Outside of Document Element (with comments)");
	        }

		[Test]
		[ExpectedException (typeof (SecurityException))]
		public void C14NSpecExample1_WithoutResolver ()
		{
			using (StreamWriter sw = new StreamWriter ("doc.dtd", false, Encoding.ASCII)) {
				sw.Write ("<!-- presence, not content, required -->");
				sw.Close ();
			}
			if (!SecurityManager.SecurityEnabled)
				NUnit.Framework.Assert.Ignore ("SecurityManager isn't enabled.");
			string res = ExecuteXmlDSigC14NTransform (C14NSpecExample1Input, false);
			Assert.AreEqual (C14NSpecExample1Output, res, "Example 1 from c14n spec - PIs, Comments, and Outside of Document Element (with comments)");
		}

	        [Test]
	        public void C14NSpecExample2 ()
	        {
			string res = ExecuteXmlDSigC14NTransform (C14NSpecExample2Input, false);
			Assert.AreEqual (C14NSpecExample2Output, res, "Example 2 from c14n spec - Whitespace in Document Content (with comments)");
		}

	        [Test]
	        public void C14NSpecExample3 ()
	        {
	    		string res = ExecuteXmlDSigC14NTransform (C14NSpecExample3Input, false);
	    		Assert.AreEqual (C14NSpecExample3Output, res, "Example 3 from c14n spec - Start and End Tags (with comments)");
	        }
	    
	        [Test]
	        public void C14NSpecExample4 ()
	        {
	    		string res = ExecuteXmlDSigC14NTransform (C14NSpecExample4Input, false);
	    		Assert.AreEqual (C14NSpecExample4Output, res, "Example 4 from c14n spec - Character Modifications and Character References (with comments)");
	        }
	    
	        [Test]
	        public void C14NSpecExample5 ()
	        {
			if (!File.Exists ("world.txt")) {
				using (StreamWriter sw = new StreamWriter ("world.txt", false, Encoding.ASCII)) {
					sw.Write ("world");
					sw.Close ();
				}
			}
	    	    	string res = ExecuteXmlDSigC14NTransform (C14NSpecExample5Input, false);
	    	    	Assert.AreEqual (C14NSpecExample5Output, res, "Example 5 from c14n spec - Entity References (with comments)");
	        }
    
	        [Test]
	        public void C14NSpecExample6 ()
	        {
	    	    	string res = ExecuteXmlDSigC14NTransform (C14NSpecExample6Input, false);
	    	    	Assert.AreEqual (C14NSpecExample6Output, res, "Example 6 from c14n spec - UTF-8 Encoding (with comments)");
	        }

		private string ExecuteXmlDSigC14NTransform (string InputXml, bool resolver)
		{
			XmlDocument doc = new XmlDocument ();
			doc.PreserveWhitespace = true;
			doc.LoadXml (InputXml);
		
			// Testing default attribute support with
			// vreader.ValidationType = ValidationType.None.
			UTF8Encoding utf8 = new UTF8Encoding ();
			byte[] data = utf8.GetBytes (InputXml.ToString ());
			Stream stream = new MemoryStream (data);
			XmlTextReader reader = new XmlTextReader (stream);
			XmlValidatingReader vreader = new XmlValidatingReader (reader);
			vreader.ValidationType = ValidationType.None;
			vreader.EntityHandling = EntityHandling.ExpandCharEntities;
			doc.Load (vreader);
			if (resolver)
				transform.Resolver = new XmlUrlResolver ();
			transform.LoadInput (doc);
			return Stream2String ((Stream)transform.GetOutput ());
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

	        //
	        // Example 1 from C14N spec - PIs, Comments, and Outside of Document Element: 
	        // http://www.w3.org/TR/xml-c14n#Example-OutsideDoc
	        //
	        static string C14NSpecExample1Input =  
	    		"<?xml version=\"1.0\"?>\n" +
	    	        "\n" +
	    		    "<?xml-stylesheet   href=\"doc.xsl\"\n" +
	    		    "   type=\"text/xsl\"   ?>\n" +
		    	    "\n" +
		    	    "<!DOCTYPE doc SYSTEM \"doc.dtd\">\n" +
		    	    "\n" +
		    	    "<doc>Hello, world!<!-- Comment 1 --></doc>\n" +
		    	    "\n" +
		    	    "<?pi-without-data     ?>\n\n" +
		    	    "<!-- Comment 2 -->\n\n" +
		    	    "<!-- Comment 3 -->\n";
	        static string C14NSpecExample1Output =   
		    	    "<?xml-stylesheet href=\"doc.xsl\"\n" +
		    	    "   type=\"text/xsl\"   ?>\n" +
		    	    "<doc>Hello, world!<!-- Comment 1 --></doc>\n" +
		    	    "<?pi-without-data?>\n" +
		    	    "<!-- Comment 2 -->\n" +
		    	    "<!-- Comment 3 -->";
	        
	        //
	        // Example 2 from C14N spec - Whitespace in Document Content: 
	        // http://www.w3.org/TR/xml-c14n#Example-WhitespaceInContent
	        // 
	        static string C14NSpecExample2Input =  
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
	        static string C14NSpecExample2Output =   
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
	        // Example 3 from C14N spec - Start and End Tags: 
	        // http://www.w3.org/TR/xml-c14n#Example-SETags
	        //
	        static string C14NSpecExample3Input =  
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
	        static string C14NSpecExample3Output =   
		    	    "<doc>\n" +
		    	    "   <e1></e1>\n" +
		    	    "   <e2></e2>\n" +
		    	    "   <e3 id=\"elem3\" name=\"elem3\"></e3>\n" +
		    	    "   <e4 id=\"elem4\" name=\"elem4\"></e4>\n" +
		    	    "   <e5 xmlns=\"http://www.uvic.ca\" xmlns:a=\"http://www.w3.org\" xmlns:b=\"http://www.ietf.org\" attr=\"I\'m\" attr2=\"all\" b:attr=\"sorted\" a:attr=\"out\"></e5>\n" +
    		    	    "   <e6 xmlns:a=\"http://www.w3.org\">\n" +
		    	    "       <e7 xmlns=\"http://www.ietf.org\">\n" +
		    	    "           <e8 xmlns=\"\">\n" +
		    	    "               <e9 xmlns:a=\"http://www.ietf.org\" attr=\"default\"></e9>\n" +
//		    	    "               <e9 xmlns:a=\"http://www.ietf.org\"></e9>\n" +
		    	    "           </e8>\n" +
		    	    "       </e7>\n" +
		    	    "   </e6>\n" +
		    	    "</doc>";
	    
	    
	        //
	        // Example 4 from C14N spec - Character Modifications and Character References: 
	        // http://www.w3.org/TR/xml-c14n#Example-Chars
	        //
	        // Aleksey: 
	        // This test does not include "normId" element
	        // because it has an invalid ID attribute "id" which
	        // should be normalized by XML parser. Currently Mono
	        // does not support this (see comment after this example
	        // in the spec).
	        static string C14NSpecExample4Input =  
		    	    "<!DOCTYPE doc [<!ATTLIST normId id ID #IMPLIED>]>\n" +
		    	    "<doc>\n" +
		    	    "   <text>First line&#x0d;&#10;Second line</text>\n" +
		    	    "   <value>&#x32;</value>\n" +
		    	    "   <compute><![CDATA[value>\"0\" && value<\"10\" ?\"valid\":\"error\"]]></compute>\n" +
		    	    "   <compute expr=\'value>\"0\" &amp;&amp; value&lt;\"10\" ?\"valid\":\"error\"\'>valid</compute>\n" +
		    	    "   <norm attr=\' &apos;   &#x20;&#13;&#xa;&#9;   &apos; \'/>\n" +
		    	    // "   <normId id=\' &apos;   &#x20;&#13;&#xa;&#9;   &apos; \'/>\n" +
		    	    "</doc>\n";
	        static string C14NSpecExample4Output =   
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
	        // Example 5 from C14N spec - Entity References: 
	        // http://www.w3.org/TR/xml-c14n#Example-Entities
	        //
	        static string C14NSpecExample5Input =  
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
	        static string C14NSpecExample5Output =  
		    	    "<doc attrExtEnt=\"entExt\">\n" +
		    	    "   Hello, world!\n" +
		    	    "</doc>\n" + 
		    	    "<!-- Let world.txt contain \"world\" (excluding the quotes) -->";
		    	
	        //
	        // Example 6 from C14N spec - UTF-8 Encoding: 
	        // http://www.w3.org/TR/xml-c14n#Example-UTF8
	        // 
	        static string C14NSpecExample6Input =  
		    	    "<?xml version=\"1.0\" encoding=\"ISO-8859-1\"?>\n" +
		    	    "<doc>&#169;</doc>\n";
	        static string C14NSpecExample6Output =  
		    	    "<doc>\xC2\xA9</doc>";
	    

	        //
	        // Example 7 from C14N spec - Document Subsets: 
	        // http://www.w3.org/TR/xml-c14n#Example-DocSubsets
	        // 
		// Aleksey:
		// Well, XPath support in Mono is far from complete....
		// I was not able to simplify the xpath expression from this test
		// so it runs on Mono and still makes sense for testing this feature.
		// Thus this test is not included in the suite now.
	        static string C14NSpecExample7Input =  
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

	        static string C14NSpecExample7Xpath =  
    		    	    "(//.|//@*|//namespace::*)\n" +
		    	    "[\n" +
		    	    "self::ietf:e1\n" +
		    	    "    or\n" +
		    	    "(parent::ietf:e1 and not(self::text() or self::e2))\n" +
		    	    "    or\n" +
		    	    "count(id(\"E3\")|ancestor-or-self::node()) = count(ancestor-or-self::node())\n" +
		    	    "]";
	        static string C14NSpecExample7Output =  
		    	    "<e1 xmlns=\"http://www.ietf.org\" xmlns:w3c=\"http://www.w3.org\"><e3 xmlns=\"\" id=\"E3\" xml:space=\"preserve\"></e3></e1>";   
	}
}
