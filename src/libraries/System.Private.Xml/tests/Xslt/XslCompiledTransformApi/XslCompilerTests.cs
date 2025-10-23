// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.IO;
using System.Threading;
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
            CultureInfo originalCulture = Thread.CurrentThread.CurrentCulture;
            try
            {
                Thread.CurrentThread.CurrentCulture = new CultureInfo("en-GB");

                string xml = @"<?xml version=""1.0"" encoding=""UTF-8""?><root/>";
                string xsl = @"<xsl:stylesheet version=""1.0"" xmlns:xsl=""http://www.w3.org/1999/XSL/Transform""
                    xmlns:ms=""urn:schemas-microsoft-com:xslt"">
    <xsl:output method=""text"" />
    <xsl:template match=""/"">
        <xsl:value-of select=""ms:format-date('2001-02-03T01:02:03', '')"" />
    </xsl:template>
</xsl:stylesheet>";

                using (var outWriter = new StringWriter())
                {
                    using (var xslStringReader = new StringReader(xsl))
                    using (var xmlStringReader = new StringReader(xml))
                    using (var xslReader = XmlReader.Create(xslStringReader))
                    using (var xmlReader = XmlReader.Create(xmlStringReader))
                    {
                        var transform = new XslCompiledTransform();
                        transform.Load(xslReader);
                        transform.Transform(xmlReader, null, outWriter);
                    }

                    string result = outWriter.ToString();
                    Assert.DoesNotContain("01:02:03", result);
                    Assert.Contains("03/02/2001", result);
                }
            }
            finally
            {
                Thread.CurrentThread.CurrentCulture = originalCulture;
            }
        }

        [Fact]
        public void FormatTimeWithEmptyFormatString()
        {
            CultureInfo originalCulture = Thread.CurrentThread.CurrentCulture;
            try
            {
                Thread.CurrentThread.CurrentCulture = new CultureInfo("en-GB");

                string xml = @"<?xml version=""1.0"" encoding=""UTF-8""?><root/>";
                string xsl = @"<xsl:stylesheet version=""1.0"" xmlns:xsl=""http://www.w3.org/1999/XSL/Transform""
                    xmlns:ms=""urn:schemas-microsoft-com:xslt"">
    <xsl:output method=""text"" />
    <xsl:template match=""/"">
        <xsl:value-of select=""ms:format-time('2001-02-03T01:02:03', '')"" />
    </xsl:template>
</xsl:stylesheet>";

                using (var outWriter = new StringWriter())
                {
                    using (var xslStringReader = new StringReader(xsl))
                    using (var xmlStringReader = new StringReader(xml))
                    using (var xslReader = XmlReader.Create(xslStringReader))
                    using (var xmlReader = XmlReader.Create(xmlStringReader))
                    {
                        var transform = new XslCompiledTransform();
                        transform.Load(xslReader);
                        transform.Transform(xmlReader, null, outWriter);
                    }

                    string result = outWriter.ToString();
                    Assert.DoesNotContain("03/02/2001", result);
                    Assert.Contains("01:02:03", result);
                }
            }
            finally
            {
                Thread.CurrentThread.CurrentCulture = originalCulture;
            }
        }

        [Fact]
        public void FormatDateWithExplicitFormatString()
        {
            string xml = @"<?xml version=""1.0"" encoding=""UTF-8""?><root/>";
            string xsl = @"<xsl:stylesheet version=""1.0"" xmlns:xsl=""http://www.w3.org/1999/XSL/Transform""
                xmlns:ms=""urn:schemas-microsoft-com:xslt"">
    <xsl:output method=""text"" />
    <xsl:template match=""/"">
        <xsl:value-of select=""ms:format-date('2001-02-03T01:02:03', 'd')"" />
    </xsl:template>
</xsl:stylesheet>";

            using (var outWriter = new StringWriter())
            {
                using (var xslStringReader = new StringReader(xsl))
                using (var xmlStringReader = new StringReader(xml))
                using (var xslReader = XmlReader.Create(xslStringReader))
                using (var xmlReader = XmlReader.Create(xmlStringReader))
                {
                    var transform = new XslCompiledTransform();
                    transform.Load(xslReader);
                    transform.Transform(xmlReader, null, outWriter);
                }

                string result = outWriter.ToString();
                Assert.DoesNotContain("01:02:03", result);
            }
        }

        [Fact]
        public void FormatTimeWithExplicitFormatString()
        {
            string xml = @"<?xml version=""1.0"" encoding=""UTF-8""?><root/>";
            string xsl = @"<xsl:stylesheet version=""1.0"" xmlns:xsl=""http://www.w3.org/1999/XSL/Transform""
                xmlns:ms=""urn:schemas-microsoft-com:xslt"">
    <xsl:output method=""text"" />
    <xsl:template match=""/"">
        <xsl:value-of select=""ms:format-time('2001-02-03T01:02:03', 'T')"" />
    </xsl:template>
</xsl:stylesheet>";

            using (var outWriter = new StringWriter())
            {
                using (var xslStringReader = new StringReader(xsl))
                using (var xmlStringReader = new StringReader(xml))
                using (var xslReader = XmlReader.Create(xslStringReader))
                using (var xmlReader = XmlReader.Create(xmlStringReader))
                {
                    var transform = new XslCompiledTransform();
                    transform.Load(xslReader);
                    transform.Transform(xmlReader, null, outWriter);
                }

                string result = outWriter.ToString();
                Assert.DoesNotContain("2001", result);
                Assert.Matches(@"0?1:02:03", result);
            }
        }
    }
}
