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
        // Sentinel object passed to callers of PostCompletion to use as the
        // "result" of this operation, in order to avoid boxing the actual result.
        private static readonly object s_resultObjectSentinel = new object();
        // The actual result (number of bytes transferred)
        internal int _numBytes;

        // PostCompletion returns the result object to be set before the user's callback is invoked.
        internal virtual object PostCompletion(int numBytes)
        {
            _numBytes = numBytes;
            return s_resultObjectSentinel; // return sentinel rather than boxing numBytes
        }

        // Used instead of the base InternalWaitForCompletion when storing an Int32 result
        internal int InternalWaitForCompletionInt32Result()
        {
            base.InternalWaitForCompletion();
            return _numBytes;
        }

        // This method is called after an asynchronous call is made for the user.
        // It checks and acts accordingly if the IO:
        // 1) completed synchronously.
        // 2) was pended.
        // 3) failed.
        internal unsafe SocketError CheckAsyncCallOverlappedResult(SocketError errorCode)
        {
            if (NetEventSource.IsEnabled) NetEventSource.Info(this, errorCode);

            if (errorCode == SocketError.Success || errorCode == SocketError.IOPending)
            {
                // Ignore cases in which a completion packet will be queued:
                // we'll deal with this IO in the callback.
                return SocketError.Success;
            }

            // In the remaining cases a completion packet will NOT be queued:
            // we have to call the callback explicitly signaling an error.
            ErrorCode = (int)errorCode;
            Result = -1;

            ReleaseUnmanagedStructures();  // Additional release for the completion that won't happen.
            return errorCode;
        }
    }
}
