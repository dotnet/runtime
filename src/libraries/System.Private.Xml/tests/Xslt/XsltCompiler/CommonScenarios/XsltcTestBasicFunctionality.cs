// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;
using Xunit.Abstractions;

namespace System.Xml.Tests
{
    //[TestCase(Name = "Basic Functionality", Desc = "This Testcase maps to test variations described in 'Basic Functional Tests'")]
    public class XsltcTestBasicFunctionality : XsltcTestCaseBase
    {
        private ITestOutputHelper _output;
        public XsltcTestBasicFunctionality(ITestOutputHelper output) : base(output)
        {
            _output = output;
        }

        [ActiveIssue(30017)]
        // All variations that are Invalid are run by Var2()
        //[Variation("15", Desc = "Exercise what would be a circular reference, a primary stylesheet that includes another, followed by the include itself", Pri = 2, Params = new object[] { "bft15a.xsl bft15b.xsl", "bft15.txt", "EnglishOnly" })]
        [InlineData("bft15a.xsl bft15b.xsl", "bft15.txt"/*, "EnglishOnly"*/)]
        //[Variation("27", Desc = "Exercise whitespace before flag values", Pri = 1, Params = new object[] { "/ out:bft27.dll bft27.xsl", "bft27.txt" })]
        [InlineData("/ out:bft27.dll bft27.xsl", "bft27.txt")]
        //[Variation("30", Desc = "Exercise help with option values", Pri = 1, Params = new object[] { "bft2.xsl /? bft2.xsl", "help.txt" })]
        [InlineData("bft2.xsl /? bft2.xsl", "help.txt")]
        //[Variation("35", Desc = "Device name as stylesheetname", Pri = 1, Params = new object[] { "nul.xsl", "bft35.txt" })]
        [InlineData("nul.xsl", "bft35.txt")]
        //[Variation("37", Desc = "No input source", Pri = 1, Params = new object[] { "/debug+", "bft37.txt" })]
        [InlineData("/debug+", "bft37.txt")]
        //[Variation("38", Desc = "Empty string in arguments", Pri = 1, Params = new object[] { "\"\"", "bft38.txt" })]
        [InlineData("\"\"", "bft38.txt")]
        [Trait("category", "XsltcExeRequired")]
        [ConditionalTheory(nameof(xsltcExeFound))]
        public void Var2(object param0, object param1)
        {
            if (ShouldSkip(new object[] { param0, param1 }))
            {
                return;// TEST_SKIPPED;
            }
            string cmdLine = param0.ToString();
            string baselineFile = param1.ToString();

            VerifyTest(cmdLine, baselineFile, _createFromInputFile);
        }
    }
}