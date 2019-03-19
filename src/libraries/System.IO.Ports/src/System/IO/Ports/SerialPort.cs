// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Win32;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;

namespace System.IO.Ports
{
    public partial class SerialPort : Component
    {
        public const int InfiniteTimeout = -1;

        // ---------- default values -------------*

        private const int DefaultDataBits = 8;
        private const Parity DefaultParity = Parity.None;
        private const StopBits DefaultStopBits = StopBits.One;
        private const Handshake DefaultHandshake = Handshake.None;
        private const int DefaultBufferSize = 1024;
        private const string DefaultPortName = "COM1";
        private const int DefaultBaudRate = 9600;
        private const bool DefaultDtrEnable = false;
        private const bool DefaultRtsEnable = false;
        private const bool DefaultDiscardNull = false;
        private const byte DefaultParityReplace = (byte)'?';
        private const int DefaultReceivedBytesThreshold = 1;
        private const int DefaultReadTimeout = InfiniteTimeout;
        private const int DefaultWriteTimeout = InfiniteTimeout;
        private const int DefaultReadBufferSize = 4096;
        private const int DefaultWriteBufferSize = 2048;
        private const int MaxDataBits = 8;
        private const int MinDataBits = 5;
        private const string DefaultNewLine = "\n";

        private const string SERIAL_NAME = @"\Device\Serial";

        // --------- members supporting exposed properties ------------*
        private int _baudRate = DefaultBaudRate;
        private int _dataBits = DefaultDataBits;
        private Parity _parity = DefaultParity;
        private StopBits _stopBits = DefaultStopBits;
        private string _portName = DefaultPortName;
        private Encoding _encoding = Encoding.ASCII; // ASCII is default encoding for modem communication, etc.
        private Decoder _decoder = Encoding.ASCII.GetDecoder();
        private int _maxByteCountForSingleChar = Encoding.ASCII.GetMaxByteCount(1);
        private Handshake _handshake = DefaultHandshake;
        private int _readTimeout = DefaultReadTimeout;
        private int _writeTimeout = DefaultWriteTimeout;
        private int _receivedBytesThreshold = DefaultReceivedBytesThreshold;
        private bool _discardNull = DefaultDiscardNull;
        private bool _dtrEnable = DefaultDtrEnable;
        private bool _rtsEnable = DefaultRtsEnable;
        private byte _parityReplace = DefaultParityReplace;
        private string _newLine = DefaultNewLine;
        private int _readBufferSize = DefaultReadBufferSize;
        private int _writeBufferSize = DefaultWriteBufferSize;

        // ---------- members for internal support ---------*
        private SerialStream _internalSerialStream = null;
        private byte[] _inBuffer = new byte[DefaultBufferSize];
        private int _readPos = 0;    // position of next byte to read in the read buffer.  readPos <= readLen
        private int _readLen = 0;    // position of first unreadable byte => CachedBytesToRead is the number of readable bytes left.
        private char[] _oneChar = new char[1];
        private char[] _singleCharBuffer = null;

        public event SerialErrorReceivedEventHandler ErrorReceived;
        public event SerialPinChangedEventHandler PinChanged;

        // handler for the underlying stream
        private SerialDataReceivedEventHandler _dataReceivedHandler;

        private SerialDataReceivedEventHandler _dataReceived;
        public event SerialDataReceivedEventHandler DataReceived
        {
            add
            {
                bool wasNull = _dataReceived == null;
                _dataReceived += value;

                if (wasNull)
                {
                    if (_internalSerialStream != null)
                    {
                        _internalSerialStream.DataReceived += _dataReceivedHandler;
                    }
                }
            }
            remove
            {
                _dataReceived -= value;

                if (_dataReceived == null)
                {
                    if (_internalSerialStream != null)
                    {
                        _internalSerialStream.DataReceived -= _dataReceivedHandler;
                    }
                }
            }
        }

        //--- component properties---------------*

        // ---- SECTION: public properties --------------*
        // Note: information about port properties passes in ONE direction: from SerialPort to
        // its underlying Stream.  No changes are able to be made in the important properties of
        // the stream and its behavior, so no reflection back to SerialPort is necessary.

        // Gets the internal SerialStream object.  Used to pass essence of SerialPort to another Stream wrapper.
        public Stream BaseStream
        {
            get
            {
                if (!IsOpen)
                    throw new InvalidOperationException(SR.BaseStream_Invalid_Not_Open);

                return _internalSerialStream;
            }
        }

        public int BaudRate
        {
            get { return _baudRate; }
            set
            {
                if (value <= 0)
                    throw new ArgumentOutOfRangeException(nameof(BaudRate), SR.ArgumentOutOfRange_NeedPosNum);

                if (IsOpen)
                    _internalSerialStream.BaudRate = value;
                _baudRate = value;
            }
        }

        public bool BreakState
        {
            get
            {
                if (!IsOpen)
                    throw new InvalidOperationException(SR.Port_not_open);

                return _internalSerialStream.BreakState;
            }
            set
            {
                if (!IsOpen)
                    throw new InvalidOperationException(SR.Port_not_open);

                _internalSerialStream.BreakState = value;
            }
        }

        // includes all bytes available on serial driver's output buffer.  Note that we do not internally buffer output bytes in SerialPort.
        public int BytesToWrite
        {
            get
            {
                if (!IsOpen)
                    throw new InvalidOperationException(SR.Port_not_open);
                return _internalSerialStream.BytesToWrite;
            }
        }

        // includes all bytes available on serial driver's input buffer as well as bytes internally buffered int the SerialPort class.
        public int BytesToRead
        {
            get
            {
                if (!IsOpen)
                    throw new InvalidOperationException(SR.Port_not_open);
                return _internalSerialStream.BytesToRead + CachedBytesToRead; // count the number of bytes we have in the internal buffer too.
            }
        }

