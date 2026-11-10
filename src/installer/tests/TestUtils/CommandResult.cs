// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Microsoft.DotNet.Cli.Build.Framework
{
    public struct CommandResult
    {
        public ProcessStartInfo StartInfo { get; }
        public int ProcessId { get; }
        public int ExitCode { get; }
        public string StdOut { get; }
        public string StdErr { get; }

        public CommandResult(ProcessStartInfo startInfo, int pid, int exitCode, string stdOut, string stdErr)
        {
            StartInfo = startInfo;
            ProcessId = pid;
            ExitCode = exitCode;
            StdOut = stdOut;
            StdErr = stdErr;
        }

        internal string GetDiagnosticsInfo()
            => $"""

                File Name: {StartInfo.FileName}
                Arguments: {StartInfo.Arguments}
                Environment:
                {string.Join(Environment.NewLine, StartInfo.Environment.Where(i => i.Key.StartsWith("DOTNET_", OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal)).Select(i => $"  {i.Key} = {i.Value}"))}
                Exit Code: 0x{ExitCode:x}
                StdOut:
                {StdOut}
                StdErr:
                {StdErr}
                """;
    }
}
