// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal partial class Interop
{
    internal partial class mincore
    {
        [DllImport(Libraries.CoreComm_L1_1_2, SetLastError = true)]
        private static extern unsafe int GetCommPorts(
             uint* lpPortNumbers,
             uint uPortNumbersCount,
             out uint puPortNumbersFound);

        internal static unsafe int GetCommPorts(
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
