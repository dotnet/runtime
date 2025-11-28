// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.IO;
using System.Tests;
using System.Xml.Xsl;
using Xunit;

namespace System.Xml.XslCompiledTransformApiTests
{
    [ConditionalClass(typeof(PlatformDetection), nameof(PlatformDetection.IsReflectionEmitSupported))]
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

        [Fact]
        public void FormatDateWithEmptyFormatString()
        {
            const string Xml = @"<?xml version=""1.0"" encoding=""UTF-8""?><root/>";
            const string Xsl = @"<xsl:stylesheet version=""1.0"" xmlns:xsl=""http://www.w3.org/1999/XSL/Transform""
                    xmlns:ms=""urn:schemas-microsoft-com:xslt"">
    <xsl:output method=""text"" />
    <xsl:template match=""/"">
        <xsl:value-of select=""ms:format-date('2001-02-03T01:02:03', '')"" />
    </xsl:template>
</xsl:stylesheet>";

            using (new ThreadCultureChange("en-GB"))
            using (var outWriter = new StringWriter())
            {
                using (var xslStringReader = new StringReader(Xsl))
                using (var xmlStringReader = new StringReader(Xml))
                using (var xslReader = XmlReader.Create(xslStringReader))
                using (var xmlReader = XmlReader.Create(xmlStringReader))
                {
                    var transform = new XslCompiledTransform();
                    transform.Load(xslReader);
                    transform.Transform(xmlReader, null, outWriter);
                }

                string result = outWriter.ToString();
                DateTime expectedDate = new DateTime(2001, 2, 3, 1, 2, 3, DateTimeKind.Utc);
                string expectedResult = expectedDate.ToString("d", new CultureInfo("en-GB"));
                Assert.Equal(expectedResult, result);
            }
        }

        [Fact]
        public void FormatTimeWithEmptyFormatString()
        {
            const string Xml = @"<?xml version=""1.0"" encoding=""UTF-8""?><root/>";
            const string Xsl = @"<xsl:stylesheet version=""1.0"" xmlns:xsl=""http://www.w3.org/1999/XSL/Transform""
                    xmlns:ms=""urn:schemas-microsoft-com:xslt"">
    <xsl:output method=""text"" />
    <xsl:template match=""/"">
        <xsl:value-of select=""ms:format-time('2001-02-03T01:02:03', '')"" />
    </xsl:template>
</xsl:stylesheet>";

            using (new ThreadCultureChange("en-GB"))
            using (var outWriter = new StringWriter())
            {
                using (var xslStringReader = new StringReader(Xsl))
                using (var xmlStringReader = new StringReader(Xml))
                using (var xslReader = XmlReader.Create(xslStringReader))
                using (var xmlReader = XmlReader.Create(xmlStringReader))
                {
                    var transform = new XslCompiledTransform();
                    transform.Load(xslReader);
                    transform.Transform(xmlReader, null, outWriter);
                }

                string result = outWriter.ToString();
                DateTime expectedTime = new DateTime(2001, 2, 3, 1, 2, 3, DateTimeKind.Utc);
                string expectedResult = expectedTime.ToString("T", new CultureInfo("en-GB"));
                Assert.Equal(expectedResult, result);
            }
        }


    }
}
