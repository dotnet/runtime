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

    internal readonly struct ConstArray
    {
        // Keep the definition in sync with vm\ManagedMdImport.hpp
        internal readonly int m_length;
        internal readonly IntPtr m_constArray;

        public IntPtr Signature => m_constArray;
        public int Length => m_length;

        public byte this[int index]
        {
            get
            {
                if (index < 0 || index >= m_length)
                    throw new IndexOutOfRangeException();

                unsafe
                {
                    return ((byte*)m_constArray.ToPointer())[index];
                }
            }
        }
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

    internal unsafe struct MetadataEnumResult
    {
        // Keep the definition in sync with vm\ManagedMdImport.hpp
        private int[] largeResult;
        private int length;
        private fixed int smallResult[16];

        public int Length => length;

        public int this[int index]
        {
            get
            {
                Debug.Assert(0 <= index && index < Length);
                if (largeResult != null)
                    return largeResult[index];

                fixed (int* p = smallResult)
                    return p[index];
            }
        }
    }

#pragma warning disable CA1066 // IEquatable<MetadataImport> interface implementation isn't used
    internal readonly struct MetadataImport
#pragma warning restore CA1067
    {
        private readonly IntPtr m_metadataImport2;
        private readonly object? m_keepalive;

        #region Override methods from Object
        internal static readonly MetadataImport EmptyImport = new MetadataImport((IntPtr)0, null);

        public override int GetHashCode()
        {
            return RuntimeHelpers.GetHashCodeOfPtr(m_metadataImport2);
        }

        public override bool Equals(object? obj)
        {
            if (!(obj is MetadataImport))
                return false;
            return Equals((MetadataImport)obj);
        }

        private bool Equals(MetadataImport import)
        {
            return import.m_metadataImport2 == m_metadataImport2;
        }

        #endregion

        #region Static Members
        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern void _GetMarshalAs(IntPtr pNativeType, int cNativeType, out int unmanagedType, out int safeArraySubType, out string? safeArrayUserDefinedSubType,
            out int arraySubType, out int sizeParamIndex, out int sizeConst, out string? marshalType, out string? marshalCookie,
            out int iidParamIndex);
        internal static void GetMarshalAs(ConstArray nativeType,
            out UnmanagedType unmanagedType, out VarEnum safeArraySubType, out string? safeArrayUserDefinedSubType,
            out UnmanagedType arraySubType, out int sizeParamIndex, out int sizeConst, out string? marshalType, out string? marshalCookie,
            out int iidParamIndex)
        {

            _GetMarshalAs(nativeType.Signature, (int)nativeType.Length,
                out int _unmanagedType, out int _safeArraySubType, out safeArrayUserDefinedSubType,
                out int _arraySubType, out sizeParamIndex, out sizeConst, out marshalType, out marshalCookie,
                out iidParamIndex);
            unmanagedType = (UnmanagedType)_unmanagedType;
            safeArraySubType = (VarEnum)_safeArraySubType;
            arraySubType = (UnmanagedType)_arraySubType;
        }
        #endregion

        #region Internal Static Members
        internal static void ThrowError(int hResult)
        {
            throw new MetadataException(hResult);
        }
        #endregion

        #region Constructor
        internal MetadataImport(IntPtr metadataImport2, object? keepalive)
        {
            m_metadataImport2 = metadataImport2;
            m_keepalive = keepalive;
        }
        #endregion

        #region FCalls
        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern void _Enum(IntPtr scope, int type, int parent, out MetadataEnumResult result);

        public void Enum(MetadataTokenType type, int parent, out MetadataEnumResult result)
        {
            _Enum(m_metadataImport2, (int)type, parent, out result);
        }

        public void EnumNestedTypes(int mdTypeDef, out MetadataEnumResult result)
        {
            Enum(MetadataTokenType.TypeDef, mdTypeDef, out result);
        }

        public void EnumCustomAttributes(int mdToken, out MetadataEnumResult result)
        {
            Enum(MetadataTokenType.CustomAttribute, mdToken, out result);
        }

        public void EnumParams(int mdMethodDef, out MetadataEnumResult result)
        {
            Enum(MetadataTokenType.ParamDef, mdMethodDef, out result);
        }

        public void EnumFields(int mdTypeDef, out MetadataEnumResult result)
        {
            Enum(MetadataTokenType.FieldDef, mdTypeDef, out result);
        }

        public void EnumProperties(int mdTypeDef, out MetadataEnumResult result)
        {
            Enum(MetadataTokenType.Property, mdTypeDef, out result);
        }

        public void EnumEvents(int mdTypeDef, out MetadataEnumResult result)
        {
            Enum(MetadataTokenType.Event, mdTypeDef, out result);
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern string? _GetDefaultValue(IntPtr scope, int mdToken, out long value, out int length, out int corElementType);
        public string? GetDefaultValue(int mdToken, out long value, out int length, out CorElementType corElementType)
        {
            string? stringVal = _GetDefaultValue(m_metadataImport2, mdToken, out value, out length, out int _corElementType);
            corElementType = (CorElementType)_corElementType;
            return stringVal;
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern unsafe void _GetUserString(IntPtr scope, int mdToken, void** name, out int length);
        public unsafe string? GetUserString(int mdToken)
        {
            void* name;
            _GetUserString(m_metadataImport2, mdToken, &name, out int length);

            return name != null ?
                new string((char*)name, 0, length) :
                null;
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern unsafe void _GetName(IntPtr scope, int mdToken, void** name);
        public unsafe MdUtf8String GetName(int mdToken)
        {
            void* name;
            _GetName(m_metadataImport2, mdToken, &name);

            return new MdUtf8String(name);
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern unsafe void _GetNamespace(IntPtr scope, int mdToken, void** namesp);
        public unsafe MdUtf8String GetNamespace(int mdToken)
        {
            void* namesp;
            _GetNamespace(m_metadataImport2, mdToken, &namesp);

            return new MdUtf8String(namesp);
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern unsafe void _GetEventProps(IntPtr scope, int mdToken, void** name, out int eventAttributes);
        public unsafe void GetEventProps(int mdToken, out void* name, out EventAttributes eventAttributes)
        {
            void* _name;
            _GetEventProps(m_metadataImport2, mdToken, &_name, out int _eventAttributes);
            name = _name;
            eventAttributes = (EventAttributes)_eventAttributes;
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern void _GetFieldDefProps(IntPtr scope, int mdToken, out int fieldAttributes);
        public void GetFieldDefProps(int mdToken, out FieldAttributes fieldAttributes)
        {
            _GetFieldDefProps(m_metadataImport2, mdToken, out int _fieldAttributes);
            fieldAttributes = (FieldAttributes)_fieldAttributes;
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern unsafe void _GetPropertyProps(IntPtr scope,
            int mdToken, void** name, out int propertyAttributes, out ConstArray signature);
        public unsafe void GetPropertyProps(int mdToken, out void* name, out PropertyAttributes propertyAttributes, out ConstArray signature)
        {
            void* _name;
            _GetPropertyProps(m_metadataImport2, mdToken, &_name, out int _propertyAttributes, out signature);
            name = _name;
            propertyAttributes = (PropertyAttributes)_propertyAttributes;
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern void _GetParentToken(IntPtr scope,
            int mdToken, out int tkParent);
        public int GetParentToken(int tkToken)
        {
            _GetParentToken(m_metadataImport2, tkToken, out int tkParent);
            return tkParent;
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern void _GetParamDefProps(IntPtr scope,
            int parameterToken, out int sequence, out int attributes);
        public void GetParamDefProps(int parameterToken, out int sequence, out ParameterAttributes attributes)
        {

            _GetParamDefProps(m_metadataImport2, parameterToken, out sequence, out int _attributes);

            attributes = (ParameterAttributes)_attributes;
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern void _GetGenericParamProps(IntPtr scope,
            int genericParameter,
            out int flags);

        public void GetGenericParamProps(
            int genericParameter,
            out GenericParameterAttributes attributes)
        {
            _GetGenericParamProps(m_metadataImport2, genericParameter, out int _attributes);
            attributes = (GenericParameterAttributes)_attributes;
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern void _GetScopeProps(IntPtr scope,
            out Guid mvid);

        public void GetScopeProps(
            out Guid mvid)
        {
            _GetScopeProps(m_metadataImport2, out mvid);
        }

        public ConstArray GetMethodSignature(MetadataToken token)
        {
            if (token.IsMemberRef)
                return GetMemberRefProps(token);

            return GetSigOfMethodDef(token);
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern void _GetSigOfMethodDef(IntPtr scope,
            int methodToken,
            ref ConstArray signature);

        public ConstArray GetSigOfMethodDef(int methodToken)
        {
            ConstArray signature = default;

            _GetSigOfMethodDef(m_metadataImport2, methodToken, ref signature);

            return signature;
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern void _GetSignatureFromToken(IntPtr scope,
            int methodToken,
            ref ConstArray signature);

        public ConstArray GetSignatureFromToken(int token)
        {
            ConstArray signature = default;

            _GetSignatureFromToken(m_metadataImport2, token, ref signature);

            return signature;
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern void _GetMemberRefProps(IntPtr scope,
            int memberTokenRef,
            out ConstArray signature);

        public ConstArray GetMemberRefProps(int memberTokenRef)
        {
            _GetMemberRefProps(m_metadataImport2, memberTokenRef, out ConstArray signature);

            return signature;
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern void _GetCustomAttributeProps(IntPtr scope,
            int customAttributeToken,
            out int constructorToken,
            out ConstArray signature);

        public void GetCustomAttributeProps(
            int customAttributeToken,
            out int constructorToken,
            out ConstArray signature)
        {
            _GetCustomAttributeProps(m_metadataImport2, customAttributeToken,
                out constructorToken, out signature);
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern void _GetClassLayout(IntPtr scope,
            int typeTokenDef, out int packSize, out int classSize);
        public void GetClassLayout(
            int typeTokenDef,
            out int packSize,
            out int classSize)
        {
            _GetClassLayout(m_metadataImport2, typeTokenDef, out packSize, out classSize);
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern bool _GetFieldOffset(IntPtr scope,
            int typeTokenDef, int fieldTokenDef, out int offset);
        public bool GetFieldOffset(
            int typeTokenDef,
            int fieldTokenDef,
            out int offset)
        {
            return _GetFieldOffset(m_metadataImport2, typeTokenDef, fieldTokenDef, out offset);
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern void _GetSigOfFieldDef(IntPtr scope,
            int fieldToken,
            ref ConstArray fieldMarshal);

        public ConstArray GetSigOfFieldDef(int fieldToken)
        {
            ConstArray fieldMarshal = default;

            _GetSigOfFieldDef(m_metadataImport2, fieldToken, ref fieldMarshal);

            return fieldMarshal;
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern void _GetFieldMarshal(IntPtr scope,
            int fieldToken,
            ref ConstArray fieldMarshal);

        public ConstArray GetFieldMarshal(int fieldToken)
        {
            ConstArray fieldMarshal = default;

            _GetFieldMarshal(m_metadataImport2, fieldToken, ref fieldMarshal);

            return fieldMarshal;
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern unsafe void _GetPInvokeMap(IntPtr scope,
            int token,
            out int attributes,
            void** importName,
            void** importDll);

        public unsafe void GetPInvokeMap(
            int token,
            out PInvokeAttributes attributes,
            out string importName,
            out string importDll)
        {
            void* _importName, _importDll;
            _GetPInvokeMap(m_metadataImport2, token, out int _attributes, &_importName, &_importDll);
            importName = new MdUtf8String(_importName).ToString();
            importDll = new MdUtf8String(_importDll).ToString();

            attributes = (PInvokeAttributes)_attributes;
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern bool _IsValidToken(IntPtr scope, int token);
        public bool IsValidToken(int token)
        {
            return _IsValidToken(m_metadataImport2, token);
        }
        #endregion
    }

    internal sealed class MetadataException : Exception
    {
        private int m_hr;
        internal MetadataException(int hr) { m_hr = hr; }

        public override string ToString() => $"MetadataException HResult = {m_hr:x}.";
    }
}
