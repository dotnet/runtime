// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using OLEDB.Test.ModuleCore;
using XmlCoreTest.Common;
using Xunit;

namespace System.Xml.Tests
{
    public class TCNewLineChars
    {
        //[Variation(id=1, Desc="Set to tab char", Pri=0)]
        [Fact]
        public void NewLineChars_1()
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.OmitXmlDeclaration = true;
            wSettings.NewLineChars = "\x9";

            XmlWriter w = CreateWriter(wSettings);
            CError.Compare(w.Settings.NewLineChars, "\x9", "Mismatch in NewLineChars");
            w.WriteStartElement("root");
            w.WriteString("Test\r\nNewLine");
            w.WriteEndElement();
            w.Dispose();

            Assert.True(CompareString("<root>Test\x9NewLine</root>"));
        }

        //[Variation(id=2, Desc="Set to multiple whitespace chars", Pri=0)]
        [Fact]
        public void NewLineChars_2()
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.OmitXmlDeclaration = true;
            wSettings.NewLineChars = "     ";

            XmlWriter w = CreateWriter(wSettings);
            CError.Compare(w.Settings.NewLineChars, "     ", "Mismatch in NewLineChars");
            w.WriteStartElement("root");
            w.WriteString("Test\r\nNewLine");
            w.WriteEndElement();
            w.Dispose();

            Assert.True(CompareString("<root>Test     NewLine</root>"));
        }

        //[Variation(id=3, Desc="Set to 0xA", Pri=0)]
        [Fact]
        public void NewLineChars_3()
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.OmitXmlDeclaration = true;
            wSettings.NewLineChars = "\xA";

            XmlWriter w = CreateWriter(wSettings);
            CError.Compare(w.Settings.NewLineChars, "\xA", "Mismatch in NewLineChars");
            w.WriteStartElement("root");
            w.WriteString("Test\r\nNewLine");
            w.WriteEndElement();
            w.Dispose();

            Assert.True(CompareString("<root>Test\xANewLine</root>"));
        }

        //[Variation(id=4, Desc="Set to 0xD", Pri=0)]
        [Fact]
        public void NewLineChars_4()
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.OmitXmlDeclaration = true;
            wSettings.NewLineChars = "\xD";

            XmlWriter w = CreateWriter(wSettings);
            CError.Compare(w.Settings.NewLineChars, "\xD", "Mismatch in NewLineChars");
            w.WriteStartElement("root");
            w.WriteString("Test\r\nNewLine");
            w.WriteEndElement();
            w.Dispose();

            Assert.True(CompareString("<root>Test\xDNewLine</root>"));
        }

        //[Variation(id=5, Desc="Set to 0x20", Pri=0)]
        [Fact]
        public void NewLineChars_5()
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.OmitXmlDeclaration = true;
            wSettings.NewLineChars = "\x20";

            XmlWriter w = CreateWriter(wSettings);
            CError.Compare(w.Settings.NewLineChars, "\x20", "Mismatch in NewLineChars");
            w.WriteStartElement("root");
            w.WriteString("Test\r\nNewLine");
            w.WriteEndElement();
            w.Dispose();

            Assert.True(CompareString("<root>Test\x20NewLine</root>"));
        }

        //[Variation(id=6, Desc="Set to <", Pri=1, Param="<")]
        //[Variation(id=7, Desc="Set to &", Pri=1, Param="&")]
        //[Variation(id=8, Desc="Set to comment start tag", Pri=1, Param="<!--")]
        [Fact]
        public void NewLineChars_6()
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.CloseOutput = true;
            wSettings.NewLineChars = CurVariation.Param.ToString();

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

            CError.WriteLine("Did not throw ArgumentException");
            Assert.True((WriterType == WriterType.CharCheckingWriter));
        }
    }
}
