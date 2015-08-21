// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;

namespace System.Net.Sockets
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal struct hostent
    {
        public IntPtr h_name;
        public IntPtr h_aliases;
        public short h_addrtype;
        public short h_length;
        public IntPtr h_addr_list;
    }
} // namespace System.Net.Sockets
