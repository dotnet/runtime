// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using OLEDB.Test.ModuleCore;
using Xunit;

namespace System.Xml.Tests
{
    public class TCNewLineOnAttributes
    {
        //[Variation(id=1, Desc="Make sure the setting has no effect when Indent is false", Pri=0)]
        [Fact]
        public void NewLineOnAttributes_1()
        {
            if (IsIndent()) return;
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.OmitXmlDeclaration = true;
            wSettings.NewLineOnAttributes = true;

            XmlWriter w = CreateWriter(wSettings);
            CError.Compare(w.Settings.NewLineOnAttributes, false, "Mismatch in NewLineOnAttributes");

            w.WriteStartElement("root");
            w.WriteAttributeString("attr1", "value1");
            w.WriteAttributeString("attr2", "value2");
            w.WriteEndElement();
            w.Dispose();

            Assert.True(CompareString("<root attr1=\"value1\" attr2=\"value2\" />"));
        }

        //[Variation(id=2, Desc="Sanity test when Indent is true", Pri=1)]
        [Fact]
        public void NewLineOnAttributes_2()
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.OmitXmlDeclaration = true;
            wSettings.Indent = true;
            wSettings.NewLineOnAttributes = true;

            XmlWriter w = CreateWriter(wSettings);
            CError.Compare(w.Settings.NewLineOnAttributes, true, "Mismatch in NewLineOnAttributes");

            w.WriteStartElement("root");
            w.WriteAttributeString("attr1", "value1");
            w.WriteAttributeString("attr2", "value2");
            w.WriteEndElement();
            w.Dispose();

            return CompareString("<root" + wSettings.NewLineChars + "  attr1=\"value1\"" + wSettings.NewLineChars + "  attr2=\"value2\" />") ? TEST_PASS : TEST_FAIL;
        }

        //[Variation(id=3, Desc="Attributes of nested elements", Pri=1)]
        [Fact]
        public void NewLineOnAttributes_3()
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.OmitXmlDeclaration = true;
            wSettings.Indent = true;
            wSettings.NewLineOnAttributes = true;

            XmlWriter w = CreateWriter(wSettings);
            w.WriteStartElement("level1");
            w.WriteAttributeString("attr1", "value1");
            w.WriteAttributeString("attr2", "value2");
            w.WriteStartElement("level2");
            w.WriteAttributeString("attr1", "value1");
            w.WriteAttributeString("attr2", "value2");
            w.WriteEndElement();
            w.WriteEndElement();
            w.Dispose();

            Assert.True(CompareBaseline("NewLineOnAttributes3.txt"));
        }
    }
}
