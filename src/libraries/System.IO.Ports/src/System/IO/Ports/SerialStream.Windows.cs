// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;

// Notes about the SerialStream:
//  * The stream is always opened via the SerialStream constructor.
//  * Lifetime of the COM port's handle is controlled via a SafeHandle.  Thus, all properties are available
//  * only when the SerialStream is open and not disposed.
//  * Handles to serial communications resources here always:
//  * 1) own the handle
//  * 2) are opened for asynchronous operation
//  * 3) set access at the level of FileAccess.ReadWrite
//  * 4) Allow for reading AND writing
//  * 5) Disallow seeking, since they encapsulate a file of type FILE_TYPE_CHAR

namespace System.IO.Ports
{
    internal sealed partial class SerialStream : Stream
    {
        private const int ErrorEvents = (int)(SerialError.Frame | SerialError.Overrun |
                                 SerialError.RXOver | SerialError.RXParity | SerialError.TXFull);
        private const int ReceivedEvents = (int)(SerialData.Chars | SerialData.Eof);
        private const int PinChangedEvents = (int)(SerialPinChange.Break | SerialPinChange.CDChanged | SerialPinChange.CtsChanged |
                                      SerialPinChange.Ring | SerialPinChange.DsrChanged);

        private const int infiniteTimeoutConst = -2;

        // called when one character is received.
        internal event SerialDataReceivedEventHandler DataReceived;

        // called when any of the pin/ring-related triggers occurs
        internal event SerialPinChangedEventHandler PinChanged;

        private SafeFileHandle _handle;

        // members supporting properties exposed to SerialPort
        private byte _parityReplace = (byte)'?';
        private readonly bool _isAsync = true;
        private bool _rtsEnable;

        // The internal C# representations of Win32 structures necessary for communication
        // hold most of the internal "fields" maintaining information about the port.
        private Interop.Kernel32.DCB _dcb;
        private Interop.Kernel32.COMMTIMEOUTS _commTimeouts;
        private Interop.Kernel32.COMSTAT _comStat;
        private Interop.Kernel32.COMMPROP _commProp;

        private readonly ThreadPoolBoundHandle _threadPoolBinding;
        private readonly EventLoopRunner _eventRunner;
        private readonly Task _waitForComEventTask;

        private readonly byte[] _tempBuf;                 // used to avoid multiple array allocations in ReadByte()

        // called whenever any async i/o operation completes.
        private static readonly unsafe IOCompletionCallback s_IOCallback = new IOCompletionCallback(AsyncFSCallback);

        // ----- new get-set properties -----------------*

        // Standard port properties, also called from SerialPort
        // BaudRate may not be settable to an arbitrary integer between dwMinBaud and dwMaxBaud,
        // and is limited only by the serial driver.  Typically about twelve values such
        // as Winbase.h's CBR_110 through CBR_256000 are used.
        internal int BaudRate
        {
            set
            {
                if (value <= 0 || (value > _commProp.dwMaxBaud && _commProp.dwMaxBaud > 0))
                {
                    // if no upper bound on baud rate imposed by serial driver, note that argument must be positive
                    if (_commProp.dwMaxBaud == 0)
                    {
                        throw new ArgumentOutOfRangeException(nameof(BaudRate), SR.ArgumentOutOfRange_NeedPosNum);
                    }
                    else
                    {
                        // otherwise, we can present the bounds on the baud rate for this driver
                        throw new ArgumentOutOfRangeException(nameof(BaudRate), SR.Format(SR.ArgumentOutOfRange_Bounds_Lower_Upper, 0, _commProp.dwMaxBaud));
                    }
                }
                // Set only if it's different.  Rollback to previous values if setting fails.
                //  This pattern occurs through most of the other properties in this class.
                if (value != _dcb.BaudRate)
                {
                    int baudRateOld = (int)_dcb.BaudRate;
                    _dcb.BaudRate = (uint)value;

                    if (Interop.Kernel32.SetCommState(_handle, ref _dcb) == false)
                    {
                        _dcb.BaudRate = (uint)baudRateOld;
                        throw Win32Marshal.GetExceptionForLastWin32Error();
                    }
                }
            }
        }

        public bool BreakState
        {
            get { return _inBreak; }
            set
            {
                if (value)
                {
                    if (Interop.Kernel32.SetCommBreak(_handle) == false)
                        throw Win32Marshal.GetExceptionForLastWin32Error();
                    _inBreak = true;
                }
                else
                {
                    if (Interop.Kernel32.ClearCommBreak(_handle) == false)
                        throw Win32Marshal.GetExceptionForLastWin32Error();
                    _inBreak = false;
                }
            }
        }

        internal int DataBits
        {
            set
            {
                Debug.Assert(!(value < MinDataBits || value > MaxDataBits), "An invalid value was passed to DataBits");
                if (value != _dcb.ByteSize)
                {
                    byte byteSizeOld = _dcb.ByteSize;
                    _dcb.ByteSize = (byte)value;

                    if (Interop.Kernel32.SetCommState(_handle, ref _dcb) == false)
                    {
                        _dcb.ByteSize = byteSizeOld;
                        throw Win32Marshal.GetExceptionForLastWin32Error();
                    }
                }
            }
        }

        internal bool DiscardNull
        {
            set
            {
                int fNullFlag = GetDcbFlag(Interop.Kernel32.DCBFlags.FNULL);
                if (value && fNullFlag == 0 || !value && fNullFlag == 1)
                {
                    int fNullOld = fNullFlag;
                    SetDcbFlag(Interop.Kernel32.DCBFlags.FNULL, value ? 1 : 0);

                    if (Interop.Kernel32.SetCommState(_handle, ref _dcb) == false)
                    {
                        SetDcbFlag(Interop.Kernel32.DCBFlags.FNULL, fNullOld);
                        throw Win32Marshal.GetExceptionForLastWin32Error();
                    }
                }
            }
        }

        internal bool DtrEnable
        {
            get
            {
                int fDtrControl = GetDcbFlag(Interop.Kernel32.DCBFlags.FDTRCONTROL);

                return (fDtrControl == Interop.Kernel32.DCBDTRFlowControl.DTR_CONTROL_ENABLE);
            }
            set
            {
                // first set the FDTRCONTROL field in the DCB struct
                int fDtrControlOld = GetDcbFlag(Interop.Kernel32.DCBFlags.FDTRCONTROL);

                SetDcbFlag(Interop.Kernel32.DCBFlags.FDTRCONTROL, value ? Interop.Kernel32.DCBDTRFlowControl.DTR_CONTROL_ENABLE : Interop.Kernel32.DCBDTRFlowControl.DTR_CONTROL_DISABLE);
                if (Interop.Kernel32.SetCommState(_handle, ref _dcb) == false)
                {
                    SetDcbFlag(Interop.Kernel32.DCBFlags.FDTRCONTROL, fDtrControlOld);
                    throw Win32Marshal.GetExceptionForLastWin32Error();
                }

                // then set the actual pin
                if (!Interop.Kernel32.EscapeCommFunction(_handle, value ? Interop.Kernel32.CommFunctions.SETDTR : Interop.Kernel32.CommFunctions.CLRDTR))
                    throw Win32Marshal.GetExceptionForLastWin32Error();
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
                    // in the DCB, handshake affects the fRtsControl, fOutxCtsFlow, and fInX, fOutX fields,
                    // so we must save everything in that closure before making any changes.
                    Handshake handshakeOld = _handshake;
                    int fInOutXOld = GetDcbFlag(Interop.Kernel32.DCBFlags.FINX);
                    int fOutxCtsFlowOld = GetDcbFlag(Interop.Kernel32.DCBFlags.FOUTXCTSFLOW);
                    int fRtsControlOld = GetDcbFlag(Interop.Kernel32.DCBFlags.FRTSCONTROL);

                    _handshake = value;
                    int fInXOutXFlag = (_handshake == Handshake.XOnXOff || _handshake == Handshake.RequestToSendXOnXOff) ? 1 : 0;
                    SetDcbFlag(Interop.Kernel32.DCBFlags.FINX, fInXOutXFlag);
                    SetDcbFlag(Interop.Kernel32.DCBFlags.FOUTX, fInXOutXFlag);

                    SetDcbFlag(Interop.Kernel32.DCBFlags.FOUTXCTSFLOW, (_handshake == Handshake.RequestToSend ||
                        _handshake == Handshake.RequestToSendXOnXOff) ? 1 : 0);

                    if ((_handshake == Handshake.RequestToSend ||
                        _handshake == Handshake.RequestToSendXOnXOff))
                    {
                        SetDcbFlag(Interop.Kernel32.DCBFlags.FRTSCONTROL, Interop.Kernel32.DCBRTSFlowControl.RTS_CONTROL_HANDSHAKE);
                    }
                    else if (_rtsEnable)
                    {
                        SetDcbFlag(Interop.Kernel32.DCBFlags.FRTSCONTROL, Interop.Kernel32.DCBRTSFlowControl.RTS_CONTROL_ENABLE);
                    }
                    else
                    {
                        SetDcbFlag(Interop.Kernel32.DCBFlags.FRTSCONTROL, Interop.Kernel32.DCBRTSFlowControl.RTS_CONTROL_DISABLE);
                    }

                    if (Interop.Kernel32.SetCommState(_handle, ref _dcb) == false)
                    {
                        _handshake = handshakeOld;
                        SetDcbFlag(Interop.Kernel32.DCBFlags.FINX, fInOutXOld);
                        SetDcbFlag(Interop.Kernel32.DCBFlags.FOUTX, fInOutXOld);
                        SetDcbFlag(Interop.Kernel32.DCBFlags.FOUTXCTSFLOW, fOutxCtsFlowOld);
                        SetDcbFlag(Interop.Kernel32.DCBFlags.FRTSCONTROL, fRtsControlOld);
                        throw Win32Marshal.GetExceptionForLastWin32Error();
                    }

                }
            }
        }

