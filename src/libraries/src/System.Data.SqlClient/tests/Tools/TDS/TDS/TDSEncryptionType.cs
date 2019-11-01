// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.SqlServer.TDS
{
    /// <summary>
    /// Types of encryption TDS clients and servers supports
    /// </summary>
    public enum TDSEncryptionType
    {
        /// <summary>
        /// No transport encryption
        /// </summary>
        Off,

        /// <summary>
        /// Encryption of the login packet only
        /// </summary>
        LoginOnly,

        /// <summary>
        /// Encryption of the entire session
        /// </summary>
        Full
    }
}
