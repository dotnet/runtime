// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Drawing;
using System.Linq;
using System.Resources.Extensions.BinaryFormat;
using System.Formats.Nrbf;
using System.Resources.Extensions.Tests.Common;

namespace System.Resources.Extensions.Tests.FormattedObject;

public class ListTests : SerializationTest<FormattedObjectSerializer>
{
    [Fact]
    public void BinaryFormattedObject_ParseEmptyArrayList()
    {
        BinaryFormattedObject format = new(Serialize(new ArrayList()));

        VerifyArrayList((ClassRecord)format[format.RootRecord.Id]);

        format[((ClassRecord)format.RootRecord).GetArrayRecord("_items").Id].Should().BeAssignableTo<SZArrayRecord<object>>();
    }

    private static void VerifyArrayList(ClassRecord systemClass)
    {
        systemClass.RecordType.Should().Be(SerializationRecordType.SystemClassWithMembersAndTypes);

        systemClass.TypeName.FullName.Should().Be(typeof(ArrayList).FullName);
        systemClass.MemberNames.Should().BeEquivalentTo(["_items", "_size", "_version"]);
        systemClass.GetSerializationRecord("_items").Should().BeAssignableTo<SZArrayRecord<object>>();
    }

    [Theory]
    [MemberData(nameof(ArrayList_Primitive_Data))]
    public void BinaryFormattedObject_ParsePrimitivesArrayList(object value)
    {
        BinaryFormattedObject format = new(Serialize(new ArrayList()
        {
            value
        }));

        ClassRecord listRecord = (ClassRecord)format[format.RootRecord.Id];
        VerifyArrayList(listRecord);

        SZArrayRecord<object> array = (SZArrayRecord<object>)format[listRecord.GetArrayRecord("_items").Id];

        array.GetArray().Take(listRecord.GetInt32("_size")).Should().BeEquivalentTo(new[] { value });
    }

    [Fact]
    public void BinaryFormattedObject_ParseStringArrayList()
    {
        BinaryFormattedObject format = new(Serialize(new ArrayList()
        {
            "JarJar"
        }));

        ClassRecord listRecord = (ClassRecord)format[format.RootRecord.Id];
        VerifyArrayList(listRecord);

        SZArrayRecord<object> array = (SZArrayRecord<object>)format[listRecord.GetArrayRecord("_items").Id];

        array.GetArray().Take(listRecord.GetInt32("_size")).ToArray().Should().BeEquivalentTo(new object[] { "JarJar" });
    }

    public static TheoryData<object> ArrayList_Primitive_Data => new()
    {
        int.MaxValue,
        uint.MaxValue,
        long.MaxValue,
        ulong.MaxValue,
        short.MaxValue,
        ushort.MaxValue,
        byte.MaxValue,
        sbyte.MaxValue,
        true,
        float.MaxValue,
        double.MaxValue,
        char.MaxValue,
        TimeSpan.MaxValue,
        DateTime.MaxValue,
        decimal.MaxValue,
    };

    public static TheoryData<ArrayList> ArrayLists_TestData => new()
    {
        new ArrayList(),
        new ArrayList()
        {
            int.MaxValue,
            uint.MaxValue,
            long.MaxValue,
            ulong.MaxValue,
            short.MaxValue,
            ushort.MaxValue,
            byte.MaxValue,
            sbyte.MaxValue,
            true,
            float.MaxValue,
            double.MaxValue,
            char.MaxValue,
            TimeSpan.MaxValue,
            DateTime.MaxValue,
            decimal.MaxValue,
            "You betcha"
        },
        new ArrayList() { "Same", "old", "same", "old" }
    };

    public static TheoryData<ArrayList> ArrayLists_UnsupportedTestData => new()
    {
        new ArrayList()
        {
            new object(),
        },
        new ArrayList()
        {
            int.MaxValue,
            new Point()
        }
    };

    [Fact]
    public void BinaryFormattedObject_ParseEmptyIntList()
    {
        BinaryFormattedObject format = new(Serialize(new List<int>()));
        ClassRecord classInfo = (ClassRecord)format[format.RootRecord.Id];
        classInfo.RecordType.Should().Be(SerializationRecordType.SystemClassWithMembersAndTypes);

        // Note that T types are serialized as the mscorlib type.
        classInfo.TypeName.FullName.Should().Be(
            "System.Collections.Generic.List`1[[System.Int32, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]]");

        classInfo.MemberNames.Should().BeEquivalentTo(
        [
            "_items",
            // This is something that wouldn't be needed if List<T> implemented ISerializable. If we format
            // we can save any extra unused array spots.
            "_size",
            // It is a bit silly that _version gets serialized, it's only use is as a check to see if
            // the collection is modified while it is being enumerated.
            "_version"
        ]);

        classInfo.GetSerializationRecord("_items").Should().BeAssignableTo<SZArrayRecord<int>>();
        classInfo.GetInt32("_size").Should().Be(0);
        classInfo.GetInt32("_version").Should().Be(0);

        SZArrayRecord<int> array = (SZArrayRecord<int>)format[classInfo.GetArrayRecord("_items").Id];
        array.Length.Should().Be(0);
    }

    [Fact]
    public void BinaryFormattedObject_ParseEmptyStringList()
    {
        BinaryFormattedObject format = new(Serialize(new List<string>()));

        ClassRecord classInfo = (ClassRecord)format[format.RootRecord.Id];
        classInfo.RecordType.Should().Be(SerializationRecordType.SystemClassWithMembersAndTypes);
        classInfo.TypeName.FullName.Should().StartWith("System.Collections.Generic.List`1[[System.String,");
        classInfo.GetSerializationRecord("_items").Should().BeAssignableTo<SZArrayRecord<string>>();

        SZArrayRecord<string> array = (SZArrayRecord<string>)format[classInfo.GetArrayRecord("_items").Id];
        array.Length.Should().Be(0);
    }

    public static TheoryData<IList> Lists_UnsupportedTestData => new()
    {
        new List<object>(),
        new List<nint>(),
        new List<(int, int)>()
    };
}
