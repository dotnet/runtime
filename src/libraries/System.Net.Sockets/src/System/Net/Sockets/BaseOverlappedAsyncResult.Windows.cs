// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Threading;

namespace System.Net.Sockets
{
    // BaseOverlappedAsyncResult
    //
    // This class is used to track state for async Socket operations such as the BeginSend, BeginSendTo,
    // BeginReceive, BeginReceiveFrom, BeginSendFile, and BeginAccept calls.
    internal partial class BaseOverlappedAsyncResult : ContextAwareResult
    {
        private int _cleanupCount;
        private SafeNativeOverlapped _nativeOverlapped;

        // The WinNT Completion Port callback.
        private static unsafe readonly IOCompletionCallback s_ioCallback = new IOCompletionCallback(CompletionPortCallback);

        internal BaseOverlappedAsyncResult(Socket socket, Object asyncState, AsyncCallback asyncCallback)
            : base(socket, asyncState, asyncCallback)
        {
            _cleanupCount = 1;
            if (NetEventSource.IsEnabled) NetEventSource.Info(this, socket);
        }

        // SetUnmanagedStructures
        //
        // This needs to be called for overlapped IO to function properly.
        //
        // Fills in overlapped Structures used in an async overlapped Winsock call.
        // These calls are outside the runtime and are unmanaged code, so we need
        // to prepare specific structures and ints that lie in unmanaged memory
        // since the overlapped calls may complete asynchronously.
        internal void SetUnmanagedStructures(object objectsToPin)
        {
            Socket s = (Socket)AsyncObject;

            // Bind the Win32 Socket Handle to the ThreadPool
            Debug.Assert(s != null, "m_CurrentSocket is null");
            Debug.Assert(s.SafeHandle != null, "m_CurrentSocket.SafeHandle is null");

            if (s.SafeHandle.IsInvalid)
            {
                throw new ObjectDisposedException(s.GetType().FullName);
            }

            ThreadPoolBoundHandle boundHandle = s.GetOrAllocateThreadPoolBoundHandle();

            unsafe
            {
                NativeOverlapped* overlapped = boundHandle.AllocateNativeOverlapped(s_ioCallback, this, objectsToPin);
                _nativeOverlapped = new SafeNativeOverlapped(s.SafeHandle, overlapped);
            }

            if (NetEventSource.IsEnabled) NetEventSource.Info(this, $"{boundHandle}::AllocateNativeOverlapped. return={_nativeOverlapped}");
        }

        private static unsafe void CompletionPortCallback(uint errorCode, uint numBytes, NativeOverlapped* nativeOverlapped)
        {
#if DEBUG
            DebugThreadTracking.SetThreadSource(ThreadKinds.CompletionPort);
            using (DebugThreadTracking.SetThreadKind(ThreadKinds.System))
            {
#endif
                BaseOverlappedAsyncResult asyncResult = (BaseOverlappedAsyncResult)ThreadPoolBoundHandle.GetNativeOverlappedState(nativeOverlapped);

                if (asyncResult.InternalPeekCompleted)
                {
                    NetEventSource.Fail(null, $"asyncResult.IsCompleted: {asyncResult}");
                }
                if (NetEventSource.IsEnabled) NetEventSource.Info(null, $"errorCode:{errorCode} numBytes:{numBytes} nativeOverlapped:{(IntPtr)nativeOverlapped}");

                // Complete the IO and invoke the user's callback.
                SocketError socketError = (SocketError)errorCode;

                if (socketError != SocketError.Success && socketError != SocketError.OperationAborted)
                {
                    // There are cases where passed errorCode does not reflect the details of the underlined socket error.
                    // "So as of today, the key is the difference between WSAECONNRESET and ConnectionAborted,
                    //  .e.g remote party or network causing the connection reset or something on the local host (e.g. closesocket
                    // or receiving data after shutdown (SD_RECV)).  With Winsock/TCP stack rewrite in longhorn, there may
                    // be other differences as well."

                    Socket socket = asyncResult.AsyncObject as Socket;
                    if (socket == null)
                    {
                        socketError = SocketError.NotSocket;
                    }
                    else if (socket.CleanedUp)
                    {
                        socketError = SocketError.OperationAborted;
                    }
                    else
                    {
                        try
                        {
                            // The async IO completed with a failure.
                            // Here we need to call WSAGetOverlappedResult() just so GetLastSocketError() will return the correct error.
                            SocketFlags ignore;
                            bool success = Interop.Winsock.WSAGetOverlappedResult(
                                socket.SafeHandle,
                                nativeOverlapped,
                                out numBytes,
                                false,
                                out ignore);
                            if (!success)
                            {
                                socketError = SocketPal.GetLastSocketError();
                            }
                            if (success)
                            {
                                NetEventSource.Fail(asyncResult, $"Unexpectedly succeeded. errorCode:{errorCode} numBytes:{numBytes}");
                            }
                        }
                        catch (ObjectDisposedException)
                        {
                            // CleanedUp check above does not always work since this code is subject to race conditions
                            socketError = SocketError.OperationAborted;
                        }
                    }
                }

                // Set results and invoke callback
                asyncResult.CompletionCallback((int)numBytes, socketError);
#if DEBUG
            }
#endif
        }

