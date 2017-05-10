// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Runtime.Serialization.Formatters.Binary
{
    // BinaryHeaderEnum is the first byte on binary records (except for primitive types which do not have a header)
    internal enum BinaryHeaderEnum
    {
        SerializedStreamHeader = 0,
        Object = 1,
        ObjectWithMap = 2,
        ObjectWithMapAssemId = 3,
        ObjectWithMapTyped = 4,
        ObjectWithMapTypedAssemId = 5,
        ObjectString = 6,
        Array = 7,
        MemberPrimitiveTyped = 8,
        MemberReference = 9,
        ObjectNull = 10,
        MessageEnd = 11,
        Assembly = 12,
        ObjectNullMultiple256 = 13,
        ObjectNullMultiple = 14,
        ArraySinglePrimitive = 15,
        ArraySingleObject = 16,
        ArraySingleString = 17,
        CrossAppDomainMap = 18,
        CrossAppDomainString = 19,
        CrossAppDomainAssembly = 20,
        MethodCall = 21,
        MethodReturn = 22,
    }

    // BinaryTypeEnum is used specify the type on the wire. Additional information is transmitted with Primitive and Object types
    internal enum BinaryTypeEnum
    {
        Primitive = 0,
        String = 1,
        Object = 2,
        ObjectUrt = 3,
        ObjectUser = 4,
        ObjectArray = 5,
        StringArray = 6,
        PrimitiveArray = 7,
    }

    internal enum BinaryArrayTypeEnum
    {
        Single = 0,
        Jagged = 1,
        Rectangular = 2,
        SingleOffset = 3,
        JaggedOffset = 4,
        RectangularOffset = 5,
    }

    // Enums are for internal use by the XML and Binary Serializers

    // Formatter Enums
    internal enum InternalSerializerTypeE
    {
        Soap = 1,
        Binary = 2,
    }

    // ParseRecord Enums
    internal enum InternalParseTypeE
    {
        Empty = 0,
        SerializedStreamHeader = 1,
        Object = 2,
        Member = 3,
        ObjectEnd = 4,
        MemberEnd = 5,
        Headers = 6,
        HeadersEnd = 7,
        SerializedStreamHeaderEnd = 8,
        Envelope = 9,
        EnvelopeEnd = 10,
        Body = 11,
        BodyEnd = 12,
    }

    internal enum InternalObjectTypeE
    {
        Empty = 0,
        Object = 1,
        Array = 2,
    }

    internal enum InternalObjectPositionE
    {
        Empty = 0,
        Top = 1,
        Child = 2,
        Headers = 3,
    }

    internal enum InternalArrayTypeE
    {
        Empty = 0,
        Single = 1,
        Jagged = 2,
        Rectangular = 3,
        Base64 = 4,
    }

    internal enum InternalMemberTypeE
    {
        Empty = 0,
        Header = 1,
        Field = 2,
        Item = 3,
    }

    internal enum InternalMemberValueE
    {
        Empty = 0,
        InlineValue = 1,
        Nested = 2,
        Reference = 3,
        Null = 4,
    }

    // Data Type Enums
    internal enum InternalPrimitiveTypeE
    {
        Invalid = 0,
        Boolean = 1,
        Byte = 2,
        Char = 3,
        Currency = 4,
        Decimal = 5,
        Double = 6,
        Int16 = 7,
        Int32 = 8,
        Int64 = 9,
        SByte = 10,
        Single = 11,
        TimeSpan = 12,
        DateTime = 13,
        UInt16 = 14,
        UInt32 = 15,
        UInt64 = 16,

        // Used in only for MethodCall or MethodReturn header
        Null = 17,
        String = 18,
    }

    // ValueType Fixup Enum
    internal enum ValueFixupEnum
    {
        Empty = 0,
        Array = 1,
        Header = 2,
        Member = 3,
    }

    // name space
    internal enum InternalNameSpaceE
    {
        None = 0,
        Soap = 1,
        XdrPrimitive = 2,
        XdrString = 3,
        UrtSystem = 4,
        UrtUser = 5,
        UserNameSpace = 6,
        MemberName = 7,
        Interop = 8,
        CallElement = 9
    }
}
