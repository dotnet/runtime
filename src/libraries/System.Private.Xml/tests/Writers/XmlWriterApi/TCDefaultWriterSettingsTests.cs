using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace System.Xml.Tests
{
    public class TCDefaultWriterSettings
    {
        //[Variation(id=1, Desc="Default value of Encoding")]
        [Fact]
        public void default_1()
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            CError.Compare(wSettings.Encoding, Encoding.UTF8, "Incorrect default value of Encoding");

            XmlWriter w = CreateWriter();
            switch (WriterType)
            {
                case WriterType.UTF8Writer:
                case WriterType.UTF8WriterIndent:
                case WriterType.CharCheckingWriter:
                case WriterType.WrappedWriter:
                    CError.Compare(w.Settings.Encoding.WebName, "utf-8", "Incorrect default value of Encoding");
                    break;
                case WriterType.UnicodeWriter:
                case WriterType.UnicodeWriterIndent:
                    CError.Compare(w.Settings.Encoding.WebName, "utf-16", "Incorrect default value of Encoding");
                    break;
            }
            w.Dispose();
            return;
        }

        //[Variation(id=2, Desc="Default value of OmitXmlDeclaration")]
        [Fact]
        public void default_2()
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            CError.Compare(wSettings.OmitXmlDeclaration, false, "Incorrect default value of OmitXmlDeclaration");

            return;
        }

        //[Variation(id=3, Desc="Default value of NewLineHandling")]
        [Fact]
        public void default_3()
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            CError.Compare(wSettings.NewLineHandling, NewLineHandling.Replace, "Incorrect default value of NewLineHandling");

            XmlWriter w = CreateWriter();
            switch (WriterType)
            {
                case WriterType.UTF8Writer:
                case WriterType.UnicodeWriter:
                    CError.Compare(w.Settings.NewLineHandling, NewLineHandling.Replace, "Incorrect default value of NewLineHandling");
                    break;
            }
            w.Dispose();
            return;
        }

        //[Variation(id=4, Desc="Default value of NewLineChars")]
        [Fact]
        public void default_4()
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            CError.Equals(wSettings.NewLineChars, Environment.NewLine, "Incorrect default value of NewLineChars");

            XmlWriter w = CreateWriter();
            CError.Equals(w.Settings.NewLineChars, Environment.NewLine, "Incorrect default value of NewLineChars");

            w.Dispose();
            return;
        }

        //[Variation(id=5, Desc="Default value of Indent")]
        [Fact]
        public void default_5()
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            CError.Equals(wSettings.Indent, false, "Incorrect default value of wSettings.Indent");

            XmlWriter w = CreateWriter();
            CError.Equals(w.Settings.Indent, IsIndent() ? true : false, "Incorrect default value of w.Settings.Indent");
            w.Dispose();
            return;
        }

        //[Variation(id=6, Desc="Default value of IndentChars")]
        [Fact]
        public void default_6()
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            CError.Equals(wSettings.IndentChars, "  ", "Incorrect default value of IndentChars");

            XmlWriter w = CreateWriter();
            CError.Equals(w.Settings.IndentChars, "  ", "Incorrect default value of IndentChars");

            return;
        }

        //[Variation(id=7, Desc="Default value of NewLineOnAttributes")]
        [Fact]
        public void default_7()
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            CError.Equals(wSettings.NewLineOnAttributes, false, "Incorrect default value of NewLineOnAttributes");

            XmlWriter w = CreateWriter();
            CError.Equals(w.Settings.NewLineOnAttributes, false, "Incorrect default value of NewLineOnAttributes");

            return;
        }

        //[Variation(id=8, Desc="Default value of CloseOutput")]
        [Fact]
        public void default_8()
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            CError.Compare(wSettings.CloseOutput, false, "Incorrect default value of CloseOutput");

            return;
        }

        //[Variation(id=10, Desc="Default value of CheckCharacters")]
        [Fact]
        public void default_10()
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            CError.Equals(wSettings.CheckCharacters, true, "Incorrect default value of CheckCharacters");

            XmlWriter w = CreateWriter();
            CError.Equals(w.Settings.CheckCharacters, true, "Incorrect default value of CheckCharacters");

            w.Dispose();
            return;
        }

        //[Variation(id=11, Desc="Default value of ConformanceLevel")]
        [Fact]
        public void default_11()
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            CError.Equals(wSettings.ConformanceLevel, ConformanceLevel.Document, "Incorrect default value of ConformanceLevel");

            XmlWriter w = CreateWriter();
            CError.Equals(w.Settings.ConformanceLevel, ConformanceLevel.Document, "Incorrect default value of ConformanceLevel");

            w.Dispose();
            return;
        }

        //[Variation(id = 13, Desc = "Default value of WriteEndDocumentOnClose")]
        [Fact]
        public void default_13()
        {
            XmlWriterSettings ws = new XmlWriterSettings();
            CError.Equals(ws.WriteEndDocumentOnClose, true, "Incorrect default value of WriteEndDocumentOnClose");

            XmlWriter w = CreateWriter();
            CError.Equals(w.Settings.WriteEndDocumentOnClose, true, "Incorrect default value of WriteEndDocumentOnClose");

            w.Dispose();

            return;
        }
    }

}
