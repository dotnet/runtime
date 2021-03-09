// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Text;
using Xunit;

namespace System.Xml.Tests
{
    public class SaveTests
    {
        public static readonly byte[] utf8BomBytes = Encoding.UTF8.GetPreamble();

        [Fact]
        public void SaveDocumentToFilePath()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.AppendChild(xmlDocument.CreateXmlDeclaration("1.0", "UTF-8", null));
            xmlDocument.AppendChild(xmlDocument.CreateElement("Foo"));

            var tmpFile = Path.GetTempFileName();
            try
            {
                // Let .Save() infer the encoding from the declaration.
                xmlDocument.Save(tmpFile);

                var bytes = File.ReadAllBytes(tmpFile);
                Assert.False(bytes[0] == utf8BomBytes[0] && bytes[1] == utf8BomBytes[1] && bytes[2] == utf8BomBytes[2], ".Save() should create BOM-less UTF-8 files by default.");
            }
            finally
            {
                File.Delete(tmpFile);
            }
        }

        [Fact]
        public void SaveDocumentViaWriter()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.AppendChild(xmlDocument.CreateXmlDeclaration("1.0", null, null));
            xmlDocument.AppendChild(xmlDocument.CreateElement("Foo"));

            XmlWriterSettings settings = new XmlWriterSettings();
            // Set the encoding explicitly, to UTF-8 with BOM.
            settings.Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);

            using (var ms = new MemoryStream())
            {
                using (XmlWriter writer = XmlWriter.Create(ms, settings))
                {
                    xmlDocument.Save(writer);
                }

                var bytes = ms.GetBuffer();
                Assert.True(bytes[0] == utf8BomBytes[0] && bytes[1] == utf8BomBytes[1] && bytes[2] == utf8BomBytes[2], ".Save() should respect an XmlWriter's encoding.");
            }
        }
    }
}
