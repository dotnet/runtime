// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using OLEDB.Test.ModuleCore;
using XmlCoreTest.Common;
using Xunit;

namespace System.Xml.Tests
{
    public class TCAttribute
    {
        // Sanity test for WriteAttribute
        [Theory]
        [XmlWriterInlineData]
        public void attribute_1(XmlWriterUtils utils)
        {
            using (XmlWriter w = utils.CreateWriter())
            {
                w.WriteStartElement("Root");
                w.WriteStartAttribute("attr1");
                w.WriteEndAttribute();
                w.WriteAttributeString("attr2", "val2");
                w.WriteEndElement();
            }
            Assert.True(utils.CompareReader("<Root attr1=\"\" attr2=\"val2\" />"));
        }

        // Missing EndAttribute should be fixed
        [Theory]
        [XmlWriterInlineData]
        public void attribute_2(XmlWriterUtils utils)
        {
            using (XmlWriter w = utils.CreateWriter())
            {
                w.WriteStartElement("Root");
                w.WriteStartAttribute("attr1");
                w.WriteEndElement();
            }
            Assert.True(utils.CompareReader("<Root attr1=\"\" />"));
        }

        // WriteStartAttribute followed by WriteStartAttribute
        [Theory]
        [XmlWriterInlineData]
        public void attribute_3(XmlWriterUtils utils)
        {
            using (XmlWriter w = utils.CreateWriter())
            {
                w.WriteStartElement("Root");
                w.WriteStartAttribute("attr1");
                w.WriteStartAttribute("attr2");
                w.WriteEndElement();
            }
            Assert.True(utils.CompareReader("<Root attr1=\"\" attr2=\"\" />"));
        }

        // Multiple WritetAttributeString
        [Theory]
        [XmlWriterInlineData]
        public void attribute_4(XmlWriterUtils utils)
        {
            using (XmlWriter w = utils.CreateWriter())
            {
                w.WriteStartElement("Root");
                w.WriteAttributeString("attr1", "val1");
                w.WriteAttributeString("attr2", "val2");
                w.WriteEndElement();
            }
            Assert.True(utils.CompareReader("<Root attr1=\"val1\" attr2=\"val2\" />"));
        }

        // WriteStartAttribute followed by WriteString
        [Theory]
        [XmlWriterInlineData]
        public void attribute_5(XmlWriterUtils utils)
        {
            using (XmlWriter w = utils.CreateWriter())
            {
                w.WriteStartElement("Root");
                w.WriteStartAttribute("attr1");
                w.WriteString("test");
                w.WriteEndElement();
            }
            Assert.True(utils.CompareReader("<Root attr1=\"test\" />"));
        }

        // Sanity test for overload WriteStartAttribute(name, ns)
        [Theory]
        [XmlWriterInlineData]
        public void attribute_6(XmlWriterUtils utils)
        {
            using (XmlWriter w = utils.CreateWriter())
            {
                w.WriteStartElement("Root");
                w.WriteStartAttribute("attr1", "http://my.com");
                w.WriteEndAttribute();
                w.WriteEndElement();
            }
            Assert.True(utils.CompareString("<Root ~a p1 a~:attr1=\"\" xmlns:~a p1 A~=\"http://my.com\" />"));
        }

        // Sanity test for overload WriteStartAttribute(prefix, name, ns)
        [Theory]
        [XmlWriterInlineData]
        public void attribute_7(XmlWriterUtils utils)
        {
            using (XmlWriter w = utils.CreateWriter())
            {
                w.WriteStartElement("Root");
                w.WriteStartAttribute("pre1", "attr1", "http://my.com");
                w.WriteEndAttribute();
                w.WriteEndElement();
            }
            Assert.True(utils.CompareReader("<Root pre1:attr1=\"\" xmlns:pre1=\"http://my.com\" />"));
        }

        // DCR 64183: Duplicate attribute 'attr1'
        [Theory]
        [XmlWriterInlineData]
        public void attribute_8(XmlWriterUtils utils)
        {
            using (XmlWriter w = utils.CreateWriter())
            {
                try
                {
                    w.WriteStartElement("Root");
                    w.WriteStartAttribute("attr1");
                    w.WriteStartAttribute("attr1");
                }
                catch (XmlException e)
                {
                    CError.WriteLineIgnore("Exception: " + e.ToString());
                    CError.Compare(w.WriteState, WriteState.Error, "WriteState should be Error");
                    return;
                }
            }
            CError.WriteLine("Did not throw exception");
            Assert.True(false);
        }

