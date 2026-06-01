// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.Win32.SafeHandles;

namespace System.IO.Ports
{
    internal sealed partial class SafeSerialDeviceHandle : SafeHandleMinusOneIsInvalid
    {
        // When the user calls Dispose, some operations for a pending read event might be in flight.
        // If these use the handle, the user Dispose call won't actually release the handle immediately
        // which causes opening the port after the Dispose to fail (EBUSY).
        // DisposeLock guards these operations so that they can not happen concurrent with the Dispose.
        private object DisposeLock => this;

        public SafeSerialDeviceHandle() : base(ownsHandle: true)
        {
        }

        internal static SafeSerialDeviceHandle Open(string portName)
        {
            Debug.Assert(portName != null);
            SafeSerialDeviceHandle handle = Interop.Serial.SerialPortOpen(portName);

            if (handle.IsInvalid)
            {
                handle.Dispose();

                // exception type is matching Windows
                throw new UnauthorizedAccessException(
                    SR.Format(SR.UnauthorizedAccess_IODenied_Port, portName),
                    Interop.GetIOException(Interop.Sys.GetLastErrorInfo()));
            }

            return handle;
        }

        protected override bool ReleaseHandle()
        {
            Interop.Serial.Shutdown(handle, SocketShutdown.Both);
            int result = Interop.Serial.SerialPortClose(handle);

            Debug.Assert(result == 0, $"Close failed with result {result} and error {Interop.Sys.GetLastErrorInfo()}");

            return result == 0;
        }

        // Get the amount of bytes that can be read from the handle plus the amount buffered by the caller.
        // When throwOnDispose is false, returns 0 when disposed instead of throwing.
        internal int GetBytesToRead(int buffered, bool throwOnDispose = true)
        {
            lock (DisposeLock)
            {
                if (!throwOnDispose && IsClosed)
                {
                    return 0;
                }

                try
                {
                    return buffered + BytesToRead;
                }
                catch (ObjectDisposedException) when (!throwOnDispose)
                {
                    return 0;
                }
            }
        }

        // Gets the amount input data buffered by the handle.
        partial void GetBufferedCount(ref int count);

        // Gets the amount of bytes that can be read from the handle.
        private int BytesToRead
        {
            get
            {
                Debug.Assert(Monitor.IsEntered(DisposeLock));

                int buffered = 0;
                GetBufferedCount(ref buffered);
                return Math.Max(Interop.Termios.TermiosGetAvailableBytes(this, true), 0) + buffered;
            }
        }
    }
}
