// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.IO.Pipes
{
    public abstract partial class PipeStream : Stream
    {
        internal sealed class ConnectionValueTaskSource : PipeValueTaskSource<VoidResult>
        {
            private readonly NamedPipeServerStream _serverStream;

            internal ConnectionValueTaskSource(NamedPipeServerStream server)
                : base(server)
            {
                _serverStream = server;
            }

            internal override void SetCompletedSynchronously()
            {
                _serverStream.State = PipeState.Connected;
                SetResult(default(VoidResult));
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

            protected override void HandleError(int errorCode) =>
                SetException(Win32Marshal.GetExceptionForWin32Error(errorCode));

            protected override void HandleUnexpectedCancellation() =>
                SetException(Error.GetOperationAborted());
        }

        internal struct VoidResult { }
    }
}
