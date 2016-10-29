// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Security.Authentication.ExtendedProtection;
using System.Security.Principal;

namespace System.Net.Security
{
    /*
        An authenticated stream based on NEGO SSP.

            The class that can be used by client and server side applications
            - to transfer Identities across the stream
            - to encrypt data based on NEGO SSP package

            In most cases the innerStream will be of type NetworkStream.
            On Win9x data encryption is not available and both sides have
            to explicitly drop SecurityLevel and MuatualAuth requirements.

            This is a simple wrapper class.
            All real work is done by internal NegoState class and the other partial implementation files.
    */
    public partial class NegotiateStream : AuthenticatedStream
    {
        private NegoState _negoState;
        private string _package;
        private IIdentity _remoteIdentity;

        public NegotiateStream(Stream innerStream) : this(innerStream, false)
        {
        }

        public NegotiateStream(Stream innerStream, bool leaveInnerStreamOpen) : base(innerStream, leaveInnerStreamOpen)
        {
#if DEBUG
            using (DebugThreadTracking.SetThreadKind(ThreadKinds.User))
            {
#endif
                _negoState = new NegoState(innerStream, leaveInnerStreamOpen);
                _package = NegoState.DefaultPackage;
                InitializeStreamPart();
#if DEBUG
            }
#endif
        }

        public virtual IAsyncResult BeginAuthenticateAsClient(AsyncCallback asyncCallback, object asyncState)
        {
            return BeginAuthenticateAsClient((NetworkCredential)CredentialCache.DefaultCredentials, null, string.Empty,
                                           ProtectionLevel.EncryptAndSign, TokenImpersonationLevel.Identification,
                                           asyncCallback, asyncState);
        }

        public virtual IAsyncResult BeginAuthenticateAsClient(NetworkCredential credential, string targetName, AsyncCallback asyncCallback, object asyncState)
        {
            return BeginAuthenticateAsClient(credential, null, targetName,
                                           ProtectionLevel.EncryptAndSign, TokenImpersonationLevel.Identification,
                                           asyncCallback, asyncState);
        }

        public virtual IAsyncResult BeginAuthenticateAsClient(NetworkCredential credential, ChannelBinding binding, string targetName, AsyncCallback asyncCallback, object asyncState)
        {
            return BeginAuthenticateAsClient(credential, binding, targetName,
                                             ProtectionLevel.EncryptAndSign, TokenImpersonationLevel.Identification,
                                             asyncCallback, asyncState);
        }

        public virtual IAsyncResult BeginAuthenticateAsClient(
            NetworkCredential credential,
            string targetName,
            ProtectionLevel requiredProtectionLevel,
            TokenImpersonationLevel allowedImpersonationLevel,
            AsyncCallback asyncCallback,
            object asyncState)
        {
            return BeginAuthenticateAsClient(credential, null, targetName,
                                             requiredProtectionLevel, allowedImpersonationLevel,
                                             asyncCallback, asyncState);
        }

        public virtual IAsyncResult BeginAuthenticateAsClient(
            NetworkCredential credential,
            ChannelBinding binding,
            string targetName,
            ProtectionLevel requiredProtectionLevel,
            TokenImpersonationLevel allowedImpersonationLevel,
            AsyncCallback asyncCallback,
            object asyncState)
        {
#if DEBUG
            using (DebugThreadTracking.SetThreadKind(ThreadKinds.User | ThreadKinds.Async))
            {
#endif
                _negoState.ValidateCreateContext(_package, false, credential, targetName, binding, requiredProtectionLevel, allowedImpersonationLevel);

                LazyAsyncResult result = new LazyAsyncResult(_negoState, asyncState, asyncCallback);
                _negoState.ProcessAuthentication(result);

                return result;
#if DEBUG
            }
#endif
        }

