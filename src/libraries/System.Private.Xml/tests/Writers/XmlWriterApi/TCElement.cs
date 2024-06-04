// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using OLEDB.Test.ModuleCore;
using XmlCoreTest.Common;
using Xunit;

namespace System.Xml.XmlWriterApiTests
{
    //[TestCase(Name = "WriteStart/EndElement")]
    public class TCElement
    {
        // StartElement-EndElement Sanity Test
        [Theory]
        [XmlWriterInlineData]
        public void element_1(XmlWriterUtils utils)
        {
            using (XmlWriter w = utils.CreateWriter())
            {
                w.WriteStartElement("Root");
                w.WriteEndElement();
            }
            Assert.True(utils.CompareReader("<Root />"));
        }

        // Sanity test for overload WriteStartElement(string prefix, string name, string ns)
        [Theory]
        [XmlWriterInlineData]
        public void element_2(XmlWriterUtils utils)
        {
            using (XmlWriter w = utils.CreateWriter())
            {
                w.WriteStartElement("pre1", "Root", "http://my.com");
                w.WriteEndElement();
            }
            Assert.True(utils.CompareReader("<pre1:Root xmlns:pre1=\"http://my.com\" />"));
        }

        // Sanity test for overload WriteStartElement(string name, string ns)
        [Theory]
        [XmlWriterInlineData]
        public void element_3(XmlWriterUtils utils)
        {
            using (XmlWriter w = utils.CreateWriter())
            {
                w.WriteStartElement("Root", "http://my.com");
                w.WriteEndElement();
            }
            Assert.True(utils.CompareReader("<Root xmlns=\"http://my.com\" />"));
        }

        // Element name = String.Empty should error
        [Theory]
        [XmlWriterInlineData]
        public void element_4(XmlWriterUtils utils)
        {
            using (XmlWriter w = utils.CreateWriter())
            {
                try
                {
                    w.WriteStartElement(string.Empty);
                }
                catch (ArgumentException e)
                {
                    CError.WriteLineIgnore("Exception: " + e.ToString());
                    CError.Compare(w.WriteState, (utils.WriterType == WriterType.CharCheckingWriter) ? WriteState.Start : WriteState.Error, "WriteState should be Error");
                    return;
                }
            }
            CError.WriteLine("Did not throw exception");
            Assert.Fail();
        }

        // Element name = null should error
        [Theory]
        [XmlWriterInlineData]
        public void element_5(XmlWriterUtils utils)
        {
            using (XmlWriter w = utils.CreateWriter())
            {
                try
                {
                    w.WriteStartElement(null);
                }
                catch (ArgumentException e)
                {
                    CError.WriteLineIgnore("Exception: " + e.ToString());
                    CError.Compare(w.WriteState, (utils.WriterType == WriterType.CharCheckingWriter) ? WriteState.Start : WriteState.Error, "WriteState should be Error");
                    return;
                }
            }
            CError.WriteLine("Did not throw exception");
            Assert.Fail();
        }

        // Element NS = String.Empty
        [Theory]
        [XmlWriterInlineData]
        public void element_6(XmlWriterUtils utils)
        {
            using (XmlWriter w = utils.CreateWriter())
            {
                w.WriteStartElement("Root", string.Empty);
                w.WriteEndElement();
            }
            Assert.True(utils.CompareReader("<Root />"));
        }

        // Element NS = null
        [Theory]
        [XmlWriterInlineData]
        public void element_7(XmlWriterUtils utils)
        {
            using (XmlWriter w = utils.CreateWriter())
            {
                w.WriteStartElement("Root", null);
                w.WriteEndElement();
            }
            Assert.True(utils.CompareReader("<Root />"));
        }

        // Write 100 nested elements
        [Theory]
        [XmlWriterInlineData]
        public void element_8(XmlWriterUtils utils)
        {
            using (XmlWriter w = utils.CreateWriter())
            {
                for (int i = 0; i < 100; i++)
                {
                    string eName = "Node" + i.ToString();
                    w.WriteStartElement(eName);
                }
                for (int i = 0; i < 100; i++)
                    w.WriteEndElement();
            }

            string exp = (utils.WriterType == WriterType.UTF8WriterIndent || utils.WriterType == WriterType.UnicodeWriterIndent) ?
                "100ElementsIndent.txt" : "100Elements.txt";
            Assert.True(utils.CompareBaseline(exp));
        }

