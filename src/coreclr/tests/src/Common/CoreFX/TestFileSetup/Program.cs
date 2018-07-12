// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.CommandLine;
using System.IO;
using System.Net.Http;
using System.Text;
using CoreFX.TestUtils.TestFileSetup.Helpers;
using Newtonsoft.Json;
using Newtonsoft.Json.Schema;

namespace CoreFX.TestUtils.TestFileSetup
{
    /// <summary>
    /// This is a driver class, which downloads archived CoreFX test assemblies from a specified URL, lays out their contents
    /// and subsequently runs them in parallel. 
    /// This is invoked from .tests/runtests.[cmd|sh] and depends on a test host (CoreCLR components with a dotnet executable) being present
    /// </summary>
    public class Program
    {
        // Helper class to lay out files on disk
        private static TestFileHelper testFileHelper;
        // Helper class to run tests in parallel
        private static NetCoreTestRunHelper testRunHelper;

        // Test Set-up Options
        private static string outputDir;
        private static string testUrl;
        private static string testListPath;
        private static bool cleanTestBuild;

        // Test Run Options
        private static string dotnetPath;
        private static bool runSpecifiedTests;
        private static bool runAllTests;
        private static int maximumDegreeOfParalellization;
        private static string logRootOutputPath;

        private static ExitCode exitCode;
        private static string executableName;
        private static IReadOnlyList<string> traitExclusions = Array.Empty<string>();

        public static void Main(string[] args)
        {
            // Initialize default options
            exitCode = ExitCode.Success;
            maximumDegreeOfParalellization = Environment.ProcessorCount;
            runSpecifiedTests = false;
            runAllTests = false;
            cleanTestBuild = false;

            ArgumentSyntax argSyntax = ParseCommandLine(args);
            try
            {
                // Download and lay out files on disk
                SetupTests(runAllTests);
                
                // Only run tests if the relevant commandline switch is passed
                if (runSpecifiedTests || runAllTests)
                    exitCode = RunTests();
            }
            catch (AggregateException e)
            {
                // Handle failure cases and exit gracefully
                e.Handle(innerExc =>
                {

                    if (innerExc is HttpRequestException)
                    {
                        exitCode = ExitCode.HttpError;
                        Console.WriteLine("Error downloading tests from: " + testUrl);
                        Console.WriteLine(innerExc.Message);
                        return true;
                    }
                    else if (innerExc is IOException)
                    {
                        exitCode = ExitCode.IOError;
                        Console.WriteLine(innerExc.Message);
                        return true;
                    }
                    else if (innerExc is JSchemaValidationException || innerExc is JsonSerializationException)
                    {
                        exitCode = ExitCode.JsonSchemaValidationError;
                        Console.WriteLine("Error validating test list: ");
                        Console.WriteLine(innerExc.Message);
                        return true;
                    }
                    else
                    {
                        exitCode = ExitCode.UnknownError;
                    }
                    
                    return false;
                });
            }

            Environment.Exit((int)exitCode);
        }

        /// <summary>
        /// Parse passed Command Line arguments
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        private static ArgumentSyntax ParseCommandLine(string[] args)
        {
            ArgumentSyntax argSyntax = ArgumentSyntax.Parse(args, syntax =>
            {
                syntax.DefineOption("out|outDir|outputDirectory", ref outputDir, "Directory where tests are downloaded");
                syntax.DefineOption("testUrl", ref testUrl, "URL, pointing to the list of tests");
                syntax.DefineOption("testListJsonPath", ref testListPath, "JSON-formatted list of test assembly names to download");
                syntax.DefineOption("clean|cleanOutputDir", ref cleanTestBuild, "Clean test assembly output directory");
                syntax.DefineOption("runSpecified|runSpecifiedTests", ref runSpecifiedTests, "Run specified Tests after setup");
                syntax.DefineOption("runAll|runAllTests", ref runAllTests, "Run All available Tests in the specified TestList");
                syntax.DefineOption("dotnet|dotnetPath", ref dotnetPath, "Path to dotnet executable used to run tests.");
                syntax.DefineOption("executable|executableName", ref executableName, "Name of the test executable to start");
                syntax.DefineOption("log|logPath|logRootOutputPath", ref logRootOutputPath, "Run Tests after setup");
                syntax.DefineOption("maxProcessCount|numberOfParallelTests|maximumDegreeOfParalellization", ref maximumDegreeOfParalellization, "Maximum number of concurrently executing processes");
                syntax.DefineOptionList("notrait", ref traitExclusions, "Traits to be excluded from test runs");

            });

            if (runSpecifiedTests || runAllTests)
            {
                if (String.IsNullOrEmpty(dotnetPath))
                    throw new ArgumentException("Please supply a test host location to run tests.");

                if (!File.Exists(dotnetPath))
                    throw new ArgumentException("Invalid testhost path. Please supply a test host location to run tests.");
            }

            return argSyntax;
        }

        /// <summary>
        /// Method, which calls into the Test File Setup helper class to download and layout test assemblies on disk
        /// </summary>
        /// <param name="runAll">Specifies whether all tests available in the test list should be run</param>
        private static void SetupTests(bool runAll = false)
        {
            testFileHelper = new TestFileHelper();

            if (!Directory.Exists(outputDir))
                Directory.CreateDirectory(outputDir);

            // If the --clean switch has been specified delete existing assemblies
            if (cleanTestBuild)
            {
                testFileHelper.CleanBuild(outputDir);
            }

            // Map test names to their definitions
            Dictionary<string, XUnitTestAssembly> testAssemblyDefinitions = testFileHelper.DeserializeTestJson(testListPath);

            testFileHelper.SetupTests(testUrl, outputDir, testAssemblyDefinitions, runAll).Wait();
        }

        /// <summary>
        /// Runs all tests in a directory
        /// Only tests, the executable driver of which has the same name as the passed argument are executed - e.g. xunit.console.netcore.exe
        /// </summary>
        /// <returns></returns>
        private static ExitCode RunTests()
        {
            testRunHelper = new NetCoreTestRunHelper(dotnetPath, logRootOutputPath);
            int result = testRunHelper.RunAllExecutablesInDirectory(outputDir, executableName, traitExclusions, maximumDegreeOfParalellization, logRootOutputPath);

            return result == 0 ? ExitCode.Success : ExitCode.TestFailure;
        }
    }
}
