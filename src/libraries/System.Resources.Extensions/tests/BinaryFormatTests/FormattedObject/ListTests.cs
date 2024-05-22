// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Drawing;
using System.Runtime.Serialization.BinaryFormat;
using System.Runtime.Serialization.Formatters.Binary;
using FormatTests.Common;

namespace FormatTests.FormattedObject;

public class ListTests : SerializationTest<FormattedObjectSerializer>
{
    [Fact]
    public void BinaryFormattedObject_ParseEmptyArrayList()
    {
        System.Windows.Forms.BinaryFormat.BinaryFormattedObject format = new(Serialize(new ArrayList()));

        VerifyArrayList((ClassRecord)format[1]);

        format[2].Should().BeAssignableTo<ArrayRecord<object>>();
    }

    private static void VerifyArrayList(ClassRecord systemClass)
    {
        systemClass.RecordType.Should().Be(RecordType.SystemClassWithMembersAndTypes);

        systemClass.TypeName.FullName.Should().Be(typeof(ArrayList).FullName);
        systemClass.MemberNames.Should().BeEquivalentTo(["_items", "_size", "_version"]);
        systemClass.GetSerializationRecord("_items").Should().BeAssignableTo<ArrayRecord<object>>();
    }

    [Theory]
    [MemberData(nameof(ArrayList_Primitive_Data))]
    public void BinaryFormattedObject_ParsePrimitivesArrayList(object value)
    {
        System.Windows.Forms.BinaryFormat.BinaryFormattedObject format = new(Serialize(new ArrayList()
        {
            value
        }));

        ClassRecord listRecord = (ClassRecord)format[1];
        VerifyArrayList(listRecord);

        ArrayRecord<object> array = (ArrayRecord<object>)format[2];

        array.ToArray().Take(listRecord.GetInt32("_size")).Should().BeEquivalentTo(new[] { value });
    }

    [Fact]
    public void BinaryFormattedObject_ParseStringArrayList()
    {
        System.Windows.Forms.BinaryFormat.BinaryFormattedObject format = new(Serialize(new ArrayList()
        {
            "JarJar"
        }));

        ClassRecord listRecord = (ClassRecord)format[1];
        VerifyArrayList(listRecord);

        ArrayRecord<object> array = (ArrayRecord<object>)format[2];

        array.ToArray().Take(listRecord.GetInt32("_size")).ToArray().Should().BeEquivalentTo(new object[] { "JarJar" });
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
        System.Windows.Forms.BinaryFormat.BinaryFormattedObject format = new(Serialize(new List<int>()));
        ClassRecord classInfo = (ClassRecord)format[1];
        classInfo.RecordType.Should().Be(RecordType.SystemClassWithMembersAndTypes);

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

        classInfo.GetSerializationRecord("_items").Should().BeAssignableTo<ArrayRecord<int>>();
        classInfo.GetInt32("_size").Should().Be(0);
        classInfo.GetInt32("_version").Should().Be(0);

        ArrayRecord<int> array = (ArrayRecord<int>)format[2];
        array.Length.Should().Be(0);
    }

    [Fact]
    public void BinaryFormattedObject_ParseEmptyStringList()
    {
        System.Windows.Forms.BinaryFormat.BinaryFormattedObject format = new(Serialize(new List<string>()));

        ClassRecord classInfo = (ClassRecord)format[1];
        classInfo.RecordType.Should().Be(RecordType.SystemClassWithMembersAndTypes);
        classInfo.TypeName.FullName.Should().StartWith("System.Collections.Generic.List`1[[System.String,");
        classInfo.GetSerializationRecord("_items").Should().BeAssignableTo<ArrayRecord<string>>();

        ArrayRecord<string> array = (ArrayRecord<string>)format[2];
        array.Length.Should().Be(0);
    }

    [Theory]
    [MemberData(nameof(PrimitiveLists_TestData))]
    public void BinaryFormatWriter_TryWritePrimitiveList(IList list)
    {
        using MemoryStream stream = new();
        System.Windows.Forms.BinaryFormat.BinaryFormatWriter.TryWritePrimitiveList(stream, list).Should().BeTrue();
        stream.Position = 0;

        // cs/binary-formatter-without-binder
        BinaryFormatter formatter = new(); // CodeQL [SM04191] : This is a test. Safe use because the deserialization process is performed on trusted data and the types are controlled and validated.
        // cs/dangerous-binary-deserialization
        IList deserialized = (IList)formatter.Deserialize(stream); // CodeQL[SM02229] : Testing legacy feature. This is a safe use of BinaryFormatter because the data is trusted and the types are controlled and validated.

        deserialized.Should().BeEquivalentTo(list);
    }

    [Theory]
    [MemberData(nameof(Lists_UnsupportedTestData))]
    public void BinaryFormatWriter_TryWritePrimitiveList_Unsupported(IList list)
    {
        using MemoryStream stream = new();
        System.Windows.Forms.BinaryFormat.BinaryFormatWriter.TryWritePrimitiveList(stream, list).Should().BeFalse();
        stream.Position.Should().Be(0);
    }

    [Theory]
    [MemberData(nameof(PrimitiveLists_TestData))]
    public void BinaryFormattedObjectExtensions_TryGetPrimitiveList(IList list)
    {
        System.Windows.Forms.BinaryFormat.BinaryFormattedObject format = new(Serialize(list));
        System.Windows.Forms.BinaryFormat.BinaryFormattedObjectExtensions.TryGetPrimitiveList(format, out object? deserialized).Should().BeTrue();
        deserialized.Should().BeEquivalentTo(list);
    }

    public static TheoryData<IList> PrimitiveLists_TestData => new()
    {
        new List<int>(),
        new List<float>() { 3.14f },
        new List<float>() { float.NaN, float.PositiveInfinity, float.NegativeInfinity, float.NegativeZero },
        new List<int>() { 1, 3, 4, 5, 6, 7 },
        new List<byte>() { 0xDE, 0xAD, 0xBE, 0xEF },
        new List<char>() { 'a', 'b',  'c', 'd', 'e', 'f', 'g', 'h' },
        new List<char>() { 'a', '\0',  'c' },
        new List<string>() { "Believe", "it", "or", "not" },
        new List<decimal>() { 42m },
        new List<DateTime>() { new(2000, 1, 1) },
        new List<TimeSpan>() { new(0, 0, 50) }
    };

    public static TheoryData<IList> Lists_UnsupportedTestData => new()
    {
        new List<object>(),
        new List<nint>(),
        new List<(int, int)>()
    };
}