        // WriteDecl with start element with prefix and namespace
        [Theory]
        [XmlWriterInlineData]
        public void element_9(XmlWriterUtils utils)
        {
            string enc = (utils.WriterType == WriterType.UnicodeWriter || utils.WriterType == WriterType.UnicodeWriterIndent) ? "16" : "8";
            string exp = (utils.WriterType == WriterType.UTF8WriterIndent || utils.WriterType == WriterType.UnicodeWriterIndent) ?
                string.Format("<?xml version=\"1.0\" encoding=\"utf-{0}\"?>" + Environment.NewLine + "<a:b xmlns:a=\"c\" />", enc) :
                string.Format("<?xml version=\"1.0\" encoding=\"utf-{0}\"?><a:b xmlns:a=\"c\" />", enc);

            XmlWriterSettings ws = new XmlWriterSettings();
            ws.OmitXmlDeclaration = false;
            using (XmlWriter w = utils.CreateWriter(ws))
            {
                w.WriteStartElement("a", "b", "c");
            }

            Assert.True((utils.CompareString(exp)));
        }

        [Theory]
        [XmlWriterInlineData(true)]
        [XmlWriterInlineData(false)]
        public void element_10(XmlWriterUtils utils, bool defattr)
        {
            string xml = "<a p1:a=\"\" p2:a=\"\" p3:a=\"\" p4:a=\"\" p5:a=\"\" p6:a=\"\" p7:a=\"\" p8:a=\"\" p9:a=\"\" p10:a=\"\" p11:a=\"\" p12:a=\"\" p13:a=\"\" p14:a=\"\" p15:a=\"\" p16:a=\"\" p17:a=\"\" p18:a=\"\" p19:a=\"\" p20:a=\"\" p21:a=\"\" p22:a=\"\" p23:a=\"\" p24:a=\"\" p25:a=\"\" p26:a=\"\" p27:a=\"\" p28:a=\"\" p29:a=\"\" p30:a=\"\" p31:a=\"\" p32:a=\"\" p33:a=\"\" p34:a=\"\" p35:a=\"\" p36:a=\"\" p37:a=\"\" p38:a=\"\" p39:a=\"\" p40:a=\"\" p41:a=\"\" p42:a=\"\" p43:a=\"\" p44:a=\"\" p45:a=\"\" p46:a=\"\" p47:a=\"\" p48:a=\"\" p49:a=\"\" p50:a=\"\" p51:a=\"\" p52:a=\"\" p53:a=\"\" p54:a=\"\" p55:a=\"\" p56:a=\"\" p57:a=\"\" p58:a=\"\" p59:a=\"\" p60:a=\"\" p61:a=\"\" p62:a=\"\" p63:a=\"\" p64:a=\"\" p65:a=\"\" p66:a=\"\" p67:a=\"\" p68:a=\"\" p69:a=\"\" p70:a=\"\" p71:a=\"\" p72:a=\"\" p73:a=\"\" p74:a=\"\" p75:a=\"\" p76:a=\"\" p77:a=\"\" p78:a=\"\" p79:a=\"\" p80:a=\"\" p81:a=\"\" p82:a=\"\" p83:a=\"\" p84:a=\"\" p85:a=\"\" p86:a=\"\" p87:a=\"\" p88:a=\"\" p89:a=\"\" p90:a=\"\" p91:a=\"\" p92:a=\"\" p93:a=\"\" p94:a=\"\" p95:a=\"\" p96:a=\"\" p97:a=\"\" p98:a=\"\" p99:a=\"\" p100:a=\"\" xmlns:p100=\"b99\" xmlns:p99=\"b98\" xmlns:p98=\"b97\" xmlns:p97=\"b96\" xmlns:p96=\"b95\" xmlns:p95=\"b94\" xmlns:p94=\"b93\" xmlns:p93=\"b92\" xmlns:p92=\"b91\" xmlns:p91=\"b90\" xmlns:p90=\"b89\" xmlns:p89=\"b88\" xmlns:p88=\"b87\" xmlns:p87=\"b86\" xmlns:p86=\"b85\" xmlns:p85=\"b84\" xmlns:p84=\"b83\" xmlns:p83=\"b82\" xmlns:p82=\"b81\" xmlns:p81=\"b80\" xmlns:p80=\"b79\" xmlns:p79=\"b78\" xmlns:p78=\"b77\" xmlns:p77=\"b76\" xmlns:p76=\"b75\" xmlns:p75=\"b74\" xmlns:p74=\"b73\" xmlns:p73=\"b72\" xmlns:p72=\"b71\" xmlns:p71=\"b70\" xmlns:p70=\"b69\" xmlns:p69=\"b68\" xmlns:p68=\"b67\" xmlns:p67=\"b66\" xmlns:p66=\"b65\" xmlns:p65=\"b64\" xmlns:p64=\"b63\" xmlns:p63=\"b62\" xmlns:p62=\"b61\" xmlns:p61=\"b60\" xmlns:p60=\"b59\" xmlns:p59=\"b58\" xmlns:p58=\"b57\" xmlns:p57=\"b56\" xmlns:p56=\"b55\" xmlns:p55=\"b54\" xmlns:p54=\"b53\" xmlns:p53=\"b52\" xmlns:p52=\"b51\" xmlns:p51=\"b50\" xmlns:p50=\"b49\" xmlns:p49=\"b48\" xmlns:p48=\"b47\" xmlns:p47=\"b46\" xmlns:p46=\"b45\" xmlns:p45=\"b44\" xmlns:p44=\"b43\" xmlns:p43=\"b42\" xmlns:p42=\"b41\" xmlns:p41=\"b40\" xmlns:p40=\"b39\" xmlns:p39=\"b38\" xmlns:p38=\"b37\" xmlns:p37=\"b36\" xmlns:p36=\"b35\" xmlns:p35=\"b34\" xmlns:p34=\"b33\" xmlns:p33=\"b32\" xmlns:p32=\"b31\" xmlns:p31=\"b30\" xmlns:p30=\"b29\" xmlns:p29=\"b28\" xmlns:p28=\"b27\" xmlns:p27=\"b26\" xmlns:p26=\"b25\" xmlns:p25=\"b24\" xmlns:p24=\"b23\" xmlns:p23=\"b22\" xmlns:p22=\"b21\" xmlns:p21=\"b20\" xmlns:p20=\"b19\" xmlns:p19=\"b18\" xmlns:p18=\"b17\" xmlns:p17=\"b16\" xmlns:p16=\"b15\" xmlns:p15=\"b14\" xmlns:p14=\"b13\" xmlns:p13=\"b12\" xmlns:p12=\"b11\" xmlns:p11=\"b10\" xmlns:p10=\"b9\" xmlns:p9=\"b8\" xmlns:p8=\"b7\" xmlns:p7=\"b6\" xmlns:p6=\"b5\" xmlns:p5=\"b4\" xmlns:p4=\"b3\" xmlns:p3=\"b2\" xmlns:p2=\"b1\" xmlns:p1=\"b0\" />";
            XmlReader r = ReaderHelper.Create(new StringReader(xml));
            using (XmlWriter w = utils.CreateWriter())
            {
                w.WriteNode(r, defattr);
            }

            Assert.True((utils.CompareString(xml)));
        }

