// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

internal static partial class Interop
{
    internal static partial class Sys
    {
        /// <summary>
        /// Tries to get the group name associated to the specified group ID.
        /// </summary>
        /// <param name="gid">The group ID.</param>
        /// <param name="groupName">When this method returns true, gets the value of the group name associated with the specified id. On failure, it is null.</param>
        /// <returns>On success, returns true. On failure, returns false.</returns>
        internal static bool TryGetGroupName(uint gid, [NotNullWhen(returnValue: true)] out string? groupName)
        {
            groupName = GetGroupName(gid);
            return groupName != null;
        }

        [LibraryImport(Libraries.SystemNative, EntryPoint = "SystemNative_GetGroupName", StringMarshalling = StringMarshalling.Utf8, SetLastError = true)]
        private static unsafe partial string? GetGroupName(uint uid);
    }
}
