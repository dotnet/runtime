// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

internal static partial class Interop
{
    internal static partial class Crypt32
    {
        internal enum CertIdChoice : int
        {
            CERT_ID_ISSUER_SERIAL_NUMBER = 1,
            CERT_ID_KEY_IDENTIFIER = 2,
            CERT_ID_SHA1_HASH = 3,
        }
    }
}
