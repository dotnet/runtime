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
            internal int   UserId;
            internal int   GroupId;
            internal byte* UserInfo;
            internal byte* HomeDirectory;
            internal byte* Shell;
        };

        [DllImport(Libraries.SystemNative, SetLastError = true)]
        internal static extern unsafe int GetPwUid(int uid, out Passwd pwd, byte* buf, long bufLen, out IntPtr result);
    }
}
