// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;

namespace System.IO.Pipes
{
    internal sealed class ConnectionCompletionSource : PipeCompletionSource<VoidResult>
    {
        private readonly NamedPipeServerStream _serverStream;

        // Using RunContinuationsAsynchronously for compat reasons (old API used ThreadPool.QueueUserWorkItem for continuations)
        internal ConnectionCompletionSource(NamedPipeServerStream server)
            : base(server._threadPoolBinding, ReadOnlyMemory<byte>.Empty)
        {
            _serverStream = server;
        }

        internal override void SetCompletedSynchronously()
        {
            _serverStream.State = PipeState.Connected;
            TrySetResult(default(VoidResult));
        }

        protected override void AsyncCallback(uint errorCode, uint numBytes)
        {
            // Special case for when the client has already connected to us.
            if (errorCode == Interop.Errors.ERROR_PIPE_CONNECTED)
            {
                errorCode = 0;
            }

            base.AsyncCallback(errorCode, numBytes);
        }

        protected override void HandleError(int errorCode)
        {
            TrySetException(Win32Marshal.GetExceptionForWin32Error(errorCode));
        }

        protected override void HandleUnexpectedCancellation() => TrySetException(Error.GetOperationAborted());
    }

    internal struct VoidResult { }
}
