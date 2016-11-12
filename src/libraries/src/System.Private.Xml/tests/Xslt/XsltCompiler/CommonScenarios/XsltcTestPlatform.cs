// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;
using Xunit.Abstractions;
using System;
using System.Globalization;
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

        //[Variation("1", Desc = "Exercise basic use case, no option specified", Pri = 1, Params = new object[] { "pft1.xsl", "pft1", "pft1.dll", "pft1.txt" })]
        //[InlineData("pft1.xsl", "pft1", "pft1.dll", "pft1.txt")] //Skipping this, it tries to load System.dll
        //[Variation("3", Desc = "Exercise basic use case, a supported option val", Pri = 1, Params = new object[] { "/platform:x64 pft3.xsl", "", "pft3.dll", "pft3.txt" })]
        [InlineData("/platform:x64 pft3.xsl", "", "pft3.dll", "pft3.txt")]
        //[Variation("7", Desc = "Exercise keyword case sensitivity -1 ", Pri = 1, Params = new object[] { "/PLAtforM:X86 pft7.xsl", "", "pft7.dll", "pft7.txt" })]
        //[InlineData("/PLAtforM:X86 pft7.xsl", "", "pft7.dll", "pft7.txt")] //Skipping this, it tries to load System.dll
        //[Variation("8", Desc = "Exercise keyword case sensitivity -2", Pri = 1, Params = new object[] { "/PLAtforM:ItaNiuM pft8.xsl", "", "pft8.dll", "pft8.txt" })]
        [InlineData("/PLAtforM:ItaNiuM pft8.xsl", "", "pft8.dll", "pft8.txt")]
        //[Variation("9", Desc = "Exercise keyword case sensitivity -3", Pri = 1, Params = new object[] { "/PLAtforM:X64 pft9.xsl", "", "pft9.dll", "pft9.txt" })]
        //[InlineData("/PLAtforM:X64 pft9.xsl", "", "pft9.dll", "pft9.txt")] //Skipping this, it tries to load System.dll
        //[Variation("10", Desc = "Exercise keyword case sensitivity -4", Pri = 1, Params = new object[] { "/PLAtforM:AnyCpU pft10.xsl", "", "pft10.dll", "pft10.txt" })]
        [InlineData("/PLAtforM:AnyCpU pft10.xsl", "", "pft10.dll", "pft10.txt")]
        //[Variation("17", Desc = "Compile an assembly for anycpu and load", Pri = 1, Params = new object[] { "/platform:anycpu pft17.xsl", "pft17", "pft17.dll", "pft17.txt" })]
        //[InlineData("/platform:anycpu pft17.xsl", "pft17", "pft17.dll", "pft17.txt")] //Skipping this, it tries to load System.dll
        [ConditionalTheory(nameof(xsltcExeFound))]
        public void Var1(object param0, object param1, object param2, object param3)
        {
            String cmdLine = param0.ToString();
            String asmName = param2.ToString();
            bool asmCreated = true;
            String typeName = param1.ToString();
            String pdbName = Path.ChangeExtension(param2.ToString(), ".pdb");
            bool pdbCreated = false;
            String baselineFile = param3.ToString();

            VerifyTest(cmdLine, asmName, asmCreated, typeName, pdbName, pdbCreated, baselineFile, _createFromInputFile);
        }

        //[Variation("2", Desc = "Exercise basic use case, no option value", Pri = 1, Params = new object[] { "/platform: pft2.xsl", "pft2.txt" })]
        [InlineData("/platform: pft2.xsl", "pft2.txt")]
        //[Variation("4", Desc = "Exercise basic use case, an unsupported option value", Pri = 1, Params = new object[] { "/platform:foo pft4.xsl", "pft4.txt" })]
        [InlineData("/platform:foo pft4.xsl", "pft4.txt")]
        //[Variation("11", Desc = "Exercise basic use case, an unsupported option value -1", Pri = 1, Params = new object[] { "/platform pft11.xsl", "pft11.txt" })]
        //[InlineData("/platform pft11.xsl", "pft11.txt")] //Skipping this, it tries to load System.dll
        //[Variation("12", Desc = "Exercise basic use case, an unsupported option value -2", Pri = 1, Params = new object[] { "/platform: pft12.xsl", "pft12.txt" })]
        [InlineData("/platform: pft12.xsl", "pft12.txt")]
        //[Variation("13", Desc = "Exercise basic use case, an unsupported option value -3", Pri = 1, Params = new object[] { "/platform:* pft13.xsl", "pft13.txt" })]
        //[InlineData("/platform:* pft13.xsl", "pft13.txt")] //Skipping this, it tries to load System.dll
        //[Variation("14", Desc = "Exercise basic use case, an unsupported option value -4", Pri = 1, Params = new object[] { "/platform:x86x64 pft14.xsl", "pft14.txt" })]
        //[InlineData("/platform:x86x64 pft14.xsl", "pft14.txt")] //Skipping this, it tries to load System.dll
        //[Variation("15", Desc = "Exercise basic use case, an unsupported option value -5", Pri = 1, Params = new object[] { "/platform:x86,x64 pft15.xsl", "pft15.txt" })]
        //[InlineData("/platform:x86,x64 pft15.xsl", "pft15.txt")] //Skipping this, it tries to load System.dll
        //[Variation("16", Desc = "Exercise basic use case, an unsupported option value -6", Pri = 1, Params = new object[] { "/platform:x86;x64 pft16.xsl", "pft16.txt" })]
        [InlineData("/platform:x86;x64 pft16.xsl", "pft16.txt")]
        [ConditionalTheory(nameof(xsltcExeFound))]
        public void Var2(object param0, object param1)
        {
            String cmdLine = param0.ToString();
            String baselineFile = param1.ToString();

            VerifyTest(cmdLine, baselineFile, _createFromInputFile);
        }

        //[Variation("18", Desc = "Compile an assembly for machine's platform and load", Pri = 1, Params = new object[] { "pft18.xsl", "pft18.dll", "yes", "pft18", "pft18.pdb", "no", "pft18.txt", "yes" })]
        //[InlineData("pft18.xsl", "pft18.dll", "yes", "pft18", "pft18.pdb", "no", "pft18.txt", "yes")] //Skipping this, it tries to load System.dll
        //[Variation("19", Desc = "Compile an assembly for different platform and load", Pri = 1, Params = new object[] { "pft19.xsl", "pft19.dll", "yes", "", "pft19.pdb", "no", "pft19.txt", "no" })]
        [InlineData("pft19.xsl", "pft19.dll", "yes", "", "pft19.pdb", "no", "pft19.txt", "no")]
        //[Variation("20", Desc = "Exercise multiple /platform: flags", Pri = 1, Params = new object[] { "/platform:X64 pft20.xsl" /*+ " /platform:<current>*/, "pft20.dll", "yes", "pft20", "pft20.pdb", "no", "pft20.txt", "yes" })]
        //[InlineData("/platform:X64 pft20.xsl" /*+ " /platform:<current>*/, "pft20.dll", "yes", "pft20", "pft20.pdb", "no", "pft20.txt", "yes")] //Skipping this, it tries to load System.dll
        [ConditionalTheory(nameof(xsltcExeFound))]
        public void Var3(object param0, object param1, object param2, object param3, object param4, object param5, object param6, object param7)
        {
            String platform = "X86"; //CModInfo.Options["Arc"] as String;
            bool isSameMachine = String.Compare(param7.ToString(), "yes", true) == 0;

            string[] platforms = { "x86", "x64", "Itanium" };
            int index;

            if (platform == null)
            {
                Assert.True(false);
            }
            if (String.Compare("AMD64", platform, true) == 0)
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

            String cmdLine = param0 + " " + "/platform:" + platform;
            String asmName = param1.ToString();
            bool asmCreated = String.Compare(param2.ToString(), "yes", true) == 0;
            String typeName = param3.ToString();
            String pdbName = param4.ToString();
            bool pdbCreated = String.Compare(param5.ToString(), "yes", true) == 0;
            String baselineFile = param6.ToString();

            VerifyTest(cmdLine, asmName, asmCreated, typeName, pdbName, pdbCreated, baselineFile, _createFromInputFile);
        }
    }
}