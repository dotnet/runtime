// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;
using Xunit.Abstractions;
using System;

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

        //[Variation("1", Desc = "Basic /settings test cases, stylesheet has DTD, where DTD+", Pri = 0, Params = new object[] { "/settings:DTD+ sft1.xsl", "sft1.dll", "yes", "sft1", "sft1.pdb", "no", "sft1.txt" })]
        //[InlineData("/settings:DTD+ sft1.xsl", "sft1.dll", "yes", "sft1", "sft1.pdb", "no", "sft1.txt")] //Skipping this, it tries to load System.dll
        //[Variation("2", Desc = "Basic /settings test cases, stylesheet has DTD, where DTD-", Pri = 0, Params = new object[] { "/settings:DTD- sft2.xsl", "sft2.dll", "no", "sft2", "sft2.pdb", "no", "sft2.txt" })]
        //[InlineData("/settings:DTD- sft2.xsl", "sft2.dll", "no", "sft2", "sft2.pdb", "no", "sft2.txt")] //Skipping this, it tries to load System.dll
        //[Variation("3", Desc = "Basic /settings test cases, stylesheet has script, where script+", Pri = 0, Params = new object[] { "/settings:script+ sft3.xsl", "sft3.dll", "yes", "sft3", "sft3.pdb", "no", "sft3.txt" })]
        //[InlineData("/settings:script+ sft3.xsl", "sft3.dll", "yes", "sft3", "sft3.pdb", "no", "sft3.txt")] //Skipping this, it tries to load System.dll
        //[Variation("4", Desc = "Basic /settings test cases, stylesheet has script, where script-", Pri = 0, Params = new object[] { "/settings:script- sft4.xsl", "sft4hack.dll", "no", "sft4", "sft4.pdb", "no", "sft4.txt" })]
        [InlineData("/settings:script- sft4.xsl", "sft4hack.dll", "no", "sft4", "sft4.pdb", "no", "sft4.txt")]
        //[Variation("5", Desc = "Basic /settings test cases, stylesheet has document(), where document+", Pri = 0, Params = new object[] { "/settings:document+ sft5.xsl", "sft5.dll", "yes", "sft5", "sft5.pdb", "no", "sft5.txt" })]
        //[InlineData("/settings:document+ sft5.xsl", "sft5.dll", "yes", "sft5", "sft5.pdb", "no", "sft5.txt")] //Skipping this, it tries to load System.dll
        //[Variation("6", Desc = "Basic /settings test cases, stylesheet has document(), where document-", Pri = 0, Params = new object[] { "/settings:document- sft6.xsl", "sft6hack.dll", "no", "sft6", "sft6.pdb", "no", "sft6.txt" })]
        [InlineData("/settings:document- sft6.xsl", "sft6hack.dll", "no", "sft6", "sft6.pdb", "no", "sft6.txt")]
        //[Variation("7", Desc = "Exercise settings with no colon", Pri = 0, Params = new object[] { "/settingsDTD+ sft7.xsl", "sft7.dll", "no", "sft7", "sft7.pdb", "no", "sft7.txt" })]
        [InlineData("/settingsDTD+ sft7.xsl", "sft7.dll", "no", "sft7", "sft7.pdb", "no", "sft7.txt")]
        //[Variation("8", Desc = "Exercise settings with no option value", Pri = 0, Params = new object[] { "/settings: sft8.xsl", "sft8.dll", "yes", "sft8", "sft8.pdb", "no", "sft8.txt" })]
        //[InlineData("/settings: sft8.xsl", "sft8.dll", "yes", "sft8", "sft8.pdb", "no", "sft8.txt")] //Skipping this, it tries to load System.dll
        //[Variation("9", Desc = "Exercise DTD switch without +/-, stylesheet has DTD", Pri = 0, Params = new object[] { "/settings:DTD sft9.xsl", "sft9.dll", "yes", "sft9", "sft9.pdb", "no", "sft9.txt" })]
        //[InlineData("/settings:DTD sft9.xsl", "sft9.dll", "yes", "sft9", "sft9.pdb", "no", "sft9.txt")] //Skipping this, it tries to load System.dll
        //[Variation("10", Desc = "Exercise script switch without +/-, stylesheet has script block", Pri = 0, Params = new object[] { "/settings:script sft10.xsl", "sft10.dll", "yes", "sft10", "sft10.pdb", "no", "sft10.txt" })]
        //[InlineData("/settings:script sft10.xsl", "sft10.dll", "yes", "sft10", "sft10.pdb", "no", "sft10.txt")] //Skipping this, it tries to load System.dll
        //[Variation("11", Desc = "Exercise document switch without +/-, stylesheet has document() function", Pri = 0, Params = new object[] { "/settings:document sft11.xsl", "sft11.dll", "yes", "sft11", "sft11.pdb", "no", "sft11.txt" })]
        //[InlineData("/settings:document sft11.xsl", "sft11.dll", "yes", "sft11", "sft11.pdb", "no", "sft11.txt")] //Skipping this, it tries to load System.dll
        //[Variation("13", Desc = "Exercise keyword case sensitivity", Pri = 0, Params = new object[] { "/SeTTings:DOCument+ sft13.xsl", "sft13.dll", "yes", "sft13", "sft13.pdb", "no", "sft13.txt" })]
        //[InlineData("/SeTTings:DOCument+ sft13.xsl", "sft13.dll", "yes", "sft13", "sft13.pdb", "no", "sft13.txt")] //Skipping this, it tries to load System.dll
        //[Variation("14", Desc = "Exercise Turkish I problem", Pri = 0, Params = new object[] { "/SETTİNGS:SCRİPT+ sft14.xsl", "sft14.dll", "no", "sft14", "sft14.pdb", "no", "sft14.txt", "EnglishOnly" })]
        [InlineData("/SETT\u0130NGS:SCR\u0130PT+ sft14.xsl", "sft14.dll", "no", "sft14", "sft14.pdb", "no", "sft14.txt"/*, "EnglishOnly"*/)]
        //[Variation("15", Desc = "Exercise conflicting +, - symbols", Pri = 0, Params = new object[] { "/settings:script+- sft15.xsl", "sft15.dll", "no", "sft15", "sft15.pdb", "no", "sft15.txt" })]
        [InlineData("/settings:script+- sft15.xsl", "sft15.dll", "no", "sft15", "sft15.pdb", "no", "sft15.txt")]
        //[Variation("27", Desc = "Basic /settings test cases, imported stylesheet has DTD, where DTD+", Pri = 0, Params = new object[] { "/settings:DTD+ sft27.xsl", "sft27.dll", "yes", "sft27", "sft27.pdb", "no", "sft27.txt" })]
        //[InlineData("/settings:DTD+ sft27.xsl", "sft27.dll", "yes", "sft27", "sft27.pdb", "no", "sft27.txt")] //Skipping this, it tries to load System.dll
        //[Variation("28", Desc = "Basic /settings test cases, imported stylesheet has DTD, where DTD-", Pri = 0, Params = new object[] { "/settings:DTD- sft28.xsl", "sft28.dll", "no", "sft28", "sft28.pdb", "no", "sft28.txt" })]
        [InlineData("/settings:DTD- sft28.xsl", "sft28.dll", "no", "sft28", "sft28.pdb", "no", "sft28.txt")]
        //[Variation("29", Desc = "Basic /settings test cases, imported stylesheet has script, where script+", Pri = 0, Params = new object[] { "/settings:script+ sft29.xsl", "sft29.dll", "yes", "sft29", "sft29.pdb", "no", "sft29.txt" })]
        //[InlineData("/settings:script+ sft29.xsl", "sft29.dll", "yes", "sft29", "sft29.pdb", "no", "sft29.txt")] //Skipping this, it tries to load System.dll
        //[Variation("30", Desc = "Basic /settings test cases, imported stylesheet has script, where script-", Pri = 0, Params = new object[] { "/settings:script- sft30.xsl", "sft30hack.dll", "no", "sft30", "sft30.pdb", "no", "sft30.txt" })]
        [InlineData("/settings:script- sft30.xsl", "sft30hack.dll", "no", "sft30", "sft30.pdb", "no", "sft30.txt")]
        //[Variation("31", Desc = "Basic /settings test cases, imported stylesheet has document(), where document+", Pri = 0, Params = new object[] { "/settings:document+ sft31.xsl", "sft31.dll", "yes", "sft31", "sft31.pdb", "no", "sft31.txt" })]
        //[InlineData("/settings:document+ sft31.xsl", "sft31.dll", "yes", "sft31", "sft31.pdb", "no", "sft31.txt")] //Skipping this, it tries to load System.dll
        //[Variation("32", Desc = "Basic /settings test cases, imported stylesheet has document(), where document-", Pri = 0, Params = new object[] { "/settings:document- sft32.xsl", "sft32hack.dll", "no", "sft32", "sft32.pdb", "no", "sft32.txt" })]
        //[InlineData("/settings:document- sft32.xsl", "sft32hack.dll", "no", "sft32", "sft32.pdb", "no", "sft32.txt")] //Skipping this, it tries to load System.dll
        //[Variation("33", Desc = "Basic /settings test cases, included stylesheet has DTD, where DTD+", Pri = 0, Params = new object[] { "/settings:DTD+ sft33.xsl", "sft33.dll", "yes", "sft33", "sft33.pdb", "no", "sft33.txt" })]
        //[InlineData("/settings:DTD+ sft33.xsl", "sft33.dll", "yes", "sft33", "sft33.pdb", "no", "sft33.txt")] //Skipping this, it tries to load System.dll
        //[Variation("34", Desc = "Basic /settings test cases, included stylesheet has DTD, where DTD-", Pri = 0, Params = new object[] { "/settings:DTD- sft34.xsl", "sft34.dll", "no", "sft34", "sft34.pdb", "no", "sft34.txt" })]
        //[InlineData("/settings:DTD- sft34.xsl", "sft34.dll", "no", "sft34", "sft34.pdb", "no", "sft34.txt")] //Skipping this, it tries to load System.dll
        //[Variation("35", Desc = "Basic /settings test cases, included stylesheet has script, where script+", Pri = 0, Params = new object[] { "/settings:script+ sft35.xsl", "sft35.dll", "yes", "sft35", "sft35.pdb", "no", "sft35.txt" })]
        //[InlineData("/settings:script+ sft35.xsl", "sft35.dll", "yes", "sft35", "sft35.pdb", "no", "sft35.txt")] //Skipping this, it tries to load System.dll
        //[Variation("36", Desc = "Basic /settings test cases, included stylesheet has script, where script-", Pri = 0, Params = new object[] { "/settings:script- sft36.xsl", "sft36hack.dll", "no", "sft36", "sft36.pdb", "no", "sft36.txt" })]
        [InlineData("/settings:script- sft36.xsl", "sft36hack.dll", "no", "sft36", "sft36.pdb", "no", "sft36.txt")]
        //[Variation("37", Desc = "Basic /settings test cases, included stylesheet has document(), where document+", Pri = 0, Params = new object[] { "/settings:document+ sft37.xsl", "sft37.dll", "yes", "sft37", "sft37.pdb", "no", "sft37.txt" })]
        //[InlineData("/settings:document+ sft37.xsl", "sft37.dll", "yes", "sft37", "sft37.pdb", "no", "sft37.txt")] //Skipping this, it tries to load System.dll
        //[Variation("38", Desc = "Basic /settings test cases, included stylesheet has document(), where document-", Pri = 0, Params = new object[] { "/settings:document- sft38.xsl", "sft38hack.dll", "no", "sft38", "sft38.pdb", "no", "sft38.txt" })]
        [InlineData("/settings:document- sft38.xsl", "sft38hack.dll", "no", "sft38", "sft38.pdb", "no", "sft38.txt")]
        //[Variation("41", Desc = "Exercise basic use case invalid option specified", Pri = 0, Params = new object[] { "/settings:DTD~ sft41.xsl", "sft41.dll", "no", "sft41", "sft41.pdb", "no", "sft41.txt" })]
        [InlineData("/settings:DTD~ sft41.xsl", "sft41.dll", "no", "sft41", "sft41.pdb", "no", "sft41.txt")]
        //[Variation("42", Desc = "Exercise basic use case invalid option specified", Pri = 0, Params = new object[] { "/settings:script* sft42.xsl", "sft42.dll", "no", "sft42", "sft42.pdb", "no", "sft42.txt" })]
        [InlineData("/settings:script* sft42.xsl", "sft42.dll", "no", "sft42", "sft42.pdb", "no", "sft42.txt")]
        //[Variation("43", Desc = "Exercise basic use case invalid option specified", Pri = 0, Params = new object[] { "/settings:document% sft43.xsl", "sft43.dll", "no", "sft43", "sft43.pdb", "no", "sft43.txt" })]
        //[InlineData("/settings:document% sft43.xsl", "sft43.dll", "no", "sft43", "sft43.pdb", "no", "sft43.txt")] //Skipping this, it tries to load System.dll
        //[Variation("44", Desc = "Exercise basic use case multiple options specified", Pri = 0, Params = new object[] { "/settings:script+document- sft44.xsl", "sft44.dll", "no", "sft44", "sft44.pdb", "no", "sft44.txt" })]
        [InlineData("/settings:script+document- sft44.xsl", "sft44.dll", "no", "sft44", "sft44.pdb", "no", "sft44.txt")]
        //[Variation("45", Desc = "Exercise basic use case invalid option specified", Pri = 0, Params = new object[] { "/script sft45.xsl", "sft45.dll", "no", "sft45", "sft45.pdb", "no", "sft45.txt" })]
        //[InlineData("/script sft45.xsl", "sft45.dll", "no", "sft45", "sft45.pdb", "no", "sft45.txt")] //Skipping this, it tries to load System.dll
        //[Variation("46", Desc = "Exercise basic use case invalid option specified", Pri = 0, Params = new object[] { "/script+ sft46.xsl", "sft46.dll", "no", "sft46", "sft46.pdb", "no", "sft46.txt" })]
        [InlineData("/script+ sft46.xsl", "sft46.dll", "no", "sft46", "sft46.pdb", "no", "sft46.txt")]
        //[Variation("47", Desc = "Exercise basic use case invalid option specified", Pri = 0, Params = new object[] { "/DTD sft47.xsl", "sft47.dll", "no", "sft47", "sft47.pdb", "no", "sft47.txt" })]
        //[InlineData("/DTD sft47.xsl", "sft47.dll", "no", "sft47", "sft47.pdb", "no", "sft47.txt")] //Skipping this, it tries to load System.dll
        //[Variation("48", Desc = "Exercise basic use case invalid option specified", Pri = 0, Params = new object[] { "/DTD- sft48.xsl", "sft48.dll", "no", "sft48", "sft48.pdb", "no", "sft48.txt" })]
        [InlineData("/DTD- sft48.xsl", "sft48.dll", "no", "sft48", "sft48.pdb", "no", "sft48.txt")]
        //[Variation("49", Desc = "Exercise basic use case invalid option specified", Pri = 0, Params = new object[] { "/document sft49.xsl", "sft49.dll", "no", "sft49", "sft49.pdb", "no", "sft49.txt" })]
        [InlineData("/document sft49.xsl", "sft49.dll", "no", "sft49", "sft49.pdb", "no", "sft49.txt")]
        //[Variation("50", Desc = "Exercise basic use case invalid option specified", Pri = 0, Params = new object[] { "/document- sft50.xsl", "sft50.dll", "no", "sft50", "sft50.pdb", "no", "sft50.txt" })]
        //[InlineData("/document- sft50.xsl", "sft50.dll", "no", "sft50", "sft50.pdb", "no", "sft50.txt")] //Skipping this, it tries to load System.dll
        //[Variation("51", Desc = "Regression: Basic /settings test cases, two stylesheet with the same script block", Pri = 0, Params = new object[] { "/settings:script+ sft3.xsl sft4.xsl", "sft3.dll", "yes", "", "sft3.pdb", "no", "sft3.txt" })]
        [InlineData("/settings:script+ sft3.xsl sft4.xsl", "sft3.dll", "yes", "", "sft3.pdb", "no", "sft3.txt")]
        [ConditionalTheory(nameof(xsltcExeFound))]
        public void Var1(object param0, object param1, object param2, object param3, object param4, object param5, object param6)
        {
            String cmdLine = param0.ToString();
            String asmName = param1.ToString();
            bool asmCreated = String.Compare(param2.ToString(), "yes", true) == 0;
            String typeName = param3.ToString();
            String pdbName = param4.ToString();
            bool pdbCreated = String.Compare(param5.ToString(), "yes", true) == 0;
            String baselineFile = param6.ToString();
            if (ShouldSkip(new object[] { param0, param1, param2, param3, param4, param5, param6 }))
            {
                return; //TEST_SKIPPED;
            }

            VerifyTest(cmdLine, asmName, asmCreated, typeName, pdbName, pdbCreated, baselineFile, _createFromInputFile);
        }
    }
}