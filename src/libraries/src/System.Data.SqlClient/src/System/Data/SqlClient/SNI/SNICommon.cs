// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace System.Data.SqlClient.SNI
{
    /// <summary>
    /// SNI Asynchronous callback
    /// </summary>
    /// <param name="packet">SNI packet</param>
    /// <param name="sniErrorCode">SNI error code</param>
    internal delegate void SNIAsyncCallback(SNIPacket packet, uint sniErrorCode);

    /// <summary>
    /// SNI provider identifiers
    /// </summary>
    internal enum SNIProviders
    {
        HTTP_PROV, // HTTP Provider
        NP_PROV, // Named Pipes Provider
        SESSION_PROV, // Session Provider
        SIGN_PROV, // Sign Provider
        SM_PROV, // Shared Memory Provider
        SMUX_PROV, // SMUX Provider
        SSL_PROV, // SSL Provider
        TCP_PROV, // TCP Provider
        MAX_PROVS, // Number of providers
        INVALID_PROV // SQL Network Interfaces
    }

    /// <summary>
    /// SMUX packet header
    /// </summary>
    internal class SNISMUXHeader
    {
        public const int HEADER_LENGTH = 16;

        public byte SMID;
        public byte flags;
        public ushort sessionId;
        public uint length;
        public uint sequenceNumber;
        public uint highwater;
    }

    /// <summary>
    /// SMUX packet flags
    /// </summary>
    [Flags]
    internal enum SNISMUXFlags
    {
        SMUX_SYN = 1,       // Begin SMUX connection
        SMUX_ACK = 2,       // Acknowledge SMUX packets
        SMUX_FIN = 4,       // End SMUX connection
        SMUX_DATA = 8       // SMUX data packet
    }

    internal class SNICommon
    {
        // Each error number maps to SNI_ERROR_* in String.resx
        internal const int ConnTerminatedError = 2;
        internal const int InvalidParameterError = 5;
        internal const int ProtocolNotSupportedError = 8;
        internal const int ConnTimeoutError = 11;
        internal const int ConnNotUsableError = 19;
        internal const int InvalidConnStringError = 25;
        internal const int HandshakeFailureError = 31;
        internal const int InternalExceptionError = 35;
        internal const int ConnOpenFailedError = 40;
        internal const int ErrorSpnLookup = 44;
        internal const int LocalDBErrorCode = 50;
        internal const int MultiSubnetFailoverWithMoreThan64IPs = 47;
        internal const int MultiSubnetFailoverWithInstanceSpecified = 48;
        internal const int MultiSubnetFailoverWithNonTcpProtocol = 49;
        internal const int MaxErrorValue = 50157;
        internal const int LocalDBNoInstanceName = 51;
        internal const int LocalDBNoInstallation = 52;
        internal const int LocalDBInvalidConfig = 53;
        internal const int LocalDBNoSqlUserInstanceDllPath = 54;
        internal const int LocalDBInvalidSqlUserInstanceDllPath = 55;
        internal const int LocalDBFailedToLoadDll = 56;
        internal const int LocalDBBadRuntime = 57;

        /// <summary>
        /// Validate server certificate callback for SSL
        /// </summary>
        /// <param name="targetServerName">Server that client is expecting to connect to</param>
        /// <param name="sender">Sender object</param>
        /// <param name="cert">X.509 certificate</param>
        /// <param name="chain">X.509 chain</param>
        /// <param name="policyErrors">Policy errors</param>
        /// <returns>True if certificate is valid</returns>
        internal static bool ValidateSslServerCertificate(string targetServerName, object sender, X509Certificate cert, X509Chain chain, SslPolicyErrors policyErrors)
        {
            if (policyErrors == SslPolicyErrors.None)
            {
                return true;
            }

            if ((policyErrors & SslPolicyErrors.RemoteCertificateNameMismatch) != 0)
            {
                string certServerName = cert.Subject.Substring(cert.Subject.IndexOf('=') + 1);

                // Verify that target server name matches subject in the certificate
                if (targetServerName.Length > certServerName.Length)
                {
                    return false;
                }
                else if (targetServerName.Length == certServerName.Length)
                {
                    // Both strings have the same length, so targetServerName must be a FQDN
                    if (!targetServerName.Equals(certServerName, StringComparison.OrdinalIgnoreCase))
                    {
                        return false;
                    }
                }
                else
                {
                    if (string.Compare(targetServerName, 0, certServerName, 0, targetServerName.Length, StringComparison.OrdinalIgnoreCase) != 0)
                    {
                        return false;
                    }

                    // Server name matches cert name for its whole length, so ensure that the
                    // character following the server name is a '.'. This will avoid
                    // having server name "ab" match "abc.corp.company.com"
                    // (Names have different lengths, so the target server can't be a FQDN.)
                    if (certServerName[targetServerName.Length] != '.')
                    {
                        return false;
                    }
                }
            }
            else
            {
                // Fail all other SslPolicy cases besides RemoteCertificateNameMismatch
                return false;
            }
            return true;
        }

        /// <summary>
        /// Sets last error encountered for SNI
        /// </summary>
        /// <param name="provider">SNI provider</param>
        /// <param name="nativeError">Native error code</param>
        /// <param name="sniError">SNI error code</param>
        /// <param name="errorMessage">Error message</param>
        /// <returns></returns>
        internal static uint ReportSNIError(SNIProviders provider, uint nativeError, uint sniError, string errorMessage)
        {
            return ReportSNIError(new SNIError(provider, nativeError, sniError, errorMessage));
        }

        /// <summary>
        /// Sets last error encountered for SNI
        /// </summary>
        /// <param name="provider">SNI provider</param>
        /// <param name="sniError">SNI error code</param>
        /// <param name="sniException">SNI Exception</param>
        /// <returns></returns>
        internal static uint ReportSNIError(SNIProviders provider, uint sniError, Exception sniException)
        {
            return ReportSNIError(new SNIError(provider, sniError, sniException));
        }

        /// <summary>
        /// Sets last error encountered for SNI
        /// </summary>
        /// <param name="error">SNI error</param>
        /// <returns></returns>
        internal static uint ReportSNIError(SNIError error)
        {
            SNILoadHandle.SingletonInstance.LastError = error;
            return TdsEnums.SNI_ERROR;
        }
    }
}
