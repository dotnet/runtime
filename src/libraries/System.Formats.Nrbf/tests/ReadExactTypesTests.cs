using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using Xunit;

namespace System.Formats.Nrbf.Tests;

public class ReadExactTypesTests : ReadTests
{
    [Serializable]
    public class CustomTypeWithPrimitiveFields
    {
        public byte Byte;
        public sbyte SignedByte;
        public short Short;
        public ushort UnsignedShort;
        public int Integer;
        public uint UnsignedInteger;
        public long Long;
        public ulong UnsignedLong;
    }

    [Fact]
    public void CanRead_CustomTypeWithPrimitiveFields()
    {
        CustomTypeWithPrimitiveFields input = new()
        {
            Byte = 1,
            SignedByte = 2,
            Short = -3,
            UnsignedShort = 4,
            Integer = -123,
            UnsignedInteger = 666,
            Long = long.MaxValue,
            UnsignedLong = ulong.MaxValue
        };

        using MemoryStream stream = Serialize(input);

        ClassRecord classRecord = NrbfDecoder.DecodeClassRecord(stream);

        Verify(input, classRecord);

        Assert.Throws<InvalidOperationException>(() => classRecord.GetBoolean(nameof(CustomTypeWithPrimitiveFields.Byte)));
    }

    private static void Verify(CustomTypeWithPrimitiveFields expected, ClassRecord classRecord)
    {
        Assert.Equal(expected.Byte, classRecord.GetByte(nameof(CustomTypeWithPrimitiveFields.Byte)));
        Assert.Equal(expected.SignedByte, classRecord.GetSByte(nameof(CustomTypeWithPrimitiveFields.SignedByte)));
        Assert.Equal(expected.Short, classRecord.GetInt16(nameof(CustomTypeWithPrimitiveFields.Short)));
        Assert.Equal(expected.UnsignedShort, classRecord.GetUInt16(nameof(CustomTypeWithPrimitiveFields.UnsignedShort)));
        Assert.Equal(expected.Integer, classRecord.GetInt32(nameof(CustomTypeWithPrimitiveFields.Integer)));
        Assert.Equal(expected.UnsignedInteger, classRecord.GetUInt32(nameof(CustomTypeWithPrimitiveFields.UnsignedInteger)));
        Assert.Equal(expected.Long, classRecord.GetInt64(nameof(CustomTypeWithPrimitiveFields.Long)));
        Assert.Equal(expected.UnsignedLong, classRecord.GetUInt64(nameof(CustomTypeWithPrimitiveFields.UnsignedLong)));
    }

    [Serializable]
    public class CustomTypeWithStringField
    {
        public string? Text;
    }

    [Theory]
    [InlineData(null)]
    [InlineData("Hello!")]
    public void CanRead_CustomTypeWithStringFields(string? text)
    {
        CustomTypeWithStringField input = new()
        {
            Text = text
        };

        using MemoryStream stream = Serialize(input);

        ClassRecord classRecord = NrbfDecoder.DecodeClassRecord(stream);

        Assert.True(classRecord.HasMember(nameof(CustomTypeWithStringField.Text)));
        Assert.Equal(input.Text, classRecord.GetString(nameof(CustomTypeWithStringField.Text)));
        Assert.Equal(typeof(CustomTypeWithStringField).FullName, classRecord.TypeName.FullName);
        Assert.Equal(typeof(CustomTypeWithStringField).AssemblyQualifiedName, classRecord.TypeName.AssemblyQualifiedName);
        Assert.Equal(typeof(CustomTypeWithStringField).Assembly.FullName, classRecord.TypeName.AssemblyName!.FullName);
        Assert.False(classRecord.HasMember("NotPresent"));
    }

    [Serializable]
    public class CustomTypeWithObjectField
    {
        public object? ActualObject;
        public object? SomeObject;
        public object? ReferenceToSameObject;
        public object? ReferenceToSelf;
    }

    [Fact]
    public void CanRead_CustomTypeWithObjectFields()
    {
        CustomTypeWithObjectField input = new()
        {
            ActualObject = new object(),
            SomeObject = "string"
        };

        input.ReferenceToSameObject = input.ActualObject;
        input.ReferenceToSelf = input;

        using MemoryStream stream = Serialize(input);

        ClassRecord classRecord = NrbfDecoder.DecodeClassRecord(stream);

        Assert.Equal(input.SomeObject, classRecord.GetString(nameof(CustomTypeWithObjectField.SomeObject)));
        Assert.Same(classRecord.GetRawValue(nameof(CustomTypeWithObjectField.ActualObject)),
                    classRecord.GetRawValue(nameof(CustomTypeWithObjectField.ReferenceToSameObject)));
        Assert.Same(classRecord, classRecord.GetClassRecord(nameof(CustomTypeWithObjectField.ReferenceToSelf)));
    }

