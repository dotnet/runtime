// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using System.Buffers;
using System.Text;
using System;
using System.Collections.Generic;
using System.Reflection;

internal static partial class Interop
{
    internal static partial class Sys
    {
        /// <summary>
        /// Gets the group name associated to the specified group ID.
        /// </summary>
        /// <param name="gid">The group ID.</param>
        /// <returns>On success, return a string with the group name. On failure, throws an IOException.</returns>
        internal static string GetGroupName(uint gid) => GetGroupNameInternal(gid) ?? throw GetIOException(GetLastErrorInfo());

        [LibraryImport(Libraries.SystemNative, EntryPoint = "SystemNative_GetGroupName", StringMarshalling = StringMarshalling.Utf8, SetLastError = true)]
        private static unsafe partial string? GetGroupNameInternal(uint uid);
    }
}
