// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Security.Cryptography.Pkcs
{
#if BUILDING_PKCS
    public
#else
    internal
#endif
    enum Pkcs12ConfidentialityMode
    {
        Unknown = 0,
        None = 1,
        Password = 2,
        PublicKey = 3,
    }
}
