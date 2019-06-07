// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.DotNet.Cli.Build.Framework;

namespace Microsoft.DotNet.CoreSetup.Test.HostActivation
{
    public static class CommandExtensions
    {
        public static Command EnableHostTracing(this Command command)
        {
            return command.EnvironmentVariable(Constants.HostTracing.TraceLevelEnvironmentVariable, "1");
        }

        public static Command EnableTracingAndCaptureOutputs(this Command command)
        {
            return command
                .EnableHostTracing()
                .CaptureStdOut()
                .CaptureStdErr();
        }

        public static Command RuntimeId(this Command command, string rid)
        {
            return command.EnvironmentVariable(Constants.RuntimeId.EnvironmentVariable, rid);
        }
    }
}