        // Write many attributes and dup namespace
        [Theory]
        [XmlWriterInlineData]
        public void element_10a(XmlWriterUtils utils)
        {
            XmlWriter w = utils.CreateWriter();
            w.WriteDocType("a", null, null, "<!ATTLIST oot a CDATA #IMPLIED>");
            w.WriteStartElement("Root");
            for (int i = 0; i < 200; i++)
            {
                w.WriteAttributeString("a", "n" + i, "val");
            }
            try
            {
                w.WriteAttributeString("a", "n" + 199, "val");
            }
            catch (XmlException) { return; }
            finally
            {
                w.Dispose();
            }
            Assert.Fail();
        }


        // Write many attributes and dup name
        [Theory]
        [XmlWriterInlineData]
        public void element_10b(XmlWriterUtils utils)
        {
            XmlWriter w = utils.CreateWriter();
            w.WriteDocType("a", null, null, "<!ATTLIST Root a CDATA #FIXED \"val\">");
            w.WriteStartElement("Root");
            for (int i = 0; i < 200; i++)
            {
                w.WriteAttributeString("a" + i, "val");
            }
            try
            {
                w.WriteAttributeString("a" + 199, "val");
            }
            catch (XmlException) { return; }
            finally
            {
                w.Dispose();
            }
            Assert.Fail();
        }

        // Write many attributes and dup prefix
        [Theory]
        [XmlWriterInlineData]
        public void element_10c(XmlWriterUtils utils)
        {
            XmlWriter w = utils.CreateWriter();
            w.WriteDocType("a", null, null, "<!ATTLIST Root a (val|value) \"val\">");
            w.WriteStartElement("Root");
            for (int i = 0; i < 200; i++)
            {
                w.WriteAttributeString("p", "a", "n" + i, "val");
            }
            try
            {
                w.WriteAttributeString("p", "a", "n" + 199, "val");
            }
            catch (XmlException) { return; }
            finally
            {
                w.Dispose();
            }
            Assert.Fail();
        }

        // Write invalid DOCTYPE with many attributes with prefix
        [Theory]
        [XmlWriterInlineData]
        public void element_10d(XmlWriterUtils utils)
        {
            using (XmlWriter w = utils.CreateWriter())
            {
                try
                {
                    w.WriteDocType("a", "b", "c", "d");
                    w.WriteStartElement("Root");
                    for (int i = 0; i < 200; i++)
                    {
                        w.WriteAttributeString("p", "a", "n" + i, "val");
                    }
                    w.Dispose();
                }
                catch (XmlException e)
                {
                    CError.WriteLine(e);
                    Assert.Fail();
                }
            }
            return;
        }

