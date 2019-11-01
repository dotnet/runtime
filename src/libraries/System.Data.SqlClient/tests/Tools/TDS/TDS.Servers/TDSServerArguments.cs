// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Security.Cryptography.X509Certificates;

using Microsoft.SqlServer.TDS.PreLogin;

namespace Microsoft.SqlServer.TDS.Servers
{
    /// <summary>
    /// Common arguments for TDS Server
    /// </summary>
    public class TDSServerArguments
    {
        /// <summary>
        /// Service Principal Name, representing Azure SQL Database in Azure Active Directory.
        /// </summary>
        public const string AzureADServicePrincipalName = @"https://database.windows.net/";

        /// <summary>
        /// The Azure Active Directory production token endpoint to re-direct the client to fetch a token from.
        /// </summary>
        public const string AzureADProductionTokenEndpoint = @"https://login.windows.net/common";

        /// <summary>
        /// Log to which send TDS conversation
        /// </summary>
        public TextWriter Log { get; set; }

        /// <summary>
        /// Server name
        /// </summary>
        public string ServerName { get; set; }

        /// <summary>
        /// Server version
        /// </summary>
        public Version ServerVersion { get; set; }

        /// <summary>
        /// Server principal name
        /// </summary>
        public string ServerPrincipalName { get; set; }

        /// <summary>
        /// Sts Url
        /// </summary>
        public string StsUrl { get; set; }

        /// <summary>
        /// Size of the TDS packet server should operate with
        /// </summary>
        public int PacketSize { get; set; }

        /// <summary>
        /// Transport encryption
        /// </summary>
        public TDSPreLoginTokenEncryptionType Encryption { get; set; }

        /// <summary>
        /// Specifies the FedAuthRequired option
        /// </summary>
        public TdsPreLoginFedAuthRequiredOption FedAuthRequiredPreLoginOption { get; set; }

        /// <summary>
        /// Certificate to use for transport encryption
        /// </summary>
        public X509Certificate EncryptionCertificate { get; set; }

        /// <summary>
        /// Initialization constructor
        /// </summary>
        public TDSServerArguments()
        {
            // Assign default server version
            ServerName = Environment.MachineName;
            ServerVersion = new Version(11, 0, 1083);

            // Default packet size
            PacketSize = 4096;

            // By default we don't support encryption
            Encryption = TDSPreLoginTokenEncryptionType.NotSupported;

            // By Default SQL authentication will be used.
            FedAuthRequiredPreLoginOption = TdsPreLoginFedAuthRequiredOption.FedAuthNotRequired;

            EncryptionCertificate = new X509Certificate2("TdsServerCertificate.pfx", "SecretPassword123456");

            ServerPrincipalName = AzureADServicePrincipalName;
            StsUrl = AzureADProductionTokenEndpoint;
        }
    }
}
