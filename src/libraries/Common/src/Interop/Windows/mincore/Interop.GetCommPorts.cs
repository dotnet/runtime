// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;

internal partial class Interop
{
    internal partial class mincore
    {
        [DllImport(Libraries.CoreComm_L1_1_2, SetLastError = true)]
        unsafe private static extern int GetCommPorts(
             uint* lpPortNumbers,
             uint uPortNumbersCount,
             out uint puPortNumbersFound);

        unsafe internal static int GetCommPorts(
            Span<uint> portNumbers,
            out uint portNumbersFound)
        {
            fixed (uint* portNumbersBuffer = &MemoryMarshal.GetReference(portNumbers))
            {
                return GetCommPorts(portNumbersBuffer, (uint)portNumbers.Length, out portNumbersFound);
            }
        }
    }
}