        internal bool IsOpen => _handle != null && !_eventRunner.ShutdownLoop;

        internal Parity Parity
        {
            set
            {
                Debug.Assert(!(value < Parity.None || value > Parity.Space), "An invalid value was passed to Parity");

                if ((byte)value != _dcb.Parity)
                {
                    byte parityOld = _dcb.Parity;

                    // in the DCB structure, the parity setting also potentially effects:
                    // fParity, fErrorChar, ErrorChar
                    // so these must be saved as well.
                    int fParityOld = GetDcbFlag(Interop.Kernel32.DCBFlags.FPARITY);
                    byte ErrorCharOld = _dcb.ErrorChar;
                    int fErrorCharOld = GetDcbFlag(Interop.Kernel32.DCBFlags.FERRORCHAR);
                    _dcb.Parity = (byte)value;

                    int parityFlag = (_dcb.Parity == (byte)Parity.None) ? 0 : 1;
                    SetDcbFlag(Interop.Kernel32.DCBFlags.FPARITY, parityFlag);
                    if (parityFlag == 1)
                    {
                        SetDcbFlag(Interop.Kernel32.DCBFlags.FERRORCHAR, (_parityReplace != '\0') ? 1 : 0);
                        _dcb.ErrorChar = _parityReplace;
                    }
                    else
                    {
                        SetDcbFlag(Interop.Kernel32.DCBFlags.FERRORCHAR, 0);
                        _dcb.ErrorChar = (byte)'\0';
                    }
                    if (Interop.Kernel32.SetCommState(_handle, ref _dcb) == false)
                    {
                        _dcb.Parity = parityOld;
                        SetDcbFlag(Interop.Kernel32.DCBFlags.FPARITY, fParityOld);

                        _dcb.ErrorChar = ErrorCharOld;
                        SetDcbFlag(Interop.Kernel32.DCBFlags.FERRORCHAR, fErrorCharOld);

                        throw Win32Marshal.GetExceptionForLastWin32Error();
                    }
                }
            }
        }

        // ParityReplace is the eight-bit character which replaces any bytes which
        // ParityReplace affects the equivalent field in the DCB structure: ErrorChar, and
        // the DCB flag fErrorChar.
        internal byte ParityReplace
        {
            set
            {
                if (value != _parityReplace)
                {
                    byte parityReplaceOld = _parityReplace;
                    byte errorCharOld = _dcb.ErrorChar;
                    int fErrorCharOld = GetDcbFlag(Interop.Kernel32.DCBFlags.FERRORCHAR);

                    _parityReplace = value;
                    if (GetDcbFlag(Interop.Kernel32.DCBFlags.FPARITY) == 1)
                    {
                        SetDcbFlag(Interop.Kernel32.DCBFlags.FERRORCHAR, (_parityReplace != '\0') ? 1 : 0);
                        _dcb.ErrorChar = _parityReplace;
                    }
                    else
                    {
                        SetDcbFlag(Interop.Kernel32.DCBFlags.FERRORCHAR, 0);
                        _dcb.ErrorChar = (byte)'\0';
                    }

                    if (Interop.Kernel32.SetCommState(_handle, ref _dcb) == false)
                    {
                        _parityReplace = parityReplaceOld;
                        SetDcbFlag(Interop.Kernel32.DCBFlags.FERRORCHAR, fErrorCharOld);
                        _dcb.ErrorChar = errorCharOld;
                        throw Win32Marshal.GetExceptionForLastWin32Error();
                    }
                }
            }
        }

        // Timeouts are considered to be TOTAL time for the Read/Write operation and to be in milliseconds.
        // Timeouts are translated into DCB structure as follows:
        //
        //  Desired timeout      =>  ReadTotalTimeoutConstant    ReadTotalTimeoutMultiplier  ReadIntervalTimeout
        //   0                                   0                           0               MAXDWORD
        //   0 < n < infinity                    n                       MAXDWORD            MAXDWORD
        //  infinity                             infiniteTimeoutConst    MAXDWORD            MAXDWORD
        //
        // rationale for "infinity": There does not exist in the COMMTIMEOUTS structure a way to
        // *wait indefinitely for any byte, return when found*.  Instead, if we set ReadTimeout
        // to infinity, SerialStream's EndRead loops if infiniteTimeoutConst mills have elapsed
        // without a byte received.  Note that this is approximately 24 days, so essentially
        // most practical purposes effectively equate 24 days with an infinite amount of time
        // on a serial port connection.
        public override int ReadTimeout
        {
            get
            {
                int constant = _commTimeouts.ReadTotalTimeoutConstant;

                if (constant == infiniteTimeoutConst) return SerialPort.InfiniteTimeout;
                else return constant;
            }
            set
            {
                if (value < 0 && value != SerialPort.InfiniteTimeout)
                    throw new ArgumentOutOfRangeException(nameof(ReadTimeout), SR.ArgumentOutOfRange_Timeout);
                if (_handle == null) InternalResources.FileNotOpen();

                int oldReadConstant = _commTimeouts.ReadTotalTimeoutConstant;
                int oldReadInterval = _commTimeouts.ReadIntervalTimeout;
                int oldReadMultiplier = _commTimeouts.ReadTotalTimeoutMultiplier;

                // NOTE: this logic should match what is in the constructor
                if (value == 0)
                {
                    _commTimeouts.ReadTotalTimeoutConstant = 0;
                    _commTimeouts.ReadTotalTimeoutMultiplier = 0;
                    _commTimeouts.ReadIntervalTimeout = Interop.Kernel32.MAXDWORD;
                }
                else if (value == SerialPort.InfiniteTimeout)
                {
                    // SetCommTimeouts doesn't like a value of -1 for some reason, so
                    // we'll use -2(infiniteTimeoutConst) to represent infinite.
                    _commTimeouts.ReadTotalTimeoutConstant = infiniteTimeoutConst;
                    _commTimeouts.ReadTotalTimeoutMultiplier = Interop.Kernel32.MAXDWORD;
                    _commTimeouts.ReadIntervalTimeout = Interop.Kernel32.MAXDWORD;
                }
                else
                {
                    _commTimeouts.ReadTotalTimeoutConstant = value;
                    _commTimeouts.ReadTotalTimeoutMultiplier = Interop.Kernel32.MAXDWORD;
                    _commTimeouts.ReadIntervalTimeout = Interop.Kernel32.MAXDWORD;
                }

                if (Interop.Kernel32.SetCommTimeouts(_handle, ref _commTimeouts) == false)
                {
                    _commTimeouts.ReadTotalTimeoutConstant = oldReadConstant;
                    _commTimeouts.ReadTotalTimeoutMultiplier = oldReadMultiplier;
                    _commTimeouts.ReadIntervalTimeout = oldReadInterval;
                    throw Win32Marshal.GetExceptionForLastWin32Error();
                }
            }
        }

        internal bool RtsEnable
        {
            get
            {
                int fRtsControl = GetDcbFlag(Interop.Kernel32.DCBFlags.FRTSCONTROL);
                if (fRtsControl == Interop.Kernel32.DCBRTSFlowControl.RTS_CONTROL_HANDSHAKE)
                    throw new InvalidOperationException(SR.CantSetRtsWithHandshaking);

                return (fRtsControl == Interop.Kernel32.DCBRTSFlowControl.RTS_CONTROL_ENABLE);
            }
            set
            {
                if ((_handshake == Handshake.RequestToSend || _handshake == Handshake.RequestToSendXOnXOff))
                    throw new InvalidOperationException(SR.CantSetRtsWithHandshaking);

                if (value != _rtsEnable)
                {
                    int fRtsControlOld = GetDcbFlag(Interop.Kernel32.DCBFlags.FRTSCONTROL);

                    _rtsEnable = value;
                    if (value)
                        SetDcbFlag(Interop.Kernel32.DCBFlags.FRTSCONTROL, Interop.Kernel32.DCBRTSFlowControl.RTS_CONTROL_ENABLE);
                    else
                        SetDcbFlag(Interop.Kernel32.DCBFlags.FRTSCONTROL, Interop.Kernel32.DCBRTSFlowControl.RTS_CONTROL_DISABLE);

                    if (Interop.Kernel32.SetCommState(_handle, ref _dcb) == false)
                    {
                        SetDcbFlag(Interop.Kernel32.DCBFlags.FRTSCONTROL, fRtsControlOld);
                        // set it back to the old value on a failure
                        _rtsEnable = !_rtsEnable;
                        throw Win32Marshal.GetExceptionForLastWin32Error();
                    }

                    if (!Interop.Kernel32.EscapeCommFunction(_handle, value ? Interop.Kernel32.CommFunctions.SETRTS : Interop.Kernel32.CommFunctions.CLRRTS))
                        throw Win32Marshal.GetExceptionForLastWin32Error();
                }
            }
        }

