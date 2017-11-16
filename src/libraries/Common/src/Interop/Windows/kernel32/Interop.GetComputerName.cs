// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;

internal partial class Interop
{
    internal partial class Kernel32
    {
        [DllImport(Libraries.Kernel32, CharSet = CharSet.Unicode, EntryPoint = "GetComputerNameW")]
        private static extern unsafe int GetComputerName(char* lpBuffer, ref uint nSize);

        // maximum length of the NETBIOS name (not including NULL)
        private const int MAX_COMPUTERNAME_LENGTH = 15;

        internal static unsafe string GetComputerName()
        {
            uint length = MAX_COMPUTERNAME_LENGTH + 1;
            char* buffer = stackalloc char[(int)length];

            return GetComputerName(buffer, ref length) != 0 ?
                new string(buffer, 0, checked((int)length)) :
                null;
        }
    }
}
