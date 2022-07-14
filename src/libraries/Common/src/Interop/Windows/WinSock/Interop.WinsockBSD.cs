// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

internal static partial class Interop
{
    internal static partial class Winsock
    {
        // IO-Control operations are not directly exposed.
        // blocking is controlled by "Blocking" property on socket (FIONBIO)
        // amount of data available is queried by "Available" property (FIONREAD)
        // The other flags are not exposed currently.
        internal static class IoctlSocketConstants
        {
            public const int FIONREAD = 0x4004667F;
            public const int FIONBIO = unchecked((int)0x8004667E);
            public const int FIOASYNC = unchecked((int)0x8004667D);
            public const int SIOGETEXTENSIONFUNCTIONPOINTER = unchecked((int)0xC8000006);

            // Not likely to block (sync IO ok):
            //
            // FIONBIO
            // FIONREAD
            // SIOCATMARK
            // SIO_RCVALL
            // SIO_RCVALL_MCAST
            // SIO_RCVALL_IGMPMCAST
            // SIO_KEEPALIVE_VALS
            // SIO_ASSOCIATE_HANDLE (opcode setting: I, T==1)
            // SIO_ENABLE_CIRCULAR_QUEUEING (opcode setting: V, T==1)
            // SIO_GET_BROADCAST_ADDRESS (opcode setting: O, T==1)
            // SIO_GET_EXTENSION_FUNCTION_POINTER (opcode setting: O, I, T==1)
            // SIO_MULTIPOINT_LOOPBACK (opcode setting: I, T==1)
            // SIO_MULTICAST_SCOPE (opcode setting: I, T==1)
            // SIO_TRANSLATE_HANDLE (opcode setting: I, O, T==1)
            // SIO_ROUTING_INTERFACE_QUERY (opcode setting: I, O, T==1)
            //
            // Likely to block (recommended for async IO):
            //
            // SIO_FIND_ROUTE (opcode setting: O, T==1)
            // SIO_FLUSH (opcode setting: V, T==1)
            // SIO_GET_QOS (opcode setting: O, T==1)
            // SIO_GET_GROUP_QOS (opcode setting: O, I, T==1)
            // SIO_SET_QOS (opcode setting: I, T==1)
            // SIO_SET_GROUP_QOS (opcode setting: I, T==1)
            // SIO_ROUTING_INTERFACE_CHANGE (opcode setting: I, T==1)
            // SIO_ADDRESS_LIST_CHANGE (opcode setting: T==1)
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct TimeValue
        {
            public int Seconds;
            public int Microseconds;
        }

        // Argument structure for IP_ADD_MEMBERSHIP and IP_DROP_MEMBERSHIP.
        [StructLayout(LayoutKind.Sequential)]
        internal unsafe struct IPMulticastRequest
        {
            internal int MulticastAddress; // IP multicast address of group
            internal int InterfaceAddress; // local IP address of interface

            internal static readonly int Size = sizeof(IPMulticastRequest);
        }

        // Argument structure for IPV6_ADD_MEMBERSHIP and IPV6_DROP_MEMBERSHIP.
        [NativeMarshalling(typeof(Marshaller))]
        internal struct IPv6MulticastRequest
        {
            internal byte[] MulticastAddress; // IP address of group.
            internal int InterfaceIndex; // Local interface index.

            [CustomMarshaller(typeof(IPv6MulticastRequest), MarshalMode.Default, typeof(Marshaller))]
            public static class Marshaller
            {
                public static Native ConvertToUnmanaged(IPv6MulticastRequest managed) => new(managed);
                public static IPv6MulticastRequest ConvertToManaged(Native native) => native.ToManaged();

                public unsafe struct Native
                {
                    private const int MulticastAddressLength = 16;
                    private fixed byte _multicastAddress[MulticastAddressLength];
                    private int _interfaceIndex;

                    public Native(IPv6MulticastRequest managed)
                    {
                        Debug.Assert(managed.MulticastAddress.Length == MulticastAddressLength);
                        managed.MulticastAddress.CopyTo(MemoryMarshal.CreateSpan(ref _multicastAddress[0], MulticastAddressLength));
                        _interfaceIndex = managed.InterfaceIndex;
                    }

                    public IPv6MulticastRequest ToManaged()
                    {
                        IPv6MulticastRequest managed = new()
                        {
                            MulticastAddress = new byte[MulticastAddressLength],
                            InterfaceIndex = _interfaceIndex
                        };
                        MemoryMarshal.CreateReadOnlySpan(ref _multicastAddress[0], MulticastAddressLength).CopyTo(managed.MulticastAddress);
                        return managed;
                    }
                }
            }

            internal static readonly unsafe int Size = sizeof(Marshaller.Native);
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct Linger
        {
            internal ushort OnOff; // Option on/off.
            internal ushort Time; // Linger time in seconds.
        }
    }
}
