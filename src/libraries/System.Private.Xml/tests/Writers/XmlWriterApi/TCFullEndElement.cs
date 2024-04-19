// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using OLEDB.Test.ModuleCore;
using XmlCoreTest.Common;
using Xunit;

namespace System.Xml.XmlWriterApiTests
{
    //[TestCase(Name = "WriteFullEndElement")]
    public class TCFullEndElement
    {
        // Sanity test for WriteFullEndElement()
        [Theory]
        [XmlWriterInlineData]
        public void fullEndElement_1(XmlWriterUtils utils)
        {
            using (XmlWriter w = utils.CreateWriter())
            {
                w.WriteStartElement("Root");
                w.WriteFullEndElement();
            }
            Assert.True(utils.CompareReader("<Root></Root>"));
        }

        // Call WriteFullEndElement before calling WriteStartElement
        [Theory]
        [XmlWriterInlineData]
        public void fullEndElement_2(XmlWriterUtils utils)
        {
            using (XmlWriter w = utils.CreateWriter())
            {
                try
                {
                    w.WriteFullEndElement();
                }
                catch (InvalidOperationException e)
                {
                    CError.WriteLineIgnore("Exception: " + e.ToString());
                    CError.Compare(w.WriteState, WriteState.Error, "WriteState should be Error");
                    return;
                }
            }
            CError.WriteLine("Did not throw exception");
            Assert.Fail();
        }

        // Call WriteFullEndElement after WriteEndElement
        [Theory]
        [XmlWriterInlineData]
        public void fullEndElement_3(XmlWriterUtils utils)
        {
            using (XmlWriter w = utils.CreateWriter())
            {
                try
                {
                    w.WriteStartElement("Root");
                    w.WriteEndElement();
                    w.WriteFullEndElement();
                }
                catch (InvalidOperationException e)
                {
                    CError.WriteLineIgnore("Exception: " + e.ToString());
                    CError.Compare(w.WriteState, WriteState.Error, "WriteState should be Error");
                    return;
                }
            }
            CError.WriteLine("Did not throw exception");
            Assert.Fail();
        }

        // Call WriteFullEndElement without closing attributes
        [Theory]
        [XmlWriterInlineData]
        public void fullEndElement_4(XmlWriterUtils utils)
        {
            using (XmlWriter w = utils.CreateWriter())
            {
                w.WriteStartElement("Root");
                w.WriteStartAttribute("a");
                w.WriteString("b");
                w.WriteFullEndElement();
            }
            Assert.True(utils.CompareReader("<Root a=\"b\"></Root>"));
        }

        // Call WriteFullEndElement after WriteStartAttribute
        [Theory]
        [XmlWriterInlineData]
        public void fullEndElement_5(XmlWriterUtils utils)
        {
            using (XmlWriter w = utils.CreateWriter())
            {
                w.WriteStartElement("Root");
                w.WriteStartAttribute("a");
                w.WriteFullEndElement();
            }
            Assert.True(utils.CompareReader("<Root a=\"\"></Root>"));
        }

        // WriteFullEndElement for 100 nested elements
        [Theory]
        [XmlWriterInlineData]
        public void fullEndElement_6(XmlWriterUtils utils)
        {
            using (XmlWriter w = utils.CreateWriter())
            {
                for (int i = 0; i < 100; i++)
                {
                    string eName = "Node" + i.ToString();
                    w.WriteStartElement(eName);
                }
                for (int i = 0; i < 100; i++)
                    w.WriteFullEndElement();

                w.Dispose();
                Assert.True(utils.CompareBaseline("100FullEndElements.txt"));
            }
        }

        //[TestCase(Name = "Element Namespace")]
        public partial class TCElemNamespace
        {
            // Multiple NS decl for same prefix on an element
            [Theory]
            [XmlWriterInlineData]
            public void elemNamespace_1(XmlWriterUtils utils)
            {
                using (XmlWriter w = utils.CreateWriter())
                {
                    try
                    {
                        w.WriteStartElement("Root");
                        w.WriteAttributeString("xmlns", "x", null, "foo");
                        w.WriteAttributeString("xmlns", "x", null, "bar");
                    }
                    catch (XmlException e)
                    {
                        CError.WriteLineIgnore("Exception: " + e.ToString());
                        return;
                    }
                }
                CError.WriteLine("Did not throw exception");
                Assert.Fail();
            }

            // Multiple NS decl for same prefix (same NS value) on an element
            [Theory]
            [XmlWriterInlineData]
            public void elemNamespace_2(XmlWriterUtils utils)
            {
                using (XmlWriter w = utils.CreateWriter())
                {
                    try
                    {
                        w.WriteStartElement("Root");
                        w.WriteAttributeString("xmlns", "x", null, "foo");
                        w.WriteAttributeString("xmlns", "x", null, "foo");
                        w.WriteEndElement();
                    }
                    catch (XmlException e)
                    {
                        CError.WriteLineIgnore("Exception: " + e.ToString());
                        CError.Compare(w.WriteState, WriteState.Error, "WriteState should be Error");
                        return;
                    }
                }
                CError.WriteLine("Did not throw exception");
                Assert.Fail();
            }

            // Element and attribute have same prefix, but different namespace value
            [Theory]
            [XmlWriterInlineData]
            public void elemNamespace_3(XmlWriterUtils utils)
            {
                using (XmlWriter w = utils.CreateWriter())
                {
                    w.WriteStartElement("x", "Root", "foo");
                    w.WriteAttributeString("x", "a", "bar", "b");
                    w.WriteEndElement();
                }
                Assert.True(utils.CompareString("<~f x a~:Root ~a p1 a~:a=\"b\" xmlns:~a p1 A~=\"bar\" xmlns:~f x A~=\"foo\" />"));
            }

            // Nested elements have same prefix, but different namespace
            [Theory]
            [XmlWriterInlineData]
            public void elemNamespace_4(XmlWriterUtils utils)
            {
                using (XmlWriter w = utils.CreateWriter())
                {
                    w.WriteStartElement("x", "Root", "foo");
                    w.WriteStartElement("x", "level1", "bar");
                    w.WriteStartElement("x", "level2", "blah");
                    w.WriteEndElement();
                    w.WriteEndElement();
                    w.WriteEndElement();
                }
                Assert.True(utils.CompareReader("<x:Root xmlns:x=\"foo\"><x:level1 xmlns:x=\"bar\"><x:level2 xmlns:x=\"blah\" /></x:level1></x:Root>"));
            }

            // Mapping reserved prefix xml to invalid namespace
            [Theory]
            [XmlWriterInlineData]
            public void elemNamespace_5(XmlWriterUtils utils)
            {
                using (XmlWriter w = utils.CreateWriter())
                {
                    try
                    {
                        w.WriteStartElement("xml", "Root", "blah");
                    }
                    catch (ArgumentException e)
                    {
                        CError.WriteLineIgnore("Exception: " + e.ToString());
                        CError.Compare(w.WriteState, WriteState.Error, "WriteState should be Error");
                        return;
                    }
                }
                CError.WriteLine("Did not throw exception");
                Assert.Fail();
            }

            // Mapping reserved prefix xml to correct namespace
            [Theory]
            [XmlWriterInlineData]
            public void elemNamespace_6(XmlWriterUtils utils)
            {
                using (XmlWriter w = utils.CreateWriter())
                {
                    w.WriteStartElement("xml", "Root", "http://www.w3.org/XML/1998/namespace");
                    w.WriteEndElement();
                }

                Assert.True(utils.CompareReader("<xml:Root />"));
            }

            // Write element with prefix beginning with xml
            [Theory]
            [XmlWriterInlineData]
            public void elemNamespace_7(XmlWriterUtils utils)
            {
                using (XmlWriter w = utils.CreateWriter())
                {
                    w.WriteStartElement("Root");
                    w.WriteStartElement("xmlA", "elem1", "test");
                    w.WriteEndElement();
                    w.WriteStartElement("xMlB", "elem2", "test");
                    w.WriteEndElement();
                    w.WriteEndElement();
                }
                Assert.True(utils.CompareReader("<Root><xmlA:elem1 xmlns:xmlA=\"test\" /><xMlB:elem2 xmlns:xMlB=\"test\" /></Root>"));
            }

            // Reuse prefix that refers the same as default namespace
            [Theory]
            [XmlWriterInlineData]
            public void elemNamespace_8(XmlWriterUtils utils)
            {
                using (XmlWriter w = utils.CreateWriter())
                {
                    w.WriteStartElement("x", "foo", "uri-1");
                    w.WriteStartElement("", "bar", "uri-1");
                    w.WriteStartElement("x", "bop", "uri-1");
                    w.WriteEndElement();
                    w.WriteEndElement();
                    w.WriteEndElement();
                }
                Assert.True(utils.CompareReader("<x:foo xmlns:x=\"uri-1\"><bar xmlns=\"uri-1\"><x:bop /></bar></x:foo>"));
            }

            // Should throw error for prefix=xmlns
            [Theory]
            [XmlWriterInlineData]
            public void elemNamespace_9(XmlWriterUtils utils)
            {
                using (XmlWriter w = utils.CreateWriter())
                {
                    try
                    {
                        w.WriteStartElement("xmlns", "localname", "uri:bogus");
                        w.WriteEndElement();
                    }
                    catch (Exception e)
                    {
                        CError.WriteLineIgnore("Exception: " + e.ToString());
                        CError.Compare(w.WriteState, WriteState.Error, "WriteState should be Error");
                        return;
                    }
                }
                CError.WriteLine("Did not throw error");
                Assert.Fail();
            }

            // Create nested element without prefix but with namespace of parent element with a defined prefix
            [Theory]
            [XmlWriterInlineData]
            public void elemNamespace_10(XmlWriterUtils utils)
            {
                using (XmlWriter w = utils.CreateWriter())
                {
                    w.WriteStartElement("Root");
                    w.WriteAttributeString("xmlns", "x", null, "fo");
                    w.WriteStartElement("level1", "fo");
                    w.WriteEndElement();
                    w.WriteEndElement();
                }
                Assert.True(utils.CompareReader("<Root xmlns:x=\"fo\"><x:level1 /></Root>"));
            }

            // Create different prefix for element and attribute that have same namespace
            [Theory]
            [XmlWriterInlineData]
            public void elemNamespace_11(XmlWriterUtils utils)
            {
                using (XmlWriter w = utils.CreateWriter())
                {
                    w.WriteStartElement("x", "Root", "foo");
                    w.WriteAttributeString("y", "attr", "foo", "b");
                    w.WriteEndElement();
                }
                Assert.True(utils.CompareReader("<x:Root y:attr=\"b\" xmlns:y=\"foo\" xmlns:x=\"foo\" />"));
            }

            // Create same prefix for element and attribute that have same namespace
            [Theory]
            [XmlWriterInlineData]
            public void elemNamespace_12(XmlWriterUtils utils)
            {
                using (XmlWriter w = utils.CreateWriter())
                {
                    w.WriteStartElement("x", "Root", "foo");
                    w.WriteAttributeString("x", "attr", "foo", "b");
                    w.WriteEndElement();
                }
                Assert.True(utils.CompareReader("<x:Root x:attr=\"b\" xmlns:x=\"foo\" />"));
            }

            // Try to re-define NS prefix on attribute which is already defined on an element
            [Theory]
            [XmlWriterInlineData]
            public void elemNamespace_13(XmlWriterUtils utils)
            {
                using (XmlWriter w = utils.CreateWriter())
                {
                    w.WriteStartElement("x", "Root", "foo");
                    w.WriteAttributeString("x", "attr", "bar", "test");
                    w.WriteEndElement();
                }
                Assert.True(utils.CompareString("<~f x a~:Root ~a p1 a~:attr=\"test\" xmlns:~a p1 A~=\"bar\" xmlns:~f x A~=\"foo\" />"));
            }

            // Namespace string contains surrogates, reuse at different levels
            [Theory]
            [XmlWriterInlineData]
            public void elemNamespace_14(XmlWriterUtils utils)
            {
                string uri = "urn:\uD800\uDC00";

                using (XmlWriter w = utils.CreateWriter())
                {
                    w.WriteStartElement("root");
                    w.WriteAttributeString("xmlns", "pre", null, uri);
                    w.WriteElementString("elt", uri, "text");
                    w.WriteEndElement();
                }
                string strExpected = string.Format("<root xmlns:pre=\"{0}\"><pre:elt>text</pre:elt></root>", uri);
                Assert.True(utils.CompareReader(strExpected));
            }

            // Namespace containing entities, use at multiple levels
            [Theory]
            [XmlWriterInlineData]
            public void elemNamespace_15(XmlWriterUtils utils)
            {
                using (XmlWriter w = utils.CreateWriter())
                {
                    string strxml = "<?xml version=\"1.0\" ?><root xmlns:foo=\"urn:&lt;&gt;\"><foo:elt1 /><foo:elt2 /><foo:elt3 /></root>";

                    XmlReader xr = ReaderHelper.Create(new StringReader(strxml));
                    w.WriteNode(xr, false);
                    xr.Dispose();
                }
                Assert.True(utils.CompareReader("<root xmlns:foo=\"urn:&lt;&gt;\"><foo:elt1 /><foo:elt2 /><foo:elt3 /></root>"));
            }

            // Verify it resets default namespace when redefined earlier in the stack
            [Theory]
            [XmlWriterInlineData]
            public void elemNamespace_16(XmlWriterUtils utils)
            {
                using (XmlWriter w = utils.CreateWriter())
                {
                    w.WriteStartElement("", "x", "foo");
                    w.WriteAttributeString("xmlns", "foo");
                    w.WriteStartElement("", "y", "");
                    w.WriteStartElement("", "z", "foo");
                    w.WriteEndElement();
                    w.WriteEndElement();
                    w.WriteEndElement();
                }
                Assert.True(utils.CompareReader("<x xmlns=\"foo\"><y xmlns=\"\"><z xmlns=\"foo\" /></y></x>"));
            }

            // The default namespace for an element can not be changed once it is written out
            [Theory]
            [XmlWriterInlineData]
            public void elemNamespace_17(XmlWriterUtils utils)
            {
                using (XmlWriter w = utils.CreateWriter())
                {
                    try
                    {
                        w.WriteStartElement("Root");
                        w.WriteAttributeString("xmlns", null, "test");
                        w.WriteEndElement();
                    }
                    catch (XmlException e)
                    {
                        CError.WriteLineIgnore("Exception: " + e.ToString());
                        CError.Compare(w.WriteState, WriteState.Error, "WriteState should be Error");
                        return;
                    }
                }
                CError.WriteLine("Did not throw exception");
                Assert.Fail();
            }

            // Map XML NS 'http://www.w3.org/XML/1998/namaespace' to another prefix
            [Theory]
            [XmlWriterInlineData]
            public void elemNamespace_18(XmlWriterUtils utils)
            {
                using (XmlWriter w = utils.CreateWriter())
                {
                    w.WriteStartElement("foo", "bar", "http://www.w3.org/XML/1998/namaespace");
                    w.WriteEndElement();
                }
                Assert.True(utils.CompareReader("<foo:bar xmlns:foo=\"http://www.w3.org/XML/1998/namaespace\" />"));
            }

            // Pass NULL as NS to WriteStartElement
            [Theory]
            [XmlWriterInlineData]
            public void elemNamespace_19(XmlWriterUtils utils)
            {
                using (XmlWriter w = utils.CreateWriter())
                {
                    w.WriteStartElement("foo", "Root", "NS");
                    w.WriteStartElement("bar", null);
                    w.WriteEndElement();
                    w.WriteEndElement();
                }
                Assert.True(utils.CompareReader("<foo:Root xmlns:foo=\"NS\"><bar /></foo:Root>"));
            }

            // Write element in reserved XML namespace, should error
            [Theory]
            [XmlWriterInlineData]
            public void elemNamespace_20(XmlWriterUtils utils)
            {
                using (XmlWriter w = utils.CreateWriter())
                {
                    try
                    {
                        w.WriteStartElement("foo", "Root", "http://www.w3.org/XML/1998/namespace");
                    }
                    catch (ArgumentException e)
                    {
                        CError.WriteLineIgnore(e.ToString());
                        CError.Compare(w.WriteState, WriteState.Error, "WriteState should be Error");
                        return;
                    }
                }
                Assert.Fail();
            }

            // Write element in reserved XMLNS namespace, should error
            [Theory]
            [XmlWriterInlineData]
            public void elemNamespace_21(XmlWriterUtils utils)
            {
                using (XmlWriter w = utils.CreateWriter())
                {
                    try
                    {
                        w.WriteStartElement("foo", "Root", "http://www.w3.org/XML/1998/namespace");
                    }
                    catch (ArgumentException e)
                    {
                        CError.WriteLineIgnore(e.ToString());
                        CError.Compare(w.WriteState, WriteState.Error, "WriteState should be Error");
                        return;
                    }
                }
                Assert.Fail();
            }

            // Mapping a prefix to empty ns should error
            [Theory]
            [XmlWriterInlineData]
            public void elemNamespace_22(XmlWriterUtils utils)
            {
                using (XmlWriter w = utils.CreateWriter())
                {
                    try
                    {
                        w.WriteStartElement("pre", "test", string.Empty);
                        w.WriteEndElement();
                    }
                    catch (ArgumentException e)
                    {
                        CError.WriteLineIgnore("Exception: " + e.ToString());
                        CError.Compare(w.WriteState, WriteState.Error, "WriteState should be Error");
                        return;
                    }
                }
                CError.WriteLine("Did not throw exception");
                Assert.Fail();
            }

            // Pass null prefix to WriteStartElement()
            [Theory]
            [XmlWriterInlineData]
            public void elemNamespace_23(XmlWriterUtils utils)
            {
                using (XmlWriter w = utils.CreateWriter())
                {
                    w.WriteStartElement(null, "Root", "ns");
                    w.WriteEndElement();
                }
                Assert.True(utils.CompareReader("<Root xmlns='ns' />"));
            }

            // Pass String.Empty prefix to WriteStartElement()
            [Theory]
            [XmlWriterInlineData]
            public void elemNamespace_24(XmlWriterUtils utils)
            {
                using (XmlWriter w = utils.CreateWriter())
                {
                    w.WriteStartElement(string.Empty, "Root", "ns");
                    w.WriteEndElement();
                }
                Assert.True(utils.CompareReader("<Root xmlns='ns' />"));
            }

            // Pass null ns to WriteStartElement()
            [Theory]
            [XmlWriterInlineData]
            public void elemNamespace_25(XmlWriterUtils utils)
            {
                using (XmlWriter w = utils.CreateWriter())
                {
                    w.WriteStartElement("Root", null);
                    w.WriteEndElement();
                }
                Assert.True(utils.CompareReader("<Root />"));
            }

            // Pass String.Empty ns to WriteStartElement()
            [Theory]
            [XmlWriterInlineData]
            public void elemNamespace_26(XmlWriterUtils utils)
            {
                using (XmlWriter w = utils.CreateWriter())
                {
                    w.WriteStartElement("Root", string.Empty);
                    w.WriteEndElement();
                }
                Assert.True(utils.CompareReader("<Root />"));
            }

            // Pass null prefix to WriteStartElement() when namespace is in scope
            [Theory]
            [XmlWriterInlineData]
            public void elemNamespace_27(XmlWriterUtils utils)
            {
                using (XmlWriter w = utils.CreateWriter())
                {
                    w.WriteStartElement("pre", "Root", "ns");
                    w.WriteElementString(null, "child", "ns", "test");
                    w.WriteEndElement();
                }
                Assert.True(utils.CompareReader("<pre:Root xmlns:pre='ns'><pre:child>test</pre:child></pre:Root>"));
            }

            // Pass String.Empty prefix to WriteStartElement() when namespace is in scope
            [Theory]
            [XmlWriterInlineData]
            public void elemNamespace_28(XmlWriterUtils utils)
            {
                using (XmlWriter w = utils.CreateWriter())
                {
                    w.WriteStartElement("pre", "Root", "ns");
                    w.WriteElementString(string.Empty, "child", "ns", "test");
                    w.WriteEndElement();
                }
                Assert.True(utils.CompareReader("<pre:Root xmlns:pre='ns'><child xmlns='ns'>test</child></pre:Root>"));
            }

            // Pass null ns to WriteStartElement() when prefix is in scope
            [Theory]
            [XmlWriterInlineData]
            public void elemNamespace_29(XmlWriterUtils utils)
            {
                using (XmlWriter w = utils.CreateWriter())
                {
                    w.WriteStartElement("pre", "Root", "ns");
                    w.WriteElementString("pre", "child", null, "test");
                    w.WriteEndElement();
                }
                Assert.True(utils.CompareReader("<pre:Root xmlns:pre='ns'><pre:child>test</pre:child></pre:Root>"));
            }

            // Pass String.Empty ns to WriteStartElement() when prefix is in scope
            [Theory]
            [XmlWriterInlineData]
            public void elemNamespace_30(XmlWriterUtils utils)
            {
                using (XmlWriter w = utils.CreateWriter())
                {
                    try
                    {
                        w.WriteStartElement("pre", "Root", "ns");
                        w.WriteElementString("pre", "child", string.Empty, "test");
                    }
                    catch (ArgumentException)
                    {
                        return;
                    }
                }
                Assert.Fail();
            }

            // Pass String.Empty ns to WriteStartElement() when prefix is in scope
            [Theory]
            [XmlWriterInlineData]
            public void elemNamespace_31(XmlWriterUtils utils)
            {
                using (XmlWriter w = utils.CreateWriter())
                {
                    try
                    {
                        w.WriteStartElement("pre", "Root", "ns");
                        w.WriteElementString("pre", "child", string.Empty, "test");
                    }
                    catch (ArgumentException)
                    {
                        return;
                    }
                }
                Assert.Fail();
            }

            // Mapping empty ns uri to a prefix should error
            [Theory]
            [XmlWriterInlineData]
            public void elemNamespace_32(XmlWriterUtils utils)
            {
                using (XmlWriter w = utils.CreateWriter())
                {
                    try
                    {
                        w.WriteStartElement("prefix", "localname", null);
                        w.WriteEndElement();
                    }
                    catch (ArgumentException e)
                    {
                        CError.WriteLineIgnore(e.ToString());
                        return;
                    }
                }
                Assert.Fail();
            }
        }

        //[TestCase(Name = "Attribute Namespace")]
        public partial class TCAttrNamespace
        {
            // Define prefix 'xml' with invalid namespace URI 'foo'
            [Theory]
            [XmlWriterInlineData]
            public void attrNamespace_1(XmlWriterUtils utils)
            {
                using (XmlWriter w = utils.CreateWriter())
                {
                    try
                    {
                        w.WriteStartElement("Root");
                        w.WriteAttributeString("xmlns", "xml", null, "foo");
                    }
                    catch (ArgumentException e)
                    {
                        CError.WriteLineIgnore("Exception: " + e.ToString());
                        CError.Compare(w.WriteState, WriteState.Error, "WriteState should be Error");
                        return;
                    }
                }
                CError.WriteLine("Did not throw exception");
                Assert.Fail();
            }

            // Bind NS prefix 'xml' with valid namespace URI
            [Theory]
            [XmlWriterInlineData]
            public void attrNamespace_2(XmlWriterUtils utils)
            {
                using (XmlWriter w = utils.CreateWriter())
                {
                    w.WriteStartElement("Root");
                    w.WriteAttributeString("xmlns", "xml", null, "http://www.w3.org/XML/1998/namespace");
                    w.WriteEndElement();
                }
                string exp = (utils.WriterType == WriterType.UnicodeWriter) ? "<Root />" : "<Root xmlns:xml=\"http://www.w3.org/XML/1998/namespace\" />";
                Assert.True(utils.CompareReader(exp));
            }

            // Bind NS prefix 'xmlA' with namespace URI 'foo'
            [Theory]
            [XmlWriterInlineData]
            public void attrNamespace_3(XmlWriterUtils utils)
            {
                using (XmlWriter w = utils.CreateWriter())
                {
                    w.WriteStartElement("Root");
                    w.WriteAttributeString("xmlns", "xmlA", null, "foo");
                    w.WriteEndElement();
                }
                Assert.True(utils.CompareReader("<Root xmlns:xmlA=\"foo\" />"));
            }

            // Write attribute xml:space with correct namespace
            [Theory]
            [XmlWriterInlineData]
            public void attrNamespace_4(XmlWriterUtils utils)
            {
                using (XmlWriter w = utils.CreateWriter())
                {
                    w.WriteStartElement("Root");
                    w.WriteAttributeString("xml", "space", "http://www.w3.org/XML/1998/namespace", "default");
                    w.WriteEndElement();
                }
                Assert.True(utils.CompareReader("<Root xml:space=\"default\" />"));
            }

            // Write attribute xml:space with incorrect namespace
            [Theory]
            [XmlWriterInlineData]
            public void attrNamespace_5(XmlWriterUtils utils)
            {
                using (XmlWriter w = utils.CreateWriter())
                {
                    try
                    {
                        w.WriteStartElement("Root");
                        w.WriteAttributeString("xml", "space", "foo", "default");
                        w.WriteEndElement();
                    }
                    catch (ArgumentException e)
                    {
                        CError.WriteLineIgnore("Exception: " + e.ToString());
                        CError.Compare(w.WriteState, WriteState.Error, "WriteState should be Error");
                        return;
                    }
                }
                CError.WriteLine("Did not throw exception");
                Assert.Fail();
            }

            // Write attribute xml:lang with incorrect namespace
            [Theory]
            [XmlWriterInlineData]
            public void attrNamespace_6(XmlWriterUtils utils)
            {
                using (XmlWriter w = utils.CreateWriter())
                {
                    try
                    {
                        w.WriteStartElement("Root");
                        w.WriteAttributeString("xml", "lang", "foo", "EN");
                        w.WriteEndElement();
                    }
                    catch (ArgumentException e)
                    {
                        CError.WriteLineIgnore("Exception: " + e.ToString());
                        CError.Compare(w.WriteState, WriteState.Error, "WriteState should be Error");
                        return;
                    }
                }
                CError.WriteLine("Did not throw exception");
                Assert.Fail();
            }


            // WriteAttribute, define namespace attribute before value attribute
            [Theory]
            [XmlWriterInlineData]
            public void attrNamespace_7(XmlWriterUtils utils)
            {
                using (XmlWriter w = utils.CreateWriter())
                {
                    w.WriteStartElement("Root");
                    w.WriteAttributeString("xmlns", "x", null, "fo");
                    w.WriteAttributeString("a", "fo", "b");
                    w.WriteEndElement();
                }
                Assert.True(utils.CompareReader("<Root xmlns:x=\"fo\" x:a=\"b\" />"));
            }

            // WriteAttribute, define namespace attribute after value attribute
            [Theory]
            [XmlWriterInlineData]
            public void attrNamespace_8(XmlWriterUtils utils)
            {
                using (XmlWriter w = utils.CreateWriter())
                {
                    w.WriteStartElement("Root");
                    w.WriteAttributeString("x", "a", "fo", "b");
                    w.WriteAttributeString("xmlns", "x", null, "fo");
                    w.WriteEndElement();
                }
                Assert.True(utils.CompareReader("<Root x:a=\"b\" xmlns:x=\"fo\" />"));
            }

            // WriteAttribute, redefine prefix at different scope and use both of them
            [Theory]
            [XmlWriterInlineData]
            public void attrNamespace_9(XmlWriterUtils utils)
            {
                using (XmlWriter w = utils.CreateWriter())
                {
                    w.WriteStartElement("level1");
                    w.WriteAttributeString("xmlns", "x", null, "fo");
                    w.WriteAttributeString("a", "fo", "b");
                    w.WriteStartElement("level2");
                    w.WriteAttributeString("xmlns", "x", null, "bar");
                    w.WriteAttributeString("c", "bar", "d");
                    w.WriteEndElement();
                    w.WriteEndElement();
                }
                Assert.True(utils.CompareReader("<level1 xmlns:x=\"fo\" x:a=\"b\"><level2 xmlns:x=\"bar\" x:c=\"d\" /></level1>"));
            }

            // WriteAttribute, redefine namespace at different scope and use both of them
            [Theory]
            [XmlWriterInlineData]
            public void attrNamespace_10(XmlWriterUtils utils)
            {
                using (XmlWriter w = utils.CreateWriter())
                {
                    w.WriteStartElement("level1");
                    w.WriteAttributeString("xmlns", "x", null, "fo");
                    w.WriteAttributeString("a", "fo", "b");
                    w.WriteStartElement("level2");
                    w.WriteAttributeString("xmlns", "y", null, "fo");
                    w.WriteAttributeString("c", "fo", "d");
                    w.WriteEndElement();
                    w.WriteEndElement();
                }
                Assert.True(utils.CompareReader("<level1 xmlns:x=\"fo\" x:a=\"b\"><level2 xmlns:y=\"fo\" y:c=\"d\" /></level1>"));
            }

            // WriteAttribute with colliding prefix with element
            [Theory]
            [XmlWriterInlineData]
            public void attrNamespace_11(XmlWriterUtils utils)
            {
                using (XmlWriter w = utils.CreateWriter())
                {
                    w.WriteStartElement("x", "Root", "fo");
                    w.WriteAttributeString("x", "a", "bar", "b");
                    w.WriteEndElement();
                }
                Assert.True(utils.CompareString("<~f x a~:Root ~a p1 a~:a=\"b\" xmlns:~a p1 A~=\"bar\" xmlns:~f x A~=\"fo\" />"));
            }

            // WriteAttribute with colliding namespace with element
            [Theory]
            [XmlWriterInlineData]
            public void attrNamespace_12(XmlWriterUtils utils)
            {
                using (XmlWriter w = utils.CreateWriter())
                {
                    w.WriteStartElement("x", "Root", "fo");
                    w.WriteAttributeString("y", "a", "fo", "b");
                    w.WriteEndElement();
                }
                Assert.True(utils.CompareReader("<x:Root y:a=\"b\" xmlns:y=\"fo\" xmlns:x=\"fo\" />"));
            }

            // WriteAttribute with namespace but no prefix
            [Theory]
            [XmlWriterInlineData]
            public void attrNamespace_13(XmlWriterUtils utils)
            {
                using (XmlWriter w = utils.CreateWriter())
                {
                    w.WriteStartElement("Root");
                    w.WriteAttributeString("a", "fo", "b");
                    w.WriteEndElement();
                }
                Assert.True(utils.CompareString("<Root ~a p1 a~:a=\"b\" xmlns:~a p1 A~=\"fo\" />"));
            }

