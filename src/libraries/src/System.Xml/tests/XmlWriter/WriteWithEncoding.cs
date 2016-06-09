// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Text;
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
    }
}