        private int CachedBytesToRead
        {
            get
            {
                return _readLen - _readPos;
            }
        }

        public bool CDHolding
        {
            get
            {
                if (!IsOpen)
                    throw new InvalidOperationException(SR.Port_not_open);
                return _internalSerialStream.CDHolding;
            }
        }

        public bool CtsHolding
        {
            get
            {
                if (!IsOpen)
                    throw new InvalidOperationException(SR.Port_not_open);
                return _internalSerialStream.CtsHolding;
            }
        }

        public int DataBits
        {
            get
            { return _dataBits; }
            set
            {
                if (value < MinDataBits || value > MaxDataBits)
                    throw new ArgumentOutOfRangeException(nameof(DataBits), SR.Format(SR.ArgumentOutOfRange_Bounds_Lower_Upper, MinDataBits, MaxDataBits));

                if (IsOpen)
                    _internalSerialStream.DataBits = value;
                _dataBits = value;
            }
        }

        public bool DiscardNull
        {
            get
            {
                return _discardNull;
            }
            set
            {
                if (IsOpen)
                    _internalSerialStream.DiscardNull = value;
                _discardNull = value;
            }
        }

        public bool DsrHolding
        {
            get
            {
                if (!IsOpen)
                    throw new InvalidOperationException(SR.Port_not_open);
                return _internalSerialStream.DsrHolding;
            }
        }

        public bool DtrEnable
        {
            get
            {
                if (IsOpen)
                    _dtrEnable = _internalSerialStream.DtrEnable;

                return _dtrEnable;
            }
            set
            {
                if (IsOpen)
                    _internalSerialStream.DtrEnable = value;
                _dtrEnable = value;
            }
        }

        // Allows specification of an arbitrary encoding for the reading and writing functions of the port
        // which deal with chars and strings.  Set by default in the code to System.Text.ASCIIEncoding(), which
        // is the standard text encoding for modem commands and most of serial communication.
        public Encoding Encoding
        {
            get
            {
                return _encoding;
            }
            set
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(Encoding));

                // Limit the encodings we support to some known ones.  The code pages < 50000 represent all of the single-byte
                // and double-byte code pages.  Code page 54936 is GB18030.
                if (!(value is ASCIIEncoding || value is UTF8Encoding || value is UnicodeEncoding || value is UTF32Encoding ||
                      value.CodePage < 50000 || value.CodePage == 54936))
                {
                    throw new ArgumentException(SR.Format(SR.NotSupportedEncoding, value.WebName), nameof(Encoding));
                }

                _encoding = value;
                _decoder = _encoding.GetDecoder();

