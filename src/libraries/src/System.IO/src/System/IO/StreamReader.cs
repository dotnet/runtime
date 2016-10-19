// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Diagnostics.Contracts;

namespace System.IO
{
    // This class implements a TextReader for reading characters to a Stream.
    // This is designed for character input in a particular Encoding, 
    // whereas the Stream class is designed for byte input and output.  
    [Serializable]
    public class StreamReader : TextReader
    {
        // StreamReader.Null is threadsafe.
        public new static readonly StreamReader Null = new NullStreamReader();

        // Encoding.GetPreamble() always allocates and returns a new byte[] array for
        // encodings that have a preamble.
        // We can avoid repeated allocations for the default and commonly used Encoding.UTF8
        // encoding by using our own private cached instance of the UTF8 preamble.
        // This is lazily allocated the first time it is used.
        private static byte[] s_utf8Preamble;

        // Using a 1K byte buffer and a 4K FileStream buffer works out pretty well
        // perf-wise.  On even a 40 MB text file, any perf loss by using a 4K
        // buffer is negated by the win of allocating a smaller byte[], which 
        // saves construction time.  This does break adaptive buffering,
        // but this is slightly faster.
        internal const int DefaultBufferSize = 1024;  // Byte buffer size
        private const int DefaultFileStreamBufferSize = 4096;
        private const int MinBufferSize = 128;

        private Stream _stream;
        private Encoding _encoding;
        private Decoder _decoder;
        private byte[] _byteBuffer;
        private char[] _charBuffer;
        private byte[] _preamble;   // Encoding's preamble, which identifies this encoding.
        private int _charPos;
        private int _charLen;
        // Record the number of valid bytes in the byteBuffer, for a few checks.
        private int _byteLen;
        // This is used only for preamble detection
        private int _bytePos;

        // This is the maximum number of chars we can get from one call to 
        // ReadBuffer.  Used so ReadBuffer can tell when to copy data into
        // a user's char[] directly, instead of our internal char[].
        private int _maxCharsPerBuffer;

        // We will support looking for byte order marks in the stream and trying
        // to decide what the encoding might be from the byte order marks, IF they
        // exist.  But that's all we'll do.  
        private bool _detectEncoding;

        // Whether we must still check for the encoding's given preamble at the
        // beginning of this file.
        private bool _checkPreamble;

        // Whether the stream is most likely not going to give us back as much 
        // data as we want the next time we call it.  We must do the computation
        // before we do any byte order mark handling and save the result.  Note
        // that we need this to allow users to handle streams used for an 
        // interactive protocol, where they block waiting for the remote end 
        // to send a response, like logging in on a Unix machine.
        private bool _isBlocked;

        // The intent of this field is to leave open the underlying stream when 
        // disposing of this StreamReader.  A name like _leaveOpen is better, 
        // but this type is serializable, and this field's name was _closable.
        private bool _closable;  // Whether to close the underlying stream.

        // We don't guarantee thread safety on StreamReader, but we should at 
        // least prevent users from trying to read anything while an Async
        // read from the same thread is in progress.
        [NonSerialized]
        private volatile Task _asyncReadTask;

        private void CheckAsyncTaskInProgress()
        {
            // We are not locking the access to _asyncReadTask because this is not meant to guarantee thread safety. 
            // We are simply trying to deter calling any Read APIs while an async Read from the same thread is in progress.

            Task t = _asyncReadTask;

            if (t != null && !t.IsCompleted)
            {
                throw new InvalidOperationException(SR.InvalidOperation_AsyncIOInProgress);
            }
        }

        // StreamReader by default will ignore illegal UTF8 characters. We don't want to 
        // throw here because we want to be able to read ill-formed data without choking. 
        // The high level goal is to be tolerant of encoding errors when we read and very strict 
        // when we write. Hence, default StreamWriter encoding will throw on error.   

        internal StreamReader()
        {
        }

        public StreamReader(Stream stream)
            : this(stream, true)
        {
        }

        public StreamReader(Stream stream, bool detectEncodingFromByteOrderMarks)
            : this(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks, DefaultBufferSize, false)
        {
        }

        public StreamReader(Stream stream, Encoding encoding)
            : this(stream, encoding, true, DefaultBufferSize, false)
        {
        }

        public StreamReader(Stream stream, Encoding encoding, bool detectEncodingFromByteOrderMarks)
            : this(stream, encoding, detectEncodingFromByteOrderMarks, DefaultBufferSize, false)
        {
        }

        // Creates a new StreamReader for the given stream.  The 
        // character encoding is set by encoding and the buffer size, 
        // in number of 16-bit characters, is set by bufferSize.  
        // 
        // Note that detectEncodingFromByteOrderMarks is a very
        // loose attempt at detecting the encoding by looking at the first
        // 3 bytes of the stream.  It will recognize UTF-8, little endian
        // unicode, and big endian unicode text, but that's it.  If neither
        // of those three match, it will use the Encoding you provided.
        // 
        public StreamReader(Stream stream, Encoding encoding, bool detectEncodingFromByteOrderMarks, int bufferSize)
            : this(stream, encoding, detectEncodingFromByteOrderMarks, bufferSize, false)
        {
        }

        public StreamReader(Stream stream, Encoding encoding, bool detectEncodingFromByteOrderMarks, int bufferSize, bool leaveOpen)
        {
            if (stream == null || encoding == null)
            {
                throw new ArgumentNullException(stream == null ? nameof(stream) : nameof(encoding));
            }
            if (!stream.CanRead)
            {
                throw new ArgumentException(SR.Argument_StreamNotReadable);
            }
            if (bufferSize <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(bufferSize), SR.ArgumentOutOfRange_NeedPosNum);
            }

