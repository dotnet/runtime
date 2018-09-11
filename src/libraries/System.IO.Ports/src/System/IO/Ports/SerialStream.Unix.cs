// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Win32.SafeHandles;
using System.Collections;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.IO.Ports;
using System.Net.Sockets;

namespace System.IO.Ports
{
    internal sealed partial class SerialStream : Stream
    {
        private const int MaxDataBits = 8;
        private const int MinDataBits = 5;

        private string _portName;
        private int _baudRate;
        private StopBits _stopBits;
        private Handshake _handshake;
        private Parity _parity;
        private int _dataBits = 8;
        private bool _inBreak = false;
        private SafeFileHandle _handle = null;
        private bool _rtsEnable = false;
        private int _readTimeout = 0;
        private int _writeTimeout = 0;
        private byte[] _tempBuf = new byte[1];

        // three different events, also wrapped by SerialPort.
        internal event SerialDataReceivedEventHandler DataReceived;     // called when one character is received.
        internal event SerialPinChangedEventHandler PinChanged;         // called when any of the pin/ring-related triggers occurs
        internal event SerialErrorReceivedEventHandler ErrorReceived;   // called when any runtime error occurs on the port (frame, overrun, parity, etc.)

        // ----SECTION: inherited properties from Stream class ------------*

        // These six properties are required for SerialStream to inherit from the abstract Stream class.
        // Note four of them are always true or false, and two of them throw exceptions, so these
        // are not usefully queried by applications which know they have a SerialStream, etc...

        public override int ReadTimeout
        {
            get { return _readTimeout; }
            set
            {
                if (value < 0 && value != SerialPort.InfiniteTimeout)
                    throw new ArgumentOutOfRangeException(nameof(ReadTimeout), SR.ArgumentOutOfRange_Timeout);
                if (_handle == null) {
                    throw new ObjectDisposedException(SR.Port_not_open);
                }
                _readTimeout = value;
            }
        }

        public override int WriteTimeout
        {
            get { return _writeTimeout; }
            set
            {
                if (value < 0 && value != SerialPort.InfiniteTimeout)
                    throw new ArgumentOutOfRangeException(nameof(ReadTimeout), SR.ArgumentOutOfRange_Timeout);
                if (_handle == null) {
                    throw new ObjectDisposedException(SR.Port_not_open);
                }
                _writeTimeout = value;
            }
        }

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

        internal int BaudRate
        {
            set
            {
                if (value != _baudRate)
                {
                    if (value <= 0 || value > 230400)
                    {
                        throw new ArgumentOutOfRangeException(nameof(BaudRate), SR.ArgumentOutOfRange_NeedPosNum);
                    }

                    if (Interop.Termios.TermiosSetSpeed(_handle, value) < 0)
                    {
                        throw new IOException();
                    }

                    _baudRate = value;
                }
            }

            get
            {
                return Interop.Termios.TermiosGetSpeed(_handle);
            }
        }

        public bool BreakState
        {
            get { return _inBreak; }
            set
            {
                if (value)
                {
                    // Unlike Windows, there is no infinite break and positive value is platform dependent.
                    // As best guess, send break with default duration.
                    Interop.Termios.TermiosSendBreak(_handle, 0);
                }
                _inBreak = value;
            }
        }

        internal int BytesToWrite
        {
            get { return Interop.Termios.TermiosGetAvailableBytes(_handle, Interop.Termios.Queue.SendQueue); }
        }

        internal int BytesToRead
        {
            get { return Interop.Termios.TermiosGetAvailableBytes(_handle, Interop.Termios.Queue.ReceiveQueue); }
        }

        internal bool CDHolding
        {
            get
            {
                int status = Interop.Termios.TermiosGetSignal(_handle, Interop.Termios.Signals.SignalDcd);
                if (status < 0)
                {
                    throw new IOException();
                }

                return status == 1;
            }
        }

