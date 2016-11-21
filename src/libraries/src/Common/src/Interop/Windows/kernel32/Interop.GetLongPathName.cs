// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Runtime.InteropServices;
using System.Text;

internal partial class Interop
{
    internal partial class Kernel32
    {
        /// <summary>
        /// WARNING: This method does not implicitly handle long paths. Use GetLongPathName.
        /// </summary>
        [DllImport(Libraries.Kernel32, EntryPoint = "GetLongPathNameW", SetLastError = true, CharSet = CharSet.Unicode, BestFitMapping = false, ExactSpelling = false)]
        private static extern int GetLongPathNamePrivate(string path, [Out]StringBuilder longPathBuffer, int bufferLength);

        internal static int GetLongPathName(string path, [Out]StringBuilder longPathBuffer, int bufferLength)
        {
            path = PathInternal.EnsureExtendedPrefixOverMaxPath(path);
            return GetLongPathNamePrivate(path, longPathBuffer, bufferLength);
        }
    }
}