        public virtual void EndAuthenticateAsClient(IAsyncResult asyncResult)
        {
#if DEBUG
            using (DebugThreadTracking.SetThreadKind(ThreadKinds.User))
            {
#endif
                _negoState.EndProcessAuthentication(asyncResult);
#if DEBUG
            }
#endif
        }

        public virtual void AuthenticateAsServer()
        {
            AuthenticateAsServer((NetworkCredential)CredentialCache.DefaultCredentials, null, ProtectionLevel.EncryptAndSign, TokenImpersonationLevel.Identification);
        }

        public virtual void AuthenticateAsServer(ExtendedProtectionPolicy policy)
        {
            AuthenticateAsServer((NetworkCredential)CredentialCache.DefaultCredentials, policy, ProtectionLevel.EncryptAndSign, TokenImpersonationLevel.Identification);
        }

        public virtual void AuthenticateAsServer(NetworkCredential credential, ProtectionLevel requiredProtectionLevel, TokenImpersonationLevel requiredImpersonationLevel)
        {
            AuthenticateAsServer(credential, null, requiredProtectionLevel, requiredImpersonationLevel);
        }

        public virtual void AuthenticateAsServer(NetworkCredential credential, ExtendedProtectionPolicy policy, ProtectionLevel requiredProtectionLevel, TokenImpersonationLevel requiredImpersonationLevel)
        {
#if DEBUG
            using (DebugThreadTracking.SetThreadKind(ThreadKinds.User | ThreadKinds.Sync))
            {
#endif
                _negoState.ValidateCreateContext(_package, credential, string.Empty, policy, requiredProtectionLevel, requiredImpersonationLevel);
                _negoState.ProcessAuthentication(null);
#if DEBUG
            }
#endif
        }

        public virtual IAsyncResult BeginAuthenticateAsServer(AsyncCallback asyncCallback, object asyncState)
        {
            return BeginAuthenticateAsServer((NetworkCredential)CredentialCache.DefaultCredentials, null, ProtectionLevel.EncryptAndSign, TokenImpersonationLevel.Identification, asyncCallback, asyncState);
        }

        public virtual IAsyncResult BeginAuthenticateAsServer(ExtendedProtectionPolicy policy, AsyncCallback asyncCallback, object asyncState)
        {
            return BeginAuthenticateAsServer((NetworkCredential)CredentialCache.DefaultCredentials, policy, ProtectionLevel.EncryptAndSign, TokenImpersonationLevel.Identification, asyncCallback, asyncState);
        }

        public virtual IAsyncResult BeginAuthenticateAsServer(
            NetworkCredential credential,
            ProtectionLevel requiredProtectionLevel,
            TokenImpersonationLevel requiredImpersonationLevel,
            AsyncCallback asyncCallback,
            object asyncState)
        {
            return BeginAuthenticateAsServer(credential, null, requiredProtectionLevel, requiredImpersonationLevel, asyncCallback, asyncState);
        }

        public virtual IAsyncResult BeginAuthenticateAsServer(
            NetworkCredential credential,
            ExtendedProtectionPolicy policy,
            ProtectionLevel requiredProtectionLevel,
            TokenImpersonationLevel requiredImpersonationLevel,
            AsyncCallback asyncCallback,
            object asyncState)
        {
#if DEBUG
            using (DebugThreadTracking.SetThreadKind(ThreadKinds.User | ThreadKinds.Async))
            {
#endif
                _negoState.ValidateCreateContext(_package, credential, string.Empty, policy, requiredProtectionLevel, requiredImpersonationLevel);

                LazyAsyncResult result = new LazyAsyncResult(_negoState, asyncState, asyncCallback);
                _negoState.ProcessAuthentication(result);

                return result;
#if DEBUG
            }
#endif
        }
        //
        public virtual void EndAuthenticateAsServer(IAsyncResult asyncResult)
        {
#if DEBUG
            using (DebugThreadTracking.SetThreadKind(ThreadKinds.User))
            {
#endif
                _negoState.EndProcessAuthentication(asyncResult);
#if DEBUG
            }
#endif
        }

