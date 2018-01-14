// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Win32.SafeHandles;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Security;
using System.Threading;
using System.Threading.Tasks;

namespace System.IO.Pipes
{
    public abstract partial class PipeStream : Stream
    {
        internal const string AnonymousPipeName = "anonymous";
        private static readonly Task<int> s_zeroTask = Task.FromResult(0);

        private SafePipeHandle _handle;
        private bool _canRead;
        private bool _canWrite;
        private bool _isAsync;
        private bool _isMessageComplete;
        private bool _isFromExistingHandle;
        private bool _isHandleExposed;
        private PipeTransmissionMode _readMode;
        private PipeTransmissionMode _transmissionMode;
        private PipeDirection _pipeDirection;
        private int _outBufferSize;
        private PipeState _state;

        protected PipeStream(PipeDirection direction, int bufferSize)
        {
            if (direction < PipeDirection.In || direction > PipeDirection.InOut)
            {
                throw new ArgumentOutOfRangeException(nameof(direction), SR.ArgumentOutOfRange_DirectionModeInOutOrInOut);
            }
            if (bufferSize < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(bufferSize), SR.ArgumentOutOfRange_NeedNonNegNum);
            }

            Init(direction, PipeTransmissionMode.Byte, bufferSize);
        }

        protected PipeStream(PipeDirection direction, PipeTransmissionMode transmissionMode, int outBufferSize)
        {
            if (direction < PipeDirection.In || direction > PipeDirection.InOut)
            {
                throw new ArgumentOutOfRangeException(nameof(direction), SR.ArgumentOutOfRange_DirectionModeInOutOrInOut);
            }
            if (transmissionMode < PipeTransmissionMode.Byte || transmissionMode > PipeTransmissionMode.Message)
            {
                throw new ArgumentOutOfRangeException(nameof(transmissionMode), SR.ArgumentOutOfRange_TransmissionModeByteOrMsg);
            }
            if (outBufferSize < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(outBufferSize), SR.ArgumentOutOfRange_NeedNonNegNum);
            }

