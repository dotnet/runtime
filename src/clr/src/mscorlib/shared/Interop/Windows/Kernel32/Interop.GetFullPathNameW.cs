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
        /// WARNING: This method does not implicitly handle long paths. Use GetFullPathName or PathHelper.
        /// </summary>
        [DllImport(Libraries.Kernel32, SetLastError = true, CharSet = CharSet.Unicode, BestFitMapping = false, ExactSpelling = true)]
#if PROJECTN
        internal static extern unsafe uint GetFullPathNameW(string path, uint numBufferChars, char* buffer, IntPtr mustBeZero);

        // Works around https://devdiv.visualstudio.com/web/wi.aspx?pcguid=011b8bdf-6d56-4f87-be0d-0092136884d9&id=575202
        internal static unsafe uint GetFullPathNameW(string path, uint numBufferChars, ref char buffer, IntPtr mustBeZero)
        {
            fixed (char* pBuffer = &buffer)
                return GetFullPathNameW(path, numBufferChars, pBuffer, mustBeZero);
        }
#else
        internal static extern uint GetFullPathNameW(string path, uint numBufferChars, ref char buffer, IntPtr mustBeZero);
#endif
    }
}
