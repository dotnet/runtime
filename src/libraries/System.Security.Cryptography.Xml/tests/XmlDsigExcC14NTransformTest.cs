// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// XmlDsigExcC14NTransformTest.cs - Test Cases for XmlDsigExcC14NTransform
//
// Author:
//  original:
//  Sebastien Pouliot <sebastien@ximian.com>
//  Aleksey Sanin (aleksey@aleksey.com)
//  this file:
//  Atsushi Enomoto <atsushi@ximian.com>
//
// (C) 2002, 2003 Motus Technologies Inc. (http://www.motus.com)
// (C) 2003 Aleksey Sanin (aleksey@aleksey.com)
// (C) 2004 Novell (http://www.novell.com)


using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Resolvers;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;

namespace System.Security.Cryptography.Xml.Tests
{

    // Note: GetInnerXml is protected in XmlDsigExcC14NTransform making it
    // difficult to test properly. This class "open it up" :-)
    public class UnprotectedXmlDsigExcC14NTransform : XmlDsigExcC14NTransform
    {
        public UnprotectedXmlDsigExcC14NTransform()
        {
        }

        public UnprotectedXmlDsigExcC14NTransform(bool includeComments)
            : base(includeComments)
        {
        }

        public UnprotectedXmlDsigExcC14NTransform(string inclusiveNamespacesPrefixList)
            : base(inclusiveNamespacesPrefixList)
        {
        }

        public UnprotectedXmlDsigExcC14NTransform(bool includeComments, string inclusiveNamespacesPrefixList)
            : base(includeComments, inclusiveNamespacesPrefixList)
        {
        }

        public XmlNodeList UnprotectedGetInnerXml()
        {
            return base.GetInnerXml();
        }
    }

    public class XmlDsigExcC14NTransformTest
    {

        protected UnprotectedXmlDsigExcC14NTransform transform;

        public XmlDsigExcC14NTransformTest()
        {
            transform = new UnprotectedXmlDsigExcC14NTransform();
        }

        [Fact] // ctor ()
        public void Constructor1()
        {
            Assert.Equal("http://www.w3.org/2001/10/xml-exc-c14n#", transform.Algorithm);
            Assert.Null(transform.InclusiveNamespacesPrefixList);
            CheckProperties(transform);
        }

        [Fact] // ctor (Boolean)
        public void Constructor2()
        {
            transform = new UnprotectedXmlDsigExcC14NTransform(true);
            Assert.Equal("http://www.w3.org/2001/10/xml-exc-c14n#WithComments", transform.Algorithm);
            Assert.Null(transform.InclusiveNamespacesPrefixList);
            CheckProperties(transform);

            transform = new UnprotectedXmlDsigExcC14NTransform(false);
            Assert.Equal("http://www.w3.org/2001/10/xml-exc-c14n#", transform.Algorithm);
            Assert.Null(transform.InclusiveNamespacesPrefixList);
            CheckProperties(transform);
        }

        [Fact] // ctor (String)
        public void Constructor3()
        {
            transform = new UnprotectedXmlDsigExcC14NTransform(null);
            Assert.Equal("http://www.w3.org/2001/10/xml-exc-c14n#", transform.Algorithm);
            Assert.Null(transform.InclusiveNamespacesPrefixList);
            CheckProperties(transform);

            transform = new UnprotectedXmlDsigExcC14NTransform(string.Empty);
            Assert.Equal("http://www.w3.org/2001/10/xml-exc-c14n#", transform.Algorithm);
            Assert.Equal(string.Empty, transform.InclusiveNamespacesPrefixList);
            CheckProperties(transform);

            transform = new UnprotectedXmlDsigExcC14NTransform("#default xsd");
            Assert.Equal("http://www.w3.org/2001/10/xml-exc-c14n#", transform.Algorithm);
            Assert.Equal("#default xsd", transform.InclusiveNamespacesPrefixList);
            CheckProperties(transform);
        }