            Init(direction, transmissionMode, outBufferSize);
        }

        private void Init(PipeDirection direction, PipeTransmissionMode transmissionMode, int outBufferSize)
        {
            Debug.Assert(direction >= PipeDirection.In && direction <= PipeDirection.InOut, "invalid pipe direction");
            Debug.Assert(transmissionMode >= PipeTransmissionMode.Byte && transmissionMode <= PipeTransmissionMode.Message, "transmissionMode is out of range");
            Debug.Assert(outBufferSize >= 0, "outBufferSize is negative");

            // always defaults to this until overridden
            _readMode = transmissionMode;
            _transmissionMode = transmissionMode;

            _pipeDirection = direction;

            if ((_pipeDirection & PipeDirection.In) != 0)
            {
                _canRead = true;
            }
            if ((_pipeDirection & PipeDirection.Out) != 0)
            {
                _canWrite = true;
            }

            _outBufferSize = outBufferSize;

            // This should always default to true
            _isMessageComplete = true;

            _state = PipeState.WaitingToConnect;
        }

        // Once a PipeStream has a handle ready, it should call this method to set up the PipeStream.  If
        // the pipe is in a connected state already, it should also set the IsConnected (protected) property.
        // This method may also be called to uninitialize a handle, setting it to null.
        protected void InitializeHandle(SafePipeHandle handle, bool isExposed, bool isAsync)
        {
            if (isAsync && handle != null)
            {
                InitializeAsyncHandle(handle);
            }

            _handle = handle;
            _isAsync = isAsync;

            // track these separately; _isHandleExposed will get updated if accessed though the property
            _isHandleExposed = isExposed;
            _isFromExistingHandle = isExposed;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (_isAsync)
            {
                return ReadAsync(buffer, offset, count, CancellationToken.None).GetAwaiter().GetResult();
            }

            CheckReadWriteArgs(buffer, offset, count);
            if (!CanRead)
            {
                throw Error.GetReadNotSupported();
            }
            CheckReadOperations();

            return ReadCore(new Span<byte>(buffer, offset, count));
        }

        public override int Read(Span<byte> destination)
        {
            if (_isAsync)
            {
                return base.Read(destination);
            }

            if (!CanRead)
            {
                throw Error.GetReadNotSupported();
            }
            CheckReadOperations();

            return ReadCore(destination);
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            CheckReadWriteArgs(buffer, offset, count);
            if (!CanRead)
            {
                throw Error.GetReadNotSupported();
            }

            if (cancellationToken.IsCancellationRequested)
            {
                return Task.FromCanceled<int>(cancellationToken);
            }

            CheckReadOperations();

            if (!_isAsync)
            {
                return base.ReadAsync(buffer, offset, count, cancellationToken);
            }

            if (count == 0)
            {
                UpdateMessageCompletion(false);
                return s_zeroTask;
            }

            return ReadAsyncCore(new Memory<byte>(buffer, offset, count), cancellationToken);
        }

        public override ValueTask<int> ReadAsync(Memory<byte> destination, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (!_isAsync)
            {
                return base.ReadAsync(destination, cancellationToken);
            }

            if (!CanRead)
            {
                throw Error.GetReadNotSupported();
            }

            if (cancellationToken.IsCancellationRequested)
            {
                return new ValueTask<int>(Task.FromCanceled<int>(cancellationToken));
            }

            CheckReadOperations();

            if (destination.Length == 0)
            {
                UpdateMessageCompletion(false);
                return new ValueTask<int>(0);
            }

            return new ValueTask<int>(ReadAsyncCore(destination, cancellationToken));
        }

        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            if (_isAsync)
                return TaskToApm.Begin(ReadAsync(buffer, offset, count, CancellationToken.None), callback, state);
            else
                return base.BeginRead(buffer, offset, count, callback, state);
        }

        public override int EndRead(IAsyncResult asyncResult)
        {
            if (_isAsync)
                return TaskToApm.End<int>(asyncResult);
            else
                return base.EndRead(asyncResult);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (_isAsync)
            {
                WriteAsync(buffer, offset, count, CancellationToken.None).GetAwaiter().GetResult();
                return;
            }

            CheckReadWriteArgs(buffer, offset, count);
            if (!CanWrite)
            {
                throw Error.GetWriteNotSupported();
            }
            CheckWriteOperations();

            WriteCore(new ReadOnlySpan<byte>(buffer, offset, count));
        }

        public override void Write(ReadOnlySpan<byte> source)
        {
            if (_isAsync)
            {
                base.Write(source);
                return;
            }

            if (!CanWrite)
            {
                throw Error.GetWriteNotSupported();
            }
            CheckWriteOperations();

            WriteCore(source);
        }

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            CheckReadWriteArgs(buffer, offset, count);
            if (!CanWrite)
            {
                throw Error.GetWriteNotSupported();
            }

            if (cancellationToken.IsCancellationRequested)
            {
                return Task.FromCanceled<int>(cancellationToken);
            }

            CheckWriteOperations();

            if (!_isAsync)
            {
                return base.WriteAsync(buffer, offset, count, cancellationToken);
            }

            if (count == 0)
            {
                return Task.CompletedTask;
            }

            return WriteAsyncCore(new ReadOnlyMemory<byte>(buffer, offset, count), cancellationToken);
        }

        public override Task WriteAsync(ReadOnlyMemory<byte> source, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (!_isAsync)
            {
                return base.WriteAsync(source, cancellationToken);
            }

            if (!CanWrite)
            {
                throw Error.GetWriteNotSupported();
            }

            if (cancellationToken.IsCancellationRequested)
            {
                return Task.FromCanceled<int>(cancellationToken);
            }

            CheckWriteOperations();

            if (source.Length == 0)
            {
                return Task.CompletedTask;
            }

            return WriteAsyncCore(source, cancellationToken);
        }

        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            if (_isAsync)
                return TaskToApm.Begin(WriteAsync(buffer, offset, count, CancellationToken.None), callback, state);
            else
                return base.BeginWrite(buffer, offset, count, callback, state);
        }

        public override void EndWrite(IAsyncResult asyncResult)
        {
            if (_isAsync)
                TaskToApm.End(asyncResult);
            else
                base.EndWrite(asyncResult);
        }

        private void CheckReadWriteArgs(byte[] buffer, int offset, int count)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer), SR.ArgumentNull_Buffer);
            if (offset < 0)
                throw new ArgumentOutOfRangeException(nameof(offset), SR.ArgumentOutOfRange_NeedNonNegNum);
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count), SR.ArgumentOutOfRange_NeedNonNegNum);
            if (buffer.Length - offset < count)
                throw new ArgumentException(SR.Argument_InvalidOffLen);
        }

        [Conditional("DEBUG")]
        private static void DebugAssertHandleValid(SafePipeHandle handle)
        {
            Debug.Assert(handle != null, "handle is null");
            Debug.Assert(!handle.IsClosed, "handle is closed");
        }

        [Conditional("DEBUG")]
        private static void DebugAssertReadWriteArgs(byte[] buffer, int offset, int count, SafePipeHandle handle)
        {
            Debug.Assert(buffer != null, "buffer is null");
            Debug.Assert(offset >= 0, "offset is negative");
            Debug.Assert(count >= 0, "count is negative");
            Debug.Assert(offset <= buffer.Length - count, "offset + count is too big");
            DebugAssertHandleValid(handle);
        }

        // Reads a byte from the pipe stream.  Returns the byte cast to an int
        // or -1 if the connection has been broken.
        public override unsafe int ReadByte()
        {
            byte b;
            return Read(new Span<byte>(&b, 1)) > 0 ? b : -1;
        }

        public override unsafe void WriteByte(byte value)
        {
            Write(new ReadOnlySpan<byte>(&value, 1));
        }

        // Does nothing on PipeStreams.  We cannot call Interop.FlushFileBuffers here because we can deadlock
        // if the other end of the pipe is no longer interested in reading from the pipe. 
        public override void Flush()
        {
            CheckWriteOperations();
            if (!CanWrite)
            {
                throw Error.GetWriteNotSupported();
            }
        }

        protected override void Dispose(bool disposing)
        {
            try
            {
                // Nothing will be done differently based on whether we are 
                // disposing vs. finalizing.  
                if (_handle != null && !_handle.IsClosed)
                {
                    _handle.Dispose();
                }

                DisposeCore(disposing);
            }
            finally
            {
                base.Dispose(disposing);
            }

            _state = PipeState.Closed;
        }

        // ********************** Public Properties *********************** //

        // APIs use coarser definition of connected, but these map to internal 
        // Connected/Disconnected states. Note that setter is protected; only
        // intended to be called by custom PipeStream concrete children
        public bool IsConnected
        {
            get
            {
                return State == PipeState.Connected;
            }
            protected set
            {
                _state = (value) ? PipeState.Connected : PipeState.Disconnected;
            }
        }

        public bool IsAsync
        {
            get { return _isAsync; }
        }

        // Set by the most recent call to Read or EndRead.  Will be false if there are more buffer in the
        // message, otherwise it is set to true. 
        public bool IsMessageComplete
        {
            [SuppressMessage("Microsoft.Security", "CA2122:DoNotIndirectlyExposeMethodsWithLinkDemands", Justification = "Security model of pipes: demand at creation but no subsequent demands")]
            get
            {
                // omitting pipe broken exception to allow reader to finish getting message
                if (_state == PipeState.WaitingToConnect)
                {
                    throw new InvalidOperationException(SR.InvalidOperation_PipeNotYetConnected);
                }
                if (_state == PipeState.Disconnected)
                {
                    throw new InvalidOperationException(SR.InvalidOperation_PipeDisconnected);
                }
                if (CheckOperationsRequiresSetHandle && _handle == null)
                {
                    throw new InvalidOperationException(SR.InvalidOperation_PipeHandleNotSet);
                }

                if ((_state == PipeState.Closed) || (_handle != null && _handle.IsClosed))
                {
                    throw Error.GetPipeNotOpen();
                }
                // don't need to check transmission mode; just care about read mode. Always use
                // cached mode; otherwise could throw for valid message when other side is shutting down
                if (_readMode != PipeTransmissionMode.Message)
                {
                    throw new InvalidOperationException(SR.InvalidOperation_PipeReadModeNotMessage);
                }

                return _isMessageComplete;
            }
        }

        internal void UpdateMessageCompletion(bool completion)
        {
            // Set message complete to true because the pipe is broken as well.
            // Need this to signal to readers to stop reading.
            _isMessageComplete = (completion || _state == PipeState.Broken);
        }

        public SafePipeHandle SafePipeHandle
        {
            [SuppressMessage("Microsoft.Security", "CA2122:DoNotIndirectlyExposeMethodsWithLinkDemands", Justification = "Security model of pipes: demand at creation but no subsequent demands")]
            get
            {
                if (_handle == null)
                {
                    throw new InvalidOperationException(SR.InvalidOperation_PipeHandleNotSet);
                }
                if (_handle.IsClosed)
                {
                    throw Error.GetPipeNotOpen();
                }

                _isHandleExposed = true;
                return _handle;
            }
        }

        internal SafePipeHandle InternalHandle
        {
            get
            {
                return _handle;
            }
        }

        protected bool IsHandleExposed
        {
            get
            {
                return _isHandleExposed;
            }
        }

        public override bool CanRead
        {
            get
            {
                return _canRead;
            }
        }

        public override bool CanWrite
        {
            get
            {
                return _canWrite;
            }
        }

        public override bool CanSeek
        {
            get
            {
                return false;
            }
        }

        public override long Length
        {
            get
            {
                throw Error.GetSeekNotSupported();
            }
        }

        public override long Position
        {
            get
            {
                throw Error.GetSeekNotSupported();
            }
            set
            {
                throw Error.GetSeekNotSupported();
            }
        }

        public override void SetLength(long value)
        {
            throw Error.GetSeekNotSupported();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw Error.GetSeekNotSupported();
        }

        // anonymous pipe ends and named pipe server can get/set properties when broken 
        // or connected. Named client overrides
        protected internal virtual void CheckPipePropertyOperations()
        {
            if (CheckOperationsRequiresSetHandle && _handle == null)
            {
                throw new InvalidOperationException(SR.InvalidOperation_PipeHandleNotSet);
            }

            // these throw object disposed
            if ((_state == PipeState.Closed) || (_handle != null && _handle.IsClosed))
            {
                throw Error.GetPipeNotOpen();
            }
        }

        // Reads can be done in Connected and Broken. In the latter,
        // read returns 0 bytes
        protected internal void CheckReadOperations()
        {
            // Invalid operation
            if (_state == PipeState.WaitingToConnect)
            {
                throw new InvalidOperationException(SR.InvalidOperation_PipeNotYetConnected);
            }
            if (_state == PipeState.Disconnected)
            {
                throw new InvalidOperationException(SR.InvalidOperation_PipeDisconnected);
            }
            if (CheckOperationsRequiresSetHandle && _handle == null)
            {
                throw new InvalidOperationException(SR.InvalidOperation_PipeHandleNotSet);
            }

            // these throw object disposed
            if ((_state == PipeState.Closed) || (_handle != null && _handle.IsClosed))
            {
                throw Error.GetPipeNotOpen();
            }
        }

        // Writes can only be done in connected state
        protected internal void CheckWriteOperations()
        {
            // Invalid operation
            if (_state == PipeState.WaitingToConnect)
            {
                throw new InvalidOperationException(SR.InvalidOperation_PipeNotYetConnected);
            }
            if (_state == PipeState.Disconnected)
            {
                throw new InvalidOperationException(SR.InvalidOperation_PipeDisconnected);
            }
            if (CheckOperationsRequiresSetHandle && _handle == null)
            {
                throw new InvalidOperationException(SR.InvalidOperation_PipeHandleNotSet);
            }

            // IOException
            if (_state == PipeState.Broken)
            {
                throw new IOException(SR.IO_PipeBroken);
            }

            // these throw object disposed
            if ((_state == PipeState.Closed) || (_handle != null && _handle.IsClosed))
            {
                throw Error.GetPipeNotOpen();
            }
        }

        internal PipeState State
        {
            get
            {
                return _state;
            }
            set
            {
                _state = value;
            }
        }
    }
}