                // This is somewhat of an approximate guesstimate to get the max char[] size needed to encode a single character
                _maxByteCountForSingleChar = _encoding.GetMaxByteCount(1);
                _singleCharBuffer = null;
            }
        }

        public Handshake Handshake
        {
            get
            {
                return _handshake;
            }
            set
            {
                if (value < Handshake.None || value > Handshake.RequestToSendXOnXOff)
                    throw new ArgumentOutOfRangeException(nameof(Handshake), SR.ArgumentOutOfRange_Enum);

                if (IsOpen)
                    _internalSerialStream.Handshake = value;
                _handshake = value;
            }
        }

        public bool IsOpen
        {
            // true only if the Open() method successfully called on this SerialPort object, without Close() being called more recently.
            get { return (_internalSerialStream != null && _internalSerialStream.IsOpen); }
        }

        public string NewLine
        {
            get { return _newLine; }
            set
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(NewLine));
                if (value.Length == 0)
                    throw new ArgumentException(SR.Format(SR.InvalidNullEmptyArgument, nameof(NewLine)), nameof(NewLine));

                _newLine = value;
            }
        }

        public Parity Parity
        {
            get
            {
                return _parity;
            }
            set
            {
                if (value < Parity.None || value > Parity.Space)
                    throw new ArgumentOutOfRangeException(nameof(Parity), SR.ArgumentOutOfRange_Enum);

                if (IsOpen)
                    _internalSerialStream.Parity = value;
                _parity = value;
            }
        }

        public byte ParityReplace
        {
            get { return _parityReplace; }
            set
            {
                if (IsOpen)
                    _internalSerialStream.ParityReplace = value;
                _parityReplace = value;
            }
        }

        // Note that the communications port cannot be meaningfully re-set when the port is open,
        // and so once set by the constructor becomes read-only.
        public string PortName
        {
            get
            {
                return _portName;
            }
            set
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(PortName));
                if (value.Length == 0)
                    throw new ArgumentException(SR.PortNameEmpty_String, nameof(PortName));

                if (IsOpen)
                    throw new InvalidOperationException(SR.Format(SR.Cant_be_set_when_open, nameof(PortName)));
                _portName = value;
            }
        }

        public int ReadBufferSize
        {
            get
            {
                return _readBufferSize;
            }
            set
            {
                if (value <= 0)
                    throw new ArgumentOutOfRangeException(nameof(ReadBufferSize));

                if (IsOpen)
                    throw new InvalidOperationException(SR.Format(SR.Cant_be_set_when_open, nameof(ReadBufferSize)));

                _readBufferSize = value;
            }
        }

        // timeout for all read operations.  May be set to SerialPort.InfiniteTimeout, 0, or any positive value
        public int ReadTimeout
        {
            get
            {
                return _readTimeout;
            }
            set
            {
                if (value < 0 && value != InfiniteTimeout)
                    throw new ArgumentOutOfRangeException(nameof(ReadTimeout), SR.ArgumentOutOfRange_Timeout);

                if (IsOpen)
                    _internalSerialStream.ReadTimeout = value;
                _readTimeout = value;
            }
        }

        // If we have the SerialData.Chars event set, this property indicates the number of bytes necessary
        // to exist in our buffers before the event is thrown.  This is useful if we expect to receive n-byte
        // packets and can only act when we have this many, etc.
        public int ReceivedBytesThreshold
        {
            get
            {
                return _receivedBytesThreshold;
            }
            set
            {
                if (value <= 0)
                    throw new ArgumentOutOfRangeException(nameof(ReceivedBytesThreshold), SR.ArgumentOutOfRange_NeedPosNum);

                _receivedBytesThreshold = value;

                if (IsOpen)
                {
                    // fake the call to our event handler in case the threshold has been set lower
                    // than how many bytes we currently have.
                    SerialDataReceivedEventArgs args = new SerialDataReceivedEventArgs(SerialData.Chars);
                    CatchReceivedEvents(this, args);
                }
            }
        }

        public bool RtsEnable
        {
            get
            {
                if (IsOpen)
                    _rtsEnable = _internalSerialStream.RtsEnable;

                return _rtsEnable;
            }
            set
            {
                if (IsOpen)
                    _internalSerialStream.RtsEnable = value;
                _rtsEnable = value;
            }
        }

        // StopBits represented in C# as StopBits enum type and in Win32 as an integer 1, 2, or 3.
        public StopBits StopBits
        {
            get
            {
                return _stopBits;
            }
            set
            {
                // this range check looks wrong, but it really is correct.  One = 1, Two = 2, and OnePointFive = 3
                if (value < StopBits.One || value > StopBits.OnePointFive)
                    throw new ArgumentOutOfRangeException(nameof(StopBits), SR.ArgumentOutOfRange_Enum);

                if (IsOpen)
                    _internalSerialStream.StopBits = value;
                _stopBits = value;
            }
        }

        public int WriteBufferSize
        {
            get
            {
                return _writeBufferSize;
            }
            set
            {
                if (value <= 0)
                    throw new ArgumentOutOfRangeException(nameof(WriteBufferSize));

                if (IsOpen)
                    throw new InvalidOperationException(SR.Format(SR.Cant_be_set_when_open, nameof(WriteBufferSize)));

                _writeBufferSize = value;
            }
        }

        // timeout for all write operations.  May be set to SerialPort.InfiniteTimeout or any positive value
        public int WriteTimeout
        {
            get
            {
                return _writeTimeout;
            }
            set
            {
                if (value <= 0 && value != InfiniteTimeout)
                    throw new ArgumentOutOfRangeException(nameof(WriteTimeout), SR.ArgumentOutOfRange_WriteTimeout);

                if (IsOpen)
                    _internalSerialStream.WriteTimeout = value;
                _writeTimeout = value;
            }
        }

        // -------- SECTION: constructors -----------------*
        public SerialPort()
        {
            _dataReceivedHandler = new SerialDataReceivedEventHandler(CatchReceivedEvents);
        }

        public SerialPort(IContainer container) : this()
        {
            // Required for Windows.Forms Class Composition Designer support
            container.Add(this);
        }

        // Non-design SerialPort constructors here chain, using default values for members left unspecified by parameters
        // Note: Calling SerialPort() does not open a port connection but merely instantiates an object.
        //     : A connection must be made using SerialPort's Open() method.
        public SerialPort(string portName) : this(portName, DefaultBaudRate, DefaultParity, DefaultDataBits, DefaultStopBits)
        {
        }

        public SerialPort(string portName, int baudRate) : this(portName, baudRate, DefaultParity, DefaultDataBits, DefaultStopBits)
        {
        }

        public SerialPort(string portName, int baudRate, Parity parity) : this(portName, baudRate, parity, DefaultDataBits, DefaultStopBits)
        {
        }

        public SerialPort(string portName, int baudRate, Parity parity, int dataBits) : this(portName, baudRate, parity, dataBits, DefaultStopBits)
        {
        }

        // all the magic happens in the call to the instance's .Open() method.
        // Internally, the SerialStream constructor opens the file handle, sets the device
        // control block and associated Win32 structures, and begins the event-watching cycle.
        public SerialPort(string portName, int baudRate, Parity parity, int dataBits, StopBits stopBits) : this()
        {
            PortName = portName;
            BaudRate = baudRate;
            Parity = parity;
            DataBits = dataBits;
            StopBits = stopBits;
        }

        // Calls internal Serial Stream's Close() method on the internal Serial Stream.
        public void Close()
        {
            Dispose();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (IsOpen)
                {
                    _internalSerialStream.DataReceived -= _dataReceivedHandler;
                    _internalSerialStream.Flush();
                    _internalSerialStream.Close();
                    _internalSerialStream = null;
                }
            }
            base.Dispose(disposing);
        }

        public void DiscardInBuffer()
        {
            if (!IsOpen)
                throw new InvalidOperationException(SR.Port_not_open);
            _internalSerialStream.DiscardInBuffer();
            _readPos = _readLen = 0;
        }

        public void DiscardOutBuffer()
        {
            if (!IsOpen)
                throw new InvalidOperationException(SR.Port_not_open);
            _internalSerialStream.DiscardOutBuffer();
        }

        // SerialPort is open <=> SerialPort has an associated SerialStream.
        // The two statements are functionally equivalent here, so this method basically calls underlying Stream's
        // constructor from the main properties specified in SerialPort: baud, stopBits, parity, dataBits,
        // comm portName, handshaking, and timeouts.
        public void Open()
        {
            if (IsOpen)
                throw new InvalidOperationException(SR.Port_already_open);

            _internalSerialStream = new SerialStream(_portName, _baudRate, _parity, _dataBits, _stopBits, _readTimeout,
                _writeTimeout, _handshake, _dtrEnable, _rtsEnable, _discardNull, _parityReplace);

            _internalSerialStream.SetBufferSizes(_readBufferSize, _writeBufferSize);

            _internalSerialStream.ErrorReceived += new SerialErrorReceivedEventHandler(CatchErrorEvents);
            _internalSerialStream.PinChanged += new SerialPinChangedEventHandler(CatchPinChangedEvents);

            if (_dataReceived != null)
            {
                _internalSerialStream.DataReceived += _dataReceivedHandler;
            }
        }

        // Read Design pattern:
        //  : ReadChar() returns the first available full char if found before, throws TimeoutExc if timeout.
        //  : Read(byte[] buffer..., int count) returns all data available before read timeout expires up to *count* bytes
        //  : Read(char[] buffer..., int count) returns all data available before read timeout expires up to *count* chars.
        //  :                                   Note, this does not return "half-characters".
        //  : ReadByte() is the binary analogue of the first one.
        //  : ReadLine(): returns null string on timeout, saves received data in buffer
        //  : ReadAvailable(): returns all full characters which are IMMEDIATELY available.

        public int Read(byte[] buffer, int offset, int count)
        {
            if (!IsOpen)
                throw new InvalidOperationException(SR.Port_not_open);
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));
            if (offset < 0)
                throw new ArgumentOutOfRangeException(nameof(offset), SR.ArgumentOutOfRange_NeedNonNegNumRequired);
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count), SR.ArgumentOutOfRange_NeedNonNegNumRequired);
            if (buffer.Length - offset < count)
                throw new ArgumentException(SR.Argument_InvalidOffLen);
            int bytesReadToBuffer = 0;

            // if any bytes available in internal buffer, return those without calling any read ops.
            if (CachedBytesToRead >= 1)
            {
                bytesReadToBuffer = Math.Min(CachedBytesToRead, count);
                Buffer.BlockCopy(_inBuffer, _readPos, buffer, offset, bytesReadToBuffer);
                _readPos += bytesReadToBuffer;
                if (bytesReadToBuffer == count)
                {
                    if (_readPos == _readLen) _readPos = _readLen = 0;  // just a check to see if we can reset buffer
                    return count;
                }

                // if we have read some bytes but there's none immediately available, return.
                if (BytesToRead == 0)
                    return bytesReadToBuffer;
            }

            Debug.Assert(CachedBytesToRead == 0, "there should be nothing left in our internal buffer");
            _readLen = _readPos = 0;

            int bytesLeftToRead = count - bytesReadToBuffer;

            // request to read the requested number of bytes to fulfill the contract,
            // doesn't matter if we time out.  We still return all the data we have available.
            bytesReadToBuffer += _internalSerialStream.Read(buffer, offset + bytesReadToBuffer, bytesLeftToRead);

            _decoder.Reset();
            return bytesReadToBuffer;
        }

        // publicly exposed "ReadOneChar"-type: Read()
        // reads one full character from the stream
        public int ReadChar()
        {
            if (!IsOpen)
                throw new InvalidOperationException(SR.Port_not_open);

            return ReadOneChar(_readTimeout);
        }

        // gets next available full character, which may be from the buffer, the stream, or both.
        // this takes size^2 time at most, where *size* is the maximum size of any one character in an encoding.
        // The user can call Read(1) to mimic this functionality.

        // We can replace ReadOneChar with Read at some point
        private int ReadOneChar(int timeout)
        {
            int nextByte;
            int timeUsed = 0;
            Debug.Assert(IsOpen, "ReadOneChar - port not open");

            // case 1: we have >= 1 character in the internal buffer.
            if (_decoder.GetCharCount(_inBuffer, _readPos, CachedBytesToRead) != 0)
            {
                int beginReadPos = _readPos;
                // get characters from buffer.
                do
                {
                    _readPos++;
                } while (_decoder.GetCharCount(_inBuffer, beginReadPos, _readPos - beginReadPos) < 1);

                try
                {
                    _decoder.GetChars(_inBuffer, beginReadPos, _readPos - beginReadPos, _oneChar, 0);
                }
                catch
                {

                    // Handle surrogate chars correctly, restore readPos
                    _readPos = beginReadPos;
                    throw;
                }
                return _oneChar[0];
            }
            else
            {

                // need to return immediately.
                if (timeout == 0)
                {
                    // read all bytes in the serial driver in here.  Make sure we ask for at least 1 byte
                    // so that we get the proper timeout behavior
                    int bytesInStream = _internalSerialStream.BytesToRead;
                    if (bytesInStream == 0)
                        bytesInStream = 1;
                    MaybeResizeBuffer(bytesInStream);
                    _readLen += _internalSerialStream.Read(_inBuffer, _readLen, bytesInStream); // read all immediately avail.

                    // If what we have in the buffer is not enough, throw TimeoutExc
                    // if we are reading surrogate char then ReadBufferIntoChars
                    // will throw argexc and that is okay as readPos is not altered
                    if (ReadBufferIntoChars(_oneChar, 0, 1, false) == 0)
                        throw new TimeoutException();
                    else
                        return _oneChar[0];
                }

                // case 2: we need to read from outside to find this.
                // timeout is either infinite or positive.
                int startTicks = Environment.TickCount;
                do
                {
                    if (timeout == InfiniteTimeout)
                        nextByte = _internalSerialStream.ReadByte(InfiniteTimeout);
                    else if (timeout - timeUsed >= 0)
                    {
                        nextByte = _internalSerialStream.ReadByte(timeout - timeUsed);
                        timeUsed = Environment.TickCount - startTicks;
                    }
                    else
                        throw new TimeoutException();

                    MaybeResizeBuffer(1);
                    _inBuffer[_readLen++] = (byte)nextByte;  // we must add to the end of the buffer
                } while (_decoder.GetCharCount(_inBuffer, _readPos, _readLen - _readPos) < 1);
            }

            // If we are reading surrogate char then this will throw argexc
            // we need not deal with that exc because we have not altered readPos yet.
            _decoder.GetChars(_inBuffer, _readPos, _readLen - _readPos, _oneChar, 0);

            // Everything should be out of inBuffer now.  We'll just reset the pointers.
            _readLen = _readPos = 0;
            return _oneChar[0];
        }

        // Will return 'n' (1 < n < count) characters (or) TimeoutExc
        public int Read(char[] buffer, int offset, int count)
        {
            if (!IsOpen)
                throw new InvalidOperationException(SR.Port_not_open);
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));
            if (offset < 0)
                throw new ArgumentOutOfRangeException(nameof(offset), SR.ArgumentOutOfRange_NeedNonNegNumRequired);
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count), SR.ArgumentOutOfRange_NeedNonNegNumRequired);
            if (buffer.Length - offset < count)
                throw new ArgumentException(SR.Argument_InvalidOffLen);

            return InternalRead(buffer, offset, count, _readTimeout, false);
        }

        private int InternalRead(char[] buffer, int offset, int count, int timeout, bool countMultiByteCharsAsOne)
        {
            Debug.Assert(IsOpen, "port not open!");
            Debug.Assert(buffer != null, "invalid buffer!");
            Debug.Assert(offset >= 0, "invalid offset!");
            Debug.Assert(count >= 0, "invalid count!");
            Debug.Assert(buffer.Length - offset >= count, "invalid offset/count!");

            if (count == 0) return 0;   // immediately return on zero chars desired.  This simplifies things later.

            // Get the startticks before we read the underlying stream
            int startTicks = Environment.TickCount;

            // read everything else into internal buffer, which we know we can do instantly, and see if we NOW have enough.
            int bytesInStream = _internalSerialStream.BytesToRead;
            MaybeResizeBuffer(bytesInStream);
            _readLen += _internalSerialStream.Read(_inBuffer, _readLen, bytesInStream);    // should execute instantaneously.

            int charsWeAlreadyHave = _decoder.GetCharCount(_inBuffer, _readPos, CachedBytesToRead); // full chars already in our buffer
            if (charsWeAlreadyHave > 0)
            {
                // we found some chars after reading everything the SerialStream had to offer.  We'll return what we have
                // rather than wait for more.
                return ReadBufferIntoChars(buffer, offset, count, countMultiByteCharsAsOne);
            }

            if (timeout == 0)
                throw new TimeoutException();

            // else: we need to do incremental reads from the stream.
            // -----
            // our internal algorithm for finding exactly n characters is a bit complicated, but must overcome the
            // hurdle of NEVER READING TOO MANY BYTES from the Stream, since we can time out.  A variable-length encoding
            // allows anywhere between minimum and maximum bytes per char times number of chars to be the exactly correct
            // target, and we have to take care not to overuse GetCharCount().  The problem is that GetCharCount() will never tell
            // us if we've read "half" a character in our current set of collected bytes; it underestimates.
            // size = maximum bytes per character in the encoding.  n = number of characters requested.
            // Solution I: Use ReadOneChar() to read successive characters until we get to n.
            // Read calls: size * n; GetCharCount calls: size * n; each byte "counted": size times.
            // Solution II: Use a binary reduction and backtracking to reduce the number of calls.
            // Read calls: size * log n; GetCharCount calls: size * log n; each byte "counted": size * (log n) / n times.
            // We use the second, more complicated solution here.  Note log is actually log_(size/size - 1)...


            // we need to read some from the stream
            // read *up to* the maximum number of bytes from the stream
            // we can read more since we receive everything instantaneously, and we don't have enough,
            // so when we do receive any data, it will be necessary and sufficient.

            int justRead;
            int maxReadSize = Encoding.GetMaxByteCount(count);
            do
            {
                MaybeResizeBuffer(maxReadSize);

                _readLen += _internalSerialStream.Read(_inBuffer, _readLen, maxReadSize);
                justRead = ReadBufferIntoChars(buffer, offset, count, countMultiByteCharsAsOne);
                if (justRead > 0)
                {
                    return justRead;
                }
            } while (timeout == InfiniteTimeout || (timeout - GetElapsedTime(Environment.TickCount, startTicks) > 0));

            // must've timed out w/o getting a character.
            throw new TimeoutException();
        }

        // ReadBufferIntoChars reads from Serial Port's inBuffer up to *count* chars and
        // places them in *buffer* starting at *offset*.
        // This does not call any stream Reads, and so takes "no time".
        // If the buffer specified is insufficient to accommodate surrogate characters
        // the call to underlying Decoder.GetChars will throw argexc.
        private int ReadBufferIntoChars(char[] buffer, int offset, int count, bool countMultiByteCharsAsOne)
        {
            Debug.Assert(count != 0, "Count should never be zero.  We will probably see bugs further down if count is 0.");

            int bytesToRead = Math.Min(count, CachedBytesToRead);

            // There are lots of checks to determine if this really is a single byte encoding with no
            // funky fallbacks that would make it not single byte
            DecoderReplacementFallback fallback = _encoding.DecoderFallback as DecoderReplacementFallback;
            if (_encoding.IsSingleByte && _encoding.GetMaxCharCount(bytesToRead) == bytesToRead &&
                fallback != null && fallback.MaxCharCount == 1)
            {
                // kill ASCII/ANSI encoding easily.
                // read at least one and at most *count* characters
                _decoder.GetChars(_inBuffer, _readPos, bytesToRead, buffer, offset);

                _readPos += bytesToRead;
                if (_readPos == _readLen) _readPos = _readLen = 0;
                return bytesToRead;
            }
            else
            {
                // We want to turn inBuffer into at most count chars.  This algorithm basically works like this:
                //
                // 1) Take the largest step possible that won't give us too many chars
                // 2) If we find some chars, walk backwards until we find exactly how many bytes
                //    they occupy.  lastFullCharPos points to the end of the full chars.
                // 3) if we don't have enough chars for the buffer, goto #1

                int totalBytesExamined = 0;     // total number of Bytes in inBuffer we've looked at
                int totalCharsFound = 0;        // total number of chars we've found in inBuffer, totalCharsFound <= totalBytesExamined
                int currentBytesToExamine;      // the number of additional bytes to examine for characters
                int currentCharsFound;          // the number of additional chars found after examining currentBytesToExamine extra bytes
                int lastFullCharPos = _readPos; // first index AFTER last full char read, capped at ReadLen.
                do
                {
                    currentBytesToExamine = Math.Min(count - totalCharsFound, _readLen - _readPos - totalBytesExamined);
                    if (currentBytesToExamine <= 0)
                        break;

                    totalBytesExamined += currentBytesToExamine;

                    // recalculate currentBytesToExamine so that it includes leftover bytes from the last iteration.
                    currentBytesToExamine = _readPos + totalBytesExamined - lastFullCharPos;

                    // make sure we don't go beyond the end of the valid data that we have.
                    Debug.Assert((lastFullCharPos + currentBytesToExamine) <= _readLen, "We should never be attempting to read more bytes than we have");

                    currentCharsFound = _decoder.GetCharCount(_inBuffer, lastFullCharPos, currentBytesToExamine);

                    if (currentCharsFound > 0)
                    {
                        if ((totalCharsFound + currentCharsFound) > count)
                        {
                            // Multibyte unicode sequence (possibly surrogate chars)
                            // at the end of the buffer. We should not split the sequence,
                            // instead return with less chars now and defer reading them
                            // until next time
                            if (!countMultiByteCharsAsOne)
                                break;

                            // If we are here it is from ReadTo which attempts to read one logical character
                            // at a time. The supplied singleCharBuffer should be large enough to accommodate
                            // this multi-byte char
                            Debug.Assert((buffer.Length - offset - totalCharsFound) >= currentCharsFound, "internal buffer to read one full unicode char sequence is not sufficient!");
                        }

                        // go backwards until we know we have a full set of currentCharsFound bytes with no extra lead-bytes.
                        int foundCharsByteLength = currentBytesToExamine;
                        do
                        {
                            foundCharsByteLength--;
                        } while (_decoder.GetCharCount(_inBuffer, lastFullCharPos, foundCharsByteLength) == currentCharsFound);

                        // Fill into destination buffer all the COMPLETE characters we've read.
                        // If the buffer specified is insufficient to accommodate surrogate character
                        // the call to underlying Decoder.GetChars will throw argexc. We need not
                        // deal with this exc because we have not altered readPos yet.
                        _decoder.GetChars(_inBuffer, lastFullCharPos, foundCharsByteLength + 1, buffer, offset + totalCharsFound);
                        lastFullCharPos = lastFullCharPos + foundCharsByteLength + 1; // update the end position of last known char.
                    }

                    totalCharsFound += currentCharsFound;
                } while ((totalCharsFound < count) && (totalBytesExamined < CachedBytesToRead));

                _readPos = lastFullCharPos;

                if (_readPos == _readLen) _readPos = _readLen = 0;
                return totalCharsFound;
            }
        }

        public int ReadByte()
        {
            if (!IsOpen)
                throw new InvalidOperationException(SR.Port_not_open);
            if (_readLen != _readPos)                // stuff left in buffer, so we can read from it
                return _inBuffer[_readPos++];

            _decoder.Reset();
            return _internalSerialStream.ReadByte(); // otherwise, ask the stream.
        }

        public string ReadExisting()
        {
            if (!IsOpen)
                throw new InvalidOperationException(SR.Port_not_open);

            byte[] bytesReceived = new byte[BytesToRead];

            if (_readPos < _readLen)
            {
                // stuff in internal buffer
                Buffer.BlockCopy(_inBuffer, _readPos, bytesReceived, 0, CachedBytesToRead);
            }

            _internalSerialStream.Read(bytesReceived, CachedBytesToRead, bytesReceived.Length - (CachedBytesToRead));    // get everything

            // Read full characters and leave partial input in the buffer. Encoding.GetCharCount doesn't work because
            // it returns fallback characters on partial input, meaning that it overcounts. Instead, we use
            // GetCharCount from the decoder and tell it to preserve state, so that it returns the count of full
            // characters. Note that we don't actually want it to preserve state, so we call the decoder as if it's
            // preserving state and then call Reset in between calls. This uses a local decoder instead of the class
            // member decoder because that one may preserve state across SerialPort method calls.
            Decoder localDecoder = Encoding.GetDecoder();
            int numCharsReceived = localDecoder.GetCharCount(bytesReceived, 0, bytesReceived.Length);
            int lastFullCharIndex = bytesReceived.Length;

            if (numCharsReceived == 0)
            {
                Buffer.BlockCopy(bytesReceived, 0, _inBuffer, 0, bytesReceived.Length); // put it all back!
                // don't change readPos. --> readPos == 0?
                _readPos = 0;
                _readLen = bytesReceived.Length;
                return "";
            }

            do
            {
                localDecoder.Reset();
                lastFullCharIndex--;
            } while (localDecoder.GetCharCount(bytesReceived, 0, lastFullCharIndex) == numCharsReceived);

            _readPos = 0;
            _readLen = bytesReceived.Length - (lastFullCharIndex + 1);

            Buffer.BlockCopy(bytesReceived, lastFullCharIndex + 1, _inBuffer, 0, bytesReceived.Length - (lastFullCharIndex + 1));
            return Encoding.GetString(bytesReceived, 0, lastFullCharIndex + 1);
        }

        public string ReadLine()
        {
            return ReadTo(NewLine);
        }

        public string ReadTo(string value)
        {
            if (!IsOpen)
                throw new InvalidOperationException(SR.Port_not_open);
            if (value == null)
                throw new ArgumentNullException(nameof(value));
            if (value.Length == 0)
                throw new ArgumentException(SR.Format(SR.InvalidNullEmptyArgument, nameof(value)), nameof(value));

            int startTicks = Environment.TickCount;
            int numCharsRead;
            int timeUsed = 0;
            int timeNow;
            StringBuilder currentLine = new StringBuilder();
            char lastValueChar = value[value.Length - 1];

            // for timeout issues, best to read everything already on the stream into our buffers.
            // first make sure inBuffer is big enough
            int bytesInStream = _internalSerialStream.BytesToRead;
            MaybeResizeBuffer(bytesInStream);

            _readLen += _internalSerialStream.Read(_inBuffer, _readLen, bytesInStream);
            int beginReadPos = _readPos;

            if (_singleCharBuffer == null)
            {
                // This is somewhat of an approximate guesstimate to get the max char[] size needed to encode a single character
                _singleCharBuffer = new char[_maxByteCountForSingleChar];
            }

            try
            {
                while (true)
                {
                    if (_readTimeout == InfiniteTimeout)
                    {
                        numCharsRead = InternalRead(_singleCharBuffer, 0, 1, _readTimeout, true);
                    }
                    else if (_readTimeout - timeUsed >= 0)
                    {
                        timeNow = Environment.TickCount;
                        numCharsRead = InternalRead(_singleCharBuffer, 0, 1, _readTimeout - timeUsed, true);
                        timeUsed += Environment.TickCount - timeNow;
                    }
                    else
                        throw new TimeoutException();

#if DEBUG
                    if (numCharsRead > 1)
                    {
                        for (int i = 0; i < numCharsRead; i++)
                            Debug.Assert((Char.IsSurrogate(_singleCharBuffer[i])), "number of chars read should be more than one only for surrogate characters!");
                    }
#endif
                    Debug.Assert((numCharsRead > 0), "possible bug in ReadBufferIntoChars, reading surrogate char?");
                    currentLine.Append(_singleCharBuffer, 0, numCharsRead);

                    if (lastValueChar == (char)_singleCharBuffer[numCharsRead - 1] && (currentLine.Length >= value.Length))
                    {
                        // we found the last char in the value string.  See if the rest is there.  No need to
                        // recompare the last char of the value string.
                        bool found = true;
                        for (int i = 2; i <= value.Length; i++)
                        {
                            if (value[value.Length - i] != currentLine[currentLine.Length - i])
                            {
                                found = false;
                                break;
                            }
                        }

                        if (found)
                        {
                            // we found the search string.  Exclude it from the return string.
                            string ret = currentLine.ToString(0, currentLine.Length - value.Length);
                            if (_readPos == _readLen) _readPos = _readLen = 0;
                            return ret;
                        }
                    }
                }
            }
            catch
            {
                // We probably got here due to timeout.
                // We will try our best to restore the internal states, it's tricky!

                // 0) Save any existing data
                // 1) Restore readPos to the original position upon entering ReadTo
                // 2) Set readLen to the number of bytes read since entering ReadTo
                // 3) Restore inBuffer so that it contains the bytes from currentLine, resizing if necessary.
                // 4) Append the buffer with any saved data from 0)

                byte[] readBuffer = _encoding.GetBytes(currentLine.ToString());

                // We will compact the data by default
                if (readBuffer.Length > 0)
                {
                    int bytesToSave = CachedBytesToRead;
                    byte[] savBuffer = new byte[bytesToSave];

                    if (bytesToSave > 0)
                        Buffer.BlockCopy(_inBuffer, _readPos, savBuffer, 0, bytesToSave);

                    _readPos = 0;
                    _readLen = 0;

                    MaybeResizeBuffer(readBuffer.Length + bytesToSave);

                    Buffer.BlockCopy(readBuffer, 0, _inBuffer, _readLen, readBuffer.Length);
                    _readLen += readBuffer.Length;

                    if (bytesToSave > 0)
                    {
                        Buffer.BlockCopy(savBuffer, 0, _inBuffer, _readLen, bytesToSave);
                        _readLen += bytesToSave;
                    }
                }

                throw;
            }
        }

        // Writes string to output, no matter string's length.
        public void Write(string text)
        {
            if (!IsOpen)
                throw new InvalidOperationException(SR.Port_not_open);
            if (text == null)
                throw new ArgumentNullException(nameof(text));
            if (text.Length == 0) return;
            byte[] bytesToWrite;

            bytesToWrite = _encoding.GetBytes(text);

            _internalSerialStream.Write(bytesToWrite, 0, bytesToWrite.Length, _writeTimeout);
        }

        // encoding-dependent Write-chars method.
        // Probably as performant as direct conversion from ASCII to bytes, since we have to cast anyway (we can just call GetBytes)
        public void Write(char[] buffer, int offset, int count)
        {
            if (!IsOpen)
                throw new InvalidOperationException(SR.Port_not_open);
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));
            if (offset < 0)
                throw new ArgumentOutOfRangeException(nameof(offset), SR.ArgumentOutOfRange_NeedNonNegNumRequired);
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count), SR.ArgumentOutOfRange_NeedNonNegNumRequired);
            if (buffer.Length - offset < count)
                throw new ArgumentException(SR.Argument_InvalidOffLen);

            if (buffer.Length == 0) return;

            byte[] byteArray = Encoding.GetBytes(buffer, offset, count);
            Write(byteArray, 0, byteArray.Length);
        }

        // Writes a specified section of a byte buffer to output.
        public void Write(byte[] buffer, int offset, int count)
        {
            if (!IsOpen)
                throw new InvalidOperationException(SR.Port_not_open);
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));
            if (offset < 0)
                throw new ArgumentOutOfRangeException(nameof(offset), SR.ArgumentOutOfRange_NeedNonNegNumRequired);
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count), SR.ArgumentOutOfRange_NeedNonNegNumRequired);
            if (buffer.Length - offset < count)
                throw new ArgumentException(SR.Argument_InvalidOffLen);
            if (buffer.Length == 0) return;

            _internalSerialStream.Write(buffer, offset, count, _writeTimeout);
        }

        public void WriteLine(string text)
        {
            Write(text + NewLine);
        }