        // StopBits represented in C# as StopBits enum type and in Win32 as an integer 1, 2, or 3.
        internal StopBits StopBits
        {
            set
            {
                Debug.Assert(!(value < StopBits.One || value > StopBits.OnePointFive), "An invalid value was passed to StopBits");

                byte nativeValue;
                if (value == StopBits.One)
                {
                    nativeValue = Interop.Kernel32.DCBStopBits.ONESTOPBIT;
                }
                else if (value == StopBits.OnePointFive)
                {
                    nativeValue = Interop.Kernel32.DCBStopBits.ONE5STOPBITS;
                }
                else
                {
                    nativeValue = Interop.Kernel32.DCBStopBits.TWOSTOPBITS;
                }

                if (nativeValue != _dcb.StopBits)
                {
                    byte stopBitsOld = _dcb.StopBits;
                    _dcb.StopBits = nativeValue;

                    if (Interop.Kernel32.SetCommState(_handle, ref _dcb) == false)
                    {
                        _dcb.StopBits = stopBitsOld;
                        throw Win32Marshal.GetExceptionForLastWin32Error();
                    }
                }
            }
        }

        // note: WriteTimeout must be either SerialPort.InfiniteTimeout or POSITIVE.
        // a timeout of zero implies that every Write call throws an exception.
        public override int WriteTimeout
        {
            get
            {
                int timeout = _commTimeouts.WriteTotalTimeoutConstant;
                return (timeout == 0) ? SerialPort.InfiniteTimeout : timeout;
            }
            set
            {
                if (value <= 0 && value != SerialPort.InfiniteTimeout)
                    throw new ArgumentOutOfRangeException(nameof(WriteTimeout), SR.ArgumentOutOfRange_WriteTimeout);
                if (_handle == null) InternalResources.FileNotOpen();

                int oldWriteConstant = _commTimeouts.WriteTotalTimeoutConstant;
                _commTimeouts.WriteTotalTimeoutConstant = ((value == SerialPort.InfiniteTimeout) ? 0 : value);

                if (Interop.Kernel32.SetCommTimeouts(_handle, ref _commTimeouts) == false)
                {
                    _commTimeouts.WriteTotalTimeoutConstant = oldWriteConstant;
                    throw Win32Marshal.GetExceptionForLastWin32Error();
                }
            }
        }

        // CDHolding, CtsHolding, DsrHolding query the current state of each of the carrier, the CTS pin,
        // and the DSR pin, respectively. Read-only.
        // All will throw exceptions if the port is not open.
        internal bool CDHolding
        {
            get
            {
                int pinStatus = 0;
                if (Interop.Kernel32.GetCommModemStatus(_handle, ref pinStatus) == false)
                    throw Win32Marshal.GetExceptionForLastWin32Error();

                return (Interop.Kernel32.CommModemState.MS_RLSD_ON & pinStatus) != 0;
            }
        }

        internal bool CtsHolding
        {
            get
            {
                int pinStatus = 0;
                if (Interop.Kernel32.GetCommModemStatus(_handle, ref pinStatus) == false)
                    throw Win32Marshal.GetExceptionForLastWin32Error();
                return (Interop.Kernel32.CommModemState.MS_CTS_ON & pinStatus) != 0;
            }

        }

        internal bool DsrHolding
        {
            get
            {
                int pinStatus = 0;
                if (Interop.Kernel32.GetCommModemStatus(_handle, ref pinStatus) == false)
                    throw Win32Marshal.GetExceptionForLastWin32Error();

                return (Interop.Kernel32.CommModemState.MS_DSR_ON & pinStatus) != 0;
            }
        }


        // Fills comStat structure from an unmanaged function
        // to determine the number of bytes waiting in the serial driver's internal receive buffer.
        internal int BytesToRead
        {
            get
            {
                int errorCode = 0; // "ref" arguments need to have values, as opposed to "out" arguments
                if (Interop.Kernel32.ClearCommError(_handle, ref errorCode, ref _comStat) == false)
                {
                    throw Win32Marshal.GetExceptionForLastWin32Error();
                }
                return (int)_comStat.cbInQue;
            }
        }

        // Fills comStat structure from an unmanaged function
        // to determine the number of bytes waiting in the serial driver's internal transmit buffer.
        internal int BytesToWrite
        {
            get
            {
                int errorCode = 0; // "ref" arguments need to be set before method invocation, as opposed to "out" arguments
                if (Interop.Kernel32.ClearCommError(_handle, ref errorCode, ref _comStat) == false)
                    throw Win32Marshal.GetExceptionForLastWin32Error();
                return (int)_comStat.cbOutQue;

            }
        }

        // -----------SECTION: constructor --------------------------*

        // this method is used by SerialPort upon SerialStream's creation
        internal SerialStream(string portName, int baudRate, Parity parity, int dataBits, StopBits stopBits, int readTimeout, int writeTimeout, Handshake handshake,
            bool dtrEnable, bool rtsEnable, bool discardNull, byte parityReplace)
        {
            ArgumentNullException.ThrowIfNull(portName);

            if (!portName.StartsWith("COM", StringComparison.OrdinalIgnoreCase) ||
                !uint.TryParse(
#if NETCOREAPP
                    portName.AsSpan(3),
#else
                    portName.Substring(3),
#endif
                    out uint portNumber))
            {
                throw new ArgumentException(SR.Format(SR.Arg_InvalidSerialPort, portName), nameof(portName));
            }

            // Error checking done in SerialPort.

            SafeFileHandle tempHandle = OpenPort(portNumber);

            if (tempHandle.IsInvalid)
            {
                Exception e = Win32Marshal.GetExceptionForLastWin32Error(portName);
                tempHandle.Dispose();
                throw e;
            }

            try
            {
                int fileType = Interop.Kernel32.GetFileType(tempHandle);

                // Allowing FILE_TYPE_UNKNOWN for legitimate serial device such as USB to serial adapter device
                if ((fileType != Interop.Kernel32.FileTypes.FILE_TYPE_CHAR) && (fileType != Interop.Kernel32.FileTypes.FILE_TYPE_UNKNOWN))
                    throw new ArgumentException(SR.Format(SR.Arg_InvalidSerialPort, portName), nameof(portName));

                _handle = tempHandle;

                // set properties of the stream that exist as members in SerialStream
                _portName = portName;
                _handshake = handshake;
                _parityReplace = parityReplace;

                _tempBuf = new byte[1];          // used in ReadByte()

                // Fill COMMPROPERTIES struct, which has our maximum allowed baud rate.
                // Call a serial specific API such as GetCommModemStatus which would fail
                // in case the device is not a legitimate serial device. For instance,
                // some illegal FILE_TYPE_UNKNOWN device (or) "LPT1" on Win9x
                // trying to pass for serial will be caught here. GetCommProperties works
                // fine for "LPT1" on Win9x, so that alone can't be relied here to
                // detect non serial devices.

                _commProp = default;
                int pinStatus = 0;

                if (!Interop.Kernel32.GetCommProperties(_handle, ref _commProp)
                    || !Interop.Kernel32.GetCommModemStatus(_handle, ref pinStatus))
                {
                    // If the portName they have passed in is a FILE_TYPE_CHAR but not a serial port,
                    // for example "LPT1", this API will fail.  For this reason we handle the error message specially.
                    int errorCode = Marshal.GetLastPInvokeError();
                    if ((errorCode == Interop.Errors.ERROR_INVALID_PARAMETER) || (errorCode == Interop.Errors.ERROR_INVALID_HANDLE))
                        throw new ArgumentException(SR.Arg_InvalidSerialPortExtended, nameof(portName));
                    else
                        throw Win32Marshal.GetExceptionForWin32Error(errorCode, string.Empty);
                }

                if (_commProp.dwMaxBaud != 0 && baudRate > _commProp.dwMaxBaud)
                    throw new ArgumentOutOfRangeException(nameof(baudRate), SR.Format(SR.Max_Baud, _commProp.dwMaxBaud));

                _comStat = default;
                // create internal DCB structure, initialize according to Platform SDK
                // standard: ms-help://MS.MSNDNQTR.2002APR.1003/hardware/commun_965u.htm
                _dcb = default;

                // set constant properties of the DCB
                InitializeDCB(baudRate, parity, dataBits, stopBits, discardNull);

                DtrEnable = dtrEnable;

                // query and cache the initial RtsEnable value
                // so that set_RtsEnable can do the (value != rtsEnable) optimization
                _rtsEnable = (GetDcbFlag(Interop.Kernel32.DCBFlags.FRTSCONTROL) == Interop.Kernel32.DCBRTSFlowControl.RTS_CONTROL_ENABLE);

                // now set this.RtsEnable to the specified value.
                // Handshake takes precedence, this will be a nop if
                // handshake is either RequestToSend or RequestToSendXOnXOff
                if ((handshake != Handshake.RequestToSend && handshake != Handshake.RequestToSendXOnXOff))
                    RtsEnable = rtsEnable;

                // NOTE: this logic should match what is in the ReadTimeout property
                if (readTimeout == 0)
                {
                    _commTimeouts.ReadTotalTimeoutConstant = 0;
                    _commTimeouts.ReadTotalTimeoutMultiplier = 0;
                    _commTimeouts.ReadIntervalTimeout = Interop.Kernel32.MAXDWORD;
                }
                else if (readTimeout == SerialPort.InfiniteTimeout)
                {
                    // SetCommTimeouts doesn't like a value of -1 for some reason, so
                    // we'll use -2(infiniteTimeoutConst) to represent infinite.
                    _commTimeouts.ReadTotalTimeoutConstant = infiniteTimeoutConst;
                    _commTimeouts.ReadTotalTimeoutMultiplier = Interop.Kernel32.MAXDWORD;
                    _commTimeouts.ReadIntervalTimeout = Interop.Kernel32.MAXDWORD;
                }
                else
                {
                    _commTimeouts.ReadTotalTimeoutConstant = readTimeout;
                    _commTimeouts.ReadTotalTimeoutMultiplier = Interop.Kernel32.MAXDWORD;
                    _commTimeouts.ReadIntervalTimeout = Interop.Kernel32.MAXDWORD;
                }

                _commTimeouts.WriteTotalTimeoutMultiplier = 0;
                _commTimeouts.WriteTotalTimeoutConstant = ((writeTimeout == SerialPort.InfiniteTimeout) ? 0 : writeTimeout);

                // set unmanaged timeout structure
                if (Interop.Kernel32.SetCommTimeouts(_handle, ref _commTimeouts) == false)
                {
                    throw Win32Marshal.GetExceptionForLastWin32Error();
                }

                if (_isAsync)
                {
                    _threadPoolBinding = ThreadPoolBoundHandle.BindHandle(_handle);
                }

                // monitor all events except TXEMPTY
                Interop.Kernel32.SetCommMask(_handle, Interop.Kernel32.CommEvents.ALL_EVENTS);

                // prep. for starting event cycle.
                _eventRunner = new EventLoopRunner(this);
                _waitForComEventTask = Task.Factory.StartNew(s => ((EventLoopRunner)s).WaitForCommEvent(), _eventRunner, CancellationToken.None,
                                                                   TaskCreationOptions.DenyChildAttach | TaskCreationOptions.LongRunning, TaskScheduler.Default);
            }
            catch
            {
                // if there are any exceptions after the call to CreateFile, we need to be sure to close the
                // handle before we let them continue up.
                tempHandle.Close();
                _handle = null;
                _threadPoolBinding?.Dispose();
                throw;
            }
        }

