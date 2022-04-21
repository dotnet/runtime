// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;

namespace System.IO.Pipes
{
    /// <summary>
    /// Named pipe server
    /// </summary>
    public sealed partial class NamedPipeServerStream : PipeStream
    {
        // Use the maximum number of server instances that the system resources allow
        public const int MaxAllowedServerInstances = -1;

        public NamedPipeServerStream(string pipeName)
            : this(pipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.None, 0, 0, HandleInheritability.None)
        {
        }

        public NamedPipeServerStream(string pipeName, PipeDirection direction)
            : this(pipeName, direction, 1, PipeTransmissionMode.Byte, PipeOptions.None, 0, 0, HandleInheritability.None)
        {
        }

        public NamedPipeServerStream(string pipeName, PipeDirection direction, int maxNumberOfServerInstances)
            : this(pipeName, direction, maxNumberOfServerInstances, PipeTransmissionMode.Byte, PipeOptions.None, 0, 0, HandleInheritability.None)
        {
        }

        public NamedPipeServerStream(string pipeName, PipeDirection direction, int maxNumberOfServerInstances, PipeTransmissionMode transmissionMode)
            : this(pipeName, direction, maxNumberOfServerInstances, transmissionMode, PipeOptions.None, 0, 0, HandleInheritability.None)
        {
        }

        public NamedPipeServerStream(string pipeName, PipeDirection direction, int maxNumberOfServerInstances, PipeTransmissionMode transmissionMode, PipeOptions options)
            : this(pipeName, direction, maxNumberOfServerInstances, transmissionMode, options, 0, 0, HandleInheritability.None)
        {
        }

        public NamedPipeServerStream(string pipeName, PipeDirection direction, int maxNumberOfServerInstances, PipeTransmissionMode transmissionMode, PipeOptions options, int inBufferSize, int outBufferSize)
            : this(pipeName, direction, maxNumberOfServerInstances, transmissionMode, options, inBufferSize, outBufferSize, HandleInheritability.None)
        {
        }

        /// <summary>
        /// Full named pipe server constructor
        /// </summary>
        /// <param name="pipeName">Pipe name</param>
        /// <param name="direction">Pipe direction: In, Out or InOut (duplex).
        /// Win32 note: this gets OR'd into dwOpenMode to CreateNamedPipe
        /// </param>
        /// <param name="maxNumberOfServerInstances">Maximum number of server instances. Specify a fixed value between
        /// 1 and 254 (Windows)/greater than 1 (Unix), or use NamedPipeServerStream.MaxAllowedServerInstances to use the
        /// maximum amount allowed by system resources.</param>
        /// <param name="transmissionMode">Byte mode or message mode.
        /// Win32 note: this gets used for dwPipeMode. CreateNamedPipe allows you to specify PIPE_TYPE_BYTE/MESSAGE
        /// and PIPE_READMODE_BYTE/MESSAGE independently, but this sets type and readmode to match.
        /// </param>
        /// <param name="options">PipeOption enum: None, Asynchronous, or Write-through
        /// Win32 note: this gets passed in with dwOpenMode to CreateNamedPipe. Asynchronous corresponds to
        /// FILE_FLAG_OVERLAPPED option. PipeOptions enum doesn't expose FIRST_PIPE_INSTANCE option because
        /// this sets that automatically based on the number of instances specified.
        /// </param>
        /// <param name="inBufferSize">Incoming buffer size, 0 or higher.
        /// Note: this size is always advisory; OS uses a suggestion.
        /// </param>
        /// <param name="outBufferSize">Outgoing buffer size, 0 or higher (see above)</param>
        /// <param name="inheritability">Whether handle is inheritable</param>
        private NamedPipeServerStream(string pipeName,
            PipeDirection direction,
            int maxNumberOfServerInstances,
            PipeTransmissionMode transmissionMode,
            PipeOptions options,
            int inBufferSize,
            int outBufferSize,
            HandleInheritability inheritability)
            : base(direction, transmissionMode, outBufferSize)
        {
            ValidateParameters(pipeName, direction, maxNumberOfServerInstances, transmissionMode, options, inBufferSize, outBufferSize, inheritability);
            Create(pipeName, direction, maxNumberOfServerInstances, transmissionMode, options, inBufferSize, outBufferSize, inheritability);
        }

