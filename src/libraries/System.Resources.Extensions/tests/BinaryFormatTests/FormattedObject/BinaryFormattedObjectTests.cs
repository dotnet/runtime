// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Linq;
using System.Resources.Extensions.BinaryFormat;
using System.Runtime.Serialization.BinaryFormat;
using System.Resources.Extensions.Tests.Common;

namespace System.Resources.Extensions.Tests.FormattedObject;

public class BinaryFormattedObjectTests : SerializationTest<FormattedObjectSerializer>
{
    [Fact]
    public void ReadHeader()
    {
        BinaryFormattedObject format = new(Serialize("Hello World."));
        format.RootRecord.ObjectId.Should().Be(1);
    }

    [Theory]
    [InlineData("Hello there.")]
    [InlineData("")]
    [InlineData("Embedded\0 Null.")]
    public void ReadBinaryObjectString(string testString)
    {
        BinaryFormattedObject format = new(Serialize(testString));
        PrimitiveTypeRecord<string> stringRecord = (PrimitiveTypeRecord<string>)format[1];
        stringRecord.ObjectId.Should().Be(1);
        stringRecord.Value.Should().Be(testString);
    }

    [Fact]
    public void ReadEmptyHashTable()
    {
        BinaryFormattedObject format = new(Serialize(new Hashtable()));

        ClassRecord systemClass = (ClassRecord)format[1];
        VerifyHashTable(systemClass, expectedVersion: 0, expectedHashSize: 3);

        ArrayRecord<object> keys = (ArrayRecord<object>)systemClass.GetSerializationRecord("Keys")!;
        keys.ObjectId.Should().Be(2);
        keys.Length.Should().Be(0);
        ArrayRecord<object> values = (ArrayRecord<object>)systemClass.GetSerializationRecord("Values")!;
        values.ObjectId.Should().Be(3);
        values.Length.Should().Be(0);
    }

    private static void VerifyHashTable(ClassRecord systemClass, int expectedVersion, int expectedHashSize)
    {
        systemClass.RecordType.Should().Be(RecordType.SystemClassWithMembersAndTypes);
        systemClass.ObjectId.Should().Be(1);
        systemClass.TypeName.FullName.Should().Be("System.Collections.Hashtable");
        systemClass.MemberNames.Should().BeEquivalentTo(
        [
            "LoadFactor",
            "Version",
            "Comparer",
            "HashCodeProvider",
            "HashSize",
            "Keys",
            "Values"
        ]);

        systemClass.GetSingle("LoadFactor").Should().Be(0.72f);
        systemClass.GetInt32("Version").Should().Be(expectedVersion);
        systemClass.GetRawValue("Comparer").Should().BeNull();
        systemClass.GetRawValue("HashCodeProvider").Should().BeNull();
        systemClass.GetInt32("HashSize").Should().Be(expectedHashSize);
    }

    [Fact]
    public void ReadHashTableWithStringPair()
    {
        BinaryFormattedObject format = new(Serialize(new Hashtable()
        {
            { "This", "That" }
        }));

        ClassRecord systemClass = (ClassRecord)format[1];
        VerifyHashTable(systemClass, expectedVersion: 1, expectedHashSize: 3);

        ArrayRecord<object> keys = (ArrayRecord<object>)format[2];
        keys.ObjectId.Should().Be(2);
        keys.Length.Should().Be(1);
        keys.GetArray().Single().Should().Be("This");
        ArrayRecord<object> values = (ArrayRecord<object>)format[3];
        values.ObjectId.Should().Be(3);
        values.Length.Should().Be(1);
        values.GetArray().Single().Should().Be("That");
    }

    [Fact]
    public void ReadHashTableWithRepeatedStrings()
    {
        BinaryFormattedObject format = new(Serialize(new Hashtable()
        {
            { "This", "That" },
            { "TheOther", "This" },
            { "That", "This" }
        }));

        ClassRecord systemClass = (ClassRecord)format[1];
        VerifyHashTable(systemClass, expectedVersion: 4, expectedHashSize: 7);

        // The collections themselves get ids first before the strings do.
        // Everything in the second keys is a string reference.
        ArrayRecord<object> array = (ArrayRecord<object>)systemClass.GetSerializationRecord("Keys")!;
        array.ObjectId.Should().Be(2);
        array.GetArray().Should().BeEquivalentTo(["This", "TheOther", "That"]);
    }

