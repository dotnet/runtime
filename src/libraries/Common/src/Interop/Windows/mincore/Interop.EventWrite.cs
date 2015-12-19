// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class mincore
    {
        [DllImport(Interop.Libraries.Eventing)]
        internal static extern uint EventWrite(ulong RegHandle, ref _EVENT_DESCRIPTOR EventDescriptor, uint UserDataCount, IntPtr UserData);

        [StructLayout(LayoutKind.Sequential)]
        internal struct _EVENT_DESCRIPTOR
        {
            internal ushort Id;
            internal byte Version;
            internal byte Channel;
            internal byte Level;
            internal byte Opcode;
            internal ushort Task;
            internal ulong Keyword;
        }
    }
}
