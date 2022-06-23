// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Win32.SafeHandles;
using System.Collections;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.IO.Ports;
using System.Net.Sockets;

namespace System.IO.Ports
{
#pragma warning disable CA1844
    internal sealed partial class SerialStream : Stream
#pragma warning restore CA1844
    {
        private const int MaxDataBits = 8;
        private const int MinDataBits = 5;

        // members supporting properties exposed to SerialPort
        private readonly string _portName;
        private bool _inBreak;
        private Handshake _handshake;

#pragma warning disable CS0067 // Events shared by Windows and Linux, on Linux we currently never call them
        // called when any runtime error occurs on the port (frame, overrun, parity, etc.)
        internal event SerialErrorReceivedEventHandler ErrorReceived;
#pragma warning restore CS0067

        // ----SECTION: inherited properties from Stream class ------------*

        // These six properites are required for SerialStream to inherit from the abstract Stream class.
        // Note four of them are always true or false, and two of them throw exceptions, so these
        // are not usefully queried by applications which know they have a SerialStream, etc...
        public override bool CanRead
        {
            get { return (_handle != null); }
        }

        public override bool CanSeek
        {
            get { return false; }
        }

        public override bool CanTimeout
        {
            get { return (_handle != null); }
        }

        public override bool CanWrite
        {
            get { return (_handle != null); }
        }

        public override long Length
        {
            get { throw new NotSupportedException(SR.NotSupported_UnseekableStream); }
        }

        public override long Position
        {
            get { throw new NotSupportedException(SR.NotSupported_UnseekableStream); }
            set { throw new NotSupportedException(SR.NotSupported_UnseekableStream); }
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException(SR.NotSupported_UnseekableStream);
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException(SR.NotSupported_UnseekableStream);
        }

        public override int ReadByte()
        {
            return ReadByte(ReadTimeout);
        }

        public override void Write(byte[] array, int offset, int count)
        {
            Write(array, offset, count, WriteTimeout);
        }

        ~SerialStream()
        {
            Dispose(false);
        }

        private static void CheckArrayArguments(byte[] array, int offset, int count)
        {
            ArgumentNullException.ThrowIfNull(array);

            if (offset < 0)
                throw new ArgumentOutOfRangeException(nameof(offset), SR.ArgumentOutOfRange_NeedNonNegNumRequired);
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count), SR.ArgumentOutOfRange_NeedNonNegNumRequired);
            if (array.Length - offset < count)
                throw new ArgumentException(SR.Argument_InvalidOffLen);
        }

        private void CheckHandle()
        {
            if (_handle == null)
                InternalResources.FileNotOpen();
        }

        private void CheckReadWriteArguments(byte[] array, int offset, int count)
        {
            CheckArrayArguments(array, offset, count);

            CheckHandle();
        }

        private void CheckWriteArguments()
        {
            if (_inBreak)
                throw new InvalidOperationException(SR.In_Break_State);

            CheckHandle();
        }

        private void CheckWriteArguments(byte[] array, int offset, int count)
        {
            if (_inBreak)
                throw new InvalidOperationException(SR.In_Break_State);

            CheckReadWriteArguments(array, offset, count);
        }
    }
}
