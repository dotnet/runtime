// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