        // Duplicate attribute 'ns1:attr1'
        [Theory]
        [XmlWriterInlineData]
        public void attribute_9(XmlWriterUtils utils)
        {
            using (XmlWriter w = utils.CreateWriter())
            {
                try
                {
                    w.WriteStartElement("Root");
                    w.WriteStartAttribute("ns1", "attr1", "http://my.com");
                    w.WriteStartAttribute("ns1", "attr1", "http://my.com");
                }
                catch (XmlException e)
                {
                    CError.WriteLineIgnore("Exception: " + e.ToString());
                    CError.Compare(w.WriteState, WriteState.Error, "WriteState should be Error");
                    return;
                }
            }
            CError.WriteLine("Did not throw exception");
            Assert.True(false);
        }

        // Attribute name = String.Empty should error
        [Theory]
        [XmlWriterInlineData]
        public void attribute_10(XmlWriterUtils utils)
        {
            using (XmlWriter w = utils.CreateWriter())
            {
                try
                {
                    w.WriteStartElement("Root");
                    w.WriteStartAttribute(string.Empty);
                }
                catch (ArgumentException e)
                {
                    CError.WriteLineIgnore("Exception: " + e.ToString());
                    CError.Compare(w.WriteState, (utils.WriterType == WriterType.CharCheckingWriter) ? WriteState.Element : WriteState.Error, "WriteState should be Error");
                    return;
                }
            }
            CError.WriteLine("Did not throw exception");
            Assert.True(false);
        }

        // Attribute name = null
        [Theory]
        [XmlWriterInlineData]
        public void attribute_11(XmlWriterUtils utils)
        {
            using (XmlWriter w = utils.CreateWriter())
            {
                try
                {
                    w.WriteStartElement("Root");
                    w.WriteStartAttribute(null);
                }
                catch (ArgumentException e)
                {
                    CError.WriteLineIgnore("Exception: " + e.ToString());
                    CError.Compare(w.WriteState, (utils.WriterType == WriterType.CharCheckingWriter) ? WriteState.Element : WriteState.Error, "WriteState should be Error");
                    return;
                }
            }
            CError.WriteLine("Did not throw exception");
            Assert.True(false);
        }

        // WriteAttribute with names Foo, fOo, foO, FOO
        [Theory]
        [XmlWriterInlineData]
        public void attribute_12(XmlWriterUtils utils)
        {
            using (XmlWriter w = utils.CreateWriter())
            {
                string[] attrNames = { "Foo", "fOo", "foO", "FOO" };
                w.WriteStartElement("Root");
                for (int i = 0; i < attrNames.Length; i++)
                {
                    w.WriteAttributeString(attrNames[i], "x");
                }
                w.WriteEndElement();
            }
            Assert.True(utils.CompareReader("<Root Foo=\"x\" fOo=\"x\" foO=\"x\" FOO=\"x\" />"));
        }

        // Invalid value of xml:space
        [Theory]
        [XmlWriterInlineData]
        public void attribute_13(XmlWriterUtils utils)
        {
            using (XmlWriter w = utils.CreateWriter())
            {
                try
                {
                    w.WriteStartElement("Root");
                    w.WriteAttributeString("xml", "space", "http://www.w3.org/XML/1998/namespace", "invalid");
                }
                catch (ArgumentException e)
                {
                    CError.WriteLineIgnore("Exception: " + e.ToString());
                    CError.Compare(w.WriteState, WriteState.Error, "WriteState should be Error");
                    return;
                }
            }
            CError.WriteLine("Did not throw exception");
            Assert.True(false);
        }

        // SingleQuote in attribute value should be allowed
        [Theory]
        [XmlWriterInlineData]
        public void attribute_14(XmlWriterUtils utils)
        {
            using (XmlWriter w = utils.CreateWriter())
            {
                w.WriteStartElement("Root");
                w.WriteAttributeString("a", null, "b'c");
                w.WriteEndElement();
            }
            Assert.True(utils.CompareReader("<Root a=\"b'c\" />"));
        }

        // DoubleQuote in attribute value should be escaped
        [Theory]
        [XmlWriterInlineData]
        public void attribute_15(XmlWriterUtils utils)
        {
            using (XmlWriter w = utils.CreateWriter())
            {
                w.WriteStartElement("Root");
                w.WriteAttributeString("a", null, "b\"c");
                w.WriteEndElement();
            }
            Assert.True(utils.CompareReader("<Root a=\"b&quot;c\" />"));
        }

