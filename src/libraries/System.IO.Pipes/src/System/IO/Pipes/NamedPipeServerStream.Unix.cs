// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Security;
using System.Threading;
using System.Threading.Tasks;

namespace System.IO.Pipes
{
    /// <summary>
    /// Named pipe server
    /// </summary>
    public sealed partial class NamedPipeServerStream : PipeStream
    {
        private string _path;
        private PipeDirection _direction;
        private PipeOptions _options;
        private int _inBufferSize;
        private int _outBufferSize;
        private HandleInheritability _inheritability;

        [SecurityCritical]
        private void Create(string pipeName, PipeDirection direction, int maxNumberOfServerInstances,
                PipeTransmissionMode transmissionMode, PipeOptions options, int inBufferSize, int outBufferSize,
                HandleInheritability inheritability)
        {
            Debug.Assert(pipeName != null && pipeName.Length != 0, "fullPipeName is null or empty");
            Debug.Assert(direction >= PipeDirection.In && direction <= PipeDirection.InOut, "invalid pipe direction");
            Debug.Assert(inBufferSize >= 0, "inBufferSize is negative");
            Debug.Assert(outBufferSize >= 0, "outBufferSize is negative");
            Debug.Assert((maxNumberOfServerInstances >= 1) || (maxNumberOfServerInstances == MaxAllowedServerInstances), "maxNumberOfServerInstances is invalid");
            Debug.Assert(transmissionMode >= PipeTransmissionMode.Byte && transmissionMode <= PipeTransmissionMode.Message, "transmissionMode is out of range");

            if (transmissionMode == PipeTransmissionMode.Message)
            {
                throw new PlatformNotSupportedException();
            }

            // NOTE: We don't have a good way to enforce maxNumberOfServerInstances, and don't currently try.
            // It's a Windows-specific concept.

            // Make sure the FIFO exists, but don't open it until WaitForConnection is called.
            _path = GetPipePath(".", pipeName);
            int result = Interop.Sys.MkFifo(_path, (int)Interop.Sys.Permissions.S_IRWXU);

            // The FIFO was successfully created on result == 0 - note that although we create the FIFO here, we don't
            // ever delete it. If we remove the FIFO we could invalidate other servers that also use it. 
            // See #2764 for further discussion.

            if (result != 0)
            {
                Interop.ErrorInfo errorInfo = Interop.Sys.GetLastErrorInfo();
                // FIFO already exists if Error == EEXIST - nothing more to do

                if (errorInfo.Error != Interop.Error.EEXIST)
                {
                    // something else; fail
                    throw Interop.GetExceptionForIoErrno(errorInfo, _path);
                }
            }

            // Store the rest of the creation arguments.  They'll be used when we open the connection
            // in WaitForConnection.
            _direction = direction;
            _options = options;
            _inBufferSize = inBufferSize;
            _outBufferSize = outBufferSize;
            _inheritability = inheritability;
        }

        [SecurityCritical]
        [SuppressMessage("Microsoft.Security", "CA2122:DoNotIndirectlyExposeMethodsWithLinkDemands", Justification = "Security model of pipes: demand at creation but no subsequent demands")]
        public void WaitForConnection()
        {
            CheckConnectOperationsServer();
            if (State == PipeState.Connected)
            {
                throw new InvalidOperationException(SR.InvalidOperation_PipeAlreadyConnected);
            }

            // Open the file.  For In or Out, this will block until a client has connected.
            // Unfortunately for InOut it won't, which is different from the Windows behavior;
            // on Unix it won't block for InOut until it actually performs a read or write operation.
            var serverHandle = Microsoft.Win32.SafeHandles.SafePipeHandle.Open(
                _path, 
                TranslateFlags(_direction, _options, _inheritability), 
                (int)Interop.Sys.Permissions.S_IRWXU);

            InitializeBufferSize(serverHandle, _outBufferSize); // there's only one capacity on Linux; just use the out buffer size
            InitializeHandle(serverHandle, isExposed: false, isAsync: (_options & PipeOptions.Asynchronous) != 0);
            State = PipeState.Connected;
        }

        public Task WaitForConnectionAsync(CancellationToken cancellationToken)
        {
            return cancellationToken.IsCancellationRequested ?
                Task.FromCanceled(cancellationToken) :
                Task.Factory.StartNew(s => ((NamedPipeServerStream)s).WaitForConnection(),
                    this, cancellationToken, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
        }

        [SecurityCritical]
        public void Disconnect()
        {
            CheckDisconnectOperations();
            State = PipeState.Disconnected;
            InternalHandle.Dispose();
            InitializeHandle(null, false, false);
        }

        // Gets the username of the connected client.  Not that we will not have access to the client's 
        // username until it has written at least once to the pipe (and has set its impersonationLevel 
        // argument appropriately). 
        [SecurityCritical]
        public String GetImpersonationUserName()
        {
            CheckWriteOperations();
            throw new PlatformNotSupportedException();
        }

        private void ValidateMaxNumberOfServerInstances(int maxNumberOfServerInstances)
        {
            // Since Unix has no notion of Max allowed Server Instances per named pipe, we don't enforce an
            // upper bound on maxNumberOfServerInstances.
            if ((maxNumberOfServerInstances < 1) && (maxNumberOfServerInstances != MaxAllowedServerInstances))
            {
                throw new ArgumentOutOfRangeException(nameof(maxNumberOfServerInstances), SR.ArgumentOutOfRange_MaxNumServerInstances);
            }
        }

        // -----------------------------
        // ---- PAL layer ends here ----
        // -----------------------------

    }
}
