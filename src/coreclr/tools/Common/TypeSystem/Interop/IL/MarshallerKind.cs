// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Internal.TypeSystem.Interop
{
    internal enum MarshallerKind
    {
        Unknown,
        BlittableValue,
        Array,
        BlittableArray,
        Bool,   // 4 byte bool
        CBool,  // 1 byte bool
        VariantBool,  // Variant bool
        Enum,
        AnsiChar,  // Marshal char (Unicode 16bits) for byte (Ansi 8bits)
        UnicodeChar,
        AnsiCharArray,
        ByValArray,
        ByValAnsiCharArray, // Particular case of ByValArray because the conversion between wide Char and Byte need special treatment.
        AnsiString,
        UnicodeString,
        UTF8String,
        AnsiBSTRString,
        BSTRString,
        ByValAnsiString,
        ByValUnicodeString,
        AnsiStringBuilder,
        UnicodeStringBuilder,
        FunctionPointer,
        SafeHandle,
        CriticalHandle,
        HandleRef,
        VoidReturn,
        Variant,
        Object,
        OleDateTime,
        Decimal,
        OleCurrency,
        Guid,
        Struct,
        BlittableStruct,
        BlittableStructPtr,   // Additional indirection on top of blittable struct. Used by MarshalAs(LpStruct)
        LayoutClass,
        LayoutClassPtr,
        AsAnyA,
        AsAnyW,
        FailedTypeLoad,
        ComInterface,
        BlittableValueClassByRefReturn,
        BlittableValueClassWithCopyCtor,
        CustomMarshaler,
        Invalid
    }

    public enum MarshallerType
    {
        Argument,
        Element,
        Field
    }
}
