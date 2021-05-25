// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Kernel32
    {
        [DllImport(Libraries.Kernel32, EntryPoint = "GetComputerNameExW", SetLastError = true, CharSet = CharSet.Unicode, ExactSpelling = true)]
        internal static extern unsafe Interop.BOOL GetComputerNameEx(COMPUTER_NAME_FORMAT NameType, char *lpBuffer, uint *nSize);

        internal enum COMPUTER_NAME_FORMAT : uint
        {
            ComputerNameNetBIOS = 0,
            ComputerNameDnsHostname = 1,
            ComputerNameDnsDomain = 2,
            ComputerNameDnsFullyQualified = 3,
            ComputerNamePhysicalNetBIOS = 4,
            ComputerNamePhysicalDnsHostname = 5,
            ComputerNamePhysicalDnsDomainname = 6,
            ComputerNamePhysicalDnsFullyQualified = 7,
            ComputerNameMax
        }
    }
}
