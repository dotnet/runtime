// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using Xunit;

namespace System
{
    public static partial class AdminHelpers
    {
        /// <summary>
        /// Runs the given command as sudo (for Unix).
        /// </summary>
        /// <param name="commandLine">The command line to run as sudo</param>
        /// <returns> Returns the process exit code (0 typically means it is successful)</returns>
        public static int RunAsSudo(string commandLine)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo()
            {
                FileName = "sudo",
                Arguments = commandLine
            };

            using (Process process = Process.Start(startInfo))
            {
                Assert.True(process.WaitForExit(30000));
                return process.ExitCode;
            }
        }

    }
}
