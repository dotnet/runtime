// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;

internal partial class Interop
{
    internal partial class mincore
    {
        [DllImport(Libraries.Kernel32_L2, CharSet = CharSet.Unicode, EntryPoint = "GetComputerNameW")]
        private extern static int GetComputerName(char[] lpBuffer, ref uint nSize);

        private const int MacMachineNameLength = 256;

        internal static string GetComputerName()
        {
            char[] buffer = new char[MacMachineNameLength];
            uint length = (uint)buffer.Length;

            Interop.mincore.GetComputerName(buffer, ref length);
            return new string(buffer, 0, (int)length);
        }

    }
}
