// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// 

using System;
using System.Reflection;
using System.Globalization;
using System.Threading;
using System.Diagnostics;
using System.Collections;
using System.Runtime.CompilerServices;
using System.Security;
using System.Text;
using System.Runtime.InteropServices;
using System.Configuration.Assemblies;
using System.Runtime.Versioning;

namespace System.Reflection
{
    internal enum CorElementType : byte
    {
        ELEMENT_TYPE_END = 0x00,
        ELEMENT_TYPE_VOID = 0x01,
        ELEMENT_TYPE_BOOLEAN = 0x02,
        ELEMENT_TYPE_CHAR = 0x03,
        ELEMENT_TYPE_I1 = 0x04,
        ELEMENT_TYPE_U1 = 0x05,
        ELEMENT_TYPE_I2 = 0x06,
        ELEMENT_TYPE_U2 = 0x07,
        ELEMENT_TYPE_I4 = 0x08,
        ELEMENT_TYPE_U4 = 0x09,
        ELEMENT_TYPE_I8 = 0x0A,
        ELEMENT_TYPE_U8 = 0x0B,
        ELEMENT_TYPE_R4 = 0x0C,
        ELEMENT_TYPE_R8 = 0x0D,
        ELEMENT_TYPE_STRING = 0x0E,
        ELEMENT_TYPE_PTR = 0x0F,
        ELEMENT_TYPE_BYREF = 0x10,
        ELEMENT_TYPE_VALUETYPE = 0x11,
        ELEMENT_TYPE_CLASS = 0x12,
        ELEMENT_TYPE_VAR = 0x13,
        ELEMENT_TYPE_ARRAY = 0x14,
        ELEMENT_TYPE_GENERICINST = 0x15,
        ELEMENT_TYPE_TYPEDBYREF = 0x16,
        ELEMENT_TYPE_I = 0x18,
        ELEMENT_TYPE_U = 0x19,
        ELEMENT_TYPE_FNPTR = 0x1B,
        ELEMENT_TYPE_OBJECT = 0x1C,
        ELEMENT_TYPE_SZARRAY = 0x1D,
        ELEMENT_TYPE_MVAR = 0x1E,
        ELEMENT_TYPE_CMOD_REQD = 0x1F,
        ELEMENT_TYPE_CMOD_OPT = 0x20,
        ELEMENT_TYPE_INTERNAL = 0x21,
        ELEMENT_TYPE_MAX = 0x22,
        ELEMENT_TYPE_MODIFIER = 0x40,
        ELEMENT_TYPE_SENTINEL = 0x41,
        ELEMENT_TYPE_PINNED = 0x45,
    }

    [Flags()]
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
        Unmgd = 0x09,
        GenericInst = 0x0a,  // generic method instantiation

