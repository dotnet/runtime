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
        if (!TypeName.IsSimple)
        {
            return this;
        }

        if (MemberValues.Count == 1)
        {
            if (HasMember("m_value"))
            {
                return MemberValues[0] switch
                {
                    // there can be a value match, but no TypeName match
                    bool value when TypeNameMatches(typeof(bool)) => Create(value),
                    byte value when TypeNameMatches(typeof(byte)) => Create(value),
                    sbyte value when TypeNameMatches(typeof(sbyte)) => Create(value),
                    char value when TypeNameMatches(typeof(char)) => Create(value),
                    short value when TypeNameMatches(typeof(short)) => Create(value),
                    ushort value when TypeNameMatches(typeof(ushort)) => Create(value),
                    int value when TypeNameMatches(typeof(int)) => Create(value),
                    uint value when TypeNameMatches(typeof(uint)) => Create(value),
                    long value when TypeNameMatches(typeof(long)) => Create(value),
                    ulong value when TypeNameMatches(typeof(ulong)) => Create(value),
                    float value when TypeNameMatches(typeof(float)) => Create(value),
                    double value when TypeNameMatches(typeof(double)) => Create(value),
                    _ => this
                };
            }
            else if (HasMember("value"))
            {
                return MemberValues[0] switch
                {
                    // there can be a value match, but no TypeName match
                    long value when TypeNameMatches(typeof(IntPtr)) => Create(new IntPtr(value)),
                    ulong value when TypeNameMatches(typeof(UIntPtr)) => Create(new UIntPtr(value)),
                    _ => this
                };
            }
            else if (HasMember("_ticks") && MemberValues[0] is long ticks && TypeNameMatches(typeof(TimeSpan)))
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
