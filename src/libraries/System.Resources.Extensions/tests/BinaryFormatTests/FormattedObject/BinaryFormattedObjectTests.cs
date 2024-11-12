// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Linq;
using System.Resources.Extensions.BinaryFormat;
using System.Formats.Nrbf;
using System.Resources.Extensions.Tests.Common;

namespace System.Resources.Extensions.Tests.FormattedObject;

public class BinaryFormattedObjectTests : SerializationTest<FormattedObjectSerializer>
{
    [Fact]
    public void ReadHeader()
    {
        BinaryFormattedObject format = new(Serialize("Hello World."));
        Assert.NotEqual(default, format.RootRecord.Id);
        Assert.Equal(format.RootRecord, format[format.RootRecord.Id]);
    }

    [Theory]
    [InlineData("Hello there.")]
    [InlineData("")]
    [InlineData("Embedded\0 Null.")]
    public void ReadBinaryObjectString(string testString)
    {
        BinaryFormattedObject format = new(Serialize(testString));
        PrimitiveTypeRecord<string> stringRecord = (PrimitiveTypeRecord<string>)format[format.RootRecord.Id];
        Assert.Equal(testString, stringRecord.Value);
    }

    [Fact]
    public void ReadEmptyHashTable()
    {
        BinaryFormattedObject format = new(Serialize(new Hashtable()));

        ClassRecord systemClass = (ClassRecord)format[format.RootRecord.Id];
        VerifyHashTable(systemClass, expectedVersion: 0, expectedHashSize: 3);

        SZArrayRecord<object> keys = (SZArrayRecord<object>)systemClass.GetSerializationRecord("Keys")!;
        Assert.Equal(0, keys.Length);
        SZArrayRecord<object> values = (SZArrayRecord<object>)systemClass.GetSerializationRecord("Values")!;
        Assert.Equal(0, values.Length);
    }

    private static void VerifyHashTable(ClassRecord systemClass, int expectedVersion, int expectedHashSize)
    {
        Assert.Equal(SerializationRecordType.SystemClassWithMembersAndTypes, systemClass.RecordType);
        Assert.Equal("System.Collections.Hashtable", systemClass.TypeName.FullName);
        Assert.Equal(
        [
            "LoadFactor",
            "Version",
            "Comparer",
            "HashCodeProvider",
            "HashSize",
            "Keys",
            "Values"
        ], systemClass.MemberNames);

        Assert.Equal(0.72f, systemClass.GetSingle("LoadFactor"));
        Assert.Equal(expectedVersion, systemClass.GetInt32("Version"));
        Assert.Null(systemClass.GetRawValue("Comparer"));
        Assert.Null(systemClass.GetRawValue("HashCodeProvider"));
        Assert.Equal(expectedHashSize, systemClass.GetInt32("HashSize"));
    }

    [Fact]
    public void ReadHashTableWithStringPair()
    {
        BinaryFormattedObject format = new(Serialize(new Hashtable()
        {
            { "This", "That" }
        }));

        ClassRecord systemClass = (ClassRecord)format[format.RootRecord.Id];
        VerifyHashTable(systemClass, expectedVersion: 1, expectedHashSize: 3);

        SZArrayRecord<object> keys = (SZArrayRecord<object>)format[systemClass.GetArrayRecord("Keys").Id];
        Assert.Equal(1, keys.Length);
        Assert.Equal("This", keys.GetArray().Single());
        SZArrayRecord<object> values = (SZArrayRecord<object>)format[systemClass.GetArrayRecord("Values").Id];
        Assert.Equal(1, values.Length);
        Assert.Equal("That", values.GetArray().Single());
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

        ClassRecord systemClass = (ClassRecord)format[format.RootRecord.Id];
        VerifyHashTable(systemClass, expectedVersion: 4, expectedHashSize: 7);

        // The collections themselves get ids first before the strings do.
        // Everything in the second keys is a string reference.
        SZArrayRecord<object> array = (SZArrayRecord<object>)systemClass.GetSerializationRecord("Keys")!;
        Assert.Equivalent(new object[] { "TheOther", "That", "This" }, array.GetArray());
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

        ClassRecord systemClass = (ClassRecord)format[format.RootRecord.Id];
        VerifyHashTable(systemClass, expectedVersion: 4, expectedHashSize: 7);

        // The collections themselves get ids first before the strings do.
        // Everything in the second keys is a string reference.
        SZArrayRecord<object> keys = (SZArrayRecord<object>)systemClass.GetSerializationRecord("Keys")!;
        Assert.Equivalent(new object[] { "Yowza", "Youza", "Meeza" }, keys.GetArray());

        SZArrayRecord<object?> values = (SZArrayRecord<object?>)systemClass.GetSerializationRecord("Values")!;
        Assert.Equal(new object?[] { null, null, null }, values.GetArray());
    }

    [Fact]
    public void ReadObject()
    {
        BinaryFormattedObject format = new(Serialize(new object()));
        Assert.Equal(SerializationRecordType.SystemClassWithMembersAndTypes, format[format.RootRecord.Id].RecordType);
    }

    [Fact]
    public void ReadStruct()
    {
        ValueTuple<int> tuple = new(355);
        BinaryFormattedObject format = new(Serialize(tuple));
        Assert.Equal(SerializationRecordType.SystemClassWithMembersAndTypes, format[format.RootRecord.Id].RecordType);
    }

