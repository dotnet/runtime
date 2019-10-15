// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Internal.Cryptography.Pal.Windows
{
    internal enum AlgId : int
    {
        CALG_RSA_KEYX = 0x0000a400,
        CALG_DH_SF = 0x0000aa01,
        CALG_DH_EPHEM = 0x0000aa02,
        CALG_RC2 = 0x00006602,
        CALG_RC4 = 0x00006801,
        CALG_DES = 0x00006601,
        CALG_3DES = 0x00006603,
    }
}
