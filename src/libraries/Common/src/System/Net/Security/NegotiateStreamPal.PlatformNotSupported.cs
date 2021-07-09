// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security;
using System.Security.Authentication;
using System.Security.Authentication.ExtendedProtection;
using System.Security.Principal;
using System.Text;
using System.Threading;
using Microsoft.Win32.SafeHandles;

namespace System.Net.Security
{
    //
    // The class maintains the state of the authentication process and the security context.
    // It encapsulates security context and does the real work in authentication and
    // user data encryption with NEGO SSPI package.
    //
    [UnsupportedOSPlatform("tvos")]
    internal static partial class NegotiateStreamPal
    {
        internal static string QueryContextClientSpecifiedSpn(SafeDeleteContext securityContext)
        {
            throw new PlatformNotSupportedException(SR.net_nego_server_not_supported);
        }

        internal static string QueryContextAuthenticationPackage(SafeDeleteContext securityContext)
        {
            throw new PlatformNotSupportedException();
        }

        internal static SecurityStatusPal InitializeSecurityContext(
            ref SafeFreeCredentials credentialsHandle,
            ref SafeDeleteContext? securityContext,
            string? spn,
            ContextFlagsPal requestedContextFlags,
            byte[]? incomingBlob,
            ChannelBinding? channelBinding,
            ref byte[]? resultBlob,
            ref ContextFlagsPal contextFlags)
        {
            throw new PlatformNotSupportedException();
        }

        internal static SecurityStatusPal AcceptSecurityContext(
            SafeFreeCredentials? credentialsHandle,
            ref SafeDeleteContext? securityContext,
            ContextFlagsPal requestedContextFlags,
            byte[]? incomingBlob,
            ChannelBinding? channelBinding,
            ref byte[] resultBlob,
            ref ContextFlagsPal contextFlags)
        {
            throw new PlatformNotSupportedException();
        }

        internal static Win32Exception CreateExceptionFromError(SecurityStatusPal statusCode)
        {
            throw new PlatformNotSupportedException();
        }

        internal static int QueryMaxTokenSize(string package)
        {
            throw new PlatformNotSupportedException();
        }

        internal static SafeFreeCredentials AcquireDefaultCredential(string package, bool isServer)
        {
            throw new PlatformNotSupportedException();
        }

        internal static SafeFreeCredentials AcquireCredentialsHandle(string package, bool isServer, NetworkCredential credential)
        {
            throw new PlatformNotSupportedException();
        }

        internal static SecurityStatusPal CompleteAuthToken(
            ref SafeDeleteContext? securityContext,
            byte[]? incomingBlob)
        {
            throw new PlatformNotSupportedException();
        }

        internal static int Encrypt(
            SafeDeleteContext securityContext,
            ReadOnlySpan<byte> buffer,
            bool isConfidential,
            bool isNtlm,
            [NotNull] ref byte[]? output,
            uint sequenceNumber)
        {
            throw new PlatformNotSupportedException();
        }

        internal static int Decrypt(
            SafeDeleteContext securityContext,
            byte[]? buffer,
            int offset,
            int count,
            bool isConfidential,
            bool isNtlm,
            out int newOffset,
            uint sequenceNumber)
        {
            throw new PlatformNotSupportedException();
        }

        internal static int VerifySignature(SafeDeleteContext securityContext, byte[] buffer, int offset, int count)
        {
            throw new PlatformNotSupportedException();
        }

        internal static int MakeSignature(SafeDeleteContext securityContext, byte[] buffer, int offset, int count, [AllowNull] ref byte[] output)
        {
            throw new PlatformNotSupportedException();
        }
    }
}
