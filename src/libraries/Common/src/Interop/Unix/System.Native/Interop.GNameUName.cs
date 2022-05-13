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
        internal static string GetGName(uint gid)
        {
            string result = GetGNameInternal(gid);
            return (result == null) ? throw GetIOException(GetLastErrorInfo()) : result;
        }

        /// <summary>
        /// Gets the user name associated to the specified user ID.
        /// </summary>
        /// <param name="uid">The user ID.</param>
        /// <returns>On success, return a string with the user name. On failure, throws an IOException.</returns>
        internal static string GetUName(uint uid)
        {
            string result = GetUNameInternal(uid);
            return (result == null) ? throw GetIOException(GetLastErrorInfo()) : result;
        }

        [LibraryImport(Libraries.SystemNative, EntryPoint = "SystemNative_GetGName", StringMarshalling = StringMarshalling.Utf8, SetLastError = true)]
        private static unsafe partial string GetGNameInternal(uint uid);

        [LibraryImport(Libraries.SystemNative, EntryPoint = "SystemNative_GetUName", StringMarshalling = StringMarshalling.Utf8, SetLastError = true)]
        private static unsafe partial string GetUNameInternal(uint uid);
    }
}
