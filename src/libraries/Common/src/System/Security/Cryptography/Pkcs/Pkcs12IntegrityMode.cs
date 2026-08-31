// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Security.Cryptography.Pkcs
{
#if BUILDING_PKCS
    public
#else
    internal
#endif
    enum Pkcs12IntegrityMode
    {
        Unknown,
        None,
        Password,
        PublicKey,
    }
}
