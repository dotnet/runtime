// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;
using Xunit.Abstractions;
using System.IO;
using System.Xml.Xsl;

namespace System.Xml.Tests
{
    //[TestCase(Name = "TemporaryFiles", Desc = "This testcase tests the Temporary Files property on XslCompiledTransform")]
    public class TempFiles : XsltApiTestCaseBase2
    {
        private XslCompiledTransform _xsl = null;

        private ITestOutputHelper _output;
        public TempFiles(ITestOutputHelper output) : base(output)
        {
            _output = output;
        }

        //[Variation(Desc = "Load File from a drive c:", Pri = 2)]
        [InlineData()]
        [Theory]
        public void TempFiles1()
        {
            string childFile = Path.Combine(Directory.GetCurrentDirectory(), "child.xsl");

            string parentString = "<?xml version=\"1.0\"?>"
                + "<xsl:stylesheet xmlns:xsl=\"http://www.w3.org/1999/XSL/Transform\" version=\"1.0\">"
                + "<xsl:import href=\"" + childFile + "\"/>"
                + "<xsl:output method=\"xml\" omit-xml-declaration=\"yes\" indent=\"yes\"/>"
                + "<xsl:template match=\"book[@style='autobiography']\">"
                + "<SPAN style=\"color=blue\">From B<xsl:value-of select=\"name()\"/> : <xsl:value-of select=\"title\"/>"
                + "</SPAN><br/>"
                + "<xsl:apply-templates />"
                + "</xsl:template>"
                + "<xsl:template match=\"text()\" >"
                + "</xsl:template>"
                + "</xsl:stylesheet>";

            string childString = "<?xml version=\"1.0\"?>"
                + "<xsl:stylesheet xmlns:xsl=\"http://www.w3.org/1999/XSL/Transform\" version=\"1.0\">"
                + "<xsl:output method=\"xml\" omit-xml-declaration=\"yes\" indent=\"yes\"/>"
                + "<xsl:template match=\"book[@style='autobiography']\">"
                + "<SPAN style=\"color=blue\">From B<xsl:value-of select=\"name()\"/> : <xsl:value-of select=\"title\"/>"
                + "</SPAN><br/>"
                + "<xsl:apply-templates />"
                + "</xsl:template>"
                + "<xsl:template match=\"text()\" >"
                + "</xsl:template>"
                + "</xsl:stylesheet>";

            try
            {
                // create a xsl file in current directory on some drive, this is included in XSL above
                StreamWriter file = new StreamWriter(new FileStream(childFile, FileMode.Create, FileAccess.Write));
                file.WriteLine(childString);
                file.Dispose();
                StreamWriter parentFile = new StreamWriter(new FileStream("parent.xsl", FileMode.Create, FileAccess.Write));
                parentFile.WriteLine(parentString);
                parentFile.Dispose();
            }
            catch (Exception e)
            {
                _output.WriteLine(e.ToString());
                Assert.True(false);
            }

            try
            {
                // initialize XslCompiledTransform instance
                _xsl = new XslCompiledTransform();
    
                // Now let's load the parent xsl file
                _xsl.Load("parent.xsl", new XsltSettings(false, true), new XmlUrlResolver());
            }
            catch (XsltException e)
            {
                _output.WriteLine(e.ToString());
                Assert.True(false);
            }
            return;
        }

        //[Variation(Desc = "Bug 469775 - XSLT V2 : Exception thrown if xsl:preserve-space/xsl:strip-space is used and input document contains entities", Pri = 2)]
        [InlineData()]
        [Theory]
        public void TempFiles2()
        {
            try
            {
                XslCompiledTransform xslt = new XslCompiledTransform();
                xslt.Load(FullFilePath("bug469775.xsl"));
                Stream res = new MemoryStream();
                xslt.Transform(new XmlTextReader(FullFilePath("bug469775.xml")), (XsltArgumentList)null, res);
                _output.WriteLine(res.ToString());
            }
            catch (System.Xml.XmlException)
            {
                Assert.True(false);
            }
            return;
        }

        //[Variation(Desc = "Bug 469770 - XslCompiledTransform failed to load embedded stylesheets when prefixes are defined outside of xsl:stylesheet element", Pri = 2)]
        [InlineData()]
        [Theory]
        public void TempFiles3()
        {
            try
            {
                string xsl = "<root xmlns:ns=\"testing\">"
                    + "<xsl:stylesheet version=\"1.0\" xmlns:xsl=\"http://www.w3.org/1999/XSL/Transform\">"
                    + "<xsl:template match=\"/\">"
                    + "<xsl:value-of select=\"ns:test\" />"
                    + "</xsl:template>"
                    + "</xsl:stylesheet>"
                    + "</root>";

                XmlReader r = XmlReader.Create(new StringReader(xsl));
                while (r.NodeType != XmlNodeType.Element || r.LocalName != "stylesheet")
                {
                    if (!r.Read())
                    {
                        _output.WriteLine("There is no 'stylesheet' element in the file");
                        Assert.True(false);
                    }
                }

                XslCompiledTransform t = new XslCompiledTransform();
                t.Load(r);
            }
            catch (XsltException exception)
            {
                _output.WriteLine("The following exception should not have been thrown");
                _output.WriteLine(exception.ToString());
                Assert.True(false);
            }

            return;
        }

        //[Variation(Desc = "Bug 482971 - XslCompiledTransform cannot output numeric character reference after long output", Pri = 2)]
        [InlineData()]
        [Theory]
        public void TempFiles4()
        {
            try
            {
                XslCompiledTransform xslt = new XslCompiledTransform();
                xslt.Load(FullFilePath("bug482971.xsl"));
                xslt.Transform(FullFilePath("bug482971.xml"), "out.txt");
            }
            catch (Exception exception)
            {
                _output.WriteLine("No exception should not have been thrown");
                _output.WriteLine(exception.ToString());
                Assert.True(false);
            }
            return;
        }
    }
}