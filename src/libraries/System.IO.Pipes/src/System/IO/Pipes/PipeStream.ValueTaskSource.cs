// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Threading;
using Microsoft.Win32.SafeHandles;

namespace System.IO.Pipes
{
    public abstract partial class PipeStream : Stream
    {
        internal sealed class ReadWriteValueTaskSource : OverlappedValueTaskSource
        {
            private readonly PipeStream _pipeStream;

            internal ReadWriteValueTaskSource(PipeStream pipeStream, SafePipeHandle safePipeHandle, ThreadPoolBoundHandle threadPoolBoundHandle)
                : base(safePipeHandle, threadPoolBoundHandle, canSeek: false)
            {
                _pipeStream = pipeStream;
            }

            protected override bool TryToReuse() => _pipeStream.TryToReuse(_isRead, this);

            internal override void Handle(Stream owner, uint errorCode, uint byteCount)
            {
                Debug.Assert(errorCode is not Interop.Errors.ERROR_PIPE_CONNECTED);
                Debug.Assert(ReferenceEquals(_pipeStream, owner));

                bool messageCompletion = true;

                switch (errorCode)
                {
                    // One side has closed its handle or server disconnected.
                    // Set the state to Broken and do some cleanup work
                    case Interop.Errors.ERROR_BROKEN_PIPE:
                    case Interop.Errors.ERROR_PIPE_NOT_CONNECTED:
                    case Interop.Errors.ERROR_NO_DATA:
                    // The handle was closed
                    case Interop.Errors.ERROR_INVALID_HANDLE:
                        _pipeStream.State = PipeState.Broken;
                        break;
                    case Interop.Errors.ERROR_MORE_DATA:
                        messageCompletion = false;
                        break;
                }

                if (_isRead)
                {
                    _pipeStream.UpdateMessageCompletion(messageCompletion);
                }
            }
        }

        internal sealed class ConnectionValueTaskSource : OverlappedValueTaskSource
        {
            private readonly NamedPipeServerStream _serverStream;

            internal ConnectionValueTaskSource(NamedPipeServerStream server, SafePipeHandle safePipeHandle, ThreadPoolBoundHandle threadPoolBoundHandle)
                : base(safePipeHandle, threadPoolBoundHandle, canSeek: false)
            {
                _serverStream = server;
            }

            protected override bool TryToReuse() => _serverStream.TryToReuse(this);

            internal override void Handle(Stream owner, uint errorCode, uint byteCount)
            {
                Debug.Assert(ReferenceEquals(_serverStream, owner));

                switch (errorCode)
                {
                    // The handle was closed (when the operation was awaiting for completion)
                    case Interop.Errors.ERROR_INVALID_HANDLE:
                        _serverStream.State = PipeState.Broken;
                        break;
                    case Interop.Errors.ERROR_SUCCESS:
                    // ConnectNamedPipe can report success by returning ERROR_PIPE_CONNECTED
                    case Interop.Errors.ERROR_PIPE_CONNECTED:
                        _serverStream.State = PipeState.Connected;
                        break;
                }
            }
        }
    }
}
