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
        [Theory]
        [XmlWriterInlineData(WriterType.AllButCustom)]
        public void XmlDecl_1(XmlWriterUtils utils)
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.ConformanceLevel = ConformanceLevel.Document;

            XmlWriter w = utils.CreateWriter(wSettings);
            CError.Compare(w.Settings.ConformanceLevel, ConformanceLevel.Document, "Mismatch in CL");
            w.WriteStartElement("root");
            w.WriteEndElement();
            w.Dispose();

            XmlReader xr = utils.GetReader();
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

        [Theory]
        [XmlWriterInlineData(WriterType.AllButCustom)]
        public void XmlDecl_2(XmlWriterUtils utils)
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.ConformanceLevel = ConformanceLevel.Document;
            wSettings.OmitXmlDeclaration = true;

            XmlWriter w = utils.CreateWriter(wSettings);
            CError.Compare(w.Settings.ConformanceLevel, ConformanceLevel.Document, "Mismatch in CL");
            CError.Compare(w.Settings.OmitXmlDeclaration, true, "Mismatch in OmitXmlDecl");
            w.WriteStartElement("root");
            w.WriteEndElement();
            w.Dispose();

            XmlReader xr = utils.GetReader();
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

        [Theory]
        [XmlWriterInlineData(WriterType.AllButCustom)]
        public void XmlDecl_3(XmlWriterUtils utils)
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.ConformanceLevel = ConformanceLevel.Document;
            wSettings.OmitXmlDeclaration = true;

            XmlWriter w = utils.CreateWriter(wSettings);
            w.WriteStartDocument(true);
            w.WriteStartElement("root");
            w.WriteEndElement();
            w.WriteEndDocument();
            w.Dispose();


            XmlReader xr = utils.GetReader();
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

        [Theory]
        [XmlWriterInlineData(WriterType.AllButCustom)]
        public void XmlDecl_4(XmlWriterUtils utils)
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.ConformanceLevel = ConformanceLevel.Fragment;

            XmlWriter w = utils.CreateWriter(wSettings);
            CError.Compare(w.Settings.ConformanceLevel, ConformanceLevel.Fragment, "Mismatch in CL");
            w.WriteStartElement("root");
            w.WriteEndElement();
            w.WriteStartElement("root");
            w.WriteEndElement();
            w.Dispose();


            Assert.True(utils.CompareReader("<root /><root />"));
        }

        [Theory]
        [XmlWriterInlineData(WriterType.AllButCustom)]
        public void XmlDecl_5(XmlWriterUtils utils)
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.CloseOutput = false;

            XmlWriter w = utils.CreateWriter(wSettings);
            CError.Compare(w.Settings.CloseOutput, false, "Mismatch in CloseOutput");
            w.WriteProcessingInstruction("xml", "version = \"1.0\"");
            w.WriteStartElement("Root");
            w.WriteEndElement();
            w.Dispose();


            Assert.True(utils.CompareReader("<?xml version = \"1.0\"?><Root />"));
        }

        [Theory]
        [XmlWriterInlineData(WriterType.AllButCustom)]
        public void XmlDecl_6(XmlWriterUtils utils)
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.CloseOutput = false;
            wSettings.OmitXmlDeclaration = true;
            wSettings.ConformanceLevel = ConformanceLevel.Document;

            string strxml = "<?xml version=\"1.0\"?><root>blah</root>";
            XmlReader xr = ReaderHelper.Create(new StringReader(strxml));
            xr.Read();

            XmlWriter w = utils.CreateWriter(wSettings);
            w.WriteNode(xr, false);
            w.WriteStartElement("root");
            w.WriteEndElement();
            xr.Dispose();
            w.Dispose();


            Assert.True(utils.CompareReader("<root />"));
        }

        [Theory]
        [XmlWriterInlineData(WriterType.AllButCustom)]
        public void XmlDecl_7(XmlWriterUtils utils)
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.CloseOutput = false;
            wSettings.OmitXmlDeclaration = false;
            wSettings.ConformanceLevel = ConformanceLevel.Document;

            string strxml = "<?xml version=\"1.0\"?><root>blah</root>";
            XmlReader xr = ReaderHelper.Create(new StringReader(strxml));
            xr.Read();

            XmlWriter w = utils.CreateWriter(wSettings);
            w.WriteNode(xr, false);
            w.WriteStartElement("root");
            w.WriteString("blah");
            w.WriteEndElement();
            xr.Dispose();
            w.Dispose();

            Assert.True(utils.CompareReader(strxml));
        }
    }
}
