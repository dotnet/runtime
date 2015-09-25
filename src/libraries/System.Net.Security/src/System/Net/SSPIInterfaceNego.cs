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
    internal interface SSPIInterfaceNego
    {
        void VerifyPackageInfo();
        Exception GetException(SecurityStatus status);
        int AcquireCredentialsHandle(string moduleName, bool IsInBoundCred, ref Interop.Secur32.AuthIdentity authdata, out SafeFreeCredentials outCredential);
        int AcquireCredentialsHandle(string moduleName, bool IsInBoundCred, ref SafeSspiAuthDataHandle authdata, out SafeFreeCredentials outCredential);
        int AcquireDefaultCredential(string moduleName, bool IsInBoundCred, out SafeFreeCredentials outCredential);
        int AcquireCredentialsHandle(string moduleName, bool IsInBoundCred, ref Interop.Secur32.SecureCredential authdata, out SafeFreeCredentials outCredential);
        SafeFreeCredentials AcquireCredentialsHandle(bool IsInBoundCred, Interop.Secur32.SecureCredential secureCredential);
        int AcceptSecurityContext(ref SafeFreeCredentials credential, ref SafeDeleteContext context, SecurityBuffer inputBuffer, SecurityBuffer outputBuffer, bool remoteCertRequired);
        int InitializeSecurityContext(ref SafeFreeCredentials credential, ref SafeDeleteContext context, string targetName, SecurityBuffer inputBuffer, SecurityBuffer outputBuffer);
        int InitializeSecurityContext(SafeFreeCredentials credential, ref SafeDeleteContext context, string targetName, SecurityBuffer[] inputBuffers, SecurityBuffer outputBuffer);     
        int EncryptMessage(SafeDeleteContext securityContext, ref byte[] buffer, int size, int headerSize, int trailerSize, out int resultSize);
        int DecryptMessage(SafeDeleteContext securityContext, byte[] buffer, ref int offset, ref int count);
        int MakeSignature(SafeDeleteContext context, byte[] buffer, uint sequenceNumber);
        int VerifySignature(SafeDeleteContext context, byte[] buffer, uint sequenceNumber);
        int QueryContextChannelBinding(SafeDeleteContext phContext, ChannelBindingKind attribute, out SafeFreeContextBufferChannelBinding refHandle);
        int QueryContextStreamSizes(SafeDeleteContext securityContext, out StreamSizes streamSizesContext);
        int QueryContextConnectionInfo(SafeDeleteContext securityContext, out SslConnectionInfo conectionInfoContext);
        int QueryContextRemoteCertificate(SafeDeleteContext securityContext, out SafeFreeCertContext remoteCertContext);
        int QueryContextIssuerList(SafeDeleteContext securityContext, out Object issuerListContext);
        int QuerySecurityContextToken(SafeDeleteContext phContext, out SecurityContextTokenHandle phToken);
        int CompleteAuthToken(ref SafeDeleteContext refContext, SecurityBuffer[] inputBuffers);
    }   
}