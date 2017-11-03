// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;
using Xunit.Abstractions;
using System;

namespace System.Xml.Tests
{
    //[TestCase(Name = "File Option Tests", Desc = "This Testcase maps to test variations described in '@file Functional Tests'")]
    public class XsltcTestFile : XsltcTestCaseBase
    {
        private ITestOutputHelper _output;
        public XsltcTestFile(ITestOutputHelper output) : base(output)
        {
            _output = output;
        }

        //[Variation("2", Desc = "Create a file that is Unicode encoded and send to xsltc.exe", Pri = 1, Params = new object[] { "@infft2.txt", "fft2.dll", "yes", "fft2", "fft2.pdb", "no", "fft2.txt" })]
        //[InlineData("@infft2.txt", "fft2.dll", "yes", "fft2", "fft2.pdb", "no", "fft2.txt")] //Skipping this, it tries to load System.dll
        //[Variation("3", Desc = "Create a file that is UTF-8 encoded and send to xsltc.exe", Pri = 1, Params = new object[] { "@infft3.txt", "fft3.dll", "yes", "fft3", "fft3.pdb", "no", "fft3.txt" })]
        //[InlineData("@infft3.txt", "fft3.dll", "yes", "fft3", "fft3.pdb", "no", "fft3.txt")] //Skipping this, it tries to load System.dll
        //[Variation("4", Desc = "Write specific @files with different quote sets to test quote processing", Pri = 1, Params = new object[] { "@infft4.txt", "fft4.dll", "yes", "fft4", "fft4.pdb", "yes", "fft4.txt" })]
        //[InlineData("@infft4.txt", "fft4.dll", "yes", "fft4", "fft4.pdb", "yes", "fft4.txt")] //Skipping this, it tries to load System.dll
        //[Variation("5", Desc = "Write specific @file with unsupported format, to see how Xsltc handles invalid format input files.", Pri = 1, Params = new object[] { "@infft5.txt", "fft5.dll", "no", "fft5", "fft5.pdb", "no", "fft5.txt" })]
        //[InlineData("@infft5.txt", "fft5.dll", "no", "fft5", "fft5.pdb", "no", "fft5.txt")] //Skipping this, it tries to load System.dll
        //[Variation("6", Desc = "Write specific @file with line thats almost the same as a specified command line, see which takes precedence", Pri = 1, Params = new object[] { "/out:fft.dll @infft6.txt", "fft6.dll", "yes", "fft6", "fft6.pdb", "no", "fft6.txt" })]
        //[InlineData("/out:fft.dll @infft6.txt", "fft6.dll", "yes", "fft6", "fft6.pdb", "no", "fft6.txt")] //Skipping this, it tries to load System.dll
        //[Variation("7", Desc = "Test multiple config files, command line, 2 config files", Pri = 1, Params = new object[] { "@infft7a.txt /out:fft7.dll @infft7b.txt", "fft7.dll", "yes", "fft7a", "fft7a.pdb", "no", "fft7.txt" })]
        //[InlineData("@infft7a.txt /out:fft7.dll @infft7b.txt", "fft7.dll", "yes", "fft7a", "fft7a.pdb", "no", "fft7.txt")] //Skipping this, it tries to load System.dll
        //[Variation("8", Desc = "Test multiple config files, command line, config, command line, config", Pri = 1, Params = new object[] { "@infft8a.txt /out:fft8.dll @infft8b.txt /class:fft8d fft8d.xsl /debug+ @infft8c.txt", "fft8.dll", "yes", "fft8888b", "fft8.pdb", "yes", "fft8.txt" })]
        //[InlineData("@infft8a.txt /out:fft8.dll @infft8b.txt /class:fft8d fft8d.xsl /debug+ @infft8c.txt", "fft8.dll", "yes", "fft8888b", "fft8.pdb", "yes", "fft8.txt")] //Skipping this, it tries to load System.dll
        //[Variation("9", Desc = "Test a config file that includes itself", Pri = 1, Params = new object[] { "@infft9.txt", "fft9.dll", "no", "fft9", "fft9.pdb", "no", "fft9.txt" })]
        //[InlineData("@infft9.txt", "fft9.dll", "no", "fft9", "fft9.pdb", "no", "fft9.txt")] //Skipping this, it tries to load System.dll
        //[Variation("10", Desc = "Test multiple config files with circular reference", Pri = 1, Params = new object[] { "@infft10.txt", "fft10.dll", "no", "fft10", "fft10.pdb", "no", "fft10.txt" })]
        //[InlineData("@infft10.txt", "fft10.dll", "no", "fft10", "fft10.pdb", "no", "fft10.txt")] //Skipping this, it tries to load System.dll
        //[Variation("11", Desc = "Test multiple config files with relative paths inside the files and command line", Pri = 1, Params = new object[] { @"@.\infft11.txt", "fft11.dll", "yes", "fft11", "fft11.pdb", "no", "fft11.txt" })]
        //[InlineData(@"@.\infft11.txt", "fft11.dll", "yes", "fft11", "fft11.pdb", "no", "fft11.txt")] //Skipping this, it tries to load System.dll
        //[Variation("12", Desc = "Test multiple config files with circular reference and relative paths", Pri = 1, Params = new object[] { "@infft12.txt", "fft12.dll", "no", "fft12", "fft12.pdb", "no", "fft12.txt" })]
        //[InlineData("@infft12.txt", "fft12.dll", "no", "fft12", "fft12.pdb", "no", "fft12.txt")] //Skipping this, it tries to load System.dll
        //[Variation("13", Desc = "Test multiple config files with fully qualified path inside the files and command line", Pri = 1, Params = new object[] { @"@$(CurrentWorkingDirectory)\infft13.txt", "fft13.dll", "yes", "fft13", "fft13.pdb", "no", "fft13.txt" })]
        //[InlineData(@"@$(CurrentWorkingDirectory)\infft13.txt", "fft13.dll", "yes", "fft13", "fft13.pdb", "no", "fft13.txt")] //Skipping this, it tries to load System.dll
        //[Variation("14", Desc = "Test multiple config files with circular reference and fully qualified path inside the files", Pri = 1, Params = new object[] { "@infft14.txt", "fft14.dll", "no", "fft14", "fft14.pdb", "no", "fft14.txt" })]
        [InlineData("@infft14.txt", "fft14.dll", "no", "fft14", "fft14.pdb", "no", "fft14.txt")]
        //[Variation("15", Desc = "Test multiple config files with circular reference and case sensitive file names specified", Pri = 1, Params = new object[] { "@infft15.txt", "fft15.dll", "no", "fft15", "fft15.pdb", "no", "fft15.txt" })]
        //[InlineData("@infft15.txt", "fft15.dll", "no", "fft15", "fft15.pdb", "no", "fft15.txt")] //Skipping this, it tries to load System.dll
        //[Variation("17", Desc = "Exercise comments(#) within file", Pri = 1, Params = new object[] { "@infft17.txt", "fft17.dll", "yes", "fft17", "fft17.pdb", "no", "fft17.txt" })]
        //[InlineData("@infft17.txt", "fft17.dll", "yes", "fft17", "fft17.pdb", "no", "fft17.txt")] //Skipping this, it tries to load System.dll
        //[Variation("18", Desc = "When loading from a file exercise commands and filenames with # in them", Pri = 1, Params = new object[] { "@infft18.txt", "fft18.dll", "yes", "AB#CD", "fft18.pdb", "no", "fft18.txt" })]
        //[InlineData("@infft18.txt", "fft18.dll", "yes", "AB#CD", "fft18.pdb", "no", "fft18.txt")] //Skipping this, it tries to load System.dll
        //[Variation("19", Desc = "Exercise wildcards with @", Pri = 1, Params = new object[] { "@*.txt", "fft19.dll", "no", "fft19", "fft19.pdb", "no", "fft19.txt", "EnglishOnly" })]
        [InlineData("@*.txt", "fft19.dll", "no", "fft19", "fft19.pdb", "no", "fft19.txt"/*, "EnglishOnly"*/)]
        //[Variation("20", Desc = "Exercise @ without filename", Pri = 1, Params = new object[] { "@", "fft20.dll", "no", "fft20", "fft20.pdb", "no", "fft20.txt" })]
        [InlineData("@", "fft20.dll", "no", "fft20", "fft20.pdb", "no", "fft20.txt")]
        //[Variation("21", Desc = "Exercise @ with not existing filename", Pri = 1, Params = new object[] { "@IDontExist", "fft21.dll", "no", "fft21", "fft21.pdb", "no", "fft21.txt" })]
        [InlineData("@IDontExist", "fft21.dll", "no", "fft21", "fft21.pdb", "no", "fft21.txt")]
        [Trait("category", "XsltcExeRequired")]
        [ConditionalTheory(nameof(xsltcExeFound))]
        public void Var1(object param0, object param1, object param2, object param3, object param4, object param5, object param6)
        {
            if (ShouldSkip(new object[] { param0, param1, param2, param3, param4, param5, param6 }))
            {
                return; //TEST_SKIPPED;
            }
            String cmdLine = ReplaceCurrentWorkingDirectory(param0.ToString());
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
