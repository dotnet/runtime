// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Threading;
using Windows.Foundation;
using Windows.Storage.Streams;

namespace System.IO
{
    /// <summary>
    /// A <code>Stream</code> used to wrap a Windows Runtime stream to expose it as a managed steam.
    /// </summary>
    internal class WinRtToNetFxStreamAdapter : Stream, IDisposable
    {
        #region Construction

        internal static WinRtToNetFxStreamAdapter Create(Object windowsRuntimeStream)
        {
            if (windowsRuntimeStream == null)
                throw new ArgumentNullException(nameof(windowsRuntimeStream));

            bool canRead = windowsRuntimeStream is IInputStream;
            bool canWrite = windowsRuntimeStream is IOutputStream;
            bool canSeek = windowsRuntimeStream is IRandomAccessStream;

            if (!canRead && !canWrite && !canSeek)
                throw new ArgumentException(SR.Argument_ObjectMustBeWinRtStreamToConvertToNetFxStream);

            // Proactively guard against a non-conforming curstomer implementations:
            if (canSeek)
            {
                IRandomAccessStream iras = (IRandomAccessStream)windowsRuntimeStream;

                if (!canRead && iras.CanRead)
                    throw new ArgumentException(SR.Argument_InstancesImplementingIRASThatCanReadMustImplementIIS);

                if (!canWrite && iras.CanWrite)
                    throw new ArgumentException(SR.Argument_InstancesImplementingIRASThatCanWriteMustImplementIOS);

                if (!iras.CanRead)
                    canRead = false;

                if (!iras.CanWrite)
                    canWrite = false;
            }

            if (!canRead && !canWrite)
                throw new ArgumentException(SR.Argument_WinRtStreamCannotReadOrWrite);

            return new WinRtToNetFxStreamAdapter(windowsRuntimeStream, canRead, canWrite, canSeek);
        }


        private WinRtToNetFxStreamAdapter(Object winRtStream, bool canRead, bool canWrite, bool canSeek)
        {
            Debug.Assert(winRtStream != null);
            Debug.Assert(winRtStream is IInputStream || winRtStream is IOutputStream || winRtStream is IRandomAccessStream);

            Debug.Assert((canSeek && (winRtStream is IRandomAccessStream)) || (!canSeek && !(winRtStream is IRandomAccessStream)));

            Debug.Assert((canRead && (winRtStream is IInputStream))
                                 ||
                               (!canRead && (
                                    !(winRtStream is IInputStream)
                                        ||
                                    (winRtStream is IRandomAccessStream && !((IRandomAccessStream)winRtStream).CanRead)
                               ))
                             );

            Debug.Assert((canWrite && (winRtStream is IOutputStream))
                                 ||
                               (!canWrite && (
                                    !(winRtStream is IOutputStream)
                                        ||
                                    (winRtStream is IRandomAccessStream && !((IRandomAccessStream)winRtStream).CanWrite)
                               ))
                             );
            Contract.EndContractBlock();

            _winRtStream = winRtStream;
            _canRead = canRead;
            _canWrite = canWrite;
            _canSeek = canSeek;
        }

        #endregion Construction


        #region Instance variables

        private Byte[] _oneByteBuffer = null;
        private bool _leaveUnderlyingStreamOpen = true;

        private Object _winRtStream;
        private readonly bool _canRead;
        private readonly bool _canWrite;
        private readonly bool _canSeek;

        #endregion Instance variables


        #region Tools and Helpers

        /// <summary>
        /// We keep tables for mappings between managed and WinRT streams to make sure to always return the same adapter for a given underlying stream.
        /// However, in order to avoid global locks on those tables, several instances of this type may be created and then can race to be entered
        /// into the appropriate map table. All except for the winning instances will be thrown away. However, we must ensure that when the losers  are
        /// finalized, the do not dispose the underlying stream. To ensure that, we must call this method on the winner to notify it that it is safe to
        /// dispose the underlying stream.
        /// </summary>
        internal void SetWonInitializationRace()
        {
            _leaveUnderlyingStreamOpen = false;
        }


