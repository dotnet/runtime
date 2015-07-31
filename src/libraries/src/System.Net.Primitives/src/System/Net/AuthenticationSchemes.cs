// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Net
{
    [Flags]
    public enum AuthenticationSchemes
    {
        None = 0x00000000,
        Digest = 0x00000001,
        Negotiate = 0x00000002,
        Ntlm = 0x00000004,
        Basic = 0x00000008,

        Anonymous = 0x00008000,

        IntegratedWindowsAuthentication = Negotiate | Ntlm,
    }
}
