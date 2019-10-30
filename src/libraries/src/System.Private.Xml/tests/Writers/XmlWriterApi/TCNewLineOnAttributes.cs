// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using OLEDB.Test.ModuleCore;
using XmlCoreTest.Common;
using Xunit;

namespace System.Xml.Tests
{
    public class TCNewLineOnAttributes
    {
        [Theory]
        [XmlWriterInlineData(WriterType.AllButCustom & WriterType.AllButIndenting)]
        public void NewLineOnAttributes_1(XmlWriterUtils utils)
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.OmitXmlDeclaration = true;
            wSettings.NewLineOnAttributes = true;

            XmlWriter w = utils.CreateWriter(wSettings);
            CError.Compare(w.Settings.NewLineOnAttributes, false, "Mismatch in NewLineOnAttributes");

            w.WriteStartElement("root");
            w.WriteAttributeString("attr1", "value1");
            w.WriteAttributeString("attr2", "value2");
            w.WriteEndElement();
            w.Dispose();

            Assert.True(utils.CompareString("<root attr1=\"value1\" attr2=\"value2\" />"));
        }

        [Theory]
        [XmlWriterInlineData(WriterType.AllButCustom)]
        public void NewLineOnAttributes_2(XmlWriterUtils utils)
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.OmitXmlDeclaration = true;
            wSettings.Indent = true;
            wSettings.NewLineOnAttributes = true;

            XmlWriter w = utils.CreateWriter(wSettings);
            CError.Compare(w.Settings.NewLineOnAttributes, true, "Mismatch in NewLineOnAttributes");

            w.WriteStartElement("root");
            w.WriteAttributeString("attr1", "value1");
            w.WriteAttributeString("attr2", "value2");
            w.WriteEndElement();
            w.Dispose();

            Assert.True(utils.CompareString("<root" + wSettings.NewLineChars + "  attr1=\"value1\"" + wSettings.NewLineChars + "  attr2=\"value2\" />"));
        }

        [Theory]
        [XmlWriterInlineData]
        public void NewLineOnAttributes_3(XmlWriterUtils utils)
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.OmitXmlDeclaration = true;
            wSettings.Indent = true;
            wSettings.NewLineOnAttributes = true;

            XmlWriter w = utils.CreateWriter(wSettings);
            w.WriteStartElement("level1");
            w.WriteAttributeString("attr1", "value1");
            w.WriteAttributeString("attr2", "value2");
            w.WriteStartElement("level2");
            w.WriteAttributeString("attr1", "value1");
            w.WriteAttributeString("attr2", "value2");
            w.WriteEndElement();
            w.WriteEndElement();
            w.Dispose();

            Assert.True(utils.CompareBaseline("NewLineOnAttributes3.txt"));
        }
    }
}