        public TWinRtStream GetWindowsRuntimeStream<TWinRtStream>() where TWinRtStream : class
        {
            Object wrtStr = _winRtStream;

            if (wrtStr == null)
                return null;

            Debug.Assert(wrtStr is TWinRtStream,
                            String.Format("Attempted to get the underlying WinRT stream typed as \"{0}\"," +
                                          " but the underlying WinRT stream cannot be cast to that type. Its actual type is \"{1}\".",
                                          typeof(TWinRtStream).ToString(), wrtStr.GetType().ToString()));

            return wrtStr as TWinRtStream;
        }


        private Byte[] OneByteBuffer
        {
            get
            {
                Byte[] obb = _oneByteBuffer;
                if (obb == null)  // benign race for multiple init
                    _oneByteBuffer = obb = new Byte[1];
                return obb;
            }
        }


#if DEBUG
        private static void AssertValidStream(Object winRtStream)
        {
            Debug.Assert(winRtStream != null,
                            "This to-NetFx Stream adapter must not be disposed and the underlying WinRT stream must be of compatible type for this operation");
        }
#endif  // DEBUG


        private TWinRtStream EnsureNotDisposed<TWinRtStream>() where TWinRtStream : class
        {
            Object wrtStr = _winRtStream;

            if (wrtStr == null)
                throw new ObjectDisposedException(SR.ObjectDisposed_CannotPerformOperation);

            return (wrtStr as TWinRtStream);
        }


        private void EnsureNotDisposed()
        {
            if (_winRtStream == null)
                throw new ObjectDisposedException(SR.ObjectDisposed_CannotPerformOperation);
        }


        private void EnsureCanRead()
        {
            if (!_canRead)
                throw new NotSupportedException(SR.NotSupported_CannotReadFromStream);
        }


        private void EnsureCanWrite()
        {
            if (!_canWrite)
                throw new NotSupportedException(SR.NotSupported_CannotWriteToStream);
        }

        #endregion Tools and Helpers


        #region Simple overrides

        protected override void Dispose(bool disposing)
        {
            // WinRT streams should implement IDisposable (IClosable in WinRT), but let's be defensive:
            if (disposing && _winRtStream != null && !_leaveUnderlyingStreamOpen)
            {
                IDisposable disposableWinRtStream = _winRtStream as IDisposable;  // benign race on winRtStream
                if (disposableWinRtStream != null)
                    disposableWinRtStream.Dispose();
            }

            _winRtStream = null;
            base.Dispose(disposing);
        }


        public override bool CanRead
        {
            [Pure]
            get
            { return (_canRead && _winRtStream != null); }
        }


        public override bool CanWrite
        {
            [Pure]
            get
            { return (_canWrite && _winRtStream != null); }
        }


        public override bool CanSeek
        {
            [Pure]
            get
            { return (_canSeek && _winRtStream != null); }
        }

        #endregion Simple overrides


        #region Length and Position functions

        public override Int64 Length
        {
            get
            {
                IRandomAccessStream wrtStr = EnsureNotDisposed<IRandomAccessStream>();

                if (!_canSeek)
                    throw new NotSupportedException(SR.NotSupported_CannotUseLength_StreamNotSeekable);

#if DEBUG
                AssertValidStream(wrtStr);
#endif  // DEBUG

                UInt64 size = wrtStr.Size;

                // These are over 8000 PetaBytes, we do not expect this to happen. However, let's be defensive:
                if (size > (UInt64)Int64.MaxValue)
                    throw new IOException(SR.IO_UnderlyingWinRTStreamTooLong_CannotUseLengthOrPosition);

                return unchecked((Int64)size);
            }
        }


