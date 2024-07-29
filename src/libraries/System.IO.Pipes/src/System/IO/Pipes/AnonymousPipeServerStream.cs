// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Win32.SafeHandles;

namespace System.IO.Pipes
{
    /// <summary>
    /// Anonymous pipe server stream
    /// </summary>
    public sealed partial class AnonymousPipeServerStream : PipeStream
    {
        private SafePipeHandle _clientHandle = null!;
        private bool _clientHandleExposed, _clientHandleExposedAsString;
        private readonly HandleInheritability _inheritability;

        public AnonymousPipeServerStream()
            : this(PipeDirection.Out, HandleInheritability.None, 0)
        {
        }

        public AnonymousPipeServerStream(PipeDirection direction)
            : this(direction, HandleInheritability.None, 0)
        {
        }

        public AnonymousPipeServerStream(PipeDirection direction, HandleInheritability inheritability)
            : this(direction, inheritability, 0)
        {
        }

        // Create an AnonymousPipeServerStream from two existing pipe handles.
        public AnonymousPipeServerStream(PipeDirection direction, SafePipeHandle serverSafePipeHandle, SafePipeHandle clientSafePipeHandle)
            : base(direction, 0)
        {
            if (direction == PipeDirection.InOut)
            {
                throw new NotSupportedException(SR.NotSupported_AnonymousPipeUnidirectional);
            }
            ArgumentNullException.ThrowIfNull(serverSafePipeHandle);
            ArgumentNullException.ThrowIfNull(clientSafePipeHandle);

            if (serverSafePipeHandle.IsInvalid)
            {
                throw new ArgumentException(SR.Argument_InvalidHandle, nameof(serverSafePipeHandle));
            }
            if (clientSafePipeHandle.IsInvalid)
            {
                throw new ArgumentException(SR.Argument_InvalidHandle, nameof(clientSafePipeHandle));
            }
            ValidateHandleIsPipe(serverSafePipeHandle);
            ValidateHandleIsPipe(clientSafePipeHandle);

            InitializeHandle(serverSafePipeHandle, true, false);

            _clientHandle = clientSafePipeHandle;
            _clientHandleExposed = true;
            State = PipeState.Connected;
        }

        // bufferSize is used as a suggestion; specify 0 to let OS decide
        // This constructor instantiates the PipeSecurity using just the inheritability flag
        public AnonymousPipeServerStream(PipeDirection direction, HandleInheritability inheritability, int bufferSize)
            : base(direction, bufferSize)
        {
            if (direction == PipeDirection.InOut)
            {
                throw new NotSupportedException(SR.NotSupported_AnonymousPipeUnidirectional);
            }
            if (inheritability < HandleInheritability.None || inheritability > HandleInheritability.Inheritable)
            {
                throw new ArgumentOutOfRangeException(nameof(inheritability), SR.ArgumentOutOfRange_HandleInheritabilityNoneOrInheritable);
            }

            Create(direction, inheritability, bufferSize);
            _inheritability = inheritability;
        }

        ~AnonymousPipeServerStream()
        {
            Dispose(false);
        }

        // This method should exist until we add a first class way of passing handles between parent and child
        // processes. For now, people do it via command line arguments.
        public string GetClientHandleAsString()
        {
            _clientHandleExposedAsString = _clientHandleExposed = true;
            GC.SuppressFinalize(_clientHandle);
            return _clientHandle.DangerousGetHandle().ToString();
        }

        public SafePipeHandle ClientSafePipeHandle
        {
            get
            {
                _clientHandleExposed = true;
                return _clientHandle;
            }
        }

        // This method is an annoying one but it has to exist at least until we make passing handles between
        // processes first class.  We need this because once the child handle is inherited, the OS considers
        // the parent and child's handles to be different.  Therefore, if a child closes its handle, our
        // Read/Write methods won't throw because the OS will think that there is still a child handle around
        // that can still Write/Read to/from the other end of the pipe.
        //
        // Ideally, we would want the Process class to close this handle after it has been inherited.  See
        // the pipe spec future features section for more information.
        //
        // Right now, this is the best signal to set the anonymous pipe as connected; if this is called, we
        // know the client has been passed the handle and so the connection is live.
        public void DisposeLocalCopyOfClientHandle()
        {
            if (_clientHandle != null && !_clientHandle.IsClosed)
            {
                _clientHandle.Dispose();
            }
        }

        protected override void Dispose(bool disposing)
        {
            try
            {
                // We should dispose of the client handle when it was not exposed at all OR
                // it was exposed as a string (handle finalization has been suppressed) and created inheritable (out-of-proc communication).
                if (!_clientHandleExposed || (_clientHandleExposedAsString && _inheritability == HandleInheritability.Inheritable))
                {
                    DisposeLocalCopyOfClientHandle();
                }
            }
            finally
            {
                base.Dispose(disposing);
            }
        }

        // Anonymous pipes do not support message mode so there is no need to use the base version that P/Invokes here.
        public override PipeTransmissionMode TransmissionMode
        {
            get { return PipeTransmissionMode.Byte; }
        }

        public override PipeTransmissionMode ReadMode
        {
            set
            {
                CheckPipePropertyOperations();

                if (value < PipeTransmissionMode.Byte || value > PipeTransmissionMode.Message)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), SR.ArgumentOutOfRange_TransmissionModeByteOrMsg);
                }
                if (value == PipeTransmissionMode.Message)
                {
                    throw new NotSupportedException(SR.NotSupported_AnonymousPipeMessagesNotSupported);
                }
            }
        }
    }
}
