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
        [Theory]
        [XmlWriterInlineData(WriterType.AllButCustom)]
        public void NewLineChars_1(XmlWriterUtils utils)
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.OmitXmlDeclaration = true;
            wSettings.NewLineChars = "\x9";

            XmlWriter w = utils.CreateWriter(wSettings);
            CError.Compare(w.Settings.NewLineChars, "\x9", "Mismatch in NewLineChars");
            w.WriteStartElement("root");
            w.WriteString("Test\r\nNewLine");
            w.WriteEndElement();
            w.Dispose();

            Assert.True(utils.CompareString("<root>Test\x9NewLine</root>"));
        }

        [Theory]
        [XmlWriterInlineData(WriterType.AllButCustom)]
        public void NewLineChars_2(XmlWriterUtils utils)
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.OmitXmlDeclaration = true;
            wSettings.NewLineChars = "     ";

            XmlWriter w = utils.CreateWriter(wSettings);
            CError.Compare(w.Settings.NewLineChars, "     ", "Mismatch in NewLineChars");
            w.WriteStartElement("root");
            w.WriteString("Test\r\nNewLine");
            w.WriteEndElement();
            w.Dispose();

            Assert.True(utils.CompareString("<root>Test     NewLine</root>"));
        }

        [Theory]
        [XmlWriterInlineData(WriterType.AllButCustom)]
        public void NewLineChars_3(XmlWriterUtils utils)
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.OmitXmlDeclaration = true;
            wSettings.NewLineChars = "\xA";

            XmlWriter w = utils.CreateWriter(wSettings);
            CError.Compare(w.Settings.NewLineChars, "\xA", "Mismatch in NewLineChars");
            w.WriteStartElement("root");
            w.WriteString("Test\r\nNewLine");
            w.WriteEndElement();
            w.Dispose();

            Assert.True(utils.CompareString("<root>Test\xANewLine</root>"));
        }

        [Theory]
        [XmlWriterInlineData(WriterType.AllButCustom)]
        public void NewLineChars_4(XmlWriterUtils utils)
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.OmitXmlDeclaration = true;
            wSettings.NewLineChars = "\xD";

            XmlWriter w = utils.CreateWriter(wSettings);
            CError.Compare(w.Settings.NewLineChars, "\xD", "Mismatch in NewLineChars");
            w.WriteStartElement("root");
            w.WriteString("Test\r\nNewLine");
            w.WriteEndElement();
            w.Dispose();

            Assert.True(utils.CompareString("<root>Test\xDNewLine</root>"));
        }

        [Theory]
        [XmlWriterInlineData(WriterType.AllButCustom)]
        public void NewLineChars_5(XmlWriterUtils utils)
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.OmitXmlDeclaration = true;
            wSettings.NewLineChars = "\x20";

            XmlWriter w = utils.CreateWriter(wSettings);
            CError.Compare(w.Settings.NewLineChars, "\x20", "Mismatch in NewLineChars");
            w.WriteStartElement("root");
            w.WriteString("Test\r\nNewLine");
            w.WriteEndElement();
            w.Dispose();

            Assert.True(utils.CompareString("<root>Test\x20NewLine</root>"));
        }

        [Theory]
        [XmlWriterInlineData("<")]
        [XmlWriterInlineData("&")]
        [XmlWriterInlineData("<!--")]
        public void NewLineChars_6(XmlWriterUtils utils, string newLineChars)
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.CloseOutput = true;
            wSettings.NewLineChars = newLineChars;

            XmlWriter w = null;

            try
            {
                w = utils.CreateWriter(wSettings);
            }
            catch (ArgumentException e)
            {
                CError.WriteLineIgnore("Exception: " + e.ToString());
                return;
            }

            CError.WriteLine("Did not throw ArgumentException");
            Assert.True((utils.WriterType == WriterType.CharCheckingWriter));
        }
    }
}