            // WriteAttribute for 2 attributes with same prefix but different namespace
            [Theory]
            [XmlWriterInlineData]
            public void attrNamespace_14(XmlWriterUtils utils)
            {
                using (XmlWriter w = utils.CreateWriter())
                {
                    w.WriteStartElement("Root");
                    w.WriteAttributeString("x", "a", "fo", "b");
                    w.WriteAttributeString("x", "c", "bar", "d");
                    w.WriteEndElement();
                }
                Assert.True(utils.CompareString("<Root ~f x a~:a=\"b\" ~a p2 a~:c=\"d\" xmlns:~a p2 A~=\"bar\" xmlns:~f x A~=\"fo\" />"));
            }

            // WriteAttribute with String.Empty and null as namespace and prefix values
            [Theory]
            [XmlWriterInlineData]
            public void attrNamespace_15(XmlWriterUtils utils)
            {
                using (XmlWriter w = utils.CreateWriter())
                {
                    w.WriteStartElement("Root");
                    w.WriteAttributeString(null, "a", null, "b");
                    w.WriteAttributeString(string.Empty, "c", string.Empty, "d");
                    w.WriteAttributeString(null, "e", string.Empty, "f");
                    w.WriteAttributeString(string.Empty, "g", null, "h");
                    w.WriteEndElement();
                }
                Assert.True(utils.CompareReader("<Root a=\"b\" c=\"d\" e=\"f\" g=\"h\" />"));
            }

            // WriteAttribute to manually create attribute of xmlns:x
            [Theory]
            [XmlWriterInlineData]
            public void attrNamespace_16(XmlWriterUtils utils)
            {
                using (XmlWriter w = utils.CreateWriter())
                {
                    w.WriteStartElement("Root");
                    w.WriteAttributeString("xmlns", "x", null, "test");
                    w.WriteStartElement("x", "level1", null);
                    w.WriteEndElement();
                    w.WriteEndElement();
                }
                Assert.True(utils.CompareReader("<Root xmlns:x=\"test\"><x:level1 /></Root>"));
            }

            // WriteAttribute with namespace value = null while a prefix exists
            [Theory]
            [XmlWriterInlineData]
            public void attrNamespace_17(XmlWriterUtils utils)
            {
                using (XmlWriter w = utils.CreateWriter())
                {
                    w.WriteStartElement("Root");
                    w.WriteAttributeString("x", "a", null, "b");
                    w.WriteEndElement();
                }
                Assert.True(utils.CompareReader("<Root a=\"b\" />"));
            }

            // WriteAttribute with namespace value = String.Empty while a prefix exists
            [Theory]
            [XmlWriterInlineData]
            public void attrNamespace_18(XmlWriterUtils utils)
            {
                using (XmlWriter w = utils.CreateWriter())
                {
                    w.WriteStartElement("Root");
                    w.WriteAttributeString("x", "a", string.Empty, "b");
                    w.WriteEndElement();
                }
                Assert.True(utils.CompareReader("<Root a=\"b\" />"));
            }


            // WriteAttribe in nested elements with same namespace but different prefix
            [Theory]
            [XmlWriterInlineData]
            public void attrNamespace_19(XmlWriterUtils utils)
            {
                using (XmlWriter w = utils.CreateWriter())
                {
                    w.WriteStartElement("Root");
                    w.WriteAttributeString("a", "x", "fo", "y");
                    w.WriteAttributeString("xmlns", "a", null, "fo");
                    w.WriteStartElement("level1");
                    w.WriteAttributeString("b", "x", "fo", "y");
                    w.WriteAttributeString("xmlns", "b", null, "fo");
                    w.WriteStartElement("level2");
                    w.WriteAttributeString("c", "x", "fo", "y");
                    w.WriteAttributeString("xmlns", "c", null, "fo");
                    w.WriteEndElement();
                    w.WriteEndElement();
                    w.WriteEndElement();
                }
                Assert.True(utils.CompareReader("<Root a:x=\"y\" xmlns:a=\"fo\"><level1 b:x=\"y\" xmlns:b=\"fo\"><level2 c:x=\"y\" xmlns:c=\"fo\" /></level1></Root>"));
            }

            // WriteAttribute for x:a and xmlns:a diff namespace
            [Theory]
            [XmlWriterInlineData]
            public void attrNamespace_20(XmlWriterUtils utils)
            {
                using (XmlWriter w = utils.CreateWriter())
                {
                    w.WriteStartElement("Root");
                    w.WriteAttributeString("x", "a", "bar", "b");
                    w.WriteAttributeString("xmlns", "a", null, "foo");
                    w.WriteEndElement();
                }
                Assert.True(utils.CompareReader("<Root x:a=\"b\" xmlns:a=\"foo\" xmlns:x=\"bar\" />"));
            }

            // WriteAttribute for x:a and xmlns:a same namespace
            [Theory]
            [XmlWriterInlineData]
            public void attrNamespace_21(XmlWriterUtils utils)
            {
                using (XmlWriter w = utils.CreateWriter())
                {
                    w.WriteStartElement("Root");
                    w.WriteAttributeString("x", "a", "foo", "b");
                    w.WriteAttributeString("xmlns", "a", null, "foo");
                    w.WriteEndElement();
                }
                Assert.True(utils.CompareReader("<Root x:a=\"b\" xmlns:a=\"foo\" xmlns:x=\"foo\" />"));
            }

            // WriteAttribute with colliding NS and prefix for 2 attributes
            [Theory]
            [XmlWriterInlineData]
            public void attrNamespace_22(XmlWriterUtils utils)
            {
                using (XmlWriter w = utils.CreateWriter())
                {
                    w.WriteStartElement("Root");
                    w.WriteAttributeString("xmlns", "x", null, "foo");
                    w.WriteAttributeString("x", "a", "foo", "b");
                    w.WriteAttributeString("x", "c", "foo", "b");
                    w.WriteEndElement();
                }
                Assert.True(utils.CompareReader("<Root xmlns:x=\"foo\" x:a=\"b\" x:c=\"b\" />"));
            }

            // WriteAttribute with DQ in namespace
            [Theory]
            [XmlWriterInlineData]
            public void attrNamespace_23(XmlWriterUtils utils)
            {
                using (XmlWriter w = utils.CreateWriter())
                {
                    w.WriteStartElement("Root");
                    w.WriteAttributeString("a", "\"", "b");
                    w.WriteEndElement();
                }
                Assert.True(utils.CompareString("<Root ~a p1 a~:a=\"b\" xmlns:~a p1 A~=\"&quot;\" />"));
            }

            // Attach prefix with empty namespace
            [Theory]
            [XmlWriterInlineData]
            public void attrNamespace_24(XmlWriterUtils utils)
            {
                using (XmlWriter w = utils.CreateWriter())
                {
                    try
                    {
                        w.WriteStartElement("Root");
                        w.WriteAttributeString("xmlns", "foo", "bar", "");
                        w.WriteEndElement();
                    }
                    catch (ArgumentException e)
                    {
                        CError.WriteLineIgnore(e.ToString());
                        CError.Compare(w.WriteState, WriteState.Error, "WriteState should be Error");
                        return;
                    }
                }
                CError.WriteLine("Did not throw exception");
                Assert.Fail();
            }

            // Explicitly write namespace attribute that maps XML NS 'http://www.w3.org/XML/1998/namaespace' to another prefix
            [Theory]
            [XmlWriterInlineData]
            public void attrNamespace_25(XmlWriterUtils utils)
            {
                using (XmlWriter w = utils.CreateWriter())
                {
                    w.WriteStartElement("Root");
                    w.WriteAttributeString("xmlns", "foo", "", "http://www.w3.org/XML/1998/namaespace");
                    w.WriteEndElement();
                }
                Assert.True(utils.CompareReader("<Root xmlns:foo=\"http://www.w3.org/XML/1998/namaespace\" />"));
            }

            // Map XML NS 'http://www.w3.org/XML/1998/namaespace' to another prefix
            [Theory]
            [XmlWriterInlineData]
            public void attrNamespace_26(XmlWriterUtils utils)
            {
                using (XmlWriter w = utils.CreateWriter())
                {
                    w.WriteStartElement("Root");
                    w.WriteAttributeString("foo", "bar", "http://www.w3.org/XML/1998/namaespace", "test");
                    w.WriteEndElement();
                }
                Assert.True(utils.CompareReader("<Root foo:bar=\"test\" xmlns:foo=\"http://www.w3.org/XML/1998/namaespace\" />"));
            }

            // Pass empty namespace to WriteAttributeString(prefix, name, ns, value)
            [Theory]
            [XmlWriterInlineData]
            public void attrNamespace_27(XmlWriterUtils utils)
            {
                using (XmlWriter w = utils.CreateWriter())
                {
                    w.WriteStartElement("pre", "Root", "urn:pre");
                    w.WriteAttributeString("pre", "attr", "", "test");
                    w.WriteEndElement();
                }
                Assert.True(utils.CompareReader("<pre:Root attr=\"test\" xmlns:pre=\"urn:pre\" />"));
            }

            // Write attribute with prefix = xmlns
            [Theory]
            [XmlWriterInlineData]
            public void attrNamespace_28(XmlWriterUtils utils)
            {
                using (XmlWriter w = utils.CreateWriter())
                {
                    try
                    {
                        w.WriteStartElement("Root");
                        w.WriteAttributeString("xmlns", "xmlns", null, "test");
                    }
                    catch (ArgumentException e)
                    {
                        CError.WriteLineIgnore(e.ToString());
                        CError.Compare(w.WriteState, WriteState.Error, "WriteState should be Error");
                        return;
                    }
                }
                CError.WriteLine("Did not throw exception");
                Assert.Fail();
            }

            // Write attribute in reserved XML namespace, should error
            [Theory]
            [XmlWriterInlineData]
            public void attrNamespace_29(XmlWriterUtils utils)
            {
                using (XmlWriter w = utils.CreateWriter())
                {
                    try
                    {
                        w.WriteStartElement("foo");
                        w.WriteAttributeString("aaa", "bbb", "http://www.w3.org/XML/1998/namespace", "ccc");
                    }
                    catch (ArgumentException e)
                    {
                        CError.WriteLineIgnore(e.ToString());
                        CError.Compare(w.WriteState, WriteState.Error, "WriteState should be Error");
                        return;
                    }
                }
                Assert.Fail();
            }

            // Write attribute in reserved XMLNS namespace, should error
            [Theory]
            [XmlWriterInlineData]
            public void attrNamespace_30(XmlWriterUtils utils)
            {
                using (XmlWriter w = utils.CreateWriter())
                {
                    try
                    {
                        w.WriteStartElement("foo");
                        w.WriteStartAttribute("aaa", "bbb", "http://www.w3.org/XML/1998/namespace");
                    }
                    catch (ArgumentException e)
                    {
                        CError.WriteLineIgnore(e.ToString());
                        CError.Compare(w.WriteState, WriteState.Error, "WriteState should be Error");
                        return;
                    }
                }
                Assert.Fail();
            }

            // WriteAttributeString with no namespace under element with empty prefix
            [Theory]
            [XmlWriterInlineData]
            public void attrNamespace_31(XmlWriterUtils utils)
            {
                using (XmlWriter w = utils.CreateWriter())
                {
                    w.WriteStartElement("d", "Data", "http://example.org/data");
                    w.WriteStartElement("g", "GoodStuff", "http://example.org/data/good");
                    w.WriteAttributeString("hello", "world");
                    w.WriteEndElement();
                    w.WriteStartElement("BadStuff", "http://example.org/data/bad");
                    w.WriteAttributeString("hello", "world");
                    w.WriteEndElement();
                    w.WriteEndElement();
                }
                Assert.True(utils.CompareReader("<d:Data xmlns:d=\"http://example.org/data\">" +
                                    "<g:GoodStuff hello=\"world\" xmlns:g=\"http://example.org/data/good\" />" +
                                    "<BadStuff hello=\"world\" xmlns=\"http://example.org/data/bad\" />" +
                                    "</d:Data>"));
            }

            // Pass null prefix to WriteAttributeString()
            [Theory]
            [XmlWriterInlineData]
            public void attrNamespace_32(XmlWriterUtils utils)
            {
                using (XmlWriter w = utils.CreateWriter())
                {
                    w.WriteStartElement("Root");
                    w.WriteAttributeString(null, "attr", "ns", "value");
                    w.WriteEndElement();
                }
                Assert.True(utils.CompareString("<Root ~a p1 a~:attr=\"value\" xmlns:~a p1 A~=\"ns\" />"));
            }

            // Pass String.Empty prefix to WriteAttributeString()
            [Theory]
            [XmlWriterInlineData]
            public void attrNamespace_33(XmlWriterUtils utils)
            {
                using (XmlWriter w = utils.CreateWriter())
                {
                    w.WriteStartElement("Root");
                    w.WriteAttributeString(string.Empty, "attr", "ns", "value");
                    w.WriteEndElement();
                }
                Assert.True(utils.CompareString("<Root ~a p1 a~:attr=\"value\" xmlns:~a p1 A~=\"ns\" />"));
            }

            // Pass null ns to WriteAttributeString()
            [Theory]
            [XmlWriterInlineData]
            public void attrNamespace_34(XmlWriterUtils utils)
            {
                using (XmlWriter w = utils.CreateWriter())
                {
                    w.WriteStartElement("Root");
                    w.WriteAttributeString("pre", "attr", null, "value");
                    w.WriteEndElement();
                }
                Assert.True(utils.CompareReader("<Root attr='value' />"));
            }

            // Pass String.Empty ns to WriteAttributeString()
            [Theory]
            [XmlWriterInlineData]
            public void attrNamespace_35(XmlWriterUtils utils)
            {
                using (XmlWriter w = utils.CreateWriter())
                {
                    w.WriteStartElement("Root");
                    w.WriteAttributeString("pre", "attr", string.Empty, "value");
                    w.WriteEndElement();
                }
                Assert.True(utils.CompareReader("<Root attr='value' />"));
            }

            // Pass null prefix to WriteAttributeString() when namespace is in scope
            [Theory]
            [XmlWriterInlineData]
            public void attrNamespace_36(XmlWriterUtils utils)
            {
                using (XmlWriter w = utils.CreateWriter())
                {
                    w.WriteStartElement("pre", "Root", "ns");
                    w.WriteAttributeString(null, "child", "ns", "test");
                    w.WriteEndElement();
                }
                Assert.True(utils.CompareReader("<pre:Root pre:child='test' xmlns:pre='ns' />"));
            }

            // Pass String.Empty prefix to WriteAttributeString() when namespace is in scope
            [Theory]
            [XmlWriterInlineData]
            public void attrNamespace_37(XmlWriterUtils utils)
            {
                using (XmlWriter w = utils.CreateWriter())
                {
                    w.WriteStartElement("pre", "Root", "ns");
                    w.WriteAttributeString(string.Empty, "child", "ns", "test");
                    w.WriteEndElement();
                }
                Assert.True(utils.CompareReader("<pre:Root pre:child='test' xmlns:pre='ns' />"));
            }

            // Pass null ns to WriteAttributeString() when prefix is in scope
            [Theory]
            [XmlWriterInlineData]
            public void attrNamespace_38(XmlWriterUtils utils)
            {
                using (XmlWriter w = utils.CreateWriter())
                {
                    w.WriteStartElement("pre", "Root", "ns");
                    w.WriteAttributeString("pre", "child", null, "test");
                    w.WriteEndElement();
                }
                Assert.True(utils.CompareReader("<pre:Root pre:child='test' xmlns:pre='ns' />"));
            }

            // Pass String.Empty ns to WriteAttributeString() when prefix is in scope
            [Theory]
            [XmlWriterInlineData]
            public void attrNamespace_39(XmlWriterUtils utils)
            {
                using (XmlWriter w = utils.CreateWriter())
                {
                    w.WriteStartElement("pre", "Root", "ns");
                    w.WriteAttributeString("pre", "child", string.Empty, "test");
                    w.WriteEndElement();
                }
                Assert.True(utils.CompareReader("<pre:Root child='test' xmlns:pre='ns' />"));
            }

            // Mapping empty ns uri to a prefix should error
            [Theory]
            [XmlWriterInlineData]
            public void attrNamespace_40(XmlWriterUtils utils)
            {
                using (XmlWriter w = utils.CreateWriter())
                {
                    try
                    {
                        w.WriteStartElement("root");
                        w.WriteAttributeString("xmlns", null, null, "test");
                        w.WriteEndElement();
                    }
                    catch (XmlException e)
                    {
                        CError.WriteLineIgnore(e.ToString());
                        return;
                    }
                    catch (ArgumentException e)
                    {
                        CError.WriteLineIgnore(e.ToString());
                        return;
                    }
                }
                Assert.Fail();
            }

            // WriteStartAttribute with prefix = null, localName = xmlns - case 2
            [Theory]
            [XmlWriterInlineData]
            public void attrNamespace_42(XmlWriterUtils utils)
            {
                using (XmlWriter w = utils.CreateWriter())
                {
                    w.WriteStartElement("pre", "foo", "ns1");
                    w.WriteAttributeString(null, "xmlns", "http://www.w3.org/2000/xmlns/", "ns");
                    w.WriteEndElement();
                }
                Assert.True(utils.CompareReader("<pre:foo xmlns='ns' xmlns:pre='ns1' />"));
            }
        }

        //[TestCase(Name = "WriteCData")]
        public partial class TCCData
        {
            // WriteCData with null
            [Theory]
            [XmlWriterInlineData]
            public void CData_1(XmlWriterUtils utils)
            {
                using (XmlWriter w = utils.CreateWriter())
                {
                    w.WriteStartElement("Root");
                    w.WriteCData(null);
                    w.WriteEndElement();
                }
                Assert.True(utils.CompareReader("<Root><![CDATA[]]></Root>"));
            }

            // WriteCData with String.Empty
            [Theory]
            [XmlWriterInlineData]
            public void CData_2(XmlWriterUtils utils)
            {
                using (XmlWriter w = utils.CreateWriter())
                {
                    w.WriteStartElement("Root");
                    w.WriteCData(string.Empty);
                    w.WriteEndElement();
                }
                Assert.True(utils.CompareReader("<Root><![CDATA[]]></Root>"));
            }

            // WriteCData Sanity test
            [Theory]
            [XmlWriterInlineData]
            public void CData_3(XmlWriterUtils utils)
            {
                using (XmlWriter w = utils.CreateWriter())
                {
                    w.WriteStartElement("Root");
                    w.WriteCData("This text is in a CDATA section");
                    w.WriteEndElement();
                }
                Assert.True(utils.CompareReader("<Root><![CDATA[This text is in a CDATA section]]></Root>"));
            }

            // WriteCData with valid surrogate pair
            [Theory]
            [XmlWriterInlineData]
            public void CData_4(XmlWriterUtils utils)
            {
                using (XmlWriter w = utils.CreateWriter())
                {
                    w.WriteStartElement("Root");
                    w.WriteCData("\uD812\uDD12");
                    w.WriteEndElement();
                }
                Assert.True(utils.CompareReader("<Root><![CDATA[\uD812\uDD12]]></Root>"));
            }

            // WriteCData with ]]>
            [Theory]
            [XmlWriterInlineData]
            public void CData_5(XmlWriterUtils utils)
            {
                using (XmlWriter w = utils.CreateWriter())
                {
                    w.WriteStartElement("Root");
                    w.WriteCData("test ]]> test");
                    w.WriteEndElement();
                }
                Assert.True(utils.CompareReader("<Root><![CDATA[test ]]]]><![CDATA[> test]]></Root>"));
            }

            // WriteCData with & < > chars, they should not be escaped
            [Theory]
            [XmlWriterInlineData]
            public void CData_6(XmlWriterUtils utils)
            {
                using (XmlWriter w = utils.CreateWriter())
                {
                    w.WriteStartElement("Root");
                    w.WriteCData("<greeting>Hello World! & Hello XML</greeting>");
                    w.WriteEndElement();
                }
                Assert.True(utils.CompareReader("<Root><![CDATA[<greeting>Hello World! & Hello XML</greeting>]]></Root>"));
            }

            // WriteCData with <![CDATA[
            [Theory]
            [XmlWriterInlineData]
            public void CData_7(XmlWriterUtils utils)
            {
                using (XmlWriter w = utils.CreateWriter())
                {
                    w.WriteStartElement("Root");
                    w.WriteCData("<![CDATA[");
                    w.WriteEndElement();
                }
                Assert.True(utils.CompareReader("<Root><![CDATA[<![CDATA[]]></Root>"));
            }
            // CData state machine
            [Theory]
            [XmlWriterInlineData]
            public void CData_8(XmlWriterUtils utils)
            {
                using (XmlWriter w = utils.CreateWriter())
                {
                    w.WriteStartElement("Root");
                    w.WriteCData("]x]>]]x> x]x]x> x]]x]]x>");
                    w.WriteEndElement();
                }
                Assert.True(utils.CompareReader("<Root><![CDATA[]x]>]]x> x]x]x> x]]x]]x>]]></Root>"));
            }

            // WriteCData with invalid surrogate pair
            [Theory]
            [XmlWriterInlineData]
            public void CData_9(XmlWriterUtils utils)
            {
                using (XmlWriter w = utils.CreateWriter())
                {
                    try
                    {
                        w.WriteStartElement("Root");
                        w.WriteCData("\uD812");
                    }
                    catch (ArgumentException e)
                    {
                        CError.WriteLineIgnore("Exception: " + e.ToString());
                        utils.CheckErrorState(w.WriteState);
                        return;
                    }
                }
                CError.WriteLine("Did not throw exception");
                Assert.Fail();
            }

            // WriteCData after root element
            [Theory]
            [XmlWriterInlineData]
            public void CData_10(XmlWriterUtils utils)
            {
                using (XmlWriter w = utils.CreateWriter())
                {
                    try
                    {
                        w.WriteStartElement("Root");
                        w.WriteEndElement();
                        w.WriteCData("foo");
                    }
                    catch (InvalidOperationException e)
                    {
                        CError.WriteLineIgnore("Exception: " + e.ToString());
                        CError.Compare(w.WriteState, WriteState.Error, "WriteState should be Error");
                        return;
                    }
                }
                CError.WriteLine("Did not throw exception");
                Assert.Fail();
            }

            // Call WriteCData twice - that should write two CData blocks
            [Theory]
            [XmlWriterInlineData]
            public void CData_11(XmlWriterUtils utils)
            {
                using (XmlWriter w = utils.CreateWriter())
                {
                    w.WriteStartElement("Root");
                    w.WriteCData("foo");
                    w.WriteCData("bar");
                    w.WriteEndElement();
                }
                Assert.True(utils.CompareReader("<Root><![CDATA[foo]]><![CDATA[bar]]></Root>"));
            }

            // WriteCData with empty string at the buffer boundary
            [Theory]
            [XmlWriterInlineData]
            public void CData_12(XmlWriterUtils utils)
            {
                // WriteCData with empty string when the write buffer looks like
                // <r>aaaaaaa....   (currently length is 2048 * 3 - len("<![CDATA[")
                int buflen = 2048 * 3;
                string xml1 = "<r>";
                string xml3 = "<![CDATA[";
                int padlen = buflen - xml1.Length - xml3.Length;
                string xml2 = new string('a', padlen);
                string xml4 = "]]></r>";
                string expXml = string.Format("{0}{1}{2}{3}", xml1, xml2, xml3, xml4);
                using (XmlWriter w = utils.CreateWriter())
                {
                    w.WriteStartElement("r");
                    w.WriteRaw(xml2);
                    w.WriteCData("");
                    w.WriteEndElement();
                }

                Assert.True(utils.CompareReader(expXml));
            }

            [Theory]
            [XmlWriterInlineData(0x0d, NewLineHandling.Replace, "<r><![CDATA[\r\n]]></r>" )]
            [XmlWriterInlineData(0x0d, NewLineHandling.None, "<r><![CDATA[\r]]></r>" )]
            [XmlWriterInlineData(0x0d, NewLineHandling.Entitize, "<r><![CDATA[\r]]></r>" )]
            [XmlWriterInlineData(0x0a, NewLineHandling.Replace, "<r><![CDATA[\r\n]]></r>" )]
            [XmlWriterInlineData(0x0a, NewLineHandling.None, "<r><![CDATA[\n]]></r>" )]
            [XmlWriterInlineData(0x0a, NewLineHandling.Entitize, "<r><![CDATA[\n]]></r>" )]
            public void CData_13(XmlWriterUtils utils, char ch, NewLineHandling nlh, string expXml)
            {
                XmlWriterSettings xws = new XmlWriterSettings();
                xws.OmitXmlDeclaration = true;
                xws.NewLineHandling = nlh;
                xws.NewLineChars = "\r\n";

                using (XmlWriter w = utils.CreateWriter(xws))
                {
                    w.WriteStartElement("r");
                    w.WriteCData(new string(ch, 1));
                    w.WriteEndElement();
                }
                Assert.Equal(expXml, utils.GetString());
            }
        }

        //[TestCase(Name = "WriteComment")]
        public partial class TCComment
        {
            // Sanity test for WriteComment
            [Theory]
            [XmlWriterInlineData]
            public void comment_1(XmlWriterUtils utils)
            {
                using (XmlWriter w = utils.CreateWriter())
                {
                    w.WriteStartElement("Root");
                    w.WriteComment("This text is a comment");
                    w.WriteEndElement();
                }
                Assert.True(utils.CompareReader("<Root><!--This text is a comment--></Root>"));
            }

            // Comment value = String.Empty
            [Theory]
            [XmlWriterInlineData]
            public void comment_2(XmlWriterUtils utils)
            {
                using (XmlWriter w = utils.CreateWriter())
                {
                    w.WriteStartElement("Root");
                    w.WriteComment(string.Empty);
                    w.WriteEndElement();
                }
                Assert.True(utils.CompareReader("<Root><!----></Root>"));
            }

            // Comment value = null
            [Theory]
            [XmlWriterInlineData]
            public void comment_3(XmlWriterUtils utils)
            {
                using (XmlWriter w = utils.CreateWriter())
                {
                    w.WriteStartElement("Root");
                    w.WriteComment(null);
                    w.WriteEndElement();
                }
                Assert.True(utils.CompareReader("<Root><!----></Root>"));
            }

            // WriteComment with valid surrogate pair
            [Theory]
            [XmlWriterInlineData]
            public void comment_4(XmlWriterUtils utils)
            {
                using (XmlWriter w = utils.CreateWriter())
                {
                    w.WriteStartElement("Root");
                    w.WriteComment("\uD812\uDD12");
                    w.WriteEndElement();
                }
                Assert.True(utils.CompareReader("<Root><!--\uD812\uDD12--></Root>"));
            }

            // WriteComment with invalid surrogate pair
            [Theory]
            [XmlWriterInlineData]
            public void comment_5(XmlWriterUtils utils)
            {
                using (XmlWriter w = utils.CreateWriter())
                {
                    try
                    {
                        w.WriteStartElement("Root");
                        w.WriteComment("\uD812");
                        w.WriteEndElement();
                    }
                    catch (ArgumentException e)
                    {
                        CError.WriteLineIgnore("Exception: " + e.ToString());
                        utils.CheckErrorState(w.WriteState);
                        return;
                    }
                }
                CError.WriteLine("Did not throw error");
                Assert.Fail();
            }

            // WriteComment with -- in value
            [Theory]
            [XmlWriterInlineData]
            public void comment_6(XmlWriterUtils utils)
            {
                using (XmlWriter w = utils.CreateWriter())
                {
                    w.WriteStartElement("Root");
                    w.WriteComment("test --");
                    w.WriteEndElement();
                }
                Assert.True(utils.CompareReader("<Root><!--test - - --></Root>"));
            }
        }

        //[TestCase(Name = "WriteEntityRef")]
        public partial class TCEntityRef
        {
            [Theory]
            [XmlWriterInlineData("null")]
            [XmlWriterInlineData("String.Empty")]
            [XmlWriterInlineData("test<test")]
            [XmlWriterInlineData("test>test")]
            [XmlWriterInlineData("test&test")]
            [XmlWriterInlineData("&test;")]
            [XmlWriterInlineData("test'test")]
            [XmlWriterInlineData("test\"test")]
            [XmlWriterInlineData("\xD")]
            [XmlWriterInlineData("\xD")]
            [XmlWriterInlineData("\xD\xA")]
            public void entityRef_1(XmlWriterUtils utils, string param)
            {
                string temp = null;
                switch (param)
                {
                    case "null":
                        temp = null;
                        break;
                    case "String.Empty":
                        temp = string.Empty;
                        break;
                    default:
                        temp = param;
                        break;
                }
                using (XmlWriter w = utils.CreateWriter())
                {
                    try
                    {
                        w.WriteStartElement("Root");
                        w.WriteEntityRef(temp);
                        w.WriteEndElement();
                    }
                    catch (ArgumentException e)
                    {
                        CError.WriteLineIgnore("Exception: " + e.ToString());
                        CError.Compare(w.WriteState, (utils.WriterType == WriterType.CharCheckingWriter) ? WriteState.Element : WriteState.Error, "WriteState should be Error");
                        return;
                    }
                    catch (NullReferenceException e)
                    {
                        CError.WriteLineIgnore("Exception: " + e.ToString());
                        CError.Compare(w.WriteState, (utils.WriterType == WriterType.CharCheckingWriter) ? WriteState.Element : WriteState.Error, "WriteState should be Error");
                        return;
                    }
                }
                CError.WriteLine("Did not throw error");
                Assert.Fail();
            }

            // WriteEntityRef with entity defined in doctype
            [Theory]
            [XmlWriterInlineData]
            public void entityRef_2(XmlWriterUtils utils)
            {
                string exp = utils.IsIndent() ?
                    "<!DOCTYPE Root [<!ENTITY e \"test\">]>" + Environment.NewLine + "<Root>&e;</Root>" :
                    "<!DOCTYPE Root [<!ENTITY e \"test\">]><Root>&e;</Root>";

                using (XmlWriter w = utils.CreateWriter())
                {
                    w.WriteDocType("Root", null, null, "<!ENTITY e \"test\">");
                    w.WriteStartElement("Root");
                    w.WriteEntityRef("e");
                    w.WriteEndElement();
                }

                Assert.Equal(exp, utils.GetString());
            }

            // WriteEntityRef in value for xml:lang attribute
            [Theory]
            [XmlWriterInlineData]
            public void entityRef_3(XmlWriterUtils utils)
            {
                string exp = utils.IsIndent() ?
                    "<!DOCTYPE root [<!ENTITY e \"en-us\">]>" + Environment.NewLine + "<root xml:lang=\"&e;&lt;\" />" :
                    "<!DOCTYPE root [<!ENTITY e \"en-us\">]><root xml:lang=\"&e;&lt;\" />";

                using (XmlWriter w = utils.CreateWriter())
                {
                    w.WriteDocType("root", null, null, "<!ENTITY e \"en-us\">");
                    w.WriteStartElement("root");
                    w.WriteStartAttribute("xml", "lang", null);
                    w.WriteEntityRef("e");
                    w.WriteString("<");
                    w.WriteEndAttribute();
                    w.WriteEndElement();
                }

                Assert.Equal(exp, utils.GetString());
            }

