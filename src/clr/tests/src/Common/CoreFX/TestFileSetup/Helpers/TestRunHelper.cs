// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace CoreFX.TestUtils.TestFileSetup.Helpers
{
    /// <summary>
    /// A class which runs all tests conforming to the current format of CoreFX tests - 
    /// Each folder:
    ///     is named for the test it contains - e.g. System.Collections.Tests
    ///     contains a test assembly named for the library it tests - e.g. System.Collections.Tests.dll
    ///     contains a test executable with the specified name - e.g. xunit.console.netcore.exe
    /// </summary>
    public class NetCoreTestRunHelper
    {
  
        public string DotnetExecutablePath { get; set; }

        public string logRootOutputPath { get; set; }

        /// <summary>
        /// Default Constructor
        /// </summary>
        /// <param name="DotnetExecutablePath"> Path to the dotnet executable, which is used to run the test executable In CoreFX tests this will be the built test host</param>
        /// <param name="logRootOutputPath">Path to which to output test run logs</param>
        public NetCoreTestRunHelper(string DotnetExecutablePath, string logRootOutputPath)
        {
            this.DotnetExecutablePath = DotnetExecutablePath;
            this.logRootOutputPath = logRootOutputPath;
        }

        /// <summary>
        /// Run a single test executabke
        /// </summary>
        /// <param name="workingDirectory">Directory from which to start the test executable</param>
        /// <param name="executableName">Name of the test executable</param>
        /// <param name="xunitTestTraits">Test trait exclusions to pass to the runner.</param>
        /// <param name="logOutputPath">Path to which to output the single test run's results</param>
        /// <returns>0 if single test run is succesful; 1 if not</returns>
        public int RunExecutable(string workingDirectory, string executableName, IReadOnlyList<string> xunitTestTraits, string logOutputPath)
        {
            // Calculate and create the path to the test log
            string logPath = Path.Combine(logOutputPath, Path.GetFileName(workingDirectory));
            if (!Directory.Exists(logPath))
                Directory.CreateDirectory(logPath);
            
            // Calculate the arguments to pass to the test runner
            string arguments = CalculateCommandLineArguments(workingDirectory, executableName, xunitTestTraits, Path.Combine(logPath,"testResults.xml"));

            // Create and initialize the test executable process
            ProcessStartInfo startInfo = new ProcessStartInfo(DotnetExecutablePath, arguments)
            {
                Arguments = arguments,
                WorkingDirectory = workingDirectory
            };


            Process executableProcess = new Process();
            executableProcess.StartInfo = startInfo;
            executableProcess.EnableRaisingEvents = true;
            executableProcess.Start();
            executableProcess.WaitForExit();

            return executableProcess.ExitCode;
        }
        /// <summary>
        /// Run all test executables conforming to the specified pattern in a directory
        /// </summary>
        /// <param name="rootDirectory">Directory containing tests to run</param>
        /// <param name="executableName">Name of the test executable contained in folders</param>
        /// <param name="xunitTestTraits">Test trait exclusions to pass to the runner.</param>
        /// <param name="processLimit">Maximum number of tests to run in parallel</param>
        /// <param name="logRootOutputPath">Root path to which to output the all test runs' results</param>
        /// <returns>0 if entire test run is succesful; 1 if not</returns>
        public int RunAllExecutablesInDirectory(string rootDirectory, string executableName, IReadOnlyList<string> xunitTestTraits, int processLimit, string logRootOutputPath = null)
        {
            int result = 0;
            // Do a Depth-First Search to find and run executables with the same name 
            Stack<string> directories = new Stack<string>();
            List<string> testDirectories = new List<string>();
            // Push rootdir
            directories.Push(rootDirectory);

            while (directories.Count > 0)
            {
                string currentDirectory = directories.Pop();
                
                // If a directory contains an executable with the specified name - add it  
                if (File.Exists(Path.Combine(currentDirectory, executableName)))
                    testDirectories.Add(currentDirectory);

                foreach (string subDir in Directory.GetDirectories(currentDirectory))
                    directories.Push(subDir);
            }

            // Initialize max degree of parallelism
            ParallelOptions parallelOptions = new ParallelOptions();
            parallelOptions.MaxDegreeOfParallelism = processLimit;

            Parallel.ForEach(testDirectories, parallelOptions,
                (testDirectory) =>
                {
                    if (RunExecutable(testDirectory, executableName, xunitTestTraits, logRootOutputPath) != 0)
                    {
                        // If any tests fail mark the whole run as failed
                        Console.WriteLine("Test Run Failed " + testDirectory);
                        result = 1;
                    }
                }
                );
            return result;
        }

        /// <summary>
        /// Calculate the commandline arguments to pass to the test executable
        /// </summary>
        /// <param name="testDirectory">Current test directory name - assumed to have the same name as the test</param>
        /// <param name="executableName">>Name of the test executable contained in the folder</param>
        /// <param name="xunitTestTraits">Test trait exclusions to pass to the runner.</param>
        /// <param name="logPath">Path to which to output the single test run's results</param>
        /// <returns>A string representing command line arguments to be passed to the console test runner</returns>
        private string CalculateCommandLineArguments(string testDirectory, string executableName, IReadOnlyList<string> xunitTestTraits, string logPath)
        {
            StringBuilder arguments = new StringBuilder();

            // Append test executable name
            arguments.Append($"\"{Path.Combine(testDirectory, Path.GetFileName(executableName))}\" ");

            // Append test name dll\
            arguments.Append($"\"{Path.Combine(testDirectory, Path.GetFileName(testDirectory))}.dll\" ");

            // Append RSP file
            arguments.Append($"@\"{Path.Combine(testDirectory, Path.GetFileName(testDirectory))}.rsp\" ");

            if (!String.IsNullOrEmpty(logPath))
            {
                // Add logging information
                arguments.Append($"-xml {logPath} ");
            }

            // Append all additional arguments
            foreach (string traitToExclude in xunitTestTraits)
            {
                arguments.Append($"-notrait {traitToExclude} ");
            }

            return arguments.ToString();
        }
    }
}
