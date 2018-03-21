// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Xsl;
using Xunit;

namespace System.Xml.Tests
{
    public class XmlWriterTests_Encoding
    {
        [Fact]
        public static void WriteWithEncoding()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            XmlWriterSettings settings = new XmlWriterSettings();
            settings.OmitXmlDeclaration = true;
            settings.ConformanceLevel = ConformanceLevel.Fragment;
            settings.CloseOutput = false;
            settings.Encoding = Encoding.GetEncoding("Windows-1252");
            MemoryStream strm = new MemoryStream();

            using (XmlWriter writer = XmlWriter.Create(strm, settings))
            {
                writer.WriteElementString("orderID", "1-456-ab\u0661");
                writer.WriteElementString("orderID", "2-36-00a\uD800\uDC00\uD801\uDC01");
                writer.Flush();
            }

            strm.Seek(0, SeekOrigin.Begin);
            byte[] bytes = new byte[strm.Length];
            int bytesCount = strm.Read(bytes, 0, (int)strm.Length);
            string s = settings.Encoding.GetString(bytes, 0, bytesCount);

            Assert.Equal("<orderID>1-456-ab&#x661;</orderID><orderID>2-36-00a&#x10000;&#x10401;</orderID>", s);
        }

        [Fact]
        public void WriteWithUtf32EncodingNoBom()
        {
            // Given
            var xml = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n<breakfast_menu>\n    <food>\n        <name>Belgian Waffles</name>\n        <price>$5.95</price>\n        <description>Two of our famous Belgian Waffles with plenty of real maple syrup</description>\n        <calories>650</calories>\n    </food>\n    <food>\n        <name>Strawberry Belgian Waffles</name>\n        <price>$7.95</price>\n        <description>Light Belgian waffles covered with strawberries and whipped cream</description>\n        <calories>900</calories>\n    </food>\n</breakfast_menu>";
            var xsl = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n<html xmlns:xsl=\"http://www.w3.org/1999/XSL/Transform\" xsl:version=\"1.0\">\n    <body style=\"font-family:Arial;font-size:12pt;background-color:#EEEEEE\">\n        <xsl:for-each select=\"breakfast_menu/food\">\n            <div style=\"background-color:teal;color:white;padding:4px\">\n                <span style=\"font-weight:bold\">\n                    <xsl:value-of select=\"name\" />\n                    -\n                </span>\n                <xsl:value-of select=\"price\" />\n            </div>\n            <div style=\"margin-left:20px;margin-bottom:1em;font-size:10pt\">\n                <p>\n                    <xsl:value-of select=\"description\" />\n                    <span style=\"font-style:italic\">\n                        (\n                        <xsl:value-of select=\"calories\" />\n                        calories per serving)\n                    </span>\n                </p>\n            </div>\n        </xsl:for-each>\n    </body>\n</html>";

            // set encoding to UTF32 with no BOM
            var settings = new XmlWriterSettings
            {
                Encoding = new UTF32Encoding(false, false, true)
            };

            string resultString;
            using (TextReader xslReader = new StringReader(xsl),
                              xmlReader = new StringReader(xml))
            {
                using (var result = new MemoryStream())
                {
                    var xslXmlReader = XmlReader.Create(xslReader);
                    var xmlXmlReader = XmlReader.Create(xmlReader);

                    // BOM can be written in this call 
                    var resultXmlTextWriter = XmlWriter.Create(result, settings);

                    // When, do work and get result
                    var xslTransform = new XslCompiledTransform();
                    xslTransform.Load(xslXmlReader);
                    xslTransform.Transform(xmlXmlReader, resultXmlTextWriter);
                    result.Position = 0;
                    resultString = settings.Encoding.GetString(result.ToArray());
                }
            }

            // Then
            Assert.Equal("<?xml version=\"1.0\" encoding=\"utf-32\"?>", string.Concat(resultString.Take(39)));
        }
    }
}