    [Fact]
    public void ReadHashTableWithNullValues()
    {
        BinaryFormattedObject format = new(Serialize(new Hashtable()
        {
            { "Yowza", null },
            { "Youza", null },
            { "Meeza", null }
        }));

        ClassRecord systemClass = (ClassRecord)format[1];
        VerifyHashTable(systemClass, expectedVersion: 4, expectedHashSize: 7);

        // The collections themselves get ids first before the strings do.
        // Everything in the second keys is a string reference.
        ArrayRecord<object> keys = (ArrayRecord<object>)systemClass.GetSerializationRecord("Keys")!;
        keys.ObjectId.Should().Be(2);
        keys.GetArray().Should().BeEquivalentTo(new object[] { "Yowza", "Youza", "Meeza" });

        ArrayRecord<object?> values = (ArrayRecord<object?>)systemClass.GetSerializationRecord("Values")!;
        values.ObjectId.Should().Be(3);
        values.GetArray().Should().BeEquivalentTo(new object?[] { null, null, null });
    }

    [Fact]
    public void ReadObject()
    {
        BinaryFormattedObject format = new(Serialize(new object()));
        format[1].RecordType.Should().Be(RecordType.SystemClassWithMembersAndTypes);
    }

    [Fact]
    public void ReadStruct()
    {
        ValueTuple<int> tuple = new(355);
        BinaryFormattedObject format = new(Serialize(tuple));
        format[1].RecordType.Should().Be(RecordType.SystemClassWithMembersAndTypes);
    }

    [Fact]
    public void ReadSimpleSerializableObject()
    {
        BinaryFormattedObject format = new(Serialize(new SimpleSerializableObject()));

        ClassRecord @class = (ClassRecord)format.RootRecord;
        @class.RecordType.Should().Be(RecordType.ClassWithMembersAndTypes);
        @class.ObjectId.Should().Be(1);
        @class.TypeName.FullName.Should().Be(typeof(SimpleSerializableObject).FullName);
        @class.TypeName.AssemblyName!.FullName.Should().Be(typeof(SimpleSerializableObject).Assembly.FullName);
        @class.MemberNames.Should().BeEmpty();
    }

    [Fact]
    public void ReadNestedSerializableObject()
    {
        BinaryFormattedObject format = new(Serialize(new NestedSerializableObject()));

        ClassRecord @class = (ClassRecord)format.RootRecord;
        @class.RecordType.Should().Be(RecordType.ClassWithMembersAndTypes);
        @class.ObjectId.Should().Be(1);
        @class.TypeName.FullName.Should().Be(typeof(NestedSerializableObject).FullName);
        @class.TypeName.AssemblyName!.FullName.Should().Be(typeof(NestedSerializableObject).Assembly.FullName);
        @class.MemberNames.Should().BeEquivalentTo(["_object", "_meaning"]);
        @class.GetRawValue("_object").Should().NotBeNull();
        @class.GetInt32("_meaning").Should().Be(42);
    }

    [Fact]
    public void ReadTwoIntObject()
    {
        BinaryFormattedObject format = new(Serialize(new TwoIntSerializableObject()));

        ClassRecord @class = (ClassRecord)format.RootRecord;
        @class.RecordType.Should().Be(RecordType.ClassWithMembersAndTypes);
        @class.ObjectId.Should().Be(1);
        @class.TypeName.FullName.Should().Be(typeof(TwoIntSerializableObject).FullName);
        @class.TypeName.AssemblyName!.FullName.Should().Be(typeof(TwoIntSerializableObject).Assembly.FullName);
        @class.MemberNames.Should().BeEquivalentTo(["_value", "_meaning"]);
        @class.GetRawValue("_value").Should().Be(1970);
        @class.GetInt32("_meaning").Should().Be(42);
    }

    [Fact]
    public void ReadRepeatedNestedObject()
    {
        BinaryFormattedObject format = new(Serialize(new RepeatedNestedSerializableObject()));
        ClassRecord firstClass = (ClassRecord)format[3];
        firstClass.RecordType.Should().Be(RecordType.ClassWithMembersAndTypes);
        ClassRecord classWithId = (ClassRecord)format[4];
        classWithId.RecordType.Should().Be(RecordType.ClassWithId);
        classWithId.GetRawValue("_value").Should().Be(1970);
        classWithId.GetInt32("_meaning").Should().Be(42);
    }