        [Theory]
        [XmlWriterInlineData(1)]
        [XmlWriterInlineData(2)]
        [XmlWriterInlineData(3)]
        public void element_11(XmlWriterUtils utils, int param)
        {
            string exp = "";
            bool isIndent = false;
            if (utils.WriterType == WriterType.UTF8WriterIndent || utils.WriterType == WriterType.UnicodeWriterIndent)
                isIndent = true;

            using (XmlWriter w = utils.CreateWriter())
            {
                w.WriteDocType("root", null, null, "<!ENTITY e \"en-us\">");
                w.WriteStartElement("root");
                w.WriteStartAttribute("xml", "lang", null);
                switch (param)
                {
                    case 1:
                        w.WriteEntityRef("apos");
                        exp = !isIndent ? "<!DOCTYPE root [<!ENTITY e \"en-us\">]><root xml:lang=\"&apos;&lt;\" />" :
                            "<!DOCTYPE root [<!ENTITY e \"en-us\">]>" + Environment.NewLine + "<root xml:lang=\"&apos;&lt;\" />";
                        break;
                    case 2:
                        w.WriteEntityRef("lt");
                        exp = !isIndent ? "<!DOCTYPE root [<!ENTITY e \"en-us\">]><root xml:lang=\"&lt;&lt;\" />" :
                            "<!DOCTYPE root [<!ENTITY e \"en-us\">]>" + Environment.NewLine + "<root xml:lang=\"&lt;&lt;\" />";
                        break;
                    case 3:
                        w.WriteEntityRef("quot");
                        exp = !isIndent ? "<!DOCTYPE root [<!ENTITY e \"en-us\">]><root xml:lang=\"&quot;&lt;\" />" :
                            "<!DOCTYPE root [<!ENTITY e \"en-us\">]>" + Environment.NewLine + "<root xml:lang=\"&quot;&lt;\" />";
                        break;
                }
                w.WriteString("<");
                w.WriteEndAttribute();
                w.WriteEndElement();
            }
            Assert.True((utils.CompareString(exp)));
        }

        [Theory]
        [XmlWriterInlineData(1)]
        [XmlWriterInlineData(2)]
        [XmlWriterInlineData(3)]
        [XmlWriterInlineData(4)]
        [XmlWriterInlineData(5)]
        [XmlWriterInlineData(6)]
        public void element_12(XmlWriterUtils utils, int param)
        {
            XmlWriter w = utils.CreateWriter();
            w.WriteStartElement("Root");
            string exp = "";
            switch (param)
            {
                case 1:
                    exp = "<Root xml:xmlns=\"default    \" />";
                    break;
                case 2:
                    exp = "<Root xml:space=\"default\" />";
                    break;
                case 3:
                    exp = "<Root xml:lang=\"default    \" />";
                    break;
                case 4:
                    exp = "<Root p1:xml=\"default    \" xmlns:p1=\"xmlns\" />";
                    break;
                case 5:
                    exp = "<Root p1:xml=\"default    \" xmlns:p1=\"space\" />";
                    break;
                case 6:
                    exp = "<Root p1:xml=\"default    \" xmlns:p1=\"lang\" />";
                    break;
            }

            switch (param)
            {
                case 1:
                    w.WriteStartAttribute("xml", "xmlns", null);
                    break;
                case 2:
                    w.WriteStartAttribute("xml", "space", null);
                    break;
                case 3:
                    w.WriteStartAttribute("xml", "lang", null);
                    break;
                case 4:
                    w.WriteStartAttribute("xml", "xmlns");
                    break;
                case 5:
                    w.WriteStartAttribute("xml", "space");
                    break;
                case 6:
                    w.WriteStartAttribute("xml", "lang");
                    break;
            }
            w.WriteValue("default");
            try
            {
                w.WriteWhitespace("    ");
                w.WriteEndAttribute();
                w.WriteEndElement();
                w.Dispose();
            }
            catch (InvalidOperationException e)
            {
                CError.WriteLine(e);
                Assert.Fail();
            }
            Assert.True(utils.CompareString(exp));
        }

        [Theory]
        [XmlWriterInlineData(false, "<Root>-0</Root>")]
        [XmlWriterInlineData(true, "<Root b=\"-0\" />")]
        public void element_13(XmlWriterUtils utils, bool isAttr, string exp)
        {
            double a = 1;
            double b = 0;
            double c = 1;
            double d = -a * b / c;

            using (XmlWriter w = utils.CreateWriter())
            {
                w.WriteStartElement("Root");
                if (isAttr)
                    w.WriteStartAttribute("b");
                w.WriteValue(d);
                w.WriteEndElement();
            }
            Assert.True((utils.CompareString(exp)));
        }
    }
}
