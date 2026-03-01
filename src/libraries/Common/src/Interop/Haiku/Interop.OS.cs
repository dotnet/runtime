// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

#pragma warning disable CA1823 // analyzer incorrectly flags fixed buffer length const (https://github.com/dotnet/roslyn/issues/37593)

internal static partial class Interop
{
    internal static partial class OS
    {
        internal const int B_OS_NAME_LENGTH = 32;

        [LibraryImportAttribute(Interop.Libraries.libroot, SetLastError = false)]
        private static unsafe partial int _get_next_area_info(int team, ref nint cookie, out area_info areaInfo, nuint size);

        [StructLayout(LayoutKind.Sequential)]
        internal unsafe struct area_info
        {
            public int area;
            public fixed byte name[B_OS_NAME_LENGTH];
            public nuint size;
            public uint @lock;
            public uint protection;
            public int team;
            public uint ram_size;
            public uint copy_count;
            public uint in_count;
            public uint out_count;
            public void* address;
        }

        /// <summary>
        /// Gets information about areas owned by a team.
        /// </summary>
        /// <param name="team">The team ID of the areas to iterate.</param>
        /// <param name="cookie">A cookie to track the iteration.</param>
        /// <param name="info">The <see cref="area_info"/> structure to fill in.</param>
        /// <returns>Returns 0 on success. Returns an error code on failure or when there are no more areas to iterate.</returns>
        internal static unsafe int GetNextAreaInfo(int team, ref nint cookie, out area_info info)
        {
            return _get_next_area_info(team, ref cookie, out info, (nuint)sizeof(area_info));
        }
    }
}
