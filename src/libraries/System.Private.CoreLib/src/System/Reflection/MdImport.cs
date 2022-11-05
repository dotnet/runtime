// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Reflection
{
    [Flags]
    internal enum MdSigCallingConvention : byte
    {
        CallConvMask = 0x0f,  // Calling convention is bottom 4 bits

        Default = 0x00,
        C = 0x01,
        StdCall = 0x02,
        ThisCall = 0x03,
        FastCall = 0x04,
        Vararg = 0x05,
        Field = 0x06,
        LocalSig = 0x07,
        Property = 0x08,
        Unmanaged = 0x09,
        GenericInst = 0x0a,  // generic method instantiation

        Generic = 0x10,  // Generic method sig with explicit number of type arguments (precedes ordinary parameter count)
        HasThis = 0x20,  // Top bit indicates a 'this' parameter
        ExplicitThis = 0x40,  // This parameter is explicitly in the signature
    }

    [Flags]
    internal enum PInvokeAttributes
    {
        NoMangle = 0x0001,

        CharSetMask = 0x0006,
        CharSetNotSpec = 0x0000,
        CharSetAnsi = 0x0002,
        CharSetUnicode = 0x0004,
        CharSetAuto = 0x0006,

        BestFitUseAssem = 0x0000,
        BestFitEnabled = 0x0010,
        BestFitDisabled = 0x0020,
        BestFitMask = 0x0030,

        ThrowOnUnmappableCharUseAssem = 0x0000,
        ThrowOnUnmappableCharEnabled = 0x1000,
        ThrowOnUnmappableCharDisabled = 0x2000,
        ThrowOnUnmappableCharMask = 0x3000,

        SupportsLastError = 0x0040,

        CallConvMask = 0x0700,
        CallConvWinapi = 0x0100,
        CallConvCdecl = 0x0200,
        CallConvStdcall = 0x0300,
        CallConvThiscall = 0x0400,
        CallConvFastcall = 0x0500,

        MaxValue = 0xFFFF,
    }

    [Flags]
    internal enum MethodSemanticsAttributes
    {
        Setter = 0x0001,
        Getter = 0x0002,
        Other = 0x0004,
        AddOn = 0x0008,
        RemoveOn = 0x0010,
        Fire = 0x0020,
    }

    internal enum MetadataTokenType
    {
        Module = 0x00000000,
        TypeRef = 0x01000000,
        TypeDef = 0x02000000,
        FieldDef = 0x04000000,
        MethodDef = 0x06000000,
        ParamDef = 0x08000000,
        InterfaceImpl = 0x09000000,
        MemberRef = 0x0a000000,
        CustomAttribute = 0x0c000000,
        Permission = 0x0e000000,
        Signature = 0x11000000,
        Event = 0x14000000,
        Property = 0x17000000,
        ModuleRef = 0x1a000000,
        TypeSpec = 0x1b000000,
        Assembly = 0x20000000,
        AssemblyRef = 0x23000000,
        File = 0x26000000,
        ExportedType = 0x27000000,
        ManifestResource = 0x28000000,
        GenericPar = 0x2a000000,
        MethodSpec = 0x2b000000,
        String = 0x70000000,
        Name = 0x71000000,
        BaseType = 0x72000000,
        Invalid = 0x7FFFFFFF,
    }

    internal struct MetadataToken
    {
        public int Value;

        public static implicit operator int(MetadataToken token) => token.Value;
        public static implicit operator MetadataToken(int token) => new MetadataToken(token);

        public static bool IsTokenOfType(int token, params MetadataTokenType[] types)
        {
            for (int i = 0; i < types.Length; i++)
            {
                if ((int)(token & 0xFF000000) == (int)types[i])
                    return true;
            }

            return false;
        }

        public static bool IsNullToken(int token) => (token & 0x00FFFFFF) == 0;

        public MetadataToken(int token) { Value = token; }

        public bool IsGlobalTypeDefToken => Value == 0x02000001;
        public MetadataTokenType TokenType => (MetadataTokenType)(Value & 0xFF000000);
        public bool IsTypeRef => TokenType == MetadataTokenType.TypeRef;
        public bool IsTypeDef => TokenType == MetadataTokenType.TypeDef;
        public bool IsFieldDef => TokenType == MetadataTokenType.FieldDef;
        public bool IsMethodDef => TokenType == MetadataTokenType.MethodDef;
        public bool IsMemberRef => TokenType == MetadataTokenType.MemberRef;
        public bool IsEvent => TokenType == MetadataTokenType.Event;
        public bool IsProperty => TokenType == MetadataTokenType.Property;
        public bool IsParamDef => TokenType == MetadataTokenType.ParamDef;
        public bool IsTypeSpec => TokenType == MetadataTokenType.TypeSpec;
        public bool IsMethodSpec => TokenType == MetadataTokenType.MethodSpec;
        public bool IsString => TokenType == MetadataTokenType.String;
        public bool IsSignature => TokenType == MetadataTokenType.Signature;
        public bool IsModule => TokenType == MetadataTokenType.Module;
        public bool IsAssembly => TokenType == MetadataTokenType.Assembly;
        public bool IsGenericPar => TokenType == MetadataTokenType.GenericPar;

        public override string ToString() => string.Format(CultureInfo.InvariantCulture, "0x{0:x8}", Value);
    }
}
