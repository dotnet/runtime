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

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
#endif
    public enum ConstantTypeCode : byte
    {
        Boolean = 0x02,
        Char = 0x03,
        SByte = 0x04,
        Byte = 0x05,
        Int16 = 0x06,
        UInt16 = 0x07,
        Int32 = 0x08,
        UInt32 = 0x09,
        Int64 = 0x0a,
        UInt64 = 0x0b,
        Single = 0x0c,
        Double = 0x0d,
        String = 0x0e,
        NullReference = 0x12,
    } // ConstantTypeCode

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
        Constant = 0x1,
        ConstantStringArray = 0x2,
        ConstantStringValue = 0x3,
        CustomAttribute = 0x4,
        Event = 0x5,
        Field = 0x6,
        FieldSignature = 0x7,
        GenericParameter = 0x8,
        MemberReference = 0x9,
        Method = 0xa,
        MethodInstantiation = 0xb,
        MethodSemantics = 0xc,
        MethodSignature = 0xd,
        NamespaceDefinition = 0xe,
        NamespaceReference = 0xf,
        Parameter = 0x10,
        Property = 0x11,
        PropertySignature = 0x12,
        QualifiedField = 0x13,
        QualifiedMethod = 0x14,
        ScopeDefinition = 0x15,
        ScopeReference = 0x16,
        TypeDefinition = 0x17,
        TypeForwarder = 0x18,
        TypeReference = 0x19,
        TypeSpecification = 0x1a,
    } // HandleType
} // Internal.Metadata.NativeFormat
