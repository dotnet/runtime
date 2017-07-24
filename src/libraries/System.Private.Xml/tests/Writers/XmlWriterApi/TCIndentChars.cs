// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using OLEDB.Test.ModuleCore;
using XmlCoreTest.Common;
using Xunit;

namespace System.Xml.Tests
{
    public class TCIndentChars
    {
        //[Variation(id=1, Desc="Set to tab char", Pri=0)]
        [Fact]
        public void IndentChars_1()
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.OmitXmlDeclaration = true;
            wSettings.Indent = true;
            wSettings.IndentChars = "\x9";

            XmlWriter w = CreateWriter(wSettings);
            CError.Compare(w.Settings.IndentChars, "\x9", "Mismatch in IndentChars");
            w.WriteStartElement("root");
            w.WriteStartElement("child");
            w.WriteEndElement();
            w.WriteEndElement();
            w.Dispose();

            return CompareString("<root>" + wSettings.NewLineChars + "\x9<child />" + wSettings.NewLineChars + "</root>") ? TEST_PASS : TEST_FAIL;
        }

        //[Variation(id=2, Desc="Set to multiple whitespace chars", Pri=0)]
        [Fact]
        public void IndentChars_2()
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.OmitXmlDeclaration = true;
            wSettings.Indent = true;
            wSettings.IndentChars = "     ";

            XmlWriter w = CreateWriter(wSettings);
            CError.Compare(w.Settings.IndentChars, "     ", "Mismatch in IndentChars");
            w.WriteStartElement("root");
            w.WriteStartElement("child");
            w.WriteEndElement();
            w.WriteEndElement();
            w.Dispose();

            return CompareString("<root>" + wSettings.NewLineChars + "     <child />" + wSettings.NewLineChars + "</root>") ? TEST_PASS : TEST_FAIL;
        }

        //[Variation(id=3, Desc="Set to 0xA", Pri=0)]
        [Fact]
        public void IndentChars_3()
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.OmitXmlDeclaration = true;
            wSettings.Indent = true;
            wSettings.IndentChars = "\xA";

            XmlWriter w = CreateWriter(wSettings);
            CError.Compare(w.Settings.IndentChars, "\xA", "Mismatch in IndentChars");
            w.WriteStartElement("root");
            w.WriteStartElement("child");
            w.WriteEndElement();
            w.WriteEndElement();
            w.Dispose();

            return CompareString("<root>" + wSettings.NewLineChars + "\xA<child />" + wSettings.NewLineChars + "</root>") ? TEST_PASS : TEST_FAIL;
        }

        //[Variation(id=4, Desc="Set to 0xD", Pri=0)]
        [Fact]
        public void IndentChars_4()
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.OmitXmlDeclaration = true;
            wSettings.Indent = true;
            wSettings.IndentChars = "\xD";

            XmlWriter w = CreateWriter(wSettings);
            CError.Compare(w.Settings.IndentChars, "\xD", "Mismatch in IndentChars");
            w.WriteStartElement("root");
            w.WriteStartElement("child");
            w.WriteEndElement();
            w.WriteEndElement();
            w.Dispose();

            return CompareString("<root>" + wSettings.NewLineChars + "\xD<child />" + wSettings.NewLineChars + "</root>") ? TEST_PASS : TEST_FAIL;
        }

        //[Variation(id=5, Desc="Set to 0x20", Pri=0)]
        [Fact]
        public void IndentChars_5()
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.OmitXmlDeclaration = true;
            wSettings.Indent = true;
            wSettings.IndentChars = "\x20";

            XmlWriter w = CreateWriter(wSettings);
            CError.Compare(w.Settings.IndentChars, "\x20", "Mismatch in IndentChars");
            w.WriteStartElement("root");
            w.WriteStartElement("child");
            w.WriteEndElement();
            w.WriteEndElement();
            w.Dispose();

            return CompareString("<root>" + wSettings.NewLineChars + "\x20<child />" + wSettings.NewLineChars + "</root>") ? TEST_PASS : TEST_FAIL;
        }

        //[Variation(id=6, Desc="Set to element start tag", Pri=1, Param="<")]
        //[Variation(id=7, Desc="Set to &", Pri=1, Param="&")]
        //[Variation(id=8, Desc="Set to comment start tag", Pri=1, Param="<!--")]
        [Fact]
        public void IndentChars_6()
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.OmitXmlDeclaration = true;
            wSettings.Indent = true;
            wSettings.IndentChars = CurVariation.Param.ToString();

            XmlWriter w = null;
            try
            {
                w = CreateWriter(wSettings);
            }
            catch (ArgumentException e)
            {
                CError.WriteLineIgnore("Exception: " + e.ToString());
                return;
            }
            Assert.True((WriterType == WriterType.CharCheckingWriter));
        }
    }
}