            // XmlWriter: Entity Refs are entitized twice in xml:lang attributes
            [Theory]
            [XmlWriterInlineData]
            public void var_14(XmlWriterUtils utils)
            {
                string exp = utils.IsIndent() ?
                    "<!DOCTYPE root [<!ENTITY e \"en-us\">]>" + Environment.NewLine + "<root xml:lang=\"&e;\" />" :
                    "<!DOCTYPE root [<!ENTITY e \"en-us\">]><root xml:lang=\"&e;\" />";

                using (XmlWriter w = utils.CreateWriter())
                {
                    w.WriteDocType("root", null, null, "<!ENTITY e \"en-us\">");
                    w.WriteStartElement("root");
                    w.WriteStartAttribute("xml", "lang", null);
                    w.WriteEntityRef("e");
                    w.WriteEndAttribute();
                    w.WriteEndElement();
                }

                Assert.Equal(exp, utils.GetString());
            }
        }

        //[TestCase(Name = "WriteCharEntity")]
        public partial class TCCharEntity
        {
            // WriteCharEntity with valid Unicode character
            [Theory]
            [XmlWriterInlineData]
            public void charEntity_1(XmlWriterUtils utils)
            {
                using (XmlWriter w = utils.CreateWriter())
                {
                    w.WriteStartElement("Root");
                    w.WriteStartAttribute("a");
                    w.WriteCharEntity('\uD23E');
                    w.WriteEndAttribute();
                    w.WriteCharEntity('\uD7FF');
                    w.WriteCharEntity('\uE000');
                    w.WriteEndElement();
                }
                Assert.True(utils.CompareReader("<Root a=\"&#xD23E;\">&#xD7FF;&#xE000;</Root>"));
            }

            // Call WriteCharEntity after WriteStartElement/WriteEndElement
            [Theory]
            [XmlWriterInlineData]
            public void charEntity_2(XmlWriterUtils utils)
            {
                using (XmlWriter w = utils.CreateWriter())
                {
                    w.WriteStartElement("Root");
                    w.WriteCharEntity('\uD001');
                    w.WriteStartElement("elem");
                    w.WriteCharEntity('\uF345');
                    w.WriteEndElement();
                    w.WriteCharEntity('\u0048');
                    w.WriteEndElement();
                }
                Assert.True(utils.CompareReader("<Root>&#xD001;<elem>&#xF345;</elem>&#x48;</Root>"));
            }

            // Call WriteCharEntity after WriteStartAttribute/WriteEndAttribute
            [Theory]
            [XmlWriterInlineData]
            public void charEntity_3(XmlWriterUtils utils)
            {
                using (XmlWriter w = utils.CreateWriter())
                {
                    w.WriteStartElement("Root");
                    w.WriteStartAttribute("a");
                    w.WriteCharEntity('\u1289');
                    w.WriteEndAttribute();
                    w.WriteCharEntity('\u2584');
                    w.WriteEndElement();
                }
                Assert.True(utils.CompareReader("<Root a=\"&#x1289;\">&#x2584;</Root>"));
            }

            // Character from low surrogate range
            [Theory]
            [XmlWriterInlineData]
            public void charEntity_4(XmlWriterUtils utils)
            {
                using (XmlWriter w = utils.CreateWriter())
                {
                    try
                    {
                        w.WriteStartElement("Root");
                        w.WriteCharEntity('\uDD12');
                    }
                    catch (ArgumentException e)
                    {
                        CError.WriteLineIgnore("Exception: " + e.ToString());
                        CError.Compare(w.WriteState, WriteState.Error, "WriteState should be Error");
                        return;
                    }
                }
                CError.WriteLine("Did not throw exception");
                Assert.Fail();
            }

            // Character from high surrogate range
            [Theory]
            [XmlWriterInlineData]
            public void charEntity_5(XmlWriterUtils utils)
            {
                using (XmlWriter w = utils.CreateWriter())
                {
                    try
                    {
                        w.WriteStartElement("Root");
                        w.WriteCharEntity('\uD812');
                    }
                    catch (ArgumentException e)
                    {
                        CError.WriteLineIgnore("Exception: " + e.ToString());
                        CError.Compare(w.WriteState, WriteState.Error, "WriteState should be Error");
                        return;
                    }
                }
                CError.WriteLine("Did not throw exception");
                Assert.Fail();
            }

            // Sanity test, pass 'a'
            [Theory]
            [XmlWriterInlineData]
            public void charEntity_7(XmlWriterUtils utils)
            {
                using (XmlWriter w = utils.CreateWriter())
                {
                    w.WriteStartElement("root");
                    w.WriteCharEntity('c');
                    w.WriteEndElement();
                }
                string strExp = "<root>&#x63;</root>";
                Assert.True(utils.CompareReader(strExp));
            }

            // WriteCharEntity for special attributes
            [Theory]
            [XmlWriterInlineData]
            public void charEntity_8(XmlWriterUtils utils)
            {
                using (XmlWriter w = utils.CreateWriter())
                {
                    w.WriteStartElement("root");
                    w.WriteStartAttribute("xml", "lang", null);
                    w.WriteCharEntity('A');
                    w.WriteString("\n");
                    w.WriteEndAttribute();
                    w.WriteEndElement();
                }
                Assert.True(utils.CompareReader("<root xml:lang=\"A&#xA;\" />"));
            }

            // XmlWriter generates invalid XML
            [Theory]
            [XmlWriterInlineData]
            public void bug35637(XmlWriterUtils utils)
            {
                XmlWriterSettings settings = new XmlWriterSettings
                {
                    Indent = true,
                    IndentChars = "\t",
                };

                using (XmlWriter xw = utils.CreateWriter())
                {
                    xw.WriteStartElement("root");
                    for (int i = 0; i < 150; i++)
                    {
                        xw.WriteElementString("e", "\u00e6\u00f8\u00e5\u00e9\u00ed\u00e8\u00f9\u00f6\u00f1\u00ea\u00fb\u00ee\u00c2\u00c5\u00d8\u00f5\u00cf");
                    }
                    xw.WriteElementString("end", "end");
                    xw.WriteEndElement();
                }

                using (XmlReader reader = utils.GetReader())
                {
                    reader.ReadToDescendant("end"); // should not throw here
                }

                return;
            }
        }

        //[TestCase(Name = "WriteSurrogateCharEntity")]
        public partial class TCSurrogateCharEntity
        {
            // SurrogateCharEntity after WriteStartElement/WriteEndElement
            [Theory]
            [XmlWriterInlineData]
            public void surrogateEntity_1(XmlWriterUtils utils)
            {
                using (XmlWriter w = utils.CreateWriter())
                {
                    w.WriteStartElement("Root");
                    w.WriteSurrogateCharEntity('\uDF41', '\uD920');
                    w.WriteStartElement("Elem");
                    w.WriteSurrogateCharEntity('\uDE44', '\uDAFF');
                    w.WriteEndElement();
                    w.WriteSurrogateCharEntity('\uDC22', '\uD820');
                    w.WriteEndElement();
                }
                Assert.True(utils.CompareReader("<Root>&#x58341;<Elem>&#xCFE44;</Elem>&#x18022;</Root>"));
            }

            // SurrogateCharEntity after WriteStartAttribute/WriteEndAttribute
            [Theory]
            [XmlWriterInlineData]
            public void surrogateEntity_2(XmlWriterUtils utils)
            {
                using (XmlWriter w = utils.CreateWriter())
                {
                    w.WriteStartElement("Root");
                    w.WriteStartAttribute("a");
                    w.WriteSurrogateCharEntity('\uDF41', '\uD920');
                    w.WriteEndAttribute();
                    w.WriteSurrogateCharEntity('\uDE44', '\uDAFF');
                    w.WriteEndElement();
                }
                Assert.True(utils.CompareReader("<Root a=\"&#x58341;\">&#xCFE44;</Root>"));
            }

            // Test with limits of surrogate range
            [Theory]
            [XmlWriterInlineData]
            public void surrogateEntity_3(XmlWriterUtils utils)
            {
                using (XmlWriter w = utils.CreateWriter())
                {
                    w.WriteStartElement("Root");
                    w.WriteStartAttribute("a");
                    w.WriteSurrogateCharEntity('\uDC00', '\uD800');
                    w.WriteEndAttribute();
                    w.WriteSurrogateCharEntity('\uDFFF', '\uD800');
                    w.WriteSurrogateCharEntity('\uDC00', '\uDBFF');
                    w.WriteSurrogateCharEntity('\uDFFF', '\uDBFF');
                    w.WriteEndElement();
                }
                Assert.True(utils.CompareReader("<Root a=\"&#x10000;\">&#x103FF;&#x10FC00;&#x10FFFF;</Root>"));
            }

            // Middle surrogate character
            [Theory]
            [XmlWriterInlineData]
            public void surrogateEntity_4(XmlWriterUtils utils)
            {
                using (XmlWriter w = utils.CreateWriter())
                {
                    w.WriteStartElement("Root");
                    w.WriteSurrogateCharEntity('\uDD12', '\uDA34');
                    w.WriteEndElement();
                }
                Assert.True(utils.CompareReader("<Root>&#x9D112;</Root>"));
            }

            // Invalid high surrogate character
            [Theory]
            [XmlWriterInlineData]
            public void surrogateEntity_5(XmlWriterUtils utils)
            {
                using (XmlWriter w = utils.CreateWriter())
                {
                    try
                    {
                        w.WriteStartElement("Root");
                        w.WriteSurrogateCharEntity('\uDD12', '\uDD01');
                    }
                    catch (ArgumentException e)
                    {
                        CError.WriteLineIgnore("Exception: " + e.ToString());
                        CError.Compare(w.WriteState, WriteState.Error, "WriteState should be Error");
                        return;
                    }
                }
                CError.WriteLine("Did not throw exception");
                Assert.Fail();
            }

            // Invalid low surrogate character
            [Theory]
            [XmlWriterInlineData]
            public void surrogateEntity_6(XmlWriterUtils utils)
            {
                using (XmlWriter w = utils.CreateWriter())
                {
                    try
                    {
                        w.WriteStartElement("Root");
                        w.WriteSurrogateCharEntity('\u1025', '\uD900');
                    }
                    catch (ArgumentException e)
                    {
                        CError.WriteLineIgnore("Exception: " + e.ToString());
                        CError.Compare(w.WriteState, WriteState.Error, "WriteState should be Error");
                        return;
                    }
                }
                CError.WriteLine("Did not throw exception");
                Assert.Fail();
            }

            // Swap high-low surrogate characters
            [Theory]
            [XmlWriterInlineData]
            public void surrogateEntity_7(XmlWriterUtils utils)
            {
                using (XmlWriter w = utils.CreateWriter())
                {
                    try
                    {
                        w.WriteStartElement("Root");
                        w.WriteSurrogateCharEntity('\uD9A2', '\uDE34');
                    }
                    catch (ArgumentException e)
                    {
                        CError.WriteLineIgnore("Exception: " + e.ToString());
                        CError.Compare(w.WriteState, WriteState.Error, "WriteState should be Error");
                        return;
                    }
                }
                CError.WriteLine("Did not throw exception");
                Assert.Fail();
            }

            // WriteSurrogateCharEntity for special attributes
            [Theory]
            [XmlWriterInlineData]
            public void surrogateEntity_8(XmlWriterUtils utils)
            {
                using (XmlWriter w = utils.CreateWriter())
                {
                    w.WriteStartElement("root");
                    w.WriteStartAttribute("xml", "lang", null);
                    w.WriteSurrogateCharEntity('\uDC00', '\uDBFF');
                    w.WriteEndAttribute();
                    w.WriteEndElement();
                }
                string strExp = "<root xml:lang=\"&#x10FC00;\" />";
                Assert.True(utils.CompareReader(strExp));
            }
        }

        //[TestCase(Name = "WriteProcessingInstruction")]
        public partial class TCPI
        {
            // Sanity test for WritePI
            [Theory]
            [XmlWriterInlineData]
            public void pi_1(XmlWriterUtils utils)
            {
                using (XmlWriter w = utils.CreateWriter())
                {
                    w.WriteStartElement("Root");
                    w.WriteProcessingInstruction("test", "This text is a PI");
                    w.WriteEndElement();
                }
                Assert.True(utils.CompareReader("<Root><?test This text is a PI?></Root>"));
            }

            // PI text value = null
            [Theory]
            [XmlWriterInlineData]
            public void pi_2(XmlWriterUtils utils)
            {
                using (XmlWriter w = utils.CreateWriter())
                {
                    w.WriteStartElement("Root");
                    w.WriteProcessingInstruction("test", null);
                    w.WriteEndElement();
                }
                Assert.True(utils.CompareReader("<Root><?test?></Root>"));
            }

            // PI text value = String.Empty
            [Theory]
            [XmlWriterInlineData]
            public void pi_3(XmlWriterUtils utils)
            {
                using (XmlWriter w = utils.CreateWriter())
                {
                    w.WriteStartElement("Root");
                    w.WriteProcessingInstruction("test", string.Empty);
                    w.WriteEndElement();
                }
                Assert.True(utils.CompareReader("<Root><?test?></Root>"));
            }

            // PI name = null should error
            [Theory]
            [XmlWriterInlineData]
            public void pi_4(XmlWriterUtils utils)
            {
                using (XmlWriter w = utils.CreateWriter())
                {
                    try
                    {
                        w.WriteStartElement("Root");
                        w.WriteProcessingInstruction(null, "test");
                    }
                    catch (ArgumentException e)
                    {
                        CError.WriteLineIgnore("Exception: " + e.ToString());
                        CError.Compare(w.WriteState, WriteState.Error, "WriteState should be Error");
                        return;
                    }
                    catch (NullReferenceException e)
                    {
                        CError.WriteLineIgnore("Exception: " + e.ToString());
                        CError.Compare(w.WriteState, WriteState.Element, "WriteState should be Element ");
                        return;
                    }
                }
                CError.WriteLine("Did not throw exception");
                Assert.Fail();
            }

            // PI name = String.Empty should error
            [Theory]
            [XmlWriterInlineData]
            public void pi_5(XmlWriterUtils utils)
            {
                using (XmlWriter w = utils.CreateWriter())
                {
                    try
                    {
                        w.WriteStartElement("Root");
                        w.WriteProcessingInstruction(string.Empty, "test");
                    }
                    catch (ArgumentException e)
                    {
                        CError.WriteLineIgnore("Exception: " + e.ToString());
                        CError.Compare(w.WriteState, (utils.WriterType == WriterType.CharCheckingWriter) ? WriteState.Element : WriteState.Error, "WriteState should be Error");
                        return;
                    }
                }
                CError.WriteLine("Did not throw exception");
                Assert.Fail();
            }

            // WritePI with xmlns as the name value
            [Theory]
            [XmlWriterInlineData]
            public void pi_6(XmlWriterUtils utils)
            {
                using (XmlWriter w = utils.CreateWriter())
                {
                    w.WriteStartElement("Root");
                    w.WriteProcessingInstruction("xmlns", "text");
                    w.WriteEndElement();
                }
                Assert.True(utils.CompareReader("<Root><?xmlns text?></Root>"));
            }

            // WritePI with XmL as the name value
            [Theory]
            [XmlWriterInlineData]
            public void pi_7(XmlWriterUtils utils)
            {
                using (XmlWriter w = utils.CreateWriter())
                {
                    try
                    {
                        w.WriteStartElement("Root");
                        w.WriteProcessingInstruction("XmL", "text");
                        w.WriteEndElement();
                        w.Dispose();
                    }
                    catch (ArgumentException e)
                    {
                        CError.WriteLineIgnore("Exception: " + e.ToString());
                        CError.Compare(w.WriteState, WriteState.Error, "WriteState should be Error");
                        return;
                    }
                }
                CError.WriteLine("Did not throw exception");
                Assert.Fail();
            }

            // WritePI before XmlDecl
            [Theory]
            [XmlWriterInlineData]
            public void pi_8(XmlWriterUtils utils)
            {
                using (XmlWriter w = utils.CreateWriter())
                {
                    try
                    {
                        w.WriteProcessingInstruction("pi", "text");
                        w.WriteStartDocument(true);
                    }
                    catch (InvalidOperationException e)
                    {
                        CError.WriteLineIgnore("Exception: " + e.ToString());
                        CError.Compare(w.WriteState, WriteState.Error, "WriteState should be Error");
                        return;
                    }
                }
                CError.WriteLine("Did not throw exception");
                Assert.Fail();
            }

            // WritePI (after StartDocument) with name = 'xml' text = 'version = 1.0' should error
            [Theory]
            [XmlWriterInlineData]
            public void pi_9(XmlWriterUtils utils)
            {
                using (XmlWriter w = utils.CreateWriter())
                {
                    try
                    {
                        w.WriteStartDocument();
                        w.WriteProcessingInstruction("xml", "version = \"1.0\"");
                    }
                    catch (ArgumentException e)
                    {
                        CError.WriteLineIgnore("Exception: " + e.ToString());
                        CError.Compare(w.WriteState, WriteState.Error, "WriteState should be Error");
                        return;
                    }
                }
                CError.WriteLine("Did not throw exception");
                Assert.Fail();
            }

            // WritePI (before StartDocument) with name = 'xml' text = 'version = 1.0' should error
            [Theory]
            [XmlWriterInlineData]
            public void pi_10(XmlWriterUtils utils)
            {
                using (XmlWriter w = utils.CreateWriter())
                {
                    try
                    {
                        w.WriteProcessingInstruction("xml", "version = \"1.0\"");
                        w.WriteStartDocument();
                    }
                    catch (InvalidOperationException e)
                    {
                        CError.WriteLineIgnore("Exception: " + e.ToString());
                        CError.Compare(w.WriteState, WriteState.Error, "WriteState should be Error");
                        return;
                    }
                }
                CError.WriteLine("Did not throw exception");
                Assert.Fail();
            }

            // Include PI end tag ?> as part of the text value
            [Theory]
            [XmlWriterInlineData]
            public void pi_11(XmlWriterUtils utils)
            {
                using (XmlWriter w = utils.CreateWriter())
                {
                    w.WriteStartElement("Root");
                    w.WriteProcessingInstruction("badpi", "text ?>");
                    w.WriteEndElement();
                }
                Assert.True(utils.CompareReader("<Root><?badpi text ? >?></Root>"));
            }

            // WriteProcessingInstruction with valid surrogate pair
            [Theory]
            [XmlWriterInlineData]
            public void pi_12(XmlWriterUtils utils)
            {
                using (XmlWriter w = utils.CreateWriter())
                {
                    w.WriteStartElement("Root");
                    w.WriteProcessingInstruction("pi", "\uD812\uDD12");
                    w.WriteEndElement();
                }
                Assert.True(utils.CompareReader("<Root><?pi \uD812\uDD12?></Root>"));
            }

            // WritePI with invalid surrogate pair
            [Theory]
            [XmlWriterInlineData]
            public void pi_13(XmlWriterUtils utils)
            {
                using (XmlWriter w = utils.CreateWriter())
                {
                    try
                    {
                        w.WriteStartElement("Root");
                        w.WriteProcessingInstruction("pi", "\uD812");
                        w.WriteEndElement();
                    }
                    catch (ArgumentException e)
                    {
                        CError.WriteLineIgnore("Exception: " + e.ToString());
                        utils.CheckErrorState(w.WriteState);
                        return;
                    }
                }
                CError.WriteLine("Did not throw exception");
                Assert.Fail();
            }
        }

        //[TestCase(Name = "WriteNmToken")]
        public partial class TCWriteNmToken
        {
            [Theory]
            [XmlWriterInlineData("null")]
            [XmlWriterInlineData("String.Empty")]
            public void writeNmToken_1(XmlWriterUtils utils, string param)
            {
                using (XmlWriter w = utils.CreateWriter())
                {
                    try
                    {
                        w.WriteStartElement("root");
                        string temp;
                        if (param == "null")
                            temp = null;
                        else
                            temp = string.Empty;
                        w.WriteNmToken(temp);
                        w.WriteEndElement();
                    }
                    catch (ArgumentException e)
                    {
                        CError.WriteLineIgnore(e.ToString());
                        utils.CheckElementState(w.WriteState);//by design 396962
                        return;
                    }
                }
                CError.WriteLine("Did not throw exception");
                Assert.Fail();
            }

            // Sanity test, Name = foo
            [Theory]
            [XmlWriterInlineData]
            public void writeNmToken_2(XmlWriterUtils utils)
            {
                using (XmlWriter w = utils.CreateWriter())
                {
                    w.WriteStartElement("root");
                    w.WriteNmToken("foo");
                    w.WriteEndElement();
                }
                Assert.True(utils.CompareReader("<root>foo</root>"));
            }

            // Name contains letters, digits, . _ - : chars
            [Theory]
            [XmlWriterInlineData]
            public void writeNmToken_3(XmlWriterUtils utils)
            {
                using (XmlWriter w = utils.CreateWriter())
                {
                    w.WriteStartElement("root");
                    w.WriteNmToken("_foo:1234.bar-");
                    w.WriteEndElement();
                }
                Assert.True(utils.CompareReader("<root>_foo:1234.bar-</root>"));
            }

            [Theory]
            [XmlWriterInlineData("test test")]
            [XmlWriterInlineData("test?")]
            [XmlWriterInlineData("test'")]
            [XmlWriterInlineData("\"test")]
            public void writeNmToken_4(XmlWriterUtils utils, string param)
            {
                using (XmlWriter w = utils.CreateWriter())
                {
                    try
                    {
                        w.WriteStartElement("root");
                        w.WriteNmToken(param);
                        w.WriteEndElement();
                    }
                    catch (ArgumentException e)
                    {
                        CError.WriteLineIgnore(e.ToString());
                        utils.CheckElementState(w.WriteState);
                        return;
                    }
                    catch (XmlException e)
                    {
                        CError.WriteLineIgnore(e.ToString());
                        utils.CheckElementState(w.WriteState);
                        return;
                    }
                }
                CError.WriteLine("Did not throw exception");
                Assert.Fail();
            }
        }

        //[TestCase(Name = "WriteName")]
        public partial class TCWriteName
        {
            [Theory]
            [XmlWriterInlineData("null")]
            [XmlWriterInlineData("String.Empty")]
            public void writeName_1(XmlWriterUtils utils, string param)
            {
                using (XmlWriter w = utils.CreateWriter())
                {
                    try
                    {
                        w.WriteStartElement("root");
                        string temp;
                        if (param == "null")
                            temp = null;
                        else
                            temp = string.Empty;
                        w.WriteName(temp);
                        w.WriteEndElement();
                    }
                    catch (ArgumentException e)
                    {
                        CError.WriteLineIgnore(e.ToString());
                        utils.CheckElementState(w.WriteState);
                        return;
                    }
                }
                CError.WriteLine("Did not throw exception");
                Assert.Fail();
            }

            // Sanity test, Name = foo
            [Theory]
            [XmlWriterInlineData]
            public void writeName_2(XmlWriterUtils utils)
            {
                using (XmlWriter w = utils.CreateWriter())
                {
                    w.WriteStartElement("root");
                    w.WriteName("foo");
                    w.WriteEndElement();
                }
                Assert.True(utils.CompareReader("<root>foo</root>"));
            }

            // Sanity test, Name = foo:bar
            [Theory]
            [XmlWriterInlineData]
            public void writeName_3(XmlWriterUtils utils)
            {
                using (XmlWriter w = utils.CreateWriter())
                {
                    w.WriteStartElement("root");
                    w.WriteName("foo:bar");
                    w.WriteEndElement();
                }
                Assert.True(utils.CompareReader("<root>foo:bar</root>"));
            }

            [Theory]
            [XmlWriterInlineData(":bar")]
            [XmlWriterInlineData("foo bar")]
            public void writeName_4(XmlWriterUtils utils, string param)
            {
                using (XmlWriter w = utils.CreateWriter())
                {
                    try
                    {
                        w.WriteStartElement("root");
                        w.WriteName(param);
                        w.WriteEndElement();
                    }
                    catch (ArgumentException e)
                    {
                        CError.WriteLineIgnore(e.ToString());
                        utils.CheckElementState(w.WriteState);
                        return;
                    }
                    catch (XmlException e)
                    {
                        CError.WriteLineIgnore(e.ToString());
                        utils.CheckElementState(w.WriteState);
                        return;
                    }
                }
                CError.WriteLine("Did not throw exception");
                Assert.Fail();
            }
        }

        //[TestCase(Name = "WriteQualifiedName")]
        public partial class TCWriteQName
        {
            [Theory]
            [XmlWriterInlineData("null")]
            [XmlWriterInlineData("String.Empty")]
            public void writeQName_1(XmlWriterUtils utils, string param)
            {
                using (XmlWriter w = utils.CreateWriter())
                {
                    try
                    {
                        w.WriteStartElement("root");
                        w.WriteAttributeString("xmlns", "foo", null, "test");
                        string temp;
                        if (param == "null")
                            temp = null;
                        else
                            temp = string.Empty;
                        w.WriteQualifiedName(temp, "test");
                        w.WriteEndElement();
                    }
                    catch (ArgumentException e)
                    {
                        CError.WriteLineIgnore(e.ToString());
                        CError.Compare(w.WriteState, (utils.WriterType == WriterType.CharCheckingWriter) ? WriteState.Element : WriteState.Error, "WriteState should be Error");
                        return;
                    }
                    catch (NullReferenceException e)
                    {
                        CError.WriteLineIgnore(e.ToString());
                        CError.Compare(w.WriteState, (utils.WriterType == WriterType.CharCheckingWriter) ? WriteState.Element : WriteState.Error, "WriteState should be Error");
                        return;
                    }
                }
                CError.WriteLine("Did not throw exception");
                Assert.True(utils.WriterType == WriterType.CustomWriter);
            }

            // WriteQName with correct NS
            [Theory]
            [XmlWriterInlineData]
            public void writeQName_2(XmlWriterUtils utils)
            {
                using (XmlWriter w = utils.CreateWriter())
                {
                    w.WriteStartElement("root");
                    w.WriteAttributeString("xmlns", "foo", null, "test");
                    w.WriteQualifiedName("bar", "test");
                    w.WriteEndElement();
                }
                Assert.True(utils.CompareReader("<root xmlns:foo=\"test\">foo:bar</root>"));
            }

            // WriteQName when NS is auto-generated
            [Theory]
            [XmlWriterInlineData]
            public void writeQName_3(XmlWriterUtils utils)
            {
                using (XmlWriter w = utils.CreateWriter())
                {
                    w.WriteStartElement("foo", "root", "test");
                    w.WriteQualifiedName("bar", "test");
                    w.WriteEndElement();
                }
                Assert.True(utils.CompareReader("<foo:root xmlns:foo=\"test\">foo:bar</foo:root>"));
            }

            // QName = foo:bar when foo is not in scope
            [Theory]
            [XmlWriterInlineData]
            public void writeQName_4(XmlWriterUtils utils)
            {
                using (XmlWriter w = utils.CreateWriter())
                {
                    try
                    {
                        w.WriteStartElement("root");
                        w.WriteQualifiedName("bar", "foo");
                        w.WriteEndElement();
                    }
                    catch (ArgumentException e)
                    {
                        CError.WriteLineIgnore(e.ToString());
                        if (utils.WriterType == WriterType.CustomWriter)
                        {
                            CError.Compare(w.WriteState, WriteState.Element, "WriteState should be Element");
                        }
                        else
                        {
                            CError.Compare(w.WriteState, WriteState.Error, "WriteState should be Error");
                        }
                        return;
                    }
                }
                CError.WriteLine("Did not throw exception");
                Assert.Fail();
            }

            [Theory]
            [XmlWriterInlineData(":bar")]
            [XmlWriterInlineData("foo bar")]
            public void writeQName_5(XmlWriterUtils utils, string param)
            {
                using (XmlWriter w = utils.CreateWriter())
                {
                    try
                    {
                        w.WriteStartElement("root");
                        w.WriteAttributeString("xmlns", "foo", null, "test");
                        w.WriteQualifiedName(param, "test");
                        w.WriteEndElement();
                    }
                    catch (ArgumentException e)
                    {
                        CError.WriteLineIgnore(e.ToString());
                        CError.Compare(w.WriteState, (utils.WriterType == WriterType.CharCheckingWriter) ? WriteState.Element : WriteState.Error, "WriteState should be Error");
                        return;
                    }
                }
                CError.WriteLine("Did not throw exception");
                Assert.True(utils.WriterType == WriterType.CustomWriter);
            }
        }

        //[TestCase(Name = "WriteChars")]
        public partial class TCWriteChars : TCWriteBuffer
        {
            // WriteChars with valid buffer, number, count
            [Theory]
            [XmlWriterInlineData]
            public void writeChars_1(XmlWriterUtils utils)
            {
                using (XmlWriter w = utils.CreateWriter())
                {
                    string s = "test the buffer";
                    char[] buf = s.ToCharArray();
                    w.WriteStartElement("Root");
                    w.WriteChars(buf, 0, 4);
                    w.WriteEndElement();
                }
                Assert.True(utils.CompareReader("<Root>test</Root>"));
            }

            // WriteChars with & < >
            [Theory]
            [XmlWriterInlineData]
            public void writeChars_2(XmlWriterUtils utils)
            {
                using (XmlWriter w = utils.CreateWriter())
                {
                    string s = "&<>theend";
                    char[] buf = s.ToCharArray();

                    w.WriteStartElement("Root");
                    w.WriteChars(buf, 0, 5);
                    w.WriteEndElement();
                }
                Assert.True(utils.CompareReader("<Root>&amp;&lt;&gt;th</Root>"));
            }

            // WriteChars following WriteStartAttribute
            [Theory]
            [XmlWriterInlineData]
            public void writeChars_3(XmlWriterUtils utils)
            {
                using (XmlWriter w = utils.CreateWriter())
                {
                    string s = "valid";
                    char[] buf = s.ToCharArray();

                    w.WriteStartElement("Root");
                    w.WriteStartAttribute("a");
                    w.WriteChars(buf, 0, 5);
                    w.WriteEndElement();
                }
                Assert.True(utils.CompareReader("<Root a=\"valid\" />"));
            }

