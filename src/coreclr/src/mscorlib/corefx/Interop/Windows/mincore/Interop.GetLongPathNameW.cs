// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;

partial class Interop
{
    partial class mincore
    {
        /// <summary>
        /// WARNING: This method does not implicitly handle long paths. Use GetFullPath/PathHelper.
        /// </summary>
        [DllImport(Libraries.CoreFile_L1, SetLastError = true, CharSet = CharSet.Unicode, BestFitMapping = false, ExactSpelling = true)]
        internal static extern uint GetLongPathNameW(char[] lpszShortPath, char[] lpszLongPath, uint cchBuffer);
    }
}