        protected override void Dispose(bool disposing)
        {
            // Signal the other side that we're closing.  Should do regardless of whether we've called
            // Close() or not Dispose()
            if (_handle != null && !_handle.IsInvalid)
            {
                try
                {
                    _eventRunner.endEventLoop = true;

                    Thread.MemoryBarrier();

                    bool skipSPAccess = false;

                    // turn off all events and signal WaitCommEvent
                    Interop.Kernel32.SetCommMask(_handle, 0);
                    if (!Interop.Kernel32.EscapeCommFunction(_handle, Interop.Kernel32.CommFunctions.CLRDTR))
                    {
                        int hr = Marshal.GetLastPInvokeError();

                        // access denied can happen if USB is yanked out. If that happens, we
                        // want to at least allow finalize to succeed and clean up everything
                        // we can. To achieve this, we need to avoid further attempts to access
                        // the SerialPort.  A customer also reported seeing ERROR_BAD_COMMAND here.
                        // Do not throw an exception on the finalizer thread - that's just rude,
                        // since apps can't catch it and we may tear down the app.
                        const int ERROR_DEVICE_REMOVED = 1617;
                        if ((hr == Interop.Errors.ERROR_ACCESS_DENIED || hr == Interop.Errors.ERROR_BAD_COMMAND || hr == ERROR_DEVICE_REMOVED) && !disposing)
                        {
                            skipSPAccess = true;
                        }
                        else
                        {
                            // should not happen
                            Debug.Fail($"Unexpected error code from EscapeCommFunction in SerialPort.Dispose(bool)  Error code: 0x{(uint)hr:x}");

                            // Do not throw an exception from the finalizer here.
                            if (disposing)
                                throw Win32Marshal.GetExceptionForLastWin32Error();
                        }
                    }

                    if (!skipSPAccess && !_handle.IsClosed)
                    {
                        Flush();
                    }

                    _eventRunner.waitCommEventWaitHandle.Set();

                    if (!skipSPAccess)
                    {
                        DiscardInBuffer();
                        DiscardOutBuffer();
                    }

                    if (disposing && _eventRunner != null && _waitForComEventTask != null)
                    {
                        _waitForComEventTask.GetAwaiter().GetResult();
                        _eventRunner.waitCommEventWaitHandle.Close();
                    }
                }
                finally
                {
                    // If we are disposing synchronize closing with raising SerialPort events
                    if (disposing)
                    {
                        lock (this)
                        {
                            _handle.Close();
                            _handle = null;
                            _threadPoolBinding.Dispose();
                        }
                    }
                    else
                    {
                        _handle.Close();
                        _handle = null;
                        _threadPoolBinding.Dispose();
                    }
                    base.Dispose(disposing);
                }
            }
        }

        // -----SECTION: all public methods ------------------*

        // User-accessible async read method.  Returns SerialStreamAsyncResult : IAsyncResult
        public override IAsyncResult BeginRead(byte[] array, int offset, int numBytes, AsyncCallback userCallback, object stateObject)
        {
            CheckReadWriteArguments(array, offset, numBytes);

            int oldtimeout = ReadTimeout;
            ReadTimeout = SerialPort.InfiniteTimeout;
            IAsyncResult result;
            try
            {
                if (!_isAsync)
                    result = base.BeginRead(array, offset, numBytes, userCallback, stateObject);
                else
                    result = BeginReadCore(array, offset, numBytes, userCallback, stateObject);
            }
            finally
            {
                ReadTimeout = oldtimeout;
            }

            return result;
        }

        // User-accessible async write method.  Returns SerialStreamAsyncResult : IAsyncResult
        // Throws an exception if port is in break state.
        public override IAsyncResult BeginWrite(byte[] array, int offset, int numBytes,
            AsyncCallback userCallback, object stateObject)
        {
            CheckWriteArguments(array, offset, numBytes);

            int oldtimeout = WriteTimeout;
            WriteTimeout = SerialPort.InfiniteTimeout;
            IAsyncResult result;
            try
            {
                if (!_isAsync)
                    result = base.BeginWrite(array, offset, numBytes, userCallback, stateObject);
                else
                    result = BeginWriteCore(array, offset, numBytes, userCallback, stateObject);
            }
            finally
            {
                WriteTimeout = oldtimeout;
            }

            return result;
        }

        // Uses Win32 method to dump out the receive buffer; analogous to MSComm's "InBufferCount = 0"
        internal void DiscardInBuffer()
        {

            if (Interop.Kernel32.PurgeComm(_handle, Interop.Kernel32.PurgeFlags.PURGE_RXCLEAR | Interop.Kernel32.PurgeFlags.PURGE_RXABORT) == false)
                throw Win32Marshal.GetExceptionForLastWin32Error();
        }

        // Uses Win32 method to dump out the xmit buffer; analogous to MSComm's "OutBufferCount = 0"
        internal void DiscardOutBuffer()
        {
            if (Interop.Kernel32.PurgeComm(_handle, Interop.Kernel32.PurgeFlags.PURGE_TXCLEAR | Interop.Kernel32.PurgeFlags.PURGE_TXABORT) == false)
                throw Win32Marshal.GetExceptionForLastWin32Error();
        }

        // Async companion to BeginRead.
        // Note, assumed IAsyncResult argument is of derived type SerialStreamAsyncResult,
        // and throws an exception if untrue.
        public override unsafe int EndRead(IAsyncResult asyncResult)
        {
            if (!_isAsync)
                return base.EndRead(asyncResult);

            if (asyncResult == null)
                throw new ArgumentNullException(nameof(asyncResult));

            SerialStreamAsyncResult afsar = asyncResult as SerialStreamAsyncResult;
            if (afsar == null || afsar._isWrite)
                InternalResources.WrongAsyncResult();

            // This sidesteps race conditions, avoids memory corruption after freeing the
            // NativeOverlapped class or GCHandle twice.
            if (1 == Interlocked.CompareExchange(ref afsar._EndXxxCalled, 1, 0))
                InternalResources.EndReadCalledTwice();

            bool failed = false;

            // Obtain the WaitHandle, but don't use public property in case we
            // delay initialize the manual reset event in the future.
            ManualResetEvent wh = afsar._waitHandle;
            if (wh != null)
            {
                // We must block to ensure that AsyncFSCallback has completed,
                // and we should close the WaitHandle in here.
                try
                {
                    wh.WaitOne();
                    Debug.Assert(afsar._isComplete, "SerialStream::EndRead - AsyncFSCallback didn't set _isComplete to true!");

                    // InfiniteTimeout is not something native to the underlying serial device,
                    // we specify the timeout to be a very large value (MAXWORD-1) to achieve
                    // an infinite timeout illusion.

                    // I'm not sure what we can do here after an asyn operation with infinite
                    // timeout returns with no data. From a purist point of view we should
                    // somehow restart the read operation but we are not in a position to do so
                    // (and frankly that may not necessarily be the right thing to do here)
                    // I think the best option in this (almost impossible to run into) situation
                    // is to throw some sort of IOException.

                    if ((afsar._numBytes == 0) && (ReadTimeout == SerialPort.InfiniteTimeout) && (afsar._errorCode == 0))
                        failed = true;
                }
                finally
                {
                    wh.Close();
                }
            }

            // Free memory, GC handles.
            NativeOverlapped* overlappedPtr = afsar._overlapped;
            if (overlappedPtr != null)
            {
                // Legacy behavior as indicated by tests (e.g.: System.IO.Ports.Tests.SerialStream_EndRead.EndReadAfterClose)
                // expects to be able to call EndRead after Close/Dispose - even if disposed _threadPoolBinding can free the
                // native overlapped.
                _threadPoolBinding.FreeNativeOverlapped(overlappedPtr);
            }

            // Check for non-timeout errors during the read.
            if (afsar._errorCode != 0)
                throw Win32Marshal.GetExceptionForWin32Error(afsar._errorCode, _portName);

            if (failed)
                throw new IOException(SR.IO_OperationAborted);

            return afsar._numBytes;
        }

