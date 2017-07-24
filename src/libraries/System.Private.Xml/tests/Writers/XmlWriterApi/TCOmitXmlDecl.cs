// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using OLEDB.Test.ModuleCore;
using System.IO;
using XmlCoreTest.Common;
using Xunit;

namespace System.Xml.Tests
{
    public class TCOmitXmlDecl
    {
        //[Variation(id=1, Desc="Check when false", Pri=1)]
        [Fact]
        public void XmlDecl_1()
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.ConformanceLevel = ConformanceLevel.Document;

            XmlWriter w = CreateWriter(wSettings);
            CError.Compare(w.Settings.ConformanceLevel, ConformanceLevel.Document, "Mismatch in CL");
            w.WriteStartElement("root");
            w.WriteEndElement();
            w.Dispose();

            XmlReader xr = GetReader();
            // First node should be XmlDeclaration
            xr.Read();
            if (xr.NodeType != XmlNodeType.XmlDeclaration)
            {
                CError.WriteLine("Did not write XmlDecl when OmitXmlDecl was FALSE. NodeType = {0}", xr.NodeType.ToString());
                xr.Dispose();
                Assert.True(false);
            }
            else if (xr.NodeType == XmlNodeType.XmlDeclaration)
            {
                xr.Dispose();
                return;
            }
            else
            {
                xr.Dispose();
                Assert.True(false);
            }
        }

        //[Variation(id=2, Desc="Check when true", Pri=1)]
        [Fact]
        public void XmlDecl_2()
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.ConformanceLevel = ConformanceLevel.Document;
            wSettings.OmitXmlDeclaration = true;

            XmlWriter w = CreateWriter(wSettings);
            CError.Compare(w.Settings.ConformanceLevel, ConformanceLevel.Document, "Mismatch in CL");
            CError.Compare(w.Settings.OmitXmlDeclaration, true, "Mismatch in OmitXmlDecl");
            w.WriteStartElement("root");
            w.WriteEndElement();
            w.Dispose();

            XmlReader xr = GetReader();
            // Should not read XmlDeclaration
            while (xr.Read())
            {
                if (xr.NodeType == XmlNodeType.XmlDeclaration)
                {
                    CError.WriteLine("Wrote XmlDecl when OmitXmlDecl was TRUE");
                    xr.Dispose();
                    Assert.True(false);
                }
            }
            xr.Dispose();
            return;
        }

        //[Variation(id=3, Desc="Set to true, write standalone attribute. Should not write XmlDecl", Pri=1)]
        [Fact]
        public void XmlDecl_3()
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.ConformanceLevel = ConformanceLevel.Document;
            wSettings.OmitXmlDeclaration = true;

            XmlWriter w = CreateWriter(wSettings);
            w.WriteStartDocument(true);
            w.WriteStartElement("root");
            w.WriteEndElement();
            w.WriteEndDocument();
            w.Dispose();


            XmlReader xr = GetReader();
            // Should not read XmlDeclaration
            while (xr.Read())
            {
                if (xr.NodeType == XmlNodeType.XmlDeclaration)
                {
                    CError.WriteLine("Wrote XmlDecl when OmitXmlDecl was TRUE");
                    xr.Dispose();
                    Assert.True(false);
                }
            }
            xr.Dispose();
            return;
        }

        //[Variation(id=4, Desc="Set to false, write document fragment. Should not write XmlDecl", Pri=1)]
        [Fact]
        public void XmlDecl_4()
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.ConformanceLevel = ConformanceLevel.Fragment;

            XmlWriter w = CreateWriter(wSettings);
            CError.Compare(w.Settings.ConformanceLevel, ConformanceLevel.Fragment, "Mismatch in CL");
            w.WriteStartElement("root");
            w.WriteEndElement();
            w.WriteStartElement("root");
            w.WriteEndElement();
            w.Dispose();


            Assert.True(CompareReader("<root /><root />"));
        }

        //[Variation(id=5, Desc="WritePI with name = 'xml' text = 'version = 1.0' should work if WriteStartDocument is not called", Pri=1)]
        [Fact]
        public void XmlDecl_5()
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.CloseOutput = false;


            XmlWriter w = CreateWriter(wSettings);
            CError.Compare(w.Settings.CloseOutput, false, "Mismatch in CloseOutput");
            w.WriteProcessingInstruction("xml", "version = \"1.0\"");
            w.WriteStartElement("Root");
            w.WriteEndElement();
            w.Dispose();


            Assert.True(CompareReader("<?xml version = \"1.0\"?><Root />"));
        }

        //[Variation(id=6, Desc="WriteNode(reader) positioned on XmlDecl, OmitXmlDecl = true", Pri=1)]
        [Fact]
        public void XmlDecl_6()
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.CloseOutput = false;
            wSettings.OmitXmlDeclaration = true;
            wSettings.ConformanceLevel = ConformanceLevel.Document;

            string strxml = "<?xml version=\"1.0\"?><root>blah</root>";
            XmlReader xr = ReaderHelper.Create(new StringReader(strxml));
            xr.Read();

            XmlWriter w = CreateWriter(wSettings);
            w.WriteNode(xr, false);
            w.WriteStartElement("root");
            w.WriteEndElement();
            xr.Dispose();
            w.Dispose();


            Assert.True(CompareReader("<root />"));
        }

        //[Variation(id=7, Desc="WriteNode(reader) positioned on XmlDecl, OmitXmlDecl = false", Pri=1)]
        [Fact]
        public void XmlDecl_7()
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.CloseOutput = false;
            wSettings.OmitXmlDeclaration = false;
            wSettings.ConformanceLevel = ConformanceLevel.Document;

            string strxml = "<?xml version=\"1.0\"?><root>blah</root>";
            XmlReader xr = ReaderHelper.Create(new StringReader(strxml));
            xr.Read();

            XmlWriter w = CreateWriter(wSettings);
            w.WriteNode(xr, false);
            w.WriteStartElement("root");
            w.WriteString("blah");
            w.WriteEndElement();
            xr.Dispose();
            w.Dispose();

            Assert.True(CompareReader(strxml));
        }
    }
}
