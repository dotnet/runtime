// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Kernel32
    {
        /// <summary>
        /// WARNING: This method does not implicitly handle long paths. Use SetFileAttributes.
        /// </summary>
#if DLLIMPORTGENERATOR_ENABLED
        [GeneratedDllImport(Libraries.Kernel32, EntryPoint = "SetFileAttributesW", CharSet = CharSet.Unicode, SetLastError = true)]
        private static partial bool SetFileAttributesPrivate(
#else
        [DllImport(Libraries.Kernel32, EntryPoint = "SetFileAttributesW", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool SetFileAttributesPrivate(
#endif
            string name,
            int attr);

        internal static bool SetFileAttributes(string name, int attr)
        {
            name = PathInternal.EnsureExtendedPrefixIfNeeded(name);
            return SetFileAttributesPrivate(name, attr);
        }
    }
}