        // Async companion to BeginWrite.
        // Note, assumed IAsyncResult argument is of derived type SerialStreamAsyncResult,
        // and throws an exception if untrue.
        // Also fails if called in port's break state.
        public override unsafe void EndWrite(IAsyncResult asyncResult)
        {
            if (!_isAsync)
            {
                base.EndWrite(asyncResult);
                return;
            }

            if (_inBreak)
                throw new InvalidOperationException(SR.In_Break_State);
            if (asyncResult == null)
                throw new ArgumentNullException(nameof(asyncResult));

            SerialStreamAsyncResult afsar = asyncResult as SerialStreamAsyncResult;
            if (afsar == null || !afsar._isWrite)
                InternalResources.WrongAsyncResult();

            // This sidesteps race conditions, avoids memory corruption after freeing the
            // NativeOverlapped class or GCHandle twice.
            if (1 == Interlocked.CompareExchange(ref afsar._EndXxxCalled, 1, 0))
                InternalResources.EndWriteCalledTwice();

            // Obtain the WaitHandle, but don't use public property in case we
            // delay initialize the manual reset event in the future.
            ManualResetEvent wh = afsar._waitHandle;
            if (wh != null)
            {
                // We must block to ensure that AsyncFSCallback has completed,
                // and we should close the WaitHandle in here.
                try
                {
                    wh.WaitOne();
                    Debug.Assert(afsar._isComplete, "SerialStream::EndWrite - AsyncFSCallback didn't set _isComplete to true!");
                }
                finally
                {
                    wh.Close();
                }
            }

            // Free memory, GC handles.
            NativeOverlapped* overlappedPtr = afsar._overlapped;
            if (overlappedPtr != null)
            {
                // Legacy behavior as indicated by tests (e.g.: System.IO.Ports.Tests.SerialStream_EndWrite.EndWriteAfterSerialStreamClose)
                // expects to be able to call EndWrite after Close/Dispose - even if disposed _threadPoolBinding can free the
                // native overlapped.
                _threadPoolBinding.FreeNativeOverlapped(overlappedPtr);
            }

            // Now check for any error during the write.
            if (afsar._errorCode != 0)
                throw Win32Marshal.GetExceptionForWin32Error(afsar._errorCode, _portName);

            // Number of bytes written is afsar._numBytes.
        }

        // Flush dumps the contents of the serial driver's internal read and write buffers.
        // We actually expose the functionality for each, but fulfilling Stream's contract
        // requires a Flush() method.  Fails if handle closed.
        // Note: Serial driver's write buffer is *already* attempting to write it, so we can only wait until it finishes.
        public override void Flush()
        {
            if (_handle == null) throw new ObjectDisposedException(SR.Port_not_open);
            Interop.Kernel32.FlushFileBuffers(_handle);
        }

        // Blocking read operation, returning the number of bytes read from the stream.

        public override int Read(byte[] array, int offset, int count)
        {
            return Read(array, offset, count, ReadTimeout);
        }

        internal unsafe int Read(byte[] array, int offset, int count, int timeout)
        {
            CheckReadWriteArguments(array, offset, count);

            if (count == 0) return 0; // return immediately if no bytes requested; no need for overhead.

            Debug.Assert(timeout == SerialPort.InfiniteTimeout || timeout >= 0, $"Serial Stream Read - called with timeout {timeout}");

            int numBytes = 0;
            if (_isAsync)
            {
                IAsyncResult result = BeginReadCore(array, offset, count, null, null);
                numBytes = EndRead(result);
            }
            else
            {
                numBytes = ReadFileNative(array, offset, count, null, out _);
                if (numBytes == -1)
                {
                    throw Win32Marshal.GetExceptionForLastWin32Error();
                }
            }

            if (numBytes == 0)
                throw new TimeoutException();

            return numBytes;
        }

        internal unsafe int ReadByte(int _/*timeout*/)
        {
            if (_handle == null) InternalResources.FileNotOpen();

            int numBytes = 0;
            if (_isAsync)
            {
                IAsyncResult result = BeginReadCore(_tempBuf, 0, 1, null, null);
                numBytes = EndRead(result);
            }
            else
            {
                numBytes = ReadFileNative(_tempBuf, 0, 1, null, out _);
                if (numBytes == -1)
                {
                    throw Win32Marshal.GetExceptionForLastWin32Error();
                }
            }

            if (numBytes == 0)
                throw new TimeoutException();
            else
                return _tempBuf[0];
        }

        internal void SetBufferSizes(int readBufferSize, int writeBufferSize)
        {
            if (_handle == null) InternalResources.FileNotOpen();

            if (!Interop.Kernel32.SetupComm(_handle, readBufferSize, writeBufferSize))
                throw Win32Marshal.GetExceptionForLastWin32Error();
        }

        internal unsafe void Write(byte[] array, int offset, int count, int timeout)
        {
            CheckWriteArguments(array, offset, count);

            if (count == 0) return; // no need to expend overhead in creating asyncResult, etc.

            Debug.Assert(timeout == SerialPort.InfiniteTimeout || timeout >= 0, $"Serial Stream Write - write timeout is {timeout}");

            int numBytes;
            if (_isAsync)
            {
                IAsyncResult result = BeginWriteCore(array, offset, count, null, null);
                EndWrite(result);

                SerialStreamAsyncResult afsar = result as SerialStreamAsyncResult;
                Debug.Assert(afsar != null, "afsar should be a SerialStreamAsyncResult and should not be null");
                numBytes = afsar._numBytes;
            }
            else
            {
                int hr;
                numBytes = WriteFileNative(array, offset, count, null, out hr);
                if (numBytes == -1)
                {
                    // This is how writes timeout on Win9x.
                    if (hr == Interop.Errors.ERROR_COUNTER_TIMEOUT)
                        throw new TimeoutException(SR.Write_timed_out);

                    throw Win32Marshal.GetExceptionForLastWin32Error();
                }
            }

            if (numBytes == 0)
                throw new TimeoutException(SR.Write_timed_out);
        }

        // use default timeout as argument to WriteByte override with timeout arg
        public override unsafe void WriteByte(byte value)
        {
            if (_inBreak)
                throw new InvalidOperationException(SR.In_Break_State);

            if (_handle == null) InternalResources.FileNotOpen();
            _tempBuf[0] = value;


            int numBytes;
            if (_isAsync)
            {
                IAsyncResult result = BeginWriteCore(_tempBuf, 0, 1, null, null);
                EndWrite(result);

                SerialStreamAsyncResult afsar = result as SerialStreamAsyncResult;
                Debug.Assert(afsar != null, "afsar should be a SerialStreamAsyncResult and should not be null");
                numBytes = afsar._numBytes;
            }
            else
            {
                numBytes = WriteFileNative(_tempBuf, 0, 1, null, out _);
                if (numBytes == -1)
                {
                    // This is how writes timeout on Win9x.
                    if (Marshal.GetLastPInvokeError() == Interop.Errors.ERROR_COUNTER_TIMEOUT)
                        throw new TimeoutException(SR.Write_timed_out);

                    throw Win32Marshal.GetExceptionForLastWin32Error();
                }
            }

            if (numBytes == 0)
                throw new TimeoutException(SR.Write_timed_out);

            return;
        }

        // --------SUBSECTION: internal-use methods ----------------------*
        // ------ internal DCB-supporting methods ------- *

