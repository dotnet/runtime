// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using GotDotNet.Exslt;
using Xunit;
using Xunit.Abstractions;
using System;
using System.Globalization;
using System.IO;
using System.Security;
using System.Xml;
using System.Xml.XPath;
using System.Xml.Xsl;
using XmlCoreTest.Common;
using System.Reflection;
using System.Reflection.Emit;

namespace System.Xml.Tests
{
    //[TestCase(Name = "EXslt using XslCompiledTransform", Desc = "Compatibility of EXslt Objects using XsltV2 XsltArgumentList", Pri = 2, Param = "v2")]
    //[TestCase(Name = "EXslt using XslTransform", Desc = "Compatibility of EXslt Objects using XsltV1 XsltArgumentList", Pri = 2, Param = "v1")]
    public class EXslt //: CTestCase
    {
        private string _xmlFile = string.Empty;
        private string _xslFile = string.Empty;
        private string _baseline = string.Empty;

#pragma warning disable 0618
        private XslTransform _xsltV1 = null;
#pragma warning restore 0618
        private XslCompiledTransform _xsltV2 = null;

        private ITestOutputHelper _output;
        //Constructor
        public EXslt(ITestOutputHelper output)
        {
            _output = output;
        }

        //[Variation(id = 1, Desc = "Xslt Scenarios : common-node-set)]
        [InlineData("common-source.xml", "common-node-set.xsl", "valid", "common-node-set.xml", "v2")]
        //[Variation(id = 2, Desc = "Xslt Scenarios : common-object-type)]
        [InlineData("common-source.xml", "common-object-type.xsl", "valid", "common-object-type.xml", "v2")]
        //[Variation(id = 3, Desc = "Xslt Scenarios : datetime-add-duration)]
        [InlineData("datetime-source.xml", "datetime-add-duration.xsl", "valid", "datetime-add-duration.xml", "v2")]
        //[Variation(id = 4, Desc = "Xslt Scenarios : datetime-add)]
        ///[InlineData("datetime-source.xml", "datetime-add.xsl", "valid", "datetime-add.xml", "v2")] //disabled as references libraries not present on corefx
        //[Variation(id = 5, Desc = "Xslt Scenarios : datetime-avg)]
        [InlineData("datetime-source1.xml", "datetime-avg.xsl", "valid", "datetime-avg.xml", "v2")]
        //[Variation(id = 6, Desc = "Xslt Scenarios : datetime-date-time)]
        ///[InlineData("datetime-source.xml", "datetime-date-time.xsl", "valid", "datetime-date-time.xml", "v2")] //disabled as references libraries not present on corefx
        //[Variation(id = 7, Desc = "Xslt Scenarios : datetime-date)]
        [InlineData("datetime-source.xml", "datetime-date.xsl", "valid", "datetime-date.xml", "v2")]
        //[Variation(id = 8, Desc = "Xslt Scenarios : datetime-day-abbreviation)]
        [InlineData("datetime-source.xml", "datetime-day-abbreviation.xsl", "valid", "datetime-day-abbreviation.xml", "v2")]
        //[Variation(id = 10, Desc = "Xslt Scenarios : datetime-day-in-month)]
        [InlineData("datetime-source.xml", "datetime-day-in-month.xsl", "valid", "datetime-day-in-month.xml", "v2")]
        //[Variation(id = 11, Desc = "Xslt Scenarios : datetime-day-in-week)]
        [InlineData("datetime-source.xml", "datetime-day-in-week.xsl", "valid", "datetime-day-in-week.xml", "v2")]
        //[Variation(id = 12, Desc = "Xslt Scenarios : datetime-day-in-year)]
        [InlineData("datetime-source.xml", "datetime-day-in-year.xsl", "valid", "datetime-day-in-year.xml", "v2")]
        //[Variation(id = 13, Desc = "Xslt Scenarios : datetime-day-name)]
        [InlineData("datetime-source1.xml", "datetime-day-name.xsl", "valid", "datetime-day-name.xml", "v2")]
        //[Variation(id = 14, Desc = "Xslt Scenarios : datetime-day-name1)]
        [InlineData("datetime-source1.xml", "datetime-day-name1.xsl", "valid", "datetime-day-name1.xml", "v2")]
        //[Variation(id = 15, Desc = "Xslt Scenarios : datetime-day-of-week-in-month)]
        [InlineData("datetime-source.xml", "datetime-day-of-week-in-month.xsl", "valid", "datetime-day-of-week-in-month.xml", "v2")]
        //[Variation(id = 16, Desc = "Xslt Scenarios : datetime-difference)]
        [InlineData("datetime-source.xml", "datetime-difference.xsl", "valid", "datetime-difference.xml", "v2")]
        //[Variation(id = 17, Desc = "Xslt Scenarios : datetime-duration)]
        [InlineData("datetime-source.xml", "datetime-duration.xsl", "valid", "datetime-duration.xml", "v2")]
        //[Variation(id = 18, Desc = "Xslt Scenarios : datetime-format-date)]
        ///[InlineData("datetime-source.xml", "datetime-format-date.xsl", "valid", "datetime-format-date.xml", "v2")] //disabled as references libraries not present on corefx
        //[Variation(id = 19, Desc = "Xslt Scenarios : datetime-hour-in-day)]
        [InlineData("datetime-source.xml", "datetime-hour-in-day.xsl", "valid", "datetime-hour-in-day.xml", "v2")]
        //[Variation(id = 20, Desc = "Xslt Scenarios : datetime-leap-year)]
        [InlineData("datetime-source.xml", "datetime-leap-year.xsl", "valid", "datetime-leap-year.xml", "v2")]
        //[Variation(id = 21, Desc = "Xslt Scenarios : datetime-max)]
        [InlineData("datetime-source1.xml", "datetime-max.xsl", "valid", "datetime-max.xml", "v2")]
        //[Variation(id = 22, Desc = "Xslt Scenarios : datetime-min)]
        [InlineData("datetime-source1.xml", "datetime-min.xsl", "valid", "datetime-min.xml", "v2")]
        //[Variation(id = 23, Desc = "Xslt Scenarios : datetime-minute-in-hour)]
        [InlineData("datetime-source.xml", "datetime-minute-in-hour.xsl", "valid", "datetime-minute-in-hour.xml", "v2")]
        //[Variation(id = 24, Desc = "Xslt Scenarios : datetime-month-abbreviation)]
        [InlineData("datetime-source.xml", "datetime-month-abbreviation.xsl", "valid", "datetime-month-abbreviation.xml", "v2")]
        //[Variation(id = 25, Desc = "Xslt Scenarios : datetime-month-abbreviation1)]
        [InlineData("datetime-source1.xml", "datetime-month-abbreviation1.xsl", "valid", "datetime-month-abbreviation1.xml", "v2")]
        //[Variation(id = 26, Desc = "Xslt Scenarios : datetime-month-in-year)]
        [InlineData("datetime-source.xml", "datetime-month-in-year.xsl", "valid", "datetime-month-in-year.xml", "v2")]
        //[Variation(id = 27, Desc = "Xslt Scenarios : datetime-month-name)]
        [InlineData("datetime-source1.xml", "datetime-month-name.xsl", "valid", "datetime-month-name.xml", "v2")]
        //[Variation(id = 28, Desc = "Xslt Scenarios : datetime-month-name1)]
        [InlineData("datetime-source1.xml", "datetime-month-name1.xsl", "valid", "datetime-month-name1.xml", "v2")]
        //[Variation(id = 29, Desc = "Xslt Scenarios : datetime-parse-date)]
        [InlineData("datetime-source.xml", "datetime-parse-date.xsl", "valid", "datetime-parse-date.xml", "v2")]
        //[Variation(id = 30, Desc = "Xslt Scenarios : datetime-second-in-minute)]
        [InlineData("datetime-source.xml", "datetime-second-in-minute.xsl", "valid", "datetime-second-in-minute.xml", "v2")]
        //[Variation(id = 31, Desc = "Xslt Scenarios : datetime-seconds)]
        [InlineData("datetime-source.xml", "datetime-seconds.xsl", "valid", "datetime-seconds.xml", "v2")]
        //[Variation(id = 32, Desc = "Xslt Scenarios : datetime-sum)]
        [InlineData("datetime-source.xml", "datetime-sum.xsl", "valid", "datetime-sum.xml", "v2")]
        //[Variation(id = 33, Desc = "Xslt Scenarios : datetime-time)]
        [InlineData("datetime-source.xml", "datetime-time.xsl", "valid", "datetime-time.xml", "v2")]
        //[Variation(id = 34, Desc = "Xslt Scenarios : datetime-week-in-month)]
        [InlineData("datetime-source.xml", "datetime-week-in-month.xsl", "valid", "datetime-week-in-month.xml", "v2")]
        //[Variation(id = 35, Desc = "Xslt Scenarios : datetime-week-in-year)]
        [InlineData("datetime-source.xml", "datetime-week-in-year.xsl", "valid", "datetime-week-in-year.xml", "v2")]
        //[Variation(id = 36, Desc = "Xslt Scenarios : datetime-year)]
        [InlineData("datetime-source.xml", "datetime-year.xsl", "valid", "datetime-year.xml", "v2")]
        //[Variation(id = 37, Desc = "Xslt Scenarios : dynamic-evaluate)]
        [InlineData("datetime-source1.xml", "dynamic-evaluate.xsl", "invalid", "datetime-year.xml", "v2")]
        //[Variation(id = 38, Desc = "Xslt Scenarios : math-abs)]
        [InlineData("math-source.xml", "math-abs.xsl", "valid", "math-abs.xml", "v2")]
        //[Variation(id = 39, Desc = "Xslt Scenarios : math-acos)]
        [InlineData("math-source.xml", "math-acos.xsl", "valid", "math-acos2.xml", "v2")]
        //[Variation(id = 40, Desc = "Xslt Scenarios : math-asin)]
        [InlineData("math-source.xml", "math-asin.xsl", "valid", "math-asin2.xml", "v2")]
        //[Variation(id = 41, Desc = "Xslt Scenarios : math-atan)]
        [InlineData("math-source.xml", "math-atan.xsl", "valid", "math-atan_2.xml", "v2")]
        //[Variation(id = 42, Desc = "Xslt Scenarios : math-atan2)]
        [InlineData("math-source.xml", "math-atan2.xsl", "valid", "math-atan2_2.xml", "v2")]
        //[Variation(id = 43, Desc = "Xslt Scenarios : math-avg)]
        [InlineData("math-source1.xml", "math-avg.xsl", "valid", "math-avg.xml", "v2")]
        //[Variation(id = 44, Desc = "Xslt Scenarios : math-constant)]
        [InlineData("math-source.xml", "math-constant.xsl", "valid", "math-constant2.xml", "v2")]
        //[Variation(id = 45, Desc = "Xslt Scenarios : math-cos)]
        [InlineData("math-source.xml", "math-cos.xsl", "valid", "math-cos2.xml", "v2")]
        //[Variation(id = 46, Desc = "Xslt Scenarios : math-exp)]
        [InlineData("math-source.xml", "math-exp.xsl", "valid", "math-exp2.xml", "v2")]
        //[Variation(id = 47, Desc = "Xslt Scenarios : math-highest)]
        [InlineData("math-source.xml", "math-highest.xsl", "invalid", "math-exp2.xml", "v2")]
        //[Variation(id = 48, Desc = "Xslt Scenarios : math-log)]
        [InlineData("math-source.xml", "math-log.xsl", "valid", "math-log2.xml", "v2")]
        //[Variation(id = 49, Desc = "Xslt Scenarios : math-lowest)]
        [InlineData("math-source.xml", "math-lowest.xsl", "invalid", "math-log2.xml", "v2")]
        //[Variation(id = 50, Desc = "Xslt Scenarios : math-max)]
        [InlineData("math-source.xml", "math-max.xsl", "valid", "math-max.xml", "v2")]
        //[Variation(id = 51, Desc = "Xslt Scenarios : math-min)]
        [InlineData("math-source.xml", "math-min.xsl", "valid", "math-min.xml", "v2")]
        //[Variation(id = 52, Desc = "Xslt Scenarios : math-power)]
        [InlineData("math-source.xml", "math-power.xsl", "valid", "math-power.xml", "v2")]
        //[Variation(id = 53, Desc = "Xslt Scenarios : math-random)]
        [InlineData("math-source.xml", "math-random.xsl", "valid", "math-random.xml", "v2")]
        //[Variation(id = 54, Desc = "Xslt Scenarios : math-sin)]
        [InlineData("math-source.xml", "math-sin.xsl", "valid", "math-sin2.xml", "v2")]
        //[Variation(id = 55, Desc = "Xslt Scenarios : math-sqrt)]
        [InlineData("math-source.xml", "math-sqrt.xsl", "valid", "math-sqrt.xml", "v2")]
        //[Variation(id = 56, Desc = "Xslt Scenarios : math-tan)]
        [InlineData("math-source.xml", "math-tan.xsl", "valid", "math-tan2.xml", "v2")]
        //[Variation(id = 57, Desc = "Xslt Scenarios : random-sequence)]
        [InlineData("random-source.xml", "random-sequence.xslt", "valid", "random-sequence.xml", "v2")]
        //[Variation(id = 58, Desc = "Xslt Scenarios : regex-match)]
        ///[InlineData("regex-source.xml", "regex-match.xsl", "valid", "regex-match.xml", "v2")] //disabled as references libraries not present on corefx
        //[Variation(id = 59, Desc = "Xslt Scenarios : regex-replace)]
        ///[InlineData("regex-source.xml", "regex-replace.xsl", "valid", "regex-replace.xml", "v2")] //disabled as references libraries not present on corefx
        //[Variation(id = 60, Desc = "Xslt Scenarios : regex-test)]
        ///[InlineData("regex-source.xml", "regex-test.xsl", "valid", "regex-test.xml", "v2")] //disabled as references libraries not present on corefx
        //[Variation(id = 61, Desc = "Xslt Scenarios : regex-tokenize)]
        ///[InlineData("regex-source1.xml", "regex-tokenize.xsl", "valid", "regex-tokenize.xml", "v2")] //disabled as references libraries not present on corefx
        //[Variation(id = 62, Desc = "Xslt Scenarios : sets-difference)]
        [InlineData("sets-source.xml", "sets-difference.xsl", "invalid", "regex-tokenize.xml", "v2")]
        //[Variation(id = 63, Desc = "Xslt Scenarios : sets-distinct)]
        [InlineData("sets-source.xml", "sets-distinct.xsl", "invalid", "regex-tokenize.xml", "v2")]
        //[Variation(id = 64, Desc = "Xslt Scenarios : sets-has-same-node)]
        ///[InlineData("sets-source.xml", "sets-has-same-node.xsl", "valid", "sets-has-same-node.xml", "v2")] //disabled as references libraries not present on corefx
        //[Variation(id = 65, Desc = "Xslt Scenarios : sets-intersection)]
        [InlineData("sets-source.xml", "sets-intersection.xsl", "invalid", "sets-has-same-node.xml", "v2")]
        //[Variation(id = 66, Desc = "Xslt Scenarios : sets-leading)]
        [InlineData("sets-source.xml", "sets-leading.xsl", "invalid", "sets-has-same-node.xml", "v2")]
        //[Variation(id = 67, Desc = "Xslt Scenarios : sets-subset)]
        ///[InlineData("sets-source1.xml", "sets-subset.xsl", "valid", "sets-subset.xml", "v2")] //disabled as references libraries not present on corefx
        //[Variation(id = 68, Desc = "Xslt Scenarios : sets-trailing)]
        [InlineData("sets-source.xml", "sets-trailing.xsl", "invalid", "sets-subset.xml", "v2")]
        //[Variation(id = 69, Desc = "Xslt Scenarios : string-align)]
        [InlineData("string-source.xml", "string-align.xsl", "valid", "string-align.xml", "v2")]
        //[Variation(id = 70, Desc = "Xslt Scenarios : string-concat)]
        [InlineData("string-source.xml", "string-concat.xsl", "valid", "string-concat.xml", "v2")]
        //[Variation(id = 71, Desc = "Xslt Scenarios : string-decode-uri)]
        ///[InlineData("string-source.xml", "string-decode-uri.xsl", "valid", "string-decode-uri.xml", "v2")] //disabled as references libraries not present on corefx
        //[Variation(id = 72, Desc = "Xslt Scenarios : string-encode-uri)]
        [InlineData("string-source.xml", "string-encode-uri.xsl", "valid", "string-encode-uri.xml", "v2")]
        //[Variation(id = 73, Desc = "Xslt Scenarios : string-lowercase)]
        [InlineData("string-source1.xml", "string-lowercase.xsl", "valid", "string-lowercase.xml", "v2")]
        //[Variation(id = 74, Desc = "Xslt Scenarios : string-padding)]
        [InlineData("string-source.xml", "string-padding.xsl", "valid", "string-padding.xml", "v2")]
        //[Variation(id = 75, Desc = "Xslt Scenarios : string-replace)]
        [InlineData("string-source.xml", "string-replace.xsl", "valid", "string-replace.xml", "v2")]
        //[Variation(id = 76, Desc = "Xslt Scenarios : string-split)]
        ///[InlineData("string-source.xml", "string-split.xsl", "valid", "string-split.xml", "v2")] //disabled as references libraries not present on corefx
        //[Variation(id = 77, Desc = "Xslt Scenarios : string-tokenize)]
        ///[InlineData("string-source.xml", "string-tokenize.xsl", "valid", "string-tokenize.xml", "v2")] //disabled as references libraries not present on corefx
        //[Variation(id = 78, Desc = "Xslt Scenarios : string-uppercase)]
        [InlineData("string-source1.xml", "string-uppercase.xsl", "valid", "string-uppercase.xml", "v2")]
        [Theory]
        public void RunEXslTest(object param0, object param1, object param2, object param3, object param4)
        {
            string OutFile = "out_exslt.xml";
            string xmlFile = Path.Combine(@"TestFiles\", FilePathUtil.GetTestDataPath(), @"XsltScenarios\EXslt\", param0.ToString());
            string xslFile = Path.Combine(@"TestFiles\", FilePathUtil.GetTestDataPath(), @"XsltScenarios\EXslt\", param1.ToString());
            string resultType = param2.ToString();
            string baseLine = Path.Combine(@"TestFiles\", FilePathUtil.GetTestDataPath(), @"XsltScenarios\EXslt\out\", param3.ToString());

#pragma warning disable 0618
            if (param4.ToString() == "v1")
                _xsltV1 = new XslTransform();
#pragma warning restore 0618
            else if (param4.ToString() == "v2")
                _xsltV2 = new XslCompiledTransform();

            Utils util = new Utils(_output);

            _output.WriteLine("XmlFile : file:\\\\" + xmlFile);
            _output.WriteLine("XslFile : file:\\\\" + xslFile);

            if (resultType == "valid")
                _output.WriteLine("BaseLine : file:\\\\" + baseLine);
            else
                _output.WriteLine("Test Type : Invalid");

            //Create the argument list and load the source document
            XsltArgumentList argList = InitArgumentList();
            XPathDocument doc = null;

            try
            {
                doc = new XPathDocument(xmlFile);
            }
            catch (Exception e)
            {
                _output.WriteLine(e.ToString());
                Assert.True(false);
            }

            //Delete the output file if it exists

            using (Stream stm = new FileStream(OutFile, FileMode.Create, FileAccess.ReadWrite))
            {
                try
                {
                    if (param4.ToString() == "v1")
                    {
                        XPathDocument xslDoc = new XPathDocument(xslFile);
                        //Evidence evidence = new Evidence();
                        //evidence.AddHost(new Zone(SecurityZone.MyComputer));
                        //evidence.AddHost(new Zone(SecurityZone.Intranet));

                        _xsltV1.Load(xslDoc, new XmlUrlResolver()/*, evidence*/);
                        _xsltV1.Transform(doc, argList, stm, new XmlUrlResolver());
                    }
                    else if (param4.ToString() == "v2")
                    {
                        _xsltV2.Load(xslFile, XsltSettings.TrustedXslt, new XmlUrlResolver());
                        _xsltV2.Transform(doc, argList, stm);
                    }
                    stm.Dispose();
                    util.VerifyResult(OutFile, baseLine);
                }

                //For V1 Transform
                catch (XPathException ex)
                {
                    _output.WriteLine(ex.Message);
                    if (resultType == "invalid")
                        return;
                    else
                        Assert.True(false);
                }

                //For V2 Transform
                catch (XsltException ex)
                {
                    _output.WriteLine(ex.Message);
                    if (resultType == "invalid")
                        return;
                    else
                        Assert.True(false);
                }
            }
        }

        public enum x
        {
            None = 0,
            Common = 1,
            DatesAndTimes = 2,
        }

        private XsltArgumentList InitArgumentList()
        {
            XsltArgumentList argList = new XsltArgumentList();
            x xinstance = new x();
            if (xinstance.ToString() == null)
            { Assert.True(false); }
            ExsltDatesAndTimes date = new ExsltDatesAndTimes();
            ExsltCommon exsl = new ExsltCommon();
            //ExsltFunctionNamespace func = new ExsltFunctionNamespace();
            ExsltMath math = new ExsltMath();
            ExsltRandom random = new ExsltRandom();
            ExsltRegularExpressions regexp = new ExsltRegularExpressions();
            ExsltSets set = new ExsltSets();
            ExsltStrings str = new ExsltStrings();

            GDNDatesAndTimes date2 = new GDNDatesAndTimes();
            GDNDynamic dyn2 = new GDNDynamic();
            GDNMath math2 = new GDNMath();
            GDNRegularExpressions regexp2 = new GDNRegularExpressions();
            GDNSets set2 = new GDNSets();
            GDNStrings str2 = new GDNStrings();

            //EXSLT Objects
            argList.AddExtensionObject("http://exslt.org/dates-and-times", date);
            //argList.AddExtensionObject("http://exslt.org/dynamic", dyn);
            argList.AddExtensionObject("http://exslt.org/common", exsl);
            //argList.AddExtensionObject("http://exslt.org/functions", func);
            argList.AddExtensionObject("http://exslt.org/math", math);
            argList.AddExtensionObject("http://exslt.org/random", random);
            argList.AddExtensionObject("http://exslt.org/regular-expressions", regexp);
            argList.AddExtensionObject("http://exslt.org/sets", set);
            argList.AddExtensionObject("http://exslt.org/strings", str);

            //GotDotNet Objects
            argList.AddExtensionObject("http://gotdotnet.com/exslt/dates-and-times", date2);
            argList.AddExtensionObject("http://gotdotnet.com/exslt/dynamic", dyn2);
            argList.AddExtensionObject("http://gotdotnet.com/exslt/math", math2);
            argList.AddExtensionObject("http://gotdotnet.com/exslt/regular-expressions", regexp2);
            argList.AddExtensionObject("http://gotdotnet.com/exslt/sets", set2);
            argList.AddExtensionObject("http://gotdotnet.com/exslt/strings", str2);

            return argList;
        }


        // Helper method to autogenerate Exslt xunit test data from XsltScenarios.xml
        public void AutoGenerateXunitTestData()
        {
            StreamWriter sw = new StreamWriter(new FileStream("XunitTestList.txt", FileMode.Create, FileAccess.Write));

            //Load the control file
            XmlDocument doc = new XmlDocument { XmlResolver = new XmlUrlResolver() };
            doc.Load("XsltScenarios.xml");

            XmlNodeList TestCases = doc.DocumentElement.SelectNodes("//Variation");

            foreach (XmlNode Variation in TestCases)
            {
                foreach (string ver in new string[] { "v1", "v2" })
                {
                    string variationDesc = Variation.Attributes.GetNamedItem("Desc").Value;
                    int variationId = Convert.ToInt32(Variation.Attributes.GetNamedItem("Id").Value);

                    sw.WriteLine("//[Variation(id = " + variationId.ToString() + ", Desc = \"" + variationDesc + ")]");

                    _xmlFile = Variation.SelectSingleNode("Data/Xml").InnerText;
                    _xslFile = Variation.SelectSingleNode("Data/Xsl").InnerText;

                    string ResultType = Variation.SelectSingleNode("Data/Result[1]/@Type").Value;
                    ResultType = ResultType.ToLower();

                    if (ResultType == "valid")
                    {
                        _baseline = Variation.SelectSingleNode("Data/Result").InnerText;
                        try
                        {
                            _baseline = Variation.SelectSingleNode("Data/Result[@TransformType='" + ver + "']").InnerText; ;
                        }
                        catch (NullReferenceException)
                        {
                        }
                    }
                    sw.WriteLine("[InlineData(" + "\"" + _xmlFile + "\", " + "\"" + _xslFile + "\", " + "\"" + ResultType + "\", " + "\"" + _baseline + "\", " + "\"" + ver + "\"" + ")]");
                }
            }

            sw.WriteLine("[Theory]");
            sw.Dispose();
        }
    }
}