#pragma warning disable CA2002
        // ----- SECTION: internal utility methods ----------------*

        // included here just to use the event filter to block unwanted invocations of the Serial Port's events.
        // Plus, this enforces the requirement on the received event that the number of buffered bytes >= receivedBytesThreshold
        private void CatchErrorEvents(object src, SerialErrorReceivedEventArgs e)
        {
            SerialErrorReceivedEventHandler eventHandler = ErrorReceived;
            SerialStream stream = _internalSerialStream;

            if ((eventHandler != null) && (stream != null))
            {
                lock (stream)
                {
                    if (stream.IsOpen)
                        eventHandler(this, e);
                }
            }
        }

        private void CatchPinChangedEvents(object src, SerialPinChangedEventArgs e)
        {
            SerialPinChangedEventHandler eventHandler = PinChanged;
            SerialStream stream = _internalSerialStream;

            if ((eventHandler != null) && (stream != null))
            {
                lock (stream)
                {
                    if (stream.IsOpen)
                        eventHandler(this, e);
                }
            }
        }

        private void CatchReceivedEvents(object src, SerialDataReceivedEventArgs e)
        {
            SerialDataReceivedEventHandler eventHandler = _dataReceived;
            SerialStream stream = _internalSerialStream;

            if ((eventHandler != null) && (stream != null))
            {
                lock (stream)
                {
                    // SerialStream might be closed between the time the event runner
                    // pumped this event and the time the threadpool thread end up
                    // invoking this event handler. The above lock and IsOpen check
                    // ensures that we raise the event only when the port is open

                    bool raiseEvent = false;
                    try
                    {
                        raiseEvent = stream.IsOpen && (SerialData.Eof == e.EventType || BytesToRead >= _receivedBytesThreshold);
                    }
                    catch
                    {
                        // Ignore and continue. SerialPort might have been closed already!
                    }
                    finally
                    {
                        // ISSUE: This should be fired only when it wasn't already fired for the total number of bytes available
                        //        Similarly as done in SerialStream.Linux (IOLoop)
                        //        I.e: Let _receivedBytesThreshold be 8 - when we get an event when 7 bytes are available
                        //        BytesToRead can change while we run this event and thus
                        //        we virtually can get 2 events when 8th byte arrives
                        //        I.e. we might want to add total bytes available as internal field in the args event
                        if (raiseEvent)
                            eventHandler(this, e);  // here, do your reading, etc.
                    }
                }
            }
        }
