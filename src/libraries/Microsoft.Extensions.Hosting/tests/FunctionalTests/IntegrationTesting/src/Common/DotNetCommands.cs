// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Microsoft.Extensions.Hosting.IntegrationTesting
{
    public static class DotNetCommands
    {
        // Set by the test run script to the testhost it was handed via --runtime-path: the locally built
        // testhost for a local run, the Helix correlation payload in CI. See eng/testing/RunnerTemplate.sh
        // and eng/testing/RunnerTemplate.cmd.
        private const string RuntimePathVariableName = "RUNTIME_PATH";

        /// <summary>
        /// Gets the full path of the muxer that portable applications are launched with, or
        /// <see langword="null"/> when the current test environment has not got one.
        /// </summary>
        public static string DotNetMuxerPath { get; } = FindDotNetMuxer();

        public static string DotNetExecutableName
            => RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "dotnet.exe" : "dotnet";

        private static string FindDotNetMuxer()
        {
            var runtimePath = Environment.GetEnvironmentVariable(RuntimePathVariableName);
            if (!string.IsNullOrEmpty(runtimePath))
            {
                var fromRunScript = Path.Combine(runtimePath, DotNetExecutableName);
                if (File.Exists(fromRunScript))
                {
                    return fromRunScript;
                }
            }

#if NETFRAMEWORK
            return null;
#else
            // Outside the run script the only host we can vouch for is the one running the tests, which is
            // the muxer itself on every leg that does not publish the tests as a self-contained application.
            var processPath = Environment.ProcessPath;
            return string.Equals(Path.GetFileName(processPath), DotNetExecutableName, StringComparison.OrdinalIgnoreCase)
                ? processPath
                : null;
#endif
        }
    }
}
