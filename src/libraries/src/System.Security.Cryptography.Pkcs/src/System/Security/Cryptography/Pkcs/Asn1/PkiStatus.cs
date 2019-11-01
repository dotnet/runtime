// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Security.Cryptography.Pkcs.Asn1
{
    // https://tools.ietf.org/html/rfc4210#section-5.2.3
    internal enum PkiStatus
    {
        Granted = 0,
        GrantedWithMods = 1,
        Rejection = 2,
        Waiting = 3,
        RevocationWarning = 4,
        RevocationNotification = 5,
        KeyUpdateWarning = 6,
    }
}
