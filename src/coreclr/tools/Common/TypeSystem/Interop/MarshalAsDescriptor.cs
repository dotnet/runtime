// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace Internal.TypeSystem
{
    public enum NativeTypeKind : byte
    {
        Boolean = 0x2,
        I1 = 0x3,
        U1 = 0x4,
        I2 = 0x5,
        U2 = 0x6,
        I4 = 0x7,
        U4 = 0x8,
        I8 = 0x9,
        U8 = 0xa,
        R4 = 0xb,
        R8 = 0xc,
        Currency = 0xf,
        BStr = 0x13,
        LPStr = 0x14,
        LPWStr = 0x15,
        LPTStr = 0x16,        // Ptr to OS preferred (SBCS/Unicode) string
        ByValTStr = 0x17,     // OS preferred (SBCS/Unicode) inline string (only valid in structs)
        IUnknown = 0x19,
        IDispatch = 0x1a,
        Struct = 0x1b,
        Intf = 0x1c,
        SafeArray = 0x1d,
        ByValArray = 0x1e,
        SysInt = 0x1f,
        SysUInt = 0x20,
        AnsiBStr = 0x23,
        TBStr = 0x24,
        VariantBool = 0x25,
        Func = 0x26,
        AsAny = 0x28,
        Array = 0x2a,
        LPStruct = 0x2b,    // This is not  defined in Ecma-335(II.23.4)
        CustomMarshaler = 0x2c,
        LPUTF8Str = 0x30,
        Default = 0x50,      // This is the default value
        Variant = 0x51,
    }

    public class MarshalAsDescriptor
    {
        private TypeDesc _marshallerType;
        private string _cookie;

        public NativeTypeKind Type { get; }
        public NativeTypeKind ArraySubType { get; }
        public uint? SizeParamIndex { get; }
        public uint? SizeConst { get; }
        public TypeDesc MarshallerType
        {
            get
            {
                Debug.Assert(Type == NativeTypeKind.CustomMarshaler, "Marshaller type can be set only when using for CustomMarshaller");
                return _marshallerType;
            }
        }

        public string Cookie
        {
            get
            {
                Debug.Assert(Type == NativeTypeKind.CustomMarshaler, "Cookie can be set only when using for CustomMarshaller");
                return _cookie;
            }
        }

        public MarshalAsDescriptor(NativeTypeKind type, NativeTypeKind arraySubType, uint? sizeParamIndex, uint? sizeConst, TypeDesc customMarshallerType, string cookie)
        {
            Type = type;
            ArraySubType = arraySubType;
            SizeParamIndex = sizeParamIndex;
            SizeConst = sizeConst;
            _marshallerType = customMarshallerType;
            _cookie = cookie;
        }
    }
}
