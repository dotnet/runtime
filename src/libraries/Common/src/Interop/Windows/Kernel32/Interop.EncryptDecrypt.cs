// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Runtime.InteropServices;

internal partial class Interop
{
    internal partial class Kernel32
    {
        /// <summary>
        /// WARNING: This method does not implicitly handle long paths. Use EncryptFile.
        /// </summary>
        [DllImport(Libraries.Advapi32, EntryPoint = "EncryptFileW", SetLastError = true, CharSet = CharSet.Unicode, BestFitMapping = false)]
        private static extern bool EncryptFilePrivate(string lpFileName);

        internal static bool EncryptFile(string path)
        {
            path = PathInternal.EnsureExtendedPrefixIfNeeded(path);
            return EncryptFilePrivate(path);
        }

        /// <summary>
        /// WARNING: This method does not implicitly handle long paths. Use DecryptFile.
        /// </summary>
        [DllImport(Libraries.Advapi32, EntryPoint = "DecryptFileW", SetLastError = true, CharSet = CharSet.Unicode, BestFitMapping = false)]
        private static extern bool DecryptFileFilePrivate(string lpFileName, int dwReserved);

        internal static bool DecryptFile(string path)
        {
            path = PathInternal.EnsureExtendedPrefixIfNeeded(path);
            return DecryptFileFilePrivate(path, 0);
        }
    }
}