        // Initializes unmananged DCB struct, to be called after opening communications resource.
        // assumes we have already: baudRate, parity, dataBits, stopBits
        // should only be called in SerialStream(...)
        private unsafe void InitializeDCB(int baudRate, Parity parity, int dataBits, StopBits stopBits, bool discardNull)
        {
            // first get the current dcb structure setup
            if (Interop.Kernel32.GetCommState(_handle, ref _dcb) == false)
            {
                throw Win32Marshal.GetExceptionForLastWin32Error();
            }
            _dcb.DCBlength = (uint)sizeof(Interop.Kernel32.DCB);

            // set parameterized properties
            _dcb.BaudRate = (uint)baudRate;
            _dcb.ByteSize = (byte)dataBits;


            switch (stopBits)
            {
                case StopBits.One:
                    _dcb.StopBits = Interop.Kernel32.DCBStopBits.ONESTOPBIT;
                    break;
                case StopBits.OnePointFive:
                    _dcb.StopBits = Interop.Kernel32.DCBStopBits.ONE5STOPBITS;
                    break;
                case StopBits.Two:
                    _dcb.StopBits = Interop.Kernel32.DCBStopBits.TWOSTOPBITS;
                    break;
                default:
                    Debug.Fail("Invalid value for stopBits");
                    break;
            }

            _dcb.Parity = (byte)parity;
            // SetDcbFlag, GetDcbFlag expose access to each of the relevant bits of the 32-bit integer
            // storing all flags of the DCB.  C# provides no direct means of manipulating bit fields, so
            // this is the solution.
            SetDcbFlag(Interop.Kernel32.DCBFlags.FPARITY, ((parity == Parity.None) ? 0 : 1));

            SetDcbFlag(Interop.Kernel32.DCBFlags.FBINARY, 1);   // always true for communications resources

            // set DCB fields implied by default and the arguments given.
            // Boolean fields in C# must become 1, 0 to properly set the bit flags in the unmanaged DCB struct

            SetDcbFlag(Interop.Kernel32.DCBFlags.FOUTXCTSFLOW, ((_handshake == Handshake.RequestToSend ||
                _handshake == Handshake.RequestToSendXOnXOff) ? 1 : 0));
            // SetDcbFlag(Interop.Kernel32.DCBFlags.FOUTXDSRFLOW, (dsrTimeout != 0L) ? 1 : 0);
            SetDcbFlag(Interop.Kernel32.DCBFlags.FOUTXDSRFLOW, 0); // dsrTimeout is always set to 0.
            SetDcbFlag(Interop.Kernel32.DCBFlags.FDTRCONTROL, Interop.Kernel32.DCBDTRFlowControl.DTR_CONTROL_DISABLE);
            SetDcbFlag(Interop.Kernel32.DCBFlags.FDSRSENSITIVITY, 0); // this should remain off
            SetDcbFlag(Interop.Kernel32.DCBFlags.FINX, (_handshake == Handshake.XOnXOff || _handshake == Handshake.RequestToSendXOnXOff) ? 1 : 0);
            SetDcbFlag(Interop.Kernel32.DCBFlags.FOUTX, (_handshake == Handshake.XOnXOff || _handshake == Handshake.RequestToSendXOnXOff) ? 1 : 0);

            // if no parity, we have no error character (i.e. ErrorChar = '\0' or null character)
            if (parity != Parity.None)
            {
                SetDcbFlag(Interop.Kernel32.DCBFlags.FERRORCHAR, (_parityReplace != '\0') ? 1 : 0);
                _dcb.ErrorChar = _parityReplace;
            }
            else
            {
                SetDcbFlag(Interop.Kernel32.DCBFlags.FERRORCHAR, 0);
                _dcb.ErrorChar = (byte)'\0';
            }

            // this method only runs once in the constructor, so we only have the default value to use.
            // Later the user may change this via the NullDiscard property.
            SetDcbFlag(Interop.Kernel32.DCBFlags.FNULL, discardNull ? 1 : 0);

            // SerialStream does not handle the fAbortOnError behaviour, so we must make sure it's not enabled
            SetDcbFlag(Interop.Kernel32.DCBFlags.FABORTONOERROR, 0);

            // Setting RTS control, which is RTS_CONTROL_HANDSHAKE if RTS / RTS-XOnXOff handshaking
            // used, RTS_ENABLE (RTS pin used during operation) if rtsEnable true but XOnXoff / No handshaking
            // used, and disabled otherwise.
            if ((_handshake == Handshake.RequestToSend ||
                _handshake == Handshake.RequestToSendXOnXOff))
            {
                SetDcbFlag(Interop.Kernel32.DCBFlags.FRTSCONTROL, Interop.Kernel32.DCBRTSFlowControl.RTS_CONTROL_HANDSHAKE);
            }
            else if (GetDcbFlag(Interop.Kernel32.DCBFlags.FRTSCONTROL) == Interop.Kernel32.DCBRTSFlowControl.RTS_CONTROL_HANDSHAKE)
            {
                SetDcbFlag(Interop.Kernel32.DCBFlags.FRTSCONTROL, Interop.Kernel32.DCBRTSFlowControl.RTS_CONTROL_DISABLE);
            }

            _dcb.XonChar = Interop.Kernel32.DCB.DEFAULTXONCHAR;             // may be exposed later but for now, constant
            _dcb.XoffChar = Interop.Kernel32.DCB.DEFAULTXOFFCHAR;

            // minimum number of bytes allowed in each buffer before flow control activated
            // heuristically, this has been set at 1/4 of the buffer size
            _dcb.XonLim = _dcb.XoffLim = (ushort)(_commProp.dwCurrentRxQueue / 4);

            _dcb.EofChar = Interop.Kernel32.DCB.EOFCHAR;

            //OLD MSCOMM: dcb.EvtChar = (byte) 0;
            // now changed to make use of RXFlag WaitCommEvent event => Eof WaitForCommEvent event
            _dcb.EvtChar = Interop.Kernel32.DCB.EOFCHAR;

            // set DCB structure
            if (Interop.Kernel32.SetCommState(_handle, ref _dcb) == false)
            {
                throw Win32Marshal.GetExceptionForLastWin32Error();
            }
        }

        // Here we provide a method for getting the flags of the Device Control Block structure dcb
        // associated with each instance of SerialStream, i.e. this method gets myStream.dcb.Flags
        internal int GetDcbFlag(int whichFlag)
        {
            uint mask;

            Debug.Assert(whichFlag >= Interop.Kernel32.DCBFlags.FBINARY && whichFlag <= Interop.Kernel32.DCBFlags.FDUMMY2, "GetDcbFlag needs to fit into enum!");

            if (whichFlag == Interop.Kernel32.DCBFlags.FDTRCONTROL || whichFlag == Interop.Kernel32.DCBFlags.FRTSCONTROL)
            {
                mask = 0x3;
            }
            else if (whichFlag == Interop.Kernel32.DCBFlags.FDUMMY2)
            {
                mask = 0x1FFFF;
            }
            else
            {
                mask = 0x1;
            }
            uint result = _dcb.Flags & (mask << whichFlag);
            return (int)(result >> whichFlag);
        }

        // Since C# applications have to provide a workaround for accessing and setting bitfields in unmanaged code,
        // here we provide methods for getting and setting the Flags field of the Device Control Block structure dcb
        // associated with each instance of SerialStream, i.e. this method sets myStream.dcb.Flags
        internal void SetDcbFlag(int whichFlag, int setting)
        {
            uint mask;
            setting <<= whichFlag;

            Debug.Assert(whichFlag >= Interop.Kernel32.DCBFlags.FBINARY && whichFlag <= Interop.Kernel32.DCBFlags.FDUMMY2, "SetDcbFlag needs to fit into enum!");

            if (whichFlag == Interop.Kernel32.DCBFlags.FDTRCONTROL || whichFlag == Interop.Kernel32.DCBFlags.FRTSCONTROL)
            {
                mask = 0x3;
            }
            else if (whichFlag == Interop.Kernel32.DCBFlags.FDUMMY2)
            {
                mask = 0x1FFFF;
            }
            else
            {
                mask = 0x1;
            }

            // clear the region
            _dcb.Flags &= ~(mask << whichFlag);

            // set the region
            _dcb.Flags |= ((uint)setting);
        }

        // ----SUBSECTION: internal methods supporting public read/write methods-------*

        private unsafe SerialStreamAsyncResult BeginReadCore(byte[] array, int offset, int numBytes, AsyncCallback userCallback, object stateObject)
        {
            // Create and store async stream class library specific data in the
            // async result
            SerialStreamAsyncResult asyncResult = new SerialStreamAsyncResult();
            asyncResult._userCallback = userCallback;
            asyncResult._userStateObject = stateObject;
            asyncResult._isWrite = false;

            // For Synchronous IO, I could go with either a callback and using
            // the managed Monitor class, or I could create a handle and wait on it.
            ManualResetEvent waitHandle = new ManualResetEvent(false);
            asyncResult._waitHandle = waitHandle;

            NativeOverlapped* intOverlapped = _threadPoolBinding.AllocateNativeOverlapped(s_IOCallback, asyncResult, array);

            asyncResult._overlapped = intOverlapped;

            // queue an async ReadFile operation and pass in a packed overlapped
            //int r = ReadFile(_handle, array, numBytes, null, intOverlapped);
            int r = ReadFileNative(array, offset, numBytes, intOverlapped, out int hr);

            // ReadFile, the OS version, will return 0 on failure.  But
            // my ReadFileNative wrapper returns -1.  My wrapper will return
            // the following:
            // On error, r==-1.
            // On async requests that are still pending, r==-1 w/ hr==ERROR_IO_PENDING
            // on async requests that completed sequentially, r==0
            // Note that you will NEVER RELIABLY be able to get the number of bytes
            // read back from this call when using overlapped structures!  You must
            // not pass in a non-null lpNumBytesRead to ReadFile when using
            // overlapped structures!
            if (r == -1)
            {
                if (hr != Interop.Errors.ERROR_IO_PENDING)
                {
                    if (hr == Interop.Errors.ERROR_HANDLE_EOF)
                        InternalResources.EndOfFile();
                    else
                        throw Win32Marshal.GetExceptionForWin32Error(hr, string.Empty);
                }
            }

            return asyncResult;
        }

        private unsafe SerialStreamAsyncResult BeginWriteCore(byte[] array, int offset, int numBytes, AsyncCallback userCallback, object stateObject)
        {
            // Create and store async stream class library specific data in the
            // async result
            SerialStreamAsyncResult asyncResult = new SerialStreamAsyncResult();
            asyncResult._userCallback = userCallback;
            asyncResult._userStateObject = stateObject;
            asyncResult._isWrite = true;

            // For Synchronous IO, I could go with either a callback and using
            // the managed Monitor class, or I could create a handle and wait on it.
            ManualResetEvent waitHandle = new ManualResetEvent(false);
            asyncResult._waitHandle = waitHandle;

            NativeOverlapped* intOverlapped = _threadPoolBinding.AllocateNativeOverlapped(s_IOCallback, asyncResult, array);

            asyncResult._overlapped = intOverlapped;

            // queue an async WriteFile operation and pass in a packed overlapped
            int r = WriteFileNative(array, offset, numBytes, intOverlapped, out int hr);

            // WriteFile, the OS version, will return 0 on failure.  But
            // my WriteFileNative wrapper returns -1.  My wrapper will return
            // the following:
            // On error, r==-1.
            // On async requests that are still pending, r==-1 w/ hr==ERROR_IO_PENDING
            // On async requests that completed sequentially, r==0
            // Note that you will NEVER RELIABLY be able to get the number of bytes
            // written back from this call when using overlapped IO!  You must
            // not pass in a non-null lpNumBytesWritten to WriteFile when using
            // overlapped structures!
            if (r == -1)
            {
                if (hr != Interop.Errors.ERROR_IO_PENDING)
                {

                    if (hr == Interop.Errors.ERROR_HANDLE_EOF)
                        InternalResources.EndOfFile();
                    else
                        throw Win32Marshal.GetExceptionForWin32Error(hr, string.Empty);
                }
            }
            return asyncResult;
        }