    [Fact]
    public void ReadPrimitiveArray()
    {
        int[] input = [10, 9, 8, 7];

        BinaryFormattedObject format = new(Serialize(input));

        ArrayRecord<int> array = (ArrayRecord<int>)format[1];
        array.Length.Should().Be(4);
        array.GetArray().Should().BeEquivalentTo(input);
    }

    [Fact]
    public void ReadStringArray()
    {
        string[] input = ["Monday", "Tuesday", "Wednesday"];

        BinaryFormattedObject format = new(Serialize(input));

        ArrayRecord<string> array = (ArrayRecord<string>)format[1];
        array.ObjectId.Should().Be(1);
        array.Length.Should().Be(3);
        array.GetArray().Should().BeEquivalentTo(input);
        format.RecordMap.Count.Should().Be(4);
    }

    [Fact]
    public void ReadStringArrayWithNulls()
    {
        string?[] input = ["Monday", null, "Wednesday", null, null, null];

        BinaryFormattedObject format = new(Serialize(input));

        ArrayRecord<string?> array = (ArrayRecord<string?>)format[1];
        array.ObjectId.Should().Be(1);
        array.Length.Should().Be(6);
        array.GetArray().Should().BeEquivalentTo(input);
    }

    [Fact]
    public void ReadDuplicatedStringArray()
    {
        string[] input = ["Monday", "Tuesday", "Monday"];

        BinaryFormattedObject format = new(Serialize(input));

        ArrayRecord<string> array = (ArrayRecord<string>)format[1];
        array.ObjectId.Should().Be(1);
        array.Length.Should().Be(3);
        array.GetArray().Should().BeEquivalentTo(input);
        format.RecordMap.Count.Should().Be(3);
    }

    [Fact]
    public void ReadObjectWithNullableObjects()
    {
        BinaryFormattedObject format = new(Serialize(new ObjectWithNullableObjects()));
        ClassRecord classRecord = (ClassRecord)format.RootRecord;
        classRecord.RecordType.Should().Be(RecordType.ClassWithMembersAndTypes);
        classRecord.TypeName.AssemblyName!.FullName.Should().Be(typeof(ObjectWithNullableObjects).Assembly.FullName);
    }

    [Fact]
    public void ReadNestedObjectWithNullableObjects()
    {
        BinaryFormattedObject format = new(Serialize(new NestedObjectWithNullableObjects()));
        ClassRecord classRecord = (ClassRecord)format.RootRecord;
        classRecord.RecordType.Should().Be(RecordType.ClassWithMembersAndTypes);
        classRecord.TypeName.AssemblyName!.FullName.Should().Be(typeof(NestedObjectWithNullableObjects).Assembly.FullName);
    }

    [Serializable]
    private class SimpleSerializableObject
    {
    }

#pragma warning disable IDE0052 // Remove unread private members
#pragma warning disable IDE0051 // Remove unused private members
#pragma warning disable CS0414  // Field is assigned but its value is never used
#pragma warning disable CS0649  // Field is never assigned to, and will always have its default value null
#pragma warning disable CA1823 // Avoid unused private fields
    [Serializable]
    private class ObjectWithNullableObjects
    {
        public object? First;
        public object? Second;
        public object? Third;
    }

    [Serializable]
    private class NestedObjectWithNullableObjects
    {
        public ObjectWithNullableObjects? First;
        public ObjectWithNullableObjects? Second;
        public ObjectWithNullableObjects? Third = new();
    }

    [Serializable]
    private class NestedSerializableObject
    {
        private readonly SimpleSerializableObject _object = new();
        private readonly int _meaning = 42;
    }

    [Serializable]
    private class TwoIntSerializableObject
    {
        private readonly int _value = 1970;
        private readonly int _meaning = 42;
    }

    [Serializable]
    private class RepeatedNestedSerializableObject
    {
        private readonly TwoIntSerializableObject _first = new();
        private readonly TwoIntSerializableObject _second = new();
    }
#pragma warning restore IDE0052 // Remove unread private members
#pragma warning restore IDE0051 // Remove unused private members
#pragma warning restore CS0414  // Field is assigned but its value is never used
#pragma warning restore CS0649  // Field is never assigned to, and will always have its default value null
#pragma warning restore CA1823 // Avoid unused private fields
}