        Generic = 0x10,  // Generic method sig with explicit number of type arguments (precedes ordinary parameter count)
        HasThis = 0x20,  // Top bit indicates a 'this' parameter    
        ExplicitThis = 0x40,  // This parameter is explicitly in the signature
    }


    [Flags()]
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


    [Flags()]
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

    internal struct ConstArray
    {
        public IntPtr Signature { get { return m_constArray; } }
        public int Length { get { return m_length; } }
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

        // Keep the definition in sync with vm\ManagedMdImport.hpp
        internal int m_length;
        internal IntPtr m_constArray;
    }

    internal struct MetadataToken
    {
        #region Implicit Cast Operators
        public static implicit operator int(MetadataToken token) { return token.Value; }
        public static implicit operator MetadataToken(int token) { return new MetadataToken(token); }
        #endregion

        #region Public Static Members
        public static bool IsTokenOfType(int token, params MetadataTokenType[] types)
        {
            for (int i = 0; i < types.Length; i++)
            {
                if ((int)(token & 0xFF000000) == (int)types[i])
                    return true;
            }

            return false;
        }

        public static bool IsNullToken(int token)
        {
            return (token & 0x00FFFFFF) == 0;
        }
        #endregion

        #region Public Data Members
        public int Value;
        #endregion

        #region Constructor
        public MetadataToken(int token) { Value = token; }
        #endregion

        #region Public Members
        public bool IsGlobalTypeDefToken { get { return (Value == 0x02000001); } }
        public MetadataTokenType TokenType { get { return (MetadataTokenType)(Value & 0xFF000000); } }
        public bool IsTypeRef { get { return TokenType == MetadataTokenType.TypeRef; } }
        public bool IsTypeDef { get { return TokenType == MetadataTokenType.TypeDef; } }
        public bool IsFieldDef { get { return TokenType == MetadataTokenType.FieldDef; } }
        public bool IsMethodDef { get { return TokenType == MetadataTokenType.MethodDef; } }
        public bool IsMemberRef { get { return TokenType == MetadataTokenType.MemberRef; } }
        public bool IsEvent { get { return TokenType == MetadataTokenType.Event; } }
        public bool IsProperty { get { return TokenType == MetadataTokenType.Property; } }
        public bool IsParamDef { get { return TokenType == MetadataTokenType.ParamDef; } }
        public bool IsTypeSpec { get { return TokenType == MetadataTokenType.TypeSpec; } }
        public bool IsMethodSpec { get { return TokenType == MetadataTokenType.MethodSpec; } }
        public bool IsString { get { return TokenType == MetadataTokenType.String; } }
        public bool IsSignature { get { return TokenType == MetadataTokenType.Signature; } }
        public bool IsModule { get { return TokenType == MetadataTokenType.Module; } }
        public bool IsAssembly { get { return TokenType == MetadataTokenType.Assembly; } }
        public bool IsGenericPar { get { return TokenType == MetadataTokenType.GenericPar; } }
        #endregion

        #region Object Overrides
        public override string ToString() { return string.Format(CultureInfo.InvariantCulture, "0x{0:x8}", Value); }
        #endregion
    }

    internal unsafe struct MetadataEnumResult
    {
        // Keep the definition in sync with vm\ManagedMdImport.hpp
        private int[] largeResult;
        private int length;
        private fixed int smallResult[16];

        public int Length
        {
            get
            {
                return length;
            }
        }

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

    internal struct MetadataImport
    {
        #region Private Data Members
        private IntPtr m_metadataImport2;
        private object m_keepalive;
        #endregion

        #region Override methods from Object
        internal static readonly MetadataImport EmptyImport = new MetadataImport((IntPtr)0, null);

        public override int GetHashCode()
        {
            return ValueType.GetHashCodeOfPtr(m_metadataImport2);
        }

        public override bool Equals(object obj)
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
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern void _GetMarshalAs(IntPtr pNativeType, int cNativeType, out int unmanagedType, out int safeArraySubType, out string safeArrayUserDefinedSubType,
            out int arraySubType, out int sizeParamIndex, out int sizeConst, out string marshalType, out string marshalCookie,
            out int iidParamIndex);
        internal static void GetMarshalAs(ConstArray nativeType,
            out UnmanagedType unmanagedType, out VarEnum safeArraySubType, out string safeArrayUserDefinedSubType,
            out UnmanagedType arraySubType, out int sizeParamIndex, out int sizeConst, out string marshalType, out string marshalCookie,
            out int iidParamIndex)
        {
            int _unmanagedType, _safeArraySubType, _arraySubType;

            _GetMarshalAs(nativeType.Signature, (int)nativeType.Length,
                out _unmanagedType, out _safeArraySubType, out safeArrayUserDefinedSubType,
                out _arraySubType, out sizeParamIndex, out sizeConst, out marshalType, out marshalCookie,
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
        internal MetadataImport(IntPtr metadataImport2, object keepalive)
        {
            m_metadataImport2 = metadataImport2;
            m_keepalive = keepalive;
        }
        #endregion

        #region FCalls
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern unsafe void _Enum(IntPtr scope, int type, int parent, out MetadataEnumResult result);

        public unsafe void Enum(MetadataTokenType type, int parent, out MetadataEnumResult result)
        {
            _Enum(m_metadataImport2, (int)type, parent, out result);
        }

        public unsafe void EnumNestedTypes(int mdTypeDef, out MetadataEnumResult result)
        {
            Enum(MetadataTokenType.TypeDef, mdTypeDef, out result);
        }

        public unsafe void EnumCustomAttributes(int mdToken, out MetadataEnumResult result)
        {
            Enum(MetadataTokenType.CustomAttribute, mdToken, out result);
        }

        public unsafe void EnumParams(int mdMethodDef, out MetadataEnumResult result)
        {
            Enum(MetadataTokenType.ParamDef, mdMethodDef, out result);
        }

        public unsafe void EnumFields(int mdTypeDef, out MetadataEnumResult result)
        {
            Enum(MetadataTokenType.FieldDef, mdTypeDef, out result);
        }

        public unsafe void EnumProperties(int mdTypeDef, out MetadataEnumResult result)
        {
            Enum(MetadataTokenType.Property, mdTypeDef, out result);
        }

        public unsafe void EnumEvents(int mdTypeDef, out MetadataEnumResult result)
        {
            Enum(MetadataTokenType.Event, mdTypeDef, out result);
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern string _GetDefaultValue(IntPtr scope, int mdToken, out long value, out int length, out int corElementType);
        public string GetDefaultValue(int mdToken, out long value, out int length, out CorElementType corElementType)
        {
            int _corElementType;
            string stringVal;
            stringVal = _GetDefaultValue(m_metadataImport2, mdToken, out value, out length, out _corElementType);
            corElementType = (CorElementType)_corElementType;
            return stringVal;
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern unsafe void _GetUserString(IntPtr scope, int mdToken, void** name, out int length);
        public unsafe string GetUserString(int mdToken)
        {
            void* name;
            int length;
            _GetUserString(m_metadataImport2, mdToken, &name, out length);

            return name != null ?
                new string((char*)name, 0, length) :
                null;
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern unsafe void _GetName(IntPtr scope, int mdToken, void** name);
        public unsafe MdUtf8String GetName(int mdToken)
        {
            void* name;
            _GetName(m_metadataImport2, mdToken, &name);

            return new MdUtf8String(name);
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern unsafe void _GetNamespace(IntPtr scope, int mdToken, void** namesp);
        public unsafe MdUtf8String GetNamespace(int mdToken)
        {
            void* namesp;
            _GetNamespace(m_metadataImport2, mdToken, &namesp);

            return new MdUtf8String(namesp);
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern unsafe void _GetEventProps(IntPtr scope, int mdToken, void** name, out int eventAttributes);
        public unsafe void GetEventProps(int mdToken, out void* name, out EventAttributes eventAttributes)
        {
            int _eventAttributes;
            void* _name;
            _GetEventProps(m_metadataImport2, mdToken, &_name, out _eventAttributes);
            name = _name;
            eventAttributes = (EventAttributes)_eventAttributes;
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern void _GetFieldDefProps(IntPtr scope, int mdToken, out int fieldAttributes);
        public void GetFieldDefProps(int mdToken, out FieldAttributes fieldAttributes)
        {
            int _fieldAttributes;
            _GetFieldDefProps(m_metadataImport2, mdToken, out _fieldAttributes);
            fieldAttributes = (FieldAttributes)_fieldAttributes;
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern unsafe void _GetPropertyProps(IntPtr scope,
            int mdToken, void** name, out int propertyAttributes, out ConstArray signature);
        public unsafe void GetPropertyProps(int mdToken, out void* name, out PropertyAttributes propertyAttributes, out ConstArray signature)
        {
            int _propertyAttributes;
            void* _name;
            _GetPropertyProps(m_metadataImport2, mdToken, &_name, out _propertyAttributes, out signature);
            name = _name;
            propertyAttributes = (PropertyAttributes)_propertyAttributes;
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern void _GetParentToken(IntPtr scope,
            int mdToken, out int tkParent);
        public int GetParentToken(int tkToken)
        {
            int tkParent;
            _GetParentToken(m_metadataImport2, tkToken, out tkParent);
            return tkParent;
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern void _GetParamDefProps(IntPtr scope,
            int parameterToken, out int sequence, out int attributes);
        public void GetParamDefProps(int parameterToken, out int sequence, out ParameterAttributes attributes)
        {
            int _attributes;

            _GetParamDefProps(m_metadataImport2, parameterToken, out sequence, out _attributes);

            attributes = (ParameterAttributes)_attributes;
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern void _GetGenericParamProps(IntPtr scope,
            int genericParameter,
            out int flags);

        public void GetGenericParamProps(
            int genericParameter,
            out GenericParameterAttributes attributes)
        {
            int _attributes;
            _GetGenericParamProps(m_metadataImport2, genericParameter, out _attributes);
            attributes = (GenericParameterAttributes)_attributes;
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
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

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern void _GetSigOfMethodDef(IntPtr scope,
            int methodToken,
            ref ConstArray signature);

        public ConstArray GetSigOfMethodDef(int methodToken)
        {
            ConstArray signature = new ConstArray();

            _GetSigOfMethodDef(m_metadataImport2, methodToken, ref signature);

            return signature;
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern void _GetSignatureFromToken(IntPtr scope,
            int methodToken,
            ref ConstArray signature);

        public ConstArray GetSignatureFromToken(int token)
        {
            ConstArray signature = new ConstArray();

            _GetSignatureFromToken(m_metadataImport2, token, ref signature);

            return signature;
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern void _GetMemberRefProps(IntPtr scope,
            int memberTokenRef,
            out ConstArray signature);

        public ConstArray GetMemberRefProps(int memberTokenRef)
        {
            ConstArray signature = new ConstArray();

            _GetMemberRefProps(m_metadataImport2, memberTokenRef, out signature);

            return signature;
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
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

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern void _GetClassLayout(IntPtr scope,
            int typeTokenDef, out int packSize, out int classSize);
        public void GetClassLayout(
            int typeTokenDef,
            out int packSize,
            out int classSize)
        {
            _GetClassLayout(m_metadataImport2, typeTokenDef, out packSize, out classSize);
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern bool _GetFieldOffset(IntPtr scope,
            int typeTokenDef, int fieldTokenDef, out int offset);
        public bool GetFieldOffset(
            int typeTokenDef,
            int fieldTokenDef,
            out int offset)
        {
            return _GetFieldOffset(m_metadataImport2, typeTokenDef, fieldTokenDef, out offset);
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern void _GetSigOfFieldDef(IntPtr scope,
            int fieldToken,
            ref ConstArray fieldMarshal);

        public ConstArray GetSigOfFieldDef(int fieldToken)
        {
            ConstArray fieldMarshal = new ConstArray();

            _GetSigOfFieldDef(m_metadataImport2, fieldToken, ref fieldMarshal);

            return fieldMarshal;
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern void _GetFieldMarshal(IntPtr scope,
            int fieldToken,
            ref ConstArray fieldMarshal);

        public ConstArray GetFieldMarshal(int fieldToken)
        {
            ConstArray fieldMarshal = new ConstArray();

            _GetFieldMarshal(m_metadataImport2, fieldToken, ref fieldMarshal);

            return fieldMarshal;
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
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
            int _attributes;
            void* _importName, _importDll;
            _GetPInvokeMap(m_metadataImport2, token, out _attributes, &_importName, &_importDll);
            importName = new MdUtf8String(_importName).ToString();
            importDll = new MdUtf8String(_importDll).ToString();

            attributes = (PInvokeAttributes)_attributes;
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern bool _IsValidToken(IntPtr scope, int token);
        public bool IsValidToken(int token)
        {
            return _IsValidToken(m_metadataImport2, token);
        }
        #endregion
    }


    internal class MetadataException : Exception
    {
        private int m_hr;
        internal MetadataException(int hr) { m_hr = hr; }

        public override string ToString()
        {
            return string.Format(CultureInfo.CurrentCulture, "MetadataException HResult = {0:x}.", m_hr);
        }
    }
}