        public override Int64 Position
        {
            get
            {
                IRandomAccessStream wrtStr = EnsureNotDisposed<IRandomAccessStream>();

                if (!_canSeek)
                    throw new NotSupportedException(SR.NotSupported_CannotUsePosition_StreamNotSeekable);

#if DEBUG
                AssertValidStream(wrtStr);
#endif  // DEBUG

                UInt64 pos = wrtStr.Position;

                // These are over 8000 PetaBytes, we do not expect this to happen. However, let's be defensive:
                if (pos > (UInt64)Int64.MaxValue)
                    throw new IOException(SR.IO_UnderlyingWinRTStreamTooLong_CannotUseLengthOrPosition);

                return unchecked((Int64)pos);
            }

            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException("Position", SR.ArgumentOutOfRange_IO_CannotSeekToNegativePosition);
                Contract.EndContractBlock();

                IRandomAccessStream wrtStr = EnsureNotDisposed<IRandomAccessStream>();

                if (!_canSeek)
                    throw new NotSupportedException(SR.NotSupported_CannotUsePosition_StreamNotSeekable);

#if DEBUG
                AssertValidStream(wrtStr);
#endif  // DEBUG

                wrtStr.Seek(unchecked((UInt64)value));
            }
        }


        public override Int64 Seek(Int64 offset, SeekOrigin origin)
        {
            IRandomAccessStream wrtStr = EnsureNotDisposed<IRandomAccessStream>();

            if (!_canSeek)
                throw new NotSupportedException(SR.NotSupported_CannotSeekInStream);

#if DEBUG
            AssertValidStream(wrtStr);
#endif  // DEBUG

            switch (origin)
            {
                case SeekOrigin.Begin:
                    {
                        Position = offset;
                        return offset;
                    }

                case SeekOrigin.Current:
                    {
                        Int64 curPos = Position;

                        if (Int64.MaxValue - curPos < offset)
                            throw new IOException(SR.IO_CannotSeekBeyondInt64MaxValue);

                        Int64 newPos = curPos + offset;

                        if (newPos < 0)
                            throw new IOException(SR.ArgumentOutOfRange_IO_CannotSeekToNegativePosition);

                        Position = newPos;
                        return newPos;
                    }

                case SeekOrigin.End:
                    {
                        UInt64 size = wrtStr.Size;
                        Int64 newPos;

                        if (size > (UInt64)Int64.MaxValue)
                        {
                            if (offset >= 0)
                                throw new IOException(SR.IO_CannotSeekBeyondInt64MaxValue);

                            Debug.Assert(offset < 0);

                            UInt64 absOffset = (offset == Int64.MinValue) ? ((UInt64)Int64.MaxValue) + 1 : (UInt64)(-offset);
                            Debug.Assert(absOffset <= size);

                            UInt64 np = size - absOffset;
                            if (np > (UInt64)Int64.MaxValue)
                                throw new IOException(SR.IO_CannotSeekBeyondInt64MaxValue);

                            newPos = (Int64)np;
                        }
                        else
                        {
                            Debug.Assert(size <= (UInt64)Int64.MaxValue);

                            Int64 s = unchecked((Int64)size);

                            if (Int64.MaxValue - s < offset)
                                throw new IOException(SR.IO_CannotSeekBeyondInt64MaxValue);

                            newPos = s + offset;

                            if (newPos < 0)
                                throw new IOException(SR.ArgumentOutOfRange_IO_CannotSeekToNegativePosition);
                        }

                        Position = newPos;
                        return newPos;
                    }

                default:
                    {
                        throw new ArgumentException(SR.Argument_InvalidSeekOrigin, nameof(origin));
                    }
            }
        }


        public override void SetLength(Int64 value)
        {
            if (value < 0)
                throw new ArgumentOutOfRangeException(nameof(value), SR.ArgumentOutOfRange_CannotResizeStreamToNegative);
            Contract.EndContractBlock();

            IRandomAccessStream wrtStr = EnsureNotDisposed<IRandomAccessStream>();

            if (!_canSeek)
                throw new NotSupportedException(SR.NotSupported_CannotSeekInStream);

            EnsureCanWrite();

#if DEBUG
            AssertValidStream(wrtStr);
#endif  // DEBUG

            wrtStr.Size = unchecked((UInt64)value);

            // If the length is set to a value < that the current position, then we need to set the position to that value
            // Because we can't directly set the position, we are going to seek to it.
            if (wrtStr.Size < wrtStr.Position)
                wrtStr.Seek(unchecked((UInt64)value));
        }