        [Fact] // ctor (Boolean, String)
        public void Constructor4()
        {
            transform = new UnprotectedXmlDsigExcC14NTransform(true, null);
            Assert.Equal("http://www.w3.org/2001/10/xml-exc-c14n#WithComments", transform.Algorithm);
            Assert.Null(transform.InclusiveNamespacesPrefixList);
            CheckProperties(transform);

            transform = new UnprotectedXmlDsigExcC14NTransform(true, string.Empty);
            Assert.Equal("http://www.w3.org/2001/10/xml-exc-c14n#WithComments", transform.Algorithm);
            Assert.Equal(string.Empty, transform.InclusiveNamespacesPrefixList);
            CheckProperties(transform);

            transform = new UnprotectedXmlDsigExcC14NTransform(true, "#default xsd");
            Assert.Equal("http://www.w3.org/2001/10/xml-exc-c14n#WithComments", transform.Algorithm);
            Assert.Equal("#default xsd", transform.InclusiveNamespacesPrefixList);
            CheckProperties(transform);

            transform = new UnprotectedXmlDsigExcC14NTransform(false, null);
            Assert.Equal("http://www.w3.org/2001/10/xml-exc-c14n#", transform.Algorithm);
            Assert.Null(transform.InclusiveNamespacesPrefixList);
            CheckProperties(transform);

            transform = new UnprotectedXmlDsigExcC14NTransform(false, string.Empty);
            Assert.Equal("http://www.w3.org/2001/10/xml-exc-c14n#", transform.Algorithm);
            Assert.Equal(string.Empty, transform.InclusiveNamespacesPrefixList);
            CheckProperties(transform);

            transform = new UnprotectedXmlDsigExcC14NTransform(false, "#default xsd");
            Assert.Equal("http://www.w3.org/2001/10/xml-exc-c14n#", transform.Algorithm);
            Assert.Equal("#default xsd", transform.InclusiveNamespacesPrefixList);
            CheckProperties(transform);
        }

        void CheckProperties(XmlDsigExcC14NTransform transform)
        {
            Type[] input = transform.InputTypes;
            Assert.True((input.Length == 3), "Input #");
            // check presence of every supported input types
            bool istream = false;
            bool ixmldoc = false;
            bool ixmlnl = false;
            foreach (Type t in input)
            {
                if (t.ToString() == "System.IO.Stream")
                    istream = true;
                if (t.ToString() == "System.Xml.XmlDocument")
                    ixmldoc = true;
                if (t.ToString() == "System.Xml.XmlNodeList")
                    ixmlnl = true;
            }
            Assert.True(istream, "Input Stream");
            Assert.True(ixmldoc, "Input XmlDocument");
            Assert.True(ixmlnl, "Input XmlNodeList");

            Type[] output = transform.OutputTypes;
            Assert.True((output.Length == 1), "Output #");
            // check presence of every supported output types
            bool ostream = false;
            foreach (Type t in output)
            {
                if (t.ToString() == "System.IO.Stream")
                    ostream = true;
            }
            Assert.True(ostream, "Output Stream");
        }

        [Fact]
        public void Types()
        {
            Type[] input = transform.InputTypes;
            input[0] = null;
            input[1] = null;
            input[2] = null;
            // property does not return a clone
            foreach (Type t in transform.InputTypes)
            {
                Assert.Null(t);
            }
            // it's not a static array
            XmlDsigExcC14NTransform t2 = new XmlDsigExcC14NTransform();
            foreach (Type t in t2.InputTypes)
            {
                Assert.NotNull(t);
            }
        }

        [Fact]
        public void GetInnerXml()
        {
            XmlNodeList xnl = transform.UnprotectedGetInnerXml();
            Assert.Null(xnl);
        }

        private string Stream2String(Stream s)
        {
            StringBuilder sb = new StringBuilder();
            int b = s.ReadByte();
            while (b != -1)
            {
                sb.Append(Convert.ToChar(b));
                b = s.ReadByte();
            }
            return sb.ToString();
        }

        static string xml = "<Test  attrib='at ' xmlns=\"http://www.go-mono.com/\" > \r\n &#xD; <Toto/> text &amp; </Test   >";
        // GOOD for Stream input
        static string c14xml2 = "<Test xmlns=\"http://www.go-mono.com/\" attrib=\"at \"> \n &#xD; <Toto></Toto> text &amp; </Test>";
        // GOOD for XmlDocument input. The difference is because once
        // xml string is loaded to XmlDocument, there is no difference
        // between \r and &#xD;, so every \r must be handled as &#xD;.
        static string c14xml3 = "<Test xmlns=\"http://www.go-mono.com/\" attrib=\"at \"> &#xD;\n &#xD; <Toto></Toto> text &amp; </Test>";

        private XmlDocument GetDoc()
        {
            XmlDocument doc = new XmlDocument();
            doc.PreserveWhitespace = true;
            doc.LoadXml(xml);
            return doc;
        }

