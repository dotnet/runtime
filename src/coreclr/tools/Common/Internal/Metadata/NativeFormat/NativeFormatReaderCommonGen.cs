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
        ArraySignature = 0x1,
        ByReferenceSignature = 0x2,
        ConstantBooleanArray = 0x3,
        ConstantBooleanValue = 0x4,
        ConstantByteArray = 0x5,
        ConstantByteValue = 0x6,
        ConstantCharArray = 0x7,
        ConstantCharValue = 0x8,
        ConstantDoubleArray = 0x9,
        ConstantDoubleValue = 0xa,
        ConstantEnumArray = 0xb,
        ConstantEnumValue = 0xc,
        ConstantHandleArray = 0xd,
        ConstantInt16Array = 0xe,
        ConstantInt16Value = 0xf,
        ConstantInt32Array = 0x10,
        ConstantInt32Value = 0x11,
        ConstantInt64Array = 0x12,
        ConstantInt64Value = 0x13,
        ConstantReferenceValue = 0x14,
        ConstantSByteArray = 0x15,
        ConstantSByteValue = 0x16,
        ConstantSingleArray = 0x17,
        ConstantSingleValue = 0x18,
        ConstantStringArray = 0x19,
        ConstantStringValue = 0x1a,
        ConstantUInt16Array = 0x1b,
        ConstantUInt16Value = 0x1c,
        ConstantUInt32Array = 0x1d,
        ConstantUInt32Value = 0x1e,
        ConstantUInt64Array = 0x1f,
        ConstantUInt64Value = 0x20,
        CustomAttribute = 0x21,
        Event = 0x22,
        Field = 0x23,
        FieldSignature = 0x24,
        FunctionPointerSignature = 0x25,
        GenericParameter = 0x26,
        MemberReference = 0x27,
        Method = 0x28,
        MethodInstantiation = 0x29,
        MethodSemantics = 0x2a,
        MethodSignature = 0x2b,
        MethodTypeVariableSignature = 0x2c,
        ModifiedType = 0x2d,
        NamedArgument = 0x2e,
        NamespaceDefinition = 0x2f,
        NamespaceReference = 0x30,
        Parameter = 0x31,
        PointerSignature = 0x32,
        Property = 0x33,
        PropertySignature = 0x34,
        QualifiedField = 0x35,
        QualifiedMethod = 0x36,
        SZArraySignature = 0x37,
        ScopeDefinition = 0x38,
        ScopeReference = 0x39,
        TypeDefinition = 0x3a,
        TypeForwarder = 0x3b,
        TypeInstantiationSignature = 0x3c,
        TypeReference = 0x3d,
        TypeSpecification = 0x3e,
        TypeVariableSignature = 0x3f,
    } // HandleType
} // Internal.Metadata.NativeFormat
