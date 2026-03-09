// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Diagnostics
{
    internal static partial class ProcessUtils
    {
        /// These methods are used on other Unix systems to track how many children use the terminal,
        /// and update the terminal configuration when necessary.

        [Conditional("unnecessary")]
        internal static void ConfigureTerminalForChildProcesses(int increment, bool configureConsole = true)
        {
        }

        internal static void SetDelayedSigChildConsoleConfigurationHandler()
        {
        }

        internal static bool AreChildrenUsingTerminal => false;
    }
}
