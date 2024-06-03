// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection.Metadata;
using System.Runtime.Serialization.BinaryFormat.Utils;

namespace System.Runtime.Serialization.BinaryFormat;

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

    internal bool IsElementType(Type typeElement)
    {
        (BinaryType binaryType, object? additionalInfo) = Infos[0];

        switch (binaryType)
        {
            case BinaryType.String:
                return typeElement == typeof(string);
            case BinaryType.StringArray:
                return typeElement == typeof(string[]);
            case BinaryType.Object:
                return typeElement == typeof(object);
            case BinaryType.ObjectArray:
                return typeElement == typeof(object[]);
            case BinaryType.Primitive:
            case BinaryType.PrimitiveArray:
                if (binaryType is BinaryType.PrimitiveArray)
                {
                    if (!typeElement.IsArray)
                    {
                        return false;
                    }

                    typeElement = typeElement.GetElementType()!;
                }

                return ((PrimitiveType)additionalInfo!) switch
                {
                    PrimitiveType.Boolean => typeElement == typeof(bool),
                    PrimitiveType.Byte => typeElement == typeof(byte),
                    PrimitiveType.Char => typeElement == typeof(char),
                    PrimitiveType.Decimal => typeElement == typeof(decimal),
                    PrimitiveType.Double => typeElement == typeof(double),
                    PrimitiveType.Int16 => typeElement == typeof(short),
                    PrimitiveType.Int32 => typeElement == typeof(int),
                    PrimitiveType.Int64 => typeElement == typeof(long),
                    PrimitiveType.SByte => typeElement == typeof(sbyte),
                    PrimitiveType.Single => typeElement == typeof(float),
                    PrimitiveType.TimeSpan => typeElement == typeof(TimeSpan),
                    PrimitiveType.DateTime => typeElement == typeof(DateTime),
                    PrimitiveType.UInt16 => typeElement == typeof(ushort),
                    PrimitiveType.UInt32 => typeElement == typeof(uint),
                    PrimitiveType.UInt64 => typeElement == typeof(ulong),
                    _ => false
                };
            case BinaryType.SystemClass:
                if (typeElement.Assembly != typeof(object).Assembly)
                {
                    return false;
                }

                TypeName typeName = (TypeName)additionalInfo!;
                string fullSystemClassName = typeElement.GetTypeFullNameIncludingTypeForwards();
                return typeName.FullName == fullSystemClassName;
            default:
                Debug.Assert(binaryType is BinaryType.Class, "The parsers should reject other inputs");

                ClassTypeInfo typeInfo = (ClassTypeInfo)additionalInfo!;
                string fullClassName = typeElement.GetTypeFullNameIncludingTypeForwards();
                if (typeInfo.TypeName.FullName != fullClassName)
                {
                    return false;
                }

                string assemblyName = typeElement.GetAssemblyNameIncludingTypeForwards();
                return assemblyName == typeInfo.TypeName.AssemblyName!.FullName;
        }
    }

    internal bool ShouldBeRepresentedAsArrayOfClassRecords()
    {
        // This library tries to minimize the number of concepts the users need to learn to use it.
        // Since SZArrays are most common, it provides an ArrayRecord<T> abstraction.
        // Every other array (jagged, multi-dimensional etc) is represented using ArrayRecord.
        // The goal of this method is to determine whether given array can be represented as ArrayRecord<ClassRecord>.

        (BinaryType binaryType, object? additionalInfo) = Infos[0];

        if (binaryType == BinaryType.Class)
        {
            // An array of arrays can not be represented as ArrayRecord<ClassRecord>.
            return !((ClassTypeInfo)additionalInfo!).TypeName.IsArray;
        }
        else if (binaryType == BinaryType.SystemClass)
        {
            TypeName typeName = (TypeName)additionalInfo!;

            // An array of arrays can not be represented as ArrayRecord<ClassRecord>.
            if (typeName.IsArray)
            {
                return false;
            }

            if (!typeName.IsConstructedGenericType)
            {
                return true;
            }

            // Can't use ArrayRecord<ClassRecord> for Nullable<T>[]
            // as it consists of MemberPrimitiveTypedRecord and NullsRecord
            return typeName.GetGenericTypeDefinition().FullName != typeof(Nullable<>).FullName;
        }

        return false;
    }

    internal TypeName GetElementTypeName()
    {
        (BinaryType binaryType, object? additionalInfo) = Infos[0];

        switch (binaryType)
        {
            case BinaryType.String:
                return TypeName.Parse(typeof(string).FullName.AsSpan()).WithCoreLibAssemblyName();
            case BinaryType.StringArray:
                return TypeName.Parse(typeof(string[]).FullName.AsSpan()).WithCoreLibAssemblyName();
            case BinaryType.Object:
                return TypeName.Parse(typeof(object).FullName.AsSpan()).WithCoreLibAssemblyName();
            case BinaryType.ObjectArray:
                return TypeName.Parse(typeof(object[]).FullName.AsSpan()).WithCoreLibAssemblyName();
            case BinaryType.Primitive:
            case BinaryType.PrimitiveArray:
                string? name = ((PrimitiveType)additionalInfo!) switch
                {
                    PrimitiveType.Boolean => typeof(bool).FullName,
                    PrimitiveType.Byte => typeof(byte).FullName,
                    PrimitiveType.Char => typeof(char).FullName,
                    PrimitiveType.Decimal => typeof(decimal).FullName,
                    PrimitiveType.Double => typeof(double).FullName,
                    PrimitiveType.Int16 => typeof(short).FullName,
                    PrimitiveType.Int32 => typeof(int).FullName,
                    PrimitiveType.Int64 => typeof(long).FullName,
                    PrimitiveType.SByte => typeof(sbyte).FullName,
                    PrimitiveType.Single => typeof(float).FullName,
                    PrimitiveType.TimeSpan => typeof(TimeSpan).FullName,
                    PrimitiveType.DateTime => typeof(DateTime).FullName,
                    PrimitiveType.UInt16 => typeof(ushort).FullName,
                    PrimitiveType.UInt32 => typeof(uint).FullName,
                    _ => typeof(ulong).FullName,
                };

                return binaryType is BinaryType.PrimitiveArray
                    ? TypeName.Parse($"{name}[], {TypeNameExtensions.CoreLibAssemblyName}".AsSpan())
                    : TypeName.Parse(name.AsSpan()).WithCoreLibAssemblyName();

            case BinaryType.SystemClass:
                return (TypeName)additionalInfo!;
            default:
                Debug.Assert(binaryType is BinaryType.Class, "The parsers should reject other inputs");
                return ((ClassTypeInfo)additionalInfo!).TypeName;
        }
    }
}