        #endregion Length and Position functions


        #region Reading

        private IAsyncResult BeginRead(Byte[] buffer, Int32 offset, Int32 count, AsyncCallback callback, Object state, bool usedByBlockingWrapper)
        {
            // This method is somewhat tricky: We could consider just calling ReadAsync (recall that Task implements IAsyncResult).
            // It would be OK for cases where BeginRead is invoked directly by the public user.
            // However, in cases where it is invoked by Read to achieve a blocking (synchronous) IO operation, the ReadAsync-approach may deadlock:
            //
            // The sync-over-async IO operation will be doing a blocking wait on the completion of the async IO operation assuming that
            // a wait handle would be signalled by the completion handler. Recall that the IAsyncInfo representing the IO operation may
            // not be free-threaded and not "free-marshalled"; it may also belong to an ASTA compartment because the underlying WinRT
            // stream lives in an ASTA compartment. The completion handler is invoked on a pool thread, i.e. in MTA.
            // That handler needs to fetch the results from the async IO operation, which requires a cross-compartment call from MTA into ASTA.
            // But because the ASTA thread is busy waiting this call will deadlock.
            // (Recall that although WaitOne pumps COM, ASTA specifically schedules calls on the outermost ?idle? pump only.)
            //
            // The solution is to make sure that:
            //  - In cases where main thread is waiting for the async IO to complete:
            //    Fetch results on the main thread after it has been signalled by the completion callback.
            //  - In cases where main thread is not waiting for the async IO to complete:
            //    Fetch results in the completion callback.
            //
            // But the Task-plumbing around IAsyncInfo.AsTask *always* fetches results in the completion handler because it has
            // no way of knowing whether or not someone is waiting. So, instead of using ReadAsync here we implement our own IAsyncResult
            // and our own completion handler which can behave differently according to whether it is being used by a blocking IO
            // operation wrapping a BeginRead/EndRead pair, or by an actual async operation based on the old Begin/End pattern.

            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));

            if (offset < 0)
                throw new ArgumentOutOfRangeException(nameof(offset));

            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count));

            if (buffer.Length - offset < count)
                throw new ArgumentException(SR.Argument_InsufficientSpaceInTargetBuffer);

            Contract.EndContractBlock();

            IInputStream wrtStr = EnsureNotDisposed<IInputStream>();
            EnsureCanRead();

#if DEBUG
            AssertValidStream(wrtStr);
