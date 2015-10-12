// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Runtime.InteropServices;

partial class Interop
{
    partial class mincore
    {
        /// <summary>
        /// WARNING: This method does not implicitly handle long paths. Use GetFullPath/PathHelper.
        /// </summary>
        [DllImport(Libraries.CoreFile_L1, SetLastError = true, CharSet = CharSet.Unicode, BestFitMapping = false, ExactSpelling = true)]
        internal static extern int GetLongPathNameW(char[] path, [Out]StringBuilder longPathBuffer, int bufferLength);
    }
}