            // WriteChars with entity ref included
            [Theory]
            [XmlWriterInlineData]
            public void writeChars_4(XmlWriterUtils utils)
            {
                using (XmlWriter w = utils.CreateWriter())
                {
                    string s = "this is an entity &foo;";
                    char[] buf = s.ToCharArray();

                    w.WriteStartElement("Root");
                    w.WriteChars(buf, 0, buf.Length);
                    w.WriteEndElement();
                }
                Assert.True(utils.CompareReader("<Root>this is an entity &amp;foo;</Root>"));
            }

            // WriteChars with buffer = null
            [Theory]
            [XmlWriterInlineData]
            public void writeChars_5(XmlWriterUtils utils)
            {
                using (XmlWriter w = utils.CreateWriter())
                {
                    try
                    {
                        w.WriteStartElement("Root");
                        w.WriteChars(null, 0, 0);
                    }
                    catch (ArgumentException e)
                    {
                        CError.WriteLineIgnore("Exception: " + e.ToString());
                        CError.Compare(w.WriteState, WriteState.Error, WriteState.Element, "WriteState should be Error");
                        return;
                    }
                }
                CError.WriteLine("Did not throw exception");
                Assert.Fail();
            }

            // WriteChars with count > buffer size
            [Theory]
            [XmlWriterInlineData]
            public void writeChars_6(XmlWriterUtils utils)
            {
                VerifyInvalidWrite(utils, "WriteChars", 5, 0, 6, typeof(ArgumentOutOfRangeException));
            }

            // WriteChars with count < 0
            [Theory]
            [XmlWriterInlineData]
            public void writeChars_7(XmlWriterUtils utils)
            {
                VerifyInvalidWrite(utils, "WriteChars", 5, 2, -1, typeof(ArgumentOutOfRangeException));
            }

            // WriteChars with index > buffer size
            [Theory]
            [XmlWriterInlineData]
            public void writeChars_8(XmlWriterUtils utils)
            {
                VerifyInvalidWrite(utils, "WriteChars", 5, 6, 1, typeof(ArgumentOutOfRangeException));
            }

            // WriteChars with index < 0
            [Theory]
            [XmlWriterInlineData]
            public void writeChars_9(XmlWriterUtils utils)
            {
                VerifyInvalidWrite(utils, "WriteChars", 5, -1, 1, typeof(ArgumentOutOfRangeException));
            }

            // WriteChars with index + count exceeds buffer
            [Theory]
            [XmlWriterInlineData]
            public void writeChars_10(XmlWriterUtils utils)
            {
                VerifyInvalidWrite(utils, "WriteChars", 5, 2, 5, typeof(ArgumentOutOfRangeException));
            }

            // WriteChars for xml:lang attribute, index = count = 0
            [Theory]
            [XmlWriterInlineData]
            public void writeChars_11(XmlWriterUtils utils)
            {
                using (XmlWriter w = utils.CreateWriter())
                {
                    string s = "en-us;";
                    char[] buf = s.ToCharArray();

                    w.WriteStartElement("root");
                    w.WriteStartAttribute("xml", "lang", null);
                    w.WriteChars(buf, 0, 0);
                    w.WriteEndElement();
                }
                Assert.True(utils.CompareReader("<root xml:lang=\"\" />"));
            }
        }

        //[TestCase(Name = "WriteString")]
        public partial class TCWriteString
        {
            // WriteString(null)
            [Theory]
            [XmlWriterInlineData]
            public void writeString_1(XmlWriterUtils utils)
            {
                using (XmlWriter w = utils.CreateWriter())
                {
                    w.WriteStartElement("Root");
                    w.WriteString(null);
                    w.WriteEndElement();
                }
                Assert.True(utils.CompareReader("<Root />"));
            }

            // WriteString(String.Empty)
            [Theory]
            [XmlWriterInlineData]
            public void writeString_2(XmlWriterUtils utils)
            {
                using (XmlWriter w = utils.CreateWriter())
                {
                    w.WriteStartElement("Root");
                    w.WriteString(string.Empty);
                    w.WriteEndElement();
                }
                Assert.True(utils.CompareReader("<Root></Root>"));
            }

            // WriteString with valid surrogate pair
            [Theory]
            [XmlWriterInlineData]
            public void writeString_3(XmlWriterUtils utils)
            {
                using (XmlWriter w = utils.CreateWriter())
                {
                    w.WriteStartElement("Root");
                    w.WriteString("\uD812\uDD12");
                    w.WriteEndElement();
                }
                Assert.True(utils.CompareReader("<Root>\uD812\uDD12</Root>"));
            }

            // WriteString with invalid surrogate pair
            [Theory]
            [XmlWriterInlineData]
            public void writeString_4(XmlWriterUtils utils)
            {
                using (XmlWriter w = utils.CreateWriter())
                {
                    try
                    {
                        w.WriteStartElement("Root");
                        w.WriteString("\uD812");
                        w.WriteEndElement();
                    }
                    catch (ArgumentException e)
                    {
                        CError.WriteLineIgnore("Exception: " + e.ToString());
                        utils.CheckErrorState(w.WriteState);
                        return;
                    }
                }
                CError.WriteLine("Did not throw exception");
                Assert.Fail();
            }

            // WriteString with entity reference
            [Theory]
            [XmlWriterInlineData]
            public void writeString_5(XmlWriterUtils utils)
            {
                using (XmlWriter w = utils.CreateWriter())
                {
                    w.WriteStartElement("Root");
                    w.WriteString("&test;");
                    w.WriteEndElement();
                }
                Assert.True(utils.CompareReader("<Root>&amp;test;</Root>"));
            }

            // WriteString with single/double quote, &, <, >
            [Theory]
            [XmlWriterInlineData]
            public void writeString_6(XmlWriterUtils utils)
            {
                using (XmlWriter w = utils.CreateWriter())
                {
                    w.WriteStartElement("Root");
                    w.WriteString("' & < > \"");
                    w.WriteEndElement();
                }
                Assert.True(utils.CompareReader("<Root>&apos; &amp; &lt; &gt; \"</Root>"));
            }

            // WriteString for value greater than x1F
            [Theory]
            [XmlWriterInlineData]
            public void writeString_9(XmlWriterUtils utils)
            {
                using (XmlWriter w = utils.CreateWriter())
                {
                    w.WriteStartElement("Root");
                    w.WriteString(XmlConvert.ToString('\x21'));
                    w.WriteEndElement();
                }
                Assert.True(utils.CompareReader("<Root>!</Root>"));
            }

            // WriteString with CR, LF, CR LF inside element
            [Theory]
            [XmlWriterInlineData]
            public void writeString_10(XmlWriterUtils utils)
            {
                // By default NormalizeNewLines = false and NewLineChars = \r\n
                // So \r, \n or \r\n gets replaces by \r\n in element content
                using (XmlWriter w = utils.CreateWriter())
                {
                    w.WriteStartElement("Root");
                    w.WriteStartElement("ws1");
                    w.WriteString("\r");
                    w.WriteEndElement();
                    w.WriteStartElement("ws2");
                    w.WriteString("\n");
                    w.WriteEndElement();
                    w.WriteStartElement("ws3");
                    w.WriteString("\r\n");
                    w.WriteEndElement();
                    w.WriteEndElement();
                }
                Assert.True(utils.CompareBaseline("writeStringWhiespaceInElem.txt"));
            }

            // WriteString with CR, LF, CR LF inside attribute value
            [Theory]
            [XmlWriterInlineData]
            public void writeString_11(XmlWriterUtils utils)
            {
                // \r, \n and \r\n gets replaced by char entities &#xD; &#xA; and &#xD;&#xA; respectively

                using (XmlWriter w = utils.CreateWriter())
                {
                    w.WriteStartElement("Root");
                    w.WriteStartAttribute("attr1");
                    w.WriteString("\r");
                    w.WriteStartAttribute("attr2");
                    w.WriteString("\n");
                    w.WriteStartAttribute("attr3");
                    w.WriteString("\r\n");
                    w.WriteEndElement();
                }
                Assert.True(utils.CompareBaseline("writeStringWhiespaceInAttr.txt"));
            }

            // Call WriteString for LF inside attribute
            [Theory]
            [XmlWriterInlineData]
            public void writeString_12(XmlWriterUtils utils)
            {
                using (XmlWriter w = utils.CreateWriter())
                {
                    w.WriteStartElement("Root", "");
                    w.WriteStartAttribute("a1", "");
                    w.WriteString("x\ny");
                    w.WriteEndElement();
                }
                Assert.True(utils.CompareReader("<Root a1=\"x&#xA;y\" />"));
            }

            // Surrogate characters in text nodes, range limits
            [Theory]
            [XmlWriterInlineData]
            public void writeString_13(XmlWriterUtils utils)
            {
                char[] invalidXML = { '\uD800', '\uDC00', '\uD800', '\uDFFF', '\uDBFF', '\uDC00', '\uDBFF', '\uDFFF' };
                string invXML = new string(invalidXML);

                using (XmlWriter w = utils.CreateWriter())
                {
                    w.WriteStartElement("Root");
                    w.WriteString(invXML);
                    w.WriteEndElement();
                }
                Assert.True(utils.CompareReader("<Root>\uD800\uDC00\uD800\uDFFF\uDBFF\uDC00\uDBFF\uDFFF</Root>"));
            }

            // High surrogate on last position
            [Theory]
            [XmlWriterInlineData]
            public void writeString_14(XmlWriterUtils utils)
            {
                char[] invalidXML = { 'a', 'b', '\uDA34' };
                string invXML = new string(invalidXML);

                using (XmlWriter w = utils.CreateWriter())
                {
                    try
                    {
                        w.WriteStartElement("Root");
                        w.WriteString(invXML);
                    }
                    catch (ArgumentException e)
                    {
                        CError.WriteLineIgnore("Exception: " + e.ToString());
                        utils.CheckErrorState(w.WriteState);
                        return;
                    }
                }
                CError.WriteLine("Did not throw exception");
                Assert.Fail();
            }

            // Low surrogate on first position
            [Theory]
            [XmlWriterInlineData]
            public void writeString_15(XmlWriterUtils utils)
            {
                char[] invalidXML = { '\uDF20', 'b', 'c' };
                string invXML = new string(invalidXML);

                using (XmlWriter w = utils.CreateWriter())
                {
                    try
                    {
                        w.WriteStartElement("Root");
                        w.WriteString(invXML);
                    }
                    catch (ArgumentException e)
                    {
                        CError.WriteLineIgnore("Exception: " + e.ToString());
                        utils.CheckErrorState(w.WriteState);
                        return;
                    }
                }
                CError.WriteLine("Did not throw exception");
                Assert.Fail();
            }

            // Swap low-high surrogates
            [Theory]
            [XmlWriterInlineData]
            public void writeString_16(XmlWriterUtils utils)
            {
                char[] invalidXML = { 'a', '\uDE40', '\uDA72', 'c' };
                string invXML = new string(invalidXML);

                using (XmlWriter w = utils.CreateWriter())
                {
                    try
                    {
                        w.WriteStartElement("Root");
                        w.WriteString(invXML);
                    }
                    catch (ArgumentException e)
                    {
                        CError.WriteLineIgnore("Exception: " + e.ToString());
                        utils.CheckErrorState(w.WriteState);
                        return;
                    }
                }
                CError.WriteLine("Did not throw exception");
                Assert.Fail();
            }
        }

        //[TestCase(Name = "WriteWhitespace")]
        public partial class TCWhiteSpace
        {
            // WriteWhitespace with values #x20 #x9 #xD #xA
            [Theory]
            [XmlWriterInlineData]
            public void whitespace_1(XmlWriterUtils utils)
            {
                using (XmlWriter w = utils.CreateWriter())
                {
                    w.WriteStartElement("Root");
                    w.WriteString("text");
                    w.WriteWhitespace("\x20");
                    w.WriteString("text");
                    w.WriteWhitespace("\x9");
                    w.WriteString("text");
                    w.WriteWhitespace("\xD");
                    w.WriteString("text");
                    w.WriteWhitespace("\xA");
                    w.WriteString("text");
                    w.WriteEndElement();
                }
                Assert.True(utils.CompareBaseline("whitespace1.txt"));
            }

            // WriteWhitespace in the middle of text
            [Theory]
            [XmlWriterInlineData]
            public void whitespace_2(XmlWriterUtils utils)
            {
                using (XmlWriter w = utils.CreateWriter())
                {
                    w.WriteStartElement("Root");
                    w.WriteString("text");
                    w.WriteWhitespace("\xD");
                    w.WriteString("text");
                    w.WriteEndElement();
                }
                Assert.True(utils.CompareBaseline("whitespace2.txt"));
            }

            // WriteWhitespace before and after root element
            [Theory]
            [XmlWriterInlineData]
            public void whitespace_3(XmlWriterUtils utils)
            {
                using (XmlWriter w = utils.CreateWriter())
                {
                    w.WriteStartDocument();
                    w.WriteWhitespace("\x20");
                    w.WriteStartElement("Root");
                    w.WriteEndElement();
                    w.WriteWhitespace("\x20");
                    w.WriteEndDocument();
                }
                Assert.True(utils.CompareBaseline("whitespace3.txt"));
            }

            [Theory]
            [XmlWriterInlineData("null")]
            [XmlWriterInlineData("String.Empty")]
            public void whitespace_4(XmlWriterUtils utils, string param)
            {
                using (XmlWriter w = utils.CreateWriter())
                {
                    string temp;
                    if (param == "null")
                        temp = null;
                    else
                        temp = string.Empty;
                    w.WriteStartElement("Root");

                    w.WriteWhitespace(temp);
                    w.WriteEndElement();
                }
                Assert.True(utils.CompareReader("<Root></Root>"));
            }

            [Theory]
            [XmlWriterInlineData("a")]
            [XmlWriterInlineData("\xE")]
            [XmlWriterInlineData("\x0")]
            [XmlWriterInlineData("\x10")]
            [XmlWriterInlineData("\x1F")]
            public void whitespace_5(XmlWriterUtils utils, string param)
            {
                using (XmlWriter w = utils.CreateWriter())
                {
                    try
                    {
                        w.WriteStartElement("Root");
                        w.WriteWhitespace(param);
                    }
                    catch (ArgumentException e)
                    {
                        CError.WriteLineIgnore("Exception: " + e.ToString());
                        CError.Compare(w.WriteState, (utils.WriterType == WriterType.CharCheckingWriter) ? WriteState.Element : WriteState.Error, "WriteState should be Error");
                        return;
                    }
                }
                CError.WriteLine("Did not throw exception");
                Assert.Fail();
            }
        }

        //[TestCase(Name = "WriteValue")]
        public partial class TCWriteValue
        {
            // Write multiple atomic values inside element
            [Theory]
            [XmlWriterInlineData]
            public void writeValue_1(XmlWriterUtils utils)
            {
                using (XmlWriter w = utils.CreateWriter())
                {
                    w.WriteStartElement("Root");
                    w.WriteValue((int)2);
                    w.WriteValue((bool)true);
                    w.WriteValue((double)3.14);
                    w.WriteEndElement();
                }
                Assert.True((utils.CompareReader("<Root>2true3.14</Root>")));
            }

            // Write multiple atomic values inside attribute
            [Theory]
            [XmlWriterInlineData]
            public void writeValue_2(XmlWriterUtils utils)
            {
                using (XmlWriter w = utils.CreateWriter())
                {
                    w.WriteStartElement("Root");
                    w.WriteStartAttribute("attr");
                    w.WriteValue((int)2);
                    w.WriteValue((bool)true);
                    w.WriteValue((double)3.14);
                    w.WriteEndElement();
                }
                Assert.True((utils.CompareReader("<Root attr=\"2true3.14\" />")));
            }

            // Write multiple atomic values inside element, separate by WriteWhitespace(' ')
            [Theory]
            [XmlWriterInlineData]
            public void writeValue_3(XmlWriterUtils utils)
            {
                using (XmlWriter w = utils.CreateWriter())
                {
                    w.WriteStartElement("Root");
                    w.WriteValue((int)2);
                    w.WriteWhitespace(" ");
                    w.WriteValue((bool)true);
                    w.WriteWhitespace(" ");
                    w.WriteValue((double)3.14);
                    w.WriteWhitespace(" ");
                    w.WriteEndElement();
                }
                Assert.True((utils.CompareReader("<Root>2 true 3.14 </Root>")));
            }

            // Write multiple atomic values inside element, separate by WriteString(' ')
            [Theory]
            [XmlWriterInlineData]
            public void writeValue_4(XmlWriterUtils utils)
            {
                using (XmlWriter w = utils.CreateWriter())
                {
                    w.WriteStartElement("Root");
                    w.WriteValue((int)2);
                    w.WriteString(" ");
                    w.WriteValue((bool)true);
                    w.WriteString(" ");
                    w.WriteValue((double)3.14);
                    w.WriteString(" ");
                    w.WriteEndElement();
                }
                Assert.True((utils.CompareReader("<Root>2 true 3.14 </Root>")));
            }

            // Write multiple atomic values inside attribute, separate by WriteWhitespace(' ')
            [Theory]
            [XmlWriterInlineData]
            public void writeValue_5(XmlWriterUtils utils)
            {
                using (XmlWriter w = utils.CreateWriter())
                {
                    try
                    {
                        w.WriteStartElement("Root");
                        w.WriteStartAttribute("attr");
                        w.WriteValue((int)2);
                        w.WriteWhitespace(" ");
                        w.WriteValue((bool)true);
                        w.WriteWhitespace(" ");
                        w.WriteValue((double)3.14);
                        w.WriteWhitespace(" ");
                        w.WriteEndElement();
                    }
                    catch (InvalidOperationException e)
                    {
                        CError.WriteLine(e);
                        Assert.Fail();
                    }
                }
                Assert.True((utils.CompareReader("<Root attr=\"2 true 3.14 \" />")));
            }

            // Write multiple atomic values inside attribute, separate by WriteString(' ')
            [Theory]
            [XmlWriterInlineData]
            public void writeValue_6(XmlWriterUtils utils)
            {
                using (XmlWriter w = utils.CreateWriter())
                {
                    w.WriteStartElement("Root");
                    w.WriteStartAttribute("attr");
                    w.WriteValue((int)2);
                    w.WriteString(" ");
                    w.WriteValue((bool)true);
                    w.WriteString(" ");
                    w.WriteValue((double)3.14);
                    w.WriteString(" ");
                    w.WriteEndElement();
                }
                Assert.True((utils.CompareReader("<Root attr=\"2 true 3.14 \" />")));
            }

            // WriteValue(long)
            [Theory]
            [XmlWriterInlineData]
            public void writeValue_7(XmlWriterUtils utils)
            {
                using (XmlWriter w = utils.CreateWriter())
                {
                    w.WriteStartElement("Root");
                    w.WriteValue(long.MaxValue);
                    w.WriteStartElement("child");
                    w.WriteValue(long.MinValue);
                    w.WriteEndElement();
                    w.WriteEndElement();
                }
                Assert.True((utils.CompareReader("<Root>9223372036854775807<child>-9223372036854775808</child></Root>")));
            }

            [Theory]
            [XmlWriterInlineData("string")]
            [XmlWriterInlineData("object")]
            public void writeValue_8(XmlWriterUtils utils, string param)
            {
                using (XmlWriter w = utils.CreateWriter())
                {
                    w.WriteStartElement("root");
                    switch (param)
                    {
                        case "string":
                            w.WriteValue((string)null);
                            return;
                        case "object":
                            try
                            {
                                w.WriteValue((object)null);
                            }
                            catch (ArgumentNullException) { return; }
                            break;
                    }
                    throw new CTestFailedException("Test failed.");
                }
            }

            private void VerifyValue(Type dest, object expVal, int param)
            {
                object actual;

                using (Stream fsr = FilePathUtil.getStream("writer.out"))
                {
                    using (XmlReader r = ReaderHelper.Create(fsr))
                    {
                        while (r.Read())
                        {
                            if (r.NodeType == XmlNodeType.Element)
                                break;
                        }
                        if (param == 1)
                        {
                            actual = (object)r.ReadElementContentAs(dest, null);
                            if (!actual.Equals(expVal))
                                CError.Compare(actual.ToString(), expVal.ToString(), "RECA");
                        }
                        else
                        {
                            r.MoveToAttribute("a");
                            actual = (object)r.ReadContentAs(dest, null);
                            if (!actual.Equals(expVal))
                                CError.Compare(actual.ToString(), expVal.ToString(), "RCA");
                        }
                    }
                }
            }

            public static Dictionary<string, Type> typeMapper;
            public static Dictionary<string, object> value;
            public static Dictionary<string, object> expValue;

            static TCWriteValue()
            {
                if (typeMapper == null)
                {
                    typeMapper = new Dictionary<string, Type>();
                    typeMapper.Add("UInt64", typeof(ulong));
                    typeMapper.Add("UInt32", typeof(uint));
                    typeMapper.Add("UInt16", typeof(ushort));
                    typeMapper.Add("Int64", typeof(long));
                    typeMapper.Add("Int32", typeof(int));
                    typeMapper.Add("Int16", typeof(short));
                    typeMapper.Add("Byte", typeof(byte));
                    typeMapper.Add("SByte", typeof(sbyte));
                    typeMapper.Add("Decimal", typeof(decimal));
                    typeMapper.Add("Single", typeof(float));
                    typeMapper.Add("float", typeof(float));
                    typeMapper.Add("object", typeof(object));
                    typeMapper.Add("bool", typeof(bool));
                    typeMapper.Add("DateTime", typeof(DateTime));
                    typeMapper.Add("DateTimeOffset", typeof(DateTimeOffset));
                    typeMapper.Add("ByteArray", typeof(byte[]));
                    typeMapper.Add("BoolArray", typeof(bool[]));
                    typeMapper.Add("ObjectArray", typeof(object[]));
                    typeMapper.Add("DecimalArray", typeof(decimal[]));
                    typeMapper.Add("DoubleArray", typeof(double[]));
                    typeMapper.Add("DateTimeArray", typeof(DateTime[]));
                    typeMapper.Add("DateTimeOffsetArray", typeof(DateTimeOffset[]));
                    typeMapper.Add("Int16Array", typeof(short[]));
                    typeMapper.Add("Int32Array", typeof(int[]));
                    typeMapper.Add("Int64Array", typeof(long[]));
                    typeMapper.Add("SByteArray", typeof(sbyte[]));
                    typeMapper.Add("SingleArray", typeof(float[]));
                    typeMapper.Add("StringArray", typeof(string[]));
                    typeMapper.Add("TimeSpanArray", typeof(TimeSpan[]));
                    typeMapper.Add("UInt16Array", typeof(ushort[]));
                    typeMapper.Add("UInt32Array", typeof(uint[]));
                    typeMapper.Add("UInt64Array", typeof(ulong[]));
                    typeMapper.Add("UriArray", typeof(Uri[]));
                    typeMapper.Add("XmlQualifiedNameArray", typeof(XmlQualifiedName[]));
                    typeMapper.Add("List", typeof(List<string>));
                    typeMapper.Add("TimeSpan", typeof(TimeSpan));
                    typeMapper.Add("Double", typeof(double));
                    typeMapper.Add("Uri", typeof(Uri));
                    typeMapper.Add("XmlQualifiedName", typeof(XmlQualifiedName));
                    typeMapper.Add("Char", typeof(char));
                    typeMapper.Add("string", typeof(string));
                }
                if (value == null)
                {
                    value = new Dictionary<string, object>();
                    value.Add("UInt64", ulong.MaxValue);
                    value.Add("UInt32", uint.MaxValue);
                    value.Add("UInt16", ushort.MaxValue);
                    value.Add("Int64", long.MaxValue);
                    value.Add("Int32", int.MaxValue);
                    value.Add("Int16", short.MaxValue);
                    value.Add("Byte", byte.MaxValue);
                    value.Add("SByte", sbyte.MaxValue);
                    value.Add("Decimal", decimal.MaxValue);
                    value.Add("Single", -4582.24);
                    value.Add("float", -4582.24F);
                    value.Add("object", 0);
                    value.Add("bool", false);
                    value.Add("DateTime", new DateTime(2002, 1, 3, 21, 59, 59, 59));
                    value.Add("DateTimeOffset", new DateTimeOffset(2002, 1, 3, 21, 59, 59, 59, TimeSpan.FromHours(0)));
                    value.Add("ByteArray", new byte[] { 0xd8, 0x7e });
                    value.Add("BoolArray", new bool[] { true, false });
                    value.Add("ObjectArray", new object[] { 0, 1 });
                    value.Add("DecimalArray", new decimal[] { 0, 1 });
                    value.Add("DoubleArray", new double[] { 0, 1 });
                    value.Add("DateTimeArray", new DateTime[] { new DateTime(2002, 12, 30), new DateTime(2, 1, 3, 23, 59, 59, 59) });
                    value.Add("DateTimeOffsetArray", new DateTimeOffset[] { new DateTimeOffset(2002, 12, 30, 0, 0, 0, TimeSpan.FromHours(-8.0)), new DateTimeOffset(2, 1, 3, 23, 59, 59, 59, TimeSpan.FromHours(0)) });
                    value.Add("Int16Array", new short[] { 0, 1 });
                    value.Add("Int32Array", new int[] { 0, 1 });
                    value.Add("Int64Array", new long[] { 0, 1 });
                    value.Add("SByteArray", new sbyte[] { 0, 1 });
                    value.Add("SingleArray", new float[] { 0, 1 });
                    value.Add("StringArray", new string[] { "0", "1" });
                    value.Add("TimeSpanArray", new TimeSpan[] { TimeSpan.MinValue, TimeSpan.MaxValue });
                    value.Add("UInt16Array", new ushort[] { 0, 1 });
                    value.Add("UInt32Array", new uint[] { 0, 1 });
                    value.Add("UInt64Array", new ulong[] { 0, 1 });
                    value.Add("UriArray", new Uri[] { new Uri("http://wddata", UriKind.Absolute), new Uri("http://webxtest") });
                    value.Add("XmlQualifiedNameArray", new XmlQualifiedName[] { new XmlQualifiedName("a"), new XmlQualifiedName("b", null) });
                    value.Add("List", new List<Guid>[] { });
                    value.Add("TimeSpan", new TimeSpan());
                    value.Add("Double", double.MaxValue);
                    value.Add("Uri", "http");
                    value.Add("XmlQualifiedName", new XmlQualifiedName("a", null));
                    value.Add("Char", char.MaxValue);
                    value.Add("string", "123");
                }
            }
            private object[] _dates = new object[]
            {
                new DateTimeOffset(2002,1,3,21,59,59,59, TimeZoneInfo.Local.GetUtcOffset(new DateTime(2002,1,3))),
                "2002-01-03T21:59:59.059",
                XmlConvert.ToString(new DateTimeOffset(2002,1,3,21,59,59,59, TimeSpan.FromHours(0)))
            };

            [Theory]
            [XmlWriterInlineData(1, "UInt64", "string", true, null )]
            [XmlWriterInlineData(1, "UInt32", "string", true, null )]
            [XmlWriterInlineData(1, "UInt16", "string", true, null )]
            [XmlWriterInlineData(1, "Int64", "string", true, null )]
            [XmlWriterInlineData(1, "Int32", "string", true, null )]
            [XmlWriterInlineData(1, "Int16", "string", true, null )]
            [XmlWriterInlineData(1, "Byte", "string", true, null )]
            [XmlWriterInlineData(1, "SByte", "string", true, null )]
            [XmlWriterInlineData(1, "Decimal", "string", true, null )]
            [XmlWriterInlineData(1, "float", "string", true, null )]
            [XmlWriterInlineData(1, "object", "string", true, null )]
            [XmlWriterInlineData(1, "bool", "string", true, "false" )]
            [XmlWriterInlineData(1, "DateTime", "string", true, 1 )]
            [XmlWriterInlineData(1, "DateTimeOffset", "string", true, 2 )]
            [XmlWriterInlineData(1, "ByteArray", "string", true, "2H4=" )]
            [XmlWriterInlineData(1, "List", "string", true, "" )]
            [XmlWriterInlineData(1, "TimeSpan", "string", true, "PT0S" )]
            [XmlWriterInlineData(1, "Uri", "string", true, null )]
            [XmlWriterInlineData(1, "Double", "string", true, "1.7976931348623157E+308" )]
            [XmlWriterInlineData(1, "Single", "string", true, null )]
            [XmlWriterInlineData(1, "XmlQualifiedName", "string", true, null )]
            [XmlWriterInlineData(1, "string", "string", true, null )]

            [XmlWriterInlineData(1, "UInt64", "UInt64", true, null )]
            [XmlWriterInlineData(1, "UInt32", "UInt64", true, null )]
            [XmlWriterInlineData(1, "UInt16", "UInt64", true, null )]
            [XmlWriterInlineData(1, "Int64", "UInt64", true, null )]
            [XmlWriterInlineData(1, "Int32", "UInt64", true, null )]
            [XmlWriterInlineData(1, "Int16", "UInt64", true, null )]
            [XmlWriterInlineData(1, "Byte", "UInt64", true, null )]
            [XmlWriterInlineData(1, "SByte", "UInt64", true, null )]
            [XmlWriterInlineData(1, "Decimal", "UInt64", false, null )]
            [XmlWriterInlineData(1, "float", "UInt64", false, null )]
            [XmlWriterInlineData(1, "object", "UInt64", true, null )]
            [XmlWriterInlineData(1, "bool", "UInt64", false, null )]
            [XmlWriterInlineData(1, "DateTime", "UInt64", false, null )]
            [XmlWriterInlineData(1, "DateTimeOffset", "UInt64", false, null )]
            [XmlWriterInlineData(1, "ByteArray", "UInt64", false, null )]
            [XmlWriterInlineData(1, "List", "UInt64", false, null )]
            [XmlWriterInlineData(1, "TimeSpan", "UInt64", false, null )]
            [XmlWriterInlineData(1, "Uri", "UInt64", false, null )]
            [XmlWriterInlineData(1, "Double", "UInt64", false, null )]
            [XmlWriterInlineData(1, "Single", "UInt64", false, null )]
            [XmlWriterInlineData(1, "XmlQualifiedName", "UInt64", false, null )]
            [XmlWriterInlineData(1, "string", "UInt64", true, null )]

