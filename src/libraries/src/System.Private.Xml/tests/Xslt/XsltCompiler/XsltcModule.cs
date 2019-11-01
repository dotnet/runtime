// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using XmlCoreTest.Common;
using OLEDB.Test.ModuleCore;

namespace System.Xml.Tests
{
    //[TestModule(Name = "Xsltc", Desc = "Xsltc Compiler Tests", Pri = 1)]
    public class XsltcModule : CTestModule
    {
        public static string TargetDirectory = Path.Combine("TestFiles", FilePathUtil.GetTestDataPath(), "xsltc", "baseline");

        public void CopyDataFiles(string sourcePath, string targetDir)
        {
            if (!Directory.Exists(targetDir))
            {
                Directory.CreateDirectory(targetDir);
            }

            foreach (string file in Directory.GetFiles(sourcePath))
            {
                string fileName = Path.GetFileName(file);
                if (!File.Exists(Path.Combine(targetDir, fileName)))
                {
                    File.Copy(file, Path.Combine(targetDir, fileName), true);
                }
            }
        }

        public override int Init(object objParam)
        {
            int ret = base.Init(objParam);

            // copy all the data files to working folder
            CopyDataFiles(Path.Combine("TestFiles", FilePathUtil.GetTestDataPath(), "xsltc"), TargetDirectory);
            CopyDataFiles(Path.Combine("TestFiles", FilePathUtil.GetTestDataPath(), "xsltc", "precompiled"), TargetDirectory);
            CopyDataFiles(Path.Combine("TestFiles", FilePathUtil.GetTestDataPath(), "xsltc", "baseline"), TargetDirectory);

            return ret;
        }
    }
}