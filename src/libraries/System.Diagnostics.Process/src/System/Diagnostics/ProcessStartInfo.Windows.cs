// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.Versioning;
using System.Security;

namespace System.Diagnostics
{
    public sealed partial class ProcessStartInfo
    {
        private string? _domain;

        [SupportedOSPlatform("windows")]
        public string? PasswordInClearText { get; set; }

        [SupportedOSPlatform("windows")]
        [AllowNull]
        public string Domain
        {
            get => _domain ?? string.Empty;
            set => _domain = value;
        }

        [SupportedOSPlatform("windows")]
        public bool LoadUserProfile { get; set; }

        /// <summary>
        /// Gets or sets a value that indicates whether the user credentials
        /// are only to be used for network resources.
        /// </summary>
        /// <value><c>true</c> if the user credentials are only to be used for
        /// network resources.</value>
        /// <remarks>
        /// <para>This property is referenced if the process is being started
        /// by using the user name, password, and domain.</para>
        /// <para>If the value is <c>true</c>, the process is started with the
        /// caller's identity. The system creates a new logon session with
        /// the given credentials, which is used on the network only.</para>
        /// <para>The system does not validate the specified credentials. Therefore,
        /// the process can start, but it may not have access to network resources.</para>
        /// </remarks>
        [SupportedOSPlatform("windows")]
        public bool UseCredentialsForNetworkingOnly { get; set; }

        [CLSCompliant(false)]
        [SupportedOSPlatform("windows")]
        public SecureString? Password { get; set; }
    }
}