            [XmlWriterInlineData(1, "UInt64", "Int64", false, null )]
            [XmlWriterInlineData(1, "UInt32", "Int64", true, null )]
            [XmlWriterInlineData(1, "UInt16", "Int64", true, null )]
            [XmlWriterInlineData(1, "Int64", "Int64", true, null )]
            [XmlWriterInlineData(1, "Int32", "Int64", true, null )]
            [XmlWriterInlineData(1, "Int16", "Int64", true, null )]
            [XmlWriterInlineData(1, "Byte", "Int64", true, null )]
            [XmlWriterInlineData(1, "SByte", "Int64", true, null )]
            [XmlWriterInlineData(1, "Decimal", "Int64", false, null )]
            [XmlWriterInlineData(1, "float", "Int64", false, null )]
            [XmlWriterInlineData(1, "object", "Int64", true, null )]
            [XmlWriterInlineData(1, "bool", "Int64", false, null )]
            [XmlWriterInlineData(1, "DateTime", "Int64", false, null )]
            [XmlWriterInlineData(1, "DateTimeOffset", "Int64", false, null )]
            [XmlWriterInlineData(1, "ByteArray", "Int64", false, null )]
            [XmlWriterInlineData(1, "List", "Int64", false, null )]
            [XmlWriterInlineData(1, "TimeSpan", "Int64", false, null )]
            [XmlWriterInlineData(1, "Uri", "Int64", false, null )]
            [XmlWriterInlineData(1, "Double", "Int64", false, null )]
            [XmlWriterInlineData(1, "Single", "Int64", false, null )]
            [XmlWriterInlineData(1, "XmlQualifiedName", "Int64", false, null )]
            [XmlWriterInlineData(1, "string", "Int64", true, null )]

            [XmlWriterInlineData(1, "UInt64", "UInt32", false, null )]
            [XmlWriterInlineData(1, "UInt32", "UInt32", true, null )]
            [XmlWriterInlineData(1, "UInt16", "UInt32", true, null )]
            [XmlWriterInlineData(1, "Int64", "UInt32", false, null )]
            [XmlWriterInlineData(1, "Int32", "UInt32", true, null )]
            [XmlWriterInlineData(1, "Int16", "UInt32", true, null )]
            [XmlWriterInlineData(1, "Byte", "UInt32", true, null )]
            [XmlWriterInlineData(1, "SByte", "UInt32", true, null )]
            [XmlWriterInlineData(1, "Decimal", "UInt32", false, null )]
            [XmlWriterInlineData(1, "float", "UInt32", false, null )]
            [XmlWriterInlineData(1, "object", "UInt32", true, null )]
            [XmlWriterInlineData(1, "bool", "UInt32", false, null )]
            [XmlWriterInlineData(1, "DateTime", "UInt32", false, null )]
            [XmlWriterInlineData(1, "DateTimeOffset", "UInt32", false, null )]
            [XmlWriterInlineData(1, "ByteArray", "UInt32", false, null )]
            [XmlWriterInlineData(1, "List", "UInt32", false, null )]
            [XmlWriterInlineData(1, "TimeSpan", "UInt32", false, null )]
            [XmlWriterInlineData(1, "Uri", "UInt32", false, null )]
            [XmlWriterInlineData(1, "Double", "UInt32", false, null )]
            [XmlWriterInlineData(1, "Single", "UInt32", false, null )]
            [XmlWriterInlineData(1, "XmlQualifiedName", "UInt32", false, null )]
            [XmlWriterInlineData(1, "string", "UInt32", true, null )]

            [XmlWriterInlineData(1, "UInt64", "Int32", false, null )]
            [XmlWriterInlineData(1, "UInt32", "Int32", false, null )]
            [XmlWriterInlineData(1, "UInt16", "Int32", true, null )]
            [XmlWriterInlineData(1, "Int64", "Int32", false, null )]
            [XmlWriterInlineData(1, "Int32", "Int32", true, null )]
            [XmlWriterInlineData(1, "Int16", "Int32", true, null )]
            [XmlWriterInlineData(1, "Byte", "Int32", true, null )]
            [XmlWriterInlineData(1, "SByte", "Int32", true, null )]
            [XmlWriterInlineData(1, "Decimal", "Int32", false, null )]
            [XmlWriterInlineData(1, "float", "Int32", false, null )]
            [XmlWriterInlineData(1, "object", "Int32", true, null )]
            [XmlWriterInlineData(1, "bool", "Int32", false, null )]
            [XmlWriterInlineData(1, "DateTime", "Int32", false, null )]
            [XmlWriterInlineData(1, "DateTimeOffset", "Int32", false, null )]
            [XmlWriterInlineData(1, "ByteArray", "Int32", false, null )]
            [XmlWriterInlineData(1, "List", "Int32", false, null )]
            [XmlWriterInlineData(1, "TimeSpan", "Int32", false, null )]
            [XmlWriterInlineData(1, "Uri", "Int32", false, null )]
            [XmlWriterInlineData(1, "Double", "Int32", false, null )]
            [XmlWriterInlineData(1, "Single", "Int32", false, null )]
            [XmlWriterInlineData(1, "XmlQualifiedName", "Int32", false, null )]
            [XmlWriterInlineData(1, "string", "Int32", true, null )]

            [XmlWriterInlineData(1, "UInt64", "UInt16", false, null )]
            [XmlWriterInlineData(1, "UInt32", "UInt16", false, null )]
            [XmlWriterInlineData(1, "UInt16", "UInt16", true, null )]
            [XmlWriterInlineData(1, "Int64", "UInt16", false, null )]
            [XmlWriterInlineData(1, "Int32", "UInt16", false, null )]
            [XmlWriterInlineData(1, "Int16", "UInt16", true, null )]
            [XmlWriterInlineData(1, "Byte", "UInt16", true, null )]
            [XmlWriterInlineData(1, "SByte", "UInt16", true, null )]
            [XmlWriterInlineData(1, "Decimal", "UInt16", false, null )]
            [XmlWriterInlineData(1, "float", "UInt16", false, null )]
            [XmlWriterInlineData(1, "object", "UInt16", true, null )]
            [XmlWriterInlineData(1, "bool", "UInt16", false, null )]
            [XmlWriterInlineData(1, "DateTime", "UInt16", false, null )]
            [XmlWriterInlineData(1, "DateTimeOffset", "UInt16", false, null )]
            [XmlWriterInlineData(1, "ByteArray", "UInt16", false, null )]
            [XmlWriterInlineData(1, "List", "UInt16", false, null )]
            [XmlWriterInlineData(1, "TimeSpan", "UInt16", false, null )]
            [XmlWriterInlineData(1, "Uri", "UInt16", false, null )]
            [XmlWriterInlineData(1, "Double", "UInt16", false, null )]
            [XmlWriterInlineData(1, "Single", "UInt16", false, null )]
            [XmlWriterInlineData(1, "XmlQualifiedName", "UInt16", false, null )]
            [XmlWriterInlineData(1, "string", "UInt16", true, null )]

            [XmlWriterInlineData(1, "UInt64", "Int16", false, null )]
            [XmlWriterInlineData(1, "UInt32", "Int16", false, null )]
            [XmlWriterInlineData(1, "UInt16", "Int16", false, null )]
            [XmlWriterInlineData(1, "Int64", "Int16", false, null )]
            [XmlWriterInlineData(1, "Int32", "Int16", false, null )]
            [XmlWriterInlineData(1, "Int16", "Int16", true, null )]
            [XmlWriterInlineData(1, "Byte", "Int16", true, null )]
            [XmlWriterInlineData(1, "SByte", "Int16", true, null )]
            [XmlWriterInlineData(1, "Decimal", "Int16", false, null )]
            [XmlWriterInlineData(1, "float", "Int16", false, null )]
            [XmlWriterInlineData(1, "object", "Int16", true, null )]
            [XmlWriterInlineData(1, "bool", "Int16", false, null )]
            [XmlWriterInlineData(1, "DateTime", "Int16", false, null )]
            [XmlWriterInlineData(1, "DateTimeOffset", "Int16", false, null )]
            [XmlWriterInlineData(1, "ByteArray", "Int16", false, null )]
            [XmlWriterInlineData(1, "List", "Int16", false, null )]
            [XmlWriterInlineData(1, "TimeSpan", "Int16", false, null )]
            [XmlWriterInlineData(1, "Uri", "Int16", false, null )]
            [XmlWriterInlineData(1, "Double", "Int16", false, null )]
            [XmlWriterInlineData(1, "Single", "Int16", false, null )]
            [XmlWriterInlineData(1, "XmlQualifiedName", "Int16", false, null )]
            [XmlWriterInlineData(1, "string", "Int16", true, null )]

            [XmlWriterInlineData(1, "UInt64", "Byte", false, null )]
            [XmlWriterInlineData(1, "UInt32", "Byte", false, null )]
            [XmlWriterInlineData(1, "UInt16", "Byte", false, null )]
            [XmlWriterInlineData(1, "Int64", "Byte", false, null )]
            [XmlWriterInlineData(1, "Int32", "Byte", false, null )]
            [XmlWriterInlineData(1, "Int16", "Byte", false, null )]
            [XmlWriterInlineData(1, "Byte", "Byte", true, null )]
            [XmlWriterInlineData(1, "SByte", "Byte", true, null )]
            [XmlWriterInlineData(1, "Decimal", "Byte", false, null )]
            [XmlWriterInlineData(1, "float", "Byte", false, null )]
            [XmlWriterInlineData(1, "object", "Byte", true, null )]
            [XmlWriterInlineData(1, "bool", "Byte", false, null )]
            [XmlWriterInlineData(1, "DateTime", "Byte", false, null )]
            [XmlWriterInlineData(1, "DateTimeOffset", "Byte", false, null )]
            [XmlWriterInlineData(1, "ByteArray", "Byte", false, null )]
            [XmlWriterInlineData(1, "List", "Byte", false, null )]
            [XmlWriterInlineData(1, "TimeSpan", "Byte", false, null )]
            [XmlWriterInlineData(1, "Uri", "Byte", false, null )]
            [XmlWriterInlineData(1, "Double", "Byte", false, null )]
            [XmlWriterInlineData(1, "Single", "Byte", false, null )]
            [XmlWriterInlineData(1, "XmlQualifiedName", "Byte", false, null )]
            [XmlWriterInlineData(1, "string", "Byte", true, null )]

            [XmlWriterInlineData(1, "UInt64", "SByte", false, null )]
            [XmlWriterInlineData(1, "UInt32", "SByte", false, null )]
            [XmlWriterInlineData(1, "UInt16", "SByte", false, null )]
            [XmlWriterInlineData(1, "Int64", "SByte", false, null )]
            [XmlWriterInlineData(1, "Int32", "SByte", false, null )]
            [XmlWriterInlineData(1, "Int16", "SByte", false, null )]
            [XmlWriterInlineData(1, "Byte", "SByte", false, null )]
            [XmlWriterInlineData(1, "SByte", "SByte", true, null )]
            [XmlWriterInlineData(1, "Decimal", "SByte", false, null )]
            [XmlWriterInlineData(1, "float", "SByte", false, null )]
            [XmlWriterInlineData(1, "object", "SByte", true, null )]
            [XmlWriterInlineData(1, "bool", "SByte", false, null )]
            [XmlWriterInlineData(1, "DateTime", "SByte", false, null )]
            [XmlWriterInlineData(1, "DateTimeOffset", "SByte", false, null )]
            [XmlWriterInlineData(1, "ByteArray", "SByte", false, null )]
            [XmlWriterInlineData(1, "List", "SByte", false, null )]
            [XmlWriterInlineData(1, "TimeSpan", "SByte", false, null )]
            [XmlWriterInlineData(1, "Uri", "SByte", false, null )]
            [XmlWriterInlineData(1, "Double", "SByte", false, null )]
            [XmlWriterInlineData(1, "Single", "SByte", false, null )]
            [XmlWriterInlineData(1, "XmlQualifiedName", "SByte", false, null )]
            [XmlWriterInlineData(1, "string", "SByte", true, null )]

            [XmlWriterInlineData(1, "UInt64", "Decimal", true, null )]
            [XmlWriterInlineData(1, "UInt32", "Decimal", true, null )]
            [XmlWriterInlineData(1, "UInt16", "Decimal", true, null )]
            [XmlWriterInlineData(1, "Int64", "Decimal", true, null )]
            [XmlWriterInlineData(1, "Int32", "Decimal", true, null )]
            [XmlWriterInlineData(1, "Int16", "Decimal", true, null )]
            [XmlWriterInlineData(1, "Byte", "Decimal", true, null )]
            [XmlWriterInlineData(1, "SByte", "Decimal", true, null )]
            [XmlWriterInlineData(1, "Decimal", "Decimal", true, null )]
            [XmlWriterInlineData(1, "float", "Decimal", true, null )]
            [XmlWriterInlineData(1, "object", "Decimal", true, null )]
            [XmlWriterInlineData(1, "bool", "Decimal", false, null )]
            [XmlWriterInlineData(1, "DateTime", "Decimal", false, null )]
            [XmlWriterInlineData(1, "DateTimeOffset", "Decimal", false, null )]
            [XmlWriterInlineData(1, "ByteArray", "Decimal", false, null )]
            [XmlWriterInlineData(1, "List", "Decimal", false, null )]
            [XmlWriterInlineData(1, "TimeSpan", "Decimal", false, null )]
            [XmlWriterInlineData(1, "Uri", "Decimal", false, null )]
            [XmlWriterInlineData(1, "Double", "Decimal", false, null )]
            [XmlWriterInlineData(1, "Single", "Decimal", true, null )]
            [XmlWriterInlineData(1, "XmlQualifiedName", "Decimal", false, null )]
            [XmlWriterInlineData(1, "string", "Decimal", true, null )]

            [XmlWriterInlineData(1, "UInt16", "float", true, null )]
            [XmlWriterInlineData(1, "Int64", "float", true, 9.223372E+18F )]
            [XmlWriterInlineData(1, "Int16", "float", true, null )]
            [XmlWriterInlineData(1, "Byte", "float", true, null )]
            [XmlWriterInlineData(1, "SByte", "float", true, null )]
            [XmlWriterInlineData(1, "float", "float", true, null )]
            [XmlWriterInlineData(1, "object", "float", true, null )]
            [XmlWriterInlineData(1, "bool", "float", false, null )]
            [XmlWriterInlineData(1, "DateTime", "float", false, null )]
            [XmlWriterInlineData(1, "DateTimeOffset", "float", false, null )]
            [XmlWriterInlineData(1, "ByteArray", "float", false, null )]
            [XmlWriterInlineData(1, "List", "float", false, null )]
            [XmlWriterInlineData(1, "TimeSpan", "float", false, null )]
            [XmlWriterInlineData(1, "Uri", "float", false, null )]
            [XmlWriterInlineData(1, "Single", "float", true, null )]
            [XmlWriterInlineData(1, "XmlQualifiedName", "float", false, null )]
            [XmlWriterInlineData(1, "string", "float", true, null )]

            [XmlWriterInlineData(1, "UInt64", "bool", false, null )]
            [XmlWriterInlineData(1, "UInt32", "bool", false, null )]
            [XmlWriterInlineData(1, "UInt16", "bool", false, null )]
            [XmlWriterInlineData(1, "Int64", "bool", false, null )]
            [XmlWriterInlineData(1, "Int32", "bool", false, null )]
            [XmlWriterInlineData(1, "Int16", "bool", false, null )]
            [XmlWriterInlineData(1, "Byte", "bool", false, null )]
            [XmlWriterInlineData(1, "SByte", "bool", false, null )]
            [XmlWriterInlineData(1, "Decimal", "bool", false, null )]
            [XmlWriterInlineData(1, "float", "bool", false, null )]
            [XmlWriterInlineData(1, "object", "bool", true, false )]
            [XmlWriterInlineData(1, "bool", "bool", true, null )]
            [XmlWriterInlineData(1, "DateTime", "bool", false, null )]
            [XmlWriterInlineData(1, "DateTimeOffset", "bool", false, null )]
            [XmlWriterInlineData(1, "ByteArray", "bool", false, null )]
            [XmlWriterInlineData(1, "List", "bool", false, null )]
            [XmlWriterInlineData(1, "TimeSpan", "bool", false, null )]
            [XmlWriterInlineData(1, "Uri", "bool", false, null )]
            [XmlWriterInlineData(1, "Double", "bool", false, null )]
            [XmlWriterInlineData(1, "Single", "bool", false, null )]
            [XmlWriterInlineData(1, "XmlQualifiedName", "bool", false, null )]
            [XmlWriterInlineData(1, "string", "bool", false, null )]

            [XmlWriterInlineData(1, "UInt64", "DateTime", false, null )]
            [XmlWriterInlineData(1, "UInt32", "DateTime", false, null )]
            [XmlWriterInlineData(1, "UInt16", "DateTime", false, null )]
            [XmlWriterInlineData(1, "Int64", "DateTime", false, null )]
            [XmlWriterInlineData(1, "Int32", "DateTime", false, null )]
            [XmlWriterInlineData(1, "Int16", "DateTime", false, null )]
            [XmlWriterInlineData(1, "Byte", "DateTime", false, null )]
            [XmlWriterInlineData(1, "SByte", "DateTime", false, null )]
            [XmlWriterInlineData(1, "Decimal", "DateTime", false, null )]
            [XmlWriterInlineData(1, "float", "DateTime", false, null )]
            [XmlWriterInlineData(1, "object", "DateTime", false, null )]
            [XmlWriterInlineData(1, "bool", "DateTime", false, null )]
            [XmlWriterInlineData(1, "DateTime", "DateTime", true, null )]
            [XmlWriterInlineData(1, "DateTimeOffset", "DateTime", true, null )]
            [XmlWriterInlineData(1, "ByteArray", "DateTime", false, null )]
            [XmlWriterInlineData(1, "List", "DateTime", false, null )]
            [XmlWriterInlineData(1, "TimeSpan", "DateTime", false, null )]
            [XmlWriterInlineData(1, "Uri", "DateTime", false, null )]
            [XmlWriterInlineData(1, "Double", "DateTime", false, null )]
            [XmlWriterInlineData(1, "Single", "DateTime", false, null )]
            [XmlWriterInlineData(1, "XmlQualifiedName", "DateTime", false, null )]
            [XmlWriterInlineData(1, "string", "DateTime", false, null )]

            [XmlWriterInlineData(1, "UInt64", "DateTimeOffset", false, null )]
            [XmlWriterInlineData(1, "UInt32", "DateTimeOffset", false, null )]
            [XmlWriterInlineData(1, "UInt16", "DateTimeOffset", false, null )]
            [XmlWriterInlineData(1, "Int64", "DateTimeOffset", false, null )]
            [XmlWriterInlineData(1, "Int32", "DateTimeOffset", false, null )]
            [XmlWriterInlineData(1, "Int16", "DateTimeOffset", false, null )]
            [XmlWriterInlineData(1, "Byte", "DateTimeOffset", false, null )]
            [XmlWriterInlineData(1, "SByte", "DateTimeOffset", false, null )]
            [XmlWriterInlineData(1, "Decimal", "DateTimeOffset", false, null )]
            [XmlWriterInlineData(1, "float", "DateTimeOffset", false, null )]
            [XmlWriterInlineData(1, "object", "DateTimeOffset", false, null )]
            [XmlWriterInlineData(1, "bool", "DateTimeOffset", false, null )]
            [XmlWriterInlineData(1, "DateTime", "DateTimeOffset", true, 0 )]
            [XmlWriterInlineData(1, "DateTimeOffset", "DateTimeOffset", true, null )]
            [XmlWriterInlineData(1, "ByteArray", "DateTimeOffset", false, null )]
            [XmlWriterInlineData(1, "List", "DateTimeOffset", false, null )]
            [XmlWriterInlineData(1, "TimeSpan", "DateTimeOffset", false, null )]
            [XmlWriterInlineData(1, "Uri", "DateTimeOffset", false, null )]
            [XmlWriterInlineData(1, "Double", "DateTimeOffset", false, null )]
            [XmlWriterInlineData(1, "Single", "DateTimeOffset", false, null )]
            [XmlWriterInlineData(1, "XmlQualifiedName", "DateTimeOffset", false, null )]
            [XmlWriterInlineData(1, "string", "DateTimeOffset", false, null )]

            [XmlWriterInlineData(1, "UInt64", "List", false, null )]
            [XmlWriterInlineData(1, "UInt32", "List", false, null )]
            [XmlWriterInlineData(1, "UInt16", "List", false, null )]
            [XmlWriterInlineData(1, "Int64", "List", false, null )]
            [XmlWriterInlineData(1, "Int32", "List", false, null )]
            [XmlWriterInlineData(1, "Int16", "List", false, null )]
            [XmlWriterInlineData(1, "Byte", "List", false, null )]
            [XmlWriterInlineData(1, "SByte", "List", false, null )]
            [XmlWriterInlineData(1, "Decimal", "List", false, null )]
            [XmlWriterInlineData(1, "float", "List", false, null )]
            [XmlWriterInlineData(1, "object", "List", false, null )]
            [XmlWriterInlineData(1, "bool", "List", false, null )]
            [XmlWriterInlineData(1, "DateTime", "List", false, null )]
            [XmlWriterInlineData(1, "DateTimeOffset", "List", false, null )]
            [XmlWriterInlineData(1, "ByteArray", "List", false, null )]
            [XmlWriterInlineData(1, "List", "List", false, null )]
            [XmlWriterInlineData(1, "TimeSpan", "List", false, null )]
            [XmlWriterInlineData(1, "Uri", "List", false, null )]
            [XmlWriterInlineData(1, "Double", "List", false, null )]
            [XmlWriterInlineData(1, "Single", "List", false, null )]
            [XmlWriterInlineData(1, "XmlQualifiedName", "List", false, null )]
            [XmlWriterInlineData(1, "string", "List", false, null )]

            [XmlWriterInlineData(1, "UInt64", "Uri", true, null )]
            [XmlWriterInlineData(1, "UInt32", "Uri", true, null )]
            [XmlWriterInlineData(1, "UInt16", "Uri", true, null )]
            [XmlWriterInlineData(1, "Int64", "Uri", true, null )]
            [XmlWriterInlineData(1, "Int32", "Uri", true, null )]
            [XmlWriterInlineData(1, "Int16", "Uri", true, null )]
            [XmlWriterInlineData(1, "Byte", "Uri", true, null )]
            [XmlWriterInlineData(1, "SByte", "Uri", true, null )]
            [XmlWriterInlineData(1, "Decimal", "Uri", true, null )]
            [XmlWriterInlineData(1, "float", "Uri", true, null )]
            [XmlWriterInlineData(1, "object", "Uri", true, null )]
            [XmlWriterInlineData(1, "bool", "Uri", true, "false" )]
            [XmlWriterInlineData(1, "DateTime", "Uri", true, 1 )]
            [XmlWriterInlineData(1, "DateTimeOffset", "Uri", true, 2 )]
            [XmlWriterInlineData(1, "ByteArray", "Uri", true, "2H4=" )]
            [XmlWriterInlineData(1, "List", "Uri", true, "" )]
            [XmlWriterInlineData(1, "TimeSpan", "Uri", true, "PT0S" )]
            [XmlWriterInlineData(1, "Uri", "Uri", true, null )]
            [XmlWriterInlineData(1, "Double", "Uri", true, "1.7976931348623157E+308" )]
            [XmlWriterInlineData(1, "Single", "Uri", true, null )]
            [XmlWriterInlineData(1, "XmlQualifiedName", "Uri", true, null )]
            [XmlWriterInlineData(1, "string", "Uri", true, null )]

            [XmlWriterInlineData(1, "UInt32", "Double", true, null )]
            [XmlWriterInlineData(1, "UInt16", "Double", true, null )]
            [XmlWriterInlineData(1, "Int32", "Double", true, null )]
            [XmlWriterInlineData(1, "Int16", "Double", true, null )]
            [XmlWriterInlineData(1, "Byte", "Double", true, null )]
            [XmlWriterInlineData(1, "SByte", "Double", true, null )]
            [XmlWriterInlineData(1, "float", "Double", true, null )]
            [XmlWriterInlineData(1, "object", "Double", true, null )]
            [XmlWriterInlineData(1, "bool", "Double", false, null )]
            [XmlWriterInlineData(1, "DateTime", "Double", false, null )]
            [XmlWriterInlineData(1, "DateTimeOffset", "Double", false, null )]
            [XmlWriterInlineData(1, "ByteArray", "Double", false, null )]
            [XmlWriterInlineData(1, "List", "Double", false, null )]
            [XmlWriterInlineData(1, "TimeSpan", "Double", false, null )]
            [XmlWriterInlineData(1, "Uri", "Double", false, null )]
            [XmlWriterInlineData(1, "Double", "Double", true, null )]
            [XmlWriterInlineData(1, "Single", "Double", true, null )]
            [XmlWriterInlineData(1, "XmlQualifiedName", "Double", false, null )]
            [XmlWriterInlineData(1, "string", "Double", true, null )]

            [XmlWriterInlineData(1, "UInt16", "Single", true, null )]
            [XmlWriterInlineData(1, "Int64", "Single", true, 9.223372E+18F )]
            [XmlWriterInlineData(1, "Int16", "Single", true, null )]
            [XmlWriterInlineData(1, "Byte", "Single", true, null )]
            [XmlWriterInlineData(1, "SByte", "Single", true, null )]
            [XmlWriterInlineData(1, "float", "Single", true, null )]
            [XmlWriterInlineData(1, "object", "Single", true, null )]
            [XmlWriterInlineData(1, "bool", "Single", false, null )]
            [XmlWriterInlineData(1, "DateTime", "Single", false, null )]
            [XmlWriterInlineData(1, "DateTimeOffset", "Single", false, null )]
            [XmlWriterInlineData(1, "ByteArray", "Single", false, null )]
            [XmlWriterInlineData(1, "List", "Single", false, null )]
            [XmlWriterInlineData(1, "TimeSpan", "Single", false, null )]
            [XmlWriterInlineData(1, "Uri", "Single", false, null )]
            [XmlWriterInlineData(1, "Single", "Single", true, null )]
            [XmlWriterInlineData(1, "XmlQualifiedName", "Single", false, null )]
            [XmlWriterInlineData(1, "string", "Single", true, null )]

            [XmlWriterInlineData(1, "UInt64", "object", true, null )]
            [XmlWriterInlineData(1, "UInt32", "object", true, null )]
            [XmlWriterInlineData(1, "UInt16", "object", true, null )]
            [XmlWriterInlineData(1, "Int64", "object", true, null )]
            [XmlWriterInlineData(1, "Int32", "object", true, null )]
            [XmlWriterInlineData(1, "Int16", "object", true, null )]
            [XmlWriterInlineData(1, "Byte", "object", true, null )]
            [XmlWriterInlineData(1, "SByte", "object", true, null )]
            [XmlWriterInlineData(1, "Decimal", "object", true, null )]
            [XmlWriterInlineData(1, "float", "object", true, null )]
            [XmlWriterInlineData(1, "object", "object", true, null )]
            [XmlWriterInlineData(1, "bool", "object", true, "false" )]
            [XmlWriterInlineData(1, "DateTime", "object", true, 1 )]
            [XmlWriterInlineData(1, "DateTimeOffset", "object", true, 2 )]
            [XmlWriterInlineData(1, "ByteArray", "object", true, "2H4=" )]
            [XmlWriterInlineData(1, "List", "object", true, "" )]
            [XmlWriterInlineData(1, "TimeSpan", "object", true, "PT0S" )]
            [XmlWriterInlineData(1, "Uri", "object", true, null )]
            [XmlWriterInlineData(1, "Double", "object", true, "1.7976931348623157E+308" )]
            [XmlWriterInlineData(1, "Single", "object", true, null )]
            [XmlWriterInlineData(1, "XmlQualifiedName", "object", true, null )]
            [XmlWriterInlineData(1, "string", "object", true, null )]

            [XmlWriterInlineData(1, "ByteArray", "ByteArray", true, null )]
            [XmlWriterInlineData(1, "BoolArray", "BoolArray", true, null )]
            [XmlWriterInlineData(1, "ObjectArray", "ObjectArray", true, null )]
            [XmlWriterInlineData(1, "DateTimeArray", "DateTimeArray", true, null )]
            [XmlWriterInlineData(1, "DateTimeOffsetArray", "DateTimeOffsetArray", true, null )]
            [XmlWriterInlineData(1, "DecimalArray", "DecimalArray", true, null )]
            [XmlWriterInlineData(1, "DoubleArray", "DoubleArray", true, null )]
            [XmlWriterInlineData(1, "Int16Array", "Int16Array", true, null )]
            [XmlWriterInlineData(1, "Int32Array", "Int32Array", true, null )]
            [XmlWriterInlineData(1, "Int64Array", "Int64Array", true, null )]
            [XmlWriterInlineData(1, "SByteArray", "SByteArray", true, null )]
            [XmlWriterInlineData(1, "SingleArray", "SingleArray", true, null )]
            [XmlWriterInlineData(1, "StringArray", "StringArray", true, null )]
            [XmlWriterInlineData(1, "TimeSpanArray", "TimeSpanArray", true, null )]
            [XmlWriterInlineData(1, "UInt16Array", "UInt16Array", true, null )]
            [XmlWriterInlineData(1, "UInt32Array", "UInt32Array", true, null )]
            [XmlWriterInlineData(1, "UInt64Array", "UInt64Array", true, null )]
            [XmlWriterInlineData(1, "UriArray", "UriArray", true, null )]
            [XmlWriterInlineData(1, "XmlQualifiedNameArray", "XmlQualifiedNameArray", true, null )]
            [XmlWriterInlineData(1, "TimeSpan", "TimeSpan", true, null )]
            [XmlWriterInlineData(1, "XmlQualifiedName", "XmlQualifiedName", true, null )]

            //////////attr
            [XmlWriterInlineData(2, "Int16", "string", true, null )]
            [XmlWriterInlineData(2, "Byte", "string", true, null )]
            [XmlWriterInlineData(2, "SByte", "string", true, null )]
            [XmlWriterInlineData(2, "Decimal", "string", true, null )]
            [XmlWriterInlineData(2, "float", "string", true, null )]
            [XmlWriterInlineData(2, "object", "string", true, null )]
            [XmlWriterInlineData(2, "bool", "string", true, "False" )]
            [XmlWriterInlineData(2, "Uri", "string", true, null )]
            [XmlWriterInlineData(2, "Double", "string", true, null )]
            [XmlWriterInlineData(2, "Single", "string", true, null )]
            [XmlWriterInlineData(2, "XmlQualifiedName", "string", true, null )]
            [XmlWriterInlineData(2, "string", "string", true, null )]