    [Fact]
    public void ReadSimpleSerializableObject()
    {
        BinaryFormattedObject format = new(Serialize(new SimpleSerializableObject()));

        ClassRecord @class = (ClassRecord)format.RootRecord;
        Assert.Equal(SerializationRecordType.ClassWithMembersAndTypes, @class.RecordType);
        Assert.Equal(typeof(SimpleSerializableObject).FullName, @class.TypeName.FullName);
        Assert.Equal(typeof(SimpleSerializableObject).Assembly.FullName, @class.TypeName.AssemblyName!.FullName);
        Assert.Empty(@class.MemberNames);
    }

    [Fact]
    public void ReadNestedSerializableObject()
    {
        BinaryFormattedObject format = new(Serialize(new NestedSerializableObject()));

        ClassRecord @class = (ClassRecord)format.RootRecord;
        Assert.Equal(SerializationRecordType.ClassWithMembersAndTypes, @class.RecordType);
        Assert.Equal(typeof(NestedSerializableObject).FullName, @class.TypeName.FullName);
        Assert.Equal(typeof(NestedSerializableObject).Assembly.FullName, @class.TypeName.AssemblyName!.FullName);
        Assert.Equal(["_object", "_meaning"], @class.MemberNames);
        Assert.NotNull(@class.GetRawValue("_object"));
        Assert.Equal(42, @class.GetInt32("_meaning"));
    }

    [Fact]
    public void ReadTwoIntObject()
    {
        BinaryFormattedObject format = new(Serialize(new TwoIntSerializableObject()));

        ClassRecord @class = (ClassRecord)format.RootRecord;
        Assert.Equal(SerializationRecordType.ClassWithMembersAndTypes, @class.RecordType);
        Assert.Equal(typeof(TwoIntSerializableObject).FullName, @class.TypeName.FullName);
        Assert.Equal(typeof(TwoIntSerializableObject).Assembly.FullName, @class.TypeName.AssemblyName!.FullName);
        Assert.Equal(["_value", "_meaning"], @class.MemberNames);
        Assert.Equal(1970, @class.GetRawValue("_value"));
        Assert.Equal(42, @class.GetInt32("_meaning"));
    }

    [Fact]
    public void ReadRepeatedNestedObject()
    {
        BinaryFormattedObject format = new(Serialize(new RepeatedNestedSerializableObject()));
        ClassRecord classRecord = (ClassRecord)format.RootRecord;
        ClassRecord firstClass = classRecord.GetClassRecord("_first");
        Assert.Equal(SerializationRecordType.ClassWithMembersAndTypes, firstClass.RecordType);
        ClassRecord classWithId = classRecord.GetClassRecord("_second");
        Assert.Equal(SerializationRecordType.ClassWithId, classWithId.RecordType);
        Assert.Equal(1970, classWithId.GetRawValue("_value"));
        Assert.Equal(42, classWithId.GetInt32("_meaning"));
    }

    [Fact]
    public void ReadPrimitiveArray()
    {
        int[] input = [10, 9, 8, 7];

        BinaryFormattedObject format = new(Serialize(input));

        SZArrayRecord<int> array = (SZArrayRecord<int>)format[format.RootRecord.Id];
        Assert.Equal(input, array.GetArray());
    }

    [Fact]
    public void ReadStringArray()
    {
        string[] input = ["Monday", "Tuesday", "Wednesday"];

        BinaryFormattedObject format = new(Serialize(input));

        SZArrayRecord<string> array = (SZArrayRecord<string>)format[format.RootRecord.Id];
        Assert.Equal(input, array.GetArray());
        Assert.Equal(4, format.RecordMap.Count);
    }

    [Fact]
    public void ReadStringArrayWithNulls()
    {
        string?[] input = ["Monday", null, "Wednesday", null, null, null];

        BinaryFormattedObject format = new(Serialize(input));

        SZArrayRecord<string?> array = (SZArrayRecord<string?>)format[format.RootRecord.Id];
        Assert.Equal(input, array.GetArray());
    }

    [Fact]
    public void ReadDuplicatedStringArray()
    {
        string[] input = ["Monday", "Tuesday", "Monday"];

        BinaryFormattedObject format = new(Serialize(input));

        SZArrayRecord<string> array = (SZArrayRecord<string>)format[format.RootRecord.Id];
        Assert.Equal(input, array.GetArray());
        Assert.Equal(3, format.RecordMap.Count);
    }

    [Fact]
    public void ReadObjectWithNullableObjects()
    {
        BinaryFormattedObject format = new(Serialize(new ObjectWithNullableObjects()));
        ClassRecord classRecord = (ClassRecord)format.RootRecord;
        Assert.Equal(SerializationRecordType.ClassWithMembersAndTypes, classRecord.RecordType);
        Assert.Equal(typeof(ObjectWithNullableObjects).Assembly.FullName, classRecord.TypeName.AssemblyName!.FullName);
    }

    [Fact]
    public void ReadNestedObjectWithNullableObjects()
    {
        BinaryFormattedObject format = new(Serialize(new NestedObjectWithNullableObjects()));
        ClassRecord classRecord = (ClassRecord)format.RootRecord;
        Assert.Equal(SerializationRecordType.ClassWithMembersAndTypes, classRecord.RecordType);
        Assert.Equal(typeof(NestedObjectWithNullableObjects).Assembly.FullName, classRecord.TypeName.AssemblyName!.FullName);
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