        internal bool CtsHolding
        {
            get
            {
                int status = Interop.Termios.TermiosGetSignal(_handle, Interop.Termios.Signals.SignalCts);
                if (status < 0)
                {
                    throw new IOException();
                }

                return status == 1;
            }
        }

        internal bool DsrHolding
        {
            get
            {
                int status = Interop.Termios.TermiosGetSignal(_handle, Interop.Termios.Signals.SignalDsr);
                if (status < 0)
                {
                    throw new IOException();
                }

                return status == 1;
            }
        }

        internal bool DtrEnable
        {
            get
            {
                int status = Interop.Termios.TermiosGetSignal(_handle, Interop.Termios.Signals.SignalDtr);
                if (status < 0)
                {
                    throw new IOException();
                }

                return status == 1;
            }

            set
            {
                if (Interop.Termios.TermiosGetSignal(_handle, Interop.Termios.Signals.SignalDtr, value ? 1 : 0) != 0)
                {
                    throw new IOException();
                }
            }
        }

        internal bool RtsEnable
        {
            get
            {
                int status = Interop.Termios.TermiosGetSignal(_handle, Interop.Termios.Signals.SignalRts);
                if (status < 0)
                {
                    throw new IOException();
                }

                return status == 1;
            }

            set
            {
                if (Interop.Termios.TermiosGetSignal(_handle, Interop.Termios.Signals.SignalRts, value ? 1 : 0) != 0)
                {
                    throw new IOException();
                }
            }
        }

        internal Handshake Handshake
        {
            set
            {
                Debug.Assert(!(value < Handshake.None || value > Handshake.RequestToSendXOnXOff),
                    "An invalid value was passed to Handshake");

                if (value != _handshake)
                {
                    if (Interop.Termios.TermiosReset(_handle, _baudRate, _dataBits, _stopBits, _parity, _handshake) != 0)
                    {
                        throw new ArgumentException();
                    }
                    _handshake = value;
                }
            }
        }

        internal int DataBits
        {
            set
            {
                Debug.Assert(!(value < MinDataBits || value > MaxDataBits), "An invalid value was passed to DataBits");
                if (value != _dataBits)
                {
                    _dataBits = value;
                    if (Interop.Termios.TermiosReset(_handle, _baudRate, _dataBits, _stopBits, _parity, _handshake) != 0)
                    {
                        throw new ArgumentException();
                    }
                }
            }
        }

        internal Parity Parity
        {
            set
            {
                Debug.Assert(!(value < Parity.None || value > Parity.Space), "An invalid value was passed to Parity");

                if (value != _parity)
                {
                    _parity = value;
                    if (Interop.Termios.TermiosReset(_handle, _baudRate, _dataBits, _stopBits, _parity, _handshake) != 0)
                    {
                        throw new ArgumentException();
                    }
                }
            }
        }

        internal StopBits StopBits
        {
            set
            {
                Debug.Assert(!(value < StopBits.One || value > StopBits.OnePointFive), "An invalid value was passed to StopBits");
                if (value != _stopBits)
                {
                    _stopBits = value;
                    if (Interop.Termios.TermiosReset(_handle, _baudRate, _dataBits, _stopBits, _parity, _handshake) != 0)
                    {
                        throw new ArgumentException();
                    }
                }
            }
        }

        internal bool DiscardNull
        {
            set
            {
                // Ignore.
            }
        }

        internal byte ParityReplace
        {
            set
            {
                // Ignore.
            }
        }

        internal void DiscardInBuffer()
        {
            if (_handle == null) throw new ObjectDisposedException(SR.Port_not_open);
            // This may or may not work depending on hardware.
            Interop.Termios.TermiosDiscard(_handle, Interop.Termios.Queue.ReceiveQueue);
        }

        internal void DiscardOutBuffer()
        {
            if (_handle == null) throw new ObjectDisposedException(SR.Port_not_open);
            // This may or may not work depending on hardware.
            Interop.Termios.TermiosDiscard(_handle, Interop.Termios.Queue.SendQueue);
        }

