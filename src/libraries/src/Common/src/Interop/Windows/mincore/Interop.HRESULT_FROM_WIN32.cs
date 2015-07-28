// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;

internal partial class Interop
{
    internal partial class mincore
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
}
