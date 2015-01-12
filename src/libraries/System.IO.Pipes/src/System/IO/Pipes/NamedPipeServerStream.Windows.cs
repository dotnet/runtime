// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Win32.SafeHandles;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace System.IO.Pipes
{
    /// <summary>
    /// Named pipe server
    /// </summary>
    public sealed partial class NamedPipeServerStream : PipeStream
    {
        [SecurityCritical]
        private unsafe static readonly IOCompletionCallback s_WaitForConnectionCallback =
            new IOCompletionCallback(NamedPipeServerStream.AsyncWaitForConnectionCallback);

        [SecurityCritical]
        private void Create(string pipeName, PipeDirection direction, int maxNumberOfServerInstances,
                PipeTransmissionMode transmissionMode, PipeOptions options, int inBufferSize, int outBufferSize,
                HandleInheritability inheritability)
        {
            Debug.Assert(pipeName != null && pipeName.Length != 0, "fullPipeName is null or empty");
            Debug.Assert(direction >= PipeDirection.In && direction <= PipeDirection.InOut, "invalid pipe direction");
            Debug.Assert(inBufferSize >= 0, "inBufferSize is negative");
            Debug.Assert(outBufferSize >= 0, "outBufferSize is negative");
            Debug.Assert((maxNumberOfServerInstances >= 1 && maxNumberOfServerInstances <= 254) || (maxNumberOfServerInstances == MaxAllowedServerInstances), "maxNumberOfServerInstances is invalid");
            Debug.Assert(transmissionMode >= PipeTransmissionMode.Byte && transmissionMode <= PipeTransmissionMode.Message, "transmissionMode is out of range");

            string fullPipeName = Path.GetFullPath(@"\\.\pipe\" + pipeName);

            // Make sure the pipe name isn't one of our reserved names for anonymous pipes.
            if (String.Equals(fullPipeName, @"\\.\pipe\anonymous", StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentOutOfRangeException("pipeName", SR.ArgumentOutOfRange_AnonymousReserved);
            }


            int openMode = ((int)direction) |
                           (maxNumberOfServerInstances == 1 ? Interop.FILE_FLAG_FIRST_PIPE_INSTANCE : 0) |
                           (int)options;

            // We automatically set the ReadMode to match the TransmissionMode.
            int pipeModes = (int)transmissionMode << 2 | (int)transmissionMode << 1;

            // Convert -1 to 255 to match win32 (we asserted that it is between -1 and 254).
            if (maxNumberOfServerInstances == MaxAllowedServerInstances)
            {
                maxNumberOfServerInstances = 255;
            }

            Interop.SECURITY_ATTRIBUTES secAttrs = PipeStream.GetSecAttrs(inheritability);
            SafePipeHandle handle = Interop.mincore.CreateNamedPipe(fullPipeName, openMode, pipeModes,
                maxNumberOfServerInstances, outBufferSize, inBufferSize, 0, ref secAttrs);

            if (handle.IsInvalid)
            {
                throw Win32Marshal.GetExceptionForLastWin32Error();
            }

            InitializeHandle(handle, false, (options & PipeOptions.Asynchronous) != 0);
        }

        // This will wait until the client calls Connect().  If we return from this method, we guarantee that
        // the client has returned from its Connect call.   The client may have done so before this method 
        // was called (but not before this server is been created, or, if we were servicing another client, 
        // not before we called Disconnect), in which case, there may be some buffer already in the pipe waiting
        // for us to read.  See NamedPipeClientStream.Connect for more information.
        [SecurityCritical]
        [SuppressMessage("Microsoft.Security", "CA2122:DoNotIndirectlyExposeMethodsWithLinkDemands", Justification = "Security model of pipes: demand at creation but no subsequent demands")]
        public void WaitForConnection()
        {
            CheckConnectOperationsServer();

            if (IsAsync)
            {
                IAsyncResult result = BeginWaitForConnection(null, null);
                EndWaitForConnection(result);
            }
            else
            {
                if (!Interop.mincore.ConnectNamedPipe(InternalHandle, Interop.NULL))
                {
                    int errorCode = Marshal.GetLastWin32Error();

                    if (errorCode != Interop.ERROR_PIPE_CONNECTED)
                    {
                        throw Win32Marshal.GetExceptionForWin32Error(errorCode);
                    }

                    // pipe already connected
                    if (errorCode == Interop.ERROR_PIPE_CONNECTED && State == PipeState.Connected)
                    {
                        throw new InvalidOperationException(SR.InvalidOperation_PipeAlreadyConnected);
                    }
                    // If we reach here then a connection has been established.  This can happen if a client 
                    // connects in the interval between the call to CreateNamedPipe and the call to ConnectNamedPipe. 
                    // In this situation, there is still a good connection between client and server, even though 
                    // ConnectNamedPipe returns zero.
                }
                State = PipeState.Connected;
            }
        }

        public Task WaitForConnectionAsync(CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return Task.FromCanceled(cancellationToken);
            }

            if (!IsAsync)
            {
                return Task.Factory.StartNew(s => ((NamedPipeServerStream)s).WaitForConnection(),
                    this, cancellationToken, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
            }

            return Task.Factory.FromAsync(
                BeginWaitForConnection, EndWaitForConnection, 
                cancellationToken.CanBeCanceled ? new IOCancellationHelper(cancellationToken) : null);
        }

        // Async version of WaitForConnection.  See the comments above for more info.
        [SecurityCritical]
        private unsafe IAsyncResult BeginWaitForConnection(AsyncCallback callback, Object state)
        {
            CheckConnectOperationsServer();

            if (!IsAsync)
            {
                throw new InvalidOperationException(SR.InvalidOperation_PipeNotAsync);
            }

            // Create and store async stream class library specific data in the 
            // async result
            PipeAsyncResult asyncResult = new PipeAsyncResult();
            asyncResult._handle = InternalHandle;
            asyncResult._userCallback = callback;
            asyncResult._userStateObject = state;

            IOCancellationHelper cancellationHelper = state as IOCancellationHelper;

            // Create wait handle and store in async result
            ManualResetEvent waitHandle = new ManualResetEvent(false);
            asyncResult._waitHandle = waitHandle;

            // Create a managed overlapped class
            // We will set the file offsets later
            Overlapped overlapped = new Overlapped();
            overlapped.OffsetLow = 0;
            overlapped.OffsetHigh = 0;
            overlapped.AsyncResult = asyncResult;

            // Pack the Overlapped class, and store it in the async result
            NativeOverlapped* intOverlapped = overlapped.Pack(s_WaitForConnectionCallback, null);
            asyncResult._overlapped = intOverlapped;
            if (!Interop.mincore.ConnectNamedPipe(InternalHandle, intOverlapped))
            {
                int errorCode = Marshal.GetLastWin32Error();

                if (errorCode == Interop.ERROR_IO_PENDING)
                {
                    if (cancellationHelper != null)
                    {
                        cancellationHelper.AllowCancellation(InternalHandle, intOverlapped);
                    }
                    return asyncResult;
                }

                // WaitForConnectionCallback will not be called becasue we completed synchronously.
                // Either the pipe is already connected, or there was an error. Unpin and free the overlapped again.
                Overlapped.Free(intOverlapped);
                asyncResult._overlapped = null;

                // Did the client already connect to us?
                if (errorCode == Interop.ERROR_PIPE_CONNECTED)
                {
                    if (State == PipeState.Connected)
                    {
                        throw new InvalidOperationException(SR.InvalidOperation_PipeAlreadyConnected);
                    }
                    asyncResult.CallUserCallback();
                    return asyncResult;
                }

                throw Win32Marshal.GetExceptionForWin32Error(errorCode);
            }
            // will set state to Connected when EndWait is called
            if (cancellationHelper != null)
            {
                cancellationHelper.AllowCancellation(InternalHandle, intOverlapped);
            }

            return asyncResult;
        }

        // Async version of WaitForConnection.  See comments for WaitForConnection for more info.
        [SecurityCritical]
        private unsafe void EndWaitForConnection(IAsyncResult asyncResult)
        {
            CheckConnectOperationsServer();

            if (asyncResult == null)
            {
                throw new ArgumentNullException("asyncResult");
            }
            if (!IsAsync)
            {
                throw new InvalidOperationException(SR.InvalidOperation_PipeNotAsync);
            }

            PipeAsyncResult afsar = asyncResult as PipeAsyncResult;
            if (afsar == null)
            {
                throw __Error.GetWrongAsyncResult();
            }

            // Ensure we can't get into any races by doing an interlocked
            // CompareExchange here.  Avoids corrupting memory via freeing the
            // NativeOverlapped class or GCHandle twice.  -- 
            if (1 == Interlocked.CompareExchange(ref afsar._EndXxxCalled, 1, 0))
            {
                throw __Error.GetEndWaitForConnectionCalledTwice();
            }

            IOCancellationHelper cancellationHelper = afsar.AsyncState as IOCancellationHelper;
            if (cancellationHelper != null)
            {
                cancellationHelper.SetOperationCompleted();
            }

            // Obtain the WaitHandle, but don't use public property in case we
            // delay initialize the manual reset event in the future.
            WaitHandle wh = afsar._waitHandle;
            if (wh != null)
            {
                // We must block to ensure that ConnectionIOCallback has completed,
                // and we should close the WaitHandle in here.  AsyncFSCallback
                // and the hand-ported imitation version in COMThreadPool.cpp 
                // are the only places that set this event.
                using (wh)
                {
                    wh.WaitOne();
                    Debug.Assert(afsar._isComplete == true, "NamedPipeServerStream::EndWaitForConnection - AsyncFSCallback didn't set _isComplete to true!");
                }
            }

            // We should have freed the overlapped and set it to null either in the Begin
            // method (if ConnectNamedPipe completed synchronously) or in AsyncWaitForConnectionCallback.
            // If it is not nulled out, we should not be past the above wait:
            Debug.Assert(afsar._overlapped == null);

            // Now check for any error during the read.
            if (afsar._errorCode != 0)
            {
                if (afsar._errorCode == Interop.ERROR_OPERATION_ABORTED)
                {
                    if (cancellationHelper != null)
                    {
                        cancellationHelper.ThrowIOOperationAborted();
                    }
                }
                throw Win32Marshal.GetExceptionForWin32Error(afsar._errorCode);
            }

            // Success
            State = PipeState.Connected;
        }

        [SecurityCritical]
        public void Disconnect()
        {
            CheckDisconnectOperations();

            // Disconnect the pipe.
            if (!Interop.mincore.DisconnectNamedPipe(InternalHandle))
            {
                throw Win32Marshal.GetExceptionForLastWin32Error();
            }

            State = PipeState.Disconnected;
        }

#if RunAs
        // This method calls a delegate while impersonating the client. Note that we will not have
        // access to the client's security token until it has written at least once to the pipe 
        // (and has set its impersonationLevel argument appropriately). 
        [SecurityCritical]
        [SecurityPermissionAttribute(SecurityAction.Demand, Flags = SecurityPermissionFlag.ControlPrincipal)]
        public void RunAsClient(PipeStreamImpersonationWorker impersonationWorker)
        {
            CheckWriteOperations();
            ExecuteHelper execHelper = new ExecuteHelper(impersonationWorker, InternalHandle);
            RuntimeHelpers.ExecuteCodeWithGuaranteedCleanup(tryCode, cleanupCode, execHelper);

            // now handle win32 impersonate/revert specific errors by throwing corresponding exceptions
            if (execHelper._impersonateErrorCode != 0)
            {
                WinIOError(execHelper._impersonateErrorCode);
            }
            else if (execHelper._revertImpersonateErrorCode != 0)
            {
                WinIOError(execHelper._revertImpersonateErrorCode);
            }
        }

        // the following are needed for CER

        private static RuntimeHelpers.TryCode tryCode = new RuntimeHelpers.TryCode(ImpersonateAndTryCode);
        private static RuntimeHelpers.CleanupCode cleanupCode = new RuntimeHelpers.CleanupCode(RevertImpersonationOnBackout);

        [SecurityCritical]
        private static void ImpersonateAndTryCode(Object helper)
        {
            ExecuteHelper execHelper = (ExecuteHelper)helper;

            RuntimeHelpers.PrepareConstrainedRegions();
            try { }
            finally
            {
                if (UnsafeNativeMethods.ImpersonateNamedPipeClient(execHelper._handle))
                {
                    execHelper._mustRevert = true;
                }
                else
                {
                    execHelper._impersonateErrorCode = Marshal.GetLastWin32Error();
                }

            }

            if (execHelper._mustRevert)
            { // impersonate passed so run user code
                execHelper._userCode();
            }
        }

        [SecurityCritical]
        [PrePrepareMethod]
        private static void RevertImpersonationOnBackout(Object helper, bool exceptionThrown)
        {
            ExecuteHelper execHelper = (ExecuteHelper)helper;

            if (execHelper._mustRevert)
            {
                if (!UnsafeNativeMethods.RevertToSelf())
                {
                    execHelper._revertImpersonateErrorCode = Marshal.GetLastWin32Error();
                }
            }
        }

        internal class ExecuteHelper
        {
            internal PipeStreamImpersonationWorker _userCode;
            internal SafePipeHandle _handle;
            internal bool _mustRevert;
            internal int _impersonateErrorCode;
            internal int _revertImpersonateErrorCode;

            [SecurityCritical]
            internal ExecuteHelper(PipeStreamImpersonationWorker userCode, SafePipeHandle handle)
            {
                _userCode = userCode;
                _handle = handle;
            }
        }
#endif

        // Gets the username of the connected client.  Not that we will not have access to the client's 
        // username until it has written at least once to the pipe (and has set its impersonationLevel 
        // argument appropriately). 
        [SecurityCritical]
        public String GetImpersonationUserName()
        {
            CheckWriteOperations();

            StringBuilder userName = new StringBuilder(Interop.CREDUI_MAX_USERNAME_LENGTH + 1);

            if (!Interop.mincore.GetNamedPipeHandleState(InternalHandle, Interop.NULL, Interop.NULL,
                Interop.NULL, Interop.NULL, userName, userName.Capacity))
            {
                WinIOError(Marshal.GetLastWin32Error());
            }

            return userName.ToString();
        }

        // Callback to be called by the OS when completing the async WaitForConnection operation.
        [SecurityCritical]
        unsafe private static void AsyncWaitForConnectionCallback(uint errorCode, uint numBytes, NativeOverlapped* pOverlapped)
        {
            // Unpack overlapped
            Overlapped overlapped = Overlapped.Unpack(pOverlapped);

            // Extract async result from overlapped 
            PipeAsyncResult asyncResult = (PipeAsyncResult)overlapped.AsyncResult;

            // Free the pinned overlapped:    
            Debug.Assert(asyncResult._overlapped == pOverlapped);
            Overlapped.Free(pOverlapped);
            asyncResult._overlapped = null;

            // Special case for when the client has already connected to us.
            if (errorCode == Interop.ERROR_PIPE_CONNECTED)
            {
                errorCode = 0;
            }

            asyncResult._errorCode = (int)errorCode;

            // Call the user-provided callback.  It can and often should
            // call EndWaitForConnection.  There's no reason to use an async 
            // delegate here - we're already on a threadpool thread.  
            // IAsyncResult's completedSynchronously property must return
            // false here, saying the user callback was called on another thread.
            asyncResult._completedSynchronously = false;
            asyncResult._isComplete = true;

            // The OS does not signal this event.  We must do it ourselves.
            ManualResetEvent wh = asyncResult._waitHandle;
            if (wh != null)
            {
                Debug.Assert(!wh.GetSafeWaitHandle().IsClosed, "ManualResetEvent already closed!");
                bool r = wh.Set();
                Debug.Assert(r, "ManualResetEvent::Set failed!");
                if (!r)
                {
                    throw Win32Marshal.GetExceptionForLastWin32Error();
                }
            }

            AsyncCallback userCallback = asyncResult._userCallback;

            if (userCallback != null)
            {
                userCallback(asyncResult);
            }
        }
    }
}