        internal void SetBufferSizes(int readBufferSize, int writeBufferSize)
        {
            if (_handle == null) throw new ObjectDisposedException(SR.Port_not_open);

            // Ignore for now.
        }

        internal bool IsOpen => _handle != null;


        // Flush dumps the contents of the serial driver's internal read and write buffers.
        // We actually expose the functionality for each, but fulfilling Stream's contract
        // requires a Flush() method.  Fails if handle closed.
        // Note: Serial driver's write buffer is *already* attempting to write it, so we can only wait until it finishes.
        public override void Flush()
        {
            if (_handle == null) throw new ObjectDisposedException(SR.Port_not_open);
            Interop.Termios.TermiosDiscard(_handle, Interop.Termios.Queue.AllQueues);
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

        internal int ReadByte(int timeout)
        {
            Read(_tempBuf, 0, 1, timeout);
            return _tempBuf[0];
        }

        public override int Read(byte[] array, int offset, int count)
        {
            return Read(array, offset, count, ReadTimeout);
        }

        internal unsafe int Read(byte[] array, int offset, int count, int timeout)
        {
            if (_handle == null) throw new ObjectDisposedException(SR.Port_not_open);
            if (array == null)
                throw new ArgumentNullException(nameof(array));
            if (offset < 0)
                throw new ArgumentOutOfRangeException(nameof(offset), SR.ArgumentOutOfRange_NeedNonNegNumRequired);
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count), SR.ArgumentOutOfRange_NeedNonNegNumRequired);
            if (array.Length - offset < count)
                throw new ArgumentException(SR.Argument_InvalidOffLen);
            if (count == 0) return 0; // return immediately if no bytes requested; no need for overhead.

            if (timeout != 0)
            {
                Interop.Sys.PollEvents events = Interop.Sys.PollEvents.POLLNONE;
                Interop.Sys.Poll(_handle, Interop.Sys.PollEvents.POLLIN | Interop.Sys.PollEvents.POLLERR, timeout,out events);

                if ((events & (Interop.Sys.PollEvents.POLLERR | Interop.Sys.PollEvents.POLLNVAL)) != 0)
                {
                    throw new IOException();
                }

                if ( (events & Interop.Sys.PollEvents.POLLIN) == 0)
                {
                    throw new TimeoutException();
                }
            }

            int numBytes;
            fixed (byte* bufPtr = array)
            {
                numBytes = Interop.Sys.Read(_handle, bufPtr + offset, count);
            }

            if (numBytes < 0)
            {
                if (Interop.Error.EWOULDBLOCK == Interop.Sys.GetLastError())
                {
                    throw new TimeoutException();
                }
                throw new IOException();
            }

            return numBytes;
        }

        public override void Write(byte[] array, int offset, int count)
        {
            Write(array, offset, count, WriteTimeout);
        }

