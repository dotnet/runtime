// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;
using Xunit.Abstractions;
using System;

namespace System.Xml.Tests
{
    // Note: Skipping these tests as they try to load a dll which references System.dll
    ////[TestCase(Name = "Out Option Tests", Desc = "This Testcase maps to test variations described in 'Out Functional Tests'")]
    //public class XsltcTestOut : XsltcTestCaseBase
    //{
    //    private ITestOutputHelper _output;
    //    public XsltcTestOut(ITestOutputHelper output) : base(output)
    //    {
    //        _output = output;
    //    }

    //    //[Variation("1", Desc = "Exercise basic use case, no option specified", Pri = 1, Params = new object[] { "oft1.xsl", "oft1.dll", "yes", "oft1", "oft1.txt" })]
    //    [InlineData("oft1.xsl", "oft1.dll", "yes", "oft1", "oft1.txt")]
    //    //[Variation("2", Desc = "Exercise basic use case, no option value", Pri = 1, Params = new object[] { "/out: oft2.xsl", "oft2.dll", "no", "oft2", "oft2.txt" })]
    //    [InlineData("/out: oft2.xsl", "oft2.dll", "no", "oft2", "oft2.txt")]
    //    //[Variation("3", Desc = "Exercise basic use case, assembly name different from stylesheet", Pri = 1, Params = new object[] { "/out:MyFoo.dll oft3.xsl", "MyFoo.dll", "yes", "oft3", "oft3.txt" })]
    //    [InlineData("/out:MyFoo.dll oft3.xsl", "MyFoo.dll", "yes", "oft3", "oft3.txt")]
    //    //[Variation("4", Desc = "Exercise basic use case, multiple stylesheets, no option specified", Pri = 1, Params = new object[] { "oft4a.xsl oft4b.xsl", "oft4a.dll", "yes", "oft4a", "oft4.txt" })]
    //    [InlineData("oft4a.xsl oft4b.xsl", "oft4a.dll", "yes", "oft4a", "oft4.txt")]
    //    //[Variation("5", Desc = "Exercise basic use case, multiple stylesheets, no option specified-2", Pri = 1, Params = new object[] { "oft5b.xsl oft5a.xsl", "oft5b.dll", "yes", "oft5a", "oft5.txt" })]
    //    [InlineData("oft5b.xsl oft5a.xsl", "oft5b.dll", "yes", "oft5a", "oft5.txt")]
    //    //[Variation("6", Desc = "Exercise basic use case, multiple stylesheets in one assembly", Pri = 1, Params = new object[] { "/out:oft6.dll oft6a.xsl oft6b.xsl", "oft6.dll", "yes", "oft6a", "oft6.txt" })]
    //    [InlineData("/out:oft6.dll oft6a.xsl oft6b.xsl", "oft6.dll", "yes", "oft6a", "oft6.txt")]
    //    //[Variation("7", Desc = "Exercise use case where assembly extension is specified, exe", Pri = 1, Params = new object[] { "/out:oft7.exe oft7.xsl", "oft7.exe", "yes", "oft7", "oft7.txt" })]
    //    [InlineData("/out:oft7.exe oft7.xsl", "oft7.exe", "yes", "oft7", "oft7.txt")]
    //    //[Variation("8", Desc = "Exercise use case where assembly extension is specified but different", Pri = 1, Params = new object[] { "/out:myfoo.doc oft8.xsl", "myfoo.doc", "yes", "oft8", "oft8.txt" })]
    //    [InlineData("/out:myfoo.doc oft8.xsl", "myfoo.doc", "yes", "oft8", "oft8.txt")]
    //    //[Variation("9", Desc = "Exercise use case, assembly extension is shorter than 3 chars", Pri = 1, Params = new object[] { "/out:oft9.xy oft9.xsl", "oft9.xy", "yes", "oft9", "oft9.txt" })]
    //    [InlineData("/out:oft9.xy oft9.xsl", "oft9.xy", "yes", "oft9", "oft9.txt")]
    //    //[Variation("10", Desc = "Exercise use case, assembly extension is longer than 3 chars", Pri = 1, Params = new object[] { "/out:oft10.xyzt oft10.xsl", "oft10.xyzt", "yes", "oft10", "oft10.txt" })]
    //    [InlineData("/out:oft10.xyzt oft10.xsl", "oft10.xyzt", "yes", "oft10", "oft10.txt")]
    //    //[Variation("11", Desc = "Exercise a very long assembly name", Pri = 1, Params = new object[] { "/out:oft1101234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789.dll oft11.xsl", "oft1101234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789.dll", "no", "oft11", "oft11.txt", "EnglishOnly" })]
    //    //[InlineData("/out:oft1101234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789.dll oft11.xsl", "oft1101234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789.dll", "no", "oft11", "oft11.txt"/*, "EnglishOnly"*/)]
    //    //[Variation("12", Desc = "Exercise a case where the output assembly name contains whitespace", Pri = 1, Params = new object[] { "/out:\"oft 12.dll\" oft12.xsl", "oft 12.dll", "yes", "oft12", "oft12.txt" })]
    //    [InlineData("/out:\"oft 12.dll\" oft12.xsl", "oft 12.dll", "yes", "oft12", "oft12.txt")]
    //    //[Variation("13", Desc = "Exercise keyword case sensitivity", Pri = 1, Params = new object[] { "/OUt:oft13.dll oft13.xsl", "oft13.dll", "yes", "oft13", "oft13.txt" })]
    //    [InlineData("/OUt:oft13.dll oft13.xsl", "oft13.dll", "yes", "oft13", "oft13.txt")]
    //    //[Variation("14", Desc = "Exercise fully qualified output path", Pri = 1, Params = new object[] { @"/out:$(CurrentWorkingDirectory)\oft14.dll oft14.xsl", "oft14.dll", "yes", "oft14", "oft14.txt" })]
    //    [InlineData(@"/out:$(CurrentWorkingDirectory)\oft14.dll oft14.xsl", "oft14.dll", "yes", "oft14", "oft14.txt")]
    //    //[Variation("15", Desc = "Exercise relative output path", Pri = 1, Params = new object[] { @"/out:.\oft15.dll oft15.xsl", "oft15.dll", "yes", "oft15", "oft15.txt" })]
    //    [InlineData(@"/out:.\oft15.dll oft15.xsl", "oft15.dll", "yes", "oft15", "oft15.txt")]
    //    //[Variation("16", Desc = "Exercise relative output path with forward slash", Pri = 1, Params = new object[] { @"/out:./oft16.dll oft16.xsl", "oft16.dll", "yes", "oft16", "oft16.txt" })]
    //    [InlineData(@"/out:./oft16.dll oft16.xsl", "oft16.dll", "yes", "oft16", "oft16.txt")]
    //    //[Variation("18", Desc = "Exercise output to device", Pri = 1, Params = new object[] { "/out:a:\\oft18.dll oft18.xsl", "oft18.dll", "no", "oft18", "oft18.txt", "EnglishOnly" })]
    //    [InlineData("/out:a:\\oft18.dll oft18.xsl", "oft18.dll", "no", "oft18", "oft18.txt"/*, "EnglishOnly"*/)]
    //    //[Variation("19", Desc = "Exercise output to null device", Pri = 1, Params = new object[] { "/out:nul.dll oft19.xsl", "nul.dll", "no", "oft19", "oft19.txt" })]
    //    [InlineData("/out:nul.dll oft19.xsl", "nul.dll", "no", "oft19", "oft19.txt")]
    //    //[Variation("20", Desc = "Exercise assembly name with invalid chars", Pri = 1, Params = new object[] { "/out:\"oft|20.dll\" oft20.xsl", "oft|20.dll", "no", "oft20", "oft20.txt", "EnglishOnly" })]
    //    [InlineData("/out:\"oft|20.dll\" oft20.xsl", "oft|20.dll", "no", "oft20", "oft20.txt"/*, "EnglishOnly"*/)]
    //    //[Variation("21", Desc = "Exercise reserved device name as assembly name", Pri = 1, Params = new object[] { "/out:\"COM1.dll\" oft21.xsl", "COM1.dll", "no", "oft21", "oft21.txt" })]
    //    [InlineData("/out:\"COM1.dll\" oft21.xsl", "COM1.dll", "no", "oft21", "oft21.txt")]
    //    //[Variation("23", Desc = "Exercise assembly named ..a", Pri = 1, Params = new object[] { "/out:..oft23 oft23.xsl", "..oft23", "yes", "oft23", "oft23.txt" })]
    //    [InlineData("/out:..oft23 oft23.xsl", "..oft23", "yes", "oft23", "oft23.txt")]
    //    //[Variation("24", Desc = "Exercise file path with . in it", Pri = 1, Params = new object[] { "/out:MyFoo.dll oft3.xsl", "MyFoo.dll", "yes", "oft3", "oft3.txt" })]
    //    //[Variation("25", Desc = "Exercise space containing assembly extension", Pri = 1, Params = new object[] { "/out:\"oft25.d l\" oft25.xsl", "oft25.d l", "yes", "oft25", "oft25.txt" })]
    //    [InlineData("/out:\"oft25.d l\" oft25.xsl", "oft25.d l", "yes", "oft25", "oft25.txt")]
    //    //[Variation("26", Desc = "Exercise space containing assembly path", Pri = 1, Params = new object[] { "/out:MyFoo.dll oft3.xsl", "MyFoo.dll", "yes", "oft3", "oft3.txt" })]
    //    //[Variation("27", Desc = "Exercise multiple /out: flags", Pri = 1, Params = new object[] { "/out:oft27A.dll /out:oft27B.dll oft27.xsl", "oft27B.dll", "yes", "oft27", "oft27.txt" })]
    //    [InlineData("/out:oft27A.dll /out:oft27B.dll oft27.xsl", "oft27B.dll", "yes", "oft27", "oft27.txt")]
    //    //[Variation("29", Desc = "Exercise device name containing path", Pri = 1, Params = new object[] { "/out:d:/nul/oft29.dll oft29.xsl", "oft29.dll", "no", "oft29", "oft29.txt", "EnglishOnly" })]
    //    [InlineData("/out:d:/nul/oft29.dll oft29.xsl", "oft29.dll", "no", "oft29", "oft29.txt"/*, "EnglishOnly"*/)]
    //    //[Variation("30", Desc = "No directory with intranet path", Pri = 1, Params = new object[] { @"/out:\\foo\bar.dll oft30.xsl", "oft30.dll", "no", "oft30", "oft30.txt" })]
    //    [InlineData(@"/out:\\foo\bar.dll oft30.xsl", "oft30.dll", "no", "oft30", "oft30.txt")]
    //    //[Variation("31", Desc = "Invalid intranet path", Pri = 1, Params = new object[] { @"/out:\\foo\foo\bar.dll oft31.xsl", "oft31.dll", "no", "oft31", "oft31.txt", "EnglishOnly" })]
    //    [InlineData(@"/out:\\foo\foo\bar.dll oft31.xsl", "oft31.dll", "no", "oft31", "oft31.txt"/*, "EnglishOnly"*/)]
    //    [Theory]
    //    public virtual void Var1(object param0, object param1, object param2, object param3, object param4)
    //    {
    //        if (ShouldSkip(new object[] { param0, param1, param2, param3, param4}))
    //        {
    //            return; //TEST_SKIPPED;
    //        }
    //        String cmdLine,
    //               asmName,
    //               typeName,
    //               baselineFile;
    //        bool asmCreated;

