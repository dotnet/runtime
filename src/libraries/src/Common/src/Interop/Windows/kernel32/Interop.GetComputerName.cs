// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;

internal partial class Interop
{
    internal partial class Kernel32
    {
        [DllImport(Libraries.Kernel32, CharSet = CharSet.Unicode, EntryPoint = "GetComputerNameW")]
        private extern static int GetComputerName(char[] lpBuffer, ref uint nSize);

        private const int MacMachineNameLength = 256;

        internal static string GetComputerName()
        {
            char[] buffer = new char[MacMachineNameLength];
            uint length = (uint)buffer.Length;

            Interop.Kernel32.GetComputerName(buffer, ref length);
            return new string(buffer, 0, (int)length);
        }

    }
}
