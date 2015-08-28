// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using System.Text;
using System.Runtime.InteropServices;

partial class Interop
{
    partial class mincore
    {
        /// <summary>
        /// WARNING: This method does not implicitly handle long paths. Use GetLongPathName.
        /// </summary>
        [DllImport(Libraries.CoreFile_L1, EntryPoint = "GetLongPathNameW", SetLastError = true, CharSet = CharSet.Unicode, BestFitMapping = false)]
        internal unsafe static extern int GetLongPathNameUnsafe(char* path, char* longPathBuffer, int bufferLength);

        /// <summary>
        /// WARNING: This method does not implicitly handle long paths. Use GetLongPathName.
        /// </summary>
        [DllImport(Libraries.CoreFile_L1, EntryPoint = "GetLongPathNameW", SetLastError = true, CharSet = CharSet.Unicode, BestFitMapping = false)]
        private static extern int GetLongPathNamePrivate(string path, [Out]StringBuilder longPathBuffer, int bufferLength);

        internal static int GetLongPathName(string path, [Out]StringBuilder longPathBuffer, int bufferLength)
        {
            bool wasExtended = PathInternal.IsExtended(path);
            if (!wasExtended)
            {
                path = PathInternal.EnsureExtendedPrefixOverMaxPath(path);
            }
            int result = GetLongPathNamePrivate(path, longPathBuffer, longPathBuffer.Capacity);

            if (!wasExtended)
            {
                // We don't want to give back \\?\ if we possibly added it ourselves
                PathInternal.RemoveExtendedPrefix(longPathBuffer);
            }
            return result;
        }
    }
}
