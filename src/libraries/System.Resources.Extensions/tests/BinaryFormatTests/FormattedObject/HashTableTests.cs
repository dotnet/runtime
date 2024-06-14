// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Drawing;
using System.Resources.Extensions.BinaryFormat;
using System.Runtime.Serialization;
using System.Formats.Nrbf;
using System.Resources.Extensions.Tests.Common;

namespace System.Resources.Extensions.Tests.FormattedObject;

#pragma warning disable CS0618 // Type or member is obsolete

public class HashtableTests : SerializationTest<FormattedObjectSerializer>
{
    [Fact]
    public void HashTable_GetObjectData()
    {
        Hashtable hashtable = new()
        {
            { "This", "That" }
        };

        // The converter isn't used for this scenario and can be a no-op.
        SerializationInfo info = new(typeof(Hashtable), FormatterConverterStub.Instance);
        hashtable.GetObjectData(info, default);
        info.MemberCount.Should().Be(7);

        var enumerator = info.GetEnumerator();

        enumerator.MoveNext().Should().BeTrue();
        enumerator.Current.Name.Should().Be("LoadFactor");
        enumerator.Current.Value.Should().Be(0.72f);

        enumerator.MoveNext().Should().BeTrue();
        enumerator.Current.Name.Should().Be("Version");
        enumerator.Current.Value.Should().Be(1);

        enumerator.MoveNext().Should().BeTrue();
        enumerator.Current.Name.Should().Be("Comparer");
        enumerator.Current.Value.Should().BeNull();

        enumerator.MoveNext().Should().BeTrue();
        enumerator.Current.Name.Should().Be("HashCodeProvider");
        enumerator.Current.Value.Should().BeNull();

        enumerator.MoveNext().Should().BeTrue();
        enumerator.Current.Name.Should().Be("HashSize");
        enumerator.Current.Value.Should().Be(3);

        enumerator.MoveNext().Should().BeTrue();
        enumerator.Current.Name.Should().Be("Keys");
        enumerator.Current.Value.Should().BeEquivalentTo(new object[] { "This" });

        enumerator.MoveNext().Should().BeTrue();
        enumerator.Current.Name.Should().Be("Values");
        enumerator.Current.Value.Should().BeEquivalentTo(new object[] { "That" });
    }

    [Fact]
    public void HashTable_CustomComparer()
    {
        Hashtable hashtable = new(new CustomHashCodeProvider(), StringComparer.OrdinalIgnoreCase)
        {
            { "This", "That" }
        };

        BinaryFormattedObject format = new(Serialize(hashtable));
        ClassRecord systemClass = (ClassRecord)format.RootRecord;
        systemClass.RecordType.Should().Be(SerializationRecordType.SystemClassWithMembersAndTypes);
        systemClass.TypeName.FullName.Should().Be("System.Collections.Hashtable");
        systemClass.GetSerializationRecord("Comparer")!.Should().BeAssignableTo<ClassRecord>().Which.TypeName.FullName.Should().Be("System.OrdinalComparer");
        systemClass.GetSerializationRecord("HashCodeProvider")!.Should().BeAssignableTo<ClassRecord>().Which.TypeName.FullName.Should().Be("System.Resources.Extensions.Tests.FormattedObject.HashtableTests+CustomHashCodeProvider");
        systemClass.GetSerializationRecord("Keys")!.Should().BeAssignableTo<SZArrayRecord<object>>();
        systemClass.GetSerializationRecord("Values")!.Should().BeAssignableTo<SZArrayRecord<object>>();
    }

    [Serializable]
    public class CustomHashCodeProvider : IHashCodeProvider
    {
        public int GetHashCode(object obj) => HashCode.Combine(obj);
    }

    public static TheoryData<Hashtable> Hashtables_TestData => new()
    {
        new Hashtable(),
        new Hashtable()
        {
            { "This", "That" }
        },
        new Hashtable()
        {
            { "Meaning", 42 }
        },
        new Hashtable()
        {
            { 42, 42 }
        },
        new Hashtable()
        {
            { 42, 42 },
            { 43, 42 }
        },
        new Hashtable()
        {
            { "Hastings", new DateTime(1066, 10, 14) }
        },
        new Hashtable()
        {
            { "Decimal", decimal.MaxValue }
        },
        new Hashtable()
        {
            { "This", "That" },
            { "TheOther", "This" },
            { "That", "This" }
        },
        new Hashtable()
        {
            { "Yowza", null },
            { "Youza", null },
            { "Meeza", null }
        },
        new Hashtable()
        {
            { "Yowza", null },
            { "Youza", "Binks" },
            { "Meeza", null }
        },
        new Hashtable()
        {
            { "Yowza", "Binks" },
            { "Youza", "Binks" },
            { "Meeza", null }
        },
        new Hashtable()
        {
            { decimal.MinValue, decimal.MaxValue },
            { float.MinValue, float.MaxValue },
            { DateTime.MinValue, DateTime.MaxValue },
            { TimeSpan.MinValue, TimeSpan.MaxValue }
        },
        // Stress the string interning
        MakeRepeatedHashtable(50, "Ditto"),
        MakeRepeatedHashtable(100, "..."),
        // Cross over into ObjectNullMultiple
        MakeRepeatedHashtable(255, null),
        MakeRepeatedHashtable(256, null),
        MakeRepeatedHashtable(257, null)
    };

    public static TheoryData<Hashtable> Hashtables_UnsupportedTestData => new()
    {
        new Hashtable()
        {
            { new object(), new object() }
        },
        new Hashtable()
        {
            { "Foo", new object() }
        },
        new Hashtable()
        {
            { "Foo", new Point() }
        },
        new Hashtable()
        {
            { "Foo", new PointF() }
        },
        new Hashtable()
        {
            { "Foo", (nint)42 }
        },
    };

    private static Hashtable MakeRepeatedHashtable(int countOfEntries, object? value)
    {
        Hashtable result = new(countOfEntries);
        for (int i = 1; i <= countOfEntries; i++)
        {
            result.Add($"Entry{i}", value);
        }

        return result;
    }

    private sealed class FormatterConverterStub : IFormatterConverter
    {
        public static IFormatterConverter Instance { get; } = new FormatterConverterStub();

        public object Convert(object value, Type type) => throw new NotImplementedException();
        public object Convert(object value, TypeCode typeCode) => throw new NotImplementedException();
        public bool ToBoolean(object value) => throw new NotImplementedException();
        public byte ToByte(object value) => throw new NotImplementedException();
        public char ToChar(object value) => throw new NotImplementedException();
        public DateTime ToDateTime(object value) => throw new NotImplementedException();
        public decimal ToDecimal(object value) => throw new NotImplementedException();
        public double ToDouble(object value) => throw new NotImplementedException();
        public short ToInt16(object value) => throw new NotImplementedException();
        public int ToInt32(object value) => throw new NotImplementedException();
        public long ToInt64(object value) => throw new NotImplementedException();
        public sbyte ToSByte(object value) => throw new NotImplementedException();
        public float ToSingle(object value) => throw new NotImplementedException();
        public string? ToString(object value) => throw new NotImplementedException();
        public ushort ToUInt16(object value) => throw new NotImplementedException();
        public uint ToUInt32(object value) => throw new NotImplementedException();
        public ulong ToUInt64(object value) => throw new NotImplementedException();
    }
}

#pragma warning restore CS0618 // Type or member is obsolete