            Init(stream, encoding, detectEncodingFromByteOrderMarks, bufferSize, leaveOpen);
        }

        public StreamReader(string path)
            : this(path, true)
        {
        }

        public StreamReader(string path, bool detectEncodingFromByteOrderMarks)
            : this(path, Encoding.UTF8, detectEncodingFromByteOrderMarks, DefaultBufferSize)
        {
        }

        public StreamReader(string path, Encoding encoding)
            : this(path, encoding, true, DefaultBufferSize)
        {
        }

        public StreamReader(string path, Encoding encoding, bool detectEncodingFromByteOrderMarks)
            : this(path, encoding, detectEncodingFromByteOrderMarks, DefaultBufferSize)
        {
        }

        public StreamReader(string path, Encoding encoding, bool detectEncodingFromByteOrderMarks, int bufferSize)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));
            if (encoding == null)
                throw new ArgumentNullException(nameof(encoding));
            if (path.Length == 0)
                throw new ArgumentException(SR.Argument_EmptyPath);
            if (bufferSize <= 0)
                throw new ArgumentOutOfRangeException(nameof(bufferSize), SR.ArgumentOutOfRange_NeedPosNum);

            Stream stream = FileStreamHelpers.CreateFileStream(path, write: false, append: false);
            Init(stream, encoding, detectEncodingFromByteOrderMarks, bufferSize, leaveOpen: false);
        }

        private void Init(Stream stream, Encoding encoding, bool detectEncodingFromByteOrderMarks, int bufferSize, bool leaveOpen)
        {
            _stream = stream;
            _encoding = encoding;
            _decoder = encoding.GetDecoder();
            if (bufferSize < MinBufferSize)
            {
                bufferSize = MinBufferSize;
            }

            _byteBuffer = new byte[bufferSize];
            _maxCharsPerBuffer = encoding.GetMaxCharCount(bufferSize);
            _charBuffer = new char[_maxCharsPerBuffer];
            _byteLen = 0;
            _bytePos = 0;
            _detectEncoding = detectEncodingFromByteOrderMarks;

            // Encoding.GetPreamble() always allocates and returns a new byte[] array for
            // encodings that have a preamble.
            // We can avoid repeated allocations for the default and commonly used Encoding.UTF8
            // encoding by using our own private cached instance of the UTF8 preamble.
            // We specifically look for Encoding.UTF8 because we know it has a preamble,
            // whereas other instances of UTF8Encoding may not have a preamble enabled, and
            // there's no public way to tell if the preamble is enabled for an instance other
            // than calling GetPreamble(), which we're trying to avoid.
            // This means that other instances of UTF8Encoding are excluded from this optimization.
            _preamble = object.ReferenceEquals(encoding, Encoding.UTF8) ?
                (s_utf8Preamble ?? (s_utf8Preamble = encoding.GetPreamble())) :
                encoding.GetPreamble();

            _checkPreamble = (_preamble.Length > 0);
            _isBlocked = false;
            _closable = !leaveOpen;
        }

        // Init used by NullStreamReader, to delay load encoding
        internal void Init(Stream stream)
        {
            _stream = stream;
            _closable = true;
        }

        public override void Close()
        {
            Dispose(true);
        }

        protected override void Dispose(bool disposing)
        {
            // Dispose of our resources if this StreamReader is closable.
            // Note that Console.In should be left open.
            try
            {
                // Note that Stream.Close() can potentially throw here. So we need to 
                // ensure cleaning up internal resources, inside the finally block.  
                if (!LeaveOpen && disposing && (_stream != null))
                {
                    _stream.Close();
                }
            }
            finally
            {
                if (!LeaveOpen && (_stream != null))
                {
                    _stream = null;
                    _encoding = null;
                    _decoder = null;
                    _byteBuffer = null;
                    _charBuffer = null;
                    _charPos = 0;
                    _charLen = 0;
                    base.Dispose(disposing);
                }
            }
        }

        public virtual Encoding CurrentEncoding
        {
            get { return _encoding; }
        }

        public virtual Stream BaseStream
        {
            get { return _stream; }
        }

        internal bool LeaveOpen
        {
            get { return !_closable; }
        }

        // DiscardBufferedData tells StreamReader to throw away its internal
        // buffer contents.  This is useful if the user needs to seek on the
        // underlying stream to a known location then wants the StreamReader
        // to start reading from this new point.  This method should be called
        // very sparingly, if ever, since it can lead to very poor performance.
        // However, it may be the only way of handling some scenarios where 
        // users need to re-read the contents of a StreamReader a second time.
        public void DiscardBufferedData()
        {
            CheckAsyncTaskInProgress();

            _byteLen = 0;
            _charLen = 0;
            _charPos = 0;
            // in general we'd like to have an invariant that encoding isn't null. However,
            // for startup improvements for NullStreamReader, we want to delay load encoding. 
            if (_encoding != null)
            {
                _decoder = _encoding.GetDecoder();
            }
            _isBlocked = false;
        }

        public bool EndOfStream
        {
            get
            {
                if (_stream == null)
                {
                    throw new ObjectDisposedException(null, SR.ObjectDisposed_ReaderClosed);
                }

                CheckAsyncTaskInProgress();

                if (_charPos < _charLen)
                {
                    return false;
                }

                // This may block on pipes!
                int numRead = ReadBuffer();
                return numRead == 0;
            }
        }

        [Pure]
        public override int Peek()
        {
            if (_stream == null)
            {
                throw new ObjectDisposedException(null, SR.ObjectDisposed_ReaderClosed);
            }

            CheckAsyncTaskInProgress();

            if (_charPos == _charLen)
            {
                if (_isBlocked || ReadBuffer() == 0)
                {
                    return -1;
                }
            }
            return _charBuffer[_charPos];
        }

        public override int Read()
        {
            if (_stream == null)
            {
                throw new ObjectDisposedException(null, SR.ObjectDisposed_ReaderClosed);
            }

            CheckAsyncTaskInProgress();

            if (_charPos == _charLen)
            {
                if (ReadBuffer() == 0)
                {
                    return -1;
                }
            }
            int result = _charBuffer[_charPos];
            _charPos++;
            return result;
        }

        public override int Read(char[] buffer, int index, int count)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer), SR.ArgumentNull_Buffer);
            }
            if (index < 0 || count < 0)
            {
                throw new ArgumentOutOfRangeException(index < 0 ? nameof(index) : nameof(count), SR.ArgumentOutOfRange_NeedNonNegNum);
            }
            if (buffer.Length - index < count)
            {
                throw new ArgumentException(SR.Argument_InvalidOffLen);
            }

            if (_stream == null)
            {
                throw new ObjectDisposedException(null, SR.ObjectDisposed_ReaderClosed);
            }

            CheckAsyncTaskInProgress();

            int charsRead = 0;
            // As a perf optimization, if we had exactly one buffer's worth of 
            // data read in, let's try writing directly to the user's buffer.
            bool readToUserBuffer = false;
            while (count > 0)
            {
                int n = _charLen - _charPos;
                if (n == 0)
                {
                    n = ReadBuffer(buffer, index + charsRead, count, out readToUserBuffer);
                }
                if (n == 0)
                {
                    break;  // We're at EOF
                }
                if (n > count)
                {
                    n = count;
                }
                if (!readToUserBuffer)
                {
                    Buffer.BlockCopy(_charBuffer, _charPos * 2, buffer, (index + charsRead) * 2, n * 2);
                    _charPos += n;
                }

                charsRead += n;
                count -= n;
                // This function shouldn't block for an indefinite amount of time,
                // or reading from a network stream won't work right.  If we got
                // fewer bytes than we requested, then we want to break right here.
                if (_isBlocked)
                {
                    break;
                }
            }

            return charsRead;
        }

        public override string ReadToEnd()
        {
            if (_stream == null)
            {
                throw new ObjectDisposedException(null, SR.ObjectDisposed_ReaderClosed);
            }

            CheckAsyncTaskInProgress();

            // Call ReadBuffer, then pull data out of charBuffer.
            StringBuilder sb = new StringBuilder(_charLen - _charPos);
            do
            {
                sb.Append(_charBuffer, _charPos, _charLen - _charPos);
                _charPos = _charLen;  // Note we consumed these characters
                ReadBuffer();
            } while (_charLen > 0);
            return sb.ToString();
        }

        public override int ReadBlock(char[] buffer, int index, int count)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer), SR.ArgumentNull_Buffer);
            }
            if (index < 0 || count < 0)
            {
                throw new ArgumentOutOfRangeException(index < 0 ? nameof(index) : nameof(count), SR.ArgumentOutOfRange_NeedNonNegNum);
            }
            if (buffer.Length - index < count)
            {
                throw new ArgumentException(SR.Argument_InvalidOffLen);
            }
            if (_stream == null)
            {
                throw new ObjectDisposedException(null, SR.ObjectDisposed_ReaderClosed);
            }

            CheckAsyncTaskInProgress();

            return base.ReadBlock(buffer, index, count);
        }

        // Trims n bytes from the front of the buffer.
        private void CompressBuffer(int n)
        {
            Debug.Assert(_byteLen >= n, "CompressBuffer was called with a number of bytes greater than the current buffer length.  Are two threads using this StreamReader at the same time?");
            Buffer.BlockCopy(_byteBuffer, n, _byteBuffer, 0, _byteLen - n);
            _byteLen -= n;
        }

        private void DetectEncoding()
        {
            if (_byteLen < 2)
            {
                return;
            }
            _detectEncoding = false;
            bool changedEncoding = false;
            if (_byteBuffer[0] == 0xFE && _byteBuffer[1] == 0xFF)
            {
                // Big Endian Unicode

                _encoding = Encoding.BigEndianUnicode;
                CompressBuffer(2);
                changedEncoding = true;
            }

            else if (_byteBuffer[0] == 0xFF && _byteBuffer[1] == 0xFE)
            {
                // Little Endian Unicode, or possibly little endian UTF32
                if (_byteLen < 4 || _byteBuffer[2] != 0 || _byteBuffer[3] != 0)
                {
                    _encoding = Encoding.Unicode;
                    CompressBuffer(2);
                    changedEncoding = true;
                }
                else
                {
                    _encoding = Encoding.UTF32;
                    CompressBuffer(4);
                    changedEncoding = true;
                }
            }

            else if (_byteLen >= 3 && _byteBuffer[0] == 0xEF && _byteBuffer[1] == 0xBB && _byteBuffer[2] == 0xBF)
            {
                // UTF-8
                _encoding = Encoding.UTF8;
                CompressBuffer(3);
                changedEncoding = true;
            }
            else if (_byteLen >= 4 && _byteBuffer[0] == 0 && _byteBuffer[1] == 0 &&
                _byteBuffer[2] == 0xFE && _byteBuffer[3] == 0xFF)
            {
                // Big Endian UTF32
                _encoding = new UTF32Encoding(bigEndian: true, byteOrderMark: true);
                CompressBuffer(4);
                changedEncoding = true;
            }
            else if (_byteLen == 2)
            {
                _detectEncoding = true;
            }
            // Note: in the future, if we change this algorithm significantly,
            // we can support checking for the preamble of the given encoding.

            if (changedEncoding)
            {
                _decoder = _encoding.GetDecoder();
                _maxCharsPerBuffer = _encoding.GetMaxCharCount(_byteBuffer.Length);
                _charBuffer = new char[_maxCharsPerBuffer];
            }
        }

        // Trims the preamble bytes from the byteBuffer. This routine can be called multiple times
        // and we will buffer the bytes read until the preamble is matched or we determine that
        // there is no match. If there is no match, every byte read previously will be available 
        // for further consumption. If there is a match, we will compress the buffer for the 
        // leading preamble bytes
        private bool IsPreamble()
        {
            if (!_checkPreamble)
            {
                return _checkPreamble;
            }

            Debug.Assert(_bytePos <= _preamble.Length, "_compressPreamble was called with the current bytePos greater than the preamble buffer length.  Are two threads using this StreamReader at the same time?");
            int len = (_byteLen >= (_preamble.Length)) ? (_preamble.Length - _bytePos) : (_byteLen - _bytePos);

            for (int i = 0; i < len; i++, _bytePos++)
            {
                if (_byteBuffer[_bytePos] != _preamble[_bytePos])
                {
                    _bytePos = 0;
                    _checkPreamble = false;
                    break;
                }
            }

            Debug.Assert(_bytePos <= _preamble.Length, "possible bug in _compressPreamble.  Are two threads using this StreamReader at the same time?");

            if (_checkPreamble)
            {
                if (_bytePos == _preamble.Length)
                {
                    // We have a match
                    CompressBuffer(_preamble.Length);
                    _bytePos = 0;
                    _checkPreamble = false;
                    _detectEncoding = false;
                }
            }

            return _checkPreamble;
        }

        internal virtual int ReadBuffer()
        {
            _charLen = 0;
            _charPos = 0;

            if (!_checkPreamble)
            {
                _byteLen = 0;
            }

            do
            {
                if (_checkPreamble)
                {
                    Debug.Assert(_bytePos <= _preamble.Length, "possible bug in _compressPreamble.  Are two threads using this StreamReader at the same time?");
                    int len = _stream.Read(_byteBuffer, _bytePos, _byteBuffer.Length - _bytePos);
                    Debug.Assert(len >= 0, "Stream.Read returned a negative number!  This is a bug in your stream class.");

                    if (len == 0)
                    {
                        // EOF but we might have buffered bytes from previous 
                        // attempt to detect preamble that needs to be decoded now
                        if (_byteLen > 0)
                        {
                            _charLen += _decoder.GetChars(_byteBuffer, 0, _byteLen, _charBuffer, _charLen);
                            // Need to zero out the byteLen after we consume these bytes so that we don't keep infinitely hitting this code path
                            _bytePos = _byteLen = 0;
                        }

                        return _charLen;
                    }

                    _byteLen += len;
                }
                else
                {
                    Debug.Assert(_bytePos == 0, "bytePos can be non zero only when we are trying to _checkPreamble.  Are two threads using this StreamReader at the same time?");
                    _byteLen = _stream.Read(_byteBuffer, 0, _byteBuffer.Length);
                    Debug.Assert(_byteLen >= 0, "Stream.Read returned a negative number!  This is a bug in your stream class.");

                    if (_byteLen == 0)  // We're at EOF
                    {
                        return _charLen;
                    }
                }

                // _isBlocked == whether we read fewer bytes than we asked for.
                // Note we must check it here because CompressBuffer or 
                // DetectEncoding will change byteLen.
                _isBlocked = (_byteLen < _byteBuffer.Length);

                // Check for preamble before detect encoding. This is not to override the
                // user supplied Encoding for the one we implicitly detect. The user could
                // customize the encoding which we will loose, such as ThrowOnError on UTF8
                if (IsPreamble())
                {
                    continue;
                }

                // If we're supposed to detect the encoding and haven't done so yet,
                // do it.  Note this may need to be called more than once.
                if (_detectEncoding && _byteLen >= 2)
                {
                    DetectEncoding();
                }

                _charLen += _decoder.GetChars(_byteBuffer, 0, _byteLen, _charBuffer, _charLen);
            } while (_charLen == 0);
            //Console.WriteLine("ReadBuffer called.  chars: "+charLen);
            return _charLen;
        }


        // This version has a perf optimization to decode data DIRECTLY into the 
        // user's buffer, bypassing StreamReader's own buffer.
        // This gives a > 20% perf improvement for our encodings across the board,
        // but only when asking for at least the number of characters that one
        // buffer's worth of bytes could produce.
        // This optimization, if run, will break SwitchEncoding, so we must not do 
        // this on the first call to ReadBuffer.  
        private int ReadBuffer(char[] userBuffer, int userOffset, int desiredChars, out bool readToUserBuffer)
        {
            _charLen = 0;
            _charPos = 0;

            if (!_checkPreamble)
            {
                _byteLen = 0;
            }

            int charsRead = 0;

            // As a perf optimization, we can decode characters DIRECTLY into a
            // user's char[].  We absolutely must not write more characters 
            // into the user's buffer than they asked for.  Calculating 
            // encoding.GetMaxCharCount(byteLen) each time is potentially very 
            // expensive - instead, cache the number of chars a full buffer's 
            // worth of data may produce.  Yes, this makes the perf optimization 
            // less aggressive, in that all reads that asked for fewer than AND 
            // returned fewer than _maxCharsPerBuffer chars won't get the user 
            // buffer optimization.  This affects reads where the end of the
            // Stream comes in the middle somewhere, and when you ask for 
            // fewer chars than your buffer could produce.
            readToUserBuffer = desiredChars >= _maxCharsPerBuffer;

            do
            {
                Debug.Assert(charsRead == 0);

                if (_checkPreamble)
                {
                    Debug.Assert(_bytePos <= _preamble.Length, "possible bug in _compressPreamble.  Are two threads using this StreamReader at the same time?");
                    int len = _stream.Read(_byteBuffer, _bytePos, _byteBuffer.Length - _bytePos);
                    Debug.Assert(len >= 0, "Stream.Read returned a negative number!  This is a bug in your stream class.");

                    if (len == 0)
                    {
                        // EOF but we might have buffered bytes from previous 
                        // attempt to detect preamble that needs to be decoded now
                        if (_byteLen > 0)
                        {
                            if (readToUserBuffer)
                            {
                                charsRead = _decoder.GetChars(_byteBuffer, 0, _byteLen, userBuffer, userOffset + charsRead);
                                _charLen = 0;  // StreamReader's buffer is empty.
                            }
                            else
                            {
                                charsRead = _decoder.GetChars(_byteBuffer, 0, _byteLen, _charBuffer, charsRead);
                                _charLen += charsRead;  // Number of chars in StreamReader's buffer.
                            }
                        }

                        return charsRead;
                    }

                    _byteLen += len;
                }
                else
                {
                    Debug.Assert(_bytePos == 0, "bytePos can be non zero only when we are trying to _checkPreamble.  Are two threads using this StreamReader at the same time?");

                    _byteLen = _stream.Read(_byteBuffer, 0, _byteBuffer.Length);

                    Debug.Assert(_byteLen >= 0, "Stream.Read returned a negative number!  This is a bug in your stream class.");

                    if (_byteLen == 0)  // EOF
                    {
                        break;
                    }
                }

                // _isBlocked == whether we read fewer bytes than we asked for.
                // Note we must check it here because CompressBuffer or 
                // DetectEncoding will change byteLen.
                _isBlocked = (_byteLen < _byteBuffer.Length);

                // Check for preamble before detect encoding. This is not to override the
                // user supplied Encoding for the one we implicitly detect. The user could
                // customize the encoding which we will loose, such as ThrowOnError on UTF8
                // Note: we don't need to recompute readToUserBuffer optimization as IsPreamble
                // doesn't change the encoding or affect _maxCharsPerBuffer
                if (IsPreamble())
                {
                    continue;
                }

                // On the first call to ReadBuffer, if we're supposed to detect the encoding, do it.
                if (_detectEncoding && _byteLen >= 2)
                {
                    DetectEncoding();
                    // DetectEncoding changes some buffer state.  Recompute this.
                    readToUserBuffer = desiredChars >= _maxCharsPerBuffer;
                }

                _charPos = 0;
                if (readToUserBuffer)
                {
                    charsRead += _decoder.GetChars(_byteBuffer, 0, _byteLen, userBuffer, userOffset + charsRead);
                    _charLen = 0;  // StreamReader's buffer is empty.
                }
                else
                {
                    charsRead = _decoder.GetChars(_byteBuffer, 0, _byteLen, _charBuffer, charsRead);
                    _charLen += charsRead;  // Number of chars in StreamReader's buffer.
                }
            } while (charsRead == 0);

            _isBlocked &= charsRead < desiredChars;

            //Console.WriteLine("ReadBuffer: charsRead: "+charsRead+"  readToUserBuffer: "+readToUserBuffer);
            return charsRead;
        }


        // Reads a line. A line is defined as a sequence of characters followed by
        // a carriage return ('\r'), a line feed ('\n'), or a carriage return
        // immediately followed by a line feed. The resulting string does not
        // contain the terminating carriage return and/or line feed. The returned
        // value is null if the end of the input stream has been reached.
        //
        public override string ReadLine()
        {
            if (_stream == null)
            {
                throw new ObjectDisposedException(null, SR.ObjectDisposed_ReaderClosed);
            }

            CheckAsyncTaskInProgress();

            if (_charPos == _charLen)
            {
                if (ReadBuffer() == 0)
                {
                    return null;
                }
            }

            StringBuilder sb = null;
            do
            {
                int i = _charPos;
                do
                {
                    char ch = _charBuffer[i];
                    // Note the following common line feed chars:
                    // \n - UNIX   \r\n - DOS   \r - Mac
                    if (ch == '\r' || ch == '\n')
                    {
                        string s;
                        if (sb != null)
                        {
                            sb.Append(_charBuffer, _charPos, i - _charPos);
                            s = sb.ToString();
                        }
                        else
                        {
                            s = new string(_charBuffer, _charPos, i - _charPos);
                        }
                        _charPos = i + 1;
                        if (ch == '\r' && (_charPos < _charLen || ReadBuffer() > 0))
                        {
                            if (_charBuffer[_charPos] == '\n')
                            {
                                _charPos++;
                            }
                        }
                        return s;
                    }
                    i++;
                } while (i < _charLen);
                i = _charLen - _charPos;
                if (sb == null)
                {
                    sb = new StringBuilder(i + 80);
                }
                sb.Append(_charBuffer, _charPos, i);
            } while (ReadBuffer() > 0);
            return sb.ToString();
        }

        #region Task based Async APIs
        public override Task<string> ReadLineAsync()
        {
            // If we have been inherited into a subclass, the following implementation could be incorrect
            // since it does not call through to Read() which a subclass might have overridden.  
            // To be safe we will only use this implementation in cases where we know it is safe to do so,
            // and delegate to our base class (which will call into Read) when we are not sure.
            if (GetType() != typeof(StreamReader))
            {
                return base.ReadLineAsync();
            }

            if (_stream == null)
            {
                throw new ObjectDisposedException(null, SR.ObjectDisposed_ReaderClosed);
            }

            CheckAsyncTaskInProgress();

            Task<string> task = ReadLineAsyncInternal();
            _asyncReadTask = task;

            return task;
        }

        private async Task<string> ReadLineAsyncInternal()
        {
            if (CharPos_Prop == CharLen_Prop && (await ReadBufferAsync().ConfigureAwait(false)) == 0)
            {
                return null;
            }

            StringBuilder sb = null;

            do
            {
                char[] tmpCharBuffer = CharBuffer_Prop;
                int tmpCharLen = CharLen_Prop;
                int tmpCharPos = CharPos_Prop;
                int i = tmpCharPos;

                do
                {
                    char ch = tmpCharBuffer[i];

                    // Note the following common line feed chars:
                    // \n - UNIX   \r\n - DOS   \r - Mac
                    if (ch == '\r' || ch == '\n')
                    {
                        string s;

                        if (sb != null)
                        {
                            sb.Append(tmpCharBuffer, tmpCharPos, i - tmpCharPos);
                            s = sb.ToString();
                        }
                        else
                        {
                            s = new string(tmpCharBuffer, tmpCharPos, i - tmpCharPos);
                        }

                        CharPos_Prop = tmpCharPos = i + 1;

                        if (ch == '\r' && (tmpCharPos < tmpCharLen || (await ReadBufferAsync().ConfigureAwait(false)) > 0))
                        {
                            tmpCharPos = CharPos_Prop;
                            if (CharBuffer_Prop[tmpCharPos] == '\n')
                            {
                                CharPos_Prop = ++tmpCharPos;
                            }
                        }

                        return s;
                    }

                    i++;
                } while (i < tmpCharLen);

                i = tmpCharLen - tmpCharPos;
                if (sb == null)
                {
                    sb = new StringBuilder(i + 80);
                }
                sb.Append(tmpCharBuffer, tmpCharPos, i);
            } while (await ReadBufferAsync().ConfigureAwait(false) > 0);

            return sb.ToString();
        }

        public override Task<string> ReadToEndAsync()
        {
            // If we have been inherited into a subclass, the following implementation could be incorrect
            // since it does not call through to Read() which a subclass might have overridden.  
            // To be safe we will only use this implementation in cases where we know it is safe to do so,
            // and delegate to our base class (which will call into Read) when we are not sure.
            if (GetType() != typeof(StreamReader))
            {
                return base.ReadToEndAsync();
            }

            if (_stream == null)
            {
                throw new ObjectDisposedException(null, SR.ObjectDisposed_ReaderClosed);
            }

            CheckAsyncTaskInProgress();

            Task<string> task = ReadToEndAsyncInternal();
            _asyncReadTask = task;

            return task;
        }

        private async Task<string> ReadToEndAsyncInternal()
        {
            // Call ReadBuffer, then pull data out of charBuffer.
            StringBuilder sb = new StringBuilder(CharLen_Prop - CharPos_Prop);
            do
            {
                int tmpCharPos = CharPos_Prop;
                sb.Append(CharBuffer_Prop, tmpCharPos, CharLen_Prop - tmpCharPos);
                CharPos_Prop = CharLen_Prop;  // We consumed these characters
                await ReadBufferAsync().ConfigureAwait(false);
            } while (CharLen_Prop > 0);

            return sb.ToString();
        }

        public override Task<int> ReadAsync(char[] buffer, int index, int count)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer), SR.ArgumentNull_Buffer);
            }
            if (index < 0 || count < 0)
            {
                throw new ArgumentOutOfRangeException(index < 0 ? nameof(index) : nameof(count), SR.ArgumentOutOfRange_NeedNonNegNum);
            }
            if (buffer.Length - index < count)
            {
                throw new ArgumentException(SR.Argument_InvalidOffLen);
            }

            // If we have been inherited into a subclass, the following implementation could be incorrect
            // since it does not call through to Read() which a subclass might have overridden.  
            // To be safe we will only use this implementation in cases where we know it is safe to do so,
            // and delegate to our base class (which will call into Read) when we are not sure.
            if (GetType() != typeof(StreamReader))
            {
                return base.ReadAsync(buffer, index, count);
            }

            if (_stream == null)
            {
                throw new ObjectDisposedException(null, SR.ObjectDisposed_ReaderClosed);
            }

            CheckAsyncTaskInProgress();

            Task<int> task = ReadAsyncInternal(buffer, index, count);
            _asyncReadTask = task;

            return task;
        }

        internal override async Task<int> ReadAsyncInternal(char[] buffer, int index, int count)
        {
            if (CharPos_Prop == CharLen_Prop && (await ReadBufferAsync().ConfigureAwait(false)) == 0)
            {
                return 0;
            }

            int charsRead = 0;

            // As a perf optimization, if we had exactly one buffer's worth of 
            // data read in, let's try writing directly to the user's buffer.
            bool readToUserBuffer = false;

            Byte[] tmpByteBuffer = ByteBuffer_Prop;
            Stream tmpStream = Stream_Prop;

            while (count > 0)
            {
                // n is the characters available in _charBuffer
                int n = CharLen_Prop - CharPos_Prop;

                // charBuffer is empty, let's read from the stream
                if (n == 0)
                {
                    CharLen_Prop = 0;
                    CharPos_Prop = 0;

                    if (!CheckPreamble_Prop)
                    {
                        ByteLen_Prop = 0;
                    }

                    readToUserBuffer = count >= MaxCharsPerBuffer_Prop;

                    // We loop here so that we read in enough bytes to yield at least 1 char.
                    // We break out of the loop if the stream is blocked (EOF is reached).
                    do
                    {
                        Debug.Assert(n == 0);

                        if (CheckPreamble_Prop)
                        {
                            Debug.Assert(BytePos_Prop <= Preamble_Prop.Length, "possible bug in _compressPreamble.  Are two threads using this StreamReader at the same time?");
                            int tmpBytePos = BytePos_Prop;
                            int len = await tmpStream.ReadAsync(tmpByteBuffer, tmpBytePos, tmpByteBuffer.Length - tmpBytePos).ConfigureAwait(false);
                            Debug.Assert(len >= 0, "Stream.Read returned a negative number!  This is a bug in your stream class.");

                            if (len == 0)
                            {
                                // EOF but we might have buffered bytes from previous 
                                // attempts to detect preamble that needs to be decoded now
                                if (ByteLen_Prop > 0)
                                {
                                    if (readToUserBuffer)
                                    {
                                        n = Decoder_Prop.GetChars(tmpByteBuffer, 0, ByteLen_Prop, buffer, index + charsRead);
                                        CharLen_Prop = 0;  // StreamReader's buffer is empty.
                                    }
                                    else
                                    {
                                        n = Decoder_Prop.GetChars(tmpByteBuffer, 0, ByteLen_Prop, CharBuffer_Prop, 0);
                                        CharLen_Prop += n;  // Number of chars in StreamReader's buffer.
                                    }
                                }

                                // How can part of the preamble yield any chars?
                                Debug.Assert(n == 0);

                                IsBlocked_Prop = true;
                                break;
                            }
                            else
                            {
                                ByteLen_Prop += len;
                            }
                        }
                        else
                        {
                            Debug.Assert(BytePos_Prop == 0, "_bytePos can be non zero only when we are trying to _checkPreamble.  Are two threads using this StreamReader at the same time?");

                            ByteLen_Prop = await tmpStream.ReadAsync(tmpByteBuffer, 0, tmpByteBuffer.Length).ConfigureAwait(false);

                            Debug.Assert(ByteLen_Prop >= 0, "Stream.Read returned a negative number!  This is a bug in your stream class.");

                            if (ByteLen_Prop == 0)  // EOF
                            {
                                IsBlocked_Prop = true;
                                break;
                            }
                        }

                        // _isBlocked == whether we read fewer bytes than we asked for.
                        // Note we must check it here because CompressBuffer or 
                        // DetectEncoding will change _byteLen.
                        IsBlocked_Prop = (ByteLen_Prop < tmpByteBuffer.Length);

                        // Check for preamble before detect encoding. This is not to override the
                        // user supplied Encoding for the one we implicitly detect. The user could
                        // customize the encoding which we will loose, such as ThrowOnError on UTF8
                        // Note: we don't need to recompute readToUserBuffer optimization as IsPreamble
                        // doesn't change the encoding or affect _maxCharsPerBuffer
                        if (IsPreamble())
                        {
                            continue;
                        }

                        // On the first call to ReadBuffer, if we're supposed to detect the encoding, do it.
                        if (DetectEncoding_Prop && ByteLen_Prop >= 2)
                        {
                            DetectEncoding();
                            // DetectEncoding changes some buffer state.  Recompute this.
                            readToUserBuffer = count >= MaxCharsPerBuffer_Prop;
                        }

                        Debug.Assert(n == 0);

                        CharPos_Prop = 0;
                        if (readToUserBuffer)
                        {
                            n += Decoder_Prop.GetChars(tmpByteBuffer, 0, ByteLen_Prop, buffer, index + charsRead);

                            // Why did the bytes yield no chars?
                            Debug.Assert(n > 0);

                            CharLen_Prop = 0;  // StreamReader's buffer is empty.
                        }
                        else
                        {
                            n = Decoder_Prop.GetChars(tmpByteBuffer, 0, ByteLen_Prop, CharBuffer_Prop, 0);

                            // Why did the bytes yield no chars?
                            Debug.Assert(n > 0);

                            CharLen_Prop += n;  // Number of chars in StreamReader's buffer.
                        }
                    } while (n == 0);

                    if (n == 0)
                    {
                        break;  // We're at EOF
                    }
                }  // if (n == 0)

                // Got more chars in charBuffer than the user requested
                if (n > count)
                {
                    n = count;
                }

                if (!readToUserBuffer)
                {
                    Buffer.BlockCopy(CharBuffer_Prop, CharPos_Prop * 2, buffer, (index + charsRead) * 2, n * 2);
                    CharPos_Prop += n;
                }

                charsRead += n;
                count -= n;

                // This function shouldn't block for an indefinite amount of time,
                // or reading from a network stream won't work right.  If we got
                // fewer bytes than we requested, then we want to break right here.
                if (IsBlocked_Prop)
                {
                    break;
                }
            }  // while (count > 0)

            return charsRead;
        }

        public override Task<int> ReadBlockAsync(char[] buffer, int index, int count)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer), SR.ArgumentNull_Buffer);
            }
            if (index < 0 || count < 0)
            {
                throw new ArgumentOutOfRangeException(index < 0 ? nameof(index) : nameof(count), SR.ArgumentOutOfRange_NeedNonNegNum);
            }
            if (buffer.Length - index < count)
            {
                throw new ArgumentException(SR.Argument_InvalidOffLen);
            }

            // If we have been inherited into a subclass, the following implementation could be incorrect
            // since it does not call through to Read() which a subclass might have overridden.  
            // To be safe we will only use this implementation in cases where we know it is safe to do so,
            // and delegate to our base class (which will call into Read) when we are not sure.
            if (GetType() != typeof(StreamReader))
            {
                return base.ReadBlockAsync(buffer, index, count);
            }

            if (_stream == null)
            {
                throw new ObjectDisposedException(null, SR.ObjectDisposed_ReaderClosed);
            }

            CheckAsyncTaskInProgress();

            Task<int> task = base.ReadBlockAsync(buffer, index, count);
            _asyncReadTask = task;

            return task;
        }

