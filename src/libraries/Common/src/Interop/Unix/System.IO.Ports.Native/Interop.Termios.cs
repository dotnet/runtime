// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO.Ports;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Termios
    {
        [Flags]
        internal enum Signals
        {
            None = 0,
            SignalDtr = 1 << 0,
            SignalDsr = 1 << 1,
            SignalRts = 1 << 2,
            SignalCts = 1 << 3,
            SignalDcd = 1 << 4,
            SignalRng = 1 << 5,
            Error = -1,
        }

        internal enum Queue
        {
            AllQueues = 0,
            ReceiveQueue = 1,
            SendQueue = 2,
        }

        [LibraryImport(Libraries.IOPortsNative, EntryPoint = "SystemIoPortsNative_TermiosReset", SetLastError = true)]
        internal static partial int TermiosReset(SafeSerialDeviceHandle handle, int speed, int data, StopBits stop, Parity parity, Handshake flow);

        [LibraryImport(Libraries.IOPortsNative, EntryPoint = "SystemIoPortsNative_TermiosGetSignal", SetLastError = true)]
        internal static partial int TermiosGetSignal(SafeSerialDeviceHandle handle, Signals signal);

        [LibraryImport(Libraries.IOPortsNative, EntryPoint = "SystemIoPortsNative_TermiosSetSignal", SetLastError = true)]
        internal static partial int TermiosGetSignal(SafeSerialDeviceHandle handle, Signals signal, int set);

        [LibraryImport(Libraries.IOPortsNative, EntryPoint = "SystemIoPortsNative_TermiosGetAllSignals")]
        internal static partial Signals TermiosGetAllSignals(SafeSerialDeviceHandle handle);

        [LibraryImport(Libraries.IOPortsNative, EntryPoint = "SystemIoPortsNative_TermiosSetSpeed", SetLastError = true)]
        internal static partial int TermiosSetSpeed(SafeSerialDeviceHandle handle, int speed);

        [LibraryImport(Libraries.IOPortsNative, EntryPoint = "SystemIoPortsNative_TermiosGetSpeed", SetLastError = true)]
        internal static partial int TermiosGetSpeed(SafeSerialDeviceHandle handle);

        [LibraryImport(Libraries.IOPortsNative, EntryPoint = "SystemIoPortsNative_TermiosAvailableBytes", SetLastError = true)]
        internal static partial int TermiosGetAvailableBytes(SafeSerialDeviceHandle handle, [MarshalAs(UnmanagedType.Bool)] bool fromReadBuffer);

        [LibraryImport(Libraries.IOPortsNative, EntryPoint = "SystemIoPortsNative_TermiosDiscard", SetLastError = true)]
        internal static partial int TermiosDiscard(SafeSerialDeviceHandle handle, Queue input);

        [LibraryImport(Libraries.IOPortsNative, EntryPoint = "SystemIoPortsNative_TermiosDrain", SetLastError = true)]
        internal static partial int TermiosDrain(SafeSerialDeviceHandle handle);

        [LibraryImport(Libraries.IOPortsNative, EntryPoint = "SystemIoPortsNative_TermiosSendBreak", SetLastError = true)]
        internal static partial int TermiosSendBreak(SafeSerialDeviceHandle handle, int duration);
    }
}
