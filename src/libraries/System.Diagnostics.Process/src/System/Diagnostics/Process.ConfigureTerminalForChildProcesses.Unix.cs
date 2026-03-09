// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Diagnostics
{
    public partial class Process
    {
        internal static void ConfigureTerminalForChildProcesses(int increment, bool configureConsole = true)
            => ProcessUtils.ConfigureTerminalForChildProcesses(increment, configureConsole);

        private static void SetDelayedSigChildConsoleConfigurationHandler()
            => ProcessUtils.SetDelayedSigChildConsoleConfigurationHandler();

        private static bool AreChildrenUsingTerminal => ProcessUtils.AreChildrenUsingTerminal;
    }
}