        public virtual void AuthenticateAsClient()
        {
            AuthenticateAsClient((NetworkCredential)CredentialCache.DefaultCredentials, null, string.Empty, ProtectionLevel.EncryptAndSign, TokenImpersonationLevel.Identification);
        }

        public virtual void AuthenticateAsClient(NetworkCredential credential, string targetName)
        {
            AuthenticateAsClient(credential, null, targetName, ProtectionLevel.EncryptAndSign, TokenImpersonationLevel.Identification);
        }

        public virtual void AuthenticateAsClient(NetworkCredential credential, ChannelBinding binding, string targetName)
        {
            AuthenticateAsClient(credential, binding, targetName, ProtectionLevel.EncryptAndSign, TokenImpersonationLevel.Identification);
        }

        public virtual void AuthenticateAsClient(
            NetworkCredential credential, string targetName, ProtectionLevel requiredProtectionLevel, TokenImpersonationLevel allowedImpersonationLevel)
        {
            AuthenticateAsClient(credential, null, targetName, requiredProtectionLevel, allowedImpersonationLevel);
        }
        
        public virtual void AuthenticateAsClient(
            NetworkCredential credential, ChannelBinding binding, string targetName, ProtectionLevel requiredProtectionLevel, TokenImpersonationLevel allowedImpersonationLevel)
        {
#if DEBUG
            using (DebugThreadTracking.SetThreadKind(ThreadKinds.User | ThreadKinds.Sync))
            {
#endif
                _negoState.ValidateCreateContext(_package, false, credential, targetName, binding, requiredProtectionLevel, allowedImpersonationLevel);
                _negoState.ProcessAuthentication(null);
#if DEBUG
            }
#endif
        }

        public virtual Task AuthenticateAsClientAsync()
        {
            return Task.Factory.FromAsync(BeginAuthenticateAsClient, EndAuthenticateAsClient, null);
        }

        public virtual Task AuthenticateAsClientAsync(NetworkCredential credential, string targetName)
        {
            return Task.Factory.FromAsync(BeginAuthenticateAsClient, EndAuthenticateAsClient, credential, targetName, null);
        }

        public virtual Task AuthenticateAsClientAsync(
            NetworkCredential credential, string targetName,
            ProtectionLevel requiredProtectionLevel,
            TokenImpersonationLevel allowedImpersonationLevel)
        {
            return Task.Factory.FromAsync((callback, state) => BeginAuthenticateAsClient(credential, targetName, requiredProtectionLevel, allowedImpersonationLevel, callback, state), EndAuthenticateAsClient, null);
        }

        public virtual Task AuthenticateAsClientAsync(NetworkCredential credential, ChannelBinding binding, string targetName)
        {
            return Task.Factory.FromAsync(BeginAuthenticateAsClient, EndAuthenticateAsClient, credential, binding, targetName, null);
        }

        public virtual Task AuthenticateAsClientAsync(
            NetworkCredential credential, ChannelBinding binding,
            string targetName, ProtectionLevel requiredProtectionLevel,
            TokenImpersonationLevel allowedImpersonationLevel)
        {
            return Task.Factory.FromAsync((callback, state) => BeginAuthenticateAsClient(credential, binding, targetName, requiredProtectionLevel, allowedImpersonationLevel, callback, state), EndAuthenticateAsClient, null);
        }

        public virtual Task AuthenticateAsServerAsync()
        {
            return Task.Factory.FromAsync(BeginAuthenticateAsServer, EndAuthenticateAsServer, null);
        }

        public virtual Task AuthenticateAsServerAsync(ExtendedProtectionPolicy policy)
        {
            return Task.Factory.FromAsync(BeginAuthenticateAsServer, EndAuthenticateAsServer, policy, null);
        }

