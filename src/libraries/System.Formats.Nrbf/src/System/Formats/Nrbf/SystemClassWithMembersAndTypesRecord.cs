// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Formats.Nrbf.Utils;

namespace System.Formats.Nrbf;

/// <summary>
/// Class information with type info.
/// </summary>
/// <remarks>
/// SystemClassWithMembersAndType records are described in <see href="https://learn.microsoft.com/en-us/openspecs/windows_protocols/ms-nrbf/ecb47445-831f-4ef5-9c9b-afd4d06e3657">[MS-NRBF] 2.3.2.3</see>.
/// </remarks>
internal sealed class SystemClassWithMembersAndTypesRecord : ClassRecord
{
    private SystemClassWithMembersAndTypesRecord(ClassInfo classInfo, MemberTypeInfo memberTypeInfo)
        : base(classInfo, memberTypeInfo)
    {
    }

    public override SerializationRecordType RecordType => SerializationRecordType.SystemClassWithMembersAndTypes;

    internal static SystemClassWithMembersAndTypesRecord Decode(BinaryReader reader, RecordMap recordMap, PayloadOptions options)
    {
        ClassInfo classInfo = ClassInfo.Decode(reader);
        MemberTypeInfo memberTypeInfo = MemberTypeInfo.Decode(reader, classInfo.MemberNames.Count, options, recordMap);
        // the only difference with ClassWithMembersAndTypesRecord is that we don't read library id here
        classInfo.LoadTypeName(options);
        return new SystemClassWithMembersAndTypesRecord(classInfo, memberTypeInfo);
    }

    internal override (AllowedRecordTypes allowed, PrimitiveType primitiveType) GetNextAllowedRecordType()
        => MemberTypeInfo.GetNextAllowedRecordType(MemberValues.Count);

    // For the root records that turn out to be primitive types, we map them to
    // PrimitiveTypeRecord<T> so the users don't need to learn the BF internals
    // to get a single primitive value!
    internal SerializationRecord TryToMapToUserFriendly()
    {
        if (MemberValues.Count == 1)
        {
            if (HasMember("m_value"))
            {
                return MemberValues[0] switch
                {
                    // there can be a value match, but no TypeName match
                    bool value when TypeName.FullName == typeof(bool).FullName => Create(value),
                    byte value when TypeName.FullName == typeof(byte).FullName => Create(value),
                    sbyte value when TypeName.FullName == typeof(sbyte).FullName => Create(value),
                    char value when TypeName.FullName == typeof(char).FullName => Create(value),
                    short value when TypeName.FullName == typeof(short).FullName => Create(value),
                    ushort value when TypeName.FullName == typeof(ushort).FullName => Create(value),
                    int value when TypeName.FullName == typeof(int).FullName => Create(value),
                    uint value when TypeName.FullName == typeof(uint).FullName => Create(value),
                    long value when TypeName.FullName == typeof(long).FullName => Create(value),
                    ulong value when TypeName.FullName == typeof(ulong).FullName => Create(value),
                    float value when TypeName.FullName == typeof(float).FullName => Create(value),
                    double value when TypeName.FullName == typeof(double).FullName => Create(value),
                    _ => this
                };
            }
            else if (HasMember("value"))
            {
                return MemberValues[0] switch
                {
                    // there can be a value match, but no TypeName match
                    long value when TypeName.FullName == typeof(IntPtr).FullName => Create(new IntPtr(value)),
                    ulong value when TypeName.FullName == typeof(UIntPtr).FullName => Create(new UIntPtr(value)),
                    _ => this
                };
            }
            else if (HasMember("_ticks") && MemberValues[0] is long ticks && TypeName.FullName == typeof(TimeSpan).FullName)
            {
                return Create(new TimeSpan(ticks));
            }
        }
        else if (MemberValues.Count == 2
            && HasMember("ticks") && HasMember("dateData")
            && MemberValues[0] is long value && MemberValues[1] is ulong
            && TypeNameMatches(typeof(DateTime)))
        {
            return Create(Utils.BinaryReaderExtensions.CreateDateTimeFromData(value));
        }
        else if(MemberValues.Count == 4
            && HasMember("lo") && HasMember("mid") && HasMember("hi") && HasMember("flags")
            && MemberValues[0] is int && MemberValues[1] is int && MemberValues[2] is int && MemberValues[3] is int
            && TypeNameMatches(typeof(decimal)))
        {
            int[] bits =
            [
                GetInt32("lo"),
                GetInt32("mid"),
                GetInt32("hi"),
                GetInt32("flags")
            ];

            return Create(new decimal(bits));
        }

        return this;

        SerializationRecord Create<T>(T value) where T : unmanaged
            => new MemberPrimitiveTypedRecord<T>(value, Id);
    }
}
