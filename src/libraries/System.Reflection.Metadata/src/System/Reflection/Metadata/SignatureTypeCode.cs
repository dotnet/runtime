// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection.Metadata.Ecma335;

namespace System.Reflection.Metadata
{
    /// <summary>
    /// Represents the type codes that are used in signature encoding.
    /// </summary>
    public enum SignatureTypeCode : byte
    {
        /// <summary>
        /// Represents an invalid or uninitialized type code. It will not appear in valid signatures.
        /// </summary>
        Invalid = 0x0,

        /// <summary>
        /// Represents <see cref="System.Void"/> in signatures.
        /// </summary>
        Void = CorElementType.ELEMENT_TYPE_VOID,

        /// <summary>
        /// Represents <see cref="System.Boolean"/> in signatures.
        /// </summary>
        Boolean = CorElementType.ELEMENT_TYPE_BOOLEAN,

        /// <summary>
        /// Represents <see cref="System.Char"/> in signatures.
        /// </summary>
        Char = CorElementType.ELEMENT_TYPE_CHAR,

        /// <summary>
        /// Represents <see cref="System.SByte"/> in signatures.
        /// </summary>
        SByte = CorElementType.ELEMENT_TYPE_I1,

        /// <summary>
        /// Represents <see cref="System.Byte"/> in signatures.
        /// </summary>
        Byte = CorElementType.ELEMENT_TYPE_U1,

        /// <summary>
        /// Represents <see cref="System.Int16"/> in signatures.
        /// </summary>
        Int16 = CorElementType.ELEMENT_TYPE_I2,

        /// <summary>
        /// Represents <see cref="System.UInt16"/> in signatures.
        /// </summary>
        UInt16 = CorElementType.ELEMENT_TYPE_U2,

        /// <summary>
        /// Represents <see cref="System.Int32"/> in signatures.
        /// </summary>
        Int32 = CorElementType.ELEMENT_TYPE_I4,

        /// <summary>
        /// Represents <see cref="System.UInt32"/> in signatures.
        /// </summary>
        UInt32 = CorElementType.ELEMENT_TYPE_U4,

        /// <summary>
        /// Represents <see cref="System.Int64"/> in signatures.
        /// </summary>
        Int64 = CorElementType.ELEMENT_TYPE_I8,

        /// <summary>
        /// Represents <see cref="System.UInt64"/> in signatures.
        /// </summary>
        UInt64 = CorElementType.ELEMENT_TYPE_U8,

        /// <summary>
        /// Represents <see cref="System.Single"/> in signatures.
        /// </summary>
        Single = CorElementType.ELEMENT_TYPE_R4,

        /// <summary>
        /// Represents <see cref="System.Double"/> in signatures.
        /// </summary>
        Double = CorElementType.ELEMENT_TYPE_R8,

        /// <summary>
        /// Represents <see cref="System.String"/> in signatures.
        /// </summary>
        String = CorElementType.ELEMENT_TYPE_STRING,

        // every type above PTR will be simple type

        /// <summary>
        /// Represents a unmanaged pointers in signatures.
        /// It is followed in the blob by the signature encoding of the underlying type.
        /// </summary>
        Pointer = CorElementType.ELEMENT_TYPE_PTR,                    // PTR <type>

        /// <summary>
        /// Represents managed pointers (byref return values and parameters) in signatures.
        /// It is followed in the blob by the signature encoding of the underlying type.
        /// </summary>
        ByReference = CorElementType.ELEMENT_TYPE_BYREF,              // BYREF <type>

        // ELEMENT_TYPE_VALUETYPE (0x11) and ELEMENT_TYPE_CLASS (0x12) are unified to ELEMENT_TYPE_HANDLE.

        /// <summary>
        /// Represents a generic type parameter used within a signature. 
        /// </summary>
        GenericTypeParameter = CorElementType.ELEMENT_TYPE_VAR,        // a class type variable VAR <U1>

        /// <summary>
        /// Represents a generalized <see cref="System.Array"/> in signatures.
        /// </summary>
        Array = CorElementType.ELEMENT_TYPE_ARRAY,                     // MDARRAY <type> <rank> <bcount> <bound1> ... <lbcount> <lb1> ...
        /// <summary>
        /// Represents the instantiation of a generic type in signatures.
        /// </summary>
        GenericTypeInstance = CorElementType.ELEMENT_TYPE_GENERICINST, // GENERICINST <generic type> <argCnt> <arg1> ... <argn>

        // Doc issue: We can't use <see cref> because  System.TypedReference isn't portable.
        /// <summary>
        /// Represents a System.TypedReference in signatures.
        /// </summary>
        TypedReference = CorElementType.ELEMENT_TYPE_TYPEDBYREF,       // TYPEDREF  (it takes no args) a typed reference to some other type

        /// <summary>
        /// Represents a <see cref="System.IntPtr"/> in signatures.
        /// </summary>
        IntPtr = CorElementType.ELEMENT_TYPE_I,

        /// <summary>
        /// Represents a <see cref="System.UIntPtr"/> in signatures.
        /// </summary>
        UIntPtr = CorElementType.ELEMENT_TYPE_U,

        /// <summary>
        /// Represents function pointer types in signatures. 
        /// </summary>
        FunctionPointer = CorElementType.ELEMENT_TYPE_FNPTR,           // FNPTR <complete sig for the function including calling convention>

        /// <summary>
        /// Represents <see cref="System.Object"/>
        /// </summary>
        Object = CorElementType.ELEMENT_TYPE_OBJECT,

        /// <summary>
        /// Represents a single dimensional <see cref="System.Array"/> with 0 lower bound.
        /// </summary>
        SZArray = CorElementType.ELEMENT_TYPE_SZARRAY,

        // SZARRAY <type>

        /// <summary>
        /// Represents a generic method parameter used within a signature. 
        /// </summary>
        GenericMethodParameter = CorElementType.ELEMENT_TYPE_MVAR,     // a method type variable MVAR <U1>

        // This is only for binding
        /// <summary>
        /// Represents a custom modifier applied to a type within a signature that the caller must understand. 
        /// </summary>
        RequiredModifier = CorElementType.ELEMENT_TYPE_CMOD_REQD,      // required C modifier : E_T_CMOD_REQD <mdTypeRef/mdTypeDef>

        /// <summary>
        /// Represents a custom modifier applied to a type within a signature that the caller can ignore.
        /// </summary>
        OptionalModifier = CorElementType.ELEMENT_TYPE_CMOD_OPT,       // optional C modifier : E_T_CMOD_OPT <mdTypeRef/mdTypeDef>

        /// <summary>
        /// Precedes a type <see cref="EntityHandle"/> in signatures.
        /// </summary>
        /// <remarks>
        /// In raw metadata, this will be encoded as either ELEMENT_TYPE_CLASS (0x12) for reference
        /// types and ELEMENT_TYPE_VALUETYPE (0x11) for value types. This is collapsed to a single
        /// code because Windows Runtime projections can project from class to value type or vice-versa
        /// and the raw code is misleading in those cases.
        /// </remarks>
        TypeHandle = CorElementType.ELEMENT_TYPE_HANDLE,               // CLASS | VALUETYPE <class Token>

        /// <summary>
        /// Represents a marker to indicate the end of fixed arguments and the beginning of variable arguments. 
        /// </summary>
        Sentinel = CorElementType.ELEMENT_TYPE_SENTINEL,

        /// <summary>
        /// Represents a local variable that is pinned by garbage collector
        /// </summary>
        Pinned = CorElementType.ELEMENT_TYPE_PINNED,
    }
}