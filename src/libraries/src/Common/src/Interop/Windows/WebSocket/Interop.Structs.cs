// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Net.WebSockets;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class WebSocket
    {
        [StructLayout(LayoutKind.Explicit)]
        internal struct Buffer
        {
            [FieldOffset(0)]
            internal DataBuffer Data;
            [FieldOffset(0)]
            internal CloseBuffer CloseStatus;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct Property
        {
            internal WebSocketProtocolComponent.PropertyType Type;
            internal IntPtr PropertyData;
            internal uint PropertySize;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct DataBuffer
        {
            internal IntPtr BufferData;
            internal uint BufferLength;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct CloseBuffer
        {
            internal IntPtr ReasonData;
            internal uint ReasonLength;
            internal ushort CloseStatus;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct HttpHeader
        {
            [MarshalAs(UnmanagedType.LPStr)]
            internal string Name;
            internal uint NameLength;
            [MarshalAs(UnmanagedType.LPStr)]
            internal string Value;
            internal uint ValueLength;
        }
    }
}