        // WriteAttribute with value = &, #65, #x20
        [Theory]
        [XmlWriterInlineData]
        public void attribute_16(XmlWriterUtils utils)
        {
            using (XmlWriter w = utils.CreateWriter())
            {
                w.WriteStartElement("Root");
                w.WriteAttributeString("a", "&");
                w.WriteAttributeString("b", "&#65;");
                w.WriteAttributeString("c", "&#x43;");
                w.WriteEndElement();
            }
            Assert.True(utils.CompareReader("<Root a=\"&amp;\" b=\"&amp;#65;\" c=\"&amp;#x43;\" />"));
        }

        // WriteAttributeString followed by WriteString
        [Theory]
        [XmlWriterInlineData]
        public void attribute_17(XmlWriterUtils utils)
        {
            using (XmlWriter w = utils.CreateWriter())
            {
                w.WriteStartElement("Root");
                w.WriteAttributeString("a", null, "b");
                w.WriteString("test");
                w.WriteEndElement();
            }
            Assert.True(utils.CompareReader("<Root a=\"b\">test</Root>"));
        }

        // WriteAttribute followed by WriteString
        [Theory]
        [XmlWriterInlineData]
        public void attribute_18(XmlWriterUtils utils)
        {
            using (XmlWriter w = utils.CreateWriter())
            {
                w.WriteStartElement("Root");
                w.WriteStartAttribute("a");
                w.WriteString("test");
                w.WriteEndElement();
            }
            Assert.True(utils.CompareReader("<Root a=\"test\" />"));
        }

        // WriteAttribute with all whitespace characters
        [Theory]
        [XmlWriterInlineData]
        public void attribute_19(XmlWriterUtils utils)
        {
            using (XmlWriter w = utils.CreateWriter())
            {
                w.WriteStartElement("Root");
                w.WriteAttributeString("a", null, "\x20\x9\xD\xA");
                w.WriteEndElement();
            }

            Assert.True(utils.CompareReader("<Root a=\" &#x9;&#xD;&#xA;\" />"));
        }

        // < > & chars should be escaped in attribute value
        [Theory]
        [XmlWriterInlineData]
        public void attribute_20(XmlWriterUtils utils)
        {
            using (XmlWriter w = utils.CreateWriter())
            {
                w.WriteStartElement("Root");
                w.WriteAttributeString("a", null, "< > &");
                w.WriteEndElement();
            }
            Assert.True(utils.CompareReader("<Root a=\"&lt; &gt; &amp;\" />"));
        }

        // Redefine auto generated prefix n1
        [Theory]
        [XmlWriterInlineData]
        public void attribute_21(XmlWriterUtils utils)
        {
            using (XmlWriter w = utils.CreateWriter())
            {
                w.WriteStartElement("test");
                w.WriteAttributeString("xmlns", "n1", null, "http://testbasens");
                w.WriteStartElement("base");
                w.WriteAttributeString("id", "http://testbasens", "5");
                w.WriteAttributeString("lang", "http://common", "en");
                w.WriteEndElement();
                w.WriteEndElement();
            }
            string exp = utils.IsIndent() ?
                "<test xmlns:n1=\"http://testbasens\">" + Environment.NewLine + "  <base n1:id=\"5\" p4:lang=\"en\" xmlns:p4=\"http://common\" />" + Environment.NewLine + "</test>" :
                "<test xmlns:~f n1 A~=\"http://testbasens\"><base ~f n1 a~:id=\"5\" ~a p4 a~:lang=\"en\" xmlns:~a p4 A~=\"http://common\" /></test>";
            Assert.True(utils.CompareString(exp));
        }

        // Reuse and redefine existing prefix
        [Theory]
        [XmlWriterInlineData]
        public void attribute_22(XmlWriterUtils utils)
        {
            string exp = "<test ~f p a~:a1=\"v\" xmlns:~f p A~=\"ns1\"><base ~f p b~:a2=\"v\" ~a p4 ab~:a3=\"v\" xmlns:~a p4 AB~=\"ns2\" /></test>";

            using (XmlWriter w = utils.CreateWriter())
            {
                w.WriteStartElement("test");
                w.WriteAttributeString("p", "a1", "ns1", "v");
                w.WriteStartElement("base");
                w.WriteAttributeString("a2", "ns1", "v");
                w.WriteAttributeString("p", "a3", "ns2", "v");
                w.WriteEndElement();
                w.WriteEndElement();
            }
            exp = utils.IsIndent() ?
                "<test p:a1=\"v\" xmlns:p=\"ns1\">" + Environment.NewLine + "  <base p:a2=\"v\" p4:a3=\"v\" xmlns:p4=\"ns2\" />" + Environment.NewLine + "</test>" : exp;
            Assert.True(utils.CompareString(exp));
        }