    [Serializable]
    public class CustomTypeWithPrimitiveArrayFields
    {
        public byte[]? Bytes;
        public sbyte[]? SignedBytes;
        public short[]? Shorts;
        public ushort[]? UnsignedShorts;
        public int[]? Integers;
        public uint[]? UnsignedIntegers;
        public long[]? Longs;
        public ulong[]? UnsignedLongs;
    }

    [Fact]
    public void CanRead_CustomTypeWithPrimitiveArrayFields()
    {
        CustomTypeWithPrimitiveArrayFields input = new()
        {
            Bytes = [1, 2],
            SignedBytes = [2, 3, 4],
            Shorts = [-3, 3],
            UnsignedShorts = [4, 45],
            Integers = [-123, 222],
            UnsignedIntegers = [666, 300, 7],
            Longs = [long.MaxValue, long.MinValue],
            UnsignedLongs = [ulong.MaxValue, ulong.MinValue],
        };

        using MemoryStream stream = Serialize(input);

        ClassRecord classRecord = NrbfDecoder.DecodeClassRecord(stream);

        Verify(input.Bytes, classRecord, nameof(CustomTypeWithPrimitiveArrayFields.Bytes));
        Verify(input.SignedBytes, classRecord, nameof(CustomTypeWithPrimitiveArrayFields.SignedBytes));
        Verify(input.Shorts, classRecord, nameof(CustomTypeWithPrimitiveArrayFields.Shorts));
        Verify(input.UnsignedShorts, classRecord, nameof(CustomTypeWithPrimitiveArrayFields.UnsignedShorts));
        Verify(input.Integers, classRecord, nameof(CustomTypeWithPrimitiveArrayFields.Integers));
        Verify(input.UnsignedIntegers, classRecord, nameof(CustomTypeWithPrimitiveArrayFields.UnsignedIntegers));
        Verify(input.Longs, classRecord, nameof(CustomTypeWithPrimitiveArrayFields.Longs));
        Verify(input.UnsignedLongs, classRecord, nameof(CustomTypeWithPrimitiveArrayFields.UnsignedLongs));

        static void Verify<T>(T[] expected, ClassRecord classRecord, string fieldName) where T : unmanaged
        {
            var arrayRecord = (SZArrayRecord<T>)classRecord.GetSerializationRecord(fieldName)!;
            Assert.Equal(expected, arrayRecord.GetArray());
        }
    }

    [Serializable]
    public class CustomTypeWithStringArrayField
    {
        public string[]? Texts;
    }

    [Theory]
    [InlineData("Hello", ", ", "World!")]
    [InlineData("Single ", "null", null)]
    [InlineData("Multiple ", null, null)]
    public void CanRead_CustomTypeWithStringsArrayField(string value0, string value1, string value2)
    {
        CustomTypeWithStringArrayField input = new()
        {
            Texts = [value0, value1, value2]
        };

        ClassRecord classRecord = NrbfDecoder.DecodeClassRecord(Serialize(input));

        Assert.Equal(input.Texts, ((SZArrayRecord<string>)classRecord.GetArrayRecord(nameof(CustomTypeWithStringArrayField.Texts))).GetArray());
    }

    [Theory]
    [InlineData(byte.MaxValue)] // ObjectNullMultiple256
    [InlineData(byte.MaxValue + 2)] // ObjectNullMultiple
    public void CanRead_CustomTypeWithMultipleNullsInStringsArray(int nullCount)
    {
        CustomTypeWithStringArrayField input = new()
        {
            Texts = Enumerable.Repeat<string>(null!, nullCount).ToArray()
        };

        ClassRecord classRecord = NrbfDecoder.DecodeClassRecord(Serialize(input));

        Assert.Equal(input.Texts, ((SZArrayRecord<string>)classRecord.GetArrayRecord(nameof(CustomTypeWithStringArrayField.Texts))).GetArray());
    }

    [Fact]
    public void CanRead_RawStringArrays()
    {
        string[] input = ["TopObject", "Is", "An", "Array", "Of", "Strings"];

        using MemoryStream stream = Serialize(input);

        string?[] output = ((SZArrayRecord<string>)NrbfDecoder.Decode(stream)).GetArray();

        Assert.Equal(input, output);
    }

