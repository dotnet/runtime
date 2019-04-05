// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Sys
    {
        internal unsafe struct Passwd
        {
            internal const int InitialBufferSize = 256;

            internal byte* Name;
            internal byte* Password;
            internal uint  UserId;
            internal uint  GroupId;
            internal byte* UserInfo;
            internal byte* HomeDirectory;
            internal byte* Shell;
        }

        [DllImport(Libraries.SystemNative, EntryPoint = "SystemNative_GetPwUidR", SetLastError = false)]
        internal static extern unsafe int GetPwUidR(uint uid, out Passwd pwd, byte* buf, int bufLen);

        [DllImport(Libraries.SystemNative, EntryPoint = "SystemNative_GetPwNamR", SetLastError = false)]
        internal static extern unsafe int GetPwNamR(string name, out Passwd pwd, byte* buf, int bufLen);
    }
}