        [Fact]
        public void LoadInputAsXmlDocument()
        {
            XmlDocument doc = GetDoc();
            transform.LoadInput(doc);
            Stream s = (Stream)transform.GetOutput();
            string output = Stream2String(s);
            Assert.Equal(c14xml3, output);
        }

        [ActiveIssue("https://github.com/dotnet/runtime/issues/20429")]
        [Fact]
        // see LoadInputAsXmlNodeList2 description
        public void LoadInputAsXmlNodeList()
        {
            XmlDocument doc = GetDoc();
            // Argument list just contains element Test.
            transform.LoadInput(doc.ChildNodes);
            Stream s = (Stream)transform.GetOutput();
            string output = Stream2String(s);
            Assert.Equal("<Test></Test>", output);
        }

        [ActiveIssue("https://github.com/dotnet/runtime/issues/20429")]
        [Fact]
        // MS has a bug that those namespace declaration nodes in
        // the node-set are written to output. Related spec section is:
        // http://www.w3.org/TR/2001/REC-xml-c14n-20010315#ProcessingModel
        public void LoadInputAsXmlNodeList2()
        {
            XmlDocument doc = GetDoc();
            transform.LoadInput(doc.SelectNodes("//*"));
            Stream s = (Stream)transform.GetOutput();
            string output = Stream2String(s);
            string expected = @"<Test><Toto></Toto></Test>";
            Assert.Equal(expected, output);
        }

        [Fact]
        public void LoadInputAsStream()
        {
            MemoryStream ms = new MemoryStream();
            byte[] x = Encoding.ASCII.GetBytes(xml);
            ms.Write(x, 0, x.Length);
            ms.Position = 0;
            transform.LoadInput(ms);
            Stream s = (Stream)transform.GetOutput();
            string output = Stream2String(s);
            Assert.Equal(c14xml2, output);
        }

        [Fact]
        public void LoadInputWithUnsupportedType()
        {
            byte[] bad = { 0xBA, 0xD };
            AssertExtensions.Throws<ArgumentException>("obj", () => transform.LoadInput(bad));
        }

        [Fact]
        public void UnsupportedOutput()
        {
            XmlDocument doc = new XmlDocument();
            AssertExtensions.Throws<ArgumentException>("type", () => transform.GetOutput(doc.GetType()));
        }

        [Fact]
        public void ExcC14NSpecExample1()
        {
            XmlPreloadedResolver resolver = new XmlPreloadedResolver();
            resolver.Add(TestHelpers.ToUri("doc.dtd"), "");
            string res = ExecuteXmlDSigExcC14NTransform(ExcC14NSpecExample1Input);
            Assert.Equal(ExcC14NSpecExample1Output, res);
        }

        [Fact]
        public void ExcC14NSpecExample2()
        {
            string res = ExecuteXmlDSigExcC14NTransform(ExcC14NSpecExample2Input);
            Assert.Equal(ExcC14NSpecExample2Output, res);
        }

        [Fact]
        public void ExcC14NSpecExample3()
        {
            string res = ExecuteXmlDSigExcC14NTransform(ExcC14NSpecExample3Input);
            Assert.Equal(ExcC14NSpecExample3Output, res);
        }

        [Fact]
        //        [Ignore ("This test should be fine, but it does not pass under MS.NET")]
        public void ExcC14NSpecExample4()
        {
            string res = ExecuteXmlDSigExcC14NTransform(ExcC14NSpecExample4Input);
            Assert.Equal(ExcC14NSpecExample4Output, res);
        }

        [Fact]
        public void ExcC14NSpecExample5()
        {
            XmlPreloadedResolver resolver = new XmlPreloadedResolver();
            resolver.Add(TestHelpers.ToUri("doc.txt"), "world");
            string input = ExcC14NSpecExample5Input;
            string res = ExecuteXmlDSigExcC14NTransform(input, resolver);
            Assert.Equal(ExcC14NSpecExample5Output, res);
        }

