// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace System.IO
{
    // This class implements a TextWriter for writing characters to a Stream.
    // This is designed for character output in a particular Encoding, 
    // whereas the Stream class is designed for byte input and output.  
    public class StreamWriter : TextWriter
    {
        // For UTF-8, the values of 1K for the default buffer size and 4K for the
        // file stream buffer size are reasonable & give very reasonable
        // performance for in terms of construction time for the StreamWriter and
        // write perf.  Note that for UTF-8, we end up allocating a 4K byte buffer,
        // which means we take advantage of adaptive buffering code.
        // The performance using UnicodeEncoding is acceptable.  
        private const int DefaultBufferSize = 1024;   // char[]
        private const int DefaultFileStreamBufferSize = 4096;
        private const int MinBufferSize = 128;

        // Bit bucket - Null has no backing store. Non closable.
        public new static readonly StreamWriter Null = new StreamWriter(Stream.Null, UTF8NoBOM, MinBufferSize, leaveOpen: true);

        private readonly Stream _stream;
        private readonly Encoding _encoding;
        private readonly Encoder _encoder;
        private readonly byte[] _byteBuffer;
        private readonly char[] _charBuffer;
        private int _charPos;
        private int _charLen;
        private bool _autoFlush;
        private bool _haveWrittenPreamble;
        private readonly bool _closable;
        private bool _disposed;

        // We don't guarantee thread safety on StreamWriter, but we should at 
        // least prevent users from trying to write anything while an Async
        // write from the same thread is in progress.
        private Task _asyncWriteTask = Task.CompletedTask;

        private void CheckAsyncTaskInProgress()
        {
            // We are not locking the access to _asyncWriteTask because this is not meant to guarantee thread safety. 
            // We are simply trying to deter calling any Write APIs while an async Write from the same thread is in progress.
            if (!_asyncWriteTask.IsCompleted)
            {
                ThrowAsyncIOInProgress();
            }
        }

        private static void ThrowAsyncIOInProgress() =>
            throw new InvalidOperationException(SR.InvalidOperation_AsyncIOInProgress);

        // The high level goal is to be tolerant of encoding errors when we read and very strict 
        // when we write. Hence, default StreamWriter encoding will throw on encoding error.   
        // Note: when StreamWriter throws on invalid encoding chars (for ex, high surrogate character 
        // D800-DBFF without a following low surrogate character DC00-DFFF), it will cause the 
        // internal StreamWriter's state to be irrecoverable as it would have buffered the 
        // illegal chars and any subsequent call to Flush() would hit the encoding error again. 
        // Even Close() will hit the exception as it would try to flush the unwritten data. 
        // Maybe we can add a DiscardBufferedData() method to get out of such situation (like 
        // StreamReader though for different reason). Either way, the buffered data will be lost!
        private static Encoding UTF8NoBOM => EncodingCache.UTF8NoBOM;

        public StreamWriter(Stream stream)
            : this(stream, UTF8NoBOM, DefaultBufferSize, false)
        {
        }

        public StreamWriter(Stream stream, Encoding encoding)
            : this(stream, encoding, DefaultBufferSize, false)
        {
        }

        // Creates a new StreamWriter for the given stream.  The 
        // character encoding is set by encoding and the buffer size, 
        // in number of 16-bit characters, is set by bufferSize.  
        // 
        public StreamWriter(Stream stream, Encoding encoding, int bufferSize)
            : this(stream, encoding, bufferSize, false)
        {
        }

        public StreamWriter(Stream stream, Encoding? encoding = null, int bufferSize = -1, bool leaveOpen = false)
            : base(null) // Ask for CurrentCulture all the time
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }
            if (encoding == null)
            {
                encoding = UTF8NoBOM;
            }
            if (!stream.CanWrite)
            {
                throw new ArgumentException(SR.Argument_StreamNotWritable);
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
            _encoding = encoding;
            _encoder = _encoding.GetEncoder();
            if (bufferSize < MinBufferSize)
            {
                bufferSize = MinBufferSize;
            }

            _charBuffer = new char[bufferSize];
            _byteBuffer = new byte[_encoding.GetMaxByteCount(bufferSize)];
            _charLen = bufferSize;
            // If we're appending to a Stream that already has data, don't write
            // the preamble.
            if (_stream.CanSeek && _stream.Position > 0)
            {
                _haveWrittenPreamble = true;
            }

            _closable = !leaveOpen;
        }

        public StreamWriter(string path)
            : this(path, false, UTF8NoBOM, DefaultBufferSize)
        {
        }

        public StreamWriter(string path, bool append)
            : this(path, append, UTF8NoBOM, DefaultBufferSize)
        {
        }

        public StreamWriter(string path, bool append, Encoding encoding)
            : this(path, append, encoding, DefaultBufferSize)
        {
        }

        public StreamWriter(string path, bool append, Encoding encoding, int bufferSize) :
            this(ValidateArgsAndOpenPath(path, append, encoding, bufferSize), encoding, bufferSize, leaveOpen: false)
        { 
        }

        private static Stream ValidateArgsAndOpenPath(string path, bool append, Encoding encoding, int bufferSize)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));
            if (encoding == null)
                throw new ArgumentNullException(nameof(encoding));
            if (path.Length == 0)
                throw new ArgumentException(SR.Argument_EmptyPath);
            if (bufferSize <= 0)
                throw new ArgumentOutOfRangeException(nameof(bufferSize), SR.ArgumentOutOfRange_NeedPosNum);

            return new FileStream(path, append ? FileMode.Append : FileMode.Create, FileAccess.Write, FileShare.Read, DefaultFileStreamBufferSize, FileOptions.SequentialScan);
        }

        public override void Close()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected override void Dispose(bool disposing)
        {
            try
            {
                // We need to flush any buffered data if we are being closed/disposed.
                // Also, we never close the handles for stdout & friends.  So we can safely 
                // write any buffered data to those streams even during finalization, which 
                // is generally the right thing to do.
                if (!_disposed && disposing)
                {
                    // Note: flush on the underlying stream can throw (ex., low disk space)
                    CheckAsyncTaskInProgress();
                    Flush(flushStream: true, flushEncoder: true);
                }
            }
            finally
            {
                CloseStreamFromDispose(disposing);
            }
        }

        private void CloseStreamFromDispose(bool disposing)
        {
            // Dispose of our resources if this StreamWriter is closable. 
            if (!LeaveOpen && !_disposed)
            {
                try
                {
                    // Attempt to close the stream even if there was an IO error from Flushing.
                    // Note that Stream.Close() can potentially throw here (may or may not be
                    // due to the same Flush error). In this case, we still need to ensure 
                    // cleaning up internal resources, hence the finally block.  
                    if (disposing)
                    {
                        _stream.Close();
                    }
                }
                finally
                {
                    _disposed = true;
                    _charLen = 0;
                    base.Dispose(disposing);
                }
            }
        }

        public override ValueTask DisposeAsync() =>
            GetType() != typeof(StreamWriter) ?
                base.DisposeAsync() :
                DisposeAsyncCore();

        private async ValueTask DisposeAsyncCore()
        {
            // Same logic as in Dispose(), but with async flushing.
            Debug.Assert(GetType() == typeof(StreamWriter));
            try
            {
                if (!_disposed)
                {
                    await FlushAsync().ConfigureAwait(false);
                }
            }
            finally
            {
                CloseStreamFromDispose(disposing: true);
            }
            GC.SuppressFinalize(this);
        }

        public override void Flush()
        {
            CheckAsyncTaskInProgress();

            Flush(true, true);
        }

        private void Flush(bool flushStream, bool flushEncoder)
        {
            // flushEncoder should be true at the end of the file and if
            // the user explicitly calls Flush (though not if AutoFlush is true).
            // This is required to flush any dangling characters from our UTF-7 
            // and UTF-8 encoders.
            ThrowIfDisposed();

            // Perf boost for Flush on non-dirty writers.
            if (_charPos == 0 && !flushStream && !flushEncoder)
            {
                return;
            }

            if (!_haveWrittenPreamble)
            {
                _haveWrittenPreamble = true;
                ReadOnlySpan<byte> preamble = _encoding.Preamble;
                if (preamble.Length > 0)
                {
                    _stream.Write(preamble);
                }
            }

            int count = _encoder.GetBytes(_charBuffer, 0, _charPos, _byteBuffer, 0, flushEncoder);
            _charPos = 0;
            if (count > 0)
            {
                _stream.Write(_byteBuffer, 0, count);
            }
            // By definition, calling Flush should flush the stream, but this is
            // only necessary if we passed in true for flushStream.  The Web
            // Services guys have some perf tests where flushing needlessly hurts.
            if (flushStream)
            {
                _stream.Flush();
            }
        }

        public virtual bool AutoFlush
        {
            get { return _autoFlush; }

            set
            {
                CheckAsyncTaskInProgress();

                _autoFlush = value;
                if (value)
                {
                    Flush(true, false);
                }
            }
        }

        public virtual Stream BaseStream
        {
            get { return _stream; }
        }

        internal bool LeaveOpen
        {
            get { return !_closable; }
        }

        internal bool HaveWrittenPreamble
        {
            set { _haveWrittenPreamble = value; }
        }

        public override Encoding Encoding
        {
            get { return _encoding; }
        }

        public override void Write(char value)
        {
            CheckAsyncTaskInProgress();

            if (_charPos == _charLen)
            {
                Flush(false, false);
            }

            _charBuffer[_charPos] = value;
            _charPos++;
            if (_autoFlush)
            {
                Flush(true, false);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)] // prevent WriteSpan from bloating call sites
        public override void Write(char[]? buffer)
        {
            WriteSpan(buffer, appendNewLine: false);
        }

        [MethodImpl(MethodImplOptions.NoInlining)] // prevent WriteSpan from bloating call sites
        public override void Write(char[] buffer, int index, int count)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer), SR.ArgumentNull_Buffer);
            }
            if (index < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(index), SR.ArgumentOutOfRange_NeedNonNegNum);
            }
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count), SR.ArgumentOutOfRange_NeedNonNegNum);
            }
            if (buffer.Length - index < count)
            {
                throw new ArgumentException(SR.Argument_InvalidOffLen);
            }

            WriteSpan(buffer.AsSpan(index, count), appendNewLine: false);
        }

        [MethodImpl(MethodImplOptions.NoInlining)] // prevent WriteSpan from bloating call sites
        public override void Write(ReadOnlySpan<char> buffer)
        {
            if (GetType() == typeof(StreamWriter))
            {
                WriteSpan(buffer, appendNewLine: false);
            }
            else
            {
                // If a derived class may have overridden existing Write behavior,
                // we need to make sure we use it.
                base.Write(buffer);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private unsafe void WriteSpan(ReadOnlySpan<char> buffer, bool appendNewLine)
        {
            CheckAsyncTaskInProgress();

            if (buffer.Length <= 4 && // Threshold of 4 chosen based on perf experimentation
                buffer.Length <= _charLen - _charPos)
            {
                // For very short buffers and when we don't need to worry about running out of space
                // in the char buffer, just copy the chars individually.
                for (int i = 0; i < buffer.Length; i++)
                {
                    _charBuffer[_charPos++] = buffer[i];
                }
            }
            else
            {
                // For larger buffers or when we may run out of room in the internal char buffer, copy in chunks.
                // Use unsafe code until https://github.com/dotnet/coreclr/issues/13827 is addressed, as spans are
                // resulting in significant overhead (even when the if branch above is taken rather than this
                // else) due to temporaries that need to be cleared.  Given the use of unsafe code, we also
                // make local copies of instance state to protect against potential concurrent misuse.

                ThrowIfDisposed();
                char[] charBuffer = _charBuffer;

                fixed (char* bufferPtr = &MemoryMarshal.GetReference(buffer))
                fixed (char* dstPtr = &charBuffer[0])
                {
                    char* srcPtr = bufferPtr;
                    int count = buffer.Length;
                    int dstPos = _charPos; // use a local copy of _charPos for safety
                    while (count > 0)
                    {
                        if (dstPos == charBuffer.Length)
                        {
                            Flush(false, false);
                            dstPos = 0;
                        }

                        int n = Math.Min(charBuffer.Length - dstPos, count);
                        int bytesToCopy = n * sizeof(char);

                        Buffer.MemoryCopy(srcPtr, dstPtr + dstPos, bytesToCopy, bytesToCopy);

                        _charPos += n;
                        dstPos += n;
                        srcPtr += n;
                        count -= n;
                    }
                }
            }

            if (appendNewLine)
            {
                char[] coreNewLine = CoreNewLine;
                for (int i = 0; i < coreNewLine.Length; i++) // Generally 1 (\n) or 2 (\r\n) iterations
                {
                    if (_charPos == _charLen)
                    {
                        Flush(false, false);
                    }

                    _charBuffer[_charPos] = coreNewLine[i];
                    _charPos++;
                }
            }

            if (_autoFlush)
            {
                Flush(true, false);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)] // prevent WriteSpan from bloating call sites
        public override void Write(string? value)
        {
            WriteSpan(value, appendNewLine: false);
        }

        [MethodImpl(MethodImplOptions.NoInlining)] // prevent WriteSpan from bloating call sites
        public override void WriteLine(string? value)
        {
            CheckAsyncTaskInProgress();
            WriteSpan(value, appendNewLine: true);
        }

        [MethodImpl(MethodImplOptions.NoInlining)] // prevent WriteSpan from bloating call sites
        public override void WriteLine(ReadOnlySpan<char> value)
        {
            if (GetType() == typeof(StreamWriter))
            {
                CheckAsyncTaskInProgress();
                WriteSpan(value, appendNewLine: true);
            }
            else
            {
                // If a derived class may have overridden existing WriteLine behavior,
                // we need to make sure we use it.
                base.WriteLine(value);
            }
        }

        private void WriteFormatHelper(string format, ParamsArray args, bool appendNewLine)
        {
            StringBuilder sb =
                StringBuilderCache.Acquire((format?.Length ?? 0) + args.Length * 8)
                .AppendFormatHelper(null, format!, args); // AppendFormatHelper will appropriately throw ArgumentNullException for a null format

            StringBuilder.ChunkEnumerator chunks = sb.GetChunks();

            bool more = chunks.MoveNext();
            while (more)
            {
                ReadOnlySpan<char> current = chunks.Current.Span;
                more = chunks.MoveNext();

                // If final chunk, include the newline if needed
                WriteSpan(current, appendNewLine: more ? false : appendNewLine);
            }

            StringBuilderCache.Release(sb);
        }

        public override void Write(string format, object? arg0)
        {
            if (GetType() == typeof(StreamWriter))
            {
                WriteFormatHelper(format, new ParamsArray(arg0), appendNewLine: false);
            }
            else
            {
                base.Write(format, arg0);
            }
        }

        public override void Write(string format, object? arg0, object? arg1)
        {
            if (GetType() == typeof(StreamWriter))
            {
                WriteFormatHelper(format, new ParamsArray(arg0, arg1), appendNewLine: false);
            }
            else
            {
                base.Write(format, arg0, arg1);
            }
        }

        public override void Write(string format, object? arg0, object? arg1, object? arg2)
        {
            if (GetType() == typeof(StreamWriter))
            {
                WriteFormatHelper(format, new ParamsArray(arg0, arg1, arg2), appendNewLine: false);
            }
            else
            {
                base.Write(format, arg0, arg1, arg2);
            }
        }

        public override void Write(string format, params object?[] arg)
        {
            if (GetType() == typeof(StreamWriter))
            {
                if (arg == null)
                {
                    throw new ArgumentNullException((format == null) ? nameof(format) : nameof(arg)); // same as base logic
                }
                WriteFormatHelper(format, new ParamsArray(arg), appendNewLine: false);
            }
            else
            {
                base.Write(format, arg);
            }
        }

        public override void WriteLine(string format, object? arg0)
        {
            if (GetType() == typeof(StreamWriter))
            {
                WriteFormatHelper(format, new ParamsArray(arg0), appendNewLine: true);
            }
            else
            {
                base.WriteLine(format, arg0);
            }
        }

        public override void WriteLine(string format, object? arg0, object? arg1)
        {
            if (GetType() == typeof(StreamWriter))
            {
                WriteFormatHelper(format, new ParamsArray(arg0, arg1), appendNewLine: true);
            }
            else
            {
                base.WriteLine(format, arg0, arg1);
            }
        }

        public override void WriteLine(string format, object? arg0, object? arg1, object? arg2)
        {
            if (GetType() == typeof(StreamWriter))
            {
                WriteFormatHelper(format, new ParamsArray(arg0, arg1, arg2), appendNewLine: true);
            }
            else
            {
                base.WriteLine(format, arg0, arg1, arg2);
            }
        }

        public override void WriteLine(string format, params object?[] arg)
        {
            if (GetType() == typeof(StreamWriter))
            {
                if (arg == null)
                {
                    throw new ArgumentNullException(nameof(arg));
                }
                WriteFormatHelper(format, new ParamsArray(arg), appendNewLine: true);
            }
            else
            {
                base.WriteLine(format, arg);
            }
        }

        public override Task WriteAsync(char value)
        {
            // If we have been inherited into a subclass, the following implementation could be incorrect
            // since it does not call through to Write() which a subclass might have overridden.  
            // To be safe we will only use this implementation in cases where we know it is safe to do so,
            // and delegate to our base class (which will call into Write) when we are not sure.
            if (GetType() != typeof(StreamWriter))
            {
                return base.WriteAsync(value);
            }

            ThrowIfDisposed();
            CheckAsyncTaskInProgress();

            Task task = WriteAsyncInternal(this, value, _charBuffer, _charPos, _charLen, CoreNewLine, _autoFlush, appendNewLine: false);
            _asyncWriteTask = task;

            return task;
        }

        // We pass in private instance fields of this MarshalByRefObject-derived type as local params
        // to ensure performant access inside the state machine that corresponds this async method.
        // Fields that are written to must be assigned at the end of the method *and* before instance invocations.
        private static async Task WriteAsyncInternal(StreamWriter _this, char value,
                                                     char[] charBuffer, int charPos, int charLen, char[] coreNewLine,
                                                     bool autoFlush, bool appendNewLine)
        {
            if (charPos == charLen)
            {
                await _this.FlushAsyncInternal(false, false, charBuffer, charPos).ConfigureAwait(false);
                Debug.Assert(_this._charPos == 0);
                charPos = 0;
            }

            charBuffer[charPos] = value;
            charPos++;

            if (appendNewLine)
            {
                for (int i = 0; i < coreNewLine.Length; i++)   // Expect 2 iterations, no point calling BlockCopy
                {
                    if (charPos == charLen)
                    {
                        await _this.FlushAsyncInternal(false, false, charBuffer, charPos).ConfigureAwait(false);
                        Debug.Assert(_this._charPos == 0);
                        charPos = 0;
                    }

                    charBuffer[charPos] = coreNewLine[i];
                    charPos++;
                }
            }

            if (autoFlush)
            {
                await _this.FlushAsyncInternal(true, false, charBuffer, charPos).ConfigureAwait(false);
                Debug.Assert(_this._charPos == 0);
                charPos = 0;
            }

            _this.CharPos_Prop = charPos;
        }

        public override Task WriteAsync(string? value)
        {
            // If we have been inherited into a subclass, the following implementation could be incorrect
            // since it does not call through to Write() which a subclass might have overridden.  
            // To be safe we will only use this implementation in cases where we know it is safe to do so,
            // and delegate to our base class (which will call into Write) when we are not sure.
            if (GetType() != typeof(StreamWriter))
            {
                return base.WriteAsync(value);
            }

            if (value != null)
            {
                ThrowIfDisposed();
                CheckAsyncTaskInProgress();

                Task task = WriteAsyncInternal(this, value, _charBuffer, _charPos, _charLen, CoreNewLine, _autoFlush, appendNewLine: false);
                _asyncWriteTask = task;

                return task;
            }
            else
            {
                return Task.CompletedTask;
            }
        }

        // We pass in private instance fields of this MarshalByRefObject-derived type as local params
        // to ensure performant access inside the state machine that corresponds this async method.
        // Fields that are written to must be assigned at the end of the method *and* before instance invocations.
        private static async Task WriteAsyncInternal(StreamWriter _this, string value,
                                                     char[] charBuffer, int charPos, int charLen, char[] coreNewLine,
                                                     bool autoFlush, bool appendNewLine)
        {
            Debug.Assert(value != null);

            int count = value.Length;
            int index = 0;

            while (count > 0)
            {
                if (charPos == charLen)
                {
                    await _this.FlushAsyncInternal(false, false, charBuffer, charPos).ConfigureAwait(false);
                    Debug.Assert(_this._charPos == 0);
                    charPos = 0;
                }

                int n = charLen - charPos;
                if (n > count)
                {
                    n = count;
                }

                Debug.Assert(n > 0, "StreamWriter::Write(String) isn't making progress!  This is most likely a race condition in user code.");

                value.CopyTo(index, charBuffer, charPos, n);

                charPos += n;
                index += n;
                count -= n;
            }

            if (appendNewLine)
            {
                for (int i = 0; i < coreNewLine.Length; i++)   // Expect 2 iterations, no point calling BlockCopy
                {
                    if (charPos == charLen)
                    {
                        await _this.FlushAsyncInternal(false, false, charBuffer, charPos).ConfigureAwait(false);
                        Debug.Assert(_this._charPos == 0);
                        charPos = 0;
                    }

                    charBuffer[charPos] = coreNewLine[i];
                    charPos++;
                }
            }

            if (autoFlush)
            {
                await _this.FlushAsyncInternal(true, false, charBuffer, charPos).ConfigureAwait(false);
                Debug.Assert(_this._charPos == 0);
                charPos = 0;
            }

            _this.CharPos_Prop = charPos;
        }

        public override Task WriteAsync(char[] buffer, int index, int count)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer), SR.ArgumentNull_Buffer);
            }
            if (index < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(index), SR.ArgumentOutOfRange_NeedNonNegNum);
            }
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count), SR.ArgumentOutOfRange_NeedNonNegNum);
            }
            if (buffer.Length - index < count)
            {
                throw new ArgumentException(SR.Argument_InvalidOffLen);
            }

            // If we have been inherited into a subclass, the following implementation could be incorrect
            // since it does not call through to Write() which a subclass might have overridden.  
            // To be safe we will only use this implementation in cases where we know it is safe to do so,
            // and delegate to our base class (which will call into Write) when we are not sure.
            if (GetType() != typeof(StreamWriter))
            {
                return base.WriteAsync(buffer, index, count);
            }

            ThrowIfDisposed();
            CheckAsyncTaskInProgress();

            Task task = WriteAsyncInternal(this, new ReadOnlyMemory<char>(buffer, index, count), _charBuffer, _charPos, _charLen, CoreNewLine, _autoFlush, appendNewLine: false, cancellationToken: default);
            _asyncWriteTask = task;

            return task;
        }

        public override Task WriteAsync(ReadOnlyMemory<char> buffer, CancellationToken cancellationToken = default)
        {
            if (GetType() != typeof(StreamWriter))
            {
                // If a derived type may have overridden existing WriteASync(char[], ...) behavior, make sure we use it.
                return base.WriteAsync(buffer, cancellationToken);
            }

            ThrowIfDisposed();
            CheckAsyncTaskInProgress();

            if (cancellationToken.IsCancellationRequested)
            {
                return Task.FromCanceled(cancellationToken);
            }

            Task task = WriteAsyncInternal(this, buffer, _charBuffer, _charPos, _charLen, CoreNewLine, _autoFlush, appendNewLine: false, cancellationToken: cancellationToken);
            _asyncWriteTask = task;
            return task;
        }

        // We pass in private instance fields of this MarshalByRefObject-derived type as local params
        // to ensure performant access inside the state machine that corresponds this async method.
        // Fields that are written to must be assigned at the end of the method *and* before instance invocations.
        private static async Task WriteAsyncInternal(StreamWriter _this, ReadOnlyMemory<char> source,
                                                     char[] charBuffer, int charPos, int charLen, char[] coreNewLine,
                                                     bool autoFlush, bool appendNewLine, CancellationToken cancellationToken)
        {
            int copied = 0;
            while (copied < source.Length)
            {
                if (charPos == charLen)
                {
                    await _this.FlushAsyncInternal(false, false, charBuffer, charPos, cancellationToken).ConfigureAwait(false);
                    Debug.Assert(_this._charPos == 0);
                    charPos = 0;
                }

                int n = Math.Min(charLen - charPos, source.Length - copied);
                Debug.Assert(n > 0, "StreamWriter::Write(char[], int, int) isn't making progress!  This is most likely a race condition in user code.");

                source.Span.Slice(copied, n).CopyTo(new Span<char>(charBuffer, charPos, n));
                charPos += n;
                copied += n;
            }

            if (appendNewLine)
            {
                for (int i = 0; i < coreNewLine.Length; i++)   // Expect 2 iterations, no point calling BlockCopy
                {
                    if (charPos == charLen)
                    {
                        await _this.FlushAsyncInternal(false, false, charBuffer, charPos, cancellationToken).ConfigureAwait(false);
                        Debug.Assert(_this._charPos == 0);
                        charPos = 0;
                    }

                    charBuffer[charPos] = coreNewLine[i];
                    charPos++;
                }
            }

            if (autoFlush)
            {
                await _this.FlushAsyncInternal(true, false, charBuffer, charPos, cancellationToken).ConfigureAwait(false);
                Debug.Assert(_this._charPos == 0);
                charPos = 0;
            }

            _this.CharPos_Prop = charPos;
        }

        public override Task WriteLineAsync()
        {
            // If we have been inherited into a subclass, the following implementation could be incorrect
            // since it does not call through to Write() which a subclass might have overridden.  
            // To be safe we will only use this implementation in cases where we know it is safe to do so,
            // and delegate to our base class (which will call into Write) when we are not sure.
            if (GetType() != typeof(StreamWriter))
            {
                return base.WriteLineAsync();
            }

            ThrowIfDisposed();
            CheckAsyncTaskInProgress();

            Task task = WriteAsyncInternal(this, ReadOnlyMemory<char>.Empty, _charBuffer, _charPos, _charLen, CoreNewLine, _autoFlush, appendNewLine: true, cancellationToken: default);
            _asyncWriteTask = task;

            return task;
        }


        public override Task WriteLineAsync(char value)
        {
            // If we have been inherited into a subclass, the following implementation could be incorrect
            // since it does not call through to Write() which a subclass might have overridden.  
            // To be safe we will only use this implementation in cases where we know it is safe to do so,
            // and delegate to our base class (which will call into Write) when we are not sure.
            if (GetType() != typeof(StreamWriter))
            {
                return base.WriteLineAsync(value);
            }

            ThrowIfDisposed();
            CheckAsyncTaskInProgress();

            Task task = WriteAsyncInternal(this, value, _charBuffer, _charPos, _charLen, CoreNewLine, _autoFlush, appendNewLine: true);
            _asyncWriteTask = task;

            return task;
        }


        public override Task WriteLineAsync(string? value)
        {
            if (value == null)
            {
                return WriteLineAsync();
            }

            // If we have been inherited into a subclass, the following implementation could be incorrect
            // since it does not call through to Write() which a subclass might have overridden.  
            // To be safe we will only use this implementation in cases where we know it is safe to do so,
            // and delegate to our base class (which will call into Write) when we are not sure.
            if (GetType() != typeof(StreamWriter))
            {
                return base.WriteLineAsync(value);
            }

            ThrowIfDisposed();
            CheckAsyncTaskInProgress();

            Task task = WriteAsyncInternal(this, value, _charBuffer, _charPos, _charLen, CoreNewLine, _autoFlush, appendNewLine: true);
            _asyncWriteTask = task;

            return task;
        }


        public override Task WriteLineAsync(char[] buffer, int index, int count)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer), SR.ArgumentNull_Buffer);
            }
            if (index < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(index), SR.ArgumentOutOfRange_NeedNonNegNum);
            }
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count), SR.ArgumentOutOfRange_NeedNonNegNum);
            }
            if (buffer.Length - index < count)
            {
                throw new ArgumentException(SR.Argument_InvalidOffLen);
            }

            // If we have been inherited into a subclass, the following implementation could be incorrect
            // since it does not call through to Write() which a subclass might have overridden.  
            // To be safe we will only use this implementation in cases where we know it is safe to do so,
            // and delegate to our base class (which will call into Write) when we are not sure.
            if (GetType() != typeof(StreamWriter))
            {
                return base.WriteLineAsync(buffer, index, count);
            }

            ThrowIfDisposed();
            CheckAsyncTaskInProgress();

            Task task = WriteAsyncInternal(this, new ReadOnlyMemory<char>(buffer, index, count), _charBuffer, _charPos, _charLen, CoreNewLine, _autoFlush, appendNewLine: true, cancellationToken: default);
            _asyncWriteTask = task;

            return task;
        }

        public override Task WriteLineAsync(ReadOnlyMemory<char> buffer, CancellationToken cancellationToken = default)
        {
            if (GetType() != typeof(StreamWriter))
            {
                return base.WriteLineAsync(buffer, cancellationToken);
            }

            ThrowIfDisposed();
            CheckAsyncTaskInProgress();

            if (cancellationToken.IsCancellationRequested)
            {
                return Task.FromCanceled(cancellationToken);
            }

            Task task = WriteAsyncInternal(this, buffer, _charBuffer, _charPos, _charLen, CoreNewLine, _autoFlush, appendNewLine: true, cancellationToken: cancellationToken);
            _asyncWriteTask = task;

            return task;
        }


        public override Task FlushAsync()
        {
            // If we have been inherited into a subclass, the following implementation could be incorrect
            // since it does not call through to Flush() which a subclass might have overridden.  To be safe 
            // we will only use this implementation in cases where we know it is safe to do so,
            // and delegate to our base class (which will call into Flush) when we are not sure.
            if (GetType() != typeof(StreamWriter))
            {
                return base.FlushAsync();
            }

            // flushEncoder should be true at the end of the file and if
            // the user explicitly calls Flush (though not if AutoFlush is true).
            // This is required to flush any dangling characters from our UTF-7 
            // and UTF-8 encoders.  
            ThrowIfDisposed();
            CheckAsyncTaskInProgress();

            Task task = FlushAsyncInternal(true, true, _charBuffer, _charPos);
            _asyncWriteTask = task;

            return task;
        }

        private int CharPos_Prop
        {
            set { _charPos = value; }
        }

        private bool HaveWrittenPreamble_Prop
        {
            set { _haveWrittenPreamble = value; }
        }

        private Task FlushAsyncInternal(bool flushStream, bool flushEncoder,
                                        char[] sCharBuffer, int sCharPos, CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return Task.FromCanceled(cancellationToken);
            }

            // Perf boost for Flush on non-dirty writers.
            if (sCharPos == 0 && !flushStream && !flushEncoder)
            {
                return Task.CompletedTask;
            }

            Task flushTask = FlushAsyncInternal(this, flushStream, flushEncoder, sCharBuffer, sCharPos, _haveWrittenPreamble,
                                                _encoding, _encoder, _byteBuffer, _stream, cancellationToken);

            _charPos = 0;
            return flushTask;
        }


        // We pass in private instance fields of this MarshalByRefObject-derived type as local params
        // to ensure performant access inside the state machine that corresponds this async method.
        private static async Task FlushAsyncInternal(StreamWriter _this, bool flushStream, bool flushEncoder,
                                                     char[] charBuffer, int charPos, bool haveWrittenPreamble,
                                                     Encoding encoding, Encoder encoder, byte[] byteBuffer, Stream stream, CancellationToken cancellationToken)
        {
            if (!haveWrittenPreamble)
            {
                _this.HaveWrittenPreamble_Prop = true;
                byte[] preamble = encoding.GetPreamble();
                if (preamble.Length > 0)
                {
                    await stream.WriteAsync(new ReadOnlyMemory<byte>(preamble), cancellationToken).ConfigureAwait(false);
                }
            }

            int count = encoder.GetBytes(charBuffer, 0, charPos, byteBuffer, 0, flushEncoder);
            if (count > 0)
            {
                await stream.WriteAsync(new ReadOnlyMemory<byte>(byteBuffer, 0, count), cancellationToken).ConfigureAwait(false);
            }

            // By definition, calling Flush should flush the stream, but this is
            // only necessary if we passed in true for flushStream.  The Web
            // Services guys have some perf tests where flushing needlessly hurts.
            if (flushStream)
            {
                await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                ThrowObjectDisposedException();
            }

            void ThrowObjectDisposedException() => throw new ObjectDisposedException(GetType().Name, SR.ObjectDisposed_WriterClosed);
        }
    }  // class StreamWriter
}  // namespace
