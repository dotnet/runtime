// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

internal static partial class Interop
{
    internal static partial class libc
    {
        public const int HOST_NOT_FOUND = 1;
        public const int TRY_AGAIN = 2;
        public const int NO_RECOVERY = 3;
        public const int NO_DATA = 4;
        public const int NO_ADDRESS = NO_DATA;

        // Disable CS0169 (The field 'Interop.libc.hostent.h_length' is never used) and CS0649
        // (Field 'Interop.libc.hostent.sa_family' is never assigned to, and will always have its
        // default value 0)
#pragma warning disable 169, 649
        public unsafe struct hostent
        {
            public byte* h_name;       // Official bane of host
            public byte** h_aliases;   // Alias list
            public int h_addrtype;     // Host address type
            public int h_length;       // Length of address
            public byte** h_addr_list; // List of addresses from name server
        }
#pragma warning restore 169, 649
    }
}