        [Fact]
        public void ExcC14NSpecExample6()
        {
            string res = ExecuteXmlDSigExcC14NTransform(ExcC14NSpecExample6Input);
            Assert.Equal(ExcC14NSpecExample6Output, res);
        }

#if NET
        [Fact]
        public void ExcC14N_AttributeLimit_DefaultBoundary_RendersUpToLimitThenThrows()
        {
            TestHelpers.ExecuteTransform(BuildUtilizedPrefixes(512), new XmlDsigExcC14NTransform());

            Assert.Throws<CryptographicException>(
                () => TestHelpers.ExecuteTransform(BuildUtilizedPrefixes(513), new XmlDsigExcC14NTransform()));

            static string BuildUtilizedPrefixes(int count)
            {
                StringBuilder element = new StringBuilder("<doc");
                for (int i = 0; i < count; i++)
                {
                    element.Append(FormattableString.Invariant($" xmlns:p{i:D4}=\"urn:{i}\""));
                }
                for (int i = 0; i < count; i++)
                {
                    element.Append(FormattableString.Invariant($" p{i:D4}:a=\"{i}\""));
                }

                return element.Append("></doc>").ToString();
            }
        }

        [Fact]
        public void ExcC14N_NamespaceLimit_InclusivePrefixList_RendersUpToLimitThenThrows()
        {
            Execute(512);

            Assert.Throws<CryptographicException>(() => Execute(513));

            static void Execute(int count)
            {
                StringBuilder element = new StringBuilder("<doc");
                StringBuilder prefixes = new StringBuilder();
                for (int i = 0; i < count; i++)
                {
                    element.Append(FormattableString.Invariant($" xmlns:p{i:D4}=\"urn:{i}\""));

                    if (prefixes.Length > 0)
                    {
                        prefixes.Append(' ');
                    }

                    prefixes.Append(FormattableString.Invariant($"p{i:D4}"));
                }

                element.Append("></doc>");

                TestHelpers.ExecuteTransform(element.ToString(), new XmlDsigExcC14NTransform(prefixes.ToString()));
            }
        }
#endif

