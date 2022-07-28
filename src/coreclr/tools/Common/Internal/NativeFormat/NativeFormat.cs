// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

//
// Native Format
//
// NativeFormat is a binary metadata format. It primarily designed for storing layout decisions done by static code
// generator that the dynamic code generator needs to be aware of. However, it can be also used for storing general
// managed code metadata in future. The key properties of the format are:
//
// - Extensible: It should be possible to attach new data to existing records without breaking existing consumers that
//   do not understand the new data yet.
//
// - Naturally compressed: Integers are stored using variable length encoding. Offsets are stored as relative offsets.
//
// - Random access: Random access to selected information should be fast. It is achieved by using tokens as offsets.
//
// - Locality: Access to related information should be accessing data that are close to each other.
//
// The format is essentially a collection of variable size records that can reference each other.
//

namespace Internal.NativeFormat
{
    //
    // Bag is the key record type for extensibility. It is a list <id, data> pairs. Data is integer that
    // is interpretted according to the id. It is typically relative offset of another record.
    //
#if NATIVEFORMAT_PUBLICWRITER
    public
#else
    internal
#endif
    enum BagElementKind : uint
    {
        End                         = 0x00,
        BaseType                    = 0x01,
        ImplementedInterfaces       = 0x02,

        DictionaryLayout            = 0x40,
        TypeFlags                   = 0x41,
        NonGcStaticData             = 0x42,
        GcStaticData                = 0x43,
        NonGcStaticDataSize         = 0x44,
        GcStaticDataSize            = 0x45,
        GcStaticDesc                = 0x46,
        ThreadStaticDataSize        = 0x47,
        ThreadStaticDesc            = 0x48,
        ThreadStaticIndex           = 0x49,
        ThreadStaticOffset          = 0x4a,
        FieldLayout                 = 0x4b,
        VTableMethodSignatures      = 0x4c,
        SealedVTableEntries         = 0x4d,
        ClassConstructorPointer     = 0x4e,
        BaseTypeSize                = 0x4f,
        GenericVarianceInfo         = 0x50,
        DelegateInvokeSignature     = 0x51,

        // Add new custom bag elements that don't match to something you'd find in the ECMA metadata here.
    }

    //
    // FixupSignature signature describes indirection. It starts with integer describing the kind of data stored in the indirection,
    // followed by kind-specific signature.
    //
#if NATIVEFORMAT_PUBLICWRITER
    public
#else
    internal
#endif
    enum FixupSignatureKind : uint
    {
        Null                        = 0x00,
        TypeHandle                  = 0x01,
        InterfaceCall               = 0x02,
        // unused                   = 0x03,
        MethodDictionary            = 0x04,
        StaticData                  = 0x05,
        UnwrapNullableType          = 0x06,
        FieldLdToken                = 0x07,
        MethodLdToken               = 0x08,
        AllocateObject              = 0x09,
        DefaultConstructor          = 0x0a,
        ThreadStaticIndex           = 0x0b,
        // unused                   = 0x0c,
        Method                      = 0x0d,
        IsInst                      = 0x0e,
        CastClass                   = 0x0f,
        AllocateArray               = 0x10,
        // unused                   = 0x11,
        TypeSize                    = 0x12,
        FieldOffset                 = 0x13,
        CallingConventionConverter  = 0x14,
        VTableOffset                = 0x15,
        NonGenericConstrainedMethod = 0x16,
        GenericConstrainedMethod    = 0x17,
        NonGenericDirectConstrainedMethod = 0x18,
        PointerToOtherSlot          = 0x19,
        IntValue                    = 0x20,
        NonGenericStaticConstrainedMethod = 0x21,
        GenericStaticConstrainedMethod = 0x22,

        NotYetSupported             = 0xee,
    }

    //
    // TypeSignature describes type. The low 4 bits of the integer that is starts with describe the kind. Upper 28 bits are kind
    // specific data. The argument signatures immediately follow for nested types.
    //
#if NATIVEFORMAT_PUBLICWRITER
    public
#else
    internal
#endif
    enum TypeSignatureKind : uint
    {
        Null                        = 0x0,
        Lookback                    = 0x1, // Go back in the stream for signature continuation (data - number of bytes to go back)
        Modifier                    = 0x2, // Type modifier (data - TypeModifierKind)
        Instantiation               = 0x3, // Generic instantiation (data - number of instantiation args)
        Variable                    = 0x4, // Generic variable (data - 2 * varnum + method)
        BuiltIn                     = 0x5, // Built-in type (data - BuildInTypeKind)
        External                    = 0x6, // External type reference (data - external type id)

        MultiDimArray               = 0xA, // Multi-dimensional array (data - dimension)
        FunctionPointer             = 0xB, // Function pointer (data - calling convention, arg count, args)
    };

#if NATIVEFORMAT_PUBLICWRITER
    public
#else
    internal
#endif
    enum TypeModifierKind : uint
    {
        Array                       = 0x1,
        ByRef                       = 0x2,
        Pointer                     = 0x3,
    };

#if NATIVEFORMAT_PUBLICWRITER
    public
#else
    internal
#endif
    enum StaticDataKind : uint
    {
        Gc                          = 0x1,
        NonGc                       = 0x2,
    };

#if NATIVEFORMAT_PUBLICWRITER
    public
#else
    internal
#endif
    enum GenericContextKind : uint
    {
        FromThis                    = 0x00,
        FromHiddenArg               = 0x01,
        FromMethodHiddenArg         = 0x02,

        HasDeclaringType            = 0x04,

        NeedsUSGContext             = 0x08,
    };

#if NATIVEFORMAT_PUBLICWRITER
    public
#else
    internal
#endif
    enum CallingConventionConverterKind : uint
    {
        NoInstantiatingParam        = 0x00,   // The calling convention interpreter can assume that the calling convention of the target method has no instantiating parameter
        HasInstantiatingParam       = 0x01,   // The calling convention interpreter can assume that the calling convention of the target method has an instantiating parameter
        MaybeInstantiatingParam     = 0x02,   // The calling convention interpreter can assume that the calling convention of the target method may be a fat function pointer
    }

    [Flags]
#if NATIVEFORMAT_PUBLICWRITER
    public
#else
    internal
#endif
    enum TypeFlags : uint
    {
        HasClassConstructor             = 0x1,
        HasInstantiationDeterminedSize  = 0x2,
    };

    [Flags]
#if NATIVEFORMAT_PUBLICWRITER
    public
#else
    internal
#endif
    enum MethodFlags : uint
    {
        HasInstantiation            = 0x1,
        IsUnboxingStub              = 0x2,
        HasFunctionPointer          = 0x4,
        FunctionPointerIsUSG        = 0x8,
    };

    [Flags]
#if NATIVEFORMAT_PUBLICWRITER
    public
#else
    internal
#endif
    enum MethodCallingConvention : uint
    {
        Generic                     = 0x1,
        Static                      = 0x2,
    };

#if NATIVEFORMAT_PUBLICWRITER
    public
#else
    internal
#endif
    enum FieldStorage : uint
    {
        Instance                    = 0x0,
        NonGCStatic                 = 0x1,
        GCStatic                    = 0x2,
        TLSStatic                   = 0x3,
    }
}
