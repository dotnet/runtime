// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.Cli.Build.Framework;
using Xunit;

namespace Microsoft.DotNet.CoreSetup.Test
{
    public static class CommandResultExtensions
    {
        public static CommandResultAssertions Should(this CommandResult commandResult)
        {
            return new CommandResultAssertions(commandResult);
        }

        public static CommandResult StdErrAfter(this CommandResult commandResult, string pattern)
        {
            int i = commandResult.StdErr.IndexOf(pattern);
            Assert.True(i >= 0, $"'{pattern}' should be in StdErr - cannot filter StdErr to after expected string.{commandResult.GetDiagnosticsInfo()}");
            string filteredStdErr = commandResult.StdErr.Substring(i);

            return new CommandResult(commandResult.StartInfo, commandResult.ProcessId, commandResult.ExitCode, commandResult.StdOut, filteredStdErr);
        }
    }
}
