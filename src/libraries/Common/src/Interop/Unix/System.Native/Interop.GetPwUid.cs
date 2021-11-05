// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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

        [GeneratedDllImport(Libraries.SystemNative, EntryPoint = "SystemNative_GetPwUidR", SetLastError = false)]
        internal static unsafe partial int GetPwUidR(uint uid, out Passwd pwd, byte* buf, int bufLen);

        [GeneratedDllImport(Libraries.SystemNative, EntryPoint = "SystemNative_GetPwNamR", CharSet = CharSet.Ansi, SetLastError = false)]
        internal static unsafe partial int GetPwNamR(string name, out Passwd pwd, byte* buf, int bufLen);
    }
}
