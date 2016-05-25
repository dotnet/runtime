// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.Cli.Build.Framework;

namespace Microsoft.DotNet.Scripts
{
    public class Program
    {
        public static int Main(string[] args)
        {
            DebugHelper.HandleDebugSwitch(ref args);

            return BuildSetup.Create(".NET core-setup Dependency Updater")
                .UseTargets(new[]
                {
                    new BuildTarget("Default", "Dependency Updater Goals", new [] { "UpdateFiles", "PushPR" }),
                    new BuildTarget("UpdateFiles", "Dependency Updater Goals"),
                    new BuildTarget("PushPR", "Dependency Updater Goals"),
                })
                .UseAllTargetsFromAssembly<Program>()
                .Run(args);
        }
    }
}
