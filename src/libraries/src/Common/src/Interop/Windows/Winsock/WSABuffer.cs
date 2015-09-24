// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;

namespace System.Net.Sockets
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct WSABuffer
    {
        internal int Length; // Length of Buffer
        internal IntPtr Pointer;// Pointer to Buffer
    }
}
