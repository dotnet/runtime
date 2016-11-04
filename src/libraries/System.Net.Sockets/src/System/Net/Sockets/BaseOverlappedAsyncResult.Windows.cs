// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Net;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.Win32;

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
        private unsafe static readonly IOCompletionCallback s_ioCallback = new IOCompletionCallback(CompletionPortCallback);

        internal BaseOverlappedAsyncResult(Socket socket, Object asyncState, AsyncCallback asyncCallback)
            : base(socket, asyncState, asyncCallback)
        {
            _cleanupCount = 1;
            if (NetEventSource.IsEnabled) NetEventSource.Info(this, socket);
        }

        internal SafeNativeOverlapped NativeOverlapped
        {
            get
            {
                return _nativeOverlapped;
            }
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

            ThreadPoolBoundHandle boundHandle = s.SafeHandle.GetOrAllocateThreadPoolBoundHandle();

            unsafe
            {
                NativeOverlapped* overlapped = boundHandle.AllocateNativeOverlapped(s_ioCallback, this, objectsToPin);
                _nativeOverlapped = new SafeNativeOverlapped(s.SafeHandle, overlapped);
                if (NetEventSource.IsEnabled) NetEventSource.Info(this, $"{boundHandle}::AllocateNativeOverlapped. return={_nativeOverlapped}");
            }
        }

        private unsafe static void CompletionPortCallback(uint errorCode, uint numBytes, NativeOverlapped* nativeOverlapped)
        {
#if DEBUG
            DebugThreadTracking.SetThreadSource(ThreadKinds.CompletionPort);
            using (DebugThreadTracking.SetThreadKind(ThreadKinds.System))
            {
#endif
                BaseOverlappedAsyncResult asyncResult = (BaseOverlappedAsyncResult)ThreadPoolBoundHandle.GetNativeOverlappedState(nativeOverlapped);

                object returnObject = null;

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
                            // Here we need to call WSAGetOverlappedResult() just so Marshal.GetLastWin32Error() will return the correct error.
                            SocketFlags ignore;
                            bool success = Interop.Winsock.WSAGetOverlappedResult(
                                socket.SafeHandle,
                                asyncResult.NativeOverlapped,
                                out numBytes,
                                false,
                                out ignore);
                            if (!success)
                            {
                                socketError = (SocketError)Marshal.GetLastWin32Error();
                                if (socketError == 0)
                                {
                                    NetEventSource.Fail(asyncResult, $"socketError:0 numBytes:{numBytes}");
                                }
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
                asyncResult.ErrorCode = (int)socketError;
                returnObject = asyncResult.PostCompletion((int)numBytes);
                asyncResult.ReleaseUnmanagedStructures();
                asyncResult.InvokeCallback(returnObject);
#if DEBUG
            }
#endif
        }

        // The following property returns the Win32 unsafe pointer to
        // whichever Overlapped structure we're using for IO.
        internal SafeHandle OverlappedHandle
        {
            get
            {
                // On WinNT we need to use (due to the current implementation)
                // an Overlapped object in order to bind the socket to the
                // ThreadPool's completion port, so return the native handle
                return _nativeOverlapped == null ? SafeNativeOverlapped.Zero : _nativeOverlapped;
            }
        }

        private void ReleaseUnmanagedStructures()
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
