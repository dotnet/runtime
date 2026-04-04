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
                case BinaryType.String:
                case BinaryType.StringArray:
                case BinaryType.Object:
                case BinaryType.ObjectArray:
                    // These types have no additional data.
                    break;
                default:
                    throw new InvalidOperationException();
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
            | AllowedRecordTypes.BinaryLibrary; // Classes may be preceded with a library record (System too!)
        // but System Classes can be expressed only by System records
        const AllowedRecordTypes SystemClass = Classes | AllowedRecordTypes.SystemClassWithMembersAndTypes
            // All primitive types can be stored by using one of the interfaces they implement.
            // Example: `new IEnumerable[1] { "hello" }` or `new IComparable[1] { int.MaxValue }`.
            | AllowedRecordTypes.BinaryObjectString | AllowedRecordTypes.MemberPrimitiveTyped
            // System.Nullable<UserStruct> is a special case of SystemClassWithMembersAndTypes
            | AllowedRecordTypes.ClassWithMembersAndTypes;
        const AllowedRecordTypes NonSystemClass = Classes | AllowedRecordTypes.ClassWithMembersAndTypes;

        return binaryType switch
        {
            BinaryType.Primitive => (default, (PrimitiveType)additionalInfo!),
            BinaryType.String => (Strings, default),
            BinaryType.Object => (AllowedRecordTypes.AnyObject, default),
            BinaryType.StringArray => (StringArray, default),
            BinaryType.PrimitiveArray => (PrimitiveArray, default),
            BinaryType.Class => (NonSystemClass, default),
            BinaryType.SystemClass => (SystemClass, default),
            BinaryType.ObjectArray => (ObjectArray, default),
            _ => throw new InvalidOperationException()
        };
    }

    internal TypeName GetArrayTypeName(ArrayInfo arrayInfo)
    {
        (BinaryType binaryType, object? additionalInfo) = Infos[0];

        TypeName elementTypeName = binaryType switch
        {
            BinaryType.String => TypeNameHelpers.GetPrimitiveTypeName(TypeNameHelpers.StringPrimitiveType),
            BinaryType.StringArray => TypeNameHelpers.GetPrimitiveSZArrayTypeName(TypeNameHelpers.StringPrimitiveType),
            BinaryType.Primitive => TypeNameHelpers.GetPrimitiveTypeName((PrimitiveType)additionalInfo!),
            BinaryType.PrimitiveArray => TypeNameHelpers.GetPrimitiveSZArrayTypeName((PrimitiveType)additionalInfo!),
            BinaryType.Object => TypeNameHelpers.GetPrimitiveTypeName(TypeNameHelpers.ObjectPrimitiveType),
            BinaryType.ObjectArray => TypeNameHelpers.GetPrimitiveSZArrayTypeName(TypeNameHelpers.ObjectPrimitiveType),
            BinaryType.SystemClass => (TypeName)additionalInfo!,
            BinaryType.Class => ((ClassTypeInfo)additionalInfo!).TypeName,
            _ => throw new InvalidOperationException()
        };

        // In general, arrayRank == 1 may have two different meanings:
        // - [] is a single-dimensional array with a zero lower bound (SZArray),
        // - [*] is a single-dimensional array with an arbitrary lower bound (variable bound array).
        // Variable bound arrays are not supported by design, so in our case it's always SZArray.
        // That is why we don't call TypeName.MakeArrayTypeName(1) because it would create [*] instead of [] name.
        return arrayInfo.Rank == 1
            ? elementTypeName.MakeSZArrayTypeName()
            : elementTypeName.MakeArrayTypeName(arrayInfo.Rank);
    }
}
