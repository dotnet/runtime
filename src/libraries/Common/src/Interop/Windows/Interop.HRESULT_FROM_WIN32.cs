// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;

internal partial class Interop
{
    // Implementation of HRESULT_FROM_WIN32 macro
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int HRESULT_FROM_WIN32(int errorCode)
    {
        if ((errorCode & 0x80000000) == 0x80000000)
        {
            return errorCode;
        }

        return (errorCode & 0x0000FFFF) | unchecked((int)0x80070000);
    } 
}