#endif  // DEBUG

            IBuffer userBuffer = buffer.AsBuffer(offset, count);
            IAsyncOperationWithProgress<IBuffer, UInt32> asyncReadOperation = wrtStr.ReadAsync(userBuffer,
                                                                                               unchecked((UInt32)count),
                                                                                               InputStreamOptions.Partial);

            StreamReadAsyncResult asyncResult = new StreamReadAsyncResult(asyncReadOperation, userBuffer, callback, state,
                                                                          processCompletedOperationInCallback: !usedByBlockingWrapper);

            // The StreamReadAsyncResult will set a private instance method to act as a Completed handler for asyncOperation.
            // This will cause a CCW to be created for the delegate and the delegate has a reference to its target, i.e. to
            // asyncResult, so asyncResult will not be collected. If we loose the entire AppDomain, then asyncResult and its CCW
            // will be collected but the stub will remain and the callback will fail gracefully. The underlying buffer is the only
            // item to which we expose a direct pointer and this is properly pinned using a mechanism similar to Overlapped.

            return asyncResult;
        }

        public override Int32 EndRead(IAsyncResult asyncResult)
        {
            if (asyncResult == null)
                throw new ArgumentNullException(nameof(asyncResult));

            Contract.EndContractBlock();

            EnsureNotDisposed();
            EnsureCanRead();

            StreamOperationAsyncResult streamAsyncResult = asyncResult as StreamOperationAsyncResult;
            if (streamAsyncResult == null)
                throw new ArgumentException(SR.Argument_UnexpectedAsyncResult, nameof(asyncResult));

            streamAsyncResult.Wait();

            try
            {
                // If the async result did NOT process the async IO operation in its completion handler (i.e. check for errors,
                // cache results etc), then we need to do that processing now. This is to allow blocking-over-async IO operations.
                // See the big comment in BeginRead for details.

                if (!streamAsyncResult.ProcessCompletedOperationInCallback)
                    streamAsyncResult.ProcessCompletedOperation();

                // Rethrow errors caught in the completion callback, if any:
                if (streamAsyncResult.HasError)
                {
                    streamAsyncResult.CloseStreamOperation();
                    streamAsyncResult.ThrowCachedError();
                }

                // Done:

                Int64 bytesCompleted = streamAsyncResult.BytesCompleted;
                Debug.Assert(bytesCompleted <= unchecked((Int64)Int32.MaxValue));

                return (Int32)bytesCompleted;
            }
            finally
            {
                // Closing multiple times is Ok.
                streamAsyncResult.CloseStreamOperation();
            }
        }

        public override Task<Int32> ReadAsync(Byte[] buffer, Int32 offset, Int32 count, CancellationToken cancellationToken)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));

            if (offset < 0)
                throw new ArgumentOutOfRangeException(nameof(offset));

            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count));

            if (buffer.Length - offset < count)
                throw new ArgumentException(SR.Argument_InsufficientSpaceInTargetBuffer);

            Contract.EndContractBlock();

            EnsureNotDisposed();
            EnsureCanRead();

            // If already cancelled, bail early:
            cancellationToken.ThrowIfCancellationRequested();

            // State is Ok. Do the actual read:
            return ReadAsyncInternal(buffer, offset, count, cancellationToken);
        }


        public override Int32 Read(Byte[] buffer, Int32 offset, Int32 count)
        {
            // Arguments validation and not-disposed validation are done in BeginRead.

            IAsyncResult asyncResult = BeginRead(buffer, offset, count, null, null, usedByBlockingWrapper: true);
            Int32 bytesRead = EndRead(asyncResult);
            return bytesRead;
        }


        public override Int32 ReadByte()
        {
            Contract.Ensures(Contract.Result<int>() >= -1);
            Contract.Ensures(Contract.Result<int>() < 256);
            Contract.EndContractBlock();

            // EnsureNotDisposed will be called in Read->BeginRead.

            Byte[] oneByteArray = OneByteBuffer;

            if (0 == Read(oneByteArray, 0, 1))
                return -1;

            Int32 value = oneByteArray[0];
            return value;
        }

        #endregion Reading


        #region Writing


        public override IAsyncResult BeginWrite(Byte[] buffer, Int32 offset, Int32 count, AsyncCallback callback, Object state)
        {
            return BeginWrite(buffer, offset, count, callback, state, usedByBlockingWrapper: false);
        }

        private IAsyncResult BeginWrite(Byte[] buffer, Int32 offset, Int32 count, AsyncCallback callback, Object state, bool usedByBlockingWrapper)
        {
            // See the large comment in BeginRead about why we are not using this.WriteAsync,
            // and instead using a custom implementation of IAsyncResult.

            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));

            if (offset < 0)
                throw new ArgumentOutOfRangeException(nameof(offset));

            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count));

            if (buffer.Length - offset < count)
                throw new ArgumentException(SR.Argument_InsufficientArrayElementsAfterOffset);

            Contract.EndContractBlock();

            IOutputStream wrtStr = EnsureNotDisposed<IOutputStream>();
            EnsureCanWrite();

#if DEBUG
            AssertValidStream(wrtStr);
