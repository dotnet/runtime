// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

internal static partial class Interop
{
    internal static partial class Crypt32
    {
        internal enum CryptKeySpec : int
        {
            AT_KEYEXCHANGE = 1,
            AT_SIGNATURE = 2,
            CERT_NCRYPT_KEY_SPEC = -1,  // Special sentinel indicating that an api returned an NCrypt key rather than a CAPI key
        }
    }
}
