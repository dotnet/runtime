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
    public delegate void ProfilerCallback();

    [Flags]
    public enum ProfileeOptions
    {
        None = 0,
        OptimizationSensitive,
        NoStartupAttach,
        ReverseDiagnosticsMode
    }

    public class ProfilerTestRunner
    {
        public static int Run(string profileePath,
                              string testName,
                              Guid profilerClsid,
                              string profileeArguments = "",
                              ProfileeOptions profileeOptions = ProfileeOptions.None,
                              Dictionary<string, string> envVars = null,
                              string reverseServerName = null,
                              bool loadAsNotification = false,
                              int notificationCopies = 1)
        {
            string arguments;
            string program;
            string profileeAppDir = Path.GetDirectoryName(profileePath);

            if (envVars == null)
            {
                envVars = new Dictionary<string, string>();
            }

            arguments = profileePath + " RunTest " + profileeArguments;
            program = GetCorerunPath();
            string profilerPath = GetProfilerPath();
            if (!profileeOptions.HasFlag(ProfileeOptions.NoStartupAttach))
            {
                envVars.Add("CORECLR_ENABLE_PROFILING", "1");

                if (loadAsNotification)
                {
                    StringBuilder builder = new StringBuilder();
                    for(int i = 0; i < notificationCopies; ++i)
                    {
                        builder.Append(profilerPath);
                        builder.Append("=");
                        builder.Append("{");
                        builder.Append(profilerClsid.ToString());
                        builder.Append("}");
                        builder.Append(";");
                    }

                    envVars.Add("CORECLR_ENABLE_NOTIFICATION_PROFILERS", "1");
                    envVars.Add("CORECLR_NOTIFICATION_PROFILERS", builder.ToString());

                }
                else
                {
                    envVars.Add("CORECLR_PROFILER", "{" + profilerClsid + "}");
                    envVars.Add("CORECLR_PROFILER_PATH", profilerPath);
                }
            }

            if (profileeOptions.HasFlag(ProfileeOptions.OptimizationSensitive))
            {
                Console.WriteLine("Disabling tiered compilation, jitstress, and minopts.");
                envVars.Add("COMPlus_TieredCompilation", "0");
                envVars.Add("COMPlus_JitStress", "0");
                envVars.Add("COMPlus_JITMinOpts", "0");
            }

            if (profileeOptions.HasFlag(ProfileeOptions.ReverseDiagnosticsMode))
            {
                Console.WriteLine("Launching profilee in reverse diagnostics port mode.");
                if (String.IsNullOrEmpty(reverseServerName))
                {
                    throw new ArgumentException();
                }

                envVars.Add("DOTNET_DiagnosticPorts", reverseServerName);
            }

            envVars.Add("Profiler_Test_Name", testName);

            if(!File.Exists(profilerPath))
            {
                FailFastWithMessage("Profiler library not found at expected path: " + profilerPath);
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

            // There are two conditions for profiler tests to pass, the output of the profiled program
            // must contain the phrase "PROFILER TEST PASSES" and the return code must be 100. This is
            // because lots of verification happen in the profiler code, where it is hard to change the
            // program return value.

            if (!verifier.HasPassingOutput)
            {
                FailFastWithMessage("Profiler tests are expected to contain the text \'" + verifier.SuccessPhrase + "\' in the console output " +
                    "of the profilee app to indicate a passing test. Usually it is printed from the Shutdown() method of the profiler implementation. This " +
                    "text was not found in the output above.");
            }

            if (process.ExitCode != 100)
            {
                FailFastWithMessage($"Profilee returned exit code {process.ExitCode} instead of expected exit code 100.");
            }

            return 100;
        }

        public static string GetProfilerPath()
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

        private static void FailFastWithMessage(string error)
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
