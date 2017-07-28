// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using OLEDB.Test.ModuleCore;
using XmlCoreTest.Common;
using Xunit;

namespace System.Xml.Tests
{
    public class TCNewLineHandling
    {
        [Theory]
        [XmlWriterInlineData(WriterType.AllButCustom)]
        public void NewLineHandling_1(XmlWriterUtils utils)
        {
            XmlWriter w = utils.CreateWriter();
            w.WriteStartElement("root");
            w.WriteString("\r");
            w.WriteEndElement();
            w.Dispose();
            Assert.True(utils.CompareString("<root>" + w.Settings.NewLineChars + "</root>"));
        }

        [Theory]
        [XmlWriterInlineData(WriterType.AllButCustom)]
        public void NewLineHandling_2(XmlWriterUtils utils)
        {
            XmlWriter w = utils.CreateWriter();
            w.WriteStartElement("root");
            w.WriteString("\n");
            w.WriteEndElement();
            w.Dispose();
            Assert.True(utils.CompareString("<root>" + w.Settings.NewLineChars + "</root>"));
        }

        [Theory]
        [XmlWriterInlineData(WriterType.AllButCustom)]
        public void NewLineHandling_3(XmlWriterUtils utils)
        {
            XmlWriter w = utils.CreateWriter();
            w.WriteStartElement("root");
            w.WriteString("\r\n");
            w.WriteEndElement();
            w.Dispose();
            Assert.True(utils.CompareString("<root>" + w.Settings.NewLineChars + "</root>"));
        }

        [Theory]
        [XmlWriterInlineData(WriterType.AllButCustom)]
        public void NewLineHandling_4(XmlWriterUtils utils)
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.OmitXmlDeclaration = true;
            wSettings.NewLineHandling = NewLineHandling.Entitize;

            XmlWriter w = utils.CreateWriter(wSettings);
            CError.Compare(w.Settings.NewLineHandling, NewLineHandling.Entitize, "Mismatch in NewLineHandling");
            w.WriteStartElement("root");
            w.WriteString("\r");
            w.WriteEndElement();
            w.Dispose();

            Assert.True(utils.CompareString("<root>&#xD;</root>"));
        }

        [Theory]
        [XmlWriterInlineData]
        public void NewLineHandling_5(XmlWriterUtils utils)
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.OmitXmlDeclaration = true;
            wSettings.NewLineHandling = NewLineHandling.Entitize;

            XmlWriter w = utils.CreateWriter(wSettings);
            w.WriteStartElement("root");
            w.WriteString("\n");
            w.WriteEndElement();
            w.Dispose();

            Assert.True(utils.CompareString("<root>\xA</root>"));
        }

        [Theory]
        [XmlWriterInlineData]
        public void NewLineHandling_6(XmlWriterUtils utils)
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.OmitXmlDeclaration = true;
            wSettings.NewLineHandling = NewLineHandling.Entitize;

            XmlWriter w = utils.CreateWriter(wSettings);
            w.WriteStartElement("root");
            w.WriteString("\r\n");
            w.WriteEndElement();
            w.Dispose();

            Assert.True(utils.CompareString("<root>&#xD;\xA</root>"));
        }

        [Theory]
        [XmlWriterInlineData]
        public void NewLineHandling_7(XmlWriterUtils utils)
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.OmitXmlDeclaration = true;

            XmlWriter w = utils.CreateWriter(wSettings);
            w.WriteStartElement("root");
            w.WriteAttributeString("attr", "\r");
            w.Dispose();

            Assert.True(utils.CompareString("<root attr=\"&#xD;\" />"));
        }

        [Theory]
        [XmlWriterInlineData]
        public void NewLineHandling_8(XmlWriterUtils utils)
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.OmitXmlDeclaration = true;

            XmlWriter w = utils.CreateWriter(wSettings);
            w.WriteStartElement("root");
            w.WriteAttributeString("attr", "\n");
            w.WriteEndElement();
            w.Dispose();

            Assert.True(utils.CompareString("<root attr=\"&#xA;\" />"));
        }

        [Theory]
        [XmlWriterInlineData]
        public void NewLineHandling_9(XmlWriterUtils utils)
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.OmitXmlDeclaration = true;

            XmlWriter w = utils.CreateWriter(wSettings);
            w.WriteStartElement("root");
            w.WriteAttributeString("attr", "\r\n");
            w.WriteEndElement();
            w.Dispose();

            Assert.True(utils.CompareString("<root attr=\"&#xD;&#xA;\" />"));
        }

        [Theory]
        [XmlWriterInlineData]
        public void NewLineHandling_10(XmlWriterUtils utils)
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.OmitXmlDeclaration = true;
            wSettings.NewLineHandling = NewLineHandling.Entitize;

            XmlWriter w = utils.CreateWriter(wSettings);
            w.WriteStartElement("root");
            w.WriteAttributeString("attr", "\r");
            w.WriteEndElement();
            w.Dispose();

            Assert.True(utils.CompareString("<root attr=\"&#xD;\" />"));
        }

        [Theory]
        [XmlWriterInlineData]
        public void NewLineHandling_11(XmlWriterUtils utils)
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.OmitXmlDeclaration = true;
            wSettings.NewLineHandling = NewLineHandling.Entitize;

            XmlWriter w = utils.CreateWriter(wSettings);
            w.WriteStartElement("root");
            w.WriteAttributeString("attr", "\n");
            w.WriteEndElement();
            w.Dispose();

            Assert.True(utils.CompareString("<root attr=\"&#xA;\" />"));
        }

        [Theory]
        [XmlWriterInlineData]
        public void NewLineHandling_12(XmlWriterUtils utils)
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.OmitXmlDeclaration = true;
            wSettings.NewLineHandling = NewLineHandling.Entitize;

            XmlWriter w = utils.CreateWriter(wSettings);
            w.WriteStartElement("root");
            w.WriteAttributeString("attr", "\r\n");
            w.WriteEndElement();
            w.Dispose();

            Assert.True(utils.CompareString("<root attr=\"&#xD;&#xA;\" />"));
        }

        [Theory]
        [XmlWriterInlineData]
        public void NewLineHandling_13(XmlWriterUtils utils)
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.OmitXmlDeclaration = true;
            wSettings.NewLineHandling = NewLineHandling.Entitize;

            XmlWriter w = utils.CreateWriter(wSettings);
            w.WriteStartElement("a");
            w.WriteString("A \r\n \r \n B");
            w.WriteEndElement();
            w.Dispose();

            Assert.True(utils.CompareString("<a>A &#xD;\xA &#xD; \xA B</a>"));
        }
    }
}
