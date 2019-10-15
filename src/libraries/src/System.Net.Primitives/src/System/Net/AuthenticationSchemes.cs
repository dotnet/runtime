// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
