// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;

internal partial class Interop
{
    internal partial class Kernel32
    {
        /// <summary>
        /// WARNING: This method does not implicitly handle long paths. Use GetFullPath/PathHelper.
        /// </summary>
        [DllImport(Libraries.Kernel32, SetLastError = true, CharSet = CharSet.Unicode, BestFitMapping = false, ExactSpelling = true)]
#if PROJECTN
        internal static extern unsafe uint GetLongPathNameW(char* lpszShortPath, char* lpszLongPath, uint cchBuffer);

        // Works around https://devdiv.visualstudio.com/web/wi.aspx?pcguid=011b8bdf-6d56-4f87-be0d-0092136884d9&id=575202
        internal static unsafe uint GetLongPathNameW(ref char lpszShortPath, ref char lpszLongPath, uint cchBuffer)
        {
            fixed (char* plpszLongPath = &lpszLongPath)
            fixed (char* plpszShortPath = &lpszShortPath)
                return GetLongPathNameW(plpszShortPath, plpszLongPath, cchBuffer);
        }
#else
        internal static extern uint GetLongPathNameW(ref char lpszShortPath, ref char lpszLongPath, uint cchBuffer);
#endif
    }
}
