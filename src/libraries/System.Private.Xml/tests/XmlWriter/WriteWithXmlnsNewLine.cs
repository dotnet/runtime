// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace System.Xml.XmlWriterTests
{
    public class XmlWriterTests_XmlnsNewLine
    {
        [Fact]
        public static void WriteWithXmlnsNewLine()
        {
            XmlDocument xml = new();
            xml.LoadXml("<svg version=\"1.1\" xmlns=\"http://www.w3.org/2000/svg\" width=\"10\" xmlns:xlink=\"http://www.w3.org/1999/xlink\" height=\"10\"><g /></svg>");

            XmlWriterSettings settings = new();
            settings.NewLineOnAttributes = true;
            settings.NewLineChars = "\n";
            settings.Indent = true;
            settings.IndentChars = " ";

            StringBuilder output = new();
            using (XmlWriter writer = XmlWriter.Create(output, settings))
            {
                xml.Save(writer);
            }

            Assert.Equal("<?xml version=\"1.0\" encoding=\"utf-16\"?>\n<svg\n version=\"1.1\"\n xmlns=\"http://www.w3.org/2000/svg\"\n width=\"10\"\n xmlns:xlink=\"http://www.w3.org/1999/xlink\"\n height=\"10\">\n <g />\n</svg>", output.ToString());
        }
    }
}
