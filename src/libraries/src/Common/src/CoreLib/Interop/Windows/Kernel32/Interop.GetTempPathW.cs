// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;

internal partial class Interop
{
    internal partial class Kernel32
    {
        [DllImport(Libraries.Kernel32, CharSet = CharSet.Unicode, BestFitMapping = false)]
#if PROJECTN
        internal static extern unsafe uint GetTempPathW(int bufferLen, char* buffer);

        // Works around https://devdiv.visualstudio.com/web/wi.aspx?pcguid=011b8bdf-6d56-4f87-be0d-0092136884d9&id=575202
        internal static unsafe uint GetTempPathW(int bufferLen, ref char buffer)
        {
            fixed (char* pbuffer = &buffer)
                return GetTempPathW(bufferLen, pbuffer);
        }
#else
        internal static extern uint GetTempPathW(int bufferLen, ref char buffer);
#endif
    }
}
