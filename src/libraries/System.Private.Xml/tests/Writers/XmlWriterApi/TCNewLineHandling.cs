// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using OLEDB.Test.ModuleCore;
using Xunit;

namespace System.Xml.Tests
{
    public class TCNewLineHandling
    {
        //[Variation(id=1, Desc="Test for CR (xD) inside element when NewLineHandling = Replace", Pri=0)]
        [Fact]
        public void NewLineHandling_1()
        {
            XmlWriter w = CreateWriter();
            w.WriteStartElement("root");
            w.WriteString("\r");
            w.WriteEndElement();
            w.Dispose();
            return CompareString("<root>" + w.Settings.NewLineChars + "</root>") ? TEST_PASS : TEST_FAIL;
        }

        //[Variation(id=2, Desc="Test for LF (xA) inside element when NewLineHandling = Replace", Pri=0)]
        [Fact]
        public void NewLineHandling_2()
        {
            XmlWriter w = CreateWriter();
            w.WriteStartElement("root");
            w.WriteString("\n");
            w.WriteEndElement();
            w.Dispose();
            return CompareString("<root>" + w.Settings.NewLineChars + "</root>") ? TEST_PASS : TEST_FAIL;
        }

        //[Variation(id=3, Desc="Test for CR LF (xD xA) inside element when NewLineHandling = Replace", Pri=0)]
        [Fact]
        public void NewLineHandling_3()
        {
            XmlWriter w = CreateWriter();
            w.WriteStartElement("root");
            w.WriteString("\r\n");
            w.WriteEndElement();
            w.Dispose();
            return CompareString("<root>" + w.Settings.NewLineChars + "</root>") ? TEST_PASS : TEST_FAIL;
        }

        //[Variation(id=4, Desc="Test for CR (xD) inside element when NewLineHandling = Entitize", Pri=0)]
        [Fact]
        public void NewLineHandling_4()
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.OmitXmlDeclaration = true;
            wSettings.NewLineHandling = NewLineHandling.Entitize;

            XmlWriter w = CreateWriter(wSettings);
            CError.Compare(w.Settings.NewLineHandling, NewLineHandling.Entitize, "Mismatch in NewLineHandling");
            w.WriteStartElement("root");
            w.WriteString("\r");
            w.WriteEndElement();
            w.Dispose();

            Assert.True(CompareString("<root>&#xD;</root>"));
        }

        //[Variation(id=5, Desc="Test for LF (xA) inside element when NewLineHandling = Entitize", Pri=0)]
        [Fact]
        public void NewLineHandling_5()
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.OmitXmlDeclaration = true;
            wSettings.NewLineHandling = NewLineHandling.Entitize;

            XmlWriter w = CreateWriter(wSettings);
            w.WriteStartElement("root");
            w.WriteString("\n");
            w.WriteEndElement();
            w.Dispose();

            Assert.True(CompareString("<root>\xA</root>"));
        }

        //[Variation(id=6, Desc="Test for CR LF (xD xA) inside element when NewLineHandling = Entitize", Pri=0)]
        [Fact]
        public void NewLineHandling_6()
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.OmitXmlDeclaration = true;
            wSettings.NewLineHandling = NewLineHandling.Entitize;

            XmlWriter w = CreateWriter(wSettings);
            w.WriteStartElement("root");
            w.WriteString("\r\n");
            w.WriteEndElement();
            w.Dispose();

            Assert.True(CompareString("<root>&#xD;\xA</root>"));
        }

        //[Variation(id=7, Desc="Test for CR (xD) inside attr when NewLineHandling = Replace", Pri=0)]
        [Fact]
        public void NewLineHandling_7()
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.OmitXmlDeclaration = true;

            XmlWriter w = CreateWriter(wSettings);
            w.WriteStartElement("root");
            w.WriteAttributeString("attr", "\r");
            w.Dispose();

            Assert.True(CompareString("<root attr=\"&#xD;\" />"));
        }

        //[Variation(id=8, Desc="Test for LF (xA) inside attr when NewLineHandling = Replace", Pri=0)]
        [Fact]
        public void NewLineHandling_8()
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.OmitXmlDeclaration = true;

            XmlWriter w = CreateWriter(wSettings);
            w.WriteStartElement("root");
            w.WriteAttributeString("attr", "\n");
            w.WriteEndElement();
            w.Dispose();

            Assert.True(CompareString("<root attr=\"&#xA;\" />"));
        }

        //[Variation(id=9, Desc="Test for CR LF (xD xA) inside attr when NewLineHandling = Replace", Pri=0)]
        [Fact]
        public void NewLineHandling_9()
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.OmitXmlDeclaration = true;

            XmlWriter w = CreateWriter(wSettings);
            w.WriteStartElement("root");
            w.WriteAttributeString("attr", "\r\n");
            w.WriteEndElement();
            w.Dispose();

            Assert.True(CompareString("<root attr=\"&#xD;&#xA;\" />"));
        }

        //[Variation(id=10, Desc="Test for CR (xD) inside attr when NewLineHandling = Entitize", Pri=0)]
        [Fact]
        public void NewLineHandling_10()
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.OmitXmlDeclaration = true;
            wSettings.NewLineHandling = NewLineHandling.Entitize;

            XmlWriter w = CreateWriter(wSettings);
            w.WriteStartElement("root");
            w.WriteAttributeString("attr", "\r");
            w.WriteEndElement();
            w.Dispose();

            Assert.True(CompareString("<root attr=\"&#xD;\" />"));
        }

        //[Variation(id=11, Desc="Test for LF (xA) inside attr when NewLineHandling = Entitize", Pri=0)]
        [Fact]
        public void NewLineHandling_11()
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.OmitXmlDeclaration = true;
            wSettings.NewLineHandling = NewLineHandling.Entitize;

            XmlWriter w = CreateWriter(wSettings);
            w.WriteStartElement("root");
            w.WriteAttributeString("attr", "\n");
            w.WriteEndElement();
            w.Dispose();

            Assert.True(CompareString("<root attr=\"&#xA;\" />"));
        }

        //[Variation(id=12, Desc="Test for CR LF (xD xA) inside attr when NewLineHandling = Entitize", Pri=0)]
        [Fact]
        public void NewLineHandling_12()
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.OmitXmlDeclaration = true;
            wSettings.NewLineHandling = NewLineHandling.Entitize;

            XmlWriter w = CreateWriter(wSettings);
            w.WriteStartElement("root");
            w.WriteAttributeString("attr", "\r\n");
            w.WriteEndElement();
            w.Dispose();

            Assert.True(CompareString("<root attr=\"&#xD;&#xA;\" />"));
        }

        //[Variation(id=13, Desc="Factory-created writers do not entitize 0xD character in text content when NewLineHandling=Entitize")]
        [Fact]
        public void NewLineHandling_13()
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.OmitXmlDeclaration = true;
            wSettings.NewLineHandling = NewLineHandling.Entitize;

            XmlWriter w = CreateWriter(wSettings);
            w.WriteStartElement("a");
            w.WriteString("A \r\n \r \n B");
            w.WriteEndElement();
            w.Dispose();

            Assert.True(CompareString("<a>A &#xD;\xA &#xD; \xA B</a>"));
        }
    }
}