        // Internal method, wrapping the PInvoke to ReadFile().
        private unsafe int ReadFileNative(byte[] bytes, int offset, int count, NativeOverlapped* overlapped, out int hr)
        {

            // Don't corrupt memory when multiple threads are erroneously writing
            // to this stream simultaneously.
            if (bytes.Length - offset < count)
                throw new IndexOutOfRangeException(SR.IndexOutOfRange_IORaceCondition);

            if (bytes.Length == 0)
            {
                hr = 0;
                return 0;
            }

            int r = 0;
            int numBytesRead = 0;

            fixed (byte* p = bytes)
            {
                if (_isAsync)
                    r = Interop.Kernel32.ReadFile(_handle, p + offset, count, IntPtr.Zero, overlapped);
                else
                    r = Interop.Kernel32.ReadFile(_handle, p + offset, count, out numBytesRead, IntPtr.Zero);
            }

            if (r == 0)
            {
                hr = Marshal.GetLastPInvokeError();

                // Note: we should never silently ignore an error here without some
                // extra work.  We must make sure that BeginReadCore won't return an
                // IAsyncResult that will cause EndRead to block, since the OS won't
                // call AsyncFSCallback for us.

                // For invalid handles, detect the error and mark our handle
                // as closed to give slightly better error messages.  Also
                // help ensure we avoid handle recycling bugs.
                if (hr == Interop.Errors.ERROR_INVALID_HANDLE)
                    _handle.SetHandleAsInvalid();

                return -1;
            }
            else
                hr = 0;
            return numBytesRead;
        }

        private unsafe int WriteFileNative(byte[] bytes, int offset, int count, NativeOverlapped* overlapped, out int hr)
        {

            // Don't corrupt memory when multiple threads are erroneously writing
            // to this stream simultaneously.  (Note that the OS is reading from
            // the array we pass to WriteFile, but if we read beyond the end and
            // that memory isn't allocated, we could get an AV.)
            if (bytes.Length - offset < count)
                throw new IndexOutOfRangeException(SR.IndexOutOfRange_IORaceCondition);

            if (bytes.Length == 0)
            {
                hr = 0;
                return 0;
            }

            int numBytesWritten = 0;
            int r = 0;

            fixed (byte* p = bytes)
            {
                if (_isAsync)
                    r = Interop.Kernel32.WriteFile(_handle, p + offset, count, IntPtr.Zero, overlapped);
                else
                    r = Interop.Kernel32.WriteFile(_handle, p + offset, count, out numBytesWritten, IntPtr.Zero);
            }

            if (r == 0)
            {
                hr = Marshal.GetLastPInvokeError();
                // Note: we should never silently ignore an error here without some
                // extra work.  We must make sure that BeginWriteCore won't return an
                // IAsyncResult that will cause EndWrite to block, since the OS won't
                // call AsyncFSCallback for us.

                // For invalid handles, detect the error and mark our handle
                // as closed to give slightly better error messages.  Also
                // help ensure we avoid handle recycling bugs.
                if (hr == Interop.Errors.ERROR_INVALID_HANDLE)
                    _handle.SetHandleAsInvalid();

                return -1;
            }
            else
                hr = 0;
            return numBytesWritten;
        }

        // ----SUBSECTION: internal methods supporting events/async operation------*

        // This is a the callback prompted when a thread completes any async I/O operation.
        private static unsafe void AsyncFSCallback(uint errorCode, uint numBytes, NativeOverlapped* pOverlapped)
        {
            // Extract async the result from overlapped structure
            SerialStreamAsyncResult asyncResult =
                (SerialStreamAsyncResult)ThreadPoolBoundHandle.GetNativeOverlappedState(pOverlapped);

            asyncResult._numBytes = (int)numBytes;
            asyncResult._errorCode = (int)errorCode;

            // Call the user-provided callback.  Note that it can and often should
            // call EndRead or EndWrite.  There's no reason to use an async
            // delegate here - we're already on a threadpool thread.
            // Note the IAsyncResult's completedSynchronously property must return
            // false here, saying the user callback was called on another thread.
            asyncResult._completedSynchronously = false;
            asyncResult._isComplete = true;

            // The OS does not signal this event.  We must do it ourselves.
            // But don't close it if the user callback called EndXxx,
            // which then closed the manual reset event already.
            ManualResetEvent wh = asyncResult._waitHandle;
            if (wh != null)
            {
                bool r = wh.Set();
                if (!r) throw Win32Marshal.GetExceptionForLastWin32Error();
            }

            asyncResult._userCallback?.Invoke(asyncResult);
        }


        // ----SECTION: internal classes --------*

        internal sealed class EventLoopRunner
        {
            private readonly WeakReference streamWeakReference;
            internal ManualResetEvent waitCommEventWaitHandle = new ManualResetEvent(false);
            private readonly SafeFileHandle handle;
            private readonly ThreadPoolBoundHandle threadPoolBinding;
            private readonly bool isAsync;
            internal bool endEventLoop;
            private readonly int eventsOccurred;

            private readonly WaitCallback callErrorEvents;
            private readonly WaitCallback callReceiveEvents;
            private readonly WaitCallback callPinEvents;
            private readonly IOCompletionCallback freeNativeOverlappedCallback;

#if DEBUG
            private readonly string portName;
#endif

            internal unsafe EventLoopRunner(SerialStream stream)
            {
                handle = stream._handle;
                threadPoolBinding = stream._threadPoolBinding;
                streamWeakReference = new WeakReference(stream);

                callErrorEvents = new WaitCallback(CallErrorEvents);
                callReceiveEvents = new WaitCallback(CallReceiveEvents);
                callPinEvents = new WaitCallback(CallPinEvents);
                freeNativeOverlappedCallback = new IOCompletionCallback(FreeNativeOverlappedCallback);
                isAsync = stream._isAsync;
#if DEBUG
                portName = stream._portName;
#endif
            }

            internal bool ShutdownLoop
            {
                get
                {
                    return endEventLoop;
                }
            }

            // This is the blocking method that waits for an event to occur.  It wraps the SDK's WaitCommEvent function.
            internal unsafe void WaitForCommEvent()
            {
                int unused = 0;
                bool doCleanup = false;
                NativeOverlapped* intOverlapped = null;
                while (!ShutdownLoop)
                {
                    SerialStreamAsyncResult asyncResult = null;
                    if (isAsync)
                    {
                        asyncResult = new SerialStreamAsyncResult();
                        asyncResult._userCallback = null;
                        asyncResult._userStateObject = null;
                        asyncResult._isWrite = false;

                        // we're going to use _numBytes for something different in this loop.  In this case, both
                        // freeNativeOverlappedCallback and this thread will decrement that value.  Whichever one decrements it
                        // to zero will be the one to free the native overlapped.  This guarantees the overlapped gets freed
                        // after both the callback and GetOverlappedResult have had a chance to use it.
                        asyncResult._numBytes = 2;
                        asyncResult._waitHandle = waitCommEventWaitHandle;

                        waitCommEventWaitHandle.Reset();
                        intOverlapped = threadPoolBinding.AllocateNativeOverlapped(freeNativeOverlappedCallback, asyncResult, null);
                        intOverlapped->EventHandle = waitCommEventWaitHandle.SafeWaitHandle.DangerousGetHandle();
                    }

                    fixed (int* eventsOccurredPtr = &eventsOccurred)
                    {
                        if (Interop.Kernel32.WaitCommEvent(handle, eventsOccurredPtr, intOverlapped) == false)
                        {
                            int hr = Marshal.GetLastPInvokeError();

                            // When a device is disconnected unexpectedly from a serial port, there appear to be
                            // at least three error codes Windows or drivers may return.
                            const int ERROR_DEVICE_REMOVED = 1617;
                            if (hr == Interop.Errors.ERROR_ACCESS_DENIED || hr == Interop.Errors.ERROR_BAD_COMMAND || hr == ERROR_DEVICE_REMOVED)
                            {
                                doCleanup = true;
                                break;
                            }
                            if (hr == Interop.Errors.ERROR_IO_PENDING)
                            {
                                Debug.Assert(isAsync, "The port is not open for async, so we should not get ERROR_IO_PENDING from WaitCommEvent");
                                int error;

                                // if we get IO pending, MSDN says we should wait on the WaitHandle, then call GetOverlappedResult
                                // to get the results of WaitCommEvent.
                                bool success = waitCommEventWaitHandle.WaitOne();
                                Debug.Assert(success, $"waitCommEventWaitHandle.WaitOne() returned error {Marshal.GetLastPInvokeError()}");

                                do
                                {
                                    // NOTE: GetOverlappedResult will modify the original pointer passed into WaitCommEvent.
                                    success = Interop.Kernel32.GetOverlappedResult(handle, intOverlapped, ref unused, false);
                                    error = Marshal.GetLastPInvokeError();
                                }
                                while (error == Interop.Errors.ERROR_IO_INCOMPLETE && !ShutdownLoop && !success);

                                if (!success)
                                {
                                    // Ignore ERROR_IO_INCOMPLETE and ERROR_INVALID_PARAMETER, because there's a chance we'll get
                                    // one of those while shutting down
                                    if (!((error == Interop.Errors.ERROR_IO_INCOMPLETE || error == Interop.Errors.ERROR_INVALID_PARAMETER) && ShutdownLoop))
                                        Debug.Fail("GetOverlappedResult returned error, we might leak intOverlapped memory" + error.ToString(CultureInfo.InvariantCulture));
                                }
                            }
                            else if (hr != Interop.Errors.ERROR_INVALID_PARAMETER)
                            {
                                // ignore ERROR_INVALID_PARAMETER errors.  WaitCommError seems to return this
                                // when SetCommMask is changed while it's blocking (like we do in Dispose())
                                Debug.Fail("WaitCommEvent returned error " + hr);
                            }
                        }
                    }

                    if (!ShutdownLoop)
                        CallEvents(eventsOccurred);

                    if (isAsync)
                    {
                        if (Interlocked.Decrement(ref asyncResult._numBytes) == 0)
                            threadPoolBinding.FreeNativeOverlapped(intOverlapped);
                    }
                } // while (!ShutdownLoop)

                if (doCleanup)
                {
                    // the rest will be handled in Dispose()
                    endEventLoop = true;
                    threadPoolBinding.FreeNativeOverlapped(intOverlapped);
                }
            }

