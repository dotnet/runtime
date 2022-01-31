// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Sys
    {
        internal static unsafe bool IsMemberOfGroup(uint gid)
        {
            if (gid == GetEGid())
            {
                return true;
            }

            const int InitialGroupsLength =
#if DEBUG
                1;
#else
                64;
#endif
            Span<uint> groups = stackalloc uint[InitialGroupsLength];
            do
            {
                int rv;
                fixed (uint* pGroups = groups)
                {
                    rv = Interop.Sys.GetGroups(groups.Length, pGroups);
                }

                if (rv >= 0)
                {
                    // success
                    return groups.Slice(0, rv).IndexOf(gid) >= 0;
                }
                else if (rv == -1 && Interop.Sys.GetLastError() == Interop.Error.EINVAL)
                {
                    // increase buffer size
                    groups = new uint[groups.Length * 2];
                }
                else
                {
                    // failure (unexpected)
                    return false;
                }
            }
            while (true);
        }

        [GeneratedDllImport(Libraries.SystemNative, EntryPoint = "SystemNative_GetEGid")]
        private static partial uint GetEGid();

        [GeneratedDllImport(Libraries.SystemNative, EntryPoint = "SystemNative_GetGroups", SetLastError = true)]
        private static unsafe partial int GetGroups(int ngroups, uint* groups);
    }
}
