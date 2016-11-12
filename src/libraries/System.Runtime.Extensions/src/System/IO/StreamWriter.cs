// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace System.IO
{
    // This class implements a TextWriter for writing characters to a Stream.
    // This is designed for character output in a particular Encoding, 
    // whereas the Stream class is designed for byte input and output.  
    [Serializable]
    public class StreamWriter : TextWriter
    {
        // For UTF-8, the values of 1K for the default buffer size and 4K for the
        // file stream buffer size are reasonable & give very reasonable
        // performance for in terms of construction time for the StreamWriter and
        // write perf.  Note that for UTF-8, we end up allocating a 4K byte buffer,
        // which means we take advantage of adaptive buffering code.
        // The performance using UnicodeEncoding is acceptable.  
        internal const int DefaultBufferSize = 1024;   // char[]
        private const int DefaultFileStreamBufferSize = 4096;
        private const int MinBufferSize = 128;

        private const int DontCopyOnWriteLineThreshold = 512;

        // Bit bucket - Null has no backing store. Non closable.
        public new static readonly StreamWriter Null = new StreamWriter(Stream.Null, UTF8NoBOM, MinBufferSize, true);

        private Stream _stream;
        private Encoding _encoding;
        private Encoder _encoder;
        private byte[] _byteBuffer;
        private char[] _charBuffer;
        private int _charPos;
        private int _charLen;
        private bool _autoFlush;
        private bool _haveWrittenPreamble;
        private bool _closable;

        // We don't guarantee thread safety on StreamWriter, but we should at 
        // least prevent users from trying to write anything while an Async
        // write from the same thread is in progress.
        [NonSerialized]
        private volatile Task _asyncWriteTask;

        private void CheckAsyncTaskInProgress()
        {
            // We are not locking the access to _asyncWriteTask because this is not meant to guarantee thread safety. 
            // We are simply trying to deter calling any Write APIs while an async Write from the same thread is in progress.

            Task t = _asyncWriteTask;

            if (t != null && !t.IsCompleted)
                throw new InvalidOperationException(SR.InvalidOperation_AsyncIOInProgress);
        }

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


        internal StreamWriter() : base(null)
        { // Ask for CurrentCulture all the time 
        }

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

        public StreamWriter(Stream stream, Encoding encoding, int bufferSize, bool leaveOpen)
            : base(null) // Ask for CurrentCulture all the time
        {
            if (stream == null || encoding == null)
            {
                throw new ArgumentNullException(stream == null ? nameof(stream) : nameof(encoding));
            }
            if (!stream.CanWrite)
            {
                throw new ArgumentException(SR.Argument_StreamNotWritable);
            }
            if (bufferSize <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(bufferSize), SR.ArgumentOutOfRange_NeedPosNum);
            }

            Init(stream, encoding, bufferSize, leaveOpen);
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

        public StreamWriter(string path, bool append, Encoding encoding, int bufferSize)
        { 
            if (path == null)
                throw new ArgumentNullException(nameof(path));
            if (encoding == null)
                throw new ArgumentNullException(nameof(encoding));
            if (path.Length == 0)
                throw new ArgumentException(SR.Argument_EmptyPath);
            if (bufferSize <= 0)
                throw new ArgumentOutOfRangeException(nameof(bufferSize), SR.ArgumentOutOfRange_NeedPosNum);

            Stream stream = FileStreamHelpers.CreateFileStream(path, write: true, append: append);
            Init(stream, encoding, bufferSize, shouldLeaveOpen: false);
        }

        private void Init(Stream streamArg, Encoding encodingArg, int bufferSize, bool shouldLeaveOpen)
        {
            _stream = streamArg;
            _encoding = encodingArg;
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

            _closable = !shouldLeaveOpen;
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
                if (_stream != null)
                {
                    // Note: flush on the underlying stream can throw (ex., low disk space)
                    if (disposing /* || (LeaveOpen && stream is __ConsoleStream) */)
                    {
                        CheckAsyncTaskInProgress();

                        Flush(true, true);
                    }
                }
            }
            finally
            {
                // Dispose of our resources if this StreamWriter is closable. 
                // Note: Console.Out and other such non closable streamwriters should be left alone 
                if (!LeaveOpen && _stream != null)
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
                        _stream = null;
                        _byteBuffer = null;
                        _charBuffer = null;
                        _encoding = null;
                        _encoder = null;
                        _charLen = 0;
                        base.Dispose(disposing);
                    }
                }
            }
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
            if (_stream == null)
            {
                throw new ObjectDisposedException(null, SR.ObjectDisposed_WriterClosed);
            }

            // Perf boost for Flush on non-dirty writers.
            if (_charPos == 0 && !flushStream && !flushEncoder)
            {
                return;
            }

            if (!_haveWrittenPreamble)
            {
                _haveWrittenPreamble = true;
                byte[] preamble = _encoding.GetPreamble();
                if (preamble.Length > 0)
                {
                    _stream.Write(preamble, 0, preamble.Length);
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

        public override void Write(char[] buffer)
        {
            // This may be faster than the one with the index & count since it
            // has to do less argument checking.
            if (buffer == null)
            {
                return;
            }

            CheckAsyncTaskInProgress();

            int index = 0;
            int count = buffer.Length;
            while (count > 0)
            {
                if (_charPos == _charLen)
                {
                    Flush(false, false);
                }

                int n = _charLen - _charPos;
                if (n > count)
                {
                    n = count;
                }

                Debug.Assert(n > 0, "StreamWriter::Write(char[]) isn't making progress!  This is most likely a race in user code.");
                Buffer.BlockCopy(buffer, index * sizeof(char), _charBuffer, _charPos * sizeof(char), n * sizeof(char));
                _charPos += n;
                index += n;
                count -= n;
            }

            if (_autoFlush)
            {
                Flush(true, false);
            }
        }

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

            CheckAsyncTaskInProgress();

            while (count > 0)
            {
                if (_charPos == _charLen)
                {
                    Flush(false, false);
                }

                int n = _charLen - _charPos;
                if (n > count)
                {
                    n = count;
                }

                Debug.Assert(n > 0, "StreamWriter::Write(char[], int, int) isn't making progress!  This is most likely a race condition in user code.");
                Buffer.BlockCopy(buffer, index * sizeof(char), _charBuffer, _charPos * sizeof(char), n * sizeof(char));
                _charPos += n;
                index += n;
                count -= n;
            }

            if (_autoFlush)
            {
                Flush(true, false);
            }
        }

        public override void Write(string value)
        {
            if (value == null)
            {
                return;
            }

            CheckAsyncTaskInProgress();

            int count = value.Length;
            int index = 0;
            while (count > 0)
            {
                if (_charPos == _charLen)
                {
                    Flush(false, false);
                }

                int n = _charLen - _charPos;
                if (n > count)
                {
                    n = count;
                }

                Debug.Assert(n > 0, "StreamWriter::Write(String) isn't making progress!  This is most likely a race condition in user code.");
                value.CopyTo(index, _charBuffer, _charPos, n);
                _charPos += n;
                index += n;
                count -= n;
            }

            if (_autoFlush)
            {
                Flush(true, false);
            }
        }

        //
        // Optimize the most commonly used WriteLine overload. This optimization is important for System.Console in particular
        // because of it will make one WriteLine equal to one call to the OS instead of two in the common case.
        //
        public override void WriteLine(string value)
        {
            if (value == null)
            {
                value = String.Empty;
            }

            CheckAsyncTaskInProgress();

            int count = value.Length;
            int index = 0;
            while (count > 0)
            {
                if (_charPos == _charLen)
                {
                    Flush(false, false);
                }

                int n = _charLen - _charPos;
                if (n > count)
                {
                    n = count;
                }

                Debug.Assert(n > 0, "StreamWriter::WriteLine(String) isn't making progress!  This is most likely a race condition in user code.");
                value.CopyTo(index, _charBuffer, _charPos, n);
                _charPos += n;
                index += n;
                count -= n;
            }

            char[] coreNewLine = CoreNewLine;
            for (int i = 0; i < coreNewLine.Length; i++)   // Expect 2 iterations, no point calling BlockCopy
            {
                if (_charPos == _charLen)
                {
                    Flush(false, false);
                }

                _charBuffer[_charPos] = coreNewLine[i];
                _charPos++;
            }

            if (_autoFlush)
            {
                Flush(true, false);
            }
        }

        #region Task based Async APIs
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

            if (_stream == null)
            {
                throw new ObjectDisposedException(null, SR.ObjectDisposed_WriterClosed);
            }

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

        public override Task WriteAsync(string value)
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
                if (_stream == null)
                {
                    throw new ObjectDisposedException(null, SR.ObjectDisposed_WriterClosed);
                }

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

            if (_stream == null)
            {
                throw new ObjectDisposedException(null, SR.ObjectDisposed_WriterClosed);
            }

            CheckAsyncTaskInProgress();

            Task task = WriteAsyncInternal(this, buffer, index, count, _charBuffer, _charPos, _charLen, CoreNewLine, _autoFlush, appendNewLine: false);
            _asyncWriteTask = task;

            return task;
        }

        // We pass in private instance fields of this MarshalByRefObject-derived type as local params
        // to ensure performant access inside the state machine that corresponds this async method.
        // Fields that are written to must be assigned at the end of the method *and* before instance invocations.
        private static async Task WriteAsyncInternal(StreamWriter _this, char[] buffer, int index, int count,
                                                     char[] charBuffer, int charPos, int charLen, char[] coreNewLine,
                                                     bool autoFlush, bool appendNewLine)
        {
            Debug.Assert(count == 0 || (count > 0 && buffer != null));
            Debug.Assert(index >= 0);
            Debug.Assert(count >= 0);
            Debug.Assert(buffer == null || (buffer != null && buffer.Length - index >= count));

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

                Debug.Assert(n > 0, "StreamWriter::Write(char[], int, int) isn't making progress!  This is most likely a race condition in user code.");

                Buffer.BlockCopy(buffer, index * sizeof(char), charBuffer, charPos * sizeof(char), n * sizeof(char));

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

            if (_stream == null)
            {
                throw new ObjectDisposedException(null, SR.ObjectDisposed_WriterClosed);
            }

            CheckAsyncTaskInProgress();

            Task task = WriteAsyncInternal(this, null, 0, 0, _charBuffer, _charPos, _charLen, CoreNewLine, _autoFlush, appendNewLine: true);
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

            if (_stream == null)
            {
                throw new ObjectDisposedException(null, SR.ObjectDisposed_WriterClosed);
            }

            CheckAsyncTaskInProgress();

            Task task = WriteAsyncInternal(this, value, _charBuffer, _charPos, _charLen, CoreNewLine, _autoFlush, appendNewLine: true);
            _asyncWriteTask = task;

            return task;
        }


        public override Task WriteLineAsync(string value)
        {
            // If we have been inherited into a subclass, the following implementation could be incorrect
            // since it does not call through to Write() which a subclass might have overridden.  
            // To be safe we will only use this implementation in cases where we know it is safe to do so,
            // and delegate to our base class (which will call into Write) when we are not sure.
            if (GetType() != typeof(StreamWriter))
            {
                return base.WriteLineAsync(value);
            }

            if (_stream == null)
            {
                throw new ObjectDisposedException(null, SR.ObjectDisposed_WriterClosed);
            }

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

            if (_stream == null)
            {
                throw new ObjectDisposedException(null, SR.ObjectDisposed_WriterClosed);
            }

            CheckAsyncTaskInProgress();

            Task task = WriteAsyncInternal(this, buffer, index, count, _charBuffer, _charPos, _charLen, CoreNewLine, _autoFlush, appendNewLine: true);
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
            if (_stream == null)
            {
                throw new ObjectDisposedException(null, SR.ObjectDisposed_WriterClosed);
            }

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
                                        char[] sCharBuffer, int sCharPos)
        {
            // Perf boost for Flush on non-dirty writers.
            if (sCharPos == 0 && !flushStream && !flushEncoder)
            {
                return Task.CompletedTask;
            }

            Task flushTask = FlushAsyncInternal(this, flushStream, flushEncoder, sCharBuffer, sCharPos, _haveWrittenPreamble,
                                                _encoding, _encoder, _byteBuffer, _stream);

            _charPos = 0;
            return flushTask;
        }


        // We pass in private instance fields of this MarshalByRefObject-derived type as local params
        // to ensure performant access inside the state machine that corresponds this async method.
        private static async Task FlushAsyncInternal(StreamWriter _this, bool flushStream, bool flushEncoder,
                                                     char[] charBuffer, int charPos, bool haveWrittenPreamble,
                                                     Encoding encoding, Encoder encoder, Byte[] byteBuffer, Stream stream)
        {
            if (!haveWrittenPreamble)
            {
                _this.HaveWrittenPreamble_Prop = true;
                byte[] preamble = encoding.GetPreamble();
                if (preamble.Length > 0)
                {
                    await stream.WriteAsync(preamble, 0, preamble.Length).ConfigureAwait(false);
                }
            }

            int count = encoder.GetBytes(charBuffer, 0, charPos, byteBuffer, 0, flushEncoder);
            if (count > 0)
            {
                await stream.WriteAsync(byteBuffer, 0, count).ConfigureAwait(false);
            }

            // By definition, calling Flush should flush the stream, but this is
            // only necessary if we passed in true for flushStream.  The Web
            // Services guys have some perf tests where flushing needlessly hurts.
            if (flushStream)
            {
                await stream.FlushAsync().ConfigureAwait(false);
            }
        }
        #endregion
    }  // class StreamWriter
}  // namespace
