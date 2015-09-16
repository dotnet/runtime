// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Sys
    {
        internal unsafe struct Passwd
        {
            internal byte* Name;
            internal byte* Password;
            internal uint  UserId;
            internal uint  GroupId;
            internal byte* UserInfo;
            internal byte* HomeDirectory;
            internal byte* Shell;
        };

        [DllImport(Libraries.SystemNative, SetLastError = false)]
        internal static extern unsafe int GetPwUidR(uint uid, out Passwd pwd, byte* buf, int bufLen);
    }
}