            private unsafe void FreeNativeOverlappedCallback(uint errorCode, uint numBytes, NativeOverlapped* pOverlapped)
            {
                // Extract the async result from overlapped structure
                SerialStreamAsyncResult asyncResult =
                    (SerialStreamAsyncResult)ThreadPoolBoundHandle.GetNativeOverlappedState(pOverlapped);

                if (Interlocked.Decrement(ref asyncResult._numBytes) == 0)
                    threadPoolBinding.FreeNativeOverlapped(pOverlapped);
            }

            private void CallEvents(int nativeEvents)
            {
                // EV_ERR includes only CE_FRAME, CE_OVERRUN, and CE_RXPARITY
                // To catch errors such as CE_RXOVER, we need to call CleanCommErrors bit more regularly.
                // EV_RXCHAR is perhaps too loose an event to look for overflow errors but a safe side to err...
                if ((nativeEvents & (Interop.Kernel32.CommEvents.EV_ERR | Interop.Kernel32.CommEvents.EV_RXCHAR)) != 0)
                {
                    int errors = 0;
                    if (Interop.Kernel32.ClearCommError(handle, ref errors, IntPtr.Zero) == false)
                    {

                        //throw Win32Marshal.GetExceptionForLastWin32Error();

                        // We don't want to throw an exception from the background thread which is un-catchable and hence tear down the process.
                        // At present we don't have a first class event that we can raise for this class of fatal errors. One possibility is
                        // to overload SeralErrors event to include another enum (perhaps CE_IOE) that we can use for this purpose.
                        // In the absence of that, it is better to eat this error silently than tearing down the process (lesser of the evil).
                        // This uncleared comm error will most likely blow up when the device is accessed by other APIs (such as Read) on the
                        // main thread and hence become known. It is bit roundabout but acceptable.
                        //
                        // Shutdown the event runner loop (probably bit drastic but we did come across a fatal error).
                        // Defer actual dispose chores until finalization though.
                        endEventLoop = true;
                        Thread.MemoryBarrier();
                        return;
                    }

                    errors &= ErrorEvents;
                    // TODO: what about CE_BREAK?  Is this the same as EV_BREAK?  EV_BREAK happens as one of the pin events,
                    //       but CE_BREAK is returned from ClreaCommError.
                    // TODO: what about other error conditions not covered by the enum?  Should those produce some other error?

                    if (errors != 0)
                    {
                        ThreadPool.QueueUserWorkItem(callErrorEvents, errors);
                    }
                }

                // now look for pin changed and received events.
                if ((nativeEvents & PinChangedEvents) != 0)
                {
                    ThreadPool.QueueUserWorkItem(callPinEvents, nativeEvents);
                }

                if ((nativeEvents & ReceivedEvents) != 0)
                {
                    ThreadPool.QueueUserWorkItem(callReceiveEvents, nativeEvents);
                }
            }

            private void CallErrorEvents(object state)
            {
                int errors = (int)state;
                SerialStream stream = (SerialStream)streamWeakReference.Target;
                if (stream == null)
                    return;

                if (stream.ErrorReceived != null)
                {
                    if ((errors & (int)SerialError.TXFull) != 0)
                        stream.ErrorReceived(stream, new SerialErrorReceivedEventArgs(SerialError.TXFull));

                    if ((errors & (int)SerialError.RXOver) != 0)
                        stream.ErrorReceived(stream, new SerialErrorReceivedEventArgs(SerialError.RXOver));

                    if ((errors & (int)SerialError.Overrun) != 0)
                        stream.ErrorReceived(stream, new SerialErrorReceivedEventArgs(SerialError.Overrun));

                    if ((errors & (int)SerialError.RXParity) != 0)
                        stream.ErrorReceived(stream, new SerialErrorReceivedEventArgs(SerialError.RXParity));

                    if ((errors & (int)SerialError.Frame) != 0)
                        stream.ErrorReceived(stream, new SerialErrorReceivedEventArgs(SerialError.Frame));
                }
            }

            private void CallReceiveEvents(object state)
            {
                int nativeEvents = (int)state;
                SerialStream stream = (SerialStream)streamWeakReference.Target;
                if (stream == null)
                    return;

                if (stream.DataReceived != null)
                {
                    if ((nativeEvents & (int)SerialData.Chars) != 0)
                        stream.DataReceived(stream, new SerialDataReceivedEventArgs(SerialData.Chars));
                    if ((nativeEvents & (int)SerialData.Eof) != 0)
                        stream.DataReceived(stream, new SerialDataReceivedEventArgs(SerialData.Eof));
                }
            }

            private void CallPinEvents(object state)
            {
                int nativeEvents = (int)state;

                SerialStream stream = (SerialStream)streamWeakReference.Target;
                if (stream == null)
                    return;

                if (stream.PinChanged != null)
                {
                    if ((nativeEvents & (int)SerialPinChange.CtsChanged) != 0)
                        stream.PinChanged(stream, new SerialPinChangedEventArgs(SerialPinChange.CtsChanged));

                    if ((nativeEvents & (int)SerialPinChange.DsrChanged) != 0)
                        stream.PinChanged(stream, new SerialPinChangedEventArgs(SerialPinChange.DsrChanged));

                    if ((nativeEvents & (int)SerialPinChange.CDChanged) != 0)
                        stream.PinChanged(stream, new SerialPinChangedEventArgs(SerialPinChange.CDChanged));

                    if ((nativeEvents & (int)SerialPinChange.Ring) != 0)
                        stream.PinChanged(stream, new SerialPinChangedEventArgs(SerialPinChange.Ring));

                    if ((nativeEvents & (int)SerialPinChange.Break) != 0)
                        stream.PinChanged(stream, new SerialPinChangedEventArgs(SerialPinChange.Break));
                }
            }

        }


        // This is an internal object implementing IAsyncResult with fields
        // for all of the relevant data necessary to complete the IO operation.
        // This is used by AsyncFSCallback and all async methods.
        internal sealed unsafe class SerialStreamAsyncResult : IAsyncResult
        {
            // User code callback
            internal AsyncCallback _userCallback;

            internal object _userStateObject;

            internal bool _isWrite;     // Whether this is a read or a write
            internal bool _isComplete;
            internal bool _completedSynchronously;  // Which thread called callback

            internal ManualResetEvent _waitHandle;
            internal int _EndXxxCalled;   // Whether we've called EndXxx already.
            internal int _numBytes;     // number of bytes read OR written
            internal int _errorCode;
            internal NativeOverlapped* _overlapped;

            public object AsyncState
            {
                get { return _userStateObject; }
            }

            public bool IsCompleted
            {
                get { return _isComplete; }
            }

            public WaitHandle AsyncWaitHandle
            {
                get
                {
                    /*
                      // Consider uncommenting this someday soon - the EventHandle
                      // in the Overlapped struct is really useless half of the
                      // time today since the OS doesn't signal it.  If users call
                      // EndXxx after the OS call happened to complete, there's no
                      // reason to create a synchronization primitive here.  Fixing
                      // this will save us some perf, assuming we can correctly
                      // initialize the ManualResetEvent.
                    if (_waitHandle == null) {
                        ManualResetEvent mre = new ManualResetEvent(false);
                        if (_overlapped != null && _overlapped->EventHandle != IntPtr.Zero)
                            mre.Handle = _overlapped->EventHandle;
                        if (_isComplete)
                            mre.Set();
                        _waitHandle = mre;
                    }
                    */
                    return _waitHandle;
                }
            }

            // Returns true if the user callback was called by the thread that
            // called BeginRead or BeginWrite.  If we use an async delegate or
            // threadpool thread internally, this will be false.  This is used
            // by code to determine whether a successive call to BeginRead needs
            // to be done on their main thread or in their callback to avoid a
            // stack overflow on many reads or writes.
            public bool CompletedSynchronously
            {
                get { return _completedSynchronously; }
            }
        }
    }
}
