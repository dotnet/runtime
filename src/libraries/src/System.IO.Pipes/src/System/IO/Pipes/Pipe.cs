// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Win32;
using Microsoft.Win32.SafeHandles;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace System.IO.Pipes
{
    /// <summary>
    /// Anonymous pipe server stream
    /// </summary>
    public sealed class AnonymousPipeServerStream : PipeStream
    {
        private SafePipeHandle _clientHandle;
        private bool _clientHandleExposed;

        [SecuritySafeCritical]
        public AnonymousPipeServerStream()
            : this(PipeDirection.Out, HandleInheritability.None, 0)
        { }

        [SecuritySafeCritical]
        public AnonymousPipeServerStream(PipeDirection direction)
            : this(direction, HandleInheritability.None, 0)
        { }

        [SecuritySafeCritical]
        public AnonymousPipeServerStream(PipeDirection direction, HandleInheritability inheritability)
            : this(direction, inheritability, 0)
        { }

        // bufferSize is used as a suggestion; specify 0 to let OS decide
        // This constructor instantiates the PipeSecurity using just the inheritability flag
        [SecuritySafeCritical]
        public AnonymousPipeServerStream(PipeDirection direction, HandleInheritability inheritability, int bufferSize)
            : base(direction, bufferSize)
        {
            if (direction == PipeDirection.InOut)
            {
                throw new NotSupportedException(SR.NotSupported_AnonymousPipeUnidirectional);
            }
            if (inheritability < HandleInheritability.None || inheritability > HandleInheritability.Inheritable)
            {
                throw new ArgumentOutOfRangeException("inheritability", SR.ArgumentOutOfRange_HandleInheritabilityNoneOrInheritable);
            }

            Interop.SECURITY_ATTRIBUTES secAttrs = PipeStream.GetSecAttrs(inheritability);
            Create(direction, secAttrs, bufferSize);
        }

        ~AnonymousPipeServerStream()
        {
            Dispose(false);
        }

        // Create an AnonymousPipeServerStream from two existing pipe handles.
        [SecuritySafeCritical]
        public AnonymousPipeServerStream(PipeDirection direction, SafePipeHandle serverSafePipeHandle, SafePipeHandle clientSafePipeHandle)
            : base(direction, 0)
        {
            if (direction == PipeDirection.InOut)
            {
                throw new NotSupportedException(SR.NotSupported_AnonymousPipeUnidirectional);
            }
            if (serverSafePipeHandle == null)
            {
                throw new ArgumentNullException("serverSafePipeHandle");
            }
            if (clientSafePipeHandle == null)
            {
                throw new ArgumentNullException("clientSafePipeHandle");
            }
            if (serverSafePipeHandle.IsInvalid)
            {
                throw new ArgumentException(SR.Argument_InvalidHandle, "serverSafePipeHandle");
            }
            if (clientSafePipeHandle.IsInvalid)
            {
                throw new ArgumentException(SR.Argument_InvalidHandle, "clientSafePipeHandle");
            }

            // Check that these handles are in fact a handles to a pipe.
            if (Interop.mincore.GetFileType(serverSafePipeHandle) != Interop.FILE_TYPE_PIPE)
            {
                throw new IOException(SR.IO_InvalidPipeHandle);
            }
            if (Interop.mincore.GetFileType(clientSafePipeHandle) != Interop.FILE_TYPE_PIPE)
            {
                throw new IOException(SR.IO_InvalidPipeHandle);
            }

            InitializeHandle(serverSafePipeHandle, true, false);

            _clientHandle = clientSafePipeHandle;
            _clientHandleExposed = true;
            State = PipeState.Connected;
        }

        // This method should exist until we add a first class way of passing handles between parent and child
        // processes. For now, people do it via command line arguments. 
        [System.Security.SecurityCritical]
        [SuppressMessage("Microsoft.Reliability", "CA2001:AvoidCallingProblematicMethods", MessageId = "System.Runtime.InteropServices.SafeHandle.DangerousGetHandle", Justification = "By design")]
        public String GetClientHandleAsString()
        {
            _clientHandleExposed = true;
            return _clientHandle.DangerousGetHandle().ToString();
        }

        public SafePipeHandle ClientSafePipeHandle
        {
            [System.Security.SecurityCritical]
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
        [System.Security.SecurityCritical]
        public void DisposeLocalCopyOfClientHandle()
        {
            if (_clientHandle != null && !_clientHandle.IsClosed)
            {
                _clientHandle.Dispose();
            }
        }

        [System.Security.SecurityCritical]
        protected override void Dispose(bool disposing)
        {
            try
            {
                // We should dispose of the client handle if it was not exposed. 
                if (!_clientHandleExposed && _clientHandle != null && !_clientHandle.IsClosed)
                {
                    _clientHandle.Dispose();
                }
            }
            finally
            {
                base.Dispose(disposing);
            }
        }

        // Creates the anonymous pipe.
        [System.Security.SecurityCritical]
        private void Create(PipeDirection direction, Interop.SECURITY_ATTRIBUTES secAttrs, int bufferSize)
        {
            Debug.Assert(direction != PipeDirection.InOut, "Anonymous pipe direction shouldn't be InOut");
            Debug.Assert(bufferSize >= 0, "bufferSize is negative");

            bool bSuccess;
            SafePipeHandle serverHandle;
            SafePipeHandle newServerHandle;

            // Create the two pipe handles that make up the anonymous pipe.
            if (direction == PipeDirection.In)
            {
                bSuccess = Interop.mincore.CreatePipe(out serverHandle, out _clientHandle, ref secAttrs, bufferSize);
            }
            else
            {
                bSuccess = Interop.mincore.CreatePipe(out _clientHandle, out serverHandle, ref secAttrs, bufferSize);
            }

            if (!bSuccess)
            {
                throw Win32Marshal.GetExceptionForLastWin32Error();
            }

            // Duplicate the server handle to make it not inheritable.  Note: We need to do this so that the child 
            // process doesn't end up getting another copy of the server handle.  If it were to get a copy, the
            // OS wouldn't be able to inform the child that the server has closed its handle because it will see
            // that there is still one server handle that is open.  
            bSuccess = Interop.mincore.DuplicateHandle(Interop.mincore.GetCurrentProcess(), serverHandle, Interop.mincore.GetCurrentProcess(),
                    out newServerHandle, 0, false, Interop.DUPLICATE_SAME_ACCESS);

            if (!bSuccess)
            {
                throw Win32Marshal.GetExceptionForLastWin32Error();
            }

            // Close the inheritable server handle.
            serverHandle.Dispose();

            InitializeHandle(newServerHandle, false, false);

            State = PipeState.Connected;
        }

        // Anonymous pipes do not support message mode so there is no need to use the base version that P/Invokes here.
        public override PipeTransmissionMode TransmissionMode
        {
            [System.Security.SecurityCritical]
            get
            {
                return PipeTransmissionMode.Byte;
            }
        }

        public override PipeTransmissionMode ReadMode
        {
            [System.Security.SecurityCritical]
            set
            {
                CheckPipePropertyOperations();

                if (value < PipeTransmissionMode.Byte || value > PipeTransmissionMode.Message)
                {
                    throw new ArgumentOutOfRangeException("value", SR.ArgumentOutOfRange_TransmissionModeByteOrMsg);
                }

                if (value == PipeTransmissionMode.Message)
                {
                    throw new NotSupportedException(SR.NotSupported_AnonymousPipeMessagesNotSupported);
                }
            }
        }
    }


    /// <summary>
    /// Anonymous pipe client. Use this to open the client end of an anonymous pipes created with AnonymousPipeServerStream.
    /// </summary>
    public sealed class AnonymousPipeClientStream : PipeStream
    {
        [SecuritySafeCritical]
        [SuppressMessage("Microsoft.Naming", "CA1720:IdentifiersShouldNotContainTypeNames", MessageId = "string", Justification = "By design")]
        public AnonymousPipeClientStream(String pipeHandleAsString)
            : this(PipeDirection.In, pipeHandleAsString)
        { }

        [System.Security.SecurityCritical]
        [SuppressMessage("Microsoft.Naming", "CA1720:IdentifiersShouldNotContainTypeNames", MessageId = "string", Justification = "By design")]
        public AnonymousPipeClientStream(PipeDirection direction, String pipeHandleAsString)
            : base(direction, 0)
        {
            if (direction == PipeDirection.InOut)
            {
                throw new NotSupportedException(SR.NotSupported_AnonymousPipeUnidirectional);
            }
            if (pipeHandleAsString == null)
            {
                throw new ArgumentNullException("pipeHandleAsString");
            }

            // Initialize SafePipeHandle from String and check if it's valid. First see if it's parseable
            long result = 0;
            bool parseable = long.TryParse(pipeHandleAsString, out result);
            if (!parseable)
            {
                throw new ArgumentException(SR.Argument_InvalidHandle, "pipeHandleAsString");
            }

            // next check whether the handle is invalid
            SafePipeHandle safePipeHandle = new SafePipeHandle((IntPtr)result, true);
            if (safePipeHandle.IsInvalid)
            {
                throw new ArgumentException(SR.Argument_InvalidHandle, "pipeHandleAsString");
            }

            Init(direction, safePipeHandle);
        }

        [System.Security.SecurityCritical]
        public AnonymousPipeClientStream(PipeDirection direction, SafePipeHandle safePipeHandle)
            : base(direction, 0)
        {
            if (direction == PipeDirection.InOut)
            {
                throw new NotSupportedException(SR.NotSupported_AnonymousPipeUnidirectional);
            }
            if (safePipeHandle == null)
            {
                throw new ArgumentNullException("safePipeHandle");
            }
            if (safePipeHandle.IsInvalid)
            {
                throw new ArgumentException(SR.Argument_InvalidHandle, "safePipeHandle");
            }

            Init(direction, safePipeHandle);
        }

        [System.Security.SecuritySafeCritical]
        private void Init(PipeDirection direction, SafePipeHandle safePipeHandle)
        {
            Debug.Assert(direction != PipeDirection.InOut, "anonymous pipes are unidirectional, caller should have verified before calling Init");
            Debug.Assert(safePipeHandle != null && !safePipeHandle.IsInvalid, "safePipeHandle must be valid");

            // Check that this handle is infact a handle to a pipe.
            if (Interop.mincore.GetFileType(safePipeHandle) != Interop.FILE_TYPE_PIPE)
            {
                throw new IOException(SR.IO_InvalidPipeHandle);
            }

            InitializeHandle(safePipeHandle, true, false);
            State = PipeState.Connected;
        }


        ~AnonymousPipeClientStream()
        {
            Dispose(false);
        }

        // Anonymous pipes do not support message readmode so there is no need to use the base version
        // which P/Invokes (and sometimes fails).
        public override PipeTransmissionMode TransmissionMode
        {
            [System.Security.SecurityCritical]
            get
            {
                return PipeTransmissionMode.Byte;
            }
        }

        public override PipeTransmissionMode ReadMode
        {
            [System.Security.SecurityCritical]
            set
            {
                CheckPipePropertyOperations();

                if (value < PipeTransmissionMode.Byte || value > PipeTransmissionMode.Message)
                {
                    throw new ArgumentOutOfRangeException("value", SR.ArgumentOutOfRange_TransmissionModeByteOrMsg);
                }
                if (value == PipeTransmissionMode.Message)
                {
                    throw new NotSupportedException(SR.NotSupported_AnonymousPipeMessagesNotSupported);
                }
            }
        }
    }
#if RunAs
    // Users will use this delegate to specify a method to call while impersonating the client 
    // (see NamedPipeServerStream.RunAsClient).
    public delegate void PipeStreamImpersonationWorker();
#endif
    /// <summary>
    /// Named pipe server
    /// </summary>
    public sealed class NamedPipeServerStream : PipeStream
    {
        // Use the maximum number of server instances that the system resources allow
        private const int MaxAllowedServerInstances = -1;

        [SecurityCritical]
        private unsafe static readonly IOCompletionCallback s_WaitForConnectionCallback =
            new IOCompletionCallback(NamedPipeServerStream.AsyncWaitForConnectionCallback);

        [System.Security.SecurityCritical]
        static NamedPipeServerStream()
        {
        }

        [SecuritySafeCritical]
        public NamedPipeServerStream(String pipeName)
            : this(pipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.None, 0, 0,
            HandleInheritability.None)
        { }

        [SecuritySafeCritical]
        public NamedPipeServerStream(String pipeName, PipeDirection direction)
            : this(pipeName, direction, 1, PipeTransmissionMode.Byte, PipeOptions.None, 0, 0,
            HandleInheritability.None)
        { }

        [SecuritySafeCritical]
        public NamedPipeServerStream(String pipeName, PipeDirection direction, int maxNumberOfServerInstances)
            : this(pipeName, direction, maxNumberOfServerInstances, PipeTransmissionMode.Byte, PipeOptions.None,
            0, 0, HandleInheritability.None)
        { }

        [SecuritySafeCritical]
        public NamedPipeServerStream(String pipeName, PipeDirection direction, int maxNumberOfServerInstances,
                PipeTransmissionMode transmissionMode)
            : this(pipeName, direction, maxNumberOfServerInstances, transmissionMode, PipeOptions.None, 0, 0,
                HandleInheritability.None)
        { }

        [SecuritySafeCritical]
        public NamedPipeServerStream(String pipeName, PipeDirection direction, int maxNumberOfServerInstances,
                PipeTransmissionMode transmissionMode, PipeOptions options)
            : this(pipeName, direction, maxNumberOfServerInstances, transmissionMode, options, 0, 0,
                HandleInheritability.None)
        { }

        [SecuritySafeCritical]
        public NamedPipeServerStream(String pipeName, PipeDirection direction, int maxNumberOfServerInstances,
                PipeTransmissionMode transmissionMode, PipeOptions options, int inBufferSize, int outBufferSize)
            : this(pipeName, direction, maxNumberOfServerInstances, transmissionMode, options, inBufferSize, outBufferSize,
                HandleInheritability.None)
        { }

        /// <summary>
        /// Full named pipe server constructor
        /// </summary>
        /// <param name="pipeName">Pipe name</param>
        /// <param name="direction">Pipe direction: In, Out or InOut (duplex). 
        /// Win32 note: this gets OR'd into dwOpenMode to CreateNamedPipe
        /// </param>
        /// <param name="maxNumberOfServerInstances">Maximum number of server instances. Specify a fixed value between 
        /// 1 and 254, or use NamedPipeServerStream.MaxAllowedServerInstances to use the maximum amount allowed by 
        /// system resources.</param>
        /// <param name="transmissionMode">Byte mode or message mode.
        /// Win32 note: this gets used for dwPipeMode. CreateNamedPipe allows you to specify PIPE_TYPE_BYTE/MESSAGE
        /// and PIPE_READMODE_BYTE/MESSAGE independently, but this sets type and readmode to match.
        /// </param>
        /// <param name="options">PipeOption enum: None, Asynchronous, or Writethrough
        /// Win32 note: this gets passed in with dwOpenMode to CreateNamedPipe. Asynchronous corresponds to 
        /// FILE_FLAG_OVERLAPPED option. PipeOptions enum doesn't expose FIRST_PIPE_INSTANCE option because
        /// this sets that automatically based on the number of instances specified.
        /// </param>
        /// <param name="inBufferSize">Incoming buffer size, 0 or higher.
        /// Note: this size is always advisory; OS uses a suggestion.
        /// </param>
        /// <param name="outBufferSize">Outgoing buffer size, 0 or higher (see above)</param>
        /// <param name="pipeSecurity">PipeSecurity, or null for default security descriptor</param>
        /// <param name="inheritability">Whether handle is inheritable</param>
        /// <param name="additionalAccessRights">Combination (logical OR) of PipeAccessRights.TakeOwnership, 
        /// PipeAccessRights.AccessSystemSecurity, and PipeAccessRights.ChangePermissions</param>
        [SecuritySafeCritical]
        private NamedPipeServerStream(String pipeName, PipeDirection direction, int maxNumberOfServerInstances,
                PipeTransmissionMode transmissionMode, PipeOptions options, int inBufferSize, int outBufferSize,
                HandleInheritability inheritability)
            : base(direction, transmissionMode, outBufferSize)
        {
            if (pipeName == null)
            {
                throw new ArgumentNullException("pipeName");
            }
            if (pipeName.Length == 0)
            {
                throw new ArgumentException(SR.Argument_NeedNonemptyPipeName);
            }
            if ((options & ~(PipeOptions.WriteThrough | PipeOptions.Asynchronous)) != 0)
            {
                throw new ArgumentOutOfRangeException("options", SR.ArgumentOutOfRange_OptionsInvalid);
            }
            if (inBufferSize < 0)
            {
                throw new ArgumentOutOfRangeException("inBufferSize", SR.ArgumentOutOfRange_NeedNonNegNum);
            }
            // win32 allows fixed values of 1-254 or 255 to mean max allowed by system. We expose 255 as -1 (unlimited)
            // through the MaxAllowedServerInstances constant. This is consistent e.g. with -1 as infinite timeout, etc
            if ((maxNumberOfServerInstances < 1 || maxNumberOfServerInstances > 254) && (maxNumberOfServerInstances != MaxAllowedServerInstances))
            {
                throw new ArgumentOutOfRangeException("maxNumberOfServerInstances", SR.ArgumentOutOfRange_MaxNumServerInstances);
            }
            if (inheritability < HandleInheritability.None || inheritability > HandleInheritability.Inheritable)
            {
                throw new ArgumentOutOfRangeException("inheritability", SR.ArgumentOutOfRange_HandleInheritabilityNoneOrInheritable);
            }

            string normalizedPipePath = Path.GetFullPath(@"\\.\pipe\" + pipeName);

            // Make sure the pipe name isn't one of our reserved names for anonymous pipes.
            if (String.Compare(normalizedPipePath, @"\\.\pipe\anonymous", StringComparison.OrdinalIgnoreCase) == 0)
            {
                throw new ArgumentOutOfRangeException("pipeName", SR.ArgumentOutOfRange_AnonymousReserved);
            }

            Interop.SECURITY_ATTRIBUTES secAttrs = PipeStream.GetSecAttrs(inheritability);

            Create(normalizedPipePath, direction, maxNumberOfServerInstances, transmissionMode,
                    options, inBufferSize, outBufferSize, secAttrs);
        }

        // Create a NamedPipeServerStream from an existing server pipe handle.
        [System.Security.SecuritySafeCritical]
        public NamedPipeServerStream(PipeDirection direction, bool isAsync, bool isConnected, SafePipeHandle safePipeHandle)
            : base(direction, PipeTransmissionMode.Byte, 0)
        {
            if (safePipeHandle == null)
            {
                throw new ArgumentNullException("safePipeHandle");
            }
            if (safePipeHandle.IsInvalid)
            {
                throw new ArgumentException(SR.Argument_InvalidHandle, "safePipeHandle");
            }
            // Check that this handle is infact a handle to a pipe.
            if (Interop.mincore.GetFileType(safePipeHandle) != Interop.FILE_TYPE_PIPE)
            {
                throw new IOException(SR.IO_InvalidPipeHandle);
            }

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

        [System.Security.SecurityCritical]
        private void Create(String fullPipeName, PipeDirection direction, int maxNumberOfServerInstances,
                PipeTransmissionMode transmissionMode, PipeOptions options, int inBufferSize, int outBufferSize,
                Interop.SECURITY_ATTRIBUTES secAttrs)
        {
            Debug.Assert(fullPipeName != null && fullPipeName.Length != 0, "fullPipeName is null or empty");
            Debug.Assert(direction >= PipeDirection.In && direction <= PipeDirection.InOut, "invalid pipe direction");
            Debug.Assert(inBufferSize >= 0, "inBufferSize is negative");
            Debug.Assert(outBufferSize >= 0, "outBufferSize is negative");
            Debug.Assert((maxNumberOfServerInstances >= 1 && maxNumberOfServerInstances <= 254) || (maxNumberOfServerInstances == MaxAllowedServerInstances), "maxNumberOfServerInstances is invalid");
            Debug.Assert(transmissionMode >= PipeTransmissionMode.Byte && transmissionMode <= PipeTransmissionMode.Message, "transmissionMode is out of range");

            int openMode = ((int)direction) |
                           (maxNumberOfServerInstances == 1 ? Interop.FILE_FLAG_FIRST_PIPE_INSTANCE : 0) |
                           (int)options;

            // We automatically set the ReadMode to match the TransmissionMode.
            int pipeModes = (int)transmissionMode << 2 | (int)transmissionMode << 1;

            // Convert -1 to 255 to match win32 (we asserted that it is between -1 and 254).
            if (maxNumberOfServerInstances == MaxAllowedServerInstances)
            {
                maxNumberOfServerInstances = 255;
            }

            SafePipeHandle handle = Interop.mincore.CreateNamedPipe(fullPipeName, openMode, pipeModes,
                    maxNumberOfServerInstances, outBufferSize, inBufferSize, 0, ref secAttrs);

            if (handle.IsInvalid)
            {
                throw Win32Marshal.GetExceptionForLastWin32Error();
            }

            InitializeHandle(handle, false, (options & PipeOptions.Asynchronous) != 0);
        }

        // This will wait until the client calls Connect().  If we return from this method, we guarantee that
        // the client has returned from its Connect call.   The client may have done so before this method 
        // was called (but not before this server is been created, or, if we were servicing another client, 
        // not before we called Disconnect), in which case, there may be some buffer already in the pipe waiting
        // for us to read.  See NamedPipeClientStream.Connect for more information.
        [System.Security.SecurityCritical]
        [SuppressMessage("Microsoft.Security", "CA2122:DoNotIndirectlyExposeMethodsWithLinkDemands", Justification = "Security model of pipes: demand at creation but no subsequent demands")]
        public void WaitForConnection()
        {
            CheckConnectOperationsServer();

            if (IsAsync)
            {
                IAsyncResult result = BeginWaitForConnection(null, null);
                EndWaitForConnection(result);
            }
            else
            {
                if (!Interop.mincore.ConnectNamedPipe(InternalHandle, Interop.NULL))
                {
                    int errorCode = Marshal.GetLastWin32Error();

                    if (errorCode != Interop.ERROR_PIPE_CONNECTED)
                    {
                        throw Win32Marshal.GetExceptionForWin32Error(errorCode);
                    }

                    // pipe already connected
                    if (errorCode == Interop.ERROR_PIPE_CONNECTED && State == PipeState.Connected)
                    {
                        throw new InvalidOperationException(SR.InvalidOperation_PipeAlreadyConnected);
                    }
                    // If we reach here then a connection has been established.  This can happen if a client 
                    // connects in the interval between the call to CreateNamedPipe and the call to ConnectNamedPipe. 
                    // In this situation, there is still a good connection between client and server, even though 
                    // ConnectNamedPipe returns zero.
                }
                State = PipeState.Connected;
            }
        }

        public Task WaitForConnectionAsync(CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return TaskHelpers.FromCancellation(cancellationToken);
            }

            if (!IsAsync)
            {
                return Task.Factory.StartNew(WaitForConnection, cancellationToken);
            }

            // Avoiding allocation if the task cannot be cancelled
            IOCancellationHelper cancellationHelper = cancellationToken.CanBeCanceled ? new IOCancellationHelper(cancellationToken) : null;
            return Task.Factory.FromAsync(BeginWaitForConnection, EndWaitForConnection, cancellationHelper);
        }

        public Task WaitForConnectionAsync()
        {
            return WaitForConnectionAsync(CancellationToken.None);
        }

        // Async version of WaitForConnection.  See the comments above for more info.
        [System.Security.SecurityCritical]
        private unsafe IAsyncResult BeginWaitForConnection(AsyncCallback callback, Object state)
        {
            CheckConnectOperationsServer();

            if (!IsAsync)
            {
                throw new InvalidOperationException(SR.InvalidOperation_PipeNotAsync);
            }

            // Create and store async stream class library specific data in the 
            // async result
            PipeAsyncResult asyncResult = new PipeAsyncResult();
            asyncResult._handle = InternalHandle;
            asyncResult._userCallback = callback;
            asyncResult._userStateObject = state;

            IOCancellationHelper cancellationHelper = state as IOCancellationHelper;

            // Create wait handle and store in async result
            ManualResetEvent waitHandle = new ManualResetEvent(false);
            asyncResult._waitHandle = waitHandle;

            // Create a managed overlapped class
            // We will set the file offsets later
            Overlapped overlapped = new Overlapped();
            overlapped.OffsetLow = 0;
            overlapped.OffsetHigh = 0;
            overlapped.AsyncResult = asyncResult;

            // Pack the Overlapped class, and store it in the async result
            NativeOverlapped* intOverlapped = overlapped.Pack(s_WaitForConnectionCallback, null);
            asyncResult._overlapped = intOverlapped;
            if (!Interop.mincore.ConnectNamedPipe(InternalHandle, intOverlapped))
            {
                int errorCode = Marshal.GetLastWin32Error();

                if (errorCode == Interop.ERROR_IO_PENDING)
                {
                    if (cancellationHelper != null)
                    {
                        cancellationHelper.AllowCancellation(InternalHandle, intOverlapped);
                    }
                    return asyncResult;
                }

                // WaitForConnectionCallback will not be called becasue we completed synchronously.
                // Either the pipe is already connected, or there was an error. Unpin and free the overlapped again.
                Overlapped.Free(intOverlapped);
                asyncResult._overlapped = null;

                // Did the client already connect to us?
                if (errorCode == Interop.ERROR_PIPE_CONNECTED)
                {
                    if (State == PipeState.Connected)
                    {
                        throw new InvalidOperationException(SR.InvalidOperation_PipeAlreadyConnected);
                    }
                    asyncResult.CallUserCallback();
                    return asyncResult;
                }

                throw Win32Marshal.GetExceptionForWin32Error(errorCode);
            }
            // will set state to Connected when EndWait is called
            if (cancellationHelper != null)
            {
                cancellationHelper.AllowCancellation(InternalHandle, intOverlapped);
            }

            return asyncResult;
        }

        // Async version of WaitForConnection.  See comments for WaitForConnection for more info.
        [System.Security.SecurityCritical]
        private unsafe void EndWaitForConnection(IAsyncResult asyncResult)
        {
            CheckConnectOperationsServer();

            if (asyncResult == null)
            {
                throw new ArgumentNullException("asyncResult");
            }
            if (!IsAsync)
            {
                throw new InvalidOperationException(SR.InvalidOperation_PipeNotAsync);
            }

            PipeAsyncResult afsar = asyncResult as PipeAsyncResult;
            if (afsar == null)
            {
                throw __Error.GetWrongAsyncResult();
            }

            // Ensure we can't get into any races by doing an interlocked
            // CompareExchange here.  Avoids corrupting memory via freeing the
            // NativeOverlapped class or GCHandle twice.  -- 
            if (1 == Interlocked.CompareExchange(ref afsar._EndXxxCalled, 1, 0))
            {
                throw __Error.GetEndWaitForConnectionCalledTwice();
            }

            IOCancellationHelper cancellationHelper = afsar.AsyncState as IOCancellationHelper;
            if (cancellationHelper != null)
            {
                cancellationHelper.SetOperationCompleted();
            }

            // Obtain the WaitHandle, but don't use public property in case we
            // delay initialize the manual reset event in the future.
            WaitHandle wh = afsar._waitHandle;
            if (wh != null)
            {
                // We must block to ensure that ConnectionIOCallback has completed,
                // and we should close the WaitHandle in here.  AsyncFSCallback
                // and the hand-ported imitation version in COMThreadPool.cpp 
                // are the only places that set this event.
                using (wh)
                {
                    wh.WaitOne();
                    Debug.Assert(afsar._isComplete == true, "NamedPipeServerStream::EndWaitForConnection - AsyncFSCallback didn't set _isComplete to true!");
                }
            }

            // We should have freed the overlapped and set it to null either in the Begin
            // method (if ConnectNamedPipe completed synchronously) or in AsyncWaitForConnectionCallback.
            // If it is not nulled out, we should not be past the above wait:
            Debug.Assert(afsar._overlapped == null);

            // Now check for any error during the read.
            if (afsar._errorCode != 0)
            {
                if (afsar._errorCode == Interop.ERROR_OPERATION_ABORTED)
                {
                    if (cancellationHelper != null)
                    {
                        cancellationHelper.ThrowIOOperationAborted();
                    }
                }
                throw Win32Marshal.GetExceptionForWin32Error(afsar._errorCode);
            }

            // Success
            State = PipeState.Connected;
        }

        [System.Security.SecurityCritical]
        public void Disconnect()
        {
            CheckDisconnectOperations();

            // Disconnect the pipe.
            if (!Interop.mincore.DisconnectNamedPipe(InternalHandle))
            {
                throw Win32Marshal.GetExceptionForLastWin32Error();
            }

            State = PipeState.Disconnected;
        }
#if RunAs
        // This method calls a delegate while impersonating the client. Note that we will not have
        // access to the client's security token until it has written at least once to the pipe 
        // (and has set its impersonationLevel argument appropriately). 
        [System.Security.SecurityCritical]
        [SecurityPermissionAttribute(SecurityAction.Demand, Flags = SecurityPermissionFlag.ControlPrincipal)]
        public void RunAsClient(PipeStreamImpersonationWorker impersonationWorker)
        {
            CheckWriteOperations();
            ExecuteHelper execHelper = new ExecuteHelper(impersonationWorker, InternalHandle);
            RuntimeHelpers.ExecuteCodeWithGuaranteedCleanup(tryCode, cleanupCode, execHelper);

            // now handle win32 impersonate/revert specific errors by throwing corresponding exceptions
            if (execHelper.m_impersonateErrorCode != 0)
            {
                WinIOError(execHelper.m_impersonateErrorCode);
            }
            else if (execHelper.m_revertImpersonateErrorCode != 0)
            {
                WinIOError(execHelper.m_revertImpersonateErrorCode);
            }
        }

        // the following are needed for CER

        private static RuntimeHelpers.TryCode tryCode = new RuntimeHelpers.TryCode(ImpersonateAndTryCode);
        private static RuntimeHelpers.CleanupCode cleanupCode = new RuntimeHelpers.CleanupCode(RevertImpersonationOnBackout);

        [System.Security.SecurityCritical]
        private static void ImpersonateAndTryCode(Object helper)
        {
            ExecuteHelper execHelper = (ExecuteHelper)helper;

            RuntimeHelpers.PrepareConstrainedRegions();
            try { }
            finally
            {
                if (UnsafeNativeMethods.ImpersonateNamedPipeClient(execHelper.m_handle))
                {
                    execHelper.m_mustRevert = true;
                }
                else
                {
                    execHelper.m_impersonateErrorCode = Marshal.GetLastWin32Error();
                }

            }

            if (execHelper.m_mustRevert)
            { // impersonate passed so run user code
                execHelper.m_userCode();
            }
        }

        [System.Security.SecurityCritical]
        [PrePrepareMethod]
        private static void RevertImpersonationOnBackout(Object helper, bool exceptionThrown)
        {
            ExecuteHelper execHelper = (ExecuteHelper)helper;

            if (execHelper.m_mustRevert)
            {
                if (!UnsafeNativeMethods.RevertToSelf())
                {
                    execHelper.m_revertImpersonateErrorCode = Marshal.GetLastWin32Error();
                }
            }
        }

        internal class ExecuteHelper
        {
            internal PipeStreamImpersonationWorker m_userCode;
            internal SafePipeHandle m_handle;
            internal bool m_mustRevert;
            internal int m_impersonateErrorCode;
            internal int m_revertImpersonateErrorCode;

            [System.Security.SecurityCritical]
            internal ExecuteHelper(PipeStreamImpersonationWorker userCode, SafePipeHandle handle)
            {
                m_userCode = userCode;
                m_handle = handle;
            }
        }
#endif
        // Gets the username of the connected client.  Not that we will not have access to the client's 
        // username until it has written at least once to the pipe (and has set its impersonationLevel 
        // argument appropriately). 
        [System.Security.SecurityCritical]
        public String GetImpersonationUserName()
        {
            CheckWriteOperations();

            StringBuilder userName = new StringBuilder(Interop.CREDUI_MAX_USERNAME_LENGTH + 1);

            if (!Interop.mincore.GetNamedPipeHandleState(InternalHandle, Interop.NULL, Interop.NULL,
                Interop.NULL, Interop.NULL, userName, userName.Capacity))
            {
                WinIOError(Marshal.GetLastWin32Error());
            }

            return userName.ToString();
        }

        // Callback to be called by the OS when completing the async WaitForConnection operation.
        [System.Security.SecurityCritical]
        unsafe private static void AsyncWaitForConnectionCallback(uint errorCode, uint numBytes, NativeOverlapped* pOverlapped)
        {
            // Unpack overlapped
            Overlapped overlapped = Overlapped.Unpack(pOverlapped);

            // Extract async result from overlapped 
            PipeAsyncResult asyncResult = (PipeAsyncResult)overlapped.AsyncResult;

            // Free the pinned overlapped:    
            Debug.Assert(asyncResult._overlapped == pOverlapped);
            Overlapped.Free(pOverlapped);
            asyncResult._overlapped = null;

            // Special case for when the client has already connected to us.
            if (errorCode == Interop.ERROR_PIPE_CONNECTED)
            {
                errorCode = 0;
            }

            asyncResult._errorCode = (int)errorCode;

            // Call the user-provided callback.  It can and often should
            // call EndWaitForConnection.  There's no reason to use an async 
            // delegate here - we're already on a threadpool thread.  
            // IAsyncResult's completedSynchronously property must return
            // false here, saying the user callback was called on another thread.
            asyncResult._completedSynchronously = false;
            asyncResult._isComplete = true;

            // The OS does not signal this event.  We must do it ourselves.
            ManualResetEvent wh = asyncResult._waitHandle;
            if (wh != null)
            {
                Debug.Assert(!wh.GetSafeWaitHandle().IsClosed, "ManualResetEvent already closed!");
                bool r = wh.Set();
                Debug.Assert(r, "ManualResetEvent::Set failed!");
                if (!r)
                {
                    throw Win32Marshal.GetExceptionForLastWin32Error();
                }
            }

            AsyncCallback userCallback = asyncResult._userCallback;

            if (userCallback != null)
            {
                userCallback(asyncResult);
            }
        }

        // Server can only connect from Disconnected state
        [System.Security.SecurityCritical]
        [SuppressMessage("Microsoft.Security", "CA2122:DoNotIndirectlyExposeMethodsWithLinkDemands", Justification = "Consistent with security model")]
        private void CheckConnectOperationsServer()
        {
            // we're not checking whether already connected; this allows us to throw IOException
            // "pipe is being closed" if other side is closing (as does win32) or no-op if
            // already connected

            if (InternalHandle == null)
            {
                throw new InvalidOperationException(SR.InvalidOperation_PipeHandleNotSet);
            }
            // object disposed
            if (State == PipeState.Closed)
            {
                throw __Error.GetPipeNotOpen();
            }
            if (InternalHandle.IsClosed)
            {
                throw __Error.GetPipeNotOpen();
            }
            // IOException
            if (State == PipeState.Broken)
            {
                throw new IOException(SR.IO_PipeBroken);
            }
        }

        // Server is allowed to disconnect from connected and broken states
        [System.Security.SecurityCritical]
        private void CheckDisconnectOperations()
        {
            // invalid operation
            if (State == PipeState.WaitingToConnect)
            {
                throw new InvalidOperationException(SR.InvalidOperation_PipeNotYetConnected);
            }
            if (State == PipeState.Disconnected)
            {
                throw new InvalidOperationException(SR.InvalidOperation_PipeAlreadyDisconnected);
            }
            if (InternalHandle == null)
            {
                throw new InvalidOperationException(SR.InvalidOperation_PipeHandleNotSet);
            }
            // object disposed
            if (State == PipeState.Closed)
            {
                throw __Error.GetPipeNotOpen();
            }
            if (InternalHandle.IsClosed)
            {
                throw __Error.GetPipeNotOpen();
            }
        }
    }

    // Named pipe client. Use this to open the client end of a named pipes created with 
    // NamedPipeServerStream.
    public sealed class NamedPipeClientStream : PipeStream
    {
        // Maximum interval in miliseconds between which cancellation is checked.
        // Used by ConnectInternal. 50ms is fairly responsive time but really long time for processor.
        private const int CancellationCheckInterval = 50;
        private string _normalizedPipePath;
        private TokenImpersonationLevel _impersonationLevel;
        private PipeOptions _pipeOptions;
        private HandleInheritability _inheritability;
        private int _access;

        // Creates a named pipe client using default server (same machine, or "."), and PipeDirection.InOut 
        [SecuritySafeCritical]
        public NamedPipeClientStream(String pipeName)
            : this(".", pipeName, PipeDirection.InOut, PipeOptions.None, TokenImpersonationLevel.None, HandleInheritability.None)
        { }

        [SecuritySafeCritical]
        public NamedPipeClientStream(String serverName, String pipeName)
            : this(serverName, pipeName, PipeDirection.InOut, PipeOptions.None, TokenImpersonationLevel.None, HandleInheritability.None)
        { }

        [SecuritySafeCritical]
        public NamedPipeClientStream(String serverName, String pipeName, PipeDirection direction)
            : this(serverName, pipeName, direction, PipeOptions.None, TokenImpersonationLevel.None, HandleInheritability.None)
        { }

        [SecuritySafeCritical]
        public NamedPipeClientStream(String serverName, String pipeName, PipeDirection direction,
               PipeOptions options)
            : this(serverName, pipeName, direction, options, TokenImpersonationLevel.None, HandleInheritability.None)
        { }

        [SecuritySafeCritical]
        public NamedPipeClientStream(String serverName, String pipeName, PipeDirection direction,
               PipeOptions options, TokenImpersonationLevel impersonationLevel)
            : this(serverName, pipeName, direction, options, impersonationLevel, HandleInheritability.None)
        { }

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

            _normalizedPipePath = Path.GetFullPath(@"\\" + serverName + @"\pipe\" + pipeName);

            if (String.Compare(_normalizedPipePath, @"\\.\pipe\anonymous", StringComparison.OrdinalIgnoreCase) == 0)
            {
                throw new ArgumentOutOfRangeException("pipeName", SR.ArgumentOutOfRange_AnonymousReserved);
            }

            _inheritability = inheritability;
            _impersonationLevel = impersonationLevel;
            _pipeOptions = options;

            if ((PipeDirection.In & direction) != 0)
            {
                _access |= Interop.GENERIC_READ;
            }
            if ((PipeDirection.Out & direction) != 0)
            {
                _access |= Interop.GENERIC_WRITE;
            }
        }

        // Create a NamedPipeClientStream from an existing server pipe handle.
        [System.Security.SecuritySafeCritical]
        public NamedPipeClientStream(PipeDirection direction, bool isAsync, bool isConnected,
                SafePipeHandle safePipeHandle)
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
            // Check that this handle is infact a handle to a pipe.
            if (Interop.mincore.GetFileType(safePipeHandle) != Interop.FILE_TYPE_PIPE)
            {
                throw new IOException(SR.IO_InvalidPipeHandle);
            }

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
                return TaskHelpers.FromCancellation(cancellationToken);
            }

            // We need to measure time here, not in the lambda
            int startTime = Environment.TickCount;
            return Task.Factory.StartNew(() => ConnectInternal(timeout, cancellationToken, startTime), cancellationToken);
        }

        // Waits for a pipe instance to become available. This method may return before WaitForConnection is called
        // on the server end, but WaitForConnection will not return until we have returned.  Any data writen to the
        // pipe by us after we have connected but before the server has called WaitForConnection will be available
        // to the server after it calls WaitForConnection. 
        [System.Security.SecurityCritical]
        private void ConnectInternal(int timeout, CancellationToken cancellationToken, int startTime)
        {
            Interop.SECURITY_ATTRIBUTES secAttrs = PipeStream.GetSecAttrs(_inheritability);

            int _pipeFlags = (int)_pipeOptions;
            if (_impersonationLevel != TokenImpersonationLevel.None)
            {
                _pipeFlags |= Interop.SECURITY_SQOS_PRESENT;
                _pipeFlags |= (((int)_impersonationLevel - 1) << 16);
            }

            // This is the main connection loop. It will loop until the timeout expires.  Most of the 
            // time, we will be waiting in the WaitNamedPipe win32 blocking function; however, there are
            // cases when we will need to loop: 1) The server is not created (WaitNamedPipe returns 
            // straight away in such cases), and 2) when another client connects to our server in between 
            // our WaitNamedPipe and CreateFile calls.
            int elapsed = 0;
            do
            {
                // We want any other exception and and success to have priority over cancellation.
                cancellationToken.ThrowIfCancellationRequested();

                // Wait for pipe to become free (this will block unless the pipe does not exist).
                int timeLeft = timeout - elapsed;
                int waitTime;
                if (cancellationToken.CanBeCanceled)
                {
                    waitTime = Math.Min(CancellationCheckInterval, timeLeft);
                }
                else
                {
                    waitTime = timeLeft;
                }

                if (!Interop.mincore.WaitNamedPipe(_normalizedPipePath, waitTime))
                {
                    int errorCode = Marshal.GetLastWin32Error();

                    // Server is not yet created so let's keep looping.
                    if (errorCode == Interop.ERROR_FILE_NOT_FOUND)
                    {
                        continue;
                    }

                    // The timeout has expired.
                    if (errorCode == Interop.ERROR_SUCCESS)
                    {
                        if (cancellationToken.CanBeCanceled)
                        {
                            // It may not be real timeout and only checking for cancellation
                            // let the while condition check it and decide
                            continue;
                        }
                        else
                        {
                            break;
                        }
                    }

                    throw Win32Marshal.GetExceptionForWin32Error(errorCode);
                }

                // Pipe server should be free.  Let's try to connect to it.
                SafePipeHandle handle = Interop.mincore.CreateNamedPipeClient(_normalizedPipePath,
                                            _access,           // read and write access
                                            0,                  // sharing: none
                                            ref secAttrs,           // security attributes
                                            FileMode.Open,      // open existing 
                                            _pipeFlags,         // impersonation flags
                                            Interop.NULL);  // template file: null

                if (handle.IsInvalid)
                {
                    int errorCode = Marshal.GetLastWin32Error();

                    // Handle the possible race condition of someone else connecting to the server 
                    // between our calls to WaitNamedPipe & CreateFile.
                    if (errorCode == Interop.ERROR_PIPE_BUSY)
                    {
                        continue;
                    }

                    throw Win32Marshal.GetExceptionForWin32Error(errorCode);
                }

                // Success! 
                InitializeHandle(handle, false, (_pipeOptions & PipeOptions.Asynchronous) != 0);
                State = PipeState.Connected;

                return;
            }
            while (timeout == Timeout.Infinite || (elapsed = unchecked(Environment.TickCount - startTime)) < timeout);
            // BUGBUG: SerialPort does not use unchecked arithmetic when calculating elapsed times.  This is needed
            //         because Environment.TickCount can overflow (though only every 49.7 days).

            throw new TimeoutException();
        }

        public int NumberOfServerInstances
        {
            [System.Security.SecurityCritical]
            [SuppressMessage("Microsoft.Security", "CA2122:DoNotIndirectlyExposeMethodsWithLinkDemands", Justification = "Security model of pipes: demand at creation but no subsequent demands")]
            get
            {
                CheckPipePropertyOperations();

                // NOTE: MSDN says that GetNamedPipeHandleState requires that the pipe handle has 
                // GENERIC_READ access, but we don't check for that because sometimes it works without
                // GERERIC_READ access. [Edit: Seems like CreateFile slaps on a READ_ATTRIBUTES 
                // access request before calling NTCreateFile, so all NamedPipeClientStreams can read
                // this if they are created (on WinXP SP2 at least)] 
                int numInstances;
                if (!Interop.mincore.GetNamedPipeHandleState(InternalHandle, Interop.NULL, out numInstances,
                    Interop.NULL, Interop.NULL, Interop.NULL, 0))
                {
                    WinIOError(Marshal.GetLastWin32Error());
                }

                return numInstances;
            }
        }

        // override because named pipe clients can't get/set properties when waiting to connect
        // or broken
        [System.Security.SecurityCritical]
        internal override void CheckPipePropertyOperations()
        {
            base.CheckPipePropertyOperations();

            // Invalid operation
            if (State == PipeState.WaitingToConnect)
            {
                throw new InvalidOperationException(SR.InvalidOperation_PipeNotYetConnected);
            }

            // IOException
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

    unsafe internal sealed class PipeAsyncResult : IAsyncResult
    {
        internal AsyncCallback _userCallback;   // User code callback
        internal Object _userStateObject;
        internal ManualResetEvent _waitHandle;
        [SecurityCritical]
        internal SafePipeHandle _handle;
        [SecurityCritical]
        internal NativeOverlapped* _overlapped;

        internal int _EndXxxCalled;             // Whether we've called EndXxx already.
        internal int _errorCode;

        internal bool _isComplete;              // Value for IsCompleted property        
        internal bool _completedSynchronously;  // Which thread called callback

        public Object AsyncState
        {
            get
            {
                return _userStateObject;
            }
        }

        public bool IsCompleted
        {
            get
            {
                return _isComplete;
            }
        }

        public WaitHandle AsyncWaitHandle
        {
            [System.Security.SecurityCritical]
            get
            {
                if (_waitHandle == null)
                {
                    ManualResetEvent mre = new ManualResetEvent(false);
                    if (_overlapped != null && _overlapped->EventHandle != IntPtr.Zero)
                    {
                        mre.SetSafeWaitHandle(new SafeWaitHandle(_overlapped->EventHandle, true));
                    }
                    if (_isComplete)
                    {
                        mre.Set();
                    }
                    _waitHandle = mre;
                }
                return _waitHandle;
            }
        }

        public bool CompletedSynchronously
        {
            get
            {
                return _completedSynchronously;
            }
        }

        private void CallUserCallbackWorker(Object callbackState)
        {
            _isComplete = true;
            if (_waitHandle != null)
            {
                _waitHandle.Set();
            }
            _userCallback(this);
        }

        internal void CallUserCallback()
        {
            if (_userCallback != null)
            {
                _completedSynchronously = false;
                ThreadPool.QueueUserWorkItem(new WaitCallback(CallUserCallbackWorker));
            }
            else
            {
                _isComplete = true;
                if (_waitHandle != null)
                {
                    _waitHandle.Set();
                }
            }
        }
    }
}
