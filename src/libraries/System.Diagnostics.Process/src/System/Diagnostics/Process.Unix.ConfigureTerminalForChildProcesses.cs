// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;

namespace System.Diagnostics
{
    public partial class Process
    {
        private static int s_childrenUsingTerminalCount;

        static partial void ConfigureTerminalForChildProcessesInner(int increment, bool configureConsole)
        {
            Debug.Assert(increment != 0);

            int childrenUsingTerminalRemaining = Interlocked.Add(ref s_childrenUsingTerminalCount, increment);
            if (increment > 0)
            {
                Debug.Assert(s_processStartLock.IsReadLockHeld);
                Debug.Assert(configureConsole);

                // At least one child is using the terminal.
                Interop.Sys.ConfigureTerminalForChildProcess(childUsesTerminal: true);
            }
            else
            {
                Debug.Assert(s_processStartLock.IsWriteLockHeld);

                if (childrenUsingTerminalRemaining == 0 && configureConsole)
                {
                    // No more children are using the terminal.
                    Interop.Sys.ConfigureTerminalForChildProcess(childUsesTerminal: false);
                }
            }
        }
    }
}
