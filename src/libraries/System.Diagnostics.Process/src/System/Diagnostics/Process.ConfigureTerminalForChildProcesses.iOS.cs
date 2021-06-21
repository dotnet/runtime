// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;

namespace System.Diagnostics
{
    public partial class Process
    {
        /// <summary>
        /// This method is called when the number of child processes that are using the terminal changes.
        /// It updates the terminal configuration if necessary.
        /// </summary>
        internal static void ConfigureTerminalForChildProcesses(int increment, bool configureConsole = true)
        { }

        private static bool AreChildrenUsingTerminal => false;
    }
}
