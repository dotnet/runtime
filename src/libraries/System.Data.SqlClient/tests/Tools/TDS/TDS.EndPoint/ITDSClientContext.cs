// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;

namespace Microsoft.SqlServer.TDS.EndPoint
{
    /// <summary>
    /// Run time context of the TDS client
    /// </summary>
    public interface ITDSClientContext
    {
        /// <summary>
        /// Host or IP address on which SQL Server is running
        /// </summary>
        string ServerHost { get; set; }

        /// <summary>
        /// Port number of the SQL Server
        /// </summary>
        uint ServerPort { get; set; }

        /// <summary>
        /// Pipe at which SQL Server is listening
        /// </summary>
        string ServerPipe { get; set; }

        /// <summary>
        /// Name of the server client connected to.
        /// Information is coming from INFO and ERROR tokens
        /// </summary>
        string ServerName { get; }

        /// <summary>
        /// Description of the server the client connected to.
        /// Information originates in LOGINACK token
        /// </summary>
        string ServerDescription { get; }

        /// <summary>
        /// Server build version
        /// </summary>
        Version ServerVersion { get; }

        /// <summary>
        /// TDS version of the conversation
        /// </summary>
        Version TDSVersion { get; }

        /// <summary>
        /// Current database
        /// </summary>
        string Database { get; }

        /// <summary>
        /// Current language
        /// </summary>
        string Language { get; }

        /// <summary>
        /// Database collation
        /// </summary>
        byte[] Collation { get; }

        /// <summary>
        /// Size of the TDS packet
        /// </summary>
        uint PacketSize { get; set; }

        /// <summary>
        /// Failover partner for connected SQL Server
        /// </summary>
        string FailoverPartner { get; }

        /// <summary>
        /// Encryption used on the session
        /// </summary>
        TDSEncryptionType Encryption { get; set; }

        /// <summary>
        /// Query text to be sent to the TDS server
        /// </summary>
        string Query { get; set; }

        /// <summary>
        /// Respose to the query
        /// </summary>
        IList<object[]> QueryResponse { get; set; }

        /// <summary>
        /// Connection identifier
        /// </summary>
        Guid ConnectionID { get; }

        /// <summary>
        /// State of the virtual session
        /// </summary>
        object SessionState { get; }
    }
}