            [XmlWriterInlineData(2, "UInt64", "UInt64", true, null )]
            [XmlWriterInlineData(2, "UInt32", "UInt64", true, null )]
            [XmlWriterInlineData(2, "UInt16", "UInt64", true, null )]
            [XmlWriterInlineData(2, "Int64", "UInt64", true, null )]
            [XmlWriterInlineData(2, "Int32", "UInt64", true, null )]
            [XmlWriterInlineData(2, "Int16", "UInt64", true, null )]
            [XmlWriterInlineData(2, "List", "UInt64", false, null )]
            [XmlWriterInlineData(2, "TimeSpan", "UInt64", false, null )]
            [XmlWriterInlineData(2, "Uri", "UInt64", false, null )]
            [XmlWriterInlineData(2, "Double", "UInt64", false, null )]
            [XmlWriterInlineData(2, "Single", "UInt64", false, null )]
            [XmlWriterInlineData(2, "XmlQualifiedName", "UInt64", false, null )]
            [XmlWriterInlineData(2, "string", "UInt64", true, null )]

            [XmlWriterInlineData(2, "UInt64", "Int64", false, null )]
            [XmlWriterInlineData(2, "UInt32", "Int64", true, null )]
            [XmlWriterInlineData(2, "UInt16", "Int64", true, null )]
            [XmlWriterInlineData(2, "Int64", "Int64", true, null )]
            [XmlWriterInlineData(2, "Int32", "Int64", true, null )]
            [XmlWriterInlineData(2, "Int16", "Int64", true, null )]
            [XmlWriterInlineData(2, "Byte", "Int64", true, null )]
            [XmlWriterInlineData(2, "TimeSpan", "Int64", false, null )]
            [XmlWriterInlineData(2, "Uri", "Int64", false, null )]
            [XmlWriterInlineData(2, "Double", "Int64", false, null )]
            [XmlWriterInlineData(2, "Single", "Int64", false, null )]
            [XmlWriterInlineData(2, "XmlQualifiedName", "Int64", false, null )]
            [XmlWriterInlineData(2, "string", "Int64", true, null )]

            [XmlWriterInlineData(2, "UInt64", "UInt32", false, null )]
            [XmlWriterInlineData(2, "UInt32", "UInt32", true, null )]
            [XmlWriterInlineData(2, "UInt16", "UInt32", true, null )]
            [XmlWriterInlineData(2, "Int64", "UInt32", false, null )]
            [XmlWriterInlineData(2, "Int32", "UInt32", true, null )]
            [XmlWriterInlineData(2, "Int16", "UInt32", true, null )]
            [XmlWriterInlineData(2, "Byte", "UInt32", true, null )]
            [XmlWriterInlineData(2, "SByte", "UInt32", true, null )]
            [XmlWriterInlineData(2, "string", "UInt32", true, null )]

            [XmlWriterInlineData(2, "UInt64", "Int32", false, null )]
            [XmlWriterInlineData(2, "UInt32", "Int32", false, null )]
            [XmlWriterInlineData(2, "UInt16", "Int32", true, null )]
            [XmlWriterInlineData(2, "Int64", "Int32", false, null )]
            [XmlWriterInlineData(2, "Int32", "Int32", true, null )]
            [XmlWriterInlineData(2, "Int16", "Int32", true, null )]
            [XmlWriterInlineData(2, "Byte", "Int32", true, null )]
            [XmlWriterInlineData(2, "SByte", "Int32", true, null )]
            [XmlWriterInlineData(2, "Single", "Int32", false, null )]
            [XmlWriterInlineData(2, "XmlQualifiedName", "Int32", false, null )]
            [XmlWriterInlineData(2, "string", "Int32", true, null )]

            [XmlWriterInlineData(2, "UInt64", "UInt16", false, null )]
            [XmlWriterInlineData(2, "UInt32", "UInt16", false, null )]
            [XmlWriterInlineData(2, "UInt16", "UInt16", true, null )]
            [XmlWriterInlineData(2, "Int64", "UInt16", false, null )]
            [XmlWriterInlineData(2, "Int32", "UInt16", false, null )]
            [XmlWriterInlineData(2, "Int16", "UInt16", true, null )]
            [XmlWriterInlineData(2, "Byte", "UInt16", true, null )]
            [XmlWriterInlineData(2, "SByte", "UInt16", true, null )]
            [XmlWriterInlineData(2, "Decimal", "UInt16", false, null )]
            [XmlWriterInlineData(2, "float", "UInt16", false, null )]
            [XmlWriterInlineData(2, "object", "UInt16", true, null )]
            [XmlWriterInlineData(2, "bool", "UInt16", false, null )]
            [XmlWriterInlineData(2, "string", "UInt16", true, null )]

            [XmlWriterInlineData(2, "UInt64", "Int16", false, null )]
            [XmlWriterInlineData(2, "UInt32", "Int16", false, null )]
            [XmlWriterInlineData(2, "UInt16", "Int16", false, null )]
            [XmlWriterInlineData(2, "Int64", "Int16", false, null )]
            [XmlWriterInlineData(2, "Int32", "Int16", false, null )]
            [XmlWriterInlineData(2, "Int16", "Int16", true, null )]
            [XmlWriterInlineData(2, "Byte", "Int16", true, null )]
            [XmlWriterInlineData(2, "SByte", "Int16", true, null )]
            [XmlWriterInlineData(2, "Decimal", "Int16", false, null )]
            [XmlWriterInlineData(2, "float", "Int16", false, null )]
            [XmlWriterInlineData(2, "object", "Int16", true, null )]
            [XmlWriterInlineData(2, "bool", "Int16", false, null )]
            [XmlWriterInlineData(2, "DateTime", "Int16", false, null )]
            [XmlWriterInlineData(2, "string", "Int16", true, null )]

            [XmlWriterInlineData(2, "UInt64", "Byte", false, null )]
            [XmlWriterInlineData(2, "UInt32", "Byte", false, null )]
            [XmlWriterInlineData(2, "UInt16", "Byte", false, null )]
            [XmlWriterInlineData(2, "Int64", "Byte", false, null )]
            [XmlWriterInlineData(2, "Int32", "Byte", false, null )]
            [XmlWriterInlineData(2, "Int16", "Byte", false, null )]
            [XmlWriterInlineData(2, "Byte", "Byte", true, null )]
            [XmlWriterInlineData(2, "SByte", "Byte", true, null )]
            [XmlWriterInlineData(2, "string", "Byte", true, null )]

            [XmlWriterInlineData(2, "UInt64", "SByte", false, null )]
            [XmlWriterInlineData(2, "UInt32", "SByte", false, null )]
            [XmlWriterInlineData(2, "UInt16", "SByte", false, null )]
            [XmlWriterInlineData(2, "Int64", "SByte", false, null )]
            [XmlWriterInlineData(2, "Int32", "SByte", false, null )]
            [XmlWriterInlineData(2, "Uri", "SByte", false, null )]
            [XmlWriterInlineData(2, "Double", "SByte", false, null )]
            [XmlWriterInlineData(2, "Single", "SByte", false, null )]
            [XmlWriterInlineData(2, "XmlQualifiedName", "SByte", false, null )]
            [XmlWriterInlineData(2, "string", "SByte", true, null )]

            [XmlWriterInlineData(2, "UInt64", "Decimal", true, null )]
            [XmlWriterInlineData(2, "UInt32", "Decimal", true, null )]
            [XmlWriterInlineData(2, "UInt16", "Decimal", true, null )]
            [XmlWriterInlineData(2, "Int64", "Decimal", true, null )]
            [XmlWriterInlineData(2, "Int32", "Decimal", true, null )]
            [XmlWriterInlineData(2, "Int16", "Decimal", true, null )]
            [XmlWriterInlineData(2, "Byte", "Decimal", true, null )]
            [XmlWriterInlineData(2, "SByte", "Decimal", true, null )]
            [XmlWriterInlineData(2, "Decimal", "Decimal", true, null )]
            [XmlWriterInlineData(2, "float", "Decimal", true, null )]
            [XmlWriterInlineData(2, "object", "Decimal", true, null )]
            [XmlWriterInlineData(21, "XmlQualifiedName", "Decimal", false, null )]
            [XmlWriterInlineData(2, "string", "Decimal", true, null )]

            [XmlWriterInlineData(2, "UInt16", "float", true, null )]
            [XmlWriterInlineData(2, "Int64", "float", true, 9.223372E+18F )]
            [XmlWriterInlineData(2, "Int16", "float", true, null )]
            [XmlWriterInlineData(2, "Byte", "float", true, null )]
            [XmlWriterInlineData(2, "SByte", "float", true, null )]
            [XmlWriterInlineData(2, "float", "float", true, null )]
            [XmlWriterInlineData(2, "object", "float", true, null )]
            [XmlWriterInlineData(2, "bool", "float", false, null )]
            [XmlWriterInlineData(2, "Single", "float", true, null )]
            [XmlWriterInlineData(2, "XmlQualifiedName", "float", false, null )]
            [XmlWriterInlineData(2, "string", "float", true, null )]

            [XmlWriterInlineData(2, "UInt64", "bool", false, null )]
            [XmlWriterInlineData(2, "UInt32", "bool", false, null )]
            [XmlWriterInlineData(2, "object", "bool", true, false )]
            [XmlWriterInlineData(2, "DateTime", "bool", false, null )]
            [XmlWriterInlineData(2, "DateTimeOffset", "bool", false, null )]
            [XmlWriterInlineData(2, "ByteArray", "bool", false, null )]
            [XmlWriterInlineData(2, "List", "bool", false, null )]
            [XmlWriterInlineData(2, "TimeSpan", "bool", false, null )]
            [XmlWriterInlineData(2, "Uri", "bool", false, null )]
            [XmlWriterInlineData(2, "Double", "bool", false, null )]
            [XmlWriterInlineData(2, "Single", "bool", false, null )]

            [XmlWriterInlineData(2, "float", "DateTime", false, null )]
            [XmlWriterInlineData(2, "object", "DateTime", false, null )]
            [XmlWriterInlineData(2, "bool", "DateTime", false, null )]
            [XmlWriterInlineData(2, "ByteArray", "DateTime", false, null )]
            [XmlWriterInlineData(2, "List", "DateTime", false, null )]
            [XmlWriterInlineData(2, "Uri", "DateTime", false, null )]
            [XmlWriterInlineData(2, "Double", "DateTime", false, null )]
            [XmlWriterInlineData(2, "Single", "DateTime", false, null )]
            [XmlWriterInlineData(2, "XmlQualifiedName", "DateTime", false, null )]
            [XmlWriterInlineData(2, "string", "DateTime", false, null )]

            [XmlWriterInlineData(2, "UInt64", "DateTimeOffset", false, null )]
            [XmlWriterInlineData(2, "UInt32", "DateTimeOffset", false, null )]
            [XmlWriterInlineData(2, "UInt16", "DateTimeOffset", false, null )]
            [XmlWriterInlineData(2, "Int64", "DateTimeOffset", false, null )]
            [XmlWriterInlineData(2, "Int32", "DateTimeOffset", false, null )]
            [XmlWriterInlineData(2, "Int16", "DateTimeOffset", false, null )]
            [XmlWriterInlineData(2, "Byte", "DateTimeOffset", false, null )]
            [XmlWriterInlineData(2, "SByte", "DateTimeOffset", false, null )]
            [XmlWriterInlineData(2, "Decimal", "DateTimeOffset", false, null )]

            [XmlWriterInlineData(2, "UInt64", "List", false, null )]
            [XmlWriterInlineData(2, "UInt32", "List", false, null )]
            [XmlWriterInlineData(2, "UInt16", "List", false, null )]
            [XmlWriterInlineData(2, "Int64", "List", false, null )]
            [XmlWriterInlineData(2, "Int32", "List", false, null )]
            [XmlWriterInlineData(2, "Int16", "List", false, null )]
            [XmlWriterInlineData(2, "Byte", "List", false, null )]
            [XmlWriterInlineData(2, "SByte", "List", false, null )]
            [XmlWriterInlineData(2, "Decimal", "List", false, null )]
            [XmlWriterInlineData(2, "float", "List", false, null )]

            [XmlWriterInlineData(2, "UInt64", "Uri", true, null )]
            [XmlWriterInlineData(2, "UInt32", "Uri", true, null )]
            [XmlWriterInlineData(2, "UInt16", "Uri", true, null )]
            [XmlWriterInlineData(2, "Int64", "Uri", true, null )]
            [XmlWriterInlineData(2, "Int32", "Uri", true, null )]
            [XmlWriterInlineData(2, "Int16", "Uri", true, null )]
            [XmlWriterInlineData(2, "Byte", "Uri", true, null )]
            [XmlWriterInlineData(2, "SByte", "Uri", true, null )]
            [XmlWriterInlineData(2, "Decimal", "Uri", true, null )]
            [XmlWriterInlineData(2, "float", "Uri", true, null )]
            [XmlWriterInlineData(2, "object", "Uri", true, null )]
            [XmlWriterInlineData(2, "bool", "Uri", true, "False" )]
            [XmlWriterInlineData(2, "Uri", "Uri", true, null )]
            [XmlWriterInlineData(2, "Double", "Uri", true, null )]
            [XmlWriterInlineData(2, "Single", "Uri", true, null )]
            [XmlWriterInlineData(2, "XmlQualifiedName", "Uri", true, null )]
            [XmlWriterInlineData(2, "string", "Uri", true, null )]

            [XmlWriterInlineData(2, "UInt32", "Double", true, null )]
            [XmlWriterInlineData(2, "UInt16", "Double", true, null )]
            [XmlWriterInlineData(2, "Int32", "Double", true, null )]
            [XmlWriterInlineData(2, "Int16", "Double", true, null )]
            [XmlWriterInlineData(2, "Byte", "Double", true, null )]
            [XmlWriterInlineData(2, "SByte", "Double", true, null )]
            [XmlWriterInlineData(2, "float", "Double", true, null )]
            [XmlWriterInlineData(2, "object", "Double", true, null )]
            [XmlWriterInlineData(2, "bool", "Double", false, null )]
            [XmlWriterInlineData(2, "Single", "Double", true, null )]
            [XmlWriterInlineData(2, "string", "Double", true, null )]

            [XmlWriterInlineData(2, "UInt16", "Single", true, null )]
            [XmlWriterInlineData(2, "Int64", "Single", true, 9.223372E+18F )]
            [XmlWriterInlineData(2, "Int16", "Single", true, null )]
            [XmlWriterInlineData(2, "Byte", "Single", true, null )]
            [XmlWriterInlineData(2, "SByte", "Single", true, null )]
            [XmlWriterInlineData(2, "float", "Single", true, null )]
            [XmlWriterInlineData(2, "object", "Single", true, null )]
            [XmlWriterInlineData(2, "bool", "Single", false, null )]
            [XmlWriterInlineData(2, "DateTimeOffset", "Single", false, null )]
            [XmlWriterInlineData(2, "Single", "Single", true, null )]
            [XmlWriterInlineData(2, "string", "Single", true, null )]

            [XmlWriterInlineData(2, "UInt64", "object", true, null )]
            [XmlWriterInlineData(2, "Int32", "object", true, null )]
            [XmlWriterInlineData(2, "Int16", "object", true, null )]
            [XmlWriterInlineData(2, "Byte", "object", true, null )]
            [XmlWriterInlineData(2, "SByte", "object", true, null )]
            [XmlWriterInlineData(2, "Decimal", "object", true, null )]
            [XmlWriterInlineData(2, "float", "object", true, null )]
            [XmlWriterInlineData(2, "object", "object", true, null )]
            [XmlWriterInlineData(2, "bool", "object", true, "False" )]
            [XmlWriterInlineData(2, "XmlQualifiedName", "object", true, null )]
            [XmlWriterInlineData(2, "string", "object", true, null )]
            [XmlWriterInlineData(2, "ObjectArray", "ObjectArray", true, null )]
            [XmlWriterInlineData(2, "StringArray", "StringArray", true, null )]
            [XmlWriterInlineData(2, "UriArray", "UriArray", true, null )]
            [XmlWriterInlineData(2, "XmlQualifiedName", "XmlQualifiedName", true, null )]
            public void writeValue_27(XmlWriterUtils utils, int param, string sourceStr, string destStr, bool isValid, object expVal)
            {
                Type dest = typeMapper[destStr];
                CultureInfo origCulture = null;

                if (expVal == null && destStr.Contains("DateTime"))
                    expVal = value[destStr];
                else if (expVal != null && sourceStr.Contains("DateTime"))
                    expVal = _dates[(int)expVal];
                else if (sourceStr.Equals("XmlQualifiedName") && (utils.WriterType == WriterType.CustomWriter) && param == 1)
                    expVal = "{}a";
                else if (expVal == null)
                    expVal = value[sourceStr];

                using (XmlWriter w = utils.CreateWriter())
                {
                    w.WriteStartElement("Root");
                    if (param == 1)
                        w.WriteValue(value[sourceStr]);
                    else
                        w.WriteAttributeString("a",
                            string.Format(CultureInfo.InvariantCulture, "{0}", value[sourceStr]));
                    w.WriteEndElement();
                }
                try
                {
                    origCulture = CultureInfo.CurrentCulture;
                    CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;  // So that the number format doesn't depend on the current culture
                    VerifyValue(dest, expVal, param);
                }
                catch (XmlException)
                {
                    if (!isValid || (utils.WriterType == WriterType.CustomWriter) && sourceStr.Contains("XmlQualifiedName"))
                        return;
                    CError.Compare(false, "XmlException");
                }
                catch (OverflowException)
                {
                    if (!isValid)
                        return;
                    CError.Compare(false, "OverflowException");
                }
                catch (FormatException)
                {
                    if (!isValid)
                        return;
                    CError.Compare(false, "FormatException");
                }
                catch (ArgumentOutOfRangeException)
                {
                    if (!isValid)
                        return;
                    CError.Compare(false, "ArgumentOutOfRangeException");
                }
                catch (InvalidCastException)
                {
                    if (!isValid)
                        return;
                    CError.Compare(false, "ArgumentException");
                }
                finally
                {
                    CultureInfo.CurrentCulture = origCulture;
                }
                Assert.True((isValid));
            }

            [Theory]
            [XmlWriterInlineData(1, "Double", "float", true, float.PositiveInfinity)]
            [XmlWriterInlineData(1, "Double", "Single", true, float.PositiveInfinity)]
            [XmlWriterInlineData(2, "Double", "Double", true, 1.7976931348623157E+308)]

            [XmlWriterInlineData(1, "UInt64", "float", true, 1.8446744E+19F)]
            [XmlWriterInlineData(1, "UInt32", "float", true, 4.2949673E+09F)]
            [XmlWriterInlineData(1, "Int32", "float", true, 2.1474836E+09F)]
            [XmlWriterInlineData(1, "Decimal", "float", true, 7.9228163E+28F)]

            [XmlWriterInlineData(1, "UInt64", "Double", true, 1.8446744073709552E+19D)]
            [XmlWriterInlineData(1, "Int64", "Double", true, 9.223372036854776E+18D)]
            [XmlWriterInlineData(1, "Decimal", "Double", true, 7.922816251426434E+28D)]

            [XmlWriterInlineData(1, "UInt64", "Single", true, 1.8446744E+19F)]
            [XmlWriterInlineData(1, "UInt32", "Single", true, 4.2949673E+09F)]
            [XmlWriterInlineData(1, "Int32", "Single", true, 2.1474836E+09F)]
            [XmlWriterInlineData(1, "Decimal", "Single", true, 7.9228163E+28F)]

            [XmlWriterInlineData(2, "UInt64", "float", true, 1.8446744E+19F)]
            [XmlWriterInlineData(2, "UInt32", "float", true, 4.2949673E+09F)]
            [XmlWriterInlineData(2, "Int32", "float", true, 2.1474836E+09F)]
            [XmlWriterInlineData(2, "Decimal", "float", true, 7.9228163E+28F)]

            [XmlWriterInlineData(2, "UInt64", "Double", true, 1.8446744073709552E+19D)]
            [XmlWriterInlineData(2, "Int64", "Double", true, 9.223372036854776E+18D)]
            [XmlWriterInlineData(2, "Decimal", "Double", true, 7.922816251426434E+28D)]
            [XmlWriterInlineData(2, "UInt64", "Single", true, 1.8446744E+19F)]
            [XmlWriterInlineData(2, "UInt32", "Single", true, 4.2949673E+09F)]
            [XmlWriterInlineData(2, "Int32", "Single", true, 2.1474836E+09F)]
            [XmlWriterInlineData(2, "Decimal", "Single", true, 7.9228163E+28F)]
            public void writeValue_27_NotNetFramework(XmlWriterUtils utils, int param, string sourceStr, string destStr, bool isValid, object expVal)
            {
                writeValue_27(utils, param, sourceStr, destStr, isValid, expVal);
            }

            [Theory]
            [XmlWriterInlineData(1)]
            [XmlWriterInlineData(2)]
            [XmlWriterInlineData(3)]
            [XmlWriterInlineData(4)]
            [XmlWriterInlineData(6)]
            [XmlWriterInlineData(7)]
            [XmlWriterInlineData(9)]
            public void writeValue_28(XmlWriterUtils utils, int param)
            {
                Tuple<int, string, double> t = Tuple.Create(1, "Melitta", 7.5);

                using (XmlWriter w = utils.CreateWriter())
                {
                    w.WriteStartElement("Root");
                    try
                    {
                        switch (param)
                        {
                            case 1:
                                w.WriteValue(new XmlException());
                                break;
                            case 2:
                                w.WriteValue(DayOfWeek.Friday);
                                break;
                            case 3:
                                w.WriteValue(new XmlQualifiedName("b", "c"));
                                break;
                            case 4:
                                w.WriteValue(new Guid());
                                break;
                            case 6:
                                w.WriteValue(NewLineHandling.Entitize);
                                break;
                            case 7:
                                w.WriteValue(ConformanceLevel.Auto);
                                break;
                            case 9:
                                w.WriteValue(t);
                                break;
                            default:
                                Assert.Fail("invalid param");
                                break;
                        }
                    }
                    catch (InvalidCastException e)
                    {
                        CError.WriteLine(e.Message);
                        try
                        {
                            switch (param)
                            {
                                case 1:
                                    w.WriteValue(new XmlException());
                                    break;
                                case 2:
                                    w.WriteValue(DayOfWeek.Friday);
                                    break;
                                case 3:
                                    w.WriteValue(new XmlQualifiedName("b", "c"));
                                    break;
                                case 4:
                                    w.WriteValue(new Guid());
                                    break;
                                case 6:
                                    w.WriteValue(NewLineHandling.Entitize);
                                    break;
                                case 7:
                                    w.WriteValue(ConformanceLevel.Auto);
                                    break;
                                case 9:
                                    w.WriteValue(t);
                                    break;
                            }
                        }
                        catch (InvalidOperationException) { return; }
                        catch (InvalidCastException) { return; }
                    }
                }
                Assert.True(param == 3 && (utils.WriterType == WriterType.CustomWriter));
            }

            [Theory]
            [XmlWriterInlineData(1)]
            [XmlWriterInlineData(2)]
            public void writeValue_30(XmlWriterUtils utils, int param)
            {
                using (XmlWriter w = utils.CreateWriter())
                {
                    w.WriteStartElement("Root");
                    if (param == 1)
                        w.WriteValue("p:foo");
                    else
                        w.WriteAttributeString("a", "p:foo");
                    w.WriteEndElement();
                }
                try
                {
                    VerifyValue(typeof(XmlQualifiedName), "p:foo", param);
                }
                catch (XmlException) { return; }
                catch (InvalidOperationException) { return; }
                Assert.Fail();
            }

            [Theory]
            [XmlWriterInlineData(WriterType.AllButCustom, "2002-12-30T00:00:00-08:00", "<Root>2002-12-30T00:00:00-08:00</Root>" )]
            [XmlWriterInlineData(WriterType.AllButCustom, "2000-02-29T23:59:59.999999999999-13:60", "<Root>2000-03-01T00:00:00-14:00</Root>" )]
            [XmlWriterInlineData(WriterType.AllButCustom, "0001-01-01T00:00:00+00:00", "<Root>0001-01-01T00:00:00Z</Root>" )]
            [XmlWriterInlineData(WriterType.AllButCustom, "0001-01-01T00:00:00.9999999-14:00", "<Root>0001-01-01T00:00:00.9999999-14:00</Root>" )]
            [XmlWriterInlineData(WriterType.AllButCustom, "9999-12-31T12:59:59.9999999+14:00", "<Root>9999-12-31T12:59:59.9999999+14:00</Root>" )]
            [XmlWriterInlineData(WriterType.AllButCustom, "9999-12-31T12:59:59-11:00", "<Root>9999-12-31T12:59:59-11:00</Root>" )]
            [XmlWriterInlineData(WriterType.AllButCustom, "2000-02-29T23:59:59.999999999999+13:60", "<Root>2000-03-01T00:00:00+14:00</Root>" )]
            public void writeValue_31(XmlWriterUtils utils, string value, string expectedValue)
            {
                DateTimeOffset a = XmlConvert.ToDateTimeOffset(value);
                using (XmlWriter w = utils.CreateWriter())
                {
                    w.WriteStartElement("Root");
                    w.WriteValue(XmlConvert.ToDateTimeOffset(value));
                    w.WriteEndElement();
                }
                Assert.True((utils.CompareReader(expectedValue)));
            }

            // WriteValue(new DateTimeOffset) - valid
            [Theory]
            [XmlWriterInlineData(WriterType.AllButCustom)]
            public void writeValue_32(XmlWriterUtils utils)
            {
                DateTimeOffset actual;
                string expect;
                bool isPassed = true;
                object[] actualArray =
                {
                    new DateTimeOffset(2002,2,1,0,0,0,TimeSpan.FromHours(-8.0)),
                    new DateTimeOffset(9999,1,1,0,0,0,TimeSpan.FromHours(-8.0)),
                    new DateTimeOffset(9999,1,1,0,0,0,TimeSpan.FromHours(0)),
                    new DateTimeOffset(9999,12,31,12,59,59,TimeSpan.FromHours(-11.0)),
                    new DateTimeOffset(9999,12,31,12,59,59,TimeSpan.FromHours(-10) + TimeSpan.FromMinutes(-59)),
                    new DateTimeOffset(9999,12,31,12,59,59,new TimeSpan(13,59,0)),
                    new DateTimeOffset(9999,12,31,23,59,59,TimeSpan.FromHours(0)),
                    new DateTimeOffset(9999,12,31,23,59,59, new TimeSpan(14,0,0)),
                    new DateTimeOffset(9999,12,31,23,59,59, new TimeSpan(13,60,0)),
                    new DateTimeOffset(9999,12,31,23,59,59, new TimeSpan(13,59,60)),
                    new DateTimeOffset(9998,12,31,12,59,59, new TimeSpan(13,60,0)),
                    new DateTimeOffset(9998,12,31,12,59,59,TimeSpan.FromHours(-14.0)),
                    new DateTimeOffset(1,1,1,0,0,0,TimeSpan.FromHours(-8.0)),
                    new DateTimeOffset(1,1,1,0,0,0,TimeSpan.FromHours(-14.0)),
                    new DateTimeOffset(1,1,1,0,0,0,TimeSpan.FromHours(-13) + TimeSpan.FromMinutes(-59)),
                    new DateTimeOffset(1,1,1,0,0,0,TimeSpan.Zero),
                };
                object[] expectArray =
                {
                    "<Root>2002-02-01T00:00:00-08:00</Root>",
                    "<Root>9999-01-01T00:00:00-08:00</Root>",
                    "<Root>9999-01-01T00:00:00Z</Root>",
                    "<Root>9999-12-31T12:59:59-11:00</Root>",
                    "<Root>9999-12-31T12:59:59-10:59</Root>",
                    "<Root>9999-12-31T12:59:59+13:59</Root>",
                    "<Root>9999-12-31T23:59:59Z</Root>",
                    "<Root>9999-12-31T23:59:59+14:00</Root>",
                    "<Root>9999-12-31T23:59:59+14:00</Root>",
                    "<Root>9999-12-31T23:59:59+14:00</Root>",
                    "<Root>9998-12-31T12:59:59+14:00</Root>",
                    "<Root>9998-12-31T12:59:59-14:00</Root>",
                    "<Root>0001-01-01T00:00:00-08:00</Root>",
                    "<Root>0001-01-01T00:00:00-14:00</Root>",
                    "<Root>0001-01-01T00:00:00-13:59</Root>",
                    "<Root>0001-01-01T00:00:00Z</Root>"
                };

                for (int i = 0; i < actualArray.Length; i++)
                {
                    actual = (DateTimeOffset)actualArray[i];
                    expect = (string)expectArray[i];

                    using (XmlWriter w = utils.CreateWriter())
                    {
                        w.WriteStartElement("Root");
                        w.WriteValue(actual);
                        w.WriteEndElement();
                        w.Dispose();
                        if (!utils.CompareReader((string)expect))
                        {
                            isPassed = false;
                        }
                    }
                }
                Assert.True((isPassed));
            }

            //[TestCase(Name = "LookupPrefix")]
            public partial class TCLookUpPrefix
            {
                // LookupPrefix with null
                [Theory]
                [XmlWriterInlineData]
                public void lookupPrefix_1(XmlWriterUtils utils)
                {
                    using (XmlWriter w = utils.CreateWriter())
                    {
                        try
                        {
                            w.WriteStartElement("Root");
                            string s = w.LookupPrefix(null);
                            w.Dispose();
                        }
                        catch (ArgumentException e)
                        {
                            CError.WriteLineIgnore("Exception: " + e.ToString());
                            utils.CheckErrorState(w.WriteState);
                            return;
                        }
                    }
                    CError.WriteLine("Did not throw exception");
                    Assert.Fail();
                }

                // LookupPrefix with String.Empty should return String.Empty
                [Theory]
                [XmlWriterInlineData]
                public void lookupPrefix_2(XmlWriterUtils utils)
                {
                    using (XmlWriter w = utils.CreateWriter())
                    {
                        w.WriteStartElement("Root");
                        string s = w.LookupPrefix(string.Empty);
                        CError.Compare(s, string.Empty, "Error");
                    }
                    return;
                }

                // LookupPrefix with generated namespace used for attributes
                [Theory]
                [XmlWriterInlineData]
                public void lookupPrefix_3(XmlWriterUtils utils)
                {
                    using (XmlWriter w = utils.CreateWriter())
                    {
                        w.WriteStartElement("Root");
                        w.WriteAttributeString("a", "foo", "b");
                        string s = w.LookupPrefix("foo");
                        string exp = "p1";
                        CError.Compare(s, exp, "Error");
                    }
                    return;
                }

                // LookupPrefix for namespace used with element
                [Theory]
                [XmlWriterInlineData]
                public void lookupPrefix_4(XmlWriterUtils utils)
                {
                    using (XmlWriter w = utils.CreateWriter())
                    {
                        w.WriteStartElement("ns1", "Root", "foo");
                        string s = w.LookupPrefix("foo");
                        CError.Compare(s, "ns1", "Error");
                    }
                    return;
                }