#region Private properties for async method performance
        // Access to instance fields of MarshalByRefObject-derived types requires special JIT helpers that check
        // if the instance operated on is remote. This is optimised for fields on 'this' but if a method is Async
        // and is thus lifted to a state machine type, access will be slow.
        // As a workaround, we either cache instance fields in locals or use properties to access such fields.

        // See Dev11 bug #370300 for more info.

        private int CharLen_Prop
        {
            get { return _charLen; }
            set { _charLen = value; }
        }

        private int CharPos_Prop
        {
            get { return _charPos; }
            set { _charPos = value; }
        }

        private int ByteLen_Prop
        {
            get { return _byteLen; }
            set { _byteLen = value; }
        }

        private int BytePos_Prop
        {
            get { return _bytePos; }
            set { _bytePos = value; }
        }

        private Byte[] Preamble_Prop
        {
            get { return _preamble; }
        }

        private bool CheckPreamble_Prop
        {
            get { return _checkPreamble; }
        }

        private Decoder Decoder_Prop
        {
            get { return _decoder; }
        }

        private bool DetectEncoding_Prop
        {
            get { return _detectEncoding; }
        }

        private char[] CharBuffer_Prop
        {
            get { return _charBuffer; }
        }

        private Byte[] ByteBuffer_Prop
        {
            get { return _byteBuffer; }
        }

        private bool IsBlocked_Prop
        {
            get { return _isBlocked; }
            set { _isBlocked = value; }
        }

        private Stream Stream_Prop
        {
            get { return _stream; }
        }

        private int MaxCharsPerBuffer_Prop
        {
            get { return _maxCharsPerBuffer; }
        }
