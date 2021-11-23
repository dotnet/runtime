// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Advapi32
    {
        /// <summary>
        /// WARNING: This method does not implicitly handle long paths. Use EncryptFile.
        /// </summary>
        [GeneratedDllImport(Libraries.Advapi32, EntryPoint = "EncryptFileW", CharSet = CharSet.Unicode, SetLastError = true)]
        private static partial bool EncryptFilePrivate(string lpFileName);

        internal static bool EncryptFile(string path)
        {
            path = PathInternal.EnsureExtendedPrefixIfNeeded(path);
            return EncryptFilePrivate(path);
        }

        /// <summary>
        /// WARNING: This method does not implicitly handle long paths. Use DecryptFile.
        /// </summary>
        [GeneratedDllImport(Libraries.Advapi32, EntryPoint = "DecryptFileW", CharSet = CharSet.Unicode, SetLastError = true)]
        private static partial bool DecryptFileFilePrivate(
            string lpFileName,
            int dwReserved);

        internal static bool DecryptFile(string path)
        {
            path = PathInternal.EnsureExtendedPrefixIfNeeded(path);
            return DecryptFileFilePrivate(path, 0);
        }
    }
}