    [Fact]
    public void CanReadArraysOfPrimitiveTypes()
    {
        ulong[] input = [0, 1, 2, 3, 4, ulong.MaxValue];

        using MemoryStream stream = Serialize(input);

        ulong[] output = ((SZArrayRecord<ulong>)NrbfDecoder.Decode(stream)).GetArray();

        Assert.Equal(input, output);
    }

    [Fact]
    public void CanRead_ComplexSystemType()
    {
        Exception input = new("Hello, World!");

        ClassRecord classRecord = NrbfDecoder.DecodeClassRecord(Serialize(input));

        Assert.True(classRecord.HasMember(nameof(Exception.Message)));
        Assert.Equal(input.Message, classRecord.GetString(nameof(Exception.Message)));
        Assert.Equal(typeof(Exception).FullName, classRecord.TypeName.FullName);
        Assert.Equal("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089", classRecord.TypeName.AssemblyName!.FullName);
        Assert.False(classRecord.HasMember("NotPresent"));
    }

    [Fact]
    public void CanRead_ComplexSystemType_ThatReferencesOtherClassRecord()
    {
        ArgumentNullException inner = new(paramName: "innerPara");
        Exception outer = new("outer", inner);

        ClassRecord outerRecord = NrbfDecoder.DecodeClassRecord(Serialize(outer));

        Assert.Equal(outer.Message, outerRecord.GetString(nameof(Exception.Message)));

        ClassRecord innerRecord = outerRecord.GetClassRecord(nameof(Exception.InnerException))!;
        Assert.Equal(inner.ParamName, innerRecord.GetString(nameof(ArgumentNullException.ParamName)));
        Assert.Null(innerRecord.GetClassRecord(nameof(Exception.InnerException)));
    }

    [Fact]
    public void CanRead_ArraysOfComplexTypes()
    {
        CustomTypeWithPrimitiveFields[] input = [
            new () { Byte = 1 },
            new () { Integer = 3 },
            new () { Short = 4 },
            new () { Long = 5 },
        ];

        SZArrayRecord<ClassRecord> arrayRecord = ((SZArrayRecord<ClassRecord>)NrbfDecoder.Decode(Serialize(input)));

        Assert.Equal(typeof(CustomTypeWithPrimitiveFields[]).FullName, arrayRecord.TypeName.FullName);
        Assert.Equal(typeof(CustomTypeWithPrimitiveFields).Assembly.FullName, arrayRecord.TypeName.GetElementType().AssemblyName!.FullName);
        ClassRecord?[] classRecords = arrayRecord.GetArray();
        for (int i = 0; i < input.Length; i++)
        {
            Verify(input[i], classRecords[i]!);
        }
    }

    [Serializable]
    public class CustomTypeWithArrayOfComplexTypes
    {
        public CustomTypeWithPrimitiveFields?[]? Array;
    }

    [Fact]
    public void CanRead_TypesWithArraysOfComplexTypes()
    {
        CustomTypeWithArrayOfComplexTypes input = new()
        {
            Array =
            [
                new() { Byte = 1 },
                new() { Integer = 2 },
                new() { Short = 3 },
                new() { Long = 4 },
                new() { UnsignedInteger = 5 },
                null!
            ]
        };

        ClassRecord classRecord = NrbfDecoder.DecodeClassRecord(Serialize(input));

        SZArrayRecord<ClassRecord> classRecords = (SZArrayRecord<ClassRecord>)classRecord.GetSerializationRecord(nameof(CustomTypeWithArrayOfComplexTypes.Array))!;
        ClassRecord?[] array = classRecords.GetArray();
    }

    [Theory]
    [InlineData(byte.MaxValue)] // ObjectNullMultiple256
    [InlineData(byte.MaxValue + 2)] // ObjectNullMultiple
    public void CanRead_TypesWithArraysOfComplexTypes_MultipleNulls(int nullCount)
    {
        CustomTypeWithArrayOfComplexTypes input = new()
        {
            Array = Enumerable.Repeat<CustomTypeWithPrimitiveFields>(null!, nullCount).ToArray()
        };

        using MemoryStream stream = Serialize(input);

        ClassRecord classRecord = NrbfDecoder.DecodeClassRecord(stream);

        SZArrayRecord<ClassRecord> classRecords = (SZArrayRecord<ClassRecord>)classRecord.GetSerializationRecord(nameof(CustomTypeWithArrayOfComplexTypes.Array))!;
        ClassRecord?[] array = classRecords.GetArray();
        Assert.Equal(nullCount, array.Length);
        Assert.All(array, Assert.Null);

        Assert.Throws<SerializationException>(() => classRecords.GetArray(allowNulls: false));
    }

