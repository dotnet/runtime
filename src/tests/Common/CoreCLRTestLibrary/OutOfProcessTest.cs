// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CoreclrTestLib;
using Xunit;

namespace TestLibrary
{
    public static class OutOfProcessTest
    {
        internal static bool runningInWindows;
        internal static string reportBase;
        internal static string testBinaryBase;
        internal static string helixUploadRoot;

        static OutOfProcessTest()
        {
            reportBase = Directory.GetCurrentDirectory();
            testBinaryBase = AppContext.BaseDirectory;
            helixUploadRoot = Environment.GetEnvironmentVariable("HELIX_WORKITEM_UPLOAD_ROOT");
            if (!String.IsNullOrEmpty(helixUploadRoot))
            {
                reportBase = Path.Combine(Path.GetFullPath(helixUploadRoot), "Reports");
            }

            if (String.IsNullOrEmpty(reportBase))
            {
                reportBase = Path.Combine(testBinaryBase, "Reports");
            }
            else
            {
                reportBase = Path.GetFullPath(reportBase);
            }
        }

        public static bool OutOfProcessTestsSupported =>
            !OperatingSystem.IsIOS()
            && !OperatingSystem.IsTvOS()
            && !OperatingSystem.IsAndroid()
            && !OperatingSystem.IsBrowser()
            && !OperatingSystem.IsOSPlatform("Wasi");

        public static void RunOutOfProcessTest(string assemblyPath)
        {
            int ret = -100;
            string baseDir = AppContext.BaseDirectory;
            string outputDir = System.IO.Path.GetFullPath(Path.Combine(reportBase, Path.GetDirectoryName(assemblyPath)));
            string outputFile = Path.Combine(outputDir, "output.txt");
            string errorFile = Path.Combine(outputDir, "error.txt");
            string testExecutable = null;
            Exception infraEx = null;

            try
            {
                CoreclrTestWrapperLib wrapper = new CoreclrTestWrapperLib();

                if (OperatingSystem.IsWindows())
                {
                    testExecutable = Path.Combine(baseDir, Path.ChangeExtension(assemblyPath, ".cmd"));
                }
                else
                {
                    testExecutable = Path.Combine(baseDir, Path.ChangeExtension(assemblyPath.Replace("\\", "/"), ".sh"));
                }

                if (!File.Exists(testExecutable))
                {
                    // Skip platform-specific test when running on the excluded platform
                    return;
                }

                System.IO.Directory.CreateDirectory(outputDir);

                ret = wrapper.RunTest(testExecutable, outputFile, errorFile, Assembly.GetEntryAssembly()!.FullName!, testBinaryBase, outputDir);
            }
            catch (Exception ex)
            {
                infraEx = ex;
            }

            if (infraEx != null)
            {
                Assert.True(false, "Test Infrastructure Failure: " + infraEx.ToString());
            }
            else
            {
                List<string> testOutput = new List<string>();

                try
                {
                    testOutput.AddRange(System.IO.File.ReadAllLines(errorFile));
                }
                catch (Exception ex)
                {
                    testOutput.Add("Unable to read error file: " + errorFile);
                    testOutput.Add(ex.ToString());
                }

                testOutput.Add(string.Empty);
                testOutput.Add("Return code:      " + ret);
                testOutput.Add("Raw output file:      " + outputFile);
                testOutput.Add("Raw output:");

                try
                {
                    testOutput.AddRange(System.IO.File.ReadAllLines(outputFile));
                }
                catch (Exception ex)
                {
                    testOutput.Add("Unable to read output file: " + outputFile);
                    testOutput.Add(ex.ToString());
                }

                testOutput.Add("To run the test:");
                testOutput.Add("Set up CORE_ROOT and run.");
                testOutput.Add("> " + testExecutable);

                var unicodeControlCharsRegex = new Regex("%5C%5Cp{C}+");

                // Remove all characters that have no visual or spatial representation.
                for (int i = 0; i < testOutput.Count; i++)
                {
                    string line = testOutput[i];
                    line = unicodeControlCharsRegex.Replace(line, string.Empty);
                    testOutput[i] = line;
                }

                foreach (string line in testOutput)
                {
                    Console.WriteLine(line);
                }

                Assert.True(ret == CoreclrTestWrapperLib.EXIT_SUCCESS_CODE, string.Join(Environment.NewLine, testOutput));
            }
        }
    }
}
