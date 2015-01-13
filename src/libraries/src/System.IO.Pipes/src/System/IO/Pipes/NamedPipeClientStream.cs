// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Win32.SafeHandles;
using System.Security;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;

namespace System.IO.Pipes
{
    /// <summary>
    /// Named pipe client. Use this to open the client end of a named pipes created with 
    /// NamedPipeServerStream.
    /// </summary>
    public sealed partial class NamedPipeClientStream : PipeStream
    {
        // Maximum interval in miliseconds between which cancellation is checked.
        // Used by ConnectInternal. 50ms is fairly responsive time but really long time for processor.
        private const int CancellationCheckInterval = 50;
        private readonly string _normalizedPipePath;
        private readonly TokenImpersonationLevel _impersonationLevel;
        private readonly PipeOptions _pipeOptions;
        private readonly HandleInheritability _inheritability;
        private readonly PipeDirection _direction;

        // Creates a named pipe client using default server (same machine, or "."), and PipeDirection.InOut 
        [SecuritySafeCritical]
        public NamedPipeClientStream(String pipeName)
            : this(".", pipeName, PipeDirection.InOut, PipeOptions.None, TokenImpersonationLevel.None, HandleInheritability.None)
        { 
        }

        [SecuritySafeCritical]
        public NamedPipeClientStream(String serverName, String pipeName)
            : this(serverName, pipeName, PipeDirection.InOut, PipeOptions.None, TokenImpersonationLevel.None, HandleInheritability.None)
        {
        }

        [SecuritySafeCritical]
        public NamedPipeClientStream(String serverName, String pipeName, PipeDirection direction)
            : this(serverName, pipeName, direction, PipeOptions.None, TokenImpersonationLevel.None, HandleInheritability.None)
        {
        }

        [SecuritySafeCritical]
        public NamedPipeClientStream(String serverName, String pipeName, PipeDirection direction, PipeOptions options)
            : this(serverName, pipeName, direction, options, TokenImpersonationLevel.None, HandleInheritability.None)
        {
        }

        [SecuritySafeCritical]
        public NamedPipeClientStream(String serverName, String pipeName, PipeDirection direction,
            PipeOptions options, TokenImpersonationLevel impersonationLevel)
            : this(serverName, pipeName, direction, options, impersonationLevel, HandleInheritability.None)
        {
        }

        [SecuritySafeCritical]
        internal NamedPipeClientStream(String serverName, String pipeName, PipeDirection direction,
            PipeOptions options, TokenImpersonationLevel impersonationLevel, HandleInheritability inheritability)
            : base(direction, 0)
        {
            if (pipeName == null)
            {
                throw new ArgumentNullException("pipeName");
            }
            if (serverName == null)
            {
                throw new ArgumentNullException("serverName", SR.ArgumentNull_ServerName);
            }
            if (pipeName.Length == 0)
            {
                throw new ArgumentException(SR.Argument_NeedNonemptyPipeName);
            }
            if (serverName.Length == 0)
            {
                throw new ArgumentException(SR.Argument_EmptyServerName);
            }
            if ((options & ~(PipeOptions.WriteThrough | PipeOptions.Asynchronous)) != 0)
            {
                throw new ArgumentOutOfRangeException("options", SR.ArgumentOutOfRange_OptionsInvalid);
            }
            if (impersonationLevel < TokenImpersonationLevel.None || impersonationLevel > TokenImpersonationLevel.Delegation)
            {
                throw new ArgumentOutOfRangeException("impersonationLevel", SR.ArgumentOutOfRange_ImpersonationInvalid);
            }
            if (inheritability < HandleInheritability.None || inheritability > HandleInheritability.Inheritable)
            {
                throw new ArgumentOutOfRangeException("inheritability", SR.ArgumentOutOfRange_HandleInheritabilityNoneOrInheritable);
            }

            _normalizedPipePath = GetPipePath(serverName, pipeName);
            _direction = direction;
            _inheritability = inheritability;
            _impersonationLevel = impersonationLevel;
            _pipeOptions = options;
        }

        // Create a NamedPipeClientStream from an existing server pipe handle.
        [SecuritySafeCritical]
        public NamedPipeClientStream(PipeDirection direction, bool isAsync, bool isConnected, SafePipeHandle safePipeHandle)
            : base(direction, 0)
        {
            if (safePipeHandle == null)
            {
                throw new ArgumentNullException("safePipeHandle");
            }
            if (safePipeHandle.IsInvalid)
            {
                throw new ArgumentException(SR.Argument_InvalidHandle, "safePipeHandle");
            }
            ValidateHandleIsPipe(safePipeHandle);

            InitializeHandle(safePipeHandle, true, isAsync);
            if (isConnected)
            {
                State = PipeState.Connected;
            }
        }

        ~NamedPipeClientStream()
        {
            Dispose(false);
        }

        public void Connect()
        {
            Connect(Timeout.Infinite);
        }

        public void Connect(int timeout)
        {
            CheckConnectOperationsClient();

            if (timeout < 0 && timeout != Timeout.Infinite)
            {
                throw new ArgumentOutOfRangeException("timeout", SR.ArgumentOutOfRange_InvalidTimeout);
            }

            ConnectInternal(timeout, CancellationToken.None, Environment.TickCount);
        }

        public Task ConnectAsync()
        {
            // We cannot avoid creating lambda here by using Connect method
            // unless we don't care about start time to be measured before the thread is started
            return ConnectAsync(Timeout.Infinite, CancellationToken.None);
        }

        public Task ConnectAsync(int timeout)
        {
            return ConnectAsync(timeout, CancellationToken.None);
        }

        public Task ConnectAsync(CancellationToken cancellationToken)
        {
            return ConnectAsync(Timeout.Infinite, cancellationToken);
        }

        public Task ConnectAsync(int timeout, CancellationToken cancellationToken)
        {
            CheckConnectOperationsClient();

            if (timeout < 0 && timeout != Timeout.Infinite)
            {
                throw new ArgumentOutOfRangeException("timeout", SR.ArgumentOutOfRange_InvalidTimeout);
            }

            if (cancellationToken.IsCancellationRequested)
            {
                return Task.FromCanceled(cancellationToken);
            }

            int startTime = Environment.TickCount; // We need to measure time here, not in the lambda
            return Task.Run(() => ConnectInternal(timeout, cancellationToken, startTime), cancellationToken);
        }

        // override because named pipe clients can't get/set properties when waiting to connect
        // or broken
        [SecurityCritical]
        internal override void CheckPipePropertyOperations()
        {
            base.CheckPipePropertyOperations();

            if (State == PipeState.WaitingToConnect)
            {
                throw new InvalidOperationException(SR.InvalidOperation_PipeNotYetConnected);
            }
            if (State == PipeState.Broken)
            {
                throw new IOException(SR.IO_PipeBroken);
            }
        }

        // named client is allowed to connect from broken
        private void CheckConnectOperationsClient()
        {
            if (State == PipeState.Connected)
            {
                throw new InvalidOperationException(SR.InvalidOperation_PipeAlreadyConnected);
            }
            if (State == PipeState.Closed)
            {
                throw __Error.GetPipeNotOpen();
            }
        }
    }
}
