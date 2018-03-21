// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;

internal partial class Interop
{
    internal partial class Kernel32
    {
        [DllImport(Libraries.Kernel32, CharSet = CharSet.Unicode, SetLastError = true, BestFitMapping = false)]
#if PROJECTN
        internal static extern unsafe uint GetTempFileNameW(char* lpPathName, string lpPrefixString, uint uUnique, char* lpTempFileName);

        // Works around https://devdiv.visualstudio.com/web/wi.aspx?pcguid=011b8bdf-6d56-4f87-be0d-0092136884d9&id=575202
        internal static unsafe uint GetTempFileNameW(ref char lpPathName, string lpPrefixString, uint uUnique, ref char lpTempFileName)
        {
            fixed (char* plpPathName = &lpPathName)
            fixed (char* plpTempFileName = &lpTempFileName)
                return GetTempFileNameW(plpPathName, lpPrefixString, uUnique, plpTempFileName);
        }
#else
        internal static extern uint GetTempFileNameW(ref char lpPathName, string lpPrefixString, uint uUnique, ref char lpTempFileName);
#endif
    }
}
