// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace GCPerfTestFramework
{
    public static class ProcessFactory
    {
        const string ProbePathEnvironmentVariable = "GC_PERF_TEST_PROBE_PATH";
        const string CoreRunProbePathEnvironmentVariable = "GC_PERF_TEST_CORE_RUN_PROBE_PATH";
        const string UseCoreCLREnvironmentVariable = "GC_PERF_TEST_CORECLR";
        const string ConcurrentGCVariable = "COMPLUS_gcConcurrent";
        const string ServerGCVariable = "COMPLUS_gcServer";
        const string CoreRunName = "CoreRun.exe";
        const string UnixCoreRunName = "corerun";

        // The default timeout for a test is half an hour. If a test takes that long, it is
        // definitely not responding.
        const int DefaultTimeout = 1800000 /* ms */;

        /// <summary>
        /// Location of the CoreRun hosting process, for use in CoreCLR performance runs
        /// when the operating system can't launch a managed assembly directly. This runs
        /// as part of the static constructor and so all tests will fail if CoreRun cannot
        /// be found.
        /// </summary>
        private static string s_coreRun = LocateCoreRun();

        /// <summary>
        /// Launches a process that is part of a test scenario, waits for it complete, and returns.
        ///  This API does several things specific to this test framework:
        /// 
        ///  1. The fileName argument is absolute, and must be resolved using executable probing:
        ///     see <see cref="ProbeForFile"/> for more information. The reason why this function does not
        ///     perform the probing is that this function is invoked while "on the clock" by the benchmarking process
        ///     and file system probing is costly and only needs to be done once, before the test begins.
        ///     The general pattern for perf tests is that the test executable is located before beginning the benchmark
        ///     step to avoid doing file system lookups while on the clock.
        ///  2. The arguments argument is passed verbatim to the Process that is spawned,
        ///  3. The passed environment variables are set for the child process, replacing any variables
        ///     in the existing process. xunit-performance by default turns off ConcurrentGC and ServerGC, 
        ///     and we need to restore that when our process is completed.
        ///  4. The timeout parameter controls how long this function will wait once a process is spawned.
        ///     if the supplied timeout is less than or equal to zero, this function will wait indefinitely for the child process.
        ///     If a process does timeout, this function throws a <see cref="TimeoutException"/>.
        /// 
        /// This method delegates partially to a platform-specific implementation which determines whether or not the operating
        /// system is capable of executing a managed assembly directly or if a hosting process needs to be used.
        /// Currently, this means that the executable will be directly executed on Desktop CLR, running on Windows, while
        /// CoreCLR on any platform will need to invoke a hosting process.
        /// </summary>
        /// <param name="fileName">The absolute path to the executable to execute</param>
        /// <param name="arguments">The arguments to pass to the executable</param>
        /// <param name="environmentVariables">Any environment variables to pass to the child process</param>
        /// <param name="timeout">How long to wait, in milliseconds, on the child process. If less than or equal to zero,
        /// no timeout is used.</param>
        /// <exception cref="TimeoutException">Thrown if the process takes longer than timout to terminate.</exception>
        public static void LaunchProcess(
            string fileName,
            string arguments = "",
            IDictionary<string, string> environmentVariables = null,
            int timeout = DefaultTimeout)
        {
            var previousEnvironmentVars = new Dictionary<string, string>();

            if (environmentVariables != null)
            {
                foreach (var pair in environmentVariables)
                {
                    var replacedValue = Environment.GetEnvironmentVariable(pair.Key);
                    previousEnvironmentVars.Add(pair.Key, replacedValue);
                    Environment.SetEnvironmentVariable(pair.Key, pair.Value);
                }
            }

            try
            {
                Process process;

                // for CoreCLR, we need to launch using the CoreRun hosting process.
                if (ShouldUseCoreRun())
                {
                    process = LaunchProcessCoreClrImpl(fileName, arguments);
                }
                else
                {
                    process = LaunchProcessDesktopImpl(fileName, arguments);
                }

                if (timeout > 0)
                {
                    // the caller has specified a timeout. Use it.
                    if (!process.WaitForExit(timeout))
                    {
                        process.Kill();
                        throw new TimeoutException("Process did not complete within the allotted time");
                    }

                    return;
                }

                process.WaitForExit();
            }
            finally
            {
                // Restore the original environment variables
                if (environmentVariables != null)
                {
                    foreach (var pair in previousEnvironmentVars)
                    {
                        Environment.SetEnvironmentVariable(pair.Key, pair.Value);
                    }
                }
            }
        }

        /// <summary>
        /// Launches a process directly by allowing the underlying operating system to invoke the managed
        /// assembly.
        /// </summary>
        /// <param name="fileName">The absolute path of the executable to run</param>
        /// <param name="arguments">The arguments to the target executable</param>
        private static Process LaunchProcessDesktopImpl(string fileName, string arguments)
        {
            var process = new Process();
            process.StartInfo.FileName = fileName;
            process.StartInfo.Arguments = arguments;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = false;
            process.Start();
            return process;
        }

        /// <summary>
        /// Launches a process indirectly by invoking a hosting process that will invoke the given managed assembly.
        /// This is usually called "corerun" for CoreCLR.
        /// </summary>
        /// <param name="fileName">The absolute path of the executable to run</param>
        /// <param name="arguments">The arguments to the target executable</param>
        private static Process LaunchProcessCoreClrImpl(string fileName, string arguments)
        {
            var process = new Process();
            process.StartInfo.FileName = s_coreRun;
            process.StartInfo.Arguments = fileName + " " + arguments;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = false;
            process.Start();
            return process;
        }

        /// <summary>
        /// Locates the CoreRun executable based on the probe path given in the CoreRunProbePathEnvironmentVariable
        /// environment variable.
        /// </summary>
        /// <returns>The located path of CoreRun.exe</returns>
        /// <exception cref="InvalidOperationException">If CoreRun.exe cannot be found on the given path.</exception>
        private static string LocateCoreRun()
        {
            if (!ShouldUseCoreRun())
            {
                // no need to locate CoreRun if it won't be used.
                return string.Empty;
            }

            var coreRunProbePath = Environment.GetEnvironmentVariable(CoreRunProbePathEnvironmentVariable);
            if (coreRunProbePath == null)
            {
                throw new InvalidOperationException($"Environment variable {CoreRunProbePathEnvironmentVariable} must be set for CoreCLR performance runs!");
            }

            var path = ProbeForFileImpl(CoreRunName, coreRunProbePath);

#if !WINDOWS
            // CoreRun.exe may not have the .exe extension on non-Windows platforms.
            if (path == null)
            {
                path = ProbeForFileImpl(UnixCoreRunName, coreRunProbePath);
            }
#endif

            if (path == null)
            {
                throw new InvalidOperationException($"Failed to locate {CoreRunName} on search path {coreRunProbePath}");
            }

            return path;
        }

        private static bool ShouldUseCoreRun()
        {
#if WINDOWS
            return Environment.GetEnvironmentVariable(UseCoreCLREnvironmentVariable) == "1";
#else
            return true;
#endif
        }

        /// <summary>
        /// Probes for a file named fileName starting recursively from the directory named in the ProbePathEnvironmentVariable.
        /// </summary>
        /// <param name="fileName">The filename to probe for</param>
        /// <returns>An absolute path to the located file</returns>
        /// <exception cref="InvalidOperationException">
        /// If the probe path environment variable is not set, or the named file cannot be found
        /// in the probe path.
        /// </exception>
        public static string ProbeForFile(string fileName)
        {
            var probePath = Environment.GetEnvironmentVariable(ProbePathEnvironmentVariable);
            if (probePath == null)
            {
                // fall back to the current working directory if the probe path is not set
                probePath = Directory.GetCurrentDirectory();
            }

            var path = ProbeForFileImpl(fileName, probePath);
            if (path == null)
            {
                throw new InvalidOperationException($"Failed to locate file \"{ fileName }\" on path \"{probePath}\"");
            }

            return path;
        }

        /// <summary>
        /// Starting at probePath, probe all files in that directory and all directories
        /// recursively for a file named fileName. The filename equality check is case-insensitive.
        /// </summary>
        /// <param name="fileName">The name of the file to search for</param>
        /// <param name="probePath">The directory to start the recursive search</param>
        /// <returns>An absolute path to the file if found, or null if the file is not found.</returns>
        private static string ProbeForFileImpl(string fileName, string probePath)
        {
            // probe from the top down - we don't want to waste lots of time doing a bottom up
            // search in a deep directory tree if the files we are looking for are at the top-level.
            foreach (var file in Directory.EnumerateFiles(probePath))
            {
                if (StringComparer.OrdinalIgnoreCase.Equals(Path.GetFileName(file), fileName))
                {
                    return file;
                }
            }

            foreach (var directory in Directory.EnumerateDirectories(probePath))
            {
                var result = ProbeForFileImpl(fileName, directory);
                if (result != null) return result;
            }

            return null;
        }
    }
}
