// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using FluentAssertions;
using Microsoft.DotNet.Cli.Build.Framework;

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
            i.Should().BeGreaterOrEqualTo(0, "Trying to filter StdErr after '{0}', but such string can't be found in the StdErr.", pattern);
            string filteredStdErr = commandResult.StdErr.Substring(i);

            return new CommandResult(commandResult.StartInfo, commandResult.ExitCode, commandResult.StdOut, filteredStdErr);
        }
    }
}
