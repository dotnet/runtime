// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;
using Xunit.Abstractions;
using System.IO;

namespace System.Xml.Tests
{
    //[TestCase(Name = "Platform Option Tests", Desc = "This Testcase maps to test variations described in 'Platform Functional Tests'")]
    public class XsltcTestPlatform : XsltcTestCaseBase
    {
        private ITestOutputHelper _output;
        public XsltcTestPlatform(ITestOutputHelper output) : base(output)
        {
            _output = output;
        }

        [ActiveIssue(30017)]
        //[Variation("3", Desc = "Exercise basic use case, a supported option val", Pri = 1, Params = new object[] { "/platform:x64 pft3.xsl", "", "pft3.dll", "pft3.txt" })]
        [InlineData("/platform:x64 pft3.xsl", "", "pft3.dll", "pft3.txt")]
        //[Variation("8", Desc = "Exercise keyword case sensitivity -2", Pri = 1, Params = new object[] { "/PLAtforM:ItaNiuM pft8.xsl", "", "pft8.dll", "pft8.txt" })]
        [InlineData("/PLAtforM:ItaNiuM pft8.xsl", "", "pft8.dll", "pft8.txt")]
        //[Variation("10", Desc = "Exercise keyword case sensitivity -4", Pri = 1, Params = new object[] { "/PLAtforM:AnyCpU pft10.xsl", "", "pft10.dll", "pft10.txt" })]
        [InlineData("/PLAtforM:AnyCpU pft10.xsl", "", "pft10.dll", "pft10.txt")]
        [Trait("category", "XsltcExeRequired")]
        [ConditionalTheory(nameof(xsltcExeFound))]
        public void Var1(object param0, object param1, object param2, object param3)
        {
            string cmdLine = param0.ToString();
            string asmName = param2.ToString();
            bool asmCreated = true;
            string typeName = param1.ToString();
            string pdbName = Path.ChangeExtension(param2.ToString(), ".pdb");
            bool pdbCreated = false;
            string baselineFile = param3.ToString();

            VerifyTest(cmdLine, asmName, asmCreated, typeName, pdbName, pdbCreated, baselineFile, _createFromInputFile);
        }

        [ActiveIssue(30017)]
        //[Variation("2", Desc = "Exercise basic use case, no option value", Pri = 1, Params = new object[] { "/platform: pft2.xsl", "pft2.txt" })]
        [InlineData("/platform: pft2.xsl", "pft2.txt")]
        //[Variation("4", Desc = "Exercise basic use case, an unsupported option value", Pri = 1, Params = new object[] { "/platform:foo pft4.xsl", "pft4.txt" })]
        [InlineData("/platform:foo pft4.xsl", "pft4.txt")]
        //[Variation("12", Desc = "Exercise basic use case, an unsupported option value -2", Pri = 1, Params = new object[] { "/platform: pft12.xsl", "pft12.txt" })]
        [InlineData("/platform: pft12.xsl", "pft12.txt")]
        //[Variation("16", Desc = "Exercise basic use case, an unsupported option value -6", Pri = 1, Params = new object[] { "/platform:x86;x64 pft16.xsl", "pft16.txt" })]
        [InlineData("/platform:x86;x64 pft16.xsl", "pft16.txt")]
        [Trait("category", "XsltcExeRequired")]
        [ConditionalTheory(nameof(xsltcExeFound))]
        public void Var2(object param0, object param1)
        {
            string cmdLine = param0.ToString();
            string baselineFile = param1.ToString();

            VerifyTest(cmdLine, baselineFile, _createFromInputFile);
        }

        //[Variation("19", Desc = "Compile an assembly for different platform and load", Pri = 1, Params = new object[] { "pft19.xsl", "pft19.dll", "yes", "", "pft19.pdb", "no", "pft19.txt", "no" })]
        [InlineData("pft19.xsl", "pft19.dll", "yes", "", "pft19.pdb", "no", "pft19.txt", "no")]
        [Trait("category", "XsltcExeRequired")]
        [ConditionalTheory(nameof(xsltcExeFound))]
        public void Var3(object param0, object param1, object param2, object param3, object param4, object param5, object param6, object param7)
        {
            string platform = "X86"; //CModInfo.Options["Arc"] as String;
            bool isSameMachine = string.Compare(param7.ToString(), "yes", true) == 0;

            string[] platforms = { "x86", "x64", "Itanium" };
            int index;

            if (platform == null)
            {
                Assert.True(false);
            }
            if (string.Compare("AMD64", platform, true) == 0)
            {
                index = 1;
            }
            else
            {
                index = 0;
            }

            platform = platforms[(index + (isSameMachine
                                               ? 0
                                               : 1)) % platforms.Length];

            string cmdLine = param0 + " " + "/platform:" + platform;
            string asmName = param1.ToString();
            bool asmCreated = string.Compare(param2.ToString(), "yes", true) == 0;
            string typeName = param3.ToString();
            string pdbName = param4.ToString();
            bool pdbCreated = string.Compare(param5.ToString(), "yes", true) == 0;
            string baselineFile = param6.ToString();

            VerifyTest(cmdLine, asmName, asmCreated, typeName, pdbName, pdbCreated, baselineFile, _createFromInputFile);
        }
    }
}