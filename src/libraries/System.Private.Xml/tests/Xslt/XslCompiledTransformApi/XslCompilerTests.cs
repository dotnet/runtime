// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Xml.Xsl;
using Xunit;

namespace System.Xml.Tests
{
    public class XslCompilerTests
    {
        [Fact]
        public void ValueOfInDebugMode()
        {
            string xml = @"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>
<Class>
    <Info>This is my class info</Info>
</Class>";

            string xsl = @"<xsl:stylesheet version=""1.0"" xmlns:xsl=""http://www.w3.org/1999/XSL/Transform"">

    <xsl:output method=""text"" indent=""yes"" />
    
    <xsl:template match=""Class"">
        <xsl:value-of select=""Info"" />
    </xsl:template>
</xsl:stylesheet>";

            using (var outWriter = new StringWriter())
            {
                using (var xslStringReader = new StringReader(xsl))
                using (var xmlStringReader = new StringReader(xml))
                using (var xslReader = XmlReader.Create(xslStringReader))
                using (var xmlReader = XmlReader.Create(xmlStringReader))
                {
                    var transform = new XslCompiledTransform(true);
                    var argsList = new XsltArgumentList();

                    transform.Load(xslReader);
                    transform.Transform(xmlReader, argsList, outWriter);
                }

                Assert.Equal("This is my class info", outWriter.ToString());
            }
        }
    }
}
