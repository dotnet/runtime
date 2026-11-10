// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class OS
    {
        [StructLayout(LayoutKind.Sequential)]
        internal unsafe struct AreaInfo
        {
            public nuint size;
            public uint ram_size;
        }

        /// <summary>
        /// Gets information about areas owned by a team.
        /// </summary>
        /// <param name="team">The team ID of the areas to iterate.</param>
        /// <param name="cookie">A cookie to track the iteration.</param>
        /// <param name="areaInfo">The <see cref="AreaInfo"/> structure to fill in.</param>
        /// <returns>Returns 0 on success. Returns an error code on failure or when there are no more areas to iterate.</returns>
        [LibraryImport(Libraries.SystemNative, EntryPoint = "SystemNative_GetNextAreaInfo")]
        internal static unsafe partial int GetNextAreaInfo(int team, ref nint cookie, out AreaInfo areaInfo);
    }
}
