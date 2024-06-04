// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Globalization;
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

        public override string ToString() => string.Create(CultureInfo.InvariantCulture, stackalloc char[64], $"0x{Value:x8}");
    }

    internal ref struct MetadataEnumResult
    {
        internal int _length;

        internal const int SmallIntArrayLength = 16;

        [InlineArray(SmallIntArrayLength)]
        internal struct SmallIntArray
        {
            public int e;
        }
        internal SmallIntArray _smallResult;
        internal int[]? _largeResult;

        public int Length => _length;

        public int this[int index]
        {
            get
            {
                Debug.Assert(0 <= index && index < Length);
                if (_largeResult != null)
                    return _largeResult[index];

                return _smallResult[index];
            }
        }
    }

#pragma warning disable CA1066 // IEquatable<MetadataImport> interface implementation isn't used
    internal readonly partial struct MetadataImport
#pragma warning restore CA1067
    {
        private readonly IntPtr m_metadataImport2;

        #region Override methods from Object
        public override int GetHashCode()
        {
            return HashCode.Combine(m_metadataImport2);
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
        private static extern unsafe bool GetMarshalAs(
            IntPtr pNativeType,
            int cNativeType,
            out int unmanagedType,
            out int safeArraySubType,
            out byte* safeArrayUserDefinedSubType,
            out int arraySubType,
            out int sizeParamIndex,
            out int sizeConst,
            out byte* marshalType,
            out byte* marshalCookie,
            out int iidParamIndex);

        internal static unsafe MarshalAsAttribute GetMarshalAs(ConstArray nativeType, RuntimeModule scope)
        {
            if (!GetMarshalAs(
                    nativeType.Signature,
                    nativeType.Length,
                    out int unmanagedTypeRaw,
                    out int safeArraySubTypeRaw,
                    out byte* safeArrayUserDefinedSubTypeRaw,
                    out int arraySubTypeRaw,
                    out int sizeParamIndex,
                    out int sizeConst,
                    out byte* marshalTypeRaw,
                    out byte* marshalCookieRaw,
                    out int iidParamIndex))
            {
                throw new BadImageFormatException();
            }

            string? safeArrayUserDefinedTypeName = safeArrayUserDefinedSubTypeRaw == null
                ? null
                : Text.Encoding.UTF8.GetString(MemoryMarshal.CreateReadOnlySpanFromNullTerminated(safeArrayUserDefinedSubTypeRaw));
            string? marshalTypeName = marshalTypeRaw == null
                ? null
                : Text.Encoding.UTF8.GetString(MemoryMarshal.CreateReadOnlySpanFromNullTerminated(marshalTypeRaw));
            string? marshalCookie = marshalCookieRaw == null
                ? null
                : Text.Encoding.UTF8.GetString(MemoryMarshal.CreateReadOnlySpanFromNullTerminated(marshalCookieRaw));

            RuntimeType? safeArrayUserDefinedType = string.IsNullOrEmpty(safeArrayUserDefinedTypeName) ? null :
                TypeNameResolver.GetTypeReferencedByCustomAttribute(safeArrayUserDefinedTypeName, scope);
            RuntimeType? marshalTypeRef = null;

            try
            {
                marshalTypeRef = marshalTypeName is null ? null : TypeNameResolver.GetTypeReferencedByCustomAttribute(marshalTypeName, scope);
            }
            catch (TypeLoadException)
            {
                // The user may have supplied a bad type name string causing this TypeLoadException
                // Regardless, we return the bad type name
                Debug.Assert(marshalTypeName is not null);
            }

            MarshalAsAttribute attribute = new MarshalAsAttribute((UnmanagedType)unmanagedTypeRaw);

            attribute.SafeArraySubType = (VarEnum)safeArraySubTypeRaw;
            attribute.SafeArrayUserDefinedSubType = safeArrayUserDefinedType;
            attribute.IidParameterIndex = iidParamIndex;
            attribute.ArraySubType = (UnmanagedType)arraySubTypeRaw;
            attribute.SizeParamIndex = (short)sizeParamIndex;
            attribute.SizeConst = sizeConst;
            attribute.MarshalType = marshalTypeName;
            attribute.MarshalTypeRef = marshalTypeRef;
            attribute.MarshalCookie = marshalCookie;

            return attribute;
        }
        #endregion

        #region Constructor
        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern unsafe IntPtr GetMetadataImport(RuntimeModule module);

        internal MetadataImport(RuntimeModule module)
        {
            ArgumentNullException.ThrowIfNull(module);

            // The MetadataImport instance needs to be acquired in this manner
            // since the instance can be replaced during HotReload and EnC scenarios.
            m_metadataImport2 = GetMetadataImport(module);
        }
        #endregion

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "MetadataImport_Enum")]
        private static unsafe partial void Enum(IntPtr scope, int type, int parent, ref int length, int* shortResult, ObjectHandleOnStack longResult);

        public unsafe void Enum(MetadataTokenType type, int parent, out MetadataEnumResult result)
        {
            result = default;
            int length = MetadataEnumResult.SmallIntArrayLength;
            fixed (int* p = &result._smallResult.e)
            {
                Enum(m_metadataImport2, (int)type, parent, ref length, p, ObjectHandleOnStack.Create(ref result._largeResult));
            }
            result._length = length;
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

        private static unsafe string? ConvertMetadataStringPermitInvalidContent(char* stringMetadataEncoding, int length)
        {
            Debug.Assert(stringMetadataEncoding != null);
            // Metadata encoding is always UTF-16LE, but user strings can be leveraged to encode invalid surrogates.
            // This means we rely on the string's constructor rather than the stricter Encoding.Unicode API.
            return new string(stringMetadataEncoding, 0, length);
        }

        #region FCalls
        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern unsafe int GetDefaultValue(
            IntPtr scope,
            int mdToken,
            out long value,
            out char* stringMetadataEncoding,
            out int length,
            out int corElementType);

        public unsafe string? GetDefaultValue(int mdToken, out long value, out int length, out CorElementType corElementType)
        {
            ThrowBadImageExceptionForHR(GetDefaultValue(m_metadataImport2, mdToken, out value, out char* stringMetadataEncoding, out length, out int corElementTypeRaw));

            corElementType = (CorElementType)corElementTypeRaw;

            if (corElementType is CorElementType.ELEMENT_TYPE_STRING
                && stringMetadataEncoding != null)
            {
                return ConvertMetadataStringPermitInvalidContent(stringMetadataEncoding, length);
            }

            return null;
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern unsafe int GetUserString(IntPtr scope, int mdToken, out char* stringMetadataEncoding, out int length);

        public unsafe string? GetUserString(int mdToken)
        {
            ThrowBadImageExceptionForHR(GetUserString(m_metadataImport2, mdToken, out char* stringMetadataEncoding, out int length));

            return stringMetadataEncoding != null ?
                ConvertMetadataStringPermitInvalidContent(stringMetadataEncoding, length) :
                null;
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern unsafe int GetName(IntPtr scope, int mdToken, out byte* name);

        public unsafe MdUtf8String GetName(int mdToken)
        {
            ThrowBadImageExceptionForHR(GetName(m_metadataImport2, mdToken, out byte* name));
            return new MdUtf8String(name);
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern unsafe int GetNamespace(IntPtr scope, int mdToken, out byte* namesp);

        public unsafe MdUtf8String GetNamespace(int mdToken)
        {
            ThrowBadImageExceptionForHR(GetNamespace(m_metadataImport2, mdToken, out byte* namesp));
            return new MdUtf8String(namesp);
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern unsafe int GetEventProps(IntPtr scope, int mdToken, out void* name, out int eventAttributes);

        public unsafe void GetEventProps(int mdToken, out void* name, out EventAttributes eventAttributes)
        {
            ThrowBadImageExceptionForHR(GetEventProps(m_metadataImport2, mdToken, out name, out int eventAttributesRaw));
            eventAttributes = (EventAttributes)eventAttributesRaw;
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern int GetFieldDefProps(IntPtr scope, int mdToken, out int fieldAttributes);

        public void GetFieldDefProps(int mdToken, out FieldAttributes fieldAttributes)
        {
            ThrowBadImageExceptionForHR(GetFieldDefProps(m_metadataImport2, mdToken, out int fieldAttributesRaw));
            fieldAttributes = (FieldAttributes)fieldAttributesRaw;
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern unsafe int GetPropertyProps(IntPtr scope, int mdToken, out void* name, out int propertyAttributes, out ConstArray signature);

        public unsafe void GetPropertyProps(int mdToken, out void* name, out PropertyAttributes propertyAttributes, out ConstArray signature)
        {
            ThrowBadImageExceptionForHR(GetPropertyProps(m_metadataImport2, mdToken, out name, out int propertyAttributesRaw, out signature));
            propertyAttributes = (PropertyAttributes)propertyAttributesRaw;
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern int GetParentToken(IntPtr scope, int mdToken, out int tkParent);

        public int GetParentToken(int tkToken)
        {
            ThrowBadImageExceptionForHR(GetParentToken(m_metadataImport2, tkToken, out int tkParent));
            return tkParent;
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern int GetParamDefProps(IntPtr scope, int parameterToken, out int sequence, out int attributes);

        public void GetParamDefProps(int parameterToken, out int sequence, out ParameterAttributes attributes)
        {
            ThrowBadImageExceptionForHR(GetParamDefProps(m_metadataImport2, parameterToken, out sequence, out int attributesRaw));
            attributes = (ParameterAttributes)attributesRaw;
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern int GetGenericParamProps(IntPtr scope, int genericParameter, out int flags);

        public void GetGenericParamProps(
            int genericParameter,
            out GenericParameterAttributes attributes)
        {
            ThrowBadImageExceptionForHR(GetGenericParamProps(m_metadataImport2, genericParameter, out int attributesRaw));
            attributes = (GenericParameterAttributes)attributesRaw;
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern int GetScopeProps(IntPtr scope, out Guid mvid);

        public void GetScopeProps(out Guid mvid)
        {
            ThrowBadImageExceptionForHR(GetScopeProps(m_metadataImport2, out mvid));
        }

        public ConstArray GetMethodSignature(MetadataToken token)
        {
            if (token.IsMemberRef)
                return GetMemberRefProps(token);

            return GetSigOfMethodDef(token);
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern int GetSigOfMethodDef(IntPtr scope, int methodToken, ref ConstArray signature);

        public ConstArray GetSigOfMethodDef(int methodToken)
        {
            ConstArray signature = default;
            ThrowBadImageExceptionForHR(GetSigOfMethodDef(m_metadataImport2, methodToken, ref signature));
            return signature;
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern int GetSignatureFromToken(IntPtr scope, int methodToken, ref ConstArray signature);

        public ConstArray GetSignatureFromToken(int token)
        {
            ConstArray signature = default;
            ThrowBadImageExceptionForHR(GetSignatureFromToken(m_metadataImport2, token, ref signature));
            return signature;
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern int GetMemberRefProps(IntPtr scope, int memberTokenRef, out ConstArray signature);

        public ConstArray GetMemberRefProps(int memberTokenRef)
        {
            ThrowBadImageExceptionForHR(GetMemberRefProps(m_metadataImport2, memberTokenRef, out ConstArray signature));
            return signature;
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern int GetCustomAttributeProps(IntPtr scope,
            int customAttributeToken,
            out int constructorToken,
            out ConstArray signature);

        public void GetCustomAttributeProps(
            int customAttributeToken,
            out int constructorToken,
            out ConstArray signature)
        {
            ThrowBadImageExceptionForHR(GetCustomAttributeProps(m_metadataImport2, customAttributeToken, out constructorToken, out signature));
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern int GetClassLayout(IntPtr scope, int typeTokenDef, out int packSize, out int classSize);

        public void GetClassLayout(
            int typeTokenDef,
            out int packSize,
            out int classSize)
        {
            ThrowBadImageExceptionForHR(GetClassLayout(m_metadataImport2, typeTokenDef, out packSize, out classSize));
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern int GetFieldOffset(IntPtr scope, int typeTokenDef, int fieldTokenDef, out int offset, out bool found);

        public bool GetFieldOffset(
            int typeTokenDef,
            int fieldTokenDef,
            out int offset)
        {
            int hr = GetFieldOffset(m_metadataImport2, typeTokenDef, fieldTokenDef, out offset, out bool found);
            if (!found && hr < 0)
            {
                throw new BadImageFormatException();
            }
            return found;
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern int GetSigOfFieldDef(IntPtr scope, int fieldToken, ref ConstArray fieldMarshal);

        public ConstArray GetSigOfFieldDef(int fieldToken)
        {
            ConstArray sig = default;
            ThrowBadImageExceptionForHR(GetSigOfFieldDef(m_metadataImport2, fieldToken, ref sig));
            return sig;
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern int GetFieldMarshal(IntPtr scope, int fieldToken, ref ConstArray fieldMarshal);

        public ConstArray GetFieldMarshal(int fieldToken)
        {
            ConstArray fieldMarshal = default;
            ThrowBadImageExceptionForHR(GetFieldMarshal(m_metadataImport2, fieldToken, ref fieldMarshal));
            return fieldMarshal;
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern unsafe int GetPInvokeMap(IntPtr scope,
            int token,
            out int attributes,
            out byte* importName,
            out byte* importDll);

        public unsafe void GetPInvokeMap(
            int token,
            out PInvokeAttributes attributes,
            out string importName,
            out string importDll)
        {
            ThrowBadImageExceptionForHR(GetPInvokeMap(m_metadataImport2, token, out int attributesRaw, out byte* importNameRaw, out byte* importDllRaw));

            importName = Text.Encoding.UTF8.GetString(MemoryMarshal.CreateReadOnlySpanFromNullTerminated(importNameRaw));
            importDll = Text.Encoding.UTF8.GetString(MemoryMarshal.CreateReadOnlySpanFromNullTerminated(importDllRaw));
            attributes = (PInvokeAttributes)attributesRaw;
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern bool IsValidToken(IntPtr scope, int token);

        public bool IsValidToken(int token)
        {
            return IsValidToken(m_metadataImport2, token);
        }
        #endregion

        private static void ThrowBadImageExceptionForHR(int hr)
        {
            if (hr < 0)
            {
                throw new BadImageFormatException();
            }
        }
    }
}
