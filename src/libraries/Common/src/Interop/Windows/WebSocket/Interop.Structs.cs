// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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

        [NativeMarshalling(typeof(Native))]
        internal struct HttpHeader
        {
            internal string Name;
            internal uint NameLength;
            internal string Value;
            internal uint ValueLength;

            [CustomTypeMarshaller(typeof(HttpHeader), Direction = CustomTypeMarshallerDirection.In, Features = CustomTypeMarshallerFeatures.UnmanagedResources)]
            internal struct Native
            {
                private IntPtr Name;
                private uint NameLength;
                private IntPtr Value;
                private uint ValueLength;

                public Native(HttpHeader managed)
                {
                    Name = Marshal.StringToCoTaskMemAnsi(managed.Name);
                    NameLength = managed.NameLength;
                    Value = Marshal.StringToCoTaskMemAnsi(managed.Value);
                    ValueLength = managed.ValueLength;
                }

                public void FreeNative()
                {
                    Marshal.FreeCoTaskMem(Name);
                    Marshal.FreeCoTaskMem(Value);
                }
            }
        }
    }
}
