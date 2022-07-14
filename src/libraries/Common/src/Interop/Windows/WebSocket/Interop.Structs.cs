// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Net.WebSockets;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

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

        [NativeMarshalling(typeof(Marshaller))]
        internal struct HttpHeader
        {
            internal string Name;
            internal uint NameLength;
            internal string Value;
            internal uint ValueLength;

            [CustomMarshaller(typeof(HttpHeader), MarshalMode.ManagedToUnmanagedIn, typeof(Marshaller))]
            [CustomMarshaller(typeof(HttpHeader), MarshalMode.ElementIn, typeof(Marshaller))]
            public static class Marshaller
            {
                public static Native ConvertToUnmanaged(HttpHeader managed)
                {
                    Native n;
                    n.Name = Marshal.StringToCoTaskMemAnsi(managed.Name);
                    n.NameLength = managed.NameLength;
                    n.Value = Marshal.StringToCoTaskMemAnsi(managed.Value);
                    n.ValueLength = managed.ValueLength;
                    return n;
                }

                public static void Free(Native n)
                {
                    Marshal.FreeCoTaskMem(n.Name);
                    Marshal.FreeCoTaskMem(n.Value);
                }

                internal struct Native
                {
                    public IntPtr Name;
                    public uint NameLength;
                    public IntPtr Value;
                    public uint ValueLength;
                }
            }
        }
    }
}