        private string ExecuteXmlDSigExcC14NTransform(string InputXml, XmlResolver resolver = null)
        {
            XmlDocument doc = new XmlDocument();
            doc.XmlResolver = resolver;
            doc.PreserveWhitespace = true;
            doc.LoadXml(InputXml);

            // Testing default attribute support with
            // vreader.ValidationType = ValidationType.None.
            //
            UTF8Encoding utf8 = new UTF8Encoding();
            byte[] data = utf8.GetBytes(InputXml.ToString());
            Stream stream = new MemoryStream(data);
            var settings = new XmlReaderSettings
            {
                ValidationType = ValidationType.None,
                DtdProcessing = DtdProcessing.Parse,
                XmlResolver = resolver
            };
            using (XmlReader reader = XmlReader.Create(stream, settings))
            {
                doc.Load(reader);
                transform.LoadInput(doc);
                return Stream2String((Stream) transform.GetOutput());
            }
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
                "<!DOCTYPE doc SYSTEM \"doc.dtd\">\n" +
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
                //                    "               <e9 xmlns:a=\"http://www.ietf.org\"></e9>\n" +
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
        static string ExcC14NSpecExample5Input =>
                "<!DOCTYPE doc [\n" +
                "<!ATTLIST doc attrExtEnt ENTITY #IMPLIED>\n" +
                "<!ENTITY ent1 \"Hello\">\n" +
                $"<!ENTITY ent2 SYSTEM \"doc.txt\">\n" +
                "<!ENTITY entExt SYSTEM \"earth.gif\" NDATA gif>\n" +
                "<!NOTATION gif SYSTEM \"viewgif.exe\">\n" +
                "]>\n" +
                "<doc attrExtEnt=\"entExt\">\n" +
                "   &ent1;, &ent2;!\n" +
                "</doc>\n" +
                "\n" +
                $"<!-- Let doc.txt contain \"world\" (excluding the quotes) -->\n";
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

        [Fact]
        public void SimpleNamespacePrefixes()
        {
            string input = "<a:Action xmlns:a='urn:foo'>http://tempuri.org/IFoo/Echo</a:Action>";
            string expected = @"<a:Action xmlns:a=""urn:foo"">http://tempuri.org/IFoo/Echo</a:Action>";

            XmlDocument doc = new XmlDocument();
            doc.LoadXml(input);
            XmlDsigExcC14NTransform t = new XmlDsigExcC14NTransform();
            t.LoadInput(doc);
            Stream s = t.GetOutput() as Stream;
            Assert.Equal(new StreamReader(s, Encoding.UTF8).ReadToEnd(), expected);
        }

        [Fact]
        public void GetDigestedOutput_Null()
        {
            Assert.Throws<NullReferenceException>(() => new XmlDsigExcC14NTransform().GetDigestedOutput(null));
        }

#if NET
        [Theory]
        [InlineData(64, false)]   // at the default limit - should pass
        [InlineData(65, true)]    // one over the default limit - should fail
        [InlineData(1000, true)]  // way over - should fail
        public void GetOutput_DepthLimit(int depth, bool shouldThrow)
        {
            XmlDocument doc = CreateDeepXmlDocument(depth);
            XmlDsigExcC14NTransform transform = new XmlDsigExcC14NTransform();
            transform.LoadInput(doc);

            if (shouldThrow)
            {
                CryptographicException ex = Assert.Throws<CryptographicException>(() => transform.GetOutput());
                Assert.Equal("The XML element has exceeded the maximum nesting depth allowed for decryption.", ex.Message);
            }
            else
            {
                Stream s = (Stream)transform.GetOutput();
                Assert.NotNull(s);
            }
        }

        [Theory]
        [InlineData(64, false)]   // at the default limit - should pass
        [InlineData(65, true)]    // one over the default limit - should fail
        [InlineData(1000, true)]  // way over - should fail
        public void GetDigestedOutput_DepthLimit(int depth, bool shouldThrow)
        {
            XmlDocument doc = CreateDeepXmlDocument(depth);
            XmlDsigExcC14NTransform transform = new XmlDsigExcC14NTransform();
            transform.LoadInput(doc);
            using SHA256 hash = SHA256.Create();

            if (shouldThrow)
            {
                CryptographicException ex = Assert.Throws<CryptographicException>(() => transform.GetDigestedOutput(hash));
                Assert.Equal("The XML element has exceeded the maximum nesting depth allowed for decryption.", ex.Message);
            }
            else
            {
                byte[] result = transform.GetDigestedOutput(hash);
                Assert.NotNull(result);
            }
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void GetOutput_CustomDepthLimit_BoundaryAccuracy()
        {
            // Verify the limit is exact: depth 100 passes with limit 100, depth 101 fails.
            RemoteExecutor.Invoke(static () =>
            {
                AppContext.SetData("System.Security.Cryptography.Xml.DangerousMaxRecursionDepth", 100);

                XmlDocument passDoc = CreateDeepXmlDocument(100);
                XmlDsigExcC14NTransform passTransform = new XmlDsigExcC14NTransform();
                passTransform.LoadInput(passDoc);
                Stream s = (Stream)passTransform.GetOutput(); // Should not throw
                Assert.NotNull(s);

                XmlDocument failDoc = CreateDeepXmlDocument(101);
                XmlDsigExcC14NTransform failTransform = new XmlDsigExcC14NTransform();
                failTransform.LoadInput(failDoc);
                CryptographicException ex = Assert.Throws<CryptographicException>(() => failTransform.GetOutput());
                Assert.Equal("The XML element has exceeded the maximum nesting depth allowed for decryption.", ex.Message);
            }).Dispose();
        }

        private static XmlDocument CreateDeepXmlDocument(int depth)
        {
            MemoryStream ms = new();
            using (XmlWriter xw = XmlWriter.Create(ms))
            {
                xw.WriteStartDocument();
                xw.WriteStartElement("Root");
                for (int i = 0; i < depth; i++)
                {
                    xw.WriteStartElement("a");
                }
                for (int i = 0; i < depth; i++)
                {
                    xw.WriteEndElement();
                }
                xw.WriteEndElement();
                xw.WriteEndDocument();
            }
            ms.Position = 0;
            XmlDocument doc = new XmlDocument();
            doc.PreserveWhitespace = true;
            doc.Load(ms);
            return doc;
        }

        private const int DeepInputDepth = 100000;

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public static void DeepStreamInput_LoadInput_Throws()
        {
            RemoteExecutor.Invoke(static () =>
            {
                byte[] payload = Encoding.UTF8.GetBytes(BuildDeepXml(DeepInputDepth));
                Assert.Throws<CryptographicException>(() =>
                {
                    using MemoryStream ms = new MemoryStream(payload);
                    XmlDsigExcC14NTransform transform = new XmlDsigExcC14NTransform();
                    transform.LoadInput(ms);
                    transform.GetOutput();
                });
            }).Dispose();
        }

        private static string BuildDeepXml(int depth)
        {
            StringBuilder sb = new StringBuilder(depth * 7 + 1);
            for (int i = 0; i < depth; i++)
                sb.Append("<a>");
            sb.Append('x');
            for (int i = 0; i < depth; i++)
                sb.Append("</a>");
            return sb.ToString();
        }

        private const string DtdPayload = "<!DOCTYPE r []><r>x</r>";
        private const string CryptoXmlAllowDtdProcessingAppContextSwitch = "System.Security.Cryptography.Xml.CryptoXmlAllowDtdProcessing";

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public static void DtdStreamInput_LoadInput_Throws()
        {
            RemoteExecutor.Invoke(static () =>
            {
                Assert.Throws<XmlException>(() =>
                {
                    using MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(DtdPayload));
                    XmlDsigExcC14NTransform transform = new XmlDsigExcC14NTransform();
                    transform.LoadInput(ms);
                    transform.GetOutput();
                });
            }).Dispose();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public static void DtdStreamInput_LoadInput_WithOptIn_Succeeds()
        {
            RemoteExecutor.Invoke(static () =>
            {
                AppContext.SetSwitch(CryptoXmlAllowDtdProcessingAppContextSwitch, true);
                using MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(DtdPayload));
                XmlDsigExcC14NTransform transform = new XmlDsigExcC14NTransform();
                transform.LoadInput(ms);
                Assert.NotNull(transform.GetOutput());
            }).Dispose();
        }
#endif

        [Fact]
        public static void ExclusiveC14n_DoesNotCoverNamespacePrefixUsedOnlyInAttributeValue()
        {
            using (RSA rsa = RSA.Create())
            {
                string one = SignAndDigest(rsa, QNameAttributeDocument("urn:one"), new XmlDsigExcC14NTransform(), out XmlDocument signed);
                string two = SignAndDigest(rsa, QNameAttributeDocument("urn:two"), new XmlDsigExcC14NTransform(), out _);
                string inclusiveOne = SignAndDigest(rsa, QNameAttributeDocument("urn:one"), new XmlDsigC14NTransform(), out _);
                string inclusiveTwo = SignAndDigest(rsa, QNameAttributeDocument("urn:two"), new XmlDsigC14NTransform(), out _);

                XmlDocument rebound = LoadPreservingWhitespace(signed.OuterXml.Replace("urn:one", "urn:two"));

                Assert.Equal(one, two);
                Assert.NotEqual(inclusiveOne, inclusiveTwo);
                Assert.True(VerifyReboundSignature(rebound, rsa));
                Assert.Equal("{urn:two}T", ResolveQName((XmlElement)rebound.GetElementsByTagName("data")[0], "type", "http://www.w3.org/2001/XMLSchema-instance"));
            }
        }

        private static string QNameAttributeDocument(string uri) =>
            "<root xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\">" +
            "<data Id=\"d1\" xmlns:a=\"" + uri + "\" xsi:type=\"a:T\">value</data></root>";

        private static string SignAndDigest(RSA rsa, string xml, Transform transform, out XmlDocument doc)
        {
            doc = LoadPreservingWhitespace(xml);
            SignedXml signedXml = new SignedXml(doc) { SigningKey = rsa };
            signedXml.SignedInfo.SignatureMethod = SignedXml.XmlDsigRSASHA256Url;
            Reference reference = new Reference("#d1") { DigestMethod = SignedXml.XmlDsigSHA256Url };
            reference.AddTransform(transform);
            signedXml.AddReference(reference);
            signedXml.ComputeSignature();
            doc.DocumentElement.AppendChild(doc.ImportNode(signedXml.GetXml(), true));
            return doc.GetElementsByTagName("DigestValue", SignedXml.XmlDsigNamespaceUrl)[0].InnerText;
        }

        private static bool VerifyReboundSignature(XmlDocument doc, RSA rsa)
        {
            SignedXml signedXml = new SignedXml(doc);
            signedXml.LoadXml((XmlElement)doc.GetElementsByTagName("Signature", SignedXml.XmlDsigNamespaceUrl)[0]);
            return signedXml.CheckSignature(rsa);
        }

        private static string ResolveQName(XmlElement element, string localName, string namespaceUri)
        {
            string value = element.GetAttribute(localName, namespaceUri);
            int colon = value.IndexOf(':');
            string prefix = value.Substring(0, colon);
            return "{" + element.GetNamespaceOfPrefix(prefix) + "}" + value.Substring(colon + 1);
        }

        private static XmlDocument LoadPreservingWhitespace(string xml)
        {
            XmlDocument doc = new XmlDocument { PreserveWhitespace = true };
            doc.LoadXml(xml);
            return doc;
        }
    }
}
