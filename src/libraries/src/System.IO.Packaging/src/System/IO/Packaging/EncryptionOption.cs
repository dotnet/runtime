// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.IO.Packaging
{
    /// <summary>
    /// This class is used to control Encryption RM for package parts.  
    /// </summary>
    public enum EncryptionOption : int
    {
        /// <summary>
        /// Encryption is turned off in this mode. This is not supported.
        /// </summary>
        None = 0,

        /// <summary>
        /// RightsManagement is the only supported option right now.
        /// </summary>
        RightsManagement = 1,
    }
}
