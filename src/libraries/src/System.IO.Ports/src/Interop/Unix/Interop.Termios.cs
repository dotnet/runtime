// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO.Ports;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class Termios
    {
        internal enum Signals
        {
            SignalDtr = 1,
            SignalDsr = 2,
            SignalRts = 3,
            SignalCts = 4,
            SignalDcd = 5,
        }

        internal enum Queue
        {
            AllQueues = 0,
            ReceiveQueue = 1,
            SendQueue = 2,
        }

        [DllImport(Libraries.IOPortsNative, EntryPoint = "SystemIoPortsNative_TermiosReset", SetLastError = true)]
        internal static extern int TermiosReset(SafeFileHandle handle, int speed, int data, StopBits stop, Parity parity, Handshake flow);

        [DllImport(Libraries.IOPortsNative, EntryPoint = "SystemIoPortsNative_TermiosGetSignal", SetLastError = true)]
        internal static extern int TermiosGetSignal(SafeFileHandle handle, Signals signal);

        [DllImport(Libraries.IOPortsNative, EntryPoint = "SystemIoPortsNative_TermiosSetSignal", SetLastError = true)]
        internal static extern int TermiosGetSignal(SafeFileHandle handle, Signals signal, int set);

        [DllImport(Libraries.IOPortsNative, EntryPoint = "SystemIoPortsNative_TermiosSetSpeed", SetLastError = true)]
        internal static extern int TermiosSetSpeed(SafeFileHandle handle, int speed);

        [DllImport(Libraries.IOPortsNative, EntryPoint = "SystemIoPortsNative_TermiosGetSpeed", SetLastError = true)]
        internal static extern int TermiosGetSpeed(SafeFileHandle handle);

        [DllImport(Libraries.IOPortsNative, EntryPoint = "SystemIoPortsNative_TermiosAvailableBytes", SetLastError = true)]
        internal static extern int TermiosGetAvailableBytes(SafeFileHandle handle, int input);

        [DllImport(Libraries.IOPortsNative, EntryPoint = "SystemIoPortsNative_TermiosDiscard", SetLastError = true)]
        internal static extern int TermiosDiscard(SafeFileHandle handle, Queue input);

        [DllImport(Libraries.IOPortsNative, EntryPoint = "SystemIoPortsNative_TermiosDrain", SetLastError = true)]
        internal static extern int TermiosDrain(SafeFileHandle handle);

        [DllImport(Libraries.IOPortsNative, EntryPoint = "SystemIoPortsNative_TermiosSendBreak", SetLastError = true)]
        internal static extern int TermiosSendBreak(SafeFileHandle handle, int duration);
    }
}
