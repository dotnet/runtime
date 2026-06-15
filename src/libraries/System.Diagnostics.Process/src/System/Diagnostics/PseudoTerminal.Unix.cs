// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace System.Diagnostics
{
    public sealed partial class PseudoTerminal
    {
        /// <summary>
        /// Gets the primary (read/write) side of the pseudo-terminal.
        /// </summary>
        internal SafeFileHandle Primary { get; }

        /// <summary>
        /// Gets the secondary (child process) side of the pseudo-terminal.
        /// </summary>
        internal SafeFileHandle Secondary { get; }

        private PseudoTerminal(SafeFileHandle primary, SafeFileHandle secondary)
        {
            Primary = primary;
            Secondary = secondary;
        }

        private static PseudoTerminal CreateCore(PseudoTerminalOptions? options)
        {
            int columns = options?.Columns ?? 80;
            int rows = options?.Rows ?? 24;

            int result = Interop.Sys.OpenPseudoTerminal(out int primaryFd, out int secondaryFd, columns, rows);
            if (result != 0)
            {
                throw new Win32Exception(Marshal.GetLastPInvokeError());
            }

            SafeFileHandle primary = new SafeFileHandle((IntPtr)primaryFd, ownsHandle: true);
            SafeFileHandle secondary = new SafeFileHandle((IntPtr)secondaryFd, ownsHandle: true);

            return new PseudoTerminal(primary, secondary);
        }

        private void ResizeCore(int columns, int rows)
        {
            int result = Interop.Sys.ResizePseudoTerminal(Primary, columns, rows);
            if (result != 0)
            {
                throw new Win32Exception(Marshal.GetLastPInvokeError());
            }
        }

        private void DisposeCore()
        {
            Primary.Dispose();
            Secondary.Dispose();
        }
    }
}
