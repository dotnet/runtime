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
        [Theory]
        [XmlWriterInlineData(WriterType.AllButCustom)]
        public void IndentChars_1(XmlWriterUtils utils)
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.OmitXmlDeclaration = true;
            wSettings.Indent = true;
            wSettings.IndentChars = "\x9";

            XmlWriter w = utils.CreateWriter(wSettings);
            CError.Compare(w.Settings.IndentChars, "\x9", "Mismatch in IndentChars");
            w.WriteStartElement("root");
            w.WriteStartElement("child");
            w.WriteEndElement();
            w.WriteEndElement();
            w.Dispose();

            Assert.True(utils.CompareString("<root>" + wSettings.NewLineChars + "\x9<child />" + wSettings.NewLineChars + "</root>"));
        }

        [Theory]
        [XmlWriterInlineData(WriterType.AllButCustom)]
        public void IndentChars_2(XmlWriterUtils utils)
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.OmitXmlDeclaration = true;
            wSettings.Indent = true;
            wSettings.IndentChars = "     ";

            XmlWriter w = utils.CreateWriter(wSettings);
            CError.Compare(w.Settings.IndentChars, "     ", "Mismatch in IndentChars");
            w.WriteStartElement("root");
            w.WriteStartElement("child");
            w.WriteEndElement();
            w.WriteEndElement();
            w.Dispose();

            Assert.True(utils.CompareString("<root>" + wSettings.NewLineChars + "     <child />" + wSettings.NewLineChars + "</root>"));
        }

        [Theory]
        [XmlWriterInlineData(WriterType.AllButCustom)]
        public void IndentChars_3(XmlWriterUtils utils)
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.OmitXmlDeclaration = true;
            wSettings.Indent = true;
            wSettings.IndentChars = "\xA";

            XmlWriter w = utils.CreateWriter(wSettings);
            CError.Compare(w.Settings.IndentChars, "\xA", "Mismatch in IndentChars");
            w.WriteStartElement("root");
            w.WriteStartElement("child");
            w.WriteEndElement();
            w.WriteEndElement();
            w.Dispose();

            Assert.True(utils.CompareString("<root>" + wSettings.NewLineChars + "\xA<child />" + wSettings.NewLineChars + "</root>"));
        }

        [Theory]
        [XmlWriterInlineData(WriterType.AllButCustom)]
        public void IndentChars_4(XmlWriterUtils utils)
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.OmitXmlDeclaration = true;
            wSettings.Indent = true;
            wSettings.IndentChars = "\xD";

            XmlWriter w = utils.CreateWriter(wSettings);
            CError.Compare(w.Settings.IndentChars, "\xD", "Mismatch in IndentChars");
            w.WriteStartElement("root");
            w.WriteStartElement("child");
            w.WriteEndElement();
            w.WriteEndElement();
            w.Dispose();

            Assert.True(utils.CompareString("<root>" + wSettings.NewLineChars + "\xD<child />" + wSettings.NewLineChars + "</root>"));
        }

        [Theory]
        [XmlWriterInlineData(WriterType.AllButCustom)]
        public void IndentChars_5(XmlWriterUtils utils)
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.OmitXmlDeclaration = true;
            wSettings.Indent = true;
            wSettings.IndentChars = "\x20";

            XmlWriter w = utils.CreateWriter(wSettings);
            CError.Compare(w.Settings.IndentChars, "\x20", "Mismatch in IndentChars");
            w.WriteStartElement("root");
            w.WriteStartElement("child");
            w.WriteEndElement();
            w.WriteEndElement();
            w.Dispose();

            Assert.True(utils.CompareString("<root>" + wSettings.NewLineChars + "\x20<child />" + wSettings.NewLineChars + "</root>"));
        }

        [Theory]
        [XmlWriterInlineData("<")]
        [XmlWriterInlineData("&")]
        [XmlWriterInlineData("<!--")]
        public void IndentChars_6(XmlWriterUtils utils, string indentChars)
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.OmitXmlDeclaration = true;
            wSettings.Indent = true;
            wSettings.IndentChars = indentChars;

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
            Assert.True((utils.WriterType == WriterType.CharCheckingWriter));
        }
    }
}