                // LookupPrefix for namespace used with attribute
                [Theory]
                [XmlWriterInlineData]
                public void lookupPrefix_5(XmlWriterUtils utils)
                {
                    using (XmlWriter w = utils.CreateWriter())
                    {
                        w.WriteStartElement("Root");
                        w.WriteAttributeString("ns1", "attr1", "foo", "val1");
                        string s = w.LookupPrefix("foo");
                        CError.Compare(s, "ns1", "Error");
                    }
                    return;
                }

                // Lookup prefix for a default namespace
                [Theory]
                [XmlWriterInlineData]
                public void lookupPrefix_6(XmlWriterUtils utils)
                {
                    using (XmlWriter w = utils.CreateWriter())
                    {
                        w.WriteStartElement("Root", "foo");
                        w.WriteString("content");
                        string s = w.LookupPrefix("foo");
                        CError.Compare(s, string.Empty, "Error");
                    }
                    return;
                }

                // Lookup prefix for nested element with same namespace but different prefix
                [Theory]
                [XmlWriterInlineData]
                public void lookupPrefix_7(XmlWriterUtils utils)
                {
                    string s = "";
                    using (XmlWriter w = utils.CreateWriter())
                    {
                        w.WriteStartElement("x", "Root", "foo");
                        s = w.LookupPrefix("foo");
                        CError.Compare(s, "x", "Error");

                        w.WriteStartElement("y", "node", "foo");
                        s = w.LookupPrefix("foo");
                        CError.Compare(s, "y", "Error");

                        w.WriteStartElement("z", "node1", "foo");
                        s = w.LookupPrefix("foo");
                        CError.Compare(s, "z", "Error");
                        w.WriteEndElement();

                        s = w.LookupPrefix("foo");
                        CError.Compare(s, "y", "Error");
                        w.WriteEndElement();

                        w.WriteEndElement();
                    }
                    return;
                }

                // Lookup prefix for multiple prefix associated with the same namespace
                [Theory]
                [XmlWriterInlineData]
                public void lookupPrefix_8(XmlWriterUtils utils)
                {
                    using (XmlWriter w = utils.CreateWriter())
                    {
                        w.WriteStartElement("x", "Root", "foo");
                        w.WriteAttributeString("y", "a", "foo", "b");
                        string s = w.LookupPrefix("foo");
                        CError.Compare(s, "y", "Error");
                    }
                    return;
                }

                // Lookup prefix for namespace defined outside the scope of an empty element and also defined in its parent
                [Theory]
                [XmlWriterInlineData]
                public void lookupPrefix_9(XmlWriterUtils utils)
                {
                    using (XmlWriter w = utils.CreateWriter())
                    {
                        w.WriteStartElement("x", "Root", "foo");
                        w.WriteStartElement("y", "node", "foo");
                        w.WriteEndElement();
                        string s = w.LookupPrefix("foo");
                        CError.Compare(s, "x", "Error");
                        w.WriteEndElement();
                    }
                    return;
                }

                // Bug 53940: Lookup prefix for namespace declared as default and also with a prefix
                [Theory]
                [XmlWriterInlineData]
                public void lookupPrefix_10(XmlWriterUtils utils)
                {
                    string s;
                    using (XmlWriter w = utils.CreateWriter())
                    {
                        w.WriteStartElement("Root", "foo");
                        w.WriteStartElement("x", "node", "foo");
                        s = w.LookupPrefix("foo");
                        CError.Compare(s, "x", "Error in nested element");
                        w.WriteEndElement();
                        s = w.LookupPrefix("foo");
                        CError.Compare(s, string.Empty, "Error in root element");
                        w.WriteEndElement();
                    }
                    return;
                }
            }

            //[TestCase(Name = "XmlSpace")]
            public partial class TCXmlSpace
            {
                // Verify XmlSpace as Preserve
                [Theory]
                [XmlWriterInlineData]
                public void xmlSpace_1(XmlWriterUtils utils)
                {
                    using (XmlWriter w = utils.CreateWriter())
                    {
                        w.WriteStartElement("Root");
                        w.WriteAttributeString("xml", "space", null, "preserve");
                        CError.Compare(w.XmlSpace, XmlSpace.Preserve, "Error");
                    }
                    return;
                }

                // Verify XmlSpace as Default
                [Theory]
                [XmlWriterInlineData]
                public void xmlSpace_2(XmlWriterUtils utils)
                {
                    using (XmlWriter w = utils.CreateWriter())
                    {
                        w.WriteStartElement("Root");
                        w.WriteAttributeString("xml", "space", null, "default");
                        CError.Compare(w.XmlSpace, XmlSpace.Default, "Error");
                    }
                    return;
                }

                // Verify XmlSpace as None
                [Theory]
                [XmlWriterInlineData]
                public void xmlSpace_3(XmlWriterUtils utils)
                {
                    using (XmlWriter w = utils.CreateWriter())
                    {
                        w.WriteStartElement("Root");
                        CError.Compare(w.XmlSpace, XmlSpace.None, "Error");
                    }
                    return;
                }

                // Verify XmlSpace within an empty element
                [Theory]
                [XmlWriterInlineData]
                public void xmlSpace_4(XmlWriterUtils utils)
                {
                    using (XmlWriter w = utils.CreateWriter())
                    {
                        w.WriteStartElement("Root");
                        w.WriteAttributeString("xml", "space", null, "preserve");
                        w.WriteStartElement("node", null);

                        CError.Compare(w.XmlSpace, XmlSpace.Preserve, "Error");

                        w.WriteEndElement();
                        w.WriteEndElement();
                    }
                    return;
                }

                // Verify XmlSpace - scope with nested elements (both PROLOG and EPILOG)
                [Theory]
                [XmlWriterInlineData]
                public void xmlSpace_5(XmlWriterUtils utils)
                {
                    using (XmlWriter w = utils.CreateWriter())
                    {
                        w.WriteStartElement("Root");

                        w.WriteStartElement("node", null);
                        w.WriteAttributeString("xml", "space", null, "preserve");
                        CError.Compare(w.XmlSpace, XmlSpace.Preserve, "Error");

                        w.WriteStartElement("node1");
                        CError.Compare(w.XmlSpace, XmlSpace.Preserve, "Error");

                        w.WriteStartElement("node2");
                        w.WriteAttributeString("xml", "space", null, "default");
                        CError.Compare(w.XmlSpace, XmlSpace.Default, "Error");
                        w.WriteEndElement();

                        CError.Compare(w.XmlSpace, XmlSpace.Preserve, "Error");
                        w.WriteEndElement();

                        CError.Compare(w.XmlSpace, XmlSpace.Preserve, "Error");
                        w.WriteEndElement();

                        CError.Compare(w.XmlSpace, XmlSpace.None, "Error");
                        w.WriteEndElement();
                    }

                    return;
                }

                // Verify XmlSpace - outside defined scope
                [Theory]
                [XmlWriterInlineData]
                public void xmlSpace_6(XmlWriterUtils utils)
                {
                    using (XmlWriter w = utils.CreateWriter())
                    {
                        w.WriteStartElement("Root");
                        w.WriteStartElement("node", null);
                        w.WriteAttributeString("xml", "space", null, "preserve");
                        w.WriteEndElement();

                        CError.Compare(w.XmlSpace, XmlSpace.None, "Error");
                        w.WriteEndElement();
                    }

                    return;
                }

                // Verify XmlSpace with invalid space value
                [Theory]
                [XmlWriterInlineData]
                public void xmlSpace_7(XmlWriterUtils utils)
                {
                    using (XmlWriter w = utils.CreateWriter())
                    {
                        try
                        {
                            w.WriteStartElement("Root");
                            w.WriteStartElement("node", null);
                            w.WriteAttributeString("xml", "space", null, "reserve");
                        }
                        catch (ArgumentException e)
                        {
                            CError.WriteLineIgnore(e.ToString());
                            CError.Compare(w.WriteState, WriteState.Error, "WriteState should be Error");
                            return;
                        }
                    }
                    CError.WriteLine("Exception expected");
                    Assert.Fail();
                }

                // Duplicate xml:space attr should error
                [Theory]
                [XmlWriterInlineData]
                public void xmlSpace_8(XmlWriterUtils utils)
                {
                    using (XmlWriter w = utils.CreateWriter())
                    {
                        try
                        {
                            w.WriteStartElement("Root");
                            w.WriteAttributeString("xml", "space", null, "preserve");
                            w.WriteAttributeString("xml", "space", null, "default");
                        }
                        catch (XmlException e)
                        {
                            CError.WriteLineIgnore(e.ToString());
                            CError.Compare(w.WriteState, WriteState.Error, "WriteState should be Error");
                            return;
                        }
                    }
                    CError.WriteLine("Exception expected");
                    Assert.Fail();
                }

                // Verify XmlSpace value when received through WriteString
                [Theory]
                [XmlWriterInlineData]
                public void xmlSpace_9(XmlWriterUtils utils)
                {
                    using (XmlWriter w = utils.CreateWriter())
                    {
                        w.WriteStartElement("Root");
                        w.WriteStartAttribute("xml", "space", null);
                        w.WriteString("default");
                        w.WriteEndAttribute();

                        CError.Compare(w.XmlSpace, XmlSpace.Default, "Error");
                        w.WriteEndElement();
                    }
                    return;
                }
            }

            //[TestCase(Name = "XmlLang")]
            public partial class TCXmlLang
            {
                // Verify XmlLang sanity test
                [Theory]
                [XmlWriterInlineData]
                public void XmlLang_1(XmlWriterUtils utils)
                {
                    using (XmlWriter w = utils.CreateWriter())
                    {
                        w.WriteStartElement("Root");
                        w.WriteStartElement("node", null);
                        w.WriteAttributeString("xml", "lang", null, "en");

                        CError.Compare(w.XmlLang, "en", "Error");

                        w.WriteEndElement();
                        w.WriteEndElement();
                    }
                    return;
                }

                // Verify that default value of XmlLang is NULL
                [Theory]
                [XmlWriterInlineData]
                public void XmlLang_2(XmlWriterUtils utils)
                {
                    using (XmlWriter w = utils.CreateWriter())
                    {
                        w.WriteStartElement("Root");
                        if (w.XmlLang != null)
                        {
                            w.Dispose();
                            CError.WriteLine("Default value if no xml:lang attributes are currently on the stack should be null");
                            CError.WriteLine("Actual value: {0}", w.XmlLang.ToString());
                            Assert.Fail();
                        }
                    }
                    return;
                }

                // Verify XmlLang scope inside nested elements (both PROLOG and EPILOG)
                [Theory]
                [XmlWriterInlineData]
                public void XmlLang_3(XmlWriterUtils utils)
                {
                    using (XmlWriter w = utils.CreateWriter())
                    {
                        w.WriteStartElement("Root");

                        w.WriteStartElement("node", null);
                        w.WriteAttributeString("xml", "lang", null, "fr");
                        CError.Compare(w.XmlLang, "fr", "Error");

                        w.WriteStartElement("node1");
                        w.WriteAttributeString("xml", "lang", null, "en-US");
                        CError.Compare(w.XmlLang, "en-US", "Error");

                        w.WriteStartElement("node2");
                        CError.Compare(w.XmlLang, "en-US", "Error");
                        w.WriteEndElement();

                        CError.Compare(w.XmlLang, "en-US", "Error");
                        w.WriteEndElement();

                        CError.Compare(w.XmlLang, "fr", "Error");
                        w.WriteEndElement();

                        CError.Compare(w.XmlLang, null, "Error");
                        w.WriteEndElement();
                    }
                    return;
                }

                // Duplicate xml:lang attr should error
                [Theory]
                [XmlWriterInlineData]
                public void XmlLang_4(XmlWriterUtils utils)
                {
                    /*if (WriterType == WriterType.XmlTextWriter)
                        return;*/

                    using (XmlWriter w = utils.CreateWriter())
                    {
                        try
                        {
                            w.WriteStartElement("Root");
                            w.WriteAttributeString("xml", "lang", null, "en-us");
                            w.WriteAttributeString("xml", "lang", null, "ja");
                        }
                        catch (XmlException e)
                        {
                            CError.WriteLineIgnore(e.ToString());
                            CError.Compare(w.WriteState, WriteState.Error, "WriteState should be Error");
                            return;
                        }
                    }
                    CError.WriteLine("Exception expected");
                    Assert.Fail();
                }

                // Verify XmlLang value when received through WriteAttributes
                [Theory]
                [XmlWriterInlineData]
                public void XmlLang_5(XmlWriterUtils utils)
                {
                    XmlReaderSettings xrs = new XmlReaderSettings();
                    xrs.IgnoreWhitespace = true;
                    XmlReader tr = XmlReader.Create(FilePathUtil.getStream(XmlWriterUtils.FullPath("XmlReader.xml")), xrs);

                    while (tr.Read())
                    {
                        if (tr.LocalName == "XmlLangNode")
                        {
                            tr.Read();
                            tr.MoveToNextAttribute();
                            break;
                        }
                    }

                    using (XmlWriter w = utils.CreateWriter())
                    {
                        w.WriteStartElement("Root");
                        w.WriteAttributes(tr, false);

                        CError.Compare(w.XmlLang, "fr", "Error");
                        w.WriteEndElement();
                    }
                    return;
                }

                // Verify XmlLang value when received through WriteString
                [Theory]
                [XmlWriterInlineData]
                public void XmlLang_6(XmlWriterUtils utils)
                {
                    using (XmlWriter w = utils.CreateWriter())
                    {
                        w.WriteStartElement("Root");
                        w.WriteStartAttribute("xml", "lang", null);
                        w.WriteString("en-US");
                        w.WriteEndAttribute();

                        CError.Compare(w.XmlLang, "en-US", "Error");
                        w.WriteEndElement();
                    }
                    return;
                }

                // Should not check XmlLang value
                [Theory]
                [XmlWriterInlineData]
                public void XmlLang_7(XmlWriterUtils utils)
                {
                    string[] langs = new string[] { "en-", "e n", "en", "en-US", "e?", "en*US" };

                    for (int i = 0; i < langs.Length; i++)
                    {
                        using (XmlWriter w = utils.CreateWriter())
                        {
                            w.WriteStartElement("Root");
                            w.WriteAttributeString("xml", "lang", null, langs[i]);
                            w.WriteEndElement();
                        }

                        string strExp = "<Root xml:lang=\"" + langs[i] + "\" />";
                        if (!utils.CompareReader(strExp))
                            Assert.Fail();
                    }
                    return;
                }

                // More XmlLang with valid sequence
                [Theory]
                [XmlWriterInlineData]
                public void XmlLang_8(XmlWriterUtils utils)
                {
                    using (XmlWriter w = utils.CreateWriter())
                    {
                        w.WriteStartElement("Root");
                        w.WriteAttributeString("xml", "lang", null, "U.S.A.");
                    }
                    return;
                }
            }

            //[TestCase(Name = "WriteRaw")]
            public partial class TCWriteRaw : TCWriteBuffer
            {
                // Call both WriteRaw Methods
                [Theory]
                [XmlWriterInlineData]
                public void writeRaw_1(XmlWriterUtils utils)
                {
                    using (XmlWriter w = utils.CreateWriter())
                    {
                        string t = "Test Case";
                        w.WriteStartElement("Root");
                        w.WriteStartAttribute("a");
                        w.WriteRaw(t);
                        w.WriteStartAttribute("b");
                        w.WriteRaw(t.ToCharArray(), 0, 4);
                        w.WriteEndElement();
                    }
                    Assert.True(utils.CompareReader("<Root a=\"Test Case\" b=\"Test\" />"));
                }

                // WriteRaw with entities and entitized characters
                [Theory]
                [XmlWriterInlineData]
                public void writeRaw_2(XmlWriterUtils utils)
                {
                    using (XmlWriter w = utils.CreateWriter())
                    {
                        string t = "<node a=\"&'b\">\" c=\"'d\">&</node>";

                        w.WriteStartElement("Root");
                        w.WriteRaw(t);
                        w.WriteEndElement();
                    }

                    string strExp = "<Root><node a=\"&'b\">\" c=\"'d\">&</node></Root>";

                    Assert.True(utils.CompareString(strExp));
                }

                // WriteRaw with entire Xml Document in string
                [Theory]
                [XmlWriterInlineData]
                public void writeRaw_3(XmlWriterUtils utils)
                {
                    XmlWriter w = utils.CreateWriter();
                    string t = "<root><node1></node1><node2></node2></root>";

                    w.WriteRaw(t);

                    w.Dispose();
                    Assert.True(utils.CompareReader("<root><node1></node1><node2></node2></root>"));
                }

                // Call WriteRaw to write the value of xml:space
                [Theory]
                [XmlWriterInlineData]
                public void writeRaw_4(XmlWriterUtils utils)
                {
                    using (XmlWriter w = utils.CreateWriter())
                    {
                        w.WriteStartElement("Root");
                        w.WriteStartAttribute("xml", "space", null);
                        w.WriteRaw("default");
                        w.WriteEndAttribute();
                        w.WriteEndElement();
                    }
                    Assert.True(utils.CompareReader("<Root xml:space=\"default\" />"));
                }

                // Call WriteRaw to write the value of xml:lang
                [Theory]
                [XmlWriterInlineData]
                public void writerRaw_5(XmlWriterUtils utils)
                {
                    using (XmlWriter w = utils.CreateWriter())
                    {
                        string strraw = "abc";
                        char[] buffer = strraw.ToCharArray();

                        w.WriteStartElement("root");
                        w.WriteStartAttribute("xml", "lang", null);
                        w.WriteRaw(buffer, 1, 1);
                        w.WriteRaw(buffer, 0, 2);
                        w.WriteEndAttribute();
                        w.WriteEndElement();
                    }
                    Assert.True(utils.CompareReader("<root xml:lang=\"bab\" />"));
                }

                // WriteRaw with count > buffer size
                [Theory]
                [XmlWriterInlineData]
                public void writeRaw_6(XmlWriterUtils utils)
                {
                    VerifyInvalidWrite(utils, "WriteRaw", 5, 0, 6, typeof(ArgumentOutOfRangeException/*ArgumentException*/));
                }

                // WriteRaw with count < 0
                [Theory]
                [XmlWriterInlineData]
                public void writeRaw_7(XmlWriterUtils utils)
                {
                    VerifyInvalidWrite(utils, "WriteRaw", 5, 2, -1, typeof(ArgumentOutOfRangeException));
                }

                // WriteRaw with index > buffer size
                [Theory]
                [XmlWriterInlineData]
                public void writeRaw_8(XmlWriterUtils utils)
                {
                    VerifyInvalidWrite(utils, "WriteRaw", 5, 6, 1, typeof(ArgumentOutOfRangeException/*ArgumentException*/));
                }

                // WriteRaw with index < 0
                [Theory]
                [XmlWriterInlineData]
                public void writeRaw_9(XmlWriterUtils utils)
                {
                    VerifyInvalidWrite(utils, "WriteRaw", 5, -1, 1, typeof(ArgumentOutOfRangeException));
                }

                // WriteRaw with index + count exceeds buffer
                [Theory]
                [XmlWriterInlineData]
                public void writeRaw_10(XmlWriterUtils utils)
                {
                    VerifyInvalidWrite(utils, "WriteRaw", 5, 2, 5, typeof(ArgumentOutOfRangeException/*ArgumentException*/));
                }

                // WriteRaw with buffer = null
                [Theory]
                [XmlWriterInlineData]
                public void writeRaw_11(XmlWriterUtils utils)
                {
                    using (XmlWriter w = utils.CreateWriter())
                    {
                        try
                        {
                            w.WriteStartElement("Root");
                            w.WriteRaw(null, 0, 0);
                        }
                        catch (ArgumentNullException)
                        {
                            CError.Compare(w.WriteState, WriteState.Error, "WriteState should be Error");
                            return;
                        }
                    }
                    CError.WriteLine("Did not throw exception");
                    Assert.Fail();
                }

                // WriteRaw with valid surrogate pair
                [Theory]
                [XmlWriterInlineData]
                public void writeRaw_12(XmlWriterUtils utils)
                {
                    using (XmlWriter w = utils.CreateWriter())
                    {
                        w.WriteStartElement("Root");

                        string str = "\uD812\uDD12";
                        char[] chr = str.ToCharArray();

                        w.WriteRaw(str);
                        w.WriteRaw(chr, 0, chr.Length);
                        w.WriteEndElement();
                    }
                    string strExp = "<Root>\uD812\uDD12\uD812\uDD12</Root>";
                    Assert.True(utils.CompareReader(strExp));
                }

                // WriteRaw with invalid surrogate pair
                [Theory]
                [XmlWriterInlineData]
                public void writeRaw_13(XmlWriterUtils utils)
                {
                    using (XmlWriter w = utils.CreateWriter())
                    {
                        try
                        {
                            w.WriteStartElement("Root");
                            w.WriteRaw("\uD812");
                        }
                        catch (ArgumentException e)
                        {
                            CError.WriteLineIgnore(e.ToString());
                            CError.Compare(w.WriteState, WriteState.Error, "WriteState should be Error");
                            return;
                        }
                    }
                    CError.WriteLine("Did not throw exception");
                    Assert.Fail();
                }

                // Index = Count = 0
                [Theory]
                [XmlWriterInlineData]
                public void writeRaw_14(XmlWriterUtils utils)
                {
                    string lang = new string('a', 1);
                    char[] buffer = lang.ToCharArray();

                    using (XmlWriter w = utils.CreateWriter())
                    {
                        w.WriteStartElement("root");
                        w.WriteStartAttribute("xml", "lang", null);
                        w.WriteRaw(buffer, 0, 0);
                        w.WriteEndElement();
                    }
                    Assert.True(utils.CompareReader("<root xml:lang=\"\" />"));
                }
            }

            //[TestCase(Name = "WriteBase64")]
            public partial class TCWriteBase64 : TCWriteBuffer
            {
                // Base64LineSize = 76, test around this boundary size
                [Theory]
                [XmlWriterInlineData(75)]
                [XmlWriterInlineData(76)]
                [XmlWriterInlineData(77)]
                [XmlWriterInlineData(1024)]
                [XmlWriterInlineData(4096)]
                public void Base64_1(XmlWriterUtils utils, int strBase64Len)
                {
                    string strBase64 = string.Empty;
                    for (int i = 0; i < strBase64Len; i++)
                    {
                        strBase64 += "A";
                    }

                    byte[] Wbase64 = new byte[strBase64Len * 2];
                    int Wbase64len = 0;

                    for (int i = 0; i < strBase64.Length; i++)
                    {
                        WriteToBuffer(ref Wbase64, ref Wbase64len, System.BitConverter.GetBytes(strBase64[i]));
                    }

                    using (XmlWriter w = utils.CreateWriter())
                    {
                        w.WriteStartElement("root");
                        w.WriteBase64(Wbase64, 0, (int)Wbase64len);
                        w.WriteEndElement();
                    }

                    XmlReader r = utils.GetReader();
                    r.Read();
                    byte[] buffer = new byte[strBase64Len * 2];
                    int nRead = r.ReadElementContentAsBase64(buffer, 0, strBase64Len * 2);
                    r.Dispose();

                    CError.Compare(nRead, strBase64Len * 2, "Read count");

                    string strRes = string.Empty;
                    for (int i = 0; i < nRead; i += 2)
                    {
                        strRes += BitConverter.ToChar(buffer, i);
                    }
                    CError.Compare(strRes, strBase64, "Base64 value");

                    return;
                }

                // WriteBase64 with count > buffer size
                [Theory]
                [XmlWriterInlineData]
                public void Base64_2(XmlWriterUtils utils)
                {
                    VerifyInvalidWrite(utils, "WriteBase64", 5, 0, 6, typeof(ArgumentOutOfRangeException/*ArgumentException*/));
                }

                // WriteBase64 with count < 0
                [Theory]
                [XmlWriterInlineData]
                public void Base64_3(XmlWriterUtils utils)
                {
                    VerifyInvalidWrite(utils, "WriteBase64", 5, 2, -1, typeof(ArgumentOutOfRangeException));
                }

                // WriteBase64 with index > buffer size
                [Theory]
                [XmlWriterInlineData]
                public void Base64_4(XmlWriterUtils utils)
                {
                    VerifyInvalidWrite(utils, "WriteBase64", 5, 5, 1, typeof(ArgumentOutOfRangeException/*ArgumentException*/));
                }

                // WriteBase64 with index < 0
                [Theory]
                [XmlWriterInlineData]
                public void Base64_5(XmlWriterUtils utils)
                {
                    VerifyInvalidWrite(utils, "WriteBase64", 5, -1, 1, typeof(ArgumentOutOfRangeException));
                }

                // WriteBase64 with index + count exceeds buffer
                [Theory]
                [XmlWriterInlineData]
                public void Base64_6(XmlWriterUtils utils)
                {
                    VerifyInvalidWrite(utils, "WriteBase64", 5, 2, 5, typeof(ArgumentOutOfRangeException/*ArgumentException*/));
                }

                // WriteBase64 with buffer = null
                [Theory]
                [XmlWriterInlineData]
                public void Base64_7(XmlWriterUtils utils)
                {
                    using (XmlWriter w = utils.CreateWriter())
                    {
                        try
                        {
                            w.WriteStartElement("root");
                            w.WriteBase64(null, 0, 0);
                        }
                        catch (ArgumentNullException)
                        {
                            CError.Compare(w.WriteState, WriteState.Error, "WriteState should be Error");
                            return;
                        }
                    }
                    CError.WriteLine("Did not throw exception");
                    Assert.Fail();
                }

                // Index = Count = 0
                [Theory]
                [XmlWriterInlineData]
                public void Base64_8(XmlWriterUtils utils)
                {
                    byte[] buffer = new byte[10];

                    using (XmlWriter w = utils.CreateWriter())
                    {
                        w.WriteStartElement("root");
                        w.WriteStartAttribute("foo");
                        w.WriteBase64(buffer, 0, 0);
                        w.WriteEndElement();
                    }
                    Assert.True(utils.CompareReader("<root foo=\"\" />"));
                }

                [Theory]
                [XmlWriterInlineData("lang")]
                [XmlWriterInlineData("space")]
                [XmlWriterInlineData("ns")]
                public void Base64_9(XmlWriterUtils utils, string param)
                {
                    byte[] buffer = new byte[10];

                    using (XmlWriter w = utils.CreateWriter())
                    {
                        try
                        {
                            w.WriteStartElement("root");
                            switch (param)
                            {
                                case "lang":
                                    w.WriteStartAttribute("xml", "lang", null);
                                    break;
                                case "space":
                                    w.WriteStartAttribute("xml", "space", null);
                                    break;
                                case "ns":
                                    w.WriteStartAttribute("xmlns", "foo", null);
                                    break;
                            }
                            w.WriteBase64(buffer, 0, 5);
                        }
                        catch (InvalidOperationException)
                        {
                            return;
                        }
                    }

                    CError.WriteLine("Did not throw exception");
                    Assert.Fail();
                }

                // WriteBase64 should flush the buffer if WriteString is called
                [Theory]
                [XmlWriterInlineData]
                public void Base64_11(XmlWriterUtils utils)
                {
                    using (XmlWriter w = utils.CreateWriter())
                    {
                        w.WriteStartElement("fromname");
                        w.WriteString("=?gb2312?B?");
                        w.Flush();
                        byte[] bytesFrom = new byte[] { 1, 2 };
                        w.WriteBase64(bytesFrom, 0, bytesFrom.Length);
                        w.Flush();
                        w.WriteString("?=");
                        w.Flush();
                        w.WriteEndElement();
                    }

                    string strExp = "<fromname>=?gb2312?B?AQI=?=</fromname>";
                    utils.CompareString(strExp);
                    return;
                }

                // XmlWriter.WriteBase64 inserts new lines where they should not be...
                [Theory]
                [XmlWriterInlineData]
                public void Base64_12(XmlWriterUtils utils)
                {
                    byte[][] byteArrays = new byte[][]
                {
                    new byte[] {0xd8,0x7e,0x8d,0xf9,0x84,0x06,0x4a,0x67,0x93,0xba,0xc1,0x0d,0x16,0x53,0xb2,0xcc,0xbb,0x03,0xe3,0xf9},
                    new byte[] {
                        0xaa,
                        0x48,
                        0x60,
                        0x49,
                        0xa1,
                        0xb4,
                        0xa2,
                        0xe4,
                        0x65,
                        0x74,
                        0x5e,
                        0xc8,
                        0x84,
                        0x33,
                        0xae,
                        0x6a,
                        0xe3,
                        0xb5,
                        0x2f,
                        0x8c,
                    },
                    new byte[] {
                        0x46,
                        0xe4,
                        0xf9,
                        0xb9,
                        0x3e,
                        0xb6,
                        0x6b,
                        0x3f,
                        0xf9,
                        0x01,
                        0x67,
                        0x5b,
                        0xf5,
                        0x2c,
                        0xfd,
                        0xe6,
                        0x8e,
                        0x52,
                        0xc4,
                        0x1b,
                    },
                    new byte[] {
                        0x55,
                        0xca,
                        0x97,
                        0xfb,
                        0xaa,
                        0xc6,
                        0x9a,
                        0x69,
                        0xa0,
                        0x2e,
                        0x1f,
                        0xa7,
                        0xa9,
                        0x3c,
                        0x62,
                        0xe9,
                        0xa1,
                        0xf3,
                        0x0a,
                        0x07,
                    },
                    new byte[] {
                        0x28,
                        0x82,
                        0xb7,
                        0xbe,
                        0x49,
                        0x45,
                        0x37,
                        0x54,
                        0x26,
                        0x31,
                        0xd4,
                        0x24,
                        0xa6,
                        0x5a,
                        0xb6,
                        0x6b,
                        0x37,
                        0xf3,
                        0xaf,
                        0x38,
                    },
                    new byte[] {
                        0xdd,
                        0xbd,
                        0x3f,
                        0x8f,
                        0xd5,
                        0xeb,
                        0x5b,
                        0xcc,
                        0x9d,
                        0xdd,
                        0x00,
                        0xba,
                        0x90,
                        0x76,
                        0x4c,
                        0xcb,
                        0xd3,
                        0xd5,
                        0xfa,
                        0xd2,
                    }
             };

                    XmlWriter writer = utils.CreateWriter();
                    writer.WriteStartElement("Root");
                    for (int i = 0; i < byteArrays.Length; i++)
                    {
                        writer.WriteStartElement("DigestValue");
                        byte[] bytes = byteArrays[i];
                        writer.WriteBase64(bytes, 0, bytes.Length);
                        writer.WriteEndElement();
                    }
                    writer.WriteEndElement();
                    writer.Dispose();

                    Assert.True(utils.CompareBaseline("bug364698.xml"));
                }

