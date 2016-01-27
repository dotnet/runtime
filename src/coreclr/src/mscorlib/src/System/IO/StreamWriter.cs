// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================
**
**
** 
**
**
** Purpose: For writing text to streams in a particular 
** encoding.
**
**
===========================================================*/
using System;
using System.Text;
using System.Threading;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;
using System.Security.Permissions;
using System.Runtime.Serialization;
using System.Diagnostics.Contracts;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace System.IO
{
    // This class implements a TextWriter for writing characters to a Stream.
    // This is designed for character output in a particular Encoding, 
    // whereas the Stream class is designed for byte input and output.  
    // 
    [Serializable]
    [ComVisible(true)]
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

        private const Int32 DontCopyOnWriteLineThreshold = 512;

        // Bit bucket - Null has no backing store. Non closable.
        public new static readonly StreamWriter Null = new StreamWriter(Stream.Null, new UTF8Encoding(false, true), MinBufferSize, true);

        private Stream stream;
        private Encoding encoding;
        private Encoder encoder;
        private byte[] byteBuffer;
        private char[] charBuffer;
        private int charPos;
        private int charLen;
        private bool autoFlush;
        private bool haveWrittenPreamble;
        private bool closable;

#if MDA_SUPPORTED
        [NonSerialized] 
        // For StreamWriterBufferedDataLost MDA
        private MdaHelper mdaHelper;
#endif

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
                throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_AsyncIOInProgress"));
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
        private static volatile Encoding _UTF8NoBOM;
        
        internal static Encoding UTF8NoBOM {
            [FriendAccessAllowed]
            get { 
                if (_UTF8NoBOM == null) {
                    // No need for double lock - we just want to avoid extra
                    // allocations in the common case.
                    UTF8Encoding noBOM = new UTF8Encoding(false, true);
                    Thread.MemoryBarrier();
                    _UTF8NoBOM = noBOM;
                }
                return _UTF8NoBOM;
            }
        }
                

        internal StreamWriter(): base(null) { // Ask for CurrentCulture all the time 
        }
        
        public StreamWriter(Stream stream) 
            : this(stream, UTF8NoBOM, DefaultBufferSize, false) {
        }
    
        public StreamWriter(Stream stream, Encoding encoding) 
            : this(stream, encoding, DefaultBufferSize, false) {
        }
        
        // Creates a new StreamWriter for the given stream.  The 
        // character encoding is set by encoding and the buffer size, 
        // in number of 16-bit characters, is set by bufferSize.  
        // 
        public StreamWriter(Stream stream, Encoding encoding, int bufferSize)
            : this(stream, encoding, bufferSize, false) { 
        }

        public StreamWriter(Stream stream, Encoding encoding, int bufferSize, bool leaveOpen)
            : base(null) // Ask for CurrentCulture all the time
        {
            if (stream == null || encoding == null)
                throw new ArgumentNullException((stream == null ? "stream" : "encoding"));
            if (!stream.CanWrite)
                throw new ArgumentException(Environment.GetResourceString("Argument_StreamNotWritable"));
            if (bufferSize <= 0) throw new ArgumentOutOfRangeException("bufferSize", Environment.GetResourceString("ArgumentOutOfRange_NeedPosNum"));
            Contract.EndContractBlock();

            Init(stream, encoding, bufferSize, leaveOpen);
        }

        public StreamWriter(String path) 
            : this(path, false, UTF8NoBOM, DefaultBufferSize) {
        }

        public StreamWriter(String path, bool append) 
            : this(path, append, UTF8NoBOM, DefaultBufferSize) {
        }

        public StreamWriter(String path, bool append, Encoding encoding) 
            : this(path, append, encoding, DefaultBufferSize) {
        }

        [System.Security.SecuritySafeCritical]
        public StreamWriter(String path, bool append, Encoding encoding, int bufferSize): this(path, append, encoding, bufferSize, true) { 
        }

        [System.Security.SecurityCritical]  
        internal StreamWriter(String path, bool append, Encoding encoding, int bufferSize, bool checkHost)
            : base(null)
        { // Ask for CurrentCulture all the time
            if (path == null)
                throw new ArgumentNullException("path");
            if (encoding == null)
                throw new ArgumentNullException("encoding");
            if (path.Length == 0)
                throw new ArgumentException(Environment.GetResourceString("Argument_EmptyPath"));
            if (bufferSize <= 0) throw new ArgumentOutOfRangeException("bufferSize", Environment.GetResourceString("ArgumentOutOfRange_NeedPosNum"));
            Contract.EndContractBlock();

            Stream stream = CreateFile(path, append, checkHost);
            Init(stream, encoding, bufferSize, false);
        }

        [System.Security.SecuritySafeCritical]
        private void Init(Stream streamArg, Encoding encodingArg, int bufferSize, bool shouldLeaveOpen)
        {
            this.stream = streamArg;
            this.encoding = encodingArg;
            this.encoder = encoding.GetEncoder();
            if (bufferSize < MinBufferSize) bufferSize = MinBufferSize;
            charBuffer = new char[bufferSize];
            byteBuffer = new byte[encoding.GetMaxByteCount(bufferSize)];
            charLen = bufferSize;
            // If we're appending to a Stream that already has data, don't write
            // the preamble.
            if (stream.CanSeek && stream.Position > 0)
                haveWrittenPreamble = true;
            closable = !shouldLeaveOpen;
#if MDA_SUPPORTED
            if (Mda.StreamWriterBufferedDataLost.Enabled) {
                String callstack = null;
                if (Mda.StreamWriterBufferedDataLost.CaptureAllocatedCallStack)
                    callstack = Environment.GetStackTrace(null, false);
                mdaHelper = new MdaHelper(this, callstack);
            }
#endif
        }

        [System.Security.SecurityCritical]  
        private static Stream CreateFile(String path, bool append, bool checkHost) {
            FileMode mode = append? FileMode.Append: FileMode.Create;
            FileStream f = new FileStream(path, mode, FileAccess.Write, FileShare.Read,
                DefaultFileStreamBufferSize, FileOptions.SequentialScan, Path.GetFileName(path), false, false, checkHost);
            return f;
        }

        public override void Close() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected override void Dispose(bool disposing) {
            try {
                // We need to flush any buffered data if we are being closed/disposed.
                // Also, we never close the handles for stdout & friends.  So we can safely 
                // write any buffered data to those streams even during finalization, which 
                // is generally the right thing to do.
                if (stream != null) {
                    // Note: flush on the underlying stream can throw (ex., low disk space)
                    if (disposing || (LeaveOpen && stream is __ConsoleStream))
                    {
                        CheckAsyncTaskInProgress();

                        Flush(true, true);
#if MDA_SUPPORTED
                        // Disable buffered data loss mda
                        if (mdaHelper != null)
                            GC.SuppressFinalize(mdaHelper);
#endif
                    }
                }
            }
            finally {
                // Dispose of our resources if this StreamWriter is closable. 
                // Note: Console.Out and other such non closable streamwriters should be left alone 
                if (!LeaveOpen && stream != null) {
                    try {
                        // Attempt to close the stream even if there was an IO error from Flushing.
                        // Note that Stream.Close() can potentially throw here (may or may not be
                        // due to the same Flush error). In this case, we still need to ensure 
                        // cleaning up internal resources, hence the finally block.  
                        if (disposing)
                            stream.Close();
                    }
                    finally {
                        stream = null;
                        byteBuffer = null;
                        charBuffer = null;
                        encoding = null;
                        encoder = null;
                        charLen = 0;
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
            if (stream == null)
                __Error.WriterClosed();

            // Perf boost for Flush on non-dirty writers.
            if (charPos==0 && ((!flushStream && !flushEncoder) || CompatibilitySwitches.IsAppEarlierThanWindowsPhone8))
                return;

            if (!haveWrittenPreamble) {
                haveWrittenPreamble = true;
                byte[] preamble = encoding.GetPreamble();
                if (preamble.Length > 0)
                    stream.Write(preamble, 0, preamble.Length);
            }

            int count = encoder.GetBytes(charBuffer, 0, charPos, byteBuffer, 0, flushEncoder);
            charPos = 0;
            if (count > 0)
                stream.Write(byteBuffer, 0, count);
            // By definition, calling Flush should flush the stream, but this is
            // only necessary if we passed in true for flushStream.  The Web
            // Services guys have some perf tests where flushing needlessly hurts.
            if (flushStream)
                stream.Flush();
        }
    
        public virtual bool AutoFlush {
            get { return autoFlush; }

            set
            {
                CheckAsyncTaskInProgress();

                autoFlush = value;
                if (value) Flush(true, false);
            }
        }
    
        public virtual Stream BaseStream {
            get { return stream; }
        }

        internal bool LeaveOpen {
            get { return !closable; }
        }

        internal bool HaveWrittenPreamble {
            set { haveWrittenPreamble= value; }
        }

        public override Encoding Encoding {
            get { return encoding; }
        }

        public override void Write(char value)
        {
            CheckAsyncTaskInProgress();

            if (charPos == charLen) Flush(false, false);
            charBuffer[charPos] = value;
            charPos++;
            if (autoFlush) Flush(true, false);
        }
    
        public override void Write(char[] buffer)
        {
            // This may be faster than the one with the index & count since it
            // has to do less argument checking.
            if (buffer==null)
                return;

            CheckAsyncTaskInProgress();

            int index = 0;
            int count = buffer.Length;
            while (count > 0) {
                if (charPos == charLen) Flush(false, false);
                int n = charLen - charPos;
                if (n > count) n = count;
                Contract.Assert(n > 0, "StreamWriter::Write(char[]) isn't making progress!  This is most likely a race condition in user code.");
                Buffer.InternalBlockCopy(buffer, index * sizeof(char), charBuffer, charPos * sizeof(char), n * sizeof(char));
                charPos += n;
                index += n;
                count -= n;
            }
            if (autoFlush) Flush(true, false);
        }

        public override void Write(char[] buffer, int index, int count) {
            if (buffer==null)
                throw new ArgumentNullException("buffer", Environment.GetResourceString("ArgumentNull_Buffer"));
            if (index < 0)
                throw new ArgumentOutOfRangeException("index", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            if (count < 0)
                throw new ArgumentOutOfRangeException("count", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            if (buffer.Length - index < count)
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidOffLen"));
            Contract.EndContractBlock();

            CheckAsyncTaskInProgress();

            while (count > 0) {
                if (charPos == charLen) Flush(false, false);
                int n = charLen - charPos;
                if (n > count) n = count;
                Contract.Assert(n > 0, "StreamWriter::Write(char[], int, int) isn't making progress!  This is most likely a race condition in user code.");
                Buffer.InternalBlockCopy(buffer, index * sizeof(char), charBuffer, charPos * sizeof(char), n * sizeof(char));
                charPos += n;
                index += n;
                count -= n;
            }
            if (autoFlush) Flush(true, false);
        }
    
        public override void Write(String value)
        {
            if (value != null)
            {

                CheckAsyncTaskInProgress();

                int count = value.Length;
                int index = 0;
                while (count > 0) {
                    if (charPos == charLen) Flush(false, false);
                    int n = charLen - charPos;
                    if (n > count) n = count;
                    Contract.Assert(n > 0, "StreamWriter::Write(String) isn't making progress!  This is most likely a race condition in user code.");
                    value.CopyTo(index, charBuffer, charPos, n);
                    charPos += n;
                    index += n;
                    count -= n;
                }
                if (autoFlush) Flush(true, false);
            }
        }

        #region Task based Async APIs
        [HostProtection(ExternalThreading = true)]
        [ComVisible(false)]
        public override Task WriteAsync(char value)
        {
            // If we have been inherited into a subclass, the following implementation could be incorrect
            // since it does not call through to Write() which a subclass might have overriden.  
            // To be safe we will only use this implementation in cases where we know it is safe to do so,
            // and delegate to our base class (which will call into Write) when we are not sure.
            if (this.GetType() != typeof(StreamWriter))
                return base.WriteAsync(value);

            if (stream == null)
                __Error.WriterClosed();

            CheckAsyncTaskInProgress();

            Task task = WriteAsyncInternal(this, value, charBuffer, charPos, charLen, CoreNewLine, autoFlush, appendNewLine: false);
            _asyncWriteTask = task;

            return task;
        }

        // We pass in private instance fields of this MarshalByRefObject-derived type as local params
        // to ensure performant access inside the state machine that corresponds this async method.
        // Fields that are written to must be assigned at the end of the method *and* before instance invocations.
        private static async Task WriteAsyncInternal(StreamWriter _this, Char value,
                                                     Char[] charBuffer, Int32 charPos, Int32 charLen, Char[] coreNewLine,
                                                     bool autoFlush, bool appendNewLine)
        {            
            if (charPos == charLen) {
                await _this.FlushAsyncInternal(false, false, charBuffer, charPos).ConfigureAwait(false);
                Contract.Assert(_this.charPos == 0);
                charPos = 0;
            }

            charBuffer[charPos] = value;
            charPos++;

            if (appendNewLine)
            {
                for (Int32 i = 0; i < coreNewLine.Length; i++)   // Expect 2 iterations, no point calling BlockCopy
                {
                    if (charPos == charLen) {                        
                        await _this.FlushAsyncInternal(false, false, charBuffer, charPos).ConfigureAwait(false);
                        Contract.Assert(_this.charPos == 0);
                        charPos = 0;
                    }

                    charBuffer[charPos] = coreNewLine[i];
                    charPos++;
                }
            }            

            if (autoFlush) {
                await _this.FlushAsyncInternal(true, false, charBuffer, charPos).ConfigureAwait(false);
                Contract.Assert(_this.charPos == 0);
                charPos = 0;
            }

            _this.CharPos_Prop = charPos;
        }

        [HostProtection(ExternalThreading = true)]
        [ComVisible(false)]
        public override Task WriteAsync(String value)
        {
            // If we have been inherited into a subclass, the following implementation could be incorrect
            // since it does not call through to Write() which a subclass might have overriden.  
            // To be safe we will only use this implementation in cases where we know it is safe to do so,
            // and delegate to our base class (which will call into Write) when we are not sure.
            if (this.GetType() != typeof(StreamWriter))
                return base.WriteAsync(value);

            if (value != null)
            {
                if (stream == null)
                    __Error.WriterClosed();

                CheckAsyncTaskInProgress();

                Task task = WriteAsyncInternal(this, value, charBuffer, charPos, charLen, CoreNewLine, autoFlush, appendNewLine: false);
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
        private static async Task WriteAsyncInternal(StreamWriter _this, String value,
                                                     Char[] charBuffer, Int32 charPos, Int32 charLen, Char[] coreNewLine,
                                                     bool autoFlush, bool appendNewLine)
        {
            Contract.Requires(value != null);

            int count = value.Length;
            int index = 0;

            while (count > 0)
            {
                if (charPos == charLen) {
                    await _this.FlushAsyncInternal(false, false, charBuffer, charPos).ConfigureAwait(false);
                    Contract.Assert(_this.charPos == 0);
                    charPos = 0;
                }

                int n = charLen - charPos;
                if (n > count)
                    n = count;

                Contract.Assert(n > 0, "StreamWriter::Write(String) isn't making progress!  This is most likely a race condition in user code.");

                value.CopyTo(index, charBuffer, charPos, n);

                charPos += n;
                index += n;
                count -= n;
            }

            if (appendNewLine)
            {
                for (Int32 i = 0; i < coreNewLine.Length; i++)   // Expect 2 iterations, no point calling BlockCopy
                {
                    if (charPos == charLen) {
                        await _this.FlushAsyncInternal(false, false, charBuffer, charPos).ConfigureAwait(false);
                        Contract.Assert(_this.charPos == 0);
                        charPos = 0;
                    }

                    charBuffer[charPos] = coreNewLine[i];
                    charPos++;
                }
            }

            if (autoFlush) {
                await _this.FlushAsyncInternal(true, false, charBuffer, charPos).ConfigureAwait(false);
                Contract.Assert(_this.charPos == 0);
                charPos = 0;
            }

            _this.CharPos_Prop = charPos;
        }

        [HostProtection(ExternalThreading = true)]
        [ComVisible(false)]
        public override Task WriteAsync(char[] buffer, int index, int count)
        {
            if (buffer==null)
                throw new ArgumentNullException("buffer", Environment.GetResourceString("ArgumentNull_Buffer"));
            if (index < 0)
                throw new ArgumentOutOfRangeException("index", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            if (count < 0)
                throw new ArgumentOutOfRangeException("count", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            if (buffer.Length - index < count)
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidOffLen"));
            Contract.EndContractBlock();

            // If we have been inherited into a subclass, the following implementation could be incorrect
            // since it does not call through to Write() which a subclass might have overriden.  
            // To be safe we will only use this implementation in cases where we know it is safe to do so,
            // and delegate to our base class (which will call into Write) when we are not sure.
            if (this.GetType() != typeof(StreamWriter))
                return base.WriteAsync(buffer, index, count);

            if (stream == null)
                __Error.WriterClosed();

            CheckAsyncTaskInProgress();

            Task task = WriteAsyncInternal(this, buffer, index, count, charBuffer, charPos, charLen, CoreNewLine, autoFlush, appendNewLine: false);
            _asyncWriteTask = task;

            return task;
        }

        // We pass in private instance fields of this MarshalByRefObject-derived type as local params
        // to ensure performant access inside the state machine that corresponds this async method.
        // Fields that are written to must be assigned at the end of the method *and* before instance invocations.
        private static async Task WriteAsyncInternal(StreamWriter _this, Char[] buffer, Int32 index, Int32 count,
                                                     Char[] charBuffer, Int32 charPos, Int32 charLen, Char[] coreNewLine,
                                                     bool autoFlush, bool appendNewLine)
        {
            Contract.Requires(count == 0 || (count > 0 && buffer != null));
            Contract.Requires(index >= 0);
            Contract.Requires(count >= 0);
            Contract.Requires(buffer == null || (buffer != null && buffer.Length - index >= count));

            while (count > 0)
            {
                if (charPos == charLen) {
                    await _this.FlushAsyncInternal(false, false, charBuffer, charPos).ConfigureAwait(false);
                    Contract.Assert(_this.charPos == 0);
                    charPos = 0;
                }

                int n = charLen - charPos;
                if (n > count) n = count;

                Contract.Assert(n > 0, "StreamWriter::Write(char[], int, int) isn't making progress!  This is most likely a race condition in user code.");

                Buffer.InternalBlockCopy(buffer, index * sizeof(char), charBuffer, charPos * sizeof(char), n * sizeof(char));

                charPos += n;
                index += n;
                count -= n;
            }

            if (appendNewLine)
            {
                for (Int32 i = 0; i < coreNewLine.Length; i++)   // Expect 2 iterations, no point calling BlockCopy
                {
                    if (charPos == charLen) {
                        await _this.FlushAsyncInternal(false, false, charBuffer, charPos).ConfigureAwait(false);
                        Contract.Assert(_this.charPos == 0);
                        charPos = 0;
                    }

                    charBuffer[charPos] = coreNewLine[i];
                    charPos++;
                }
            }

            if (autoFlush) {
                await _this.FlushAsyncInternal(true, false, charBuffer, charPos).ConfigureAwait(false);
                Contract.Assert(_this.charPos == 0);
                charPos = 0;
            }

            _this.CharPos_Prop = charPos;
        }

        [HostProtection(ExternalThreading = true)]
        [ComVisible(false)]
        public override Task WriteLineAsync()
        {
            // If we have been inherited into a subclass, the following implementation could be incorrect
            // since it does not call through to Write() which a subclass might have overriden.  
            // To be safe we will only use this implementation in cases where we know it is safe to do so,
            // and delegate to our base class (which will call into Write) when we are not sure.
            if (this.GetType() != typeof(StreamWriter))
                return base.WriteLineAsync();

            if (stream == null)
                __Error.WriterClosed();

            CheckAsyncTaskInProgress();

            Task task = WriteAsyncInternal(this, null, 0, 0, charBuffer, charPos, charLen, CoreNewLine, autoFlush, appendNewLine: true);
            _asyncWriteTask = task;

            return task;
        }
        

        [HostProtection(ExternalThreading = true)]
        [ComVisible(false)]
        public override Task WriteLineAsync(char value)
        {
            // If we have been inherited into a subclass, the following implementation could be incorrect
            // since it does not call through to Write() which a subclass might have overriden.  
            // To be safe we will only use this implementation in cases where we know it is safe to do so,
            // and delegate to our base class (which will call into Write) when we are not sure.
            if (this.GetType() != typeof(StreamWriter))
                return base.WriteLineAsync(value);

            if (stream == null)
                __Error.WriterClosed();

            CheckAsyncTaskInProgress();

            Task task = WriteAsyncInternal(this, value, charBuffer, charPos, charLen, CoreNewLine, autoFlush, appendNewLine: true);
            _asyncWriteTask = task;

            return task;
        }
        

        [HostProtection(ExternalThreading = true)]
        [ComVisible(false)]
        public override Task WriteLineAsync(String value)
        {
            // If we have been inherited into a subclass, the following implementation could be incorrect
            // since it does not call through to Write() which a subclass might have overriden.  
            // To be safe we will only use this implementation in cases where we know it is safe to do so,
            // and delegate to our base class (which will call into Write) when we are not sure.
            if (this.GetType() != typeof(StreamWriter))
                return base.WriteLineAsync(value);

            if (stream == null)
                __Error.WriterClosed();

            CheckAsyncTaskInProgress();

            Task task = WriteAsyncInternal(this, value, charBuffer, charPos, charLen, CoreNewLine, autoFlush, appendNewLine: true);
            _asyncWriteTask = task;

            return task;
        }


        [HostProtection(ExternalThreading = true)]
        [ComVisible(false)]
        public override Task WriteLineAsync(char[] buffer, int index, int count)
        {
            if (buffer==null)
                throw new ArgumentNullException("buffer", Environment.GetResourceString("ArgumentNull_Buffer"));
            if (index < 0)
                throw new ArgumentOutOfRangeException("index", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            if (count < 0)
                throw new ArgumentOutOfRangeException("count", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            if (buffer.Length - index < count)
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidOffLen"));
            Contract.EndContractBlock();

            // If we have been inherited into a subclass, the following implementation could be incorrect
            // since it does not call through to Write() which a subclass might have overriden.  
            // To be safe we will only use this implementation in cases where we know it is safe to do so,
            // and delegate to our base class (which will call into Write) when we are not sure.
            if (this.GetType() != typeof(StreamWriter))
                return base.WriteLineAsync(buffer, index, count);
    
            if (stream == null)
                __Error.WriterClosed();

            CheckAsyncTaskInProgress();

            Task task = WriteAsyncInternal(this, buffer, index, count, charBuffer, charPos, charLen, CoreNewLine, autoFlush, appendNewLine: true);
            _asyncWriteTask = task;

            return task;
        }
        

        [HostProtection(ExternalThreading = true)]
        [ComVisible(false)]
        public override Task FlushAsync()
        {
            // If we have been inherited into a subclass, the following implementation could be incorrect
            // since it does not call through to Flush() which a subclass might have overriden.  To be safe 
            // we will only use this implementation in cases where we know it is safe to do so,
            // and delegate to our base class (which will call into Flush) when we are not sure.
            if (this.GetType() != typeof(StreamWriter))
                return base.FlushAsync();

            // flushEncoder should be true at the end of the file and if
            // the user explicitly calls Flush (though not if AutoFlush is true).
            // This is required to flush any dangling characters from our UTF-7 
            // and UTF-8 encoders.  
            if (stream == null)
                __Error.WriterClosed();

            CheckAsyncTaskInProgress();

            Task task = FlushAsyncInternal(true, true, charBuffer, charPos);
            _asyncWriteTask = task;

            return task;
        }

        private Int32 CharPos_Prop {
            set { this.charPos = value; }
        }

        private bool HaveWrittenPreamble_Prop {
            set { this.haveWrittenPreamble = value; }
        }

        private Task FlushAsyncInternal(bool flushStream, bool flushEncoder,
                                        Char[] sCharBuffer, Int32 sCharPos) {
            
            // Perf boost for Flush on non-dirty writers.
            if (sCharPos == 0 && !flushStream && !flushEncoder)
                return Task.CompletedTask;

            Task flushTask = FlushAsyncInternal(this, flushStream, flushEncoder, sCharBuffer, sCharPos, this.haveWrittenPreamble,
                                                this.encoding, this.encoder, this.byteBuffer, this.stream);
                                      
            this.charPos = 0;
            return flushTask;
        }


        // We pass in private instance fields of this MarshalByRefObject-derived type as local params
        // to ensure performant access inside the state machine that corresponds this async method.
        private static async Task FlushAsyncInternal(StreamWriter _this, bool flushStream, bool flushEncoder,
                                                     Char[] charBuffer, Int32 charPos, bool haveWrittenPreamble,
                                                     Encoding encoding, Encoder encoder, Byte[] byteBuffer, Stream stream)
        {            
            if (!haveWrittenPreamble)
            {
                _this.HaveWrittenPreamble_Prop = true;
                byte[] preamble = encoding.GetPreamble();
                if (preamble.Length > 0)
                    await stream.WriteAsync(preamble, 0, preamble.Length).ConfigureAwait(false);
            }

            int count = encoder.GetBytes(charBuffer, 0, charPos, byteBuffer, 0, flushEncoder);            
            if (count > 0)
                await stream.WriteAsync(byteBuffer, 0, count).ConfigureAwait(false);

            // By definition, calling Flush should flush the stream, but this is
            // only necessary if we passed in true for flushStream.  The Web
            // Services guys have some perf tests where flushing needlessly hurts.
            if (flushStream)
                await stream.FlushAsync().ConfigureAwait(false);
        }
        #endregion

#if MDA_SUPPORTED
        // StreamWriterBufferedDataLost MDA
        // Instead of adding a finalizer to StreamWriter for detecting buffered data loss  
        // (ie, when the user forgets to call Close/Flush on the StreamWriter), we will 
        // have a separate object with normal finalization semantics that maintains a 
        // back pointer to this StreamWriter and alerts about any data loss
        private sealed class MdaHelper
        {
            private StreamWriter streamWriter;
            private String allocatedCallstack;    // captures the callstack when this streamwriter was allocated

            internal MdaHelper(StreamWriter sw, String cs)
            {
                streamWriter = sw;
                allocatedCallstack = cs;
            }

            // Finalizer
            ~MdaHelper()
            {
                // Make sure people closed this StreamWriter, exclude StreamWriter::Null.
                if (streamWriter.charPos != 0 && streamWriter.stream != null && streamWriter.stream != Stream.Null) {
                    String fileName = (streamWriter.stream is FileStream) ? ((FileStream)streamWriter.stream).NameInternal : "<unknown>";
                    String callStack = allocatedCallstack;

                    if (callStack == null)
                        callStack = Environment.GetResourceString("IO_StreamWriterBufferedDataLostCaptureAllocatedFromCallstackNotEnabled");

                    String message = Environment.GetResourceString("IO_StreamWriterBufferedDataLost", streamWriter.stream.GetType().FullName, fileName, callStack);

                    Mda.StreamWriterBufferedDataLost.ReportError(message);
                }
            }
        }  // class MdaHelper
#endif  // MDA_SUPPORTED

    }  // class StreamWriter
}  // namespace