        public virtual Task AuthenticateAsServerAsync(NetworkCredential credential, ProtectionLevel requiredProtectionLevel, TokenImpersonationLevel requiredImpersonationLevel)
        {
            return Task.Factory.FromAsync(BeginAuthenticateAsServer, EndAuthenticateAsServer, credential, requiredProtectionLevel, requiredImpersonationLevel, null);
        }

        public virtual Task AuthenticateAsServerAsync(
            NetworkCredential credential, ExtendedProtectionPolicy policy,
            ProtectionLevel requiredProtectionLevel,
            TokenImpersonationLevel requiredImpersonationLevel)
        {
            return Task.Factory.FromAsync((callback, state) => BeginAuthenticateAsServer(credential, policy, requiredProtectionLevel, requiredImpersonationLevel, callback, state), EndAuthenticateAsClient, null);
        }

        public override bool IsAuthenticated
        {
            get
            {
#if DEBUG
                using (DebugThreadTracking.SetThreadKind(ThreadKinds.User | ThreadKinds.Async))
                {
#endif
                    return _negoState.IsAuthenticated;
#if DEBUG
                }
#endif
            }
        }

        public override bool IsMutuallyAuthenticated
        {
            get
            {
#if DEBUG
                using (DebugThreadTracking.SetThreadKind(ThreadKinds.User | ThreadKinds.Async))
                {
#endif
                    return _negoState.IsMutuallyAuthenticated;
#if DEBUG
                }
#endif
            }
        }

        public override bool IsEncrypted
        {
            get
            {
#if DEBUG
                using (DebugThreadTracking.SetThreadKind(ThreadKinds.User | ThreadKinds.Async))
                {
#endif
                    return _negoState.IsEncrypted;
#if DEBUG
                }
#endif
            }
        }

        public override bool IsSigned
        {
            get
            {
#if DEBUG
                using (DebugThreadTracking.SetThreadKind(ThreadKinds.User | ThreadKinds.Async))
                {
#endif
                    return _negoState.IsSigned;
#if DEBUG
                }
#endif
            }
        }

        public override bool IsServer
        {
            get
            {
#if DEBUG
                using (DebugThreadTracking.SetThreadKind(ThreadKinds.User | ThreadKinds.Async))
                {
#endif
                    return _negoState.IsServer;
#if DEBUG
                }
#endif
            }
        }

        public virtual TokenImpersonationLevel ImpersonationLevel
        {
            get
            {
#if DEBUG
                using (DebugThreadTracking.SetThreadKind(ThreadKinds.User | ThreadKinds.Async))
                {
#endif
                    return _negoState.AllowedImpersonation;
#if DEBUG
                }
#endif
            }
        }

        public virtual IIdentity RemoteIdentity
        {
            get
            {
#if DEBUG
                using (DebugThreadTracking.SetThreadKind(ThreadKinds.User | ThreadKinds.Async))
                {
#endif

                    if (_remoteIdentity == null)
                    {
                        _remoteIdentity = _negoState.GetIdentity();
                    }

                    return _remoteIdentity;
#if DEBUG
                }
#endif
            }
        }

        //
        // Stream contract implementation
        //
        public override bool CanSeek
        {
            get
            {
                return false;
            }
        }

        public override bool CanRead
        {
            get
            {
                return IsAuthenticated && InnerStream.CanRead;
            }
        }

        public override bool CanTimeout
        {
            get
            {
                return InnerStream.CanTimeout;
            }
        }

        public override bool CanWrite
        {
            get
            {
                return IsAuthenticated && InnerStream.CanWrite;
            }
        }

        public override int ReadTimeout
        {
            get
            {
                return InnerStream.ReadTimeout;
            }
            set
            {
                InnerStream.ReadTimeout = value;
            }
        }

        public override int WriteTimeout
        {
            get
            {
                return InnerStream.WriteTimeout;
            }
            set
            {
                InnerStream.WriteTimeout = value;
            }
        }

        public override long Length
        {
            get
            {
                return InnerStream.Length;
            }
        }

