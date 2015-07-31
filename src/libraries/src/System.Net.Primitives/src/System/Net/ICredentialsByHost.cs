// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;

namespace System.Net
{
    /// <devdoc>
    ///   <para>
    ///     Provides the base authentication interface for Web client authentication.
    ///   </para>
    /// </devdoc>
    public interface ICredentialsByHost
    {
        /// <devdoc>
        ///   <para>
        ///     Returns a NetworkCredential object that is associated with the supplied host, realm,
        ///     and authentication type. By convention, this method should return null if not information
        ///     is available for the specified host and realm.
        ///   </para>
        /// </devdoc>
        NetworkCredential GetCredential(string host, int port, string authenticationType);
    }
}