        // WriteStartAttribute(attr) sanity test
        [Theory]
        [XmlWriterInlineData]
        public void attribute_23(XmlWriterUtils utils)
        {
            using (XmlWriter w = utils.CreateWriter())
            {
                w.WriteStartElement("test");
                w.WriteStartAttribute("attr");
                w.WriteEndAttribute();
                w.WriteEndElement();
            }
            Assert.True(utils.CompareReader("<test attr=\"\" />"));
        }

        // WriteStartAttribute(attr) inside an element with changed default namespace
        [Theory]
        [XmlWriterInlineData]
        public void attribute_24(XmlWriterUtils utils)
        {
            using (XmlWriter w = utils.CreateWriter())
            {
                w.WriteStartElement(string.Empty, "test", "ns");
                w.WriteStartAttribute("attr");
                w.WriteEndAttribute();
                w.WriteEndElement();
            }
            Assert.True(utils.CompareReader("<test attr=\"\" xmlns=\"ns\" />"));
        }

        // WriteStartAttribute(attr) and duplicate attrs
        [Theory]
        [XmlWriterInlineData]
        public void attribute_25(XmlWriterUtils utils)
        {
            using (XmlWriter w = utils.CreateWriter())
            {
                try
                {
                    w.WriteStartElement("test");
                    w.WriteStartAttribute(null, "attr", null);
                    w.WriteStartAttribute("attr");
                }
                catch (XmlException e)
                {
                    CError.WriteLineIgnore("Exception: " + e.ToString());
                    return;
                }
            }
            CError.WriteLine("Did not throw error for duplicate attrs");
            Assert.True(false);
        }

        // WriteStartAttribute(attr) when element has ns:attr
        [Theory]
        [XmlWriterInlineData]
        public void attribute_26(XmlWriterUtils utils)
        {
            using (XmlWriter w = utils.CreateWriter())
            {
                w.WriteStartElement("pre", "test", "ns");
                w.WriteStartAttribute(null, "attr", "ns");
                w.WriteStartAttribute("attr");
                w.WriteEndElement();
            }
            Assert.True(utils.CompareReader("<pre:test pre:attr=\"\" attr=\"\" xmlns:pre=\"ns\" />"));
        }

        // XmlCharCheckingWriter should not normalize newLines in attribute values when NewLinesHandling = Replace
        [Theory]
        [XmlWriterInlineData]
        public void attribute_27(XmlWriterUtils utils)
        {
            XmlWriterSettings s = new XmlWriterSettings();
            s.NewLineHandling = NewLineHandling.Replace;

            using (XmlWriter w = utils.CreateWriter())
            {
                w.WriteStartElement("root");
                w.WriteAttributeString("a", "|\x0D|\x0A|\x0D\x0A|");
                w.WriteEndElement();
            }
            Assert.True(utils.CompareReader("<root a=\"|&#xD;|&#xA;|&#xD;&#xA;|\" />"));
        }

        // Wrapped XmlTextWriter: Invalid replacement of newline characters in text values
        [Theory]
        [XmlWriterInlineData]
        public void attribute_28(XmlWriterUtils utils)
        {
            XmlWriterSettings s = new XmlWriterSettings();
            s.NewLineHandling = NewLineHandling.Replace;

            using (XmlWriter w = utils.CreateWriter())
            {
                w.WriteStartElement("root");
                w.WriteAttributeString("a", "|\x0D\x0A|");
                w.WriteElementString("foo", "|\x0D\x0A|");
                w.WriteEndElement();
            }
            Assert.True(utils.CompareReader("<root a=\"|&#xD;&#xA;|\"><foo>|\x0D\x0A|</foo></root>"));
        }

        // WriteAttributeString doesn't fail on invalid surrogate pair sequences
        [Theory]
        [XmlWriterInlineData]
        public void attribute_29(XmlWriterUtils utils)
        {
            using (XmlWriter w = utils.CreateWriter())
            {
                w.WriteStartElement("root");
                try
                {
                    w.WriteAttributeString("attribute", "\ud800\ud800");
                }
                catch (ArgumentException e)
                {
                    CError.WriteLineIgnore(e.ToString());
                    return;
                }
            }
            Assert.True(false);
        }
    }
}
