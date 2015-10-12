// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel;
using System.Globalization;
using System.Net.Security;
using System.Runtime.InteropServices;
using System.Security.Authentication.ExtendedProtection;
using System.Security.Cryptography.X509Certificates;
using System.Security.Authentication;

namespace System.Net
{
    // Used to define the interface for security to use.
    internal interface SSPIInterface
    {
        void VerifyPackageInfo();
        Exception GetException(SecurityStatus status);
        SafeFreeCredentials AcquireCredentialsHandle(X509Certificate certificate, SslProtocols protocols, EncryptionPolicy policy, bool isServer);
        SecurityStatus AcceptSecurityContext(ref SafeFreeCredentials credential, ref SafeDeleteContext context, SecurityBuffer inputBuffer, SecurityBuffer outputBuffer, bool remoteCertRequired);
        SecurityStatus InitializeSecurityContext(ref SafeFreeCredentials credential, ref SafeDeleteContext context, string targetName, SecurityBuffer inputBuffer, SecurityBuffer outputBuffer);
        SecurityStatus InitializeSecurityContext(SafeFreeCredentials credential, ref SafeDeleteContext context, string targetName, SecurityBuffer[] inputBuffers, SecurityBuffer outputBuffer);
        SecurityStatus EncryptMessage(SafeDeleteContext securityContext, byte[] buffer, int size, int headerSize, int trailerSize, out int resultSize);
        SecurityStatus DecryptMessage(SafeDeleteContext securityContext, byte[] buffer, ref int offset, ref int count);
        int QueryContextChannelBinding(SafeDeleteContext phContext, ChannelBindingKind attribute, out SafeFreeContextBufferChannelBinding refHandle);
        int QueryContextStreamSizes(SafeDeleteContext securityContext, out StreamSizes streamSizes);
        int QueryContextConnectionInfo(SafeDeleteContext securityContext, out SslConnectionInfo conectionInfo);
        int QueryContextRemoteCertificate(SafeDeleteContext securityContext, out SafeFreeCertContext remoteCert);
        int QueryContextIssuerList(SafeDeleteContext securityContext, out Object issuerList);
    }
}