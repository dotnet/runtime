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

        switch (binaryType)
        {
            case BinaryType.String:
                return typeof(string).BuildCoreLibArrayTypeName(arrayInfo.Rank);
            case BinaryType.StringArray:
                return typeof(string[]).BuildCoreLibArrayTypeName(arrayInfo.Rank);
            case BinaryType.Object:
                return typeof(object).BuildCoreLibArrayTypeName(arrayInfo.Rank);
            case BinaryType.ObjectArray:
                return typeof(object[]).BuildCoreLibArrayTypeName(arrayInfo.Rank);
            case BinaryType.Primitive:
                Type primitiveType = ((PrimitiveType)additionalInfo!) switch
                {
                    PrimitiveType.Boolean => typeof(bool),
                    PrimitiveType.Byte => typeof(byte),
                    PrimitiveType.Char => typeof(char),
                    PrimitiveType.Decimal => typeof(decimal),
                    PrimitiveType.Double => typeof(double),
                    PrimitiveType.Int16 => typeof(short),
                    PrimitiveType.Int32 => typeof(int),
                    PrimitiveType.Int64 => typeof(long),
                    PrimitiveType.SByte => typeof(sbyte),
                    PrimitiveType.Single => typeof(float),
                    PrimitiveType.TimeSpan => typeof(TimeSpan),
                    PrimitiveType.DateTime => typeof(DateTime),
                    PrimitiveType.UInt16 => typeof(ushort),
                    PrimitiveType.UInt32 => typeof(uint),
                    _ => typeof(ulong),
                };

                return primitiveType.BuildCoreLibArrayTypeName(arrayInfo.Rank);
            case BinaryType.PrimitiveArray:
                Type primitiveArrayType = ((PrimitiveType)additionalInfo!) switch
                {
                    PrimitiveType.Boolean => typeof(bool[]),
                    PrimitiveType.Byte => typeof(byte[]),
                    PrimitiveType.Char => typeof(char[]),
                    PrimitiveType.Decimal => typeof(decimal[]),
                    PrimitiveType.Double => typeof(double[]),
                    PrimitiveType.Int16 => typeof(short[]),
                    PrimitiveType.Int32 => typeof(int[]),
                    PrimitiveType.Int64 => typeof(long[]),
                    PrimitiveType.SByte => typeof(sbyte[]),
                    PrimitiveType.Single => typeof(float[]),
                    PrimitiveType.TimeSpan => typeof(TimeSpan[]),
                    PrimitiveType.DateTime => typeof(DateTime[]),
                    PrimitiveType.UInt16 => typeof(ushort[]),
                    PrimitiveType.UInt32 => typeof(uint[]),
                    _ => typeof(ulong[]),
                };

                return primitiveArrayType.BuildCoreLibArrayTypeName(arrayInfo.Rank);
            case BinaryType.SystemClass:
                return ((TypeName)additionalInfo!).BuildArrayTypeName(arrayInfo.Rank);
            default:
                Debug.Assert(binaryType is BinaryType.Class, "The parsers should reject other inputs");
                return (((ClassTypeInfo)additionalInfo!).TypeName).BuildArrayTypeName(arrayInfo.Rank);
        }
    }
}