        private void ValidateParameters(
            string pipeName,
            PipeDirection direction,
            int maxNumberOfServerInstances,
            PipeTransmissionMode transmissionMode,
            PipeOptions options,
            int inBufferSize,
            int outBufferSize,
            HandleInheritability inheritability)
        {
            ArgumentException.ThrowIfNullOrEmpty(pipeName);
            if (direction < PipeDirection.In || direction > PipeDirection.InOut)
            {
                throw new ArgumentOutOfRangeException(nameof(direction), SR.ArgumentOutOfRange_DirectionModeInOutOrInOut);
            }
            if (transmissionMode < PipeTransmissionMode.Byte || transmissionMode > PipeTransmissionMode.Message)
            {
                throw new ArgumentOutOfRangeException(nameof(transmissionMode), SR.ArgumentOutOfRange_TransmissionModeByteOrMsg);
            }
            if ((options & ~(PipeOptions.WriteThrough | PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly)) != 0)
            {
                throw new ArgumentOutOfRangeException(nameof(options), SR.ArgumentOutOfRange_OptionsInvalid);
            }
            if (inBufferSize < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(inBufferSize), SR.ArgumentOutOfRange_NeedNonNegNum);
            }
            if (outBufferSize < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(outBufferSize), SR.ArgumentOutOfRange_NeedNonNegNum);
            }
            if ((maxNumberOfServerInstances < 1 || maxNumberOfServerInstances > 254) && (maxNumberOfServerInstances != MaxAllowedServerInstances))
            {
                // win32 allows fixed values of 1-254 or 255 to mean max allowed by system. We expose 255 as -1 (unlimited)
                // through the MaxAllowedServerInstances constant. This is consistent e.g. with -1 as infinite timeout, etc.
                // We do this check for consistency on Unix, even though maxNumberOfServerInstances is otherwise ignored.
                throw new ArgumentOutOfRangeException(nameof(maxNumberOfServerInstances), SR.ArgumentOutOfRange_MaxNumServerInstances);
            }

            // inheritability will always be None since this private constructor is only called from other constructors from which
            // inheritability is always set to None. Desktop has a public constructor to allow setting it to something else, but Core
            // doesn't.
            if (inheritability < HandleInheritability.None || inheritability > HandleInheritability.Inheritable)
            {
                throw new ArgumentOutOfRangeException(nameof(inheritability), SR.ArgumentOutOfRange_HandleInheritabilityNoneOrInheritable);
            }

            if ((options & PipeOptions.CurrentUserOnly) != 0)
            {
                IsCurrentUserOnly = true;
            }
        }

        // Create a NamedPipeServerStream from an existing server pipe handle.
        public NamedPipeServerStream(PipeDirection direction, bool isAsync, bool isConnected, SafePipeHandle safePipeHandle)
            : base(direction, PipeTransmissionMode.Byte, 0)
        {
            ArgumentNullException.ThrowIfNull(safePipeHandle);

            if (safePipeHandle.IsInvalid)
            {
                throw new ArgumentException(SR.Argument_InvalidHandle, nameof(safePipeHandle));
            }
            ValidateHandleIsPipe(safePipeHandle);

            InitializeHandle(safePipeHandle, true, isAsync);

            if (isConnected)
            {
                State = PipeState.Connected;
            }
        }

        ~NamedPipeServerStream()
        {
            Dispose(false);
        }

        public Task WaitForConnectionAsync()
        {
            return WaitForConnectionAsync(CancellationToken.None);
        }

        public System.IAsyncResult BeginWaitForConnection(AsyncCallback? callback, object? state) =>
            TaskToApm.Begin(WaitForConnectionAsync(), callback, state);

        public void EndWaitForConnection(IAsyncResult asyncResult) =>
            TaskToApm.End(asyncResult);

        // Server can only connect from Disconnected state
        private void CheckConnectOperationsServer()
        {
            // we're not checking whether already connected; this allows us to throw IOException
            // "pipe is being closed" if other side is closing (as does win32) or no-op if
            // already connected

            if (State == PipeState.Closed)
            {
                throw Error.GetPipeNotOpen();
            }
            if (InternalHandle != null && InternalHandle.IsClosed) // only check IsClosed if we have a handle
            {
                throw Error.GetPipeNotOpen();
            }
            if (State == PipeState.Broken)
            {
                throw new IOException(SR.IO_PipeBroken);
            }
        }

        // Server is allowed to disconnect from connected and broken states
        private void CheckDisconnectOperations()
        {
            if (State == PipeState.WaitingToConnect)
            {
                throw new InvalidOperationException(SR.InvalidOperation_PipeNotYetConnected);
            }
            if (State == PipeState.Disconnected)
            {
                throw new InvalidOperationException(SR.InvalidOperation_PipeAlreadyDisconnected);
            }
            if (InternalHandle == null && CheckOperationsRequiresSetHandle)
            {
                throw new InvalidOperationException(SR.InvalidOperation_PipeHandleNotSet);
            }
            if ((State == PipeState.Closed) || (InternalHandle != null && InternalHandle.IsClosed))
            {
                throw Error.GetPipeNotOpen();
            }
        }
    }

    // Users will use this delegate to specify a method to call while impersonating the client
    // (see NamedPipeServerStream.RunAsClient).
    public delegate void PipeStreamImpersonationWorker();
}
