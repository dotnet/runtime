// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;

namespace System.Net
{
    /// <devdoc>
    ///   <para>
    ///     Provides the base authentication interface for Web client authentication.
    ///   </para>
    /// </devdoc>
    public interface ICredentials
    {
        /// <devdoc>
        ///   <para>
        ///     Returns a NetworkCredential object that is associated with the supplied host, realm,
        ///     and authentication type. By convention, this method should return null if not information
        ///     is available for the specified host and realm.
        ///   </para>
        /// </devdoc>
        NetworkCredential GetCredential(Uri uri, string authType);
    }
}
