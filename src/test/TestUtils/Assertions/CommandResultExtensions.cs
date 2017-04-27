// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.Cli.Build.Framework;

namespace Microsoft.DotNet.CoreSetup.Test
{
    public static class CommandResultExtensions
    {
        public static CommandResultAssertions Should(this CommandResult commandResult)
        {
            return new CommandResultAssertions(commandResult);
        }
    }
}
