// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Net
{
    // Abstraction for credentials in password-based
    // authentication schemes (basic, digest, NTLM, Kerberos)
    // Note this is not applicable to public-key based
    // systems such as SSL client authentication
    // "Password" here may be the clear text password or it
    // could be a one-way hash that is sufficient to
    // authenticate, as in HTTP/1.1 digest.

    //
    // Object representing default credentials
    //
    internal class SystemNetworkCredential : NetworkCredential
    {
        internal static readonly SystemNetworkCredential defaultCredential = new SystemNetworkCredential();

        // We want reference equality to work.  Making this private is a good way to guarantee that.
        private SystemNetworkCredential() :
            base(string.Empty, string.Empty, string.Empty)
        {
        }
    }
}