#endif  // DEBUG

            IBuffer asyncWriteBuffer = buffer.AsBuffer(offset, count);

            IAsyncOperationWithProgress<UInt32, UInt32> asyncWriteOperation = wrtStr.WriteAsync(asyncWriteBuffer);

            StreamWriteAsyncResult asyncResult = new StreamWriteAsyncResult(asyncWriteOperation, callback, state,
                                                                            processCompletedOperationInCallback: !usedByBlockingWrapper);

            // The StreamReadAsyncResult will set a private instance method to act as a Completed handler for asyncOperation.
            // This will cause a CCW to be created for the delegate and the delegate has a reference to its target, i.e. to
            // asyncResult, so asyncResult will not be collected. If we loose the entire AppDomain, then asyncResult and its CCW
            // will be collected but the stub will remain and the callback will fail gracefully. The underlying buffer if the only
            // item to which we expose a direct pointer and this is properly pinned using a mechanism similar to Overlapped.

            return asyncResult;
        }

        public override void EndWrite(IAsyncResult asyncResult)
        {
            if (asyncResult == null)
                throw new ArgumentNullException(nameof(asyncResult));

            Contract.EndContractBlock();

            EnsureNotDisposed();
            EnsureCanWrite();

            StreamOperationAsyncResult streamAsyncResult = asyncResult as StreamOperationAsyncResult;
            if (streamAsyncResult == null)
                throw new ArgumentException(SR.Argument_UnexpectedAsyncResult, nameof(asyncResult));

            streamAsyncResult.Wait();

            try
            {
                // If the async result did NOT process the async IO operation in its completion handler (i.e. check for errors,
                // cache results etc), then we need to do that processing now. This is to allow blocking-over-async IO operations.
                // See the big comment in BeginWrite for details.

                if (!streamAsyncResult.ProcessCompletedOperationInCallback)
                    streamAsyncResult.ProcessCompletedOperation();

                // Rethrow errors caught in the completion callback, if any:
                if (streamAsyncResult.HasError)
                {
                    streamAsyncResult.CloseStreamOperation();
                    streamAsyncResult.ThrowCachedError();
                }
            }
            finally
            {
                // Closing multiple times is Ok.
                streamAsyncResult.CloseStreamOperation();
            }
        }

        public override Task WriteAsync(Byte[] buffer, Int32 offset, Int32 count, CancellationToken cancellationToken)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));

            if (offset < 0)
                throw new ArgumentOutOfRangeException(nameof(offset));

            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count));

            if (buffer.Length - offset < count)
                throw new ArgumentException(SR.Argument_InsufficientArrayElementsAfterOffset);

            Contract.EndContractBlock();

            IOutputStream wrtStr = EnsureNotDisposed<IOutputStream>();
            EnsureCanWrite();

#if DEBUG
            AssertValidStream(wrtStr);
