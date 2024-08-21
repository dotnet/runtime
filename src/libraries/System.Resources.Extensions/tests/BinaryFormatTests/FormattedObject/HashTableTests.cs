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
        Assert.Equal(7, info.MemberCount);

        var enumerator = info.GetEnumerator();

        Assert.True(enumerator.MoveNext());
        Assert.Equal("LoadFactor", enumerator.Current.Name);
        Assert.Equal(0.72f, enumerator.Current.Value);

        Assert.True(enumerator.MoveNext());
        Assert.Equal("Version", enumerator.Current.Name);
        Assert.Equal(1, enumerator.Current.Value);

        Assert.True(enumerator.MoveNext());
        Assert.Equal("Comparer", enumerator.Current.Name);
        Assert.Null(enumerator.Current.Value);

        Assert.True(enumerator.MoveNext());
        Assert.Equal("HashCodeProvider", enumerator.Current.Name);
        Assert.Null(enumerator.Current.Value);

        Assert.True(enumerator.MoveNext());
        Assert.Equal("HashSize", enumerator.Current.Name);
        Assert.Equal(3, enumerator.Current.Value);

        Assert.True(enumerator.MoveNext());
        Assert.Equal("Keys", enumerator.Current.Name);
        Assert.Equal(new object[] { "This" }, enumerator.Current.Value);

        Assert.True(enumerator.MoveNext());
        Assert.Equal("Values", enumerator.Current.Name);
        Assert.Equal(new object[] { "That" }, enumerator.Current.Value);
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
        Assert.Equal(SerializationRecordType.SystemClassWithMembersAndTypes, systemClass.RecordType);
        Assert.Equal("System.Collections.Hashtable", systemClass.TypeName.FullName);
        Assert.Equal("System.OrdinalComparer", systemClass.GetClassRecord("Comparer")!.TypeName.FullName);
        Assert.Equal("System.Resources.Extensions.Tests.FormattedObject.HashtableTests+CustomHashCodeProvider", systemClass.GetClassRecord("HashCodeProvider")!.TypeName.FullName);
        Assert.True(systemClass.GetSerializationRecord("Keys") is SZArrayRecord<object>);
        Assert.True(systemClass.GetSerializationRecord("Values") is SZArrayRecord<object>);
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