        internal unsafe void Write(byte[] array, int offset, int count, int timeout)
        {
            if (_inBreak)
                throw new InvalidOperationException(SR.In_Break_State);
            if (array == null)
                throw new ArgumentNullException(nameof(array));
            if (offset < 0)
                throw new ArgumentOutOfRangeException(nameof(offset), SR.ArgumentOutOfRange_NeedPosNum);
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count), SR.ArgumentOutOfRange_NeedPosNum);
            if (count == 0) return; // no need to expend overhead in creating asyncResult, etc.
            if (array.Length - offset < count)
                throw new ArgumentException(SR.Argument_InvalidOffLen);

            // check for open handle, though the port is always supposed to be open
            if (_handle == null) throw new ObjectDisposedException(SR.Port_not_open);

            int numBytes = 0;

            while (count > 0)
            {
                if (timeout > 0)
                {
                    Interop.Sys.PollEvents events = Interop.Sys.PollEvents.POLLNONE;
                    Interop.Sys.Poll(_handle, Interop.Sys.PollEvents.POLLOUT | Interop.Sys.PollEvents.POLLERR, timeout,out events);

                    if ((events & (Interop.Sys.PollEvents.POLLERR | Interop.Sys.PollEvents.POLLNVAL)) != 0)
                    {
                        throw new IOException();
                    }

                    if ( (events & Interop.Sys.PollEvents.POLLOUT) == 0)
                    {
                        throw new TimeoutException(SR.Write_timed_out);
                    }
                }

                fixed (byte* bufPtr = array)
                {
                    numBytes = Interop.Sys.Write(_handle, bufPtr + offset, count);
                }

                if (numBytes == -1)
                {
                    throw new IOException();
                }

                if (numBytes == 0)
                {
                    throw new TimeoutException(SR.Write_timed_out);
                }
                count -= numBytes;
                offset += numBytes;
            }
        }

        internal SafeFileHandle OpenPort(string portName)
        {
            SafeFileHandle handle = Interop.Serial.SerialPortOpen(portName);

            return handle;
        }

        // this method is used by SerialPort upon SerialStream's creation
        internal SerialStream(string portName, int baudRate, Parity parity, int dataBits, StopBits stopBits, int readTimeout, int writeTimeout, Handshake handshake,
            bool dtrEnable, bool rtsEnable, bool discardNull, byte parityReplace)
        {

            if (portName == null)
            {
                throw new ArgumentException(SR.Arg_InvalidSerialPort, nameof(portName));
            }

            // Error checking done in SerialPort.

            SafeFileHandle tempHandle = OpenPort(portName);

            if (tempHandle.IsInvalid)
            {
                if (Interop.Error.EACCES == Interop.Sys.GetLastError())
                {
                    throw new UnauthorizedAccessException(string.Format(SR.UnauthorizedAccess_IODenied_Port, portName));
                }
                else
                {
                    throw new ArgumentException(SR.Arg_InvalidSerialPort, nameof(portName));
                }
            }

            try
            {
                _handle = tempHandle;
                // set properties of the stream that exist as members in SerialStream
                _portName = portName;
                _handshake = handshake;
                _parity = parity;
                _readTimeout = readTimeout;
                _writeTimeout = writeTimeout;
                _baudRate = baudRate;
                _stopBits = stopBits;
                _dataBits = dataBits;
                _parity = parity;

                if (Interop.Termios.TermiosReset(_handle, _baudRate, _dataBits, _stopBits, _parity, _handshake) != 0)
                {
                    throw new ArgumentException();
                }

                DtrEnable = dtrEnable;
                // query and cache the initial RtsEnable value
                // so that set_RtsEnable can do the (value != rtsEnable) optimization
                //_rtsEnable = (GetDcbFlag(NativeMethods.FRTSCONTROL) == NativeMethods.RTS_CONTROL_ENABLE);
                _rtsEnable = RtsEnable;

                BaudRate = baudRate;

                // now set this.RtsEnable to the specified value.
                // Handshake takes precedence, this will be a nop if
                // handshake is either RequestToSend or RequestToSendXOnXOff
                if ((handshake != Handshake.RequestToSend && handshake != Handshake.RequestToSendXOnXOff))
                {
                    RtsEnable = rtsEnable;
                }
            }
            catch
            {
                // if there are any exceptions after the call to CreateFile, we need to be sure to close the
                // handle before we let them continue up.
                tempHandle.Close();
                _handle = null;
                throw;
            }
        }

        ~SerialStream()
        {
            Dispose(false);
        }

        protected override void Dispose(bool disposing)
        {
            // Signal the other side that we're closing.  Should do regardless of whether we've called
            // Close() or not Dispose()
            if (_handle != null && !_handle.IsInvalid)
            {
                   Interop.Sys.Shutdown(_handle, SocketShutdown.Both);
                   _handle.Close();
                    _handle = null;
                    base.Dispose(disposing);
                    if (PinChanged != null || ErrorReceived != null || DataReceived != null)
                    {
                    }
                    base.Dispose(disposing);
            }
        }
    }
}
