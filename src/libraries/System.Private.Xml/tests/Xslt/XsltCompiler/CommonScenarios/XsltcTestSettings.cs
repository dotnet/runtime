// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;
using Xunit.Abstractions;

namespace System.Xml.Tests
{
    //[TestCase(Name = "Settings Tests", Desc = "This Testcase maps to test variations described in 'Settings Functional Tests'")]
    public class XsltcTestSettings : XsltcTestCaseBase
    {
        private ITestOutputHelper _output;
        public XsltcTestSettings(ITestOutputHelper output) : base(output)
        {
            _output = output;
        }

        [ActiveIssue(30017)]
        //[Variation("4", Desc = "Basic /settings test cases, stylesheet has script, where script-", Pri = 0, Params = new object[] { "/settings:script- sft4.xsl", "sft4hack.dll", "no", "sft4", "sft4.pdb", "no", "sft4.txt" })]
        [InlineData("/settings:script- sft4.xsl", "sft4hack.dll", "no", "sft4", "sft4.pdb", "no", "sft4.txt")]
        //[Variation("6", Desc = "Basic /settings test cases, stylesheet has document(), where document-", Pri = 0, Params = new object[] { "/settings:document- sft6.xsl", "sft6hack.dll", "no", "sft6", "sft6.pdb", "no", "sft6.txt" })]
        [InlineData("/settings:document- sft6.xsl", "sft6hack.dll", "no", "sft6", "sft6.pdb", "no", "sft6.txt")]
        //[Variation("7", Desc = "Exercise settings with no colon", Pri = 0, Params = new object[] { "/settingsDTD+ sft7.xsl", "sft7.dll", "no", "sft7", "sft7.pdb", "no", "sft7.txt" })]
        [InlineData("/settingsDTD+ sft7.xsl", "sft7.dll", "no", "sft7", "sft7.pdb", "no", "sft7.txt")]
        //[Variation("14", Desc = "Exercise Turkish I problem", Pri = 0, Params = new object[] { "/SETTİNGS:SCRİPT+ sft14.xsl", "sft14.dll", "no", "sft14", "sft14.pdb", "no", "sft14.txt", "EnglishOnly" })]
        [InlineData("/SETT\u0130NGS:SCR\u0130PT+ sft14.xsl", "sft14.dll", "no", "sft14", "sft14.pdb", "no", "sft14.txt"/*, "EnglishOnly"*/)]
        //[Variation("15", Desc = "Exercise conflicting +, - symbols", Pri = 0, Params = new object[] { "/settings:script+- sft15.xsl", "sft15.dll", "no", "sft15", "sft15.pdb", "no", "sft15.txt" })]
        [InlineData("/settings:script+- sft15.xsl", "sft15.dll", "no", "sft15", "sft15.pdb", "no", "sft15.txt")]
        //[Variation("28", Desc = "Basic /settings test cases, imported stylesheet has DTD, where DTD-", Pri = 0, Params = new object[] { "/settings:DTD- sft28.xsl", "sft28.dll", "no", "sft28", "sft28.pdb", "no", "sft28.txt" })]
        [InlineData("/settings:DTD- sft28.xsl", "sft28.dll", "no", "sft28", "sft28.pdb", "no", "sft28.txt")]
        //[Variation("30", Desc = "Basic /settings test cases, imported stylesheet has script, where script-", Pri = 0, Params = new object[] { "/settings:script- sft30.xsl", "sft30hack.dll", "no", "sft30", "sft30.pdb", "no", "sft30.txt" })]
        [InlineData("/settings:script- sft30.xsl", "sft30hack.dll", "no", "sft30", "sft30.pdb", "no", "sft30.txt")]
        //[Variation("36", Desc = "Basic /settings test cases, included stylesheet has script, where script-", Pri = 0, Params = new object[] { "/settings:script- sft36.xsl", "sft36hack.dll", "no", "sft36", "sft36.pdb", "no", "sft36.txt" })]
        [InlineData("/settings:script- sft36.xsl", "sft36hack.dll", "no", "sft36", "sft36.pdb", "no", "sft36.txt")]
        //[Variation("38", Desc = "Basic /settings test cases, included stylesheet has document(), where document-", Pri = 0, Params = new object[] { "/settings:document- sft38.xsl", "sft38hack.dll", "no", "sft38", "sft38.pdb", "no", "sft38.txt" })]
        [InlineData("/settings:document- sft38.xsl", "sft38hack.dll", "no", "sft38", "sft38.pdb", "no", "sft38.txt")]
        //[Variation("41", Desc = "Exercise basic use case invalid option specified", Pri = 0, Params = new object[] { "/settings:DTD~ sft41.xsl", "sft41.dll", "no", "sft41", "sft41.pdb", "no", "sft41.txt" })]
        [InlineData("/settings:DTD~ sft41.xsl", "sft41.dll", "no", "sft41", "sft41.pdb", "no", "sft41.txt")]
        //[Variation("42", Desc = "Exercise basic use case invalid option specified", Pri = 0, Params = new object[] { "/settings:script* sft42.xsl", "sft42.dll", "no", "sft42", "sft42.pdb", "no", "sft42.txt" })]
        [InlineData("/settings:script* sft42.xsl", "sft42.dll", "no", "sft42", "sft42.pdb", "no", "sft42.txt")]
        //[Variation("44", Desc = "Exercise basic use case multiple options specified", Pri = 0, Params = new object[] { "/settings:script+document- sft44.xsl", "sft44.dll", "no", "sft44", "sft44.pdb", "no", "sft44.txt" })]
        [InlineData("/settings:script+document- sft44.xsl", "sft44.dll", "no", "sft44", "sft44.pdb", "no", "sft44.txt")]
        //[Variation("46", Desc = "Exercise basic use case invalid option specified", Pri = 0, Params = new object[] { "/script+ sft46.xsl", "sft46.dll", "no", "sft46", "sft46.pdb", "no", "sft46.txt" })]
        [InlineData("/script+ sft46.xsl", "sft46.dll", "no", "sft46", "sft46.pdb", "no", "sft46.txt")]
        //[Variation("48", Desc = "Exercise basic use case invalid option specified", Pri = 0, Params = new object[] { "/DTD- sft48.xsl", "sft48.dll", "no", "sft48", "sft48.pdb", "no", "sft48.txt" })]
        [InlineData("/DTD- sft48.xsl", "sft48.dll", "no", "sft48", "sft48.pdb", "no", "sft48.txt")]
        //[Variation("49", Desc = "Exercise basic use case invalid option specified", Pri = 0, Params = new object[] { "/document sft49.xsl", "sft49.dll", "no", "sft49", "sft49.pdb", "no", "sft49.txt" })]
        [InlineData("/document sft49.xsl", "sft49.dll", "no", "sft49", "sft49.pdb", "no", "sft49.txt")]
        //[Variation("51", Desc = "Regression: Basic /settings test cases, two stylesheet with the same script block", Pri = 0, Params = new object[] { "/settings:script+ sft3.xsl sft4.xsl", "sft3.dll", "yes", "", "sft3.pdb", "no", "sft3.txt" })]
        [InlineData("/settings:script+ sft3.xsl sft4.xsl", "sft3.dll", "yes", "", "sft3.pdb", "no", "sft3.txt")]
        [Trait("category", "XsltcExeRequired")]
        [ConditionalTheory(nameof(xsltcExeFound))]
        public void Var1(object param0, object param1, object param2, object param3, object param4, object param5, object param6)
        {
            string cmdLine = param0.ToString();
            string asmName = param1.ToString();
            bool asmCreated = string.Compare(param2.ToString(), "yes", true) == 0;
            string typeName = param3.ToString();
            string pdbName = param4.ToString();
            bool pdbCreated = string.Compare(param5.ToString(), "yes", true) == 0;
            string baselineFile = param6.ToString();
            if (ShouldSkip(new object[] { param0, param1, param2, param3, param4, param5, param6 }))
            {
                return; //TEST_SKIPPED;
            }

            VerifyTest(cmdLine, asmName, asmCreated, typeName, pdbName, pdbCreated, baselineFile, _createFromInputFile);
        }
    }
}