                // XmlWriter does not flush Base64 data on the Close
                [Theory]
                [XmlWriterInlineData(WriterType.All & ~WriterType.Async)]
                public void Base64_13(XmlWriterUtils utils)
                {
                    byte[] data = new byte[] { 60, 65, 47, 62 }; // <A/>

                    XmlWriterSettings ws = new XmlWriterSettings();
                    ws.ConformanceLevel = ConformanceLevel.Fragment;

                    StringBuilder sb = new StringBuilder();
                    using (XmlWriter w = WriterHelper.Create(sb, ws, overrideAsync: true, async: utils.Async))
                    {
                        w.WriteBase64(data, 0, data.Length);
                    }

                    Assert.Equal("PEEvPg==", sb.ToString());
                }
            }

            //[TestCase(Name = "WriteBinHex")]
            public partial class TCWriteBinHex : TCWriteBuffer
            {
                // Call WriteBinHex with correct byte, index, and count
                [Theory]
                [XmlWriterInlineData]
                public void BinHex_1(XmlWriterUtils utils)
                {
                    using (XmlWriter w = utils.CreateWriter())
                    {
                        w.WriteStartElement("root");

                        string str = "abcdefghijk1234567890";
                        byte[] buffer = StringToByteArray(str);
                        w.WriteBinHex(buffer, 0, str.Length * 2);
                        w.WriteEndElement();
                    }
                    return;
                }

                // WriteBinHex with count > buffer size
                [Theory]
                [XmlWriterInlineData]
                public void BinHex_2(XmlWriterUtils utils)
                {
                    VerifyInvalidWrite(utils, "WriteBinHex", 5, 0, 6, typeof(ArgumentOutOfRangeException/*ArgumentException*/));
                }

                // WriteBinHex with count < 0
                [Theory]
                [XmlWriterInlineData]
                public void BinHex_3(XmlWriterUtils utils)
                {
                    VerifyInvalidWrite(utils, "WriteBinHex", 5, 2, -1, typeof(ArgumentOutOfRangeException));
                }

                // WriteBinHex with index > buffer size
                [Theory]
                [XmlWriterInlineData]
                public void BinHex_4(XmlWriterUtils utils)
                {
                    VerifyInvalidWrite(utils, "WriteBinHex", 5, 5, 1, typeof(ArgumentOutOfRangeException/*ArgumentException*/));
                }

                // WriteBinHex with index < 0
                [Theory]
                [XmlWriterInlineData]
                public void BinHex_5(XmlWriterUtils utils)
                {
                    VerifyInvalidWrite(utils, "WriteBinHex", 5, -1, 1, typeof(ArgumentOutOfRangeException));
                }

                // WriteBinHex with index + count exceeds buffer
                [Theory]
                [XmlWriterInlineData]
                public void BinHex_6(XmlWriterUtils utils)
                {
                    VerifyInvalidWrite(utils, "WriteBinHex", 5, 2, 5, typeof(ArgumentOutOfRangeException/*ArgumentException*/));
                }

                // WriteBinHex with buffer = null
                [Theory]
                [XmlWriterInlineData]
                public void BinHex_7(XmlWriterUtils utils)
                {
                    using (XmlWriter w = utils.CreateWriter())
                    {
                        try
                        {
                            w.WriteStartElement("root");
                            w.WriteBinHex(null, 0, 0);
                        }
                        catch (ArgumentNullException)
                        {
                            if (utils.WriterType == WriterType.CustomWriter)
                            {
                                CError.Compare(w.WriteState, WriteState.Element, "WriteState should be Element");
                            }
                            else
                            {
                                utils.CheckErrorState(w.WriteState);
                            }
                            return;
                        }
                    }
                    CError.WriteLine("Did not throw exception");
                    Assert.Fail();
                }

                // Index = Count = 0
                [Theory]
                [XmlWriterInlineData]
                public void BinHex_8(XmlWriterUtils utils)
                {
                    byte[] buffer = new byte[10];

                    using (XmlWriter w = utils.CreateWriter())
                    {
                        w.WriteStartElement("root");
                        w.WriteStartAttribute("xml", "lang", null);
                        w.WriteBinHex(buffer, 0, 0);
                        w.WriteEndElement();
                    }
                    Assert.True(utils.CompareReader("<root xml:lang=\"\" />"));
                }

                // Call WriteBinHex as an attribute value
                [Theory]
                [XmlWriterInlineData]
                public void BinHex_9(XmlWriterUtils utils)
                {
                    string strBinHex = "abc";
                    byte[] Wbase64 = new byte[2000];
                    int/*uint*/ Wbase64len = 0;

                    using (XmlWriter w = utils.CreateWriter())
                    {
                        w.WriteStartElement("root");
                        w.WriteStartAttribute("a", null);
                        for (int i = 0; i < strBinHex.Length; i++)
                        {
                            WriteToBuffer(ref Wbase64, ref Wbase64len, System.BitConverter.GetBytes(strBinHex[i]));
                        }
                        w.WriteBinHex(Wbase64, 0, (int)Wbase64len);
                        w.WriteEndElement();
                    }
                    if (System.BitConverter.IsLittleEndian)
                    {
                        Assert.True(utils.CompareReader("<root a='610062006300' />"));
                    }
                    else
                    {
                        Assert.True(utils.CompareReader("<root a='006100620063' />"));
                    }
                }

                // Call WriteBinHex and verify results can be read as a string
                [Theory]
                [XmlWriterInlineData]
                public void BinHex_10(XmlWriterUtils utils)
                {
                    string strBinHex = "abc";
                    byte[] Wbase64 = new byte[2000];
                    int/*uint*/ Wbase64len = 0;

                    using (XmlWriter w = utils.CreateWriter())
                    {
                        w.WriteStartElement("root");
                        for (int i = 0; i < strBinHex.Length; i++)
                        {
                            WriteToBuffer(ref Wbase64, ref Wbase64len, System.BitConverter.GetBytes(strBinHex[i]));
                        }
                        w.WriteBinHex(Wbase64, 0, (int)Wbase64len);
                        w.WriteEndElement();
                    }
                    if (System.BitConverter.IsLittleEndian)
                    {
                        Assert.True(utils.CompareReader("<root>610062006300</root>"));
                    }
                    else
                    {
                        Assert.True(utils.CompareReader("<root>006100620063</root>"));
                    }
                }
            }

            //[TestCase(Name = "WriteState")]
            public partial class TCWriteState
            {
                // Verify WriteState.Start when nothing has been written yet
                [Theory]
                [XmlWriterInlineData]
                public void writeState_1(XmlWriterUtils utils)
                {
                    XmlWriter w = utils.CreateWriter();
                    CError.Compare(w.WriteState, WriteState.Start, "Error");
                    try
                    {
                        w.Dispose();
                    }
                    catch (InvalidOperationException)
                    {
                        Assert.Fail();
                    }
                    return;
                }

                // Verify correct state when writing in Prolog
                [Theory]
                [XmlWriterInlineData]
                public void writeState_2(XmlWriterUtils utils)
                {
                    using (XmlWriter w = utils.CreateWriter())
                    {
                        CError.Compare(w.WriteState, WriteState.Start, "Error");
                        w.WriteDocType("Root", null, null, "<!ENTITY e \"test\">");
                        CError.Compare(w.WriteState, WriteState.Prolog, "Error");
                        w.WriteStartElement("Root");
                        CError.Compare(w.WriteState, WriteState.Element, "Error");
                        w.WriteEndElement();
                    }
                    return;
                }

                // Verify correct state when writing an attribute
                [Theory]
                [XmlWriterInlineData]
                public void writeState_3(XmlWriterUtils utils)
                {
                    using (XmlWriter w = utils.CreateWriter())
                    {
                        w.WriteStartElement("Root");
                        w.WriteStartAttribute("a");
                        CError.Compare(w.WriteState, WriteState.Attribute, "Error");
                        w.WriteString("content");
                        w.WriteEndAttribute();
                        w.WriteEndElement();
                    }
                    return;
                }

                // Verify correct state when writing element content
                [Theory]
                [XmlWriterInlineData]
                public void writeState_4(XmlWriterUtils utils)
                {
                    using (XmlWriter w = utils.CreateWriter())
                    {
                        w.WriteStartElement("Root");
                        w.WriteString("content");
                        CError.Compare(w.WriteState, WriteState.Content, "Error");
                        w.WriteEndElement();
                    }
                    return;
                }

                // Verify correct state after Close has been called
                [Theory]
                [XmlWriterInlineData]
                public void writeState_5(XmlWriterUtils utils)
                {
                    XmlWriter w = utils.CreateWriter();
                    w.WriteStartElement("Root");
                    w.WriteEndElement();
                    w.Dispose();
                    CError.Compare(w.WriteState, WriteState.Closed, "Error");
                    return;
                }

                // Verify WriteState = Error after an exception
                [Theory]
                [XmlWriterInlineData]
                public void writeState_6(XmlWriterUtils utils)
                {
                    using (XmlWriter w = utils.CreateWriter())
                    {
                        try
                        {
                            w.WriteStartElement("Root");
                            w.WriteStartElement("Root");
                        }
                        catch (InvalidOperationException e)
                        {
                            CError.WriteLineIgnore(e.ToString());
                            CError.Compare(w.WriteState, WriteState.Error, "Error");
                        }
                    }
                    return;
                }

                [Theory]
                [XmlWriterInlineData("WriteStartDocument")]
                [XmlWriterInlineData("WriteStartElement")]
                [XmlWriterInlineData("WriteEndElement")]
                [XmlWriterInlineData("WriteStartAttribute")]
                [XmlWriterInlineData("WriteEndAttribute")]
                [XmlWriterInlineData("WriteCData")]
                [XmlWriterInlineData("WriteComment")]
                [XmlWriterInlineData("WritePI")]
                [XmlWriterInlineData("WriteEntityRef")]
                [XmlWriterInlineData("WriteCharEntity")]
                [XmlWriterInlineData("WriteSurrogateCharEntity")]
                [XmlWriterInlineData("WriteWhitespace")]
                [XmlWriterInlineData("WriteString")]
                [XmlWriterInlineData("WriteChars")]
                [XmlWriterInlineData("WriteRaw")]
                [XmlWriterInlineData("WriteBase64")]
                [XmlWriterInlineData("WriteBinHex")]
                [XmlWriterInlineData("LookupPrefix")]
                [XmlWriterInlineData("WriteNmToken")]
                [XmlWriterInlineData("WriteName")]
                [XmlWriterInlineData("WriteQualifiedName")]
                [XmlWriterInlineData("WriteValue")]
                [XmlWriterInlineData("WriteAttributes")]
                [XmlWriterInlineData("WriteNodeReader")]
                [XmlWriterInlineData("Flush")]
                public void writeState_7(XmlWriterUtils utils, string methodName)
                {
                    using (XmlWriter w = utils.CreateWriter())
                    {
                        try
                        {
                            w.WriteStartDocument();
                            w.WriteStartDocument();
                        }
                        catch (InvalidOperationException)
                        {
                            CError.Equals(w.WriteState, WriteState.Error, "Error");
                            try
                            {
                                this.InvokeMethod(w, methodName);
                            }
                            catch (InvalidOperationException)
                            {
                                CError.Equals(w.WriteState, WriteState.Error, "Error");
                                try
                                {
                                    this.InvokeMethod(w, methodName);
                                }
                                catch (InvalidOperationException)
                                {
                                    return;
                                }
                            }
                            catch (ArgumentException)
                            {
                                if (utils.WriterType == WriterType.CustomWriter)
                                {
                                    CError.Equals(w.WriteState, WriteState.Error, "Error");
                                    try
                                    {
                                        this.InvokeMethod(w, methodName);
                                    }
                                    catch (ArgumentException)
                                    {
                                        return;
                                    }
                                }
                            }
                            // Flush/LookupPrefix is a NOOP
                            if (methodName == "Flush" || methodName == "LookupPrefix")
                                return;
                        }
                    }
                    Assert.Fail();
                }

                [Theory]
                [XmlWriterInlineData("XmlSpace")]
                [XmlWriterInlineData("XmlLang")]
                public void writeState_8(XmlWriterUtils utils, string what)
                {
                    using (XmlWriter w = utils.CreateWriter())
                    {
                        try
                        {
                            w.WriteStartDocument();
                            w.WriteStartDocument();
                        }
                        catch (InvalidOperationException)
                        {
                            CError.Equals(w.WriteState, WriteState.Error, "Error");
                            switch (what)
                            {
                                case "XmlSpace":
                                    CError.Equals(w.XmlSpace, XmlSpace.None, "Error");
                                    break;
                                case "XmlLang":
                                    CError.Equals(w.XmlLang, null, "Error");
                                    break;
                            }
                        }
                    }
                    return;
                }

                [Theory]
                [XmlWriterInlineData("WriteStartDocument")]
                [XmlWriterInlineData("WriteStartElement")]
                [XmlWriterInlineData("WriteEndElement")]
                [XmlWriterInlineData("WriteStartAttribute")]
                [XmlWriterInlineData("WriteEndAttribute")]
                [XmlWriterInlineData("WriteCData")]
                [XmlWriterInlineData("WriteComment")]
                [XmlWriterInlineData("WritePI")]
                [XmlWriterInlineData("WriteEntityRef")]
                [XmlWriterInlineData("WriteCharEntity")]
                [XmlWriterInlineData("WriteSurrogateCharEntity")]
                [XmlWriterInlineData("WriteWhitespace")]
                [XmlWriterInlineData("WriteString")]
                [XmlWriterInlineData("WriteChars")]
                [XmlWriterInlineData("WriteRaw")]
                [XmlWriterInlineData("WriteBase64")]
                [XmlWriterInlineData("WriteBinHex")]
                [XmlWriterInlineData("LookupPrefix")]
                [XmlWriterInlineData("WriteNmToken")]
                [XmlWriterInlineData("WriteName")]
                [XmlWriterInlineData("WriteQualifiedName")]
                [XmlWriterInlineData("WriteValue")]
                [XmlWriterInlineData("WriteAttributes")]
                [XmlWriterInlineData("WriteNodeReader")]
                [XmlWriterInlineData("Flush")]
                public void writeState_9(XmlWriterUtils utils, string methodName)
                {
                    XmlWriter w = utils.CreateWriter();
                    w.WriteElementString("root", "");
                    w.Dispose();
                    try
                    {
                        this.InvokeMethod(w, methodName);
                    }
                    catch (InvalidOperationException)
                    {
                        try
                        {
                            this.InvokeMethod(w, methodName);
                        }
                        catch (InvalidOperationException)
                        {
                            return;
                        }
                    }
                    catch (ArgumentException)
                    {
                        if (utils.WriterType == WriterType.CustomWriter)
                        {
                            try
                            {
                                this.InvokeMethod(w, methodName);
                            }
                            catch (ArgumentException)
                            {
                                return;
                            }
                        }
                    }
                    // Flush/LookupPrefix is a NOOP
                    if (methodName == "Flush" || methodName == "LookupPrefix")
                        return;

                    Assert.Fail();
                }

                private void InvokeMethod(XmlWriter w, string methodName)
                {
                    byte[] buffer = new byte[10];
                    switch (methodName)
                    {
                        case "WriteStartDocument":
                            w.WriteStartDocument();
                            break;
                        case "WriteStartElement":
                            w.WriteStartElement("root");
                            break;
                        case "WriteEndElement":
                            w.WriteEndElement();
                            break;
                        case "WriteStartAttribute":
                            w.WriteStartAttribute("attr");
                            break;
                        case "WriteEndAttribute":
                            w.WriteEndAttribute();
                            break;
                        case "WriteCData":
                            w.WriteCData("test");
                            break;
                        case "WriteComment":
                            w.WriteComment("test");
                            break;
                        case "WritePI":
                            w.WriteProcessingInstruction("name", "test");
                            break;
                        case "WriteEntityRef":
                            w.WriteEntityRef("e");
                            break;
                        case "WriteCharEntity":
                            w.WriteCharEntity('c');
                            break;
                        case "WriteSurrogateCharEntity":
                            w.WriteSurrogateCharEntity('\uDC00', '\uDBFF');
                            break;
                        case "WriteWhitespace":
                            w.WriteWhitespace(" ");
                            break;
                        case "WriteString":
                            w.WriteString("foo");
                            break;
                        case "WriteChars":
                            char[] charArray = new char[] { 'a', 'b', 'c', 'd' };
                            w.WriteChars(charArray, 0, 3);
                            break;
                        case "WriteRaw":
                            w.WriteRaw("<foo>bar</foo>");
                            break;
                        case "WriteBase64":
                            w.WriteBase64(buffer, 0, 9);
                            break;
                        case "WriteBinHex":
                            w.WriteBinHex(buffer, 0, 9);
                            break;
                        case "LookupPrefix":
                            string str = w.LookupPrefix("foo");
                            break;
                        case "WriteNmToken":
                            w.WriteNmToken("foo");
                            break;
                        case "WriteName":
                            w.WriteName("foo");
                            break;
                        case "WriteQualifiedName":
                            w.WriteQualifiedName("foo", "bar");
                            break;
                        case "WriteValue":
                            w.WriteValue(int.MaxValue);
                            break;
                        case "WriteAttributes":
                            XmlReader xr1 = ReaderHelper.Create(new StringReader("<root attr='test'/>"));
                            xr1.Read();
                            w.WriteAttributes(xr1, false);
                            break;
                        case "WriteNodeReader":
                            XmlReader xr2 = ReaderHelper.Create(new StringReader("<root/>"));
                            xr2.Read();
                            w.WriteNode(xr2, false);
                            break;
                        case "Flush":
                            w.Flush();
                            break;
                        default:
                            CError.Equals(false, "Unexpected param in testcase: {0}", methodName);
                            break;
                    }
                }
            }

            //[TestCase(Name = "NDP20_NewMethods")]
            public partial class TC_NDP20_NewMethods
            {
                // WriteElementString(prefix, name, ns, value) sanity test
                [Theory]
                [XmlWriterInlineData]
                public void var_1(XmlWriterUtils utils)
                {
                    using (XmlWriter w = utils.CreateWriter())
                    {
                        w.WriteElementString("foo", "elem", "bar", "test");
                    }
                    Assert.True(utils.CompareReader("<foo:elem xmlns:foo=\"bar\">test</foo:elem>"));
                }

                // WriteElementString(prefix = xml, ns = XML namespace)
                [Theory]
                [XmlWriterInlineData]
                public void var_2(XmlWriterUtils utils)
                {
                    using (XmlWriter w = utils.CreateWriter())
                    {
                        w.WriteElementString("xml", "elem", "http://www.w3.org/XML/1998/namespace", "test");
                    }
                    Assert.True(utils.CompareReader("<xml:elem>test</xml:elem>"));
                }

                // WriteStartAttribute(string name) sanity test
                [Theory]
                [XmlWriterInlineData]
                public void var_3(XmlWriterUtils utils)
                {
                    using (XmlWriter w = utils.CreateWriter())
                    {
                        w.WriteStartElement("elem");
                        w.WriteStartAttribute("attr");
                        w.WriteEndElement();
                    }
                    Assert.True(utils.CompareReader("<elem attr=\"\" />"));
                }

                // WriteElementString followed by attribute should error
                [Theory]
                [XmlWriterInlineData]
                public void var_4(XmlWriterUtils utils)
                {
                    using (XmlWriter w = utils.CreateWriter())
                    {
                        try
                        {
                            w.WriteElementString("foo", "elem", "bar", "test");
                            w.WriteStartAttribute("attr");
                        }
                        catch (InvalidOperationException)
                        {
                            return;
                        }
                    }

                    Assert.Fail();
                }

                // XmlWellformedWriter wrapping another XmlWriter should check the duplicate attributes first
                [Theory]
                [XmlWriterInlineData(WriterType.All & ~WriterType.Async)]
                public void var_5(XmlWriterUtils utils)
                {
                    using (XmlWriter wf = utils.CreateWriter())
                    {
                        using (XmlWriter w = WriterHelper.Create(wf, overrideAsync: true, async: utils.Async))
                        {
                            w.WriteStartElement("B");
                            w.WriteStartAttribute("aaa");
                            try
                            {
                                w.WriteStartAttribute("aaa");
                            }
                            catch (XmlException)
                            {
                                return;
                            }
                        }
                    }
                    Assert.Fail();
                }

                [Theory]
                [XmlWriterInlineData(true)]
                [XmlWriterInlineData(false)]
                public void var_6a(XmlWriterUtils utils, bool standalone)
                {
                    XmlWriterSettings ws = new XmlWriterSettings();
                    ws.ConformanceLevel = ConformanceLevel.Auto;
                    XmlWriter w = utils.CreateWriter(ws);
                    w.WriteStartDocument(standalone);
                    w.WriteStartElement("a");

                    w.Dispose();
                    string enc = (utils.WriterType == WriterType.UnicodeWriter || utils.WriterType == WriterType.UnicodeWriterIndent) ? "16" : "8";
                    string param = (standalone) ? "yes" : "no";

                    string exp = (utils.WriterType == WriterType.UTF8WriterIndent || utils.WriterType == WriterType.UnicodeWriterIndent) ?
                        string.Format("<?xml version=\"1.0\" encoding=\"utf-{0}\" standalone=\"{1}\"?>" + Environment.NewLine + "<a />", enc, param) :
                        string.Format("<?xml version=\"1.0\" encoding=\"utf-{0}\" standalone=\"{1}\"?><a />", enc, param);

                    Assert.True((utils.CompareString(exp)));
                }

                // Wrapped XmlWriter::WriteStartDocument(true) is missing standalone attribute
                [Theory]
                [XmlWriterInlineData(WriterType.All & ~WriterType.Async)]
                public void var_6b(XmlWriterUtils utils)
                {
                    XmlWriterSettings ws = new XmlWriterSettings();
                    ws.ConformanceLevel = ConformanceLevel.Auto;

                    XmlWriter wf = utils.CreateWriter(ws);
                    XmlWriter w = WriterHelper.Create(wf, overrideAsync: true, async: utils.Async);
                    w.WriteStartDocument(true);
                    w.WriteStartElement("a");

                    w.Dispose();

                    string enc = (utils.WriterType == WriterType.UnicodeWriter || utils.WriterType == WriterType.UnicodeWriterIndent) ? "16" : "8";
                    string exp = (utils.WriterType == WriterType.UTF8WriterIndent || utils.WriterType == WriterType.UnicodeWriterIndent) ?
                        string.Format("<?xml version=\"1.0\" encoding=\"utf-{0}\"?>" + Environment.NewLine + "<a />", enc) :
                        string.Format("<?xml version=\"1.0\" encoding=\"utf-{0}\"?><a />", enc);

                    exp = (utils.WriterType == WriterType.CustomWriter) ? "<?xml version=\"1.0\" encoding=\"utf-8\" standalone=\"yes\"?><a />" : exp;

                    Assert.True((utils.CompareString(exp)));
                }
            }

            //[TestCase(Name = "Globalization")]
            public partial class TCGlobalization
            {
                // Characters between 0xdfff and 0xfffe are valid Unicode characters
                [Theory]
                [XmlWriterInlineData]
                public void var_1(XmlWriterUtils utils)
                {
                    string UniStr = "";
                    using (XmlWriter w = utils.CreateWriter())
                    {
                        for (char ch = '\ue000'; ch < '\ufffe'; ch++)
                            UniStr += ch;
                        w.WriteElementString("root", UniStr);
                    }

                    Assert.True(utils.CompareReader("<root>" + UniStr + "</root>"));
                }

                [Fact]
                public void XmlWriterUsingUtf16BEWritesCorrectEncodingInTheXmlDecl()
                {
                    Encoding enc = Encoding.GetEncoding("UTF-16BE");
                    Assert.NotNull(enc);

                    using (var ms = new MemoryStream())
                    {
                        var settings = new XmlWriterSettings();
                        settings.Encoding = enc;

                        using (XmlWriter writer = XmlWriter.Create(ms, settings))
                        {
                            writer.WriteStartDocument();
                            writer.WriteElementString("A", "value");
                            writer.WriteEndDocument();
                        }

                        ms.Position = 0;
                        StreamReader sr = new StreamReader(ms);
                        string str = sr.ReadToEnd();
                        CError.WriteLine(str);
                        Assert.Equal("<?xml version=\"1.0\" encoding=\"utf-16BE\"?><A>value</A>", str);
                    }
                }
            }

            //[TestCase(Name = "Close()")]
            public partial class TCClose
            {
                // Closing an XmlWriter should close all opened elements
                [Theory]
                [XmlWriterInlineData]
                public void var_1(XmlWriterUtils utils)
                {
                    XmlWriter writer = utils.CreateWriter();
                    writer.WriteStartElement("Root");
                    writer.WriteStartElement("Nesting");
                    writer.WriteStartElement("SomeDeep");
                    writer.Close();

                    Assert.True(utils.CompareReader("<Root><Nesting><SomeDeep /></Nesting></Root>"));
                }

                // Disposing an XmlWriter should close all opened elements
                [Theory]
                [XmlWriterInlineData]
                public void var_2(XmlWriterUtils utils)
                {
                    using (XmlWriter writer = utils.CreateWriter())
                    {
                        writer.WriteStartElement("Root");
                        writer.WriteStartElement("Nesting");
                        writer.WriteStartElement("SomeDeep");
                    }
                    Assert.True(utils.CompareReader("<Root><Nesting><SomeDeep /></Nesting></Root>"));
                }

                // Dispose() shouldn't throw when a tag is not closed and inner stream is closed
                [Theory]
                [XmlWriterInlineData(WriterType.All & ~WriterType.Async)]
                public void var_3(XmlWriterUtils utils)
                {
                    XmlWriter w;
                    StringWriter sw = new StringWriter(CultureInfo.InvariantCulture);
                    XmlWriterSettings s = new XmlWriterSettings();


                    switch (utils.WriterType)
                    {
                        case WriterType.UnicodeWriter:
                            s.Encoding = Encoding.Unicode;
                            w = WriterHelper.Create(sw, s, overrideAsync: true, async: utils.Async);
                            break;
                        case WriterType.UTF8Writer:
                            s.Encoding = Encoding.UTF8;
                            w = WriterHelper.Create(sw, s, overrideAsync: true, async: utils.Async);
                            break;
                        case WriterType.WrappedWriter:
                            XmlWriter ww = WriterHelper.Create(sw, s, overrideAsync: true, async: utils.Async);
                            w = WriterHelper.Create(ww, s, overrideAsync: true, async: utils.Async);
                            break;
                        case WriterType.CharCheckingWriter:
                            s.CheckCharacters = false;
                            XmlWriter w1 = WriterHelper.Create(sw, s, overrideAsync: true, async: utils.Async);
                            XmlWriterSettings ws2 = new XmlWriterSettings();
                            ws2.CheckCharacters = true;
                            w = WriterHelper.Create(w1, ws2, overrideAsync: true, async: utils.Async);
                            break;
                        case WriterType.UnicodeWriterIndent:
                            s.Encoding = Encoding.Unicode;
                            s.Indent = true;
                            w = WriterHelper.Create(sw, s, overrideAsync: true, async: utils.Async);
                            break;
                        case WriterType.UTF8WriterIndent:
                            s.Encoding = Encoding.UTF8;
                            s.Indent = true;
                            w = WriterHelper.Create(sw, s, overrideAsync: true, async: utils.Async);
                            break;
                        default:
                            return;
                    }

                    w.WriteStartElement("root");

                    ((IDisposable)sw).Dispose();
                    sw = null;
                    try
                    {
                        ((IDisposable)w).Dispose();
                    }
                    catch (ObjectDisposedException e) { CError.WriteLine(e.Message); return; }
                    Assert.Fail();
                }

                // Close() should be allowed when XML doesn't have content
                [Theory]
                [XmlWriterInlineData]
                public void var_4(XmlWriterUtils utils)
                {
                    XmlWriter w = utils.CreateWriter();
                    w.Dispose();

                    try
                    {
                        utils.CompareReader("");
                    }
                    catch (XmlException e)
                    {
                        CError.WriteLine(e.Message);
                        if (e.Message.EndsWith(".."))
                        {
                            Assert.Fail();
                        }
                        Assert.Fail();
                    }
                    return;
                }

                [Theory]
                [XmlWriterInlineData(WriterType.UnicodeWriterIndent | WriterType.UTF8WriterIndent)]
                public void SettingIndetingAllowsIndentingWhileWritingBase64(XmlWriterUtils utils)
                {
                    string base64test = "abcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyz";
                    byte[] bytesToWrite = Encoding.Unicode.GetBytes(base64test.ToCharArray());

                    using (XmlWriter writer = utils.CreateWriter())
                    {
                        writer.WriteStartDocument();
                        writer.WriteStartElement("Root");
                        writer.WriteStartElement("WB64");
                        writer.WriteBase64(bytesToWrite, 0, bytesToWrite.Length);
                        writer.WriteEndElement();

                        writer.WriteStartElement("WBC64");
                        writer.WriteString(Convert.ToBase64String(bytesToWrite));
                        writer.WriteEndElement();
                        writer.WriteEndElement();
                        writer.WriteEndDocument();
                    }

                    string xml = utils.GetString();

                    var readerSettings = new XmlReaderSettings()
                    {
                        IgnoreWhitespace = false
                    };

                    using (StringReader sr = new StringReader(xml))
                    using (XmlReader reader = XmlReader.Create(sr, readerSettings))
                    {
                        reader.ReadToFollowing("WB64");
                        Assert.Equal("WB64", reader.LocalName);
                        string one = reader.ReadInnerXml();

                        Assert.Equal(XmlNodeType.Whitespace, reader.NodeType);
                        reader.Read();

                        Assert.Equal("WBC64", reader.LocalName);
                        string two = reader.ReadInnerXml();

                        Assert.Equal(one, two);
                    }
                }

                //[Variation("WriteState returns Content even though document element has been closed")]
                [Theory]
                [XmlWriterInlineData]
                public void WriteStateReturnsContentAfterDocumentClosed(XmlWriterUtils utils)
                {
                    XmlWriter xw = utils.CreateWriter();
                    xw.WriteStartDocument(false);
                    xw.WriteStartElement("foo");
                    xw.WriteString("bar");
                    xw.WriteEndElement();

                    try
                    {
                        xw.WriteStartElement("foo2");
                        xw.Dispose();
                    }
                    catch (System.InvalidOperationException)
                    {
                        return;
                    }
                    Assert.Fail();
                }
            }
        }
    }
}