#endregion Private properties for async method performance
        private async Task<int> ReadBufferAsync()
        {
            CharLen_Prop = 0;
            CharPos_Prop = 0;
            Byte[] tmpByteBuffer = ByteBuffer_Prop;
            Stream tmpStream = Stream_Prop;

            if (!CheckPreamble_Prop)
            {
                ByteLen_Prop = 0;
            }
            do
            {
                if (CheckPreamble_Prop)
                {
                    Debug.Assert(BytePos_Prop <= Preamble_Prop.Length, "possible bug in _compressPreamble. Are two threads using this StreamReader at the same time?");
                    int tmpBytePos = BytePos_Prop;
                    int len = await tmpStream.ReadAsync(tmpByteBuffer, tmpBytePos, tmpByteBuffer.Length - tmpBytePos).ConfigureAwait(false);
                    Debug.Assert(len >= 0, "Stream.Read returned a negative number!  This is a bug in your stream class.");

                    if (len == 0)
                    {
                        // EOF but we might have buffered bytes from previous 
                        // attempt to detect preamble that needs to be decoded now
                        if (ByteLen_Prop > 0)
                        {
                            CharLen_Prop += Decoder_Prop.GetChars(tmpByteBuffer, 0, ByteLen_Prop, CharBuffer_Prop, CharLen_Prop);
                            // Need to zero out the _byteLen after we consume these bytes so that we don't keep infinitely hitting this code path
                            BytePos_Prop = 0; ByteLen_Prop = 0;
                        }

                        return CharLen_Prop;
                    }

                    ByteLen_Prop += len;
                }
                else
                {
                    Debug.Assert(BytePos_Prop == 0, "_bytePos can be non zero only when we are trying to _checkPreamble. Are two threads using this StreamReader at the same time?");
                    ByteLen_Prop = await tmpStream.ReadAsync(tmpByteBuffer, 0, tmpByteBuffer.Length).ConfigureAwait(false);
                    Debug.Assert(ByteLen_Prop >= 0, "Stream.Read returned a negative number!  Bug in stream class.");

                    if (ByteLen_Prop == 0)  // We're at EOF
                    {
                        return CharLen_Prop;
                    }
                }

                // _isBlocked == whether we read fewer bytes than we asked for.
                // Note we must check it here because CompressBuffer or 
                // DetectEncoding will change _byteLen.
                IsBlocked_Prop = (ByteLen_Prop < tmpByteBuffer.Length);

                // Check for preamble before detect encoding. This is not to override the
                // user supplied Encoding for the one we implicitly detect. The user could
                // customize the encoding which we will loose, such as ThrowOnError on UTF8
                if (IsPreamble())
                {
                    continue;
                }

                // If we're supposed to detect the encoding and haven't done so yet,
                // do it.  Note this may need to be called more than once.
                if (DetectEncoding_Prop && ByteLen_Prop >= 2)
                {
                    DetectEncoding();
                }

                CharLen_Prop += Decoder_Prop.GetChars(tmpByteBuffer, 0, ByteLen_Prop, CharBuffer_Prop, CharLen_Prop);
            } while (CharLen_Prop == 0);

            return CharLen_Prop;
        }
#endregion


        // No data, class doesn't need to be serializable.
        // Note this class is threadsafe.
        private class NullStreamReader : StreamReader
        {
            // Instantiating Encoding causes unnecessary perf hit. 
            internal NullStreamReader()
            {
                Init(Stream.Null);
            }

            public override Stream BaseStream
            {
                get { return Stream.Null; }
            }

            public override Encoding CurrentEncoding
            {
                get { return Encoding.Unicode; }
            }

            protected override void Dispose(bool disposing)
            {
                // Do nothing - this is essentially unclosable.
            }

            public override int Peek()
            {
                return -1;
            }

            public override int Read()
            {
                return -1;
            }

            [SuppressMessage("Microsoft.Contracts", "CC1055")]  // Skip extra error checking to avoid *potential* AppCompat problems.
            public override int Read(char[] buffer, int index, int count)
            {
                return 0;
            }

            public override string ReadLine()
            {
                return null;
            }

            public override string ReadToEnd()
            {
                return string.Empty;
            }

            internal override int ReadBuffer()
            {
                return 0;
            }
        }
    }
}
