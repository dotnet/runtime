// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Win32.SafeHandles;

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

        static partial void SetDelayedSigChildConsoleConfigurationHandler();

        private static bool AreChildrenUsingTerminal => false;

        internal static bool IsTerminal(SafeFileHandle? _) => false;
    }
}