        // Called either synchronously from SocketPal async routines or asynchronously via CompletionPortCallback above. 
        private void CompletionCallback(int numBytes, SocketError socketError)
        {
            ErrorCode = (int)socketError;
            object result = PostCompletion(numBytes);
            ReleaseUnmanagedStructures(); // must come after PostCompletion, as overrides may use these resources
            InvokeCallback(result);
        }

        internal unsafe NativeOverlapped* DangerousOverlappedPointer => (NativeOverlapped*)_nativeOverlapped.DangerousGetHandle();

        // Check the result of the overlapped operation.
        // Handle synchronous success by completing the asyncResult here.
        // Handle synchronous failure by cleaning up and returning a SocketError.
        internal SocketError ProcessOverlappedResult(bool success, int bytesTransferred)
        {
            if (success)
            {
                // Synchronous success.
                Socket socket = (Socket)AsyncObject;
                if (socket.SafeHandle.SkipCompletionPortOnSuccess)
                {
                    // The socket handle is configured to skip completion on success, 
                    // so we can complete this asyncResult right now.
                    CompletionCallback(bytesTransferred, SocketError.Success);
                    return SocketError.Success;
                }

                // Socket handle is going to post a completion to the completion port (may have done so already).
                // Return pending and we will continue in the completion port callback.
                return SocketError.IOPending;
            }

            // Get the socket error (which may be IOPending)
            SocketError errorCode = SocketPal.GetLastSocketError();

            if (errorCode == SocketError.IOPending)
            {
                // Operation is pending.
                // We will continue when the completion arrives (may have already at this point).
                return SocketError.IOPending;
            }

            // Synchronous failure.
            // Release overlapped and pinned structures.
            ReleaseUnmanagedStructures();

            return errorCode;
        }

        internal void ReleaseUnmanagedStructures()
        {
            if (Interlocked.Decrement(ref _cleanupCount) == 0)
            {
                ForceReleaseUnmanagedStructures();
            }
        }

        protected override void Cleanup()
        {
            base.Cleanup();

            // If we get all the way to here and it's still not cleaned up...
            if (_cleanupCount > 0 && Interlocked.Exchange(ref _cleanupCount, 0) > 0)
            {
                ForceReleaseUnmanagedStructures();
            }
        }

        // Utility cleanup routine. Frees the overlapped structure.
        // This should be overridden to free pinned and unmanaged memory in the subclass.
        // It needs to also be invoked from the subclass.
        protected virtual void ForceReleaseUnmanagedStructures()
        {
            // Free the unmanaged memory if allocated.
            if (NetEventSource.IsEnabled) NetEventSource.Enter(this);
            _nativeOverlapped.Dispose();
            _nativeOverlapped = null;
            GC.SuppressFinalize(this);
        }
    }
}
