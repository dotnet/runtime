// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


namespace System.Data.SqlClient
{
    // this structure is used for transporting packet handle references between the TdsParserStateObject
    //  base class and Managed or Native implementations. 
    // It prevents the native IntPtr type from being boxed and prevents the need to cast from object which loses compile time type safety
    // It carries type information so that assertions about the type of handle can be made in the implemented abstract methods 
    // it is a ref struct so that it can only be used to transport the handles and not store them

    // N.B. If you change this type you must also change the version for the other platform

    internal readonly ref struct PacketHandle
    {
        public const int NativePointerType = 1;
        public const int NativePacketType = 2;
        public const int ManagedPacketType = 3;

        public readonly IntPtr NativePointer;
        public readonly SNIPacket NativePacket;

        public readonly SNI.SNIPacket ManagedPacket;
        public readonly int Type;

        private PacketHandle(IntPtr nativePointer, SNIPacket nativePacket, SNI.SNIPacket managedPacket, int type)
        {
            Type = type;
            ManagedPacket = managedPacket;
            NativePointer = nativePointer;
            NativePacket = nativePacket;
        }

        public static PacketHandle FromManagedPacket(SNI.SNIPacket managedPacket) => new PacketHandle(default, default, managedPacket, ManagedPacketType);

        public static PacketHandle FromNativePointer(IntPtr nativePointer) => new PacketHandle(nativePointer, default, default, NativePointerType);

        public static PacketHandle FromNativePacket(SNIPacket nativePacket) => new PacketHandle(default, nativePacket, default, NativePacketType);


    }
}