        public override long Position
        {
            get
            {
                return InnerStream.Position;
            }
            set
            {
                throw new NotSupportedException(SR.net_noseek);
            }
        }

        public override void SetLength(long value)
        {
            InnerStream.SetLength(value);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException(SR.net_noseek);
        }

        public override void Flush()
        {
#if DEBUG
            using (DebugThreadTracking.SetThreadKind(ThreadKinds.User | ThreadKinds.Sync))
            {
#endif
                InnerStream.Flush();
#if DEBUG
            }
#endif
        }

        protected override void Dispose(bool disposing)
        {
#if DEBUG
            using (DebugThreadTracking.SetThreadKind(ThreadKinds.User))
            {
#endif
                try
                {
                    _negoState.Close();
                }
                finally
                {
                    base.Dispose(disposing);
                }
#if DEBUG
            }
#endif
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
#if DEBUG
            using (DebugThreadTracking.SetThreadKind(ThreadKinds.User | ThreadKinds.Sync))
            {
#endif
                _negoState.CheckThrow(true);

                if (!_negoState.CanGetSecureStream)
                {
                    return InnerStream.Read(buffer, offset, count);
                }

                return ProcessRead(buffer, offset, count, null);
#if DEBUG
            }
#endif
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
#if DEBUG
            using (DebugThreadTracking.SetThreadKind(ThreadKinds.User | ThreadKinds.Sync))
            {
#endif
                _negoState.CheckThrow(true);

                if (!_negoState.CanGetSecureStream)
                {
                    InnerStream.Write(buffer, offset, count);
                    return;
                }

                ProcessWrite(buffer, offset, count, null);
#if DEBUG
            }
#endif
        }

        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback asyncCallback, object asyncState)
        {
#if DEBUG
            using (DebugThreadTracking.SetThreadKind(ThreadKinds.User | ThreadKinds.Async))
            {
#endif
                _negoState.CheckThrow(true);

                if (!_negoState.CanGetSecureStream)
                {
                    return InnerStream.BeginRead(buffer, offset, count, asyncCallback, asyncState);
                }

                BufferAsyncResult bufferResult = new BufferAsyncResult(this, buffer, offset, count, asyncState, asyncCallback);
                AsyncProtocolRequest asyncRequest = new AsyncProtocolRequest(bufferResult);
                ProcessRead(buffer, offset, count, asyncRequest);
                return bufferResult;
#if DEBUG
            }
#endif
        }