    //        VarParse(param0, param1, param2, param3, param4, out cmdLine, out asmName, out asmCreated, out typeName, out baselineFile);

    //        VerifyTest(cmdLine, asmName, asmCreated, typeName, baselineFile, _createFromInputFile);
    //    }

    //    //[Variation("28", Desc = "Exercise an assembly name composed of zero width chars and Cyrillic E", Pri = 1, Params = new object[] { "/out:\u0400\uFEFF\u200B.\u0400\uFEFF\u200B\u0400 oft28.xsl", "\u0400\uFEFF\u200B.\u0400\uFEFF\u200B\u0400", "yes", "oft28", "oft28.txt" })]
    //    [InlineData("/out:\u0400\uFEFF\u200B.\u0400\uFEFF\u200B\u0400 oft28.xsl", "\u0400\uFEFF\u200B.\u0400\uFEFF\u200B\u0400", "yes", "oft28", "oft28.txt")]
    //    [Theory]
    //    public virtual void Var2(object param0, object param1, object param2, object param3, object param4)
    //    {
    //        String cmdLine,
    //               asmName,
    //               typeName,
    //               baselineFile;
    //        bool asmCreated;

    //        VarParse(param0, param1, param2, param3, param4, out cmdLine, out asmName, out asmCreated, out typeName, out baselineFile);

    //        VerifyTest(cmdLine, asmName, asmCreated, typeName, String.Empty, false, baselineFile, false, _createFromInputFile);
    //    }

    //    public virtual void VarParse(object param0, object param1, object param2, object param3, object param4, out String cmdLine, out String asmName, out bool asmCreated, out String typeName, out String baselineFile)
    //    {
    //        cmdLine = ReplaceCurrentWorkingDirectory(param0.ToString());
    //        asmName = param1.ToString();
    //        asmCreated = String.Compare(param2.ToString(), "yes", true) == 0;
    //        typeName = param3.ToString();
    //        baselineFile = param4.ToString();
    //    }
    //}
}