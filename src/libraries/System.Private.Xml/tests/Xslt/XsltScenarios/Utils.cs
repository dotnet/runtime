// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;
using Xunit.Abstractions;
using System;
using System.Globalization;
using System.IO;
using System.Xml;
using System.Xml.XmlDiff;

namespace System.Xml.Tests
{
    internal class Utils //: CTestCase
    {
        private static ITestOutputHelper s_output;
        public Utils(ITestOutputHelper output)
        {
            s_output = output;
        }

        public static void VerifyTest(string actResult, string expResult)
        {
            XmlDiff.XmlDiff diff = new XmlDiff.XmlDiff();
            diff.Option = XmlDiffOption.IgnoreEmptyElement | XmlDiffOption.IgnoreAttributeOrder;

            XmlTextReader xrActual = new XmlTextReader(new StringReader(actResult));
            XmlTextReader xrExpected = new XmlTextReader(new StringReader(expResult));

            bool bResult = false;

            try
            {
                bResult = diff.Compare(xrActual, xrExpected);
            }
            catch (Exception e)
            {
                bResult = false;
                s_output.WriteLine("Exception thrown in XmlDiff compare!");
                s_output.WriteLine(e.ToString());
            }
            finally
            {
                if (xrActual != null) xrActual.Dispose();
                if (xrExpected != null) xrExpected.Dispose();
            }

            s_output.WriteLine("Expected : " + expResult);
            s_output.WriteLine("Actual : " + actResult);

            if (bResult)
                return;
            else
                Assert.True(false);
        }

        public void VerifyResult(string actualFile, string baselineFile)
        {
            XmlDiff.XmlDiff diff = new XmlDiff.XmlDiff();
            diff.Option = XmlDiffOption.IgnoreEmptyElement | XmlDiffOption.IgnoreAttributeOrder;

            XmlParserContext context = new XmlParserContext(new NameTable(), null, "", XmlSpace.None);

            using (FileStream fsActual = new FileStream(actualFile, FileMode.Open, FileAccess.Read))
            {
                using (FileStream fsExpected = new FileStream(baselineFile, FileMode.Open, FileAccess.Read))
                {
                    XmlTextReader xrActual = new XmlTextReader(fsActual, XmlNodeType.Element, context);
                    XmlTextReader xrExpected = new XmlTextReader(fsExpected, XmlNodeType.Element, context);

                    bool bResult = false;

                    try
                    {
                        bResult = diff.Compare(xrActual, xrExpected);
                    }
                    catch (Exception e)
                    {
                        bResult = false;
                        s_output.WriteLine("Exception thrown in XmlDiff compare!");
                        s_output.WriteLine(e.ToString());
                    }
                    s_output.WriteLine("Actual result: ");
                    this.WriteFile(actualFile);

                    if (bResult)
                        return;
                    else
                    {
                        s_output.WriteLine("Mismatch in XmlDiff");
                        Assert.True(false);
                    }
                }
            }
        }

        public void WriteFile(string fileName)
        {
            Char[] rgBuffer = new Char[4096];
            StreamReader fs = null;

            fs = new StreamReader(new FileStream(fileName, FileMode.Open, FileAccess.Read));

            int cBytesRead = fs.Read(rgBuffer, 0, 4096);
            while (cBytesRead > 0)
            {
                //_output.WriteLine(new string(rgBuffer));
                cBytesRead = fs.Read(rgBuffer, 0, 4096);
            }

            if (fs != null) fs.Dispose();
            s_output.WriteLine("");
        }

        public void VerifyChecksum(string outFile, string baseline)
        {
            string actualChecksum = "0";    //For no output (empty string) checksums are 0
            string expectedCheckSum = "0";  //For no output (empty string) checksums are 0

            if (outFile != string.Empty)
                actualChecksum = CalcChecksum(outFile);
            if (baseline != string.Empty)
                expectedCheckSum = CalcChecksum(baseline);

            if (expectedCheckSum == actualChecksum)
                return;
            else
            {
                s_output.WriteLine("Actual checksum: {0}, Expected checksum: {1}", actualChecksum, expectedCheckSum);
                s_output.WriteLine("Actual result: ");
                WriteFile(outFile);
                Assert.True(false);
            }
        }

        public string CalcChecksum(string fileName)
        {
            Decimal dResult = 0;		// Numerical value of the checksum
            int i = 0;					// Generic counter
            int cBytesRead = 1;			// # of bytes read at one time
            int cTotalRead = 0;			// Total # of bytes read so far
            Decimal dEndBuffer = 0;		// Buffer to remove from the end (This is necessary because
            // notepad adds CR/LF onto the end of every file)
            Char[] rgBuffer = new Char[4096];

            string xml = "";

            StreamReader fs = null;
            try
            {
                fs = new StreamReader(new FileStream(fileName, FileMode.Open, FileAccess.Read));
                cBytesRead = fs.Read(rgBuffer, 0, 4096);
                while (cBytesRead > 0)
                {
                    // Keep XML property up to date
                    xml = String.Concat(xml, new String(rgBuffer, 0, cBytesRead));

                    // Calculate the checksum
                    for (i = 0; i < cBytesRead; i++)
                    {
                        dResult += Math.Round((Decimal)(rgBuffer[i] / (cTotalRead + i + 1.0)), 10);
                    }

                    cTotalRead += cBytesRead;
                    dEndBuffer = 0;

                    // Keep reading (in case file is bigger than 4K)
                    cBytesRead = fs.Read(rgBuffer, 0, 4096);
                }
            }
            catch (Exception ex)
            {
                s_output.WriteLine(ex.ToString());
                return "";
            }
            finally
            {
                if (fs != null) fs.Dispose();
            }
            return Convert.ToString(dResult - dEndBuffer, NumberFormatInfo.InvariantInfo);
        }
    }
}