#pragma warning restore CA2002

        private void CompactBuffer()
        {
            Buffer.BlockCopy(_inBuffer, _readPos, _inBuffer, 0, CachedBytesToRead);
            _readLen = CachedBytesToRead;
            _readPos = 0;
        }

        // This method guarantees that our inBuffer is big enough.  The parameter passed in is
        // the number of bytes that our code is going to add to inBuffer.  MaybeResizeBuffer will
        // do one of three things depending on how much data is already in the buffer and how
        // much will be added:
        // 1) Nothing.  The current buffer is big enough to hold it all
        // 2) Compact the existing data and keep the current buffer.
        // 3) Create a new, larger buffer and compact the existing data into it.
        private void MaybeResizeBuffer(int additionalByteLength)
        {
            // Case 1.  No action needed
            if (additionalByteLength + _readLen <= _inBuffer.Length)
                return;

            // Case 2.  Compact
            if (CachedBytesToRead + additionalByteLength <= _inBuffer.Length / 2)
                CompactBuffer();
            else
            {
                // Case 3.  Create a new buffer
                int newLength = Math.Max(CachedBytesToRead + additionalByteLength, _inBuffer.Length * 2);

                Debug.Assert(_inBuffer.Length >= _readLen, "ResizeBuffer - readLen > inBuffer.Length");
                byte[] newBuffer = new byte[newLength];
                // only copy the valid data from inBuffer, and put it at the beginning of newBuffer.
                Buffer.BlockCopy(_inBuffer, _readPos, newBuffer, 0, CachedBytesToRead);
                _readLen = CachedBytesToRead;
                _readPos = 0;
                _inBuffer = newBuffer;
            }
        }

        private static int GetElapsedTime(int currentTickCount, int startTickCount)
        {
            int elapsedTime = unchecked(currentTickCount - startTickCount);
            return (elapsedTime >= 0) ? (int)elapsedTime : int.MaxValue;
        }
    }
}