#endif  // DEBUG

            // If already cancelled, bail early:
            cancellationToken.ThrowIfCancellationRequested();

            IBuffer asyncWriteBuffer = buffer.AsBuffer(offset, count);

            IAsyncOperationWithProgress<UInt32, UInt32> asyncWriteOperation = wrtStr.WriteAsync(asyncWriteBuffer);
            Task asyncWriteTask = asyncWriteOperation.AsTask(cancellationToken);

            // The underlying IBuffer is the only object to which we expose a direct pointer to native,
            // and that is properly pinned using a mechanism similar to Overlapped.

            return asyncWriteTask;
        }


        public override void Write(Byte[] buffer, Int32 offset, Int32 count)
        {
            // Arguments validation and not-disposed validation are done in BeginWrite.

            IAsyncResult asyncResult = BeginWrite(buffer, offset, count, null, null, usedByBlockingWrapper: true);
            EndWrite(asyncResult);
        }


        public override void WriteByte(Byte value)
        {
            // EnsureNotDisposed will be called in Write->BeginWrite.

            Byte[] oneByteArray = OneByteBuffer;
            oneByteArray[0] = value;

            Write(oneByteArray, 0, 1);
        }

        #endregion Writing


        #region Flushing

        public override void Flush()
        {
            // See the large comment in BeginRead about why we are not using this.FlushAsync,
            // and instead using a custom implementation of IAsyncResult.

            IOutputStream wrtStr = EnsureNotDisposed<IOutputStream>();

            // Calling Flush in a non-writable stream is a no-op, not an error:
            if (!_canWrite)
                return;

#if DEBUG
            AssertValidStream(wrtStr);
#endif  // DEBUG

            IAsyncOperation<Boolean> asyncFlushOperation = wrtStr.FlushAsync();
            StreamFlushAsyncResult asyncResult = new StreamFlushAsyncResult(asyncFlushOperation, processCompletedOperationInCallback: false);

            asyncResult.Wait();

            try
            {
                // We got signaled, so process the async Flush operation back on this thread:
                // (This is to allow blocking-over-async IO operations. See the big comment in BeginRead for details.)
                asyncResult.ProcessCompletedOperation();

                // Rethrow errors cached by the async result, if any:
                if (asyncResult.HasError)
                {
                    asyncResult.CloseStreamOperation();
                    asyncResult.ThrowCachedError();
                }
            }
            finally
            {
                // Closing multiple times is Ok.
                asyncResult.CloseStreamOperation();
            }
        }


        public override Task FlushAsync(CancellationToken cancellationToken)
        {
            IOutputStream wrtStr = EnsureNotDisposed<IOutputStream>();

            // Calling Flush in a non-writable stream is a no-op, not an error:
            if (!_canWrite)
                return Task.CompletedTask;

#if DEBUG
            AssertValidStream(wrtStr);
#endif  // DEBUG

            cancellationToken.ThrowIfCancellationRequested();

            IAsyncOperation<Boolean> asyncFlushOperation = wrtStr.FlushAsync();
            Task asyncFlushTask = asyncFlushOperation.AsTask(cancellationToken);
            return asyncFlushTask;
        }

        #endregion Flushing


        #region ReadAsyncInternal implementation
        // Moved it to the end while using Dev10 VS because it does not understand async and everything that follows looses intellisense.
        // Should move this code into the Reading regios once using Dev11 VS becomes the norm.

        private async Task<Int32> ReadAsyncInternal(Byte[] buffer, Int32 offset, Int32 count, CancellationToken cancellationToken)
        {
            Debug.Assert(buffer != null);
            Debug.Assert(offset >= 0);
            Debug.Assert(count >= 0);
            Debug.Assert(buffer.Length - offset >= count);
            Debug.Assert(_canRead);

            IInputStream wrtStr = EnsureNotDisposed<IInputStream>();

#if DEBUG
            AssertValidStream(wrtStr);
#endif  // DEBUG

            try
            {
                IBuffer userBuffer = buffer.AsBuffer(offset, count);
                IAsyncOperationWithProgress<IBuffer, UInt32> asyncReadOperation = wrtStr.ReadAsync(userBuffer,
                                                                                                   unchecked((UInt32)count),
                                                                                                   InputStreamOptions.Partial);

                IBuffer resultBuffer = await asyncReadOperation.AsTask(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);

                // If cancellationToken was cancelled until now, then we are currently propagating the corresponding cancellation exception.
                // (It will be correctly rethrown by the catch block below and overall we will return a cancelled task.)
                // But if the underlying operation managed to complete before it was cancelled, we want
                // the entire task to complete as well. This is ok as the continuation is very lightweight:

                if (resultBuffer == null)
                    return 0;

                WinRtIOHelper.EnsureResultsInUserBuffer(userBuffer, resultBuffer);

                Debug.Assert(resultBuffer.Length <= unchecked((UInt32)Int32.MaxValue));
                return (Int32)resultBuffer.Length;
            }
            catch (Exception ex)
            {
                // If the interop layer gave us an Exception, we assume that it hit a general/unknown case and wrap it into
                // an IOException as this is what Stream users expect.
                WinRtIOHelper.NativeExceptionToIOExceptionInfo(ex).Throw();
                return 0;
            }
        }
        #endregion ReadAsyncInternal implementation

    }  // class WinRtToNetFxStreamAdapter
}  // namespace

// WinRtToNetFxStreamAdapter.cs
