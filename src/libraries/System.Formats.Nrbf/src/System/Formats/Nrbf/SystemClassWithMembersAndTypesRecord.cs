// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Formats.Nrbf.Utils;
using System.Reflection.Metadata;

namespace System.Formats.Nrbf;

/// <summary>
/// Class information with type info.
/// </summary>
/// <remarks>
/// SystemClassWithMembersAndType records are described in <see href="https://learn.microsoft.com/openspecs/windows_protocols/ms-nrbf/ecb47445-831f-4ef5-9c9b-afd4d06e3657">[MS-NRBF] 2.3.2.3</see>.
/// </remarks>
internal sealed class SystemClassWithMembersAndTypesRecord : ClassRecord
{
    private SystemClassWithMembersAndTypesRecord(ClassInfo classInfo, MemberTypeInfo memberTypeInfo)
        : base(classInfo, memberTypeInfo)
    {
    }

    public override SerializationRecordType RecordType => SerializationRecordType.SystemClassWithMembersAndTypes;

    internal static SerializationRecord Decode(BinaryReader reader, RecordMap recordMap, PayloadOptions options)
    {
        ClassInfo classInfo = ClassInfo.Decode(reader);
        MemberTypeInfo memberTypeInfo = MemberTypeInfo.Decode(reader, classInfo.MemberNames.Count, options, recordMap);
        // the only difference with ClassWithMembersAndTypesRecord is that we don't read library id here
        classInfo.LoadTypeName(options);
        TypeName typeName = classInfo.TypeName;

        // BinaryFormatter represents primitive types as MemberPrimitiveTypedRecord
        // only for arrays of objects. For other arrays, like arrays of some abstraction
        // (example: new IComparable[] { int.MaxValue }), it uses SystemClassWithMembersAndTypes.
        // The same goes for root records that turn out to be primitive types.
        // We want to have the behavior unified, so we map such records to
        // PrimitiveTypeRecord<T> so the users don't need to learn the BF internals
        // to get a single primitive value.
        // We need to be as strict as possible, as we don't want to map anything else by accident.
        // That is why the code below is VERY defensive.

        if (!classInfo.TypeName.IsSimple || classInfo.MemberNames.Count == 0 || memberTypeInfo.Infos[0].BinaryType != BinaryType.Primitive)
        {
            return new SystemClassWithMembersAndTypesRecord(classInfo, memberTypeInfo);
        }
        else if (classInfo.MemberNames.Count == 1)
        {
            PrimitiveType primitiveType = (PrimitiveType)memberTypeInfo.Infos[0].AdditionalInfo!;
            // Get the member name without allocating on the heap.
            Collections.Generic.Dictionary<string, int>.Enumerator structEnumerator = classInfo.MemberNames.GetEnumerator();
            _ = structEnumerator.MoveNext();
            string memberName = structEnumerator.Current.Key;
            // Everything needs to match: primitive type, type name name and member name.
            return (primitiveType, typeName.FullName, memberName) switch
            {
                (PrimitiveType.Boolean, "System.Boolean", "m_value") => Create(reader.ReadBoolean()),
                (PrimitiveType.Byte, "System.Byte", "m_value") => Create(reader.ReadByte()),
                (PrimitiveType.SByte, "System.SByte", "m_value") => Create(reader.ReadSByte()),
                (PrimitiveType.Char, "System.Char", "m_value") => Create(reader.ParseChar()),
                (PrimitiveType.Int16, "System.Int16", "m_value") => Create(reader.ReadInt16()),
                (PrimitiveType.UInt16, "System.UInt16", "m_value") => Create(reader.ReadUInt16()),
                (PrimitiveType.Int32, "System.Int32", "m_value") => Create(reader.ReadInt32()),
                (PrimitiveType.UInt32, "System.UInt32", "m_value") => Create(reader.ReadUInt32()),
                (PrimitiveType.Int64, "System.Int64", "m_value") => Create(reader.ReadInt64()),
                (PrimitiveType.Int64, "System.IntPtr", "value") => Create(new IntPtr(reader.ReadInt64())),
                (PrimitiveType.Int64, "System.TimeSpan", "_ticks") => Create(new TimeSpan(reader.ReadInt64())),
                (PrimitiveType.UInt64, "System.UInt64", "m_value") => Create(reader.ReadUInt64()),
                (PrimitiveType.UInt64, "System.UIntPtr", "value") => Create(new UIntPtr(reader.ReadUInt64())),
                (PrimitiveType.Single, "System.Single", "m_value") => Create(reader.ReadSingle()),
                (PrimitiveType.Double, "System.Double", "m_value") => Create(reader.ReadDouble()),
                _ => new SystemClassWithMembersAndTypesRecord(classInfo, memberTypeInfo)
            };
        }
        else if (classInfo.MemberNames.Count == 2 && typeName.FullName == "System.DateTime"
            && HasMember("ticks", 0, PrimitiveType.Int64)
            && HasMember("dateData", 1, PrimitiveType.UInt64))
        {
            return DecodeDateTime(reader, classInfo.Id);
        }
        else if (classInfo.MemberNames.Count == 4 && typeName.FullName == "System.Decimal"
            && HasMember("flags", 0, PrimitiveType.Int32)
            && HasMember("hi", 1, PrimitiveType.Int32)
            && HasMember("lo", 2, PrimitiveType.Int32)
            && HasMember("mid", 3, PrimitiveType.Int32))
        {
            return DecodeDecimal(reader, classInfo.Id);
        }

        return new SystemClassWithMembersAndTypesRecord(classInfo, memberTypeInfo);

        SerializationRecord Create<T>(T value) where T : unmanaged
            => new MemberPrimitiveTypedRecord<T>(value, classInfo.Id);

        bool HasMember(string name, int order, PrimitiveType primitiveType)
            => classInfo.MemberNames.TryGetValue(name, out int memberOrder)
            && memberOrder == order
            && ((PrimitiveType)memberTypeInfo.Infos[order].AdditionalInfo!) == primitiveType;
    }

    internal override (AllowedRecordTypes allowed, PrimitiveType primitiveType) GetNextAllowedRecordType()
        => MemberTypeInfo.GetNextAllowedRecordType(MemberValues.Count);

    internal static MemberPrimitiveTypedRecord<DateTime> DecodeDateTime(BinaryReader reader, SerializationRecordId id)
    {
        _ = reader.ReadInt64(); // ticks are not used, but they need to be read as they go first in the payload
        ulong dateData = reader.ReadUInt64();

        return new MemberPrimitiveTypedRecord<DateTime>(BinaryReaderExtensions.CreateDateTimeFromData(dateData), id);
    }

    internal static MemberPrimitiveTypedRecord<decimal> DecodeDecimal(BinaryReader reader, SerializationRecordId id)
    {
        int flags = reader.ReadInt32();
        int hi = reader.ReadInt32();
        int lo = reader.ReadInt32();
        int mid = reader.ReadInt32();

        return new MemberPrimitiveTypedRecord<decimal>(new decimal([lo, mid, hi, flags]), id);
    }
}
