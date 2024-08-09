// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection.Metadata;
using System.Formats.Nrbf.Utils;

namespace System.Formats.Nrbf;

/// <summary>
/// Member type info.
/// </summary>
/// <remarks>
/// MemberTypeInfo structures are described in <see href="https://learn.microsoft.com/openspecs/windows_protocols/ms-nrbf/aa509b5a-620a-4592-a5d8-7e9613e0a03e">[MS-NRBF] 2.3.1.2</see>.
/// </remarks>
internal readonly struct MemberTypeInfo
{
    internal MemberTypeInfo(IReadOnlyList<(BinaryType BinaryType, object? AdditionalInfo)> infos) => _infos = infos;

    private readonly IReadOnlyList<(BinaryType BinaryType, object? AdditionalInfo)> _infos;

    internal IReadOnlyList<(BinaryType BinaryType, object? AdditionalInfo)> Infos => _infos;

    internal static MemberTypeInfo Decode(BinaryReader reader, int count, PayloadOptions options, RecordMap recordMap)
    {
        List<(BinaryType BinaryType, object? AdditionalInfo)> info = [];

        // [MS-NRBF] 2.3.1.2
        // https://docs.microsoft.com/en-us/openspecs/windows_protocols/ms-nrbf/aa509b5a-620a-4592-a5d8-7e9613e0a03e

        // All of the BinaryTypeEnumeration values come before all of the AdditionalInfo values.
        // There's not necessarily a 1:1 mapping; some enum values don't have associated AdditionalInfo.
        for (int i = 0; i < count; i++)
        {
            info.Add((reader.ReadBinaryType(), null));
        }

        // Check for more clarifying information
        for (int i = 0; i < info.Count; i++)
        {
            BinaryType type = info[i].BinaryType;
            switch (type)
            {
                case BinaryType.Primitive:
                case BinaryType.PrimitiveArray:
                    info[i] = (type, reader.ReadPrimitiveType());
                    break;
                case BinaryType.SystemClass:
                    info[i] = (type, reader.ReadString().ParseSystemRecordTypeName(options));
                    break;
                case BinaryType.Class:
                    info[i] = (type, ClassTypeInfo.Decode(reader, options, recordMap));
                    break;
                default:
                    // Other types have no additional data.
                    Debug.Assert(type is BinaryType.String or BinaryType.ObjectArray or BinaryType.StringArray or BinaryType.Object);
                    break;
            }
        }

        return new MemberTypeInfo(info);
    }

    internal (AllowedRecordTypes allowed, PrimitiveType primitiveType) GetNextAllowedRecordType(int currentValuesCount)
    {
        (BinaryType binaryType, object? additionalInfo) = Infos[currentValuesCount];

        // Every array can be either an array itself, a null or a reference (to an array)
        const AllowedRecordTypes StringArray = AllowedRecordTypes.ArraySingleString
            | AllowedRecordTypes.ObjectNull | AllowedRecordTypes.MemberReference;
        const AllowedRecordTypes PrimitiveArray = AllowedRecordTypes.ArraySinglePrimitive
            | AllowedRecordTypes.ObjectNull | AllowedRecordTypes.MemberReference;
        const AllowedRecordTypes ObjectArray = AllowedRecordTypes.ArraySingleObject
            | AllowedRecordTypes.ObjectNull | AllowedRecordTypes.MemberReference;

        // Every string can be a string, a null or a reference (to a string)
        const AllowedRecordTypes Strings = AllowedRecordTypes.BinaryObjectString
            | AllowedRecordTypes.ObjectNull | AllowedRecordTypes.MemberReference;

        // Every class can be a null or a reference and a ClassWithId
        const AllowedRecordTypes Classes = AllowedRecordTypes.ClassWithId
            | AllowedRecordTypes.ObjectNull | AllowedRecordTypes.MemberReference
            | AllowedRecordTypes.MemberPrimitiveTyped
            | AllowedRecordTypes.BinaryLibrary; // Classes may be preceded with a library record (System too!)
        // but System Classes can be expressed only by System records
        const AllowedRecordTypes SystemClass = Classes | AllowedRecordTypes.SystemClassWithMembersAndTypes;
        const AllowedRecordTypes NonSystemClass = Classes |  AllowedRecordTypes.ClassWithMembersAndTypes;

        return binaryType switch
        {
            BinaryType.Primitive => (default, (PrimitiveType)additionalInfo!),
            BinaryType.String => (Strings, default),
            BinaryType.Object => (AllowedRecordTypes.AnyObject, default),
            BinaryType.StringArray => (StringArray, default),
            BinaryType.PrimitiveArray => (PrimitiveArray, default),
            BinaryType.Class => (NonSystemClass, default),
            BinaryType.SystemClass => (SystemClass, default),
            _ => (ObjectArray, default)
        };
    }

    internal bool ShouldBeRepresentedAsArrayOfClassRecords()
    {
        // This library tries to minimize the number of concepts the users need to learn to use it.
        // Since SZArrays are most common, it provides an SZArrayRecord<T> abstraction.
        // Every other array (jagged, multi-dimensional etc) is represented using SZArrayRecord.
        // The goal of this method is to determine whether given array can be represented as SZArrayRecord<ClassRecord>.

        (BinaryType binaryType, object? additionalInfo) = Infos[0];

        if (binaryType == BinaryType.Class)
        {
            // An array of arrays can not be represented as SZArrayRecord<ClassRecord>.
            return !((ClassTypeInfo)additionalInfo!).TypeName.IsArray;
        }
        else if (binaryType == BinaryType.SystemClass)
        {
            TypeName typeName = (TypeName)additionalInfo!;

            // An array of arrays can not be represented as SZArrayRecord<ClassRecord>.
            if (typeName.IsArray)
            {
                return false;
            }

            if (!typeName.IsConstructedGenericType)
            {
                return true;
            }

            // Can't use SZArrayRecord<ClassRecord> for Nullable<T>[]
            // as it consists of MemberPrimitiveTypedRecord and NullsRecord
            return typeName.GetGenericTypeDefinition().FullName != typeof(Nullable<>).FullName;
        }

        return false;
    }

    internal TypeName GetArrayTypeName(ArrayInfo arrayInfo)
    {
        (BinaryType binaryType, object? additionalInfo) = Infos[0];

        TypeName elementTypeName = binaryType switch
        {
            BinaryType.String => TypeNameHelpers.GetPrimitiveTypeName(PrimitiveType.String),
            BinaryType.StringArray => TypeNameHelpers.GetPrimitiveSZArrayTypeName(PrimitiveType.String),
            BinaryType.Primitive => TypeNameHelpers.GetPrimitiveTypeName((PrimitiveType)additionalInfo!),
            BinaryType.PrimitiveArray => TypeNameHelpers.GetPrimitiveSZArrayTypeName((PrimitiveType)additionalInfo!),
            BinaryType.Object => TypeNameHelpers.GetPrimitiveTypeName(TypeNameHelpers.ObjectPrimitiveType),
            BinaryType.ObjectArray => TypeNameHelpers.GetPrimitiveSZArrayTypeName(TypeNameHelpers.ObjectPrimitiveType),
            BinaryType.SystemClass => (TypeName)additionalInfo!,
            _ => ((ClassTypeInfo)additionalInfo!).TypeName,
        };

        // In general, arrayRank == 1 may have two different meanings:
        // - [] is a single dimension and zero-indexed array (SZArray)
        // - [*] is single dimension, custom offset array.
        // Custom offset arrays are not supported by design, so in our case it's always SZArray.
        // That is why we don't call TypeName.MakeArrayTypeName(1) because it would create [*] instead of [] name.
        return arrayInfo.Rank == 1
            ? elementTypeName.MakeSZArrayTypeName()
            : elementTypeName.MakeArrayTypeName(arrayInfo.Rank);
    }
}
