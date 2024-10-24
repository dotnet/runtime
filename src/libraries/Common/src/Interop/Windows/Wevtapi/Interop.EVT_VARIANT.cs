// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using System.Security;
using System;
using System.Runtime.InteropServices.Marshalling;

internal static partial class Interop
{
    internal static partial class Wevtapi
    {
        [StructLayout(LayoutKind.Explicit, CharSet = CharSet.Auto)]
#pragma warning disable 618 // System.Core still uses SecurityRuleSet.Level1
        [SecurityCritical(SecurityCriticalScope.Everything)]
#pragma warning restore 618
        internal struct EVT_VARIANT
        {
            [FieldOffset(0)]
            public uint UInteger;
            [FieldOffset(0)]
            public int Integer;
            [FieldOffset(0)]
            public byte UInt8;
            [FieldOffset(0)]
            public short Short;
            [FieldOffset(0)]
            public ushort UShort;
            [FieldOffset(0)]
            public uint Bool;
            [FieldOffset(0)]
            public byte ByteVal;
            [FieldOffset(0)]
            public byte SByte;
            [FieldOffset(0)]
            public ulong ULong;
            [FieldOffset(0)]
            public long Long;
            [FieldOffset(0)]
            public float Single;
            [FieldOffset(0)]
            public double Double;
            [FieldOffset(0)]
            public IntPtr StringVal;
            [FieldOffset(0)]
            public IntPtr AnsiString;
            [FieldOffset(0)]
            public IntPtr SidVal;
            [FieldOffset(0)]
            public IntPtr Binary;
            [FieldOffset(0)]
            public IntPtr Reference;
            [FieldOffset(0)]
            public IntPtr Handle;
            [FieldOffset(0)]
            public IntPtr GuidReference;
            [FieldOffset(0)]
            public ulong FileTime;
            [FieldOffset(0)]
            public IntPtr SystemTime;
            [FieldOffset(0)]
            public IntPtr SizeT;
            [FieldOffset(8)]
            public uint Count;   // number of elements (not length) in bytes.
            [FieldOffset(12)]
            public uint Type;
        }

#if NET
        [NativeMarshalling(typeof(Marshaller))]
#endif
        [StructLayout(LayoutKind.Explicit, CharSet = CharSet.Unicode)]
        internal struct EvtStringVariant
        {
            [MarshalAs(UnmanagedType.LPWStr), FieldOffset(0)]
            public string StringVal;
            [FieldOffset(8)]
            public uint Count;
            [FieldOffset(12)]
            public uint Type;

#if NET
            [CustomMarshaller(typeof(EvtStringVariant), MarshalMode.Default, typeof(Marshaller))]
            public static class Marshaller
            {
                public static Native ConvertToUnmanaged(EvtStringVariant managed) => new(managed);
                public static EvtStringVariant ConvertToManaged(Native native) => native.ToManaged();
                public static void Free(Native native) => native.FreeNative();

                [StructLayout(LayoutKind.Explicit)]
                public struct Native
                {
                    [FieldOffset(0)]
                    private IntPtr StringVal;
                    [FieldOffset(8)]
                    private uint Count;
                    [FieldOffset(12)]
                    private uint Type;

                    public Native(EvtStringVariant managed)
                    {
                        StringVal = Marshal.StringToCoTaskMemUni(managed.StringVal);
                        Count = managed.Count;
                        Type = managed.Type;
                    }

                    public EvtStringVariant ToManaged()
                    {
                        return new EvtStringVariant
                        {
                            StringVal = Marshal.PtrToStringUni(StringVal),
                            Count = Count,
                            Type = Type
                        };
                    }

                    public void FreeNative()
                    {
                        Marshal.FreeCoTaskMem(StringVal);
                    }
                }
            }
#endif
        };
    }
}
