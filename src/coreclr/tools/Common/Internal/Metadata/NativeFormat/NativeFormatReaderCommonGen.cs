// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// NOTE: This is a generated file - do not manually edit!

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;

#pragma warning disable 108     // base type 'uint' is not CLS-compliant
#pragma warning disable 3009    // base type 'uint' is not CLS-compliant
#pragma warning disable 282     // There is no defined ordering between fields in multiple declarations of partial class or struct

namespace Internal.Metadata.NativeFormat
{
    // Internal clone of System.Reflection.AssemblyFlags from System.Reflection.Metadata
    [Flags]
#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
#endif
    public enum AssemblyFlags : uint
    {
        /// The assembly reference holds the full (unhashed) public key.
        PublicKey = 0x1,

        /// The implementation of this assembly used at runtime is not expected to match the version seen at compile time.
        Retargetable = 0x100,

        /// Content type mask. Masked bits correspond to values of System.Reflection.AssemblyContentType
        ContentTypeMask = 0x00000e00,

    } // AssemblyFlags

    // Internal clone of System.Reflection.AssemblyHashAlgorithm from System.Reflection.Metadata
#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
#endif
    public enum AssemblyHashAlgorithm : uint
    {
        None = 0x0,
        Reserved = 0x8003,
        SHA1 = 0x8004,
    } // AssemblyHashAlgorithm

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
#endif
    public enum GenericParameterKind : byte
    {
        /// Represents a type parameter for a generic type.
        GenericTypeParameter = 0x0,

        /// Represents a type parameter from a generic method.
        GenericMethodParameter = 0x1,
    } // GenericParameterKind

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
#endif
    public enum NamedArgumentMemberKind : byte
    {
        /// Specifies the name of a property
        Property = 0x0,

        /// Specifies the name of a field
        Field = 0x1,
    } // NamedArgumentMemberKind

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
#endif
    public enum SignatureCallingConvention : byte
    {
        HasThis = 0x20,
        ExplicitThis = 0x40,
        Default = 0x00,
        Vararg = 0x05,
        Cdecl = 0x01,
        StdCall = 0x02,
        ThisCall = 0x03,
        FastCall = 0x04,
        Unmanaged = 0x09,
        UnmanagedCallingConventionMask = 0x0F,
    } // SignatureCallingConvention

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
#endif
    public enum HandleType : byte
    {
        Null = 0x0,
        ArraySignature = 0x41,
        ByReferenceSignature = 0x42,
        ConstantBooleanArray = 0x43,
        ConstantBooleanValue = 0x44,
        ConstantByteArray = 0x45,
        ConstantByteValue = 0x46,
        ConstantCharArray = 0x47,
        ConstantCharValue = 0x48,
        ConstantDoubleArray = 0x49,
        ConstantDoubleValue = 0x4a,
        ConstantEnumArray = 0x4b,
        ConstantEnumValue = 0x4c,
        ConstantHandleArray = 0x4d,
        ConstantInt16Array = 0x4e,
        ConstantInt16Value = 0x4f,
        ConstantInt32Array = 0x50,
        ConstantInt32Value = 0x51,
        ConstantInt64Array = 0x52,
        ConstantInt64Value = 0x53,
        ConstantReferenceValue = 0x54,
        ConstantSByteArray = 0x55,
        ConstantSByteValue = 0x56,
        ConstantSingleArray = 0x57,
        ConstantSingleValue = 0x58,
        ConstantStringArray = 0x59,
        ConstantStringValue = 0x5a,
        ConstantUInt16Array = 0x5b,
        ConstantUInt16Value = 0x5c,
        ConstantUInt32Array = 0x5d,
        ConstantUInt32Value = 0x5e,
        ConstantUInt64Array = 0x5f,
        ConstantUInt64Value = 0x60,
        CustomAttribute = 0x61,
        Event = 0x62,
        Field = 0x63,
        FieldSignature = 0x64,
        FunctionPointerSignature = 0x65,
        GenericParameter = 0x66,
        MemberReference = 0x67,
        Method = 0x68,
        MethodInstantiation = 0x69,
        MethodSemantics = 0x6a,
        MethodSignature = 0x6b,
        MethodTypeVariableSignature = 0x6c,
        ModifiedType = 0x6d,
        NamedArgument = 0x6e,
        NamespaceDefinition = 0x6f,
        NamespaceReference = 0x70,
        Parameter = 0x71,
        PointerSignature = 0x72,
        Property = 0x73,
        PropertySignature = 0x74,
        QualifiedField = 0x75,
        QualifiedMethod = 0x76,
        SZArraySignature = 0x77,
        ScopeDefinition = 0x78,
        ScopeReference = 0x79,
        TypeDefinition = 0x7a,
        TypeForwarder = 0x7b,
        TypeInstantiationSignature = 0x7c,
        TypeReference = 0x7d,
        TypeSpecification = 0x7e,
        TypeVariableSignature = 0x7f,
    } // HandleType
} // Internal.Metadata.NativeFormat
