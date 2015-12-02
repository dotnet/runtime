// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using System.Security;
using System.Security.Principal;
using System.Threading;
using System.ComponentModel;
using System.Security.Authentication;
using System.Security.Authentication.ExtendedProtection;

namespace System.Net.Security
{
    //
    // The class maintains the state of the authentication process and the security context.
    // It encapsulates security context and does the real work in authentication and
    // user data encryption with NEGO SSPI package.
    //
    // This is part of the NegotiateStream PAL.
    //
    internal class NegoState
    {
        internal const int MaxReadFrameSize = 64 * 1024;
        internal const int MaxWriteDataSize = 63 * 1024; // 1k for the framing and trailer that is always less as per SSPI.

        internal NegoState(Stream innerStream, bool leaveStreamOpen)
        {
            throw new PlatformNotSupportedException();
        }

        internal static string DefaultPackage
        {
            get
            {
                throw new PlatformNotSupportedException();
            }
        }

        internal void ValidateCreateContext(string package,
                                            NetworkCredential credential,
                                            string servicePrincipalName,
                                            ExtendedProtectionPolicy policy,
                                            ProtectionLevel protectionLevel,
                                            TokenImpersonationLevel impersonationLevel)
        {
            throw new PlatformNotSupportedException();
        }

        internal void ValidateCreateContext(
                                            string package,
                                            bool isServer,
                                            NetworkCredential credential,
                                            string servicePrincipalName,
                                            ChannelBinding channelBinding,
                                            ProtectionLevel protectionLevel,
                                            TokenImpersonationLevel impersonationLevel
                                            )
        {
            throw new PlatformNotSupportedException();
        }

        internal bool IsAuthenticated
        {
            get
            {
                throw new PlatformNotSupportedException();
            }
        }

        internal bool IsMutuallyAuthenticated
        {
            get
            {
                throw new PlatformNotSupportedException();
            }
        }

        internal bool IsEncrypted
        {
            get
            {
                throw new PlatformNotSupportedException();
            }
        }

        internal bool IsSigned
        {
            get
            {
                throw new PlatformNotSupportedException();
            }
        }

        internal bool IsServer
        {
            get
            {
                throw new PlatformNotSupportedException();
            }
        }

        internal bool CanGetSecureStream
        {
            get
            {
                throw new PlatformNotSupportedException();
            }
        }

        internal TokenImpersonationLevel AllowedImpersonation
        {
            get
            {
                throw new PlatformNotSupportedException();
            }
        }

        internal IIdentity GetIdentity()
        {
            throw new PlatformNotSupportedException();
        }

        internal void CheckThrow(bool authSucessCheck)
        {
            throw new PlatformNotSupportedException();
        }

        //
        // This is to not depend on GC&SafeHandle class if the context is not needed anymore.
        //
        internal void Close()
        {
            throw new PlatformNotSupportedException();
        }

        internal void ProcessAuthentication(LazyAsyncResult lazyResult)
        {
            throw new PlatformNotSupportedException();
        }

        internal void EndProcessAuthentication(IAsyncResult result)
        {
            throw new PlatformNotSupportedException();
        }


        internal int EncryptData(byte[] buffer, int offset, int count, ref byte[] outBuffer)
        {
            throw new PlatformNotSupportedException();
        }

        internal int DecryptData(byte[] buffer, int offset, int count, out int newOffset)
        {
            throw new PlatformNotSupportedException();
        }
    }
}
