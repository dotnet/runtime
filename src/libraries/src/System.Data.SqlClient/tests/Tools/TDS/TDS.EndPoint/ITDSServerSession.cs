// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Security.Cryptography.X509Certificates;

using Microsoft.SqlServer.TDS.EndPoint.SSPI;

namespace Microsoft.SqlServer.TDS.EndPoint
{
    /// <summary>
    /// Session of the TDS Server
    /// </summary>
    public interface ITDSServerSession
    {
        /// <summary>
        /// Server that created the session
        /// </summary>
        ITDSServer Server { get; }

        /// <summary>
        /// Session identifier
        /// </summary>
        uint SessionID { get; }

        /// <summary>
        /// Size of the TDS packet
        /// </summary>
        uint PacketSize { get; set; }

        /// <summary>
        /// User name if SQL authentication is used
        /// </summary>
        string SQLUserID { get; set; }

        /// <summary>
        /// Context that indicates the stage of SSPI authentication
        /// </summary>
        SSPIContext NTUserAuthenticationContext { get; set; }

        /// <summary>
        /// Database to which connection is established
        /// </summary>
        string Database { get; set; }

        /// <summary>
        /// TDS version of the communication
        /// </summary>
        Version TDSVersion { get; set; }

        /// <summary>
        /// Local connection end-point information
        /// </summary>
        TDSEndPointInfo ServerEndPointInfo { get; set; }

        /// <summary>
        /// Remote connection end-point information
        /// </summary>
        TDSEndPointInfo ClientEndPointInfo { get; set; }

        /// <summary>
        /// Transport encryption
        /// </summary>
        TDSEncryptionType Encryption { get; set; }

        /// <summary>
        /// Certificate to use for encryption
        /// </summary>
        X509Certificate EncryptionCertificate { get; }

        /// <summary>
        /// Counter of connection reset requests for this session
        /// </summary>
        int ConnectionResetRequestCount { get; set; }

        /// <summary>
        /// Indicates whether this session supports transport-level recovery
        /// </summary>
        bool IsSessionRecoveryEnabled { get; set; }
    }
}
