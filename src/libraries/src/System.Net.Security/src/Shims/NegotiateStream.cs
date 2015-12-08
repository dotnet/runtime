// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using System.Security.Authentication.ExtendedProtection;
using System.Security.Principal;
using System.Threading.Tasks;

namespace System.Net.Security
{
    public class NegotiateStream : AuthenticatedStream
    {
        public NegotiateStream(Stream innerStream) : base(innerStream, false)
        {
            throw new PlatformNotSupportedException();
        }

        public NegotiateStream(Stream innerStream, bool leaveInnerStreamOpen) : base(innerStream, leaveInnerStreamOpen)
        {
            throw new PlatformNotSupportedException();
        }

        public override bool CanRead { get { throw new PlatformNotSupportedException(); } }

        public override bool CanSeek { get { throw new PlatformNotSupportedException(); } }

        public override bool CanTimeout { get { throw new PlatformNotSupportedException(); } }

        public override bool CanWrite { get { throw new PlatformNotSupportedException(); } }

        public virtual TokenImpersonationLevel ImpersonationLevel { get { throw new PlatformNotSupportedException(); } }

        public override bool IsAuthenticated { get { throw new PlatformNotSupportedException(); } }

        public override bool IsEncrypted { get { throw new PlatformNotSupportedException(); } }

        public override bool IsMutuallyAuthenticated { get { throw new PlatformNotSupportedException(); } }

        public override bool IsServer { get { throw new PlatformNotSupportedException(); } }

        public override bool IsSigned { get { throw new PlatformNotSupportedException(); } }

        public override long Length { get { throw new PlatformNotSupportedException(); } }

        public override long Position
        {
            get
            {
                throw new PlatformNotSupportedException();
            }
            set
            {
                throw new PlatformNotSupportedException();
            }
        }

        public override int ReadTimeout
        {
            get
            {
                throw new PlatformNotSupportedException();
            }
            set
            {
                throw new PlatformNotSupportedException();
            }
        }

        public virtual IIdentity RemoteIdentity { get { throw new PlatformNotSupportedException(); } }

        public override int WriteTimeout
        {
            get
            {
                throw new PlatformNotSupportedException();
            }
            set
            {
                throw new PlatformNotSupportedException();
            }
        }

        public virtual Task AuthenticateAsClientAsync()
        {
            throw new PlatformNotSupportedException();
        }

        public virtual Task AuthenticateAsClientAsync(NetworkCredential credential, string targetName)
        {
            throw new PlatformNotSupportedException();
        }

        public virtual Task AuthenticateAsClientAsync(NetworkCredential credential, ChannelBinding binding, string targetName)
        {
            throw new PlatformNotSupportedException();
        }

        public virtual Task AuthenticateAsClientAsync(NetworkCredential credential, string targetName, ProtectionLevel requiredProtectionLevel, TokenImpersonationLevel allowedImpersonationLevel)
        {
            throw new PlatformNotSupportedException();
        }

        public virtual Task AuthenticateAsClientAsync(NetworkCredential credential, ChannelBinding binding, string targetName, ProtectionLevel requiredProtectionLevel, TokenImpersonationLevel allowedImpersonationLevel)
        {
            throw new PlatformNotSupportedException();
        }

        public virtual Task AuthenticateAsServerAsync()
        {
            throw new PlatformNotSupportedException();
        }

        public virtual Task AuthenticateAsServerAsync(ExtendedProtectionPolicy policy)
        {
            throw new PlatformNotSupportedException();
        }

        public virtual Task AuthenticateAsServerAsync(NetworkCredential credential, ProtectionLevel requiredProtectionLevel, TokenImpersonationLevel requiredImpersonationLevel)
        {
            throw new PlatformNotSupportedException();
        }

        public virtual Task AuthenticateAsServerAsync(NetworkCredential credential, ExtendedProtectionPolicy policy, ProtectionLevel requiredProtectionLevel, TokenImpersonationLevel requiredImpersonationLevel)
        {
            throw new PlatformNotSupportedException();
        }

        public override void Flush()
        {
            throw new PlatformNotSupportedException();
        }
        
        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new PlatformNotSupportedException();
        }
        
        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new PlatformNotSupportedException();
        }
        
        public override void SetLength(long value)
        {
            throw new PlatformNotSupportedException();
        }
        
        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new PlatformNotSupportedException();
        }

        protected override void Dispose(bool disposing)
        {
            throw new PlatformNotSupportedException();
        }
    }
}
