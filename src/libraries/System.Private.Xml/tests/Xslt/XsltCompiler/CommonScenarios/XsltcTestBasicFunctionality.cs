// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;
using Xunit.Abstractions;
using System;
using System.IO;

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

        //[Variation("1", Desc = "Exercise the basic use case, no options", Pri = 1, Params = new object[] { "bft1.xsl", "bft1", "bft1.txt" })]
        //[InlineData("bft1.xsl", "bft1", "bft1.txt")] //Skipping this, it tries to load System.dll
        //[Variation("2", Desc = "Exercise the basic use case, no options, fully qualified path", Pri = 1, Params = new object[] { @"$(CurrentWorkingDirectory)\bft2.xsl", "bft2", "bft2.txt" })]
        //[InlineData(@"$(CurrentWorkingDirectory)\bft2.xsl", "bft2", "bft2.txt")] //Skipping this, it tries to load System.dll
        //[Variation("10", Desc = "Exercise the basic use case, 2 stylesheets, 2 classes", Pri = 1, Params = new object[] { "/class:bft10a bft10a.xsl /class:bft10b bft10b.xsl", "bft10a", "bft10.txt" })]
        //[InlineData("/class:bft10a bft10a.xsl /class:bft10b bft10b.xsl", "bft10a", "bft10.txt")] //Skipping this, it tries to load System.dll
        //[Variation("11", Desc = "Exercise the basic use case, 3 stylesheets, 2 classes", Pri = 2, Params = new object[] { "/class:bft11a bft11a.xsl /class:bft11b bft11b.xsl bft11c.xsl", "bft11a", "bft11.txt" })]
        //[InlineData("/class:bft11a bft11a.xsl /class:bft11b bft11b.xsl bft11c.xsl", "bft11a", "bft11.txt")] //Skipping this, it tries to load System.dll
        //[Variation("12", Desc = "Exercise the basic use case, many stylesheets 1 class", Pri = 2, Params = new object[] { "/class:bft12az /out:bft12az.dll bft12a.xsl bft12b.xsl bft12c.xsl bft12d.xsl bft12e.xsl bft12f.xsl bft12g.xsl bft12h.xsl bft12i.xsl bft12j.xsl bft12k.xsl bft12l.xsl bft12m.xsl bft12n.xsl bft12o.xsl bft12p.xsl bft12q.xsl bft12r.xsl bft12s.xsl bft12t.xsl bft12u.xsl bft12v.xsl bft12w.xsl bft12x.xsl bft12y.xsl bft12z.xsl", "bft12az", "bft12.txt" })]
        //[InlineData("/class:bft12az /out:bft12az.dll bft12a.xsl bft12b.xsl bft12c.xsl bft12d.xsl bft12e.xsl bft12f.xsl bft12g.xsl bft12h.xsl bft12i.xsl bft12j.xsl bft12k.xsl bft12l.xsl bft12m.xsl bft12n.xsl bft12o.xsl bft12p.xsl bft12q.xsl bft12r.xsl bft12s.xsl bft12t.xsl bft12u.xsl bft12v.xsl bft12w.xsl bft12x.xsl bft12y.xsl bft12z.xsl", "bft12az", "bft12.txt")] //Skipping this, it tries to load System.dll
        //[Variation("13", Desc = "Exercise the basic use case, same stylesheet twice", Pri = 2, Params = new object[] { "bft13.xsl bft13.xsl", "bft13", "bft13.txt" })]
        //[InlineData("bft13.xsl bft13.xsl", "bft13", "bft13.txt")] //Skipping this, it tries to load System.dll
        //[Variation("14", Desc = "Exercise the basic use case, same stylesheet twice with 2 different classnames", Pri = 2, Params = new object[] { "/class:bft14a bft14a.xsl /class:bft14b bft14a.xsl", "bft14a", "bft14.txt" })]
        //[InlineData("/class:bft14a bft14a.xsl /class:bft14b bft14a.xsl", "bft14a", "bft14.txt")] //Skipping this, it tries to load System.dll
        //[Variation("17", Desc = "Exercise a very long stylesheetname with default classname and default assembly name", Pri = 2, Params = new object[] { "bft17012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789.xsl", "bft17012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789", "bft17.txt" })]
        //[InlineData("bft17012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789.xsl", "bft17012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789", "bft17.txt")]
        //[Variation("19", Desc = "Exercise relative URIs for imported documents", Pri = 1, Params = new object[] { @"bft19.xsl", "bft19", "bft19.txt" })]
        //[InlineData(@"bft19.xsl", "bft19", "bft19.txt")] //Skipping this, it tries to load System.dll
        //[Variation("20", Desc = "Exercise relative URIs for included documents", Pri = 1, Params = new object[] { @"bft20.xsl", "bft20", "bft20.txt" })]
        //[InlineData(@"bft20.xsl", "bft20", "bft20.txt")] //Skipping this, it tries to load System.dll
        //[Variation("21", Desc = "Exercise relative URIs for document function", Pri = 1, Params = new object[] { @"/settings:document+ bft21.xsl", "bft21", "bft21.txt" })]
        //[InlineData(@"/settings:document+ bft21.xsl", "bft21", "bft21.txt")] //Skipping this, it tries to load System.dll
        //[Variation("22", Desc = "Exercise the basic use case, no options, relative path", Pri = 1, Params = new object[] { @".\bft22.xsl", "bft22", "bft22.txt" })]
        //[InlineData(@".\bft22.xsl", "bft22", "bft22.txt")] //Skipping this, it tries to load System.dll
        //[Variation("23", Desc = "Exercise the basic use case, no options, relative path with fwd slashes", Pri = 1, Params = new object[] { @"./bft23.xsl", "bft23", "bft23.txt" })]
        //[InlineData(@"./bft23.xsl", "bft23", "bft23.txt")] //Skipping this, it tries to load System.dll
        //[Variation("25", Desc = "Exercise a stylesheet with space in its name", Pri = 1, Params = new object[] { "\"bft 25.xsl\"", "bft 25", "bft25.txt" })]
        //[InlineData("\"bft 25.xsl\"", "bft 25", "bft25.txt")] //Skipping this, it tries to load System.dll
        //[Variation("26", Desc = "Exercise Cyrillic e in filename", Pri = 1, Params = new object[] { "bft26.xsl", "bft26", "bft26.txt" })]
        //[InlineData("bft26.xsl", "bft26", "bft26.txt")] /*sd problems \u0400\u0400\u0400\u0400bft26.xsl, coverage addded to other test cases*/ //Skipping this, it tries to load System.dll
        //[Variation("31", Desc = "Exercise options with “-“", Pri = 1, Params = new object[] { "-out:bft31.dll /class:bft31 -debug- bft31.xsl", "bft31", "bft31.txt" })]
        //[InlineData("-out:bft31.dll /class:bft31 -debug- bft31.xsl", "bft31", "bft31.txt")] //Skipping this, it tries to load System.dll
        [ConditionalTheory(nameof(xsltcExeFound))]
        public void Var1(object param0, object param1, object param2)
        {
            String cmdLine = ReplaceCurrentWorkingDirectory(param0.ToString());
            String asmName = param1 + ".dll";
            String typeName = param1.ToString();
            String pdbName = param1 + ".pdb";
            String baselineFile = param2.ToString();

            VerifyTest(cmdLine, asmName, true, typeName, pdbName, false, baselineFile, _createFromInputFile);
        }

        // All variations that are Invalid are run by Var2()
        //[Variation("6", Desc = "Exercise the basic use case, invalid input", Pri = 1, Params = new object[] { "/thisisinvalid foo", "bft6.txt" })]
        //[InlineData("/thisisinvalid foo", "bft6.txt")] //Skipping this, it tries to load System.dll
        //[Variation("7", Desc = "Exercise the basic use case, invalid input", Pri = 1, Params = new object[] { "mystylesheet", "bft7.txt" })]
        //[InlineData("mystylesheet", "bft7.txt")] //Skipping this, it tries to load System.dll
        //[Variation("8", Desc = "Exercise the basic use case, invalid input", Pri = 1, Params = new object[] { "mystylesheet,xsl", "bft8.txt" })]
        //[InlineData("mystylesheet,xsl", "bft8.txt")] //Skipping this, it tries to load System.dll
        //[Variation("9", Desc = "Exercise the basic use case, wildcarded stylesheet group", Pri = 1, Params = new object[] { "*.xsl", "bft9.txt", "EnglishOnly" })]
        //[InlineData("*.xsl", "bft9.txt"/*, "EnglishOnly"*/)] //Skipping this, it tries to load System.dll
        //[Variation("15", Desc = "Exercise what would be a circular reference, a primary stylesheet that includes another, followed by the include itself", Pri = 2, Params = new object[] { "bft15a.xsl bft15b.xsl", "bft15.txt", "EnglishOnly" })]
        [InlineData("bft15a.xsl bft15b.xsl", "bft15.txt"/*, "EnglishOnly"*/)]
        //[Variation("16", Desc = "Exercise the basic use case, 2 stylesheets with different extensions", Pri = 2, Params = new object[] { "bft16.xsl bft16.xslt", "bft16.txt" })]
        //[InlineData("bft16.xsl bft16.xslt", "bft16.txt")] //Skipping this, it tries to load System.dll
        //[Variation("18", Desc = "Exercise what would be a circular reference, a primary stylesheet that imports another, followed by the import itself", Pri = 1, Params = new object[] { "bft18a.xsl bft18b.xsl", "bft18.txt", "EnglishOnly" })]
        //[InlineData("bft18a.xsl bft18b.xsl", "bft18.txt"/*, "EnglishOnly"*/)] //Skipping this, it tries to load System.dll
        //[Variation("24", Desc = "Exercise what would be a circular reference, a primary stylesheet that imports another, followed by the include itself", Pri = 1, Params = new object[] { "bft24a.xsl bft24b.xsl", "bft24.txt", "EnglishOnly" })]
        //[InlineData("bft24a.xsl bft24b.xsl", "bft24.txt"/*, "EnglishOnly"*/)] //Skipping this, it tries to load System.dll
        //[Variation("27", Desc = "Exercise whitespace before flag values", Pri = 1, Params = new object[] { "/ out:bft27.dll bft27.xsl", "bft27.txt" })]
        [InlineData("/ out:bft27.dll bft27.xsl", "bft27.txt")]
        //[Variation("28", Desc = "Exercise whitespace after flag values", Pri = 1, Params = new object[] { "/out:  bft28.dll bft28.xsl", "bft28.txt" })]
        //[InlineData("/out:  bft28.dll bft28.xsl", "bft28.txt")] //Skipping this, it tries to load System.dll
        //[Variation("29", Desc = "Exercise help", Pri = 1, Params = new object[] { "/?", "help.txt" })]
        //[InlineData("/?", "help.txt")] //Skipping this, it tries to load System.dll
        //[Variation("30", Desc = "Exercise help with option values", Pri = 1, Params = new object[] { "bft2.xsl /? bft2.xsl", "help.txt" })]
        [InlineData("bft2.xsl /? bft2.xsl", "help.txt")]
        //[Variation("33", Desc = "Exercise nologo", Pri = 1, Params = new object[] { "/nologo /foo", "bft33.txt" })]
        //[InlineData("/nologo /foo", "bft33.txt")] //Skipping this, it tries to load System.dll
        //[Variation("34", Desc = "Stylesheet path containing device name", Pri = 1, Params = new object[] { "nul/bft34.xsl", "bft34.txt" })]
        //[InlineData("nul/bft34.xsl", "bft34.txt")] //Skipping this, it tries to load System.dll
        //[Variation("35", Desc = "Device name as stylesheetname", Pri = 1, Params = new object[] { "nul.xsl", "bft35.txt" })]
        [InlineData("nul.xsl", "bft35.txt")]
        //[Variation("36", Desc = "Stylesheet path without name", Pri = 1, Params = new object[] { "..\\", "bft36.txt" })]
        //[InlineData("..\\", "bft36.txt")] //Skipping this, it tries to load System.dll
        //[Variation("37", Desc = "No input source", Pri = 1, Params = new object[] { "/debug+", "bft37.txt" })]
        [InlineData("/debug+", "bft37.txt")]
        //[Variation("38", Desc = "Empty string in arguments", Pri = 1, Params = new object[] { "\"\"", "bft38.txt" })]
        [InlineData("\"\"", "bft38.txt")]
        [ConditionalTheory(nameof(xsltcExeFound))]
        public void Var2(object param0, object param1)
        {
            if (ShouldSkip(new object[] { param0, param1 }))
            {
                return;// TEST_SKIPPED;
            }
            String cmdLine = param0.ToString();
            String baselineFile = param1.ToString();

            VerifyTest(cmdLine, baselineFile, _createFromInputFile);
        }

        //[Variation("5", Desc = "Exercise the basic use case, no input", Pri = 1, Params = new object[] { " ", "bft5.txt" })]
        //[InlineData(" ", "bft5.txt")] //Skipping this, it tries to load System.dll
        //[Theory]
        public void Var3(object param0, object param1)
        {
            String cmdLine = param0.ToString();
            String baselineFile = _createFromInputFile
                                      ? param1.ToString()
                                      : "help.txt";

            VerifyTest(cmdLine, baselineFile, _createFromInputFile);
        }
    }
}