        public override int EndRead(IAsyncResult asyncResult)
        {
#if DEBUG
            using (DebugThreadTracking.SetThreadKind(ThreadKinds.User))
            {
#endif
                _negoState.CheckThrow(true);

                if (!_negoState.CanGetSecureStream)
                {
                    return InnerStream.EndRead(asyncResult);
                }


                if (asyncResult == null)
                {
                    throw new ArgumentNullException(nameof(asyncResult));
                }

                BufferAsyncResult bufferResult = asyncResult as BufferAsyncResult;
                if (bufferResult == null)
                {
                    throw new ArgumentException(SR.Format(SR.net_io_async_result, asyncResult.GetType().FullName), nameof(asyncResult));
                }

                if (Interlocked.Exchange(ref _NestedRead, 0) == 0)
                {
                    throw new InvalidOperationException(SR.Format(SR.net_io_invalidendcall, "EndRead"));
                }

                // No "artificial" timeouts implemented so far, InnerStream controls timeout.
                bufferResult.InternalWaitForCompletion();

                if (bufferResult.Result is Exception)
                {
                    if (bufferResult.Result is IOException)
                    {
                        throw (Exception)bufferResult.Result;
                    }

                    throw new IOException(SR.net_io_read, (Exception)bufferResult.Result);
                }

                return (int)bufferResult.Result;
#if DEBUG
            }
#endif
        }
        //
        //
        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback asyncCallback, object asyncState)
        {
#if DEBUG
            using (DebugThreadTracking.SetThreadKind(ThreadKinds.User | ThreadKinds.Async))
            {
#endif
                _negoState.CheckThrow(true);

                if (!_negoState.CanGetSecureStream)
                {
                    return InnerStream.BeginWrite(buffer, offset, count, asyncCallback, asyncState);
                }

                BufferAsyncResult bufferResult = new BufferAsyncResult(this, buffer, offset, count, true, asyncState, asyncCallback);
                AsyncProtocolRequest asyncRequest = new AsyncProtocolRequest(bufferResult);

                ProcessWrite(buffer, offset, count, asyncRequest);
                return bufferResult;
#if DEBUG
            }
#endif
        }

        public override void EndWrite(IAsyncResult asyncResult)
        {
#if DEBUG
            using (DebugThreadTracking.SetThreadKind(ThreadKinds.User))
            {
#endif
                _negoState.CheckThrow(true);

                if (!_negoState.CanGetSecureStream)
                {
                    InnerStream.EndWrite(asyncResult);
                    return;
                }

                if (asyncResult == null)
                {
                    throw new ArgumentNullException(nameof(asyncResult));
                }

                BufferAsyncResult bufferResult = asyncResult as BufferAsyncResult;
                if (bufferResult == null)
                {
                    throw new ArgumentException(SR.Format(SR.net_io_async_result, asyncResult.GetType().FullName), nameof(asyncResult));
                }

                if (Interlocked.Exchange(ref _NestedWrite, 0) == 0)
                {
                    throw new InvalidOperationException(SR.Format(SR.net_io_invalidendcall, "EndWrite"));
                }

                // No "artificial" timeouts implemented so far, InnerStream controls timeout.
                bufferResult.InternalWaitForCompletion();

                if (bufferResult.Result is Exception)
                {
                    if (bufferResult.Result is IOException)
                    {
                        throw (Exception)bufferResult.Result;
                    }

                    throw new IOException(SR.net_io_write, (Exception)bufferResult.Result);
                }
#if DEBUG
            }
#endif
        }

        // ReadAsync - provide async read functionality.
        // 
        // This method provides async read functionality. All we do is
        // call through to the Begin/EndRead methods.
        // 
        // Input:
        // 
        //     buffer            - Buffer to read into.
        //     offset            - Offset into the buffer where we're to read.
        //     size              - Number of bytes to read.
        //     cancellationToken - Token used to request cancellation of the operation
        // 
        // Returns:
        // 
        //     A Task<int> representing the read.
        public override Task<int> ReadAsync(byte[] buffer, int offset, int size, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return Task.FromCanceled<int>(cancellationToken);
            }

            return Task.Factory.FromAsync(
                (bufferArg, offsetArg, sizeArg, callback, state) => ((NegotiateStream)state).BeginRead(bufferArg, offsetArg, sizeArg, callback, state),
                iar => ((NegotiateStream)iar.AsyncState).EndRead(iar),
                buffer,
                offset,
                size,
                this);
        }

        // WriteAsync - provide async write functionality.
        // 
        // This method provides async write functionality. All we do is
        // call through to the Begin/EndWrite methods.
        // 
        // Input:
        // 
        //     buffer  - Buffer to write into.
        //     offset  - Offset into the buffer where we're to write.
        //     size    - Number of bytes to write.
        //     cancellationToken - Token used to request cancellation of the operation
        // 
        // Returns:
        // 
        //     A Task representing the write.
        public override Task WriteAsync(byte[] buffer, int offset, int size, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return Task.FromCanceled<int>(cancellationToken);
            }

            return Task.Factory.FromAsync(
                (bufferArg, offsetArg, sizeArg, callback, state) => ((NegotiateStream)state).BeginWrite(bufferArg, offsetArg, sizeArg, callback, state),
                iar => ((NegotiateStream)iar.AsyncState).EndWrite(iar),
                buffer,
                offset,
                size,
                this);
        }
    }
}
