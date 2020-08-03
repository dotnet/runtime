// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Profiler.Tests
{
    [Flags]
    public enum ProfileeOptions
    {
        None = 0,
        OptimizationSensitive,
        NoStartupAttach
    }

    public class ProfilerTestRunner
    {
        public static int Run(string profileePath,
                              string testName,
                              Guid profilerClsid,
                              string profileeArguments = "",
                              ProfileeOptions profileeOptions = ProfileeOptions.None)
        {
            string arguments;
            string program;
            Dictionary<string, string> envVars = new Dictionary<string, string>();
            string profileeAppDir = Path.GetDirectoryName(profileePath);

            arguments = profileePath + " RunTest " + profileeArguments;
            program = GetCorerunPath();
            string profilerPath = GetProfilerPath();
            if (!profileeOptions.HasFlag(ProfileeOptions.NoStartupAttach))
            {
                envVars.Add("CORECLR_ENABLE_PROFILING", "1");
                envVars.Add("CORECLR_PROFILER_PATH", profilerPath);
                envVars.Add("CORECLR_PROFILER", "{" + profilerClsid + "}");
            }

            if (profileeOptions.HasFlag(ProfileeOptions.OptimizationSensitive))
            {
                Console.WriteLine("Disabling tiered compilation, jitstress, and minopts.");
                envVars.Add("COMPlus_TieredCompilation", "0");
                envVars.Add("COMPlus_JitStress", "0");
                envVars.Add("COMPlus_JITMinOpts", "0");
            }

            envVars.Add("Profiler_Test_Name", testName);

            if(!File.Exists(profilerPath))
            {
                LogTestFailure("Profiler library not found at expected path: " + profilerPath);
            }

            ProfileeOutputVerifier verifier = new ProfileeOutputVerifier();

            Process process = new Process();
            process.StartInfo.FileName = program;
            process.StartInfo.Arguments = arguments;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;

            foreach (string key in Environment.GetEnvironmentVariables().Keys)
            {
                process.StartInfo.EnvironmentVariables[key] = Environment.GetEnvironmentVariable(key);
            }

            foreach (string key in envVars.Keys)
            {
                process.StartInfo.EnvironmentVariables[key] = envVars[key];
            }

            process.OutputDataReceived += (sender, args) =>
            {
                Console.WriteLine(args.Data);
                verifier.WriteLine(args.Data);
            };
            process.Start();
            process.BeginOutputReadLine();

            process.WaitForExit();
            if (process.ExitCode == 100 && verifier.HasPassingOutput)
            {
                return 100;
            }
            else
            {
                LogTestFailure("Profiler tests are expected to contain the text \'" + verifier.SuccessPhrase + "\' in the console output " +
                    "of the profilee app to indicate a passing test. Usually it is printed from the Shutdown() method of the profiler implementation. This " +
                    "text was not found in the output above.");
                return process.ExitCode == 100 ? process.ExitCode : -1;
            }
        }

        private static string GetProfilerPath()
        {
            string profilerName;
            if (TestLibrary.Utilities.IsWindows)
            {
                profilerName = "Profiler.dll";
            }
            else if (TestLibrary.Utilities.IsLinux)
            {
                profilerName = "libProfiler.so";
            }
            else
            {
                profilerName = "libProfiler.dylib";
            }

            string profilerPath = Path.Combine(Environment.CurrentDirectory, profilerName);
            Console.WriteLine($"Profiler path: {profilerPath}");
            return profilerPath;
        }

        private static string GetCorerunPath()
        {
            string corerunName;
            if (TestLibrary.Utilities.IsWindows)
            {
                corerunName = "CoreRun.exe";
            }
            else
            {
                corerunName = "corerun";
            }

            return Path.Combine(Environment.GetEnvironmentVariable("CORE_ROOT"), corerunName);
        }

        private static void LogTestFailure(string error)
        {
            Console.WriteLine("Test failed: " + error);
            throw new Exception(error);
        }

        /// <summary>
        /// Verifies that console output from a profiler test has the output we expect for a passing test
        /// </summary>
        class ProfileeOutputVerifier
        {
            public string SuccessPhrase = "PROFILER TEST PASSES";
            public bool HasPassingOutput { get; private set; }

            public void WriteLine(string message)
            {
                if (message != null && message.Contains(SuccessPhrase))
                {
                    HasPassingOutput = true;
                }
            }

            public void WriteLine(string format, params object[] args)
            {
                if (string.Format(format,args).Contains(SuccessPhrase))
                {
                    HasPassingOutput = true;
                }
            }
        }
    }
}
