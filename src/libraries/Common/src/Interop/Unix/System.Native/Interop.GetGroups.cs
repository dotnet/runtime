// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Sys
    {
        internal static unsafe uint[]? GetGroups()
        {
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
                    return groups.Slice(0, rv).ToArray();
                }
                else if (rv == -1 && Interop.Sys.GetLastError() == Interop.Error.EINVAL)
                {
                    // increase buffer size
                    groups = new uint[groups.Length * 2];
                }
                else
                {
                    // failure
                    return null;
                }
            }
            while (true);
        }

        [DllImport(Libraries.SystemNative, EntryPoint = "SystemNative_GetGroups", SetLastError = true)]
        private static extern unsafe int GetGroups(int ngroups, uint* groups);
    }
}
