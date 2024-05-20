// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Reflection.Metadata;

namespace System.Runtime.Serialization.BinaryFormat;

internal sealed class SystemClassWithMembersAndTypesRecord : ClassRecord
{
    private SystemClassWithMembersAndTypesRecord(ClassInfo classInfo, MemberTypeInfo memberTypeInfo)
        : base(classInfo)
    {
        MemberTypeInfo = memberTypeInfo;
    }

    public override RecordType RecordType => RecordType.SystemClassWithMembersAndTypes;

    internal override AssemblyNameInfo LibraryName => FormatterServices.CoreLibAssemblyName;

    internal MemberTypeInfo MemberTypeInfo { get; }

    internal override int ExpectedValuesCount => MemberTypeInfo.Infos.Count;

    public override bool IsTypeNameMatching(Type type)
        => type.Assembly == typeof(object).Assembly
        && FormatterServices.GetTypeFullNameIncludingTypeForwards(type) == ClassInfo.Name.FullName;

    internal static SystemClassWithMembersAndTypesRecord Parse(BinaryReader reader, PayloadOptions options)
    {
        ClassInfo classInfo = ClassInfo.Parse(reader, options);
        MemberTypeInfo memberTypeInfo = MemberTypeInfo.Parse(reader, classInfo.MemberNames.Count, options);
        // the only difference with ClassWithMembersAndTypesRecord is that we don't read library id here
        return new(classInfo, memberTypeInfo);
    }

    internal override (AllowedRecordTypes allowed, PrimitiveType primitiveType) GetNextAllowedRecordType()
        => MemberTypeInfo.GetNextAllowedRecordType(MemberValues.Count);

    // For the root records that turn out to be primitive types, we map them to
    // PrimitiveTypeRecord<T> so the users don't need to learn the BF internals
    // to get a single primitive value!
    internal SerializationRecord TryToMapToUserFriendly()
    {
        // It could be implemented with way fewer ifs, but perf is important
        // and we want to bail out as soon as possible, but also don't convert something
        // that is not an exact match.
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
            && IsTypeNameMatching(typeof(DateTime)))
        {
            return Create(BinaryReaderExtensions.CreateDateTimeFromData(value));
        }
        else if(MemberValues.Count == 4
            && HasMember("lo") && HasMember("mid") && HasMember("hi") && HasMember("flags")
            && MemberValues[0] is int && MemberValues[1] is int && MemberValues[2] is int && MemberValues[3] is int
            && IsTypeNameMatching(typeof(decimal)))
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
            => new MemberPrimitiveTypedRecord<T>(value, ObjectId);
    }
}
