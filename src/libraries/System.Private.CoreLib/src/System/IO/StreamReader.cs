// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Buffers.Binary;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace System.IO
{
    // This class implements a TextReader for reading characters to a Stream.
    // This is designed for character input in a particular Encoding,
    // whereas the Stream class is designed for byte input and output.
    public class StreamReader : TextReader
    {
        // StreamReader.Null is threadsafe.
        public static new readonly StreamReader Null = new NullStreamReader();

        // Using a 1K byte buffer and a 4K FileStream buffer works out pretty well
        // perf-wise.  On even a 40 MB text file, any perf loss by using a 4K
        // buffer is negated by the win of allocating a smaller byte[], which
        // saves construction time.  This does break adaptive buffering,
        // but this is slightly faster.
        private const int DefaultBufferSize = 1024;  // Byte buffer size
        private const int DefaultFileStreamBufferSize = 4096;
        private const int MinBufferSize = 128;

        private readonly Stream _stream;
        private Encoding _encoding = null!; // only null in NullStreamReader where this is never used
        private Decoder _decoder = null!; // only null in NullStreamReader where this is never used
        private readonly byte[] _byteBuffer = null!; // only null in NullStreamReader where this is never used
        private char[] _charBuffer = null!; // only null in NullStreamReader where this is never used
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

        /// <summary>True if the writer has been disposed; otherwise, false.</summary>
        private bool _disposed;

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
        private readonly bool _closable;  // Whether to close the underlying stream.

        // We don't guarantee thread safety on StreamReader, but we should at
        // least prevent users from trying to read anything while an Async
        // read from the same thread is in progress.
        private Task _asyncReadTask = Task.CompletedTask;

        private void CheckAsyncTaskInProgress()
        {
            // We are not locking the access to _asyncReadTask because this is not meant to guarantee thread safety.
            // We are simply trying to deter calling any Read APIs while an async Read from the same thread is in progress.
            if (!_asyncReadTask.IsCompleted)
            {
                ThrowAsyncIOInProgress();
            }
        }

        [DoesNotReturn]
        private static void ThrowAsyncIOInProgress() =>
            throw new InvalidOperationException(SR.InvalidOperation_AsyncIOInProgress);

        // StreamReader by default will ignore illegal UTF8 characters. We don't want to
        // throw here because we want to be able to read ill-formed data without choking.
        // The high level goal is to be tolerant of encoding errors when we read and very strict
        // when we write. Hence, default StreamWriter encoding will throw on error.

        private StreamReader()
        {
            Debug.Assert(this is NullStreamReader);
            _stream = Stream.Null;
            _closable = true;
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

        public StreamReader(Stream stream, Encoding? encoding = null, bool detectEncodingFromByteOrderMarks = true, int bufferSize = -1, bool leaveOpen = false)
        {
            if (stream == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.stream);
            }

            if (!stream.CanRead)
            {
                throw new ArgumentException(SR.Argument_StreamNotReadable);
            }

            if (bufferSize == -1)
            {
                bufferSize = DefaultBufferSize;
            }
            else if (bufferSize <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(bufferSize), SR.ArgumentOutOfRange_NeedPosNum);
            }

            _stream = stream;
            _encoding = encoding ??= Encoding.UTF8;
            _decoder = encoding.GetDecoder();
            if (bufferSize < MinBufferSize)
            {
                bufferSize = MinBufferSize;
            }

            _byteBuffer = new byte[bufferSize];
            _maxCharsPerBuffer = encoding.GetMaxCharCount(bufferSize);
            _charBuffer = new char[_maxCharsPerBuffer];
            _detectEncoding = detectEncodingFromByteOrderMarks;

            // If the preamble length is larger than the byte buffer length,
            // we'll never match it and will enter an infinite loop. This
            // should never happen in practice, but just in case, we'll skip
            // the preamble check for absurdly long preambles.
            int preambleLength = encoding.Preamble.Length;
            _checkPreamble = preambleLength > 0 && preambleLength <= bufferSize;

            _closable = !leaveOpen;
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
            : this(ValidateArgsAndOpenPath(path, encoding, bufferSize), encoding, detectEncodingFromByteOrderMarks, bufferSize, leaveOpen: false)
        {
        }

        public StreamReader(string path, FileStreamOptions options)
            : this(path, Encoding.UTF8, true, options)
        {
        }

        public StreamReader(string path, Encoding encoding, bool detectEncodingFromByteOrderMarks, FileStreamOptions options)
            : this(ValidateArgsAndOpenPath(path, encoding, options), encoding, detectEncodingFromByteOrderMarks, DefaultBufferSize)
        {
        }

        private static Stream ValidateArgsAndOpenPath(string path, Encoding encoding, FileStreamOptions options)
        {
            ArgumentException.ThrowIfNullOrEmpty(path);
            ArgumentNullException.ThrowIfNull(encoding);
            ArgumentNullException.ThrowIfNull(options);
            if ((options.Access & FileAccess.Read) == 0)
            {
                throw new ArgumentException(SR.Argument_StreamNotReadable, nameof(options));
            }

            return new FileStream(path, options);
        }

        private static Stream ValidateArgsAndOpenPath(string path, Encoding encoding, int bufferSize)
        {
            ArgumentException.ThrowIfNullOrEmpty(path);
            ArgumentNullException.ThrowIfNull(encoding);
            if (bufferSize <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(bufferSize), SR.ArgumentOutOfRange_NeedPosNum);
            }

            return new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, DefaultFileStreamBufferSize);
        }

        public override void Close()
        {
            Dispose(true);
        }

        protected override void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }
            _disposed = true;

            // Dispose of our resources if this StreamReader is closable.
            if (_closable)
            {
                try
                {
                    // Note that Stream.Close() can potentially throw here. So we need to
                    // ensure cleaning up internal resources, inside the finally block.
                    if (disposing)
                    {
                        _stream.Close();
                    }
                }
                finally
                {
                    _charPos = 0;
                    _charLen = 0;
                    base.Dispose(disposing);
                }
            }
        }

        public virtual Encoding CurrentEncoding => _encoding;

        public virtual Stream BaseStream => _stream;

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
                ThrowIfDisposed();
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

        public override int Peek()
        {
            ThrowIfDisposed();
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
            ThrowIfDisposed();
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
            ArgumentNullException.ThrowIfNull(buffer);

            if (index < 0 || count < 0)
            {
                throw new ArgumentOutOfRangeException(index < 0 ? nameof(index) : nameof(count), SR.ArgumentOutOfRange_NeedNonNegNum);
            }
            if (buffer.Length - index < count)
            {
                throw new ArgumentException(SR.Argument_InvalidOffLen);
            }

            return ReadSpan(new Span<char>(buffer, index, count));
        }

        public override int Read(Span<char> buffer) =>
            GetType() == typeof(StreamReader) ? ReadSpan(buffer) :
            base.Read(buffer); // Defer to Read(char[], ...) if a derived type may have previously overridden it

        private int ReadSpan(Span<char> buffer)
        {
            ThrowIfDisposed();
            CheckAsyncTaskInProgress();

            int charsRead = 0;
            // As a perf optimization, if we had exactly one buffer's worth of
            // data read in, let's try writing directly to the user's buffer.
            bool readToUserBuffer = false;
            int count = buffer.Length;
            while (count > 0)
            {
                int n = _charLen - _charPos;
                if (n == 0)
                {
                    n = ReadBuffer(buffer.Slice(charsRead), out readToUserBuffer);
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
                    new Span<char>(_charBuffer, _charPos, n).CopyTo(buffer.Slice(charsRead));
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
            ThrowIfDisposed();
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
            ArgumentNullException.ThrowIfNull(buffer);

            if (index < 0 || count < 0)
            {
                throw new ArgumentOutOfRangeException(index < 0 ? nameof(index) : nameof(count), SR.ArgumentOutOfRange_NeedNonNegNum);
            }
            if (buffer.Length - index < count)
            {
                throw new ArgumentException(SR.Argument_InvalidOffLen);
            }
            ThrowIfDisposed();
            CheckAsyncTaskInProgress();

            return base.ReadBlock(buffer, index, count);
        }

        public override int ReadBlock(Span<char> buffer)
        {
            if (GetType() != typeof(StreamReader))
            {
                // Defer to Read(char[], ...) if a derived type may have previously overridden it.
                return base.ReadBlock(buffer);
            }

            int i, n = 0;
            do
            {
                i = ReadSpan(buffer.Slice(n));
                n += i;
            } while (i > 0 && n < buffer.Length);
            return n;
        }

        // Trims n bytes from the front of the buffer.
        private void CompressBuffer(int n)
        {
            Debug.Assert(_byteLen >= n, "CompressBuffer was called with a number of bytes greater than the current buffer length.  Are two threads using this StreamReader at the same time?");
            byte[] byteBuffer = _byteBuffer;
            _ = byteBuffer.Length; // allow JIT to prove object is not null
            new ReadOnlySpan<byte>(byteBuffer, n, _byteLen - n).CopyTo(byteBuffer);
            _byteLen -= n;
        }

        private void DetectEncoding()
        {
            Debug.Assert(_byteLen >= 2, "Caller should've validated that at least 2 bytes were available.");

            byte[] byteBuffer = _byteBuffer;
            _detectEncoding = false;
            bool changedEncoding = false;

            ushort firstTwoBytes = BinaryPrimitives.ReadUInt16LittleEndian(byteBuffer);
            if (firstTwoBytes == 0xFFFE)
            {
                // Big Endian Unicode
                _encoding = Encoding.BigEndianUnicode;
                CompressBuffer(2);
                changedEncoding = true;
            }
            else if (firstTwoBytes == 0xFEFF)
            {
                // Little Endian Unicode, or possibly little endian UTF32
                if (_byteLen < 4 || byteBuffer[2] != 0 || byteBuffer[3] != 0)
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
            else if (_byteLen >= 3 && firstTwoBytes == 0xBBEF && byteBuffer[2] == 0xBF)
            {
                // UTF-8
                _encoding = Encoding.UTF8;
                CompressBuffer(3);
                changedEncoding = true;
            }
            else if (_byteLen >= 4 && firstTwoBytes == 0 && byteBuffer[2] == 0xFE && byteBuffer[3] == 0xFF)
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
                int newMaxCharsPerBuffer = _encoding.GetMaxCharCount(byteBuffer.Length);
                if (newMaxCharsPerBuffer > _maxCharsPerBuffer)
                {
                    _charBuffer = new char[newMaxCharsPerBuffer];
                }
                _maxCharsPerBuffer = newMaxCharsPerBuffer;
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
                return false;
            }

            return IsPreambleWorker(); // move this call out of the hot path
            bool IsPreambleWorker()
            {
                Debug.Assert(_checkPreamble);
                ReadOnlySpan<byte> preamble = _encoding.Preamble;

                Debug.Assert(_bytePos < preamble.Length, "_compressPreamble was called with the current bytePos greater than the preamble buffer length.  Are two threads using this StreamReader at the same time?");
                int len = Math.Min(_byteLen, preamble.Length);

                for (int i = _bytePos; i < len; i++)
                {
                    if (_byteBuffer[i] != preamble[i])
                    {
                        _bytePos = 0; // preamble match failed; back up to beginning of buffer
                        _checkPreamble = false;
                        return false;
                    }
                }
                _bytePos = len; // we've matched all bytes up to this point

                Debug.Assert(_bytePos <= preamble.Length, "possible bug in _compressPreamble.  Are two threads using this StreamReader at the same time?");

                if (_bytePos == preamble.Length)
                {
                    // We have a match
                    CompressBuffer(preamble.Length);
                    _bytePos = 0;
                    _checkPreamble = false;
                    _detectEncoding = false;
                }

                return _checkPreamble;
            }
        }

        internal virtual int ReadBuffer()
        {
            _charLen = 0;
            _charPos = 0;

            if (!_checkPreamble)
            {
                _byteLen = 0;
            }

            bool eofReached = false;

            do
            {
                if (_checkPreamble)
                {
                    Debug.Assert(_bytePos <= _encoding.Preamble.Length, "possible bug in _compressPreamble.  Are two threads using this StreamReader at the same time?");
                    int len = _stream.Read(_byteBuffer, _bytePos, _byteBuffer.Length - _bytePos);
                    Debug.Assert(len >= 0, "Stream.Read returned a negative number!  This is a bug in your stream class.");

                    if (len == 0)
                    {
                        eofReached = true;
                        break;
                    }

                    _byteLen += len;
                }
                else
                {
                    Debug.Assert(_bytePos == 0, "bytePos can be non zero only when we are trying to _checkPreamble.  Are two threads using this StreamReader at the same time?");
                    _byteLen = _stream.Read(_byteBuffer, 0, _byteBuffer.Length);
                    Debug.Assert(_byteLen >= 0, "Stream.Read returned a negative number!  This is a bug in your stream class.");

                    if (_byteLen == 0)
                    {
                        eofReached = true;
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

                Debug.Assert(_charPos == 0 && _charLen == 0, "We shouldn't be trying to decode more data if we made progress in an earlier iteration.");
                _charLen = _decoder.GetChars(_byteBuffer, 0, _byteLen, _charBuffer, 0, flush: false);
            } while (_charLen == 0);

            if (eofReached)
            {
                // EOF has been reached - perform final flush.
                // We need to reset _bytePos and _byteLen just in case we hadn't
                // finished processing the preamble before we reached EOF.

                Debug.Assert(_charPos == 0 && _charLen == 0, "We shouldn't be looking for EOF unless we have an empty char buffer.");
                _charLen = _decoder.GetChars(_byteBuffer, 0, _byteLen, _charBuffer, 0, flush: true);
                _bytePos = 0;
                _byteLen = 0;
            }

            return _charLen;
        }


        // This version has a perf optimization to decode data DIRECTLY into the
        // user's buffer, bypassing StreamReader's own buffer.
        // This gives a > 20% perf improvement for our encodings across the board,
        // but only when asking for at least the number of characters that one
        // buffer's worth of bytes could produce.
        // This optimization, if run, will break SwitchEncoding, so we must not do
        // this on the first call to ReadBuffer.
        private int ReadBuffer(Span<char> userBuffer, out bool readToUserBuffer)
        {
            _charLen = 0;
            _charPos = 0;

            if (!_checkPreamble)
            {
                _byteLen = 0;
            }

            bool eofReached = false;
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
            readToUserBuffer = userBuffer.Length >= _maxCharsPerBuffer;

            do
            {
                Debug.Assert(charsRead == 0);

                if (_checkPreamble)
                {
                    Debug.Assert(_bytePos <= _encoding.Preamble.Length, "possible bug in _compressPreamble.  Are two threads using this StreamReader at the same time?");
                    int len = _stream.Read(_byteBuffer, _bytePos, _byteBuffer.Length - _bytePos);
                    Debug.Assert(len >= 0, "Stream.Read returned a negative number!  This is a bug in your stream class.");

                    if (len == 0)
                    {
                        eofReached = true;
                        break;
                    }

                    _byteLen += len;
                }
                else
                {
                    Debug.Assert(_bytePos == 0, "bytePos can be non zero only when we are trying to _checkPreamble.  Are two threads using this StreamReader at the same time?");
                    _byteLen = _stream.Read(_byteBuffer, 0, _byteBuffer.Length);
                    Debug.Assert(_byteLen >= 0, "Stream.Read returned a negative number!  This is a bug in your stream class.");

                    if (_byteLen == 0)
                    {
                        eofReached = true;
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
                    readToUserBuffer = userBuffer.Length >= _maxCharsPerBuffer;
                }

                Debug.Assert(charsRead == 0 && _charPos == 0 && _charLen == 0, "We shouldn't be trying to decode more data if we made progress in an earlier iteration.");
                if (readToUserBuffer)
                {
                    charsRead = _decoder.GetChars(new ReadOnlySpan<byte>(_byteBuffer, 0, _byteLen), userBuffer, flush: false);
                }
                else
                {
                    charsRead = _decoder.GetChars(_byteBuffer, 0, _byteLen, _charBuffer, 0, flush: false);
                    _charLen = charsRead;  // Number of chars in StreamReader's buffer.
                }
            } while (charsRead == 0);

            if (eofReached)
            {
                // EOF has been reached - perform final flush.
                // We need to reset _bytePos and _byteLen just in case we hadn't
                // finished processing the preamble before we reached EOF.

                Debug.Assert(charsRead == 0 && _charPos == 0 && _charLen == 0, "We shouldn't be looking for EOF unless we have an empty char buffer.");

                if (readToUserBuffer)
                {
                    charsRead = _decoder.GetChars(new ReadOnlySpan<byte>(_byteBuffer, 0, _byteLen), userBuffer, flush: true);
                }
                else
                {
                    charsRead = _decoder.GetChars(_byteBuffer, 0, _byteLen, _charBuffer, 0, flush: true);
                    _charLen = charsRead;  // Number of chars in StreamReader's buffer.
                }
                _bytePos = 0;
                _byteLen = 0;
            }

            _isBlocked &= charsRead < userBuffer.Length;

            return charsRead;
        }


        // Reads a line. A line is defined as a sequence of characters followed by
        // a carriage return ('\r'), a line feed ('\n'), or a carriage return
        // immediately followed by a line feed. The resulting string does not
        // contain the terminating carriage return and/or line feed. The returned
        // value is null if the end of the input stream has been reached.
        //
        public override string? ReadLine()
        {
            ThrowIfDisposed();
            CheckAsyncTaskInProgress();

            if (_charPos == _charLen)
            {
                if (ReadBuffer() == 0)
                {
                    return null;
                }
            }

            var vsb = new ValueStringBuilder(stackalloc char[256]);
            do
            {
                // Look for '\r' or \'n'.
                ReadOnlySpan<char> charBufferSpan = _charBuffer.AsSpan(_charPos, _charLen - _charPos);
                Debug.Assert(!charBufferSpan.IsEmpty, "ReadBuffer returned > 0 but didn't bump _charLen?");

                int idxOfNewline = charBufferSpan.IndexOfAny('\r', '\n');
                if (idxOfNewline >= 0)
                {
                    string retVal;
                    if (vsb.Length == 0)
                    {
                        retVal = new string(charBufferSpan.Slice(0, idxOfNewline));
                    }
                    else
                    {
                        retVal = string.Concat(vsb.AsSpan(), charBufferSpan.Slice(0, idxOfNewline));
                        vsb.Dispose();
                    }

                    char matchedChar = charBufferSpan[idxOfNewline];
                    _charPos += idxOfNewline + 1;

                    // If we found '\r', consume any immediately following '\n'.
                    if (matchedChar == '\r')
                    {
                        if (_charPos < _charLen || ReadBuffer() > 0)
                        {
                            if (_charBuffer[_charPos] == '\n')
                            {
                                _charPos++;
                            }
                        }
                    }

                    return retVal;
                }

                // We didn't find '\r' or '\n'. Add it to the StringBuilder
                // and loop until we reach a newline or EOF.

                vsb.Append(charBufferSpan);
            } while (ReadBuffer() > 0);

            return vsb.ToString();
        }

        public override Task<string?> ReadLineAsync() =>
            ReadLineAsync(default).AsTask();

        /// <summary>
        /// Reads a line of characters asynchronously from the current stream and returns the data as a string.
        /// </summary>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A value task that represents the asynchronous read operation. The value of the <c>TResult</c>
        /// parameter contains the next line from the stream, or is <see langword="null" /> if all of the characters have been read.</returns>
        /// <exception cref="ArgumentOutOfRangeException">The number of characters in the next line is larger than <see cref="int.MaxValue"/>.</exception>
        /// <exception cref="ObjectDisposedException">The stream reader has been disposed.</exception>
        /// <exception cref="InvalidOperationException">The reader is currently in use by a previous read operation.</exception>
        /// <example>
        /// The following example shows how to read and print all lines from the file until the end of the file is reached or the operation timed out.
        /// <code lang="C#">
        /// using CancellationTokenSource tokenSource = new (TimeSpan.FromSeconds(1));
        /// using StreamReader reader = File.OpenText("existingfile.txt");
        ///
        /// string line;
        /// while ((line = await reader.ReadLineAsync(tokenSource.Token)) is not null)
        /// {
        ///     Console.WriteLine(line);
        /// }
        /// </code>
        /// </example>
        /// <remarks>
        /// If this method is canceled via <paramref name="cancellationToken"/>, some data
        /// that has been read from the current <see cref="Stream"/> but not stored (by the
        /// <see cref="StreamReader"/>) or returned (to the caller) may be lost.
        /// </remarks>
        public override ValueTask<string?> ReadLineAsync(CancellationToken cancellationToken)
        {
            // If we have been inherited into a subclass, the following implementation could be incorrect
            // since it does not call through to Read() which a subclass might have overridden.
            // To be safe we will only use this implementation in cases where we know it is safe to do so,
            // and delegate to our base class (which will call into Read) when we are not sure.
            if (GetType() != typeof(StreamReader))
            {
                return base.ReadLineAsync(cancellationToken);
            }

            ThrowIfDisposed();
            CheckAsyncTaskInProgress();

            Task<string?> task = ReadLineAsyncInternal(cancellationToken);
            _asyncReadTask = task;

            return new ValueTask<string?>(task);
        }

        private async Task<string?> ReadLineAsyncInternal(CancellationToken cancellationToken)
        {
            if (_charPos == _charLen && (await ReadBufferAsync(cancellationToken).ConfigureAwait(false)) == 0)
            {
                return null;
            }

            string retVal;
            char[]? arrayPoolBuffer = null;
            int arrayPoolBufferPos = 0;

            do
            {
                char[] charBuffer = _charBuffer;
                int charLen = _charLen;
                int charPos = _charPos;

                // Look for '\r' or \'n'.
                Debug.Assert(charPos < charLen, "ReadBuffer returned > 0 but didn't bump _charLen?");

                int idxOfNewline = charBuffer.AsSpan(charPos, charLen - charPos).IndexOfAny('\r', '\n');
                if (idxOfNewline >= 0)
                {
                    if (arrayPoolBuffer is null)
                    {
                        retVal = new string(charBuffer, charPos, idxOfNewline);
                    }
                    else
                    {
                        retVal = string.Concat(arrayPoolBuffer.AsSpan(0, arrayPoolBufferPos), charBuffer.AsSpan(charPos, idxOfNewline));
                        ArrayPool<char>.Shared.Return(arrayPoolBuffer);
                    }

                    charPos += idxOfNewline;
                    char matchedChar = charBuffer[charPos++];
                    _charPos = charPos;

                    // If we found '\r', consume any immediately following '\n'.
                    if (matchedChar == '\r')
                    {
                        if (charPos < charLen || (await ReadBufferAsync(cancellationToken).ConfigureAwait(false)) > 0)
                        {
                            if (_charBuffer[_charPos] == '\n')
                            {
                                _charPos++;
                            }
                        }
                    }

                    return retVal;
                }

                // We didn't find '\r' or '\n'. Add the read data to the pooled buffer
                // and loop until we reach a newline or EOF.
                if (arrayPoolBuffer is null)
                {
                    arrayPoolBuffer = ArrayPool<char>.Shared.Rent(charLen - charPos + 80);
                }
                else if ((arrayPoolBuffer.Length - arrayPoolBufferPos) < (charLen - charPos))
                {
                    char[] newBuffer = ArrayPool<char>.Shared.Rent(checked(arrayPoolBufferPos + charLen - charPos));
                    arrayPoolBuffer.AsSpan(0, arrayPoolBufferPos).CopyTo(newBuffer);
                    ArrayPool<char>.Shared.Return(arrayPoolBuffer);
                    arrayPoolBuffer = newBuffer;
                }
                charBuffer.AsSpan(charPos, charLen - charPos).CopyTo(arrayPoolBuffer.AsSpan(arrayPoolBufferPos));
                arrayPoolBufferPos += charLen - charPos;
            }
            while (await ReadBufferAsync(cancellationToken).ConfigureAwait(false) > 0);

            if (arrayPoolBuffer is not null)
            {
                retVal = new string(arrayPoolBuffer, 0, arrayPoolBufferPos);
                ArrayPool<char>.Shared.Return(arrayPoolBuffer);
            }
            else
            {
                retVal = string.Empty;
            }

            return retVal;
        }

        public override Task<string> ReadToEndAsync() => ReadToEndAsync(default);

        /// <summary>
        /// Reads all characters from the current position to the end of the stream asynchronously and returns them as one string.
        /// </summary>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A task that represents the asynchronous read operation. The value of the <c>TResult</c> parameter contains
        /// a string with the characters from the current position to the end of the stream.</returns>
        /// <exception cref="ArgumentOutOfRangeException">The number of characters is larger than <see cref="int.MaxValue"/>.</exception>
        /// <exception cref="ObjectDisposedException">The stream reader has been disposed.</exception>
        /// <exception cref="InvalidOperationException">The reader is currently in use by a previous read operation.</exception>
        /// <example>
        /// The following example shows how to read the contents of a file by using the <see cref="ReadToEndAsync(CancellationToken)"/> method.
        /// <code lang="C#">
        /// using CancellationTokenSource tokenSource = new (TimeSpan.FromSeconds(1));
        /// using StreamReader reader = File.OpenText("existingfile.txt");
        ///
        /// Console.WriteLine(await reader.ReadToEndAsync(tokenSource.Token));
        /// </code>
        /// </example>
        /// <remarks>
        /// If this method is canceled via <paramref name="cancellationToken"/>, some data
        /// that has been read from the current <see cref="Stream"/> but not stored (by the
        /// <see cref="StreamReader"/>) or returned (to the caller) may be lost.
        /// </remarks>
        public override Task<string> ReadToEndAsync(CancellationToken cancellationToken)
        {
            // If we have been inherited into a subclass, the following implementation could be incorrect
            // since it does not call through to Read() which a subclass might have overridden.
            // To be safe we will only use this implementation in cases where we know it is safe to do so,
            // and delegate to our base class (which will call into Read) when we are not sure.
            if (GetType() != typeof(StreamReader))
            {
                return base.ReadToEndAsync(cancellationToken);
            }

            ThrowIfDisposed();
            CheckAsyncTaskInProgress();

            Task<string> task = ReadToEndAsyncInternal(cancellationToken);
            _asyncReadTask = task;

            return task;
        }

        private async Task<string> ReadToEndAsyncInternal(CancellationToken cancellationToken)
        {
            // Call ReadBuffer, then pull data out of charBuffer.
            StringBuilder sb = new StringBuilder(_charLen - _charPos);
            do
            {
                int tmpCharPos = _charPos;
                sb.Append(_charBuffer, tmpCharPos, _charLen - tmpCharPos);
                _charPos = _charLen;  // We consumed these characters
                await ReadBufferAsync(cancellationToken).ConfigureAwait(false);
            } while (_charLen > 0);

            return sb.ToString();
        }

        public override Task<int> ReadAsync(char[] buffer, int index, int count)
        {
            ArgumentNullException.ThrowIfNull(buffer);

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

            ThrowIfDisposed();
            CheckAsyncTaskInProgress();

            Task<int> task = ReadAsyncInternal(new Memory<char>(buffer, index, count), CancellationToken.None).AsTask();
            _asyncReadTask = task;

            return task;
        }

        public override ValueTask<int> ReadAsync(Memory<char> buffer, CancellationToken cancellationToken = default)
        {
            if (GetType() != typeof(StreamReader))
            {
                // Ensure we use existing overrides if a class already overrode existing overloads.
                return base.ReadAsync(buffer, cancellationToken);
            }

            ThrowIfDisposed();
            CheckAsyncTaskInProgress();

            if (cancellationToken.IsCancellationRequested)
            {
                return ValueTask.FromCanceled<int>(cancellationToken);
            }

            return ReadAsyncInternal(buffer, cancellationToken);
        }

        internal override async ValueTask<int> ReadAsyncInternal(Memory<char> buffer, CancellationToken cancellationToken)
        {
            if (_charPos == _charLen && (await ReadBufferAsync(cancellationToken).ConfigureAwait(false)) == 0)
            {
                return 0;
            }

            int charsRead = 0;

            // As a perf optimization, if we had exactly one buffer's worth of
            // data read in, let's try writing directly to the user's buffer.
            bool readToUserBuffer = false;

            byte[] tmpByteBuffer = _byteBuffer;
            Stream tmpStream = _stream;

            int count = buffer.Length;
            while (count > 0)
            {
                // n is the characters available in _charBuffer
                int n = _charLen - _charPos;

                // charBuffer is empty, let's read from the stream
                if (n == 0)
                {
                    _charLen = 0;
                    _charPos = 0;

                    if (!_checkPreamble)
                    {
                        _byteLen = 0;
                    }

                    readToUserBuffer = count >= _maxCharsPerBuffer;

                    // We loop here so that we read in enough bytes to yield at least 1 char.
                    // We break out of the loop if the stream is blocked (EOF is reached).
                    do
                    {
                        Debug.Assert(n == 0);

                        if (_checkPreamble)
                        {
                            Debug.Assert(_bytePos <= _encoding.Preamble.Length, "possible bug in _compressPreamble.  Are two threads using this StreamReader at the same time?");
                            int tmpBytePos = _bytePos;
                            int len = await tmpStream.ReadAsync(new Memory<byte>(tmpByteBuffer, tmpBytePos, tmpByteBuffer.Length - tmpBytePos), cancellationToken).ConfigureAwait(false);
                            Debug.Assert(len >= 0, "Stream.Read returned a negative number!  This is a bug in your stream class.");

                            if (len == 0)
                            {
                                // EOF but we might have buffered bytes from previous
                                // attempts to detect preamble that needs to be decoded now
                                if (_byteLen > 0)
                                {
                                    if (readToUserBuffer)
                                    {
                                        n = _decoder.GetChars(new ReadOnlySpan<byte>(tmpByteBuffer, 0, _byteLen), buffer.Span.Slice(charsRead), flush: false);
                                        _charLen = 0;  // StreamReader's buffer is empty.
                                    }
                                    else
                                    {
                                        n = _decoder.GetChars(tmpByteBuffer, 0, _byteLen, _charBuffer, 0);
                                        _charLen += n;  // Number of chars in StreamReader's buffer.
                                    }
                                }

                                // How can part of the preamble yield any chars?
                                Debug.Assert(n == 0);

                                _isBlocked = true;
                                break;
                            }
                            else
                            {
                                _byteLen += len;
                            }
                        }
                        else
                        {
                            Debug.Assert(_bytePos == 0, "_bytePos can be non zero only when we are trying to _checkPreamble.  Are two threads using this StreamReader at the same time?");

                            _byteLen = await tmpStream.ReadAsync(new Memory<byte>(tmpByteBuffer), cancellationToken).ConfigureAwait(false);

                            Debug.Assert(_byteLen >= 0, "Stream.Read returned a negative number!  This is a bug in your stream class.");

                            if (_byteLen == 0)  // EOF
                            {
                                _isBlocked = true;
                                break;
                            }
                        }

                        // _isBlocked == whether we read fewer bytes than we asked for.
                        // Note we must check it here because CompressBuffer or
                        // DetectEncoding will change _byteLen.
                        _isBlocked = (_byteLen < tmpByteBuffer.Length);

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
                            readToUserBuffer = count >= _maxCharsPerBuffer;
                        }

                        Debug.Assert(n == 0);

                        _charPos = 0;
                        if (readToUserBuffer)
                        {
                            n = _decoder.GetChars(new ReadOnlySpan<byte>(tmpByteBuffer, 0, _byteLen), buffer.Span.Slice(charsRead), flush: false);
                            _charLen = 0;  // StreamReader's buffer is empty.
                        }
                        else
                        {
                            n = _decoder.GetChars(tmpByteBuffer, 0, _byteLen, _charBuffer, 0);
                            _charLen += n;  // Number of chars in StreamReader's buffer.
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
                    new Span<char>(_charBuffer, _charPos, n).CopyTo(buffer.Span.Slice(charsRead));
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
            }  // while (count > 0)

            return charsRead;
        }

        public override Task<int> ReadBlockAsync(char[] buffer, int index, int count)
        {
            ArgumentNullException.ThrowIfNull(buffer);

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

            ThrowIfDisposed();
            CheckAsyncTaskInProgress();

            Task<int> task = base.ReadBlockAsync(buffer, index, count);
            _asyncReadTask = task;

            return task;
        }

        public override ValueTask<int> ReadBlockAsync(Memory<char> buffer, CancellationToken cancellationToken = default)
        {
            if (GetType() != typeof(StreamReader))
            {
                // If a derived type may have overridden ReadBlockAsync(char[], ...) before this overload
                // was introduced, defer to it.
                return base.ReadBlockAsync(buffer, cancellationToken);
            }

            ThrowIfDisposed();
            CheckAsyncTaskInProgress();

            if (cancellationToken.IsCancellationRequested)
            {
                return ValueTask.FromCanceled<int>(cancellationToken);
            }

            ValueTask<int> vt = ReadBlockAsyncInternal(buffer, cancellationToken);
            if (vt.IsCompletedSuccessfully)
            {
                return vt;
            }

            Task<int> t = vt.AsTask();
            _asyncReadTask = t;
            return new ValueTask<int>(t);
        }

        private async ValueTask<int> ReadBufferAsync(CancellationToken cancellationToken)
        {
            _charLen = 0;
            _charPos = 0;
            byte[] tmpByteBuffer = _byteBuffer;
            Stream tmpStream = _stream;

            if (!_checkPreamble)
            {
                _byteLen = 0;
            }

            bool eofReached = false;

            do
            {
                if (_checkPreamble)
                {
                    Debug.Assert(_bytePos <= _encoding.Preamble.Length, "possible bug in _compressPreamble. Are two threads using this StreamReader at the same time?");
                    int tmpBytePos = _bytePos;
                    int len = await tmpStream.ReadAsync(tmpByteBuffer.AsMemory(tmpBytePos), cancellationToken).ConfigureAwait(false);
                    Debug.Assert(len >= 0, "Stream.Read returned a negative number!  This is a bug in your stream class.");

                    if (len == 0)
                    {
                        eofReached = true;
                        break;
                    }

                    _byteLen += len;
                }
                else
                {
                    Debug.Assert(_bytePos == 0, "_bytePos can be non zero only when we are trying to _checkPreamble. Are two threads using this StreamReader at the same time?");
                    _byteLen = await tmpStream.ReadAsync(new Memory<byte>(tmpByteBuffer), cancellationToken).ConfigureAwait(false);
                    Debug.Assert(_byteLen >= 0, "Stream.Read returned a negative number!  Bug in stream class.");

                    if (_byteLen == 0)
                    {
                        eofReached = true;
                        break;
                    }
                }

                // _isBlocked == whether we read fewer bytes than we asked for.
                // Note we must check it here because CompressBuffer or
                // DetectEncoding will change _byteLen.
                _isBlocked = (_byteLen < tmpByteBuffer.Length);

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

                Debug.Assert(_charPos == 0 && _charLen == 0, "We shouldn't be trying to decode more data if we made progress in an earlier iteration.");
                _charLen = _decoder.GetChars(tmpByteBuffer, 0, _byteLen, _charBuffer, 0, flush: false);
            } while (_charLen == 0);

            if (eofReached)
            {
                // EOF has been reached - perform final flush.
                // We need to reset _bytePos and _byteLen just in case we hadn't
                // finished processing the preamble before we reached EOF.

                Debug.Assert(_charPos == 0 && _charLen == 0, "We shouldn't be looking for EOF unless we have an empty char buffer.");
                _charLen = _decoder.GetChars(_byteBuffer, 0, _byteLen, _charBuffer, 0, flush: true);
                _bytePos = 0;
                _byteLen = 0;
            }

            return _charLen;
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                ThrowObjectDisposedException();
            }

            void ThrowObjectDisposedException() => throw new ObjectDisposedException(GetType().Name, SR.ObjectDisposed_ReaderClosed);
        }

        // No data, class doesn't need to be serializable.
        // Note this class is threadsafe.
        internal sealed class NullStreamReader : StreamReader
        {
            public override Encoding CurrentEncoding => Encoding.Unicode;

            protected override void Dispose(bool disposing)
            {
                // Do nothing - this is essentially unclosable.
            }

            public override int Peek() => -1;

            public override int Read() => -1;

            public override int Read(char[] buffer, int index, int count) => 0;

            public override int Read(Span<char> buffer) => 0;

            public override Task<int> ReadAsync(char[] buffer, int index, int count) => Task.FromResult(0);

            public override ValueTask<int> ReadAsync(Memory<char> buffer, CancellationToken cancellationToken) =>
                cancellationToken.IsCancellationRequested ? ValueTask.FromCanceled<int>(cancellationToken) : default;

            public override int ReadBlock(char[] buffer, int index, int count) => 0;

            public override int ReadBlock(Span<char> buffer) => 0;

            public override Task<int> ReadBlockAsync(char[] buffer, int index, int count) => Task.FromResult(0);

            public override ValueTask<int> ReadBlockAsync(Memory<char> buffer, CancellationToken cancellationToken) =>
                cancellationToken.IsCancellationRequested ? ValueTask.FromCanceled<int>(cancellationToken) : default;

            public override string? ReadLine() => null;

            public override Task<string?> ReadLineAsync() => Task.FromResult<string?>(null);

            public override ValueTask<string?> ReadLineAsync(CancellationToken cancellationToken) =>
                cancellationToken.IsCancellationRequested ? ValueTask.FromCanceled<string?>(cancellationToken) : default;

            public override string ReadToEnd() => "";

            public override Task<string> ReadToEndAsync() => Task.FromResult("");

            public override Task<string> ReadToEndAsync(CancellationToken cancellationToken) =>
                cancellationToken.IsCancellationRequested ? Task.FromCanceled<string>(cancellationToken) : Task.FromResult("");

            internal override ValueTask<int> ReadAsyncInternal(Memory<char> buffer, CancellationToken cancellationToken) =>
                cancellationToken.IsCancellationRequested ? ValueTask.FromCanceled<int>(cancellationToken) : default;

            internal override int ReadBuffer() => 0;
        }
    }
}