    [Fact]
    public void CanRead_ArraysOfObjects()
    {
        object?[] input = [
            1,
            "test",
            null
        ];

        ArrayRecord arrayRecord = (ArrayRecord)NrbfDecoder.Decode(Serialize(input));

        Assert.Equal(typeof(object[]).FullName, arrayRecord.TypeName.FullName);
        Assert.Equal("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089", arrayRecord.TypeName.GetElementType().AssemblyName!.FullName);
        Assert.Equal(input, ((SZArrayRecord<object>)arrayRecord).GetArray());
    }

    [Theory]
    [InlineData(byte.MaxValue)] // ObjectNullMultiple256
    [InlineData(byte.MaxValue + 2)] // ObjectNullMultiple
    public void CanRead_ArraysOfObjects_MultipleNulls(int nullCount)
    {
        object?[] input = Enumerable.Repeat<object>(null!, nullCount).ToArray();

        ArrayRecord arrayRecord = (ArrayRecord)NrbfDecoder.Decode(Serialize(input));
        object?[] output = ((SZArrayRecord<object>)arrayRecord).GetArray();

        Assert.Equal(nullCount, output.Length);
        Assert.All(output, Assert.Null);
    }

    [Serializable]
    public class CustomTypeWithArrayOfObjects
    {
        public object?[]? Array;
    }

    [Fact]
    public void CanRead_CustomTypeWithArrayOfObjects()
    {
        CustomTypeWithArrayOfObjects input = new()
        {
            Array = [
                1,
                false,
                "string",
                null
            ]
        };

        ClassRecord classRecord = NrbfDecoder.DecodeClassRecord(Serialize(input));
        SZArrayRecord<object> arrayRecord = (SZArrayRecord<object>)classRecord.GetSerializationRecord(nameof(CustomTypeWithArrayOfObjects.Array))!;

        Assert.Equal(input.Array, arrayRecord.GetArray());
    }

    [Theory]
    [InlineData("notEmpty")]
    [InlineData("")] // null is prohibited by the BinaryFormatter itself
    public void CanReadString(string input)
    {
        string output = ((PrimitiveTypeRecord<string>)NrbfDecoder.Decode(Serialize(input))).Value;

        Assert.Equal(input, output);
    }

    [Fact]
    public void CanReadPrimitiveTypes()
    {
        Verify(true);
        Verify('c');
        Verify(byte.MaxValue);
        Verify(sbyte.MaxValue);
        Verify(short.MaxValue);
        Verify(ushort.MaxValue);
        Verify(int.MaxValue);
        Verify(uint.MaxValue);
        Verify(long.MaxValue);
        Verify(ulong.MaxValue);
        Verify(float.MaxValue);
        Verify(double.MaxValue);
        Verify(decimal.MaxValue);
        Verify(TimeSpan.MaxValue);
        Verify(DateTime.Now);
#if NET
        Verify(nint.MaxValue);
        Verify(nuint.MaxValue);
#endif

        static void Verify<T>(T input) where T : unmanaged
        {
            PrimitiveTypeRecord<T> record = (PrimitiveTypeRecord<T>)NrbfDecoder.Decode(Serialize(input));
            Assert.Equal(input, record.Value);
        }
    }

    [Serializable]
    public struct SerializableStruct
    {
        public int Integer;
        public string? Text;
    }

    [Fact]
    public void CanReadStruct()
    {
        SerializableStruct input = new()
        {
            Integer = 1988,
            Text = "StructsAreRepresentedWithClassRecords"
        };

        ClassRecord classRecord = NrbfDecoder.DecodeClassRecord(Serialize(input));

        Assert.Equal(input.Integer, classRecord.GetInt32(nameof(SerializableStruct.Integer)));
        Assert.Equal(input.Text, classRecord.GetString(nameof(SerializableStruct.Text)));
        Assert.Equal(typeof(SerializableStruct).FullName, classRecord.TypeName.FullName);
        Assert.Equal(typeof(SerializableStruct).AssemblyQualifiedName, classRecord.TypeName.AssemblyQualifiedName);
        Assert.Equal(typeof(SerializableStruct).Assembly.FullName, classRecord.TypeName.AssemblyName!.FullName);
    }
}
