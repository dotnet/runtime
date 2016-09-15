// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using ModuleCore;
using System;
using System.IO;

namespace XsltCompilerTests
{
    internal class Program
    {
        public static int Main()
        {
            try
            {
                // Verify xsltc.exe is available
                XmlCoreTest.Common.XsltVerificationLibrary.SearchPath("xsltc.exe");
            }
            catch (FileNotFoundException)
            {
                Console.WriteLine("Could not find 'xsltc.exe'.  Make sure that the .NET SDK is installed");
                return 99;
            }

            string args = @"ExecutionMode:File Host:None SDKInstalled:NotInstalled";
            return 100 + TestRunner.Execute(args);
        }
    }
}