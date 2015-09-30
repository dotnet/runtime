// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel;
using System.Globalization;
using System.Net.Security;
using System.Runtime.InteropServices;
using System.Security.Authentication;
using System.Security.Authentication.ExtendedProtection;
using System.Security.Cryptography.X509Certificates;

namespace System.Net
{
    internal static class SSPIWrapper
    {
        internal static void VerifyPackageInfo(SSPIInterface secModule)
        {
            if (Logging.On)
            {
                Logging.PrintInfo(Logging.Web, "VerifyPackageInfo");
            }

            secModule.VerifyPackageInfo();
        }

        internal static Exception GetException(SSPIInterface secModule, SecurityStatus status)
        {
            return secModule.GetException(status);
        }

        internal static SafeFreeCredentials AcquireCredentialsHandle(SSPIInterface SecModule, X509Certificate certificate, SslProtocols protocols, EncryptionPolicy policy, bool isServer)
        {
            if (Logging.On)
            {
                Logging.PrintInfo(Logging.Web,
                    "AcquireCredentialsHandle(" +
                    "protocols  = " + protocols + ", " +
                    "policy   = " + policy + ", " +
                    "isServer = " + isServer + ")");
            }

            return SecModule.AcquireCredentialsHandle(certificate, protocols, policy, isServer);
        }

        internal static SecurityStatus InitializeSecurityContext(SSPIInterface SecModule, ref SafeFreeCredentials credential, ref SafeDeleteContext context, string targetName, SecurityBuffer inputBuffer, SecurityBuffer outputBuffer)
        {
            if (Logging.On)
            {
                Logging.PrintInfo(Logging.Web,
                    "InitializeSecurityContext(" +
                    "credential = " + credential.ToString() + ", " +
                    "context = " + Logging.ObjectToString(context) + ", " +
                    "targetName = " + targetName);
            }


            SecurityStatus errorCode = SecModule.InitializeSecurityContext(ref credential, ref context, targetName, inputBuffer, outputBuffer);

            if (Logging.On)
            {
                Logging.PrintInfo(Logging.Web, SR.Format(SR.net_log_sspi_security_context_input_buffer, "InitializeSecurityContext", (inputBuffer == null ? 0 : inputBuffer.size), outputBuffer.size, (SecurityStatus)errorCode));
            }

            return errorCode;
        }

        internal static SecurityStatus InitializeSecurityContext(SSPIInterface SecModule, SafeFreeCredentials credential, ref SafeDeleteContext context, string targetName, SecurityBuffer[] inputBuffers, SecurityBuffer outputBuffer)
        {
            if (Logging.On)
            {
                Logging.PrintInfo(Logging.Web,
                    "InitializeSecurityContext(" +
                    "credential = " + credential.ToString() + ", " +
                    "context = " + Logging.ObjectToString(context) + ", " +
                    "targetName = " + targetName);
            }

            SecurityStatus errorCode = SecModule.InitializeSecurityContext(credential, ref context, targetName, inputBuffers, outputBuffer);

            return errorCode;
        }

        internal static SecurityStatus AcceptSecurityContext(SSPIInterface SecModule, ref SafeFreeCredentials credential, ref SafeDeleteContext context, SecurityBuffer inputBuffer, SecurityBuffer outputBuffer, bool remoteCertRequired)
        {
            if (Logging.On)
            {
                Logging.PrintInfo(Logging.Web,
                    "AcceptSecurityContext(" +
                    "credential = " + credential.ToString() + ", " +
                    "context = " + Logging.ObjectToString(context) + ", " +
                    "remoteCertRequired = " + remoteCertRequired);
            }
            return SecModule.AcceptSecurityContext(ref credential, ref context, inputBuffer, outputBuffer, remoteCertRequired);
        }

        internal static SecurityStatus EncryptMessage(SSPIInterface secModule, SafeDeleteContext securityContext, byte[] buffer, int size, int headerSize, int trailerSize, out int resultSize)
        {
            return secModule.EncryptMessage(securityContext, buffer, size, headerSize, trailerSize, out resultSize);
        }

        internal static SecurityStatus DecryptMessage(SSPIInterface secModule, SafeDeleteContext securityContext, byte[] buffer, ref int offset, ref int count)
        {
            return secModule.DecryptMessage(securityContext, buffer, ref offset, ref count);
        }

        internal static SafeFreeContextBufferChannelBinding QueryContextChannelBinding(SSPIInterface SecModule, SafeDeleteContext securityContext, ChannelBindingKind contextAttribute)
        {
            GlobalLog.Enter("QueryContextChannelBinding", contextAttribute.ToString());

            SafeFreeContextBufferChannelBinding result;

            int errorCode = SecModule.QueryContextChannelBinding(securityContext, contextAttribute, out result);

            if (result != null)
            {
                GlobalLog.Leave("QueryContextChannelBinding", Logging.HashString(result));
            }

            return result;
        }

        internal static int QueryContextStreamSizes(SSPIInterface SecModule, SafeDeleteContext securityContext, out StreamSizes streamSizes)
        {
            return SecModule.QueryContextStreamSizes(securityContext, out streamSizes);
        }

        internal static int QueryContextConnectionInfo(SSPIInterface SecModule, SafeDeleteContext securityContext, out SslConnectionInfo connectionInfo)
        {
            return SecModule.QueryContextConnectionInfo(securityContext, out connectionInfo);
        }

        internal static int QueryContextRemoteCertificate(SSPIInterface SecModule, SafeDeleteContext securityContext, out SafeFreeCertContext remoteCertificate)
        {
            return SecModule.QueryContextRemoteCertificate(securityContext, out remoteCertificate);
        }

        internal static int QueryContextIssuerList(SSPIInterface SecModule, SafeDeleteContext securityContext, out Object issuerList)
        {
            return SecModule.QueryContextIssuerList(securityContext, out issuerList);
        }
    }
}
