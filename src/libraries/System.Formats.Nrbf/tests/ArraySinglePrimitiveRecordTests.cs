// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Text;
using Microsoft.DotNet.XUnitExtensions;
using Xunit;

namespace System.Formats.Nrbf.Tests;

public class ArraySinglePrimitiveRecordTests : ReadTests
{
    private class NonSeekableStream : MemoryStream
    {
        public NonSeekableStream(byte[] buffer) : base(buffer) { }
        public override bool CanSeek => false;
    }

    public static IEnumerable<object[]> GetCanReadArrayOfAnySizeArgs()
    {
        foreach (int size in new[] { 1, 127, 128, 20_001 })
        {
            yield return new object[] { size, true };
            yield return new object[] { size, false };
        }
    }

    [Fact]
    public void DontCastBytesToBooleans()
    {
        using MemoryStream stream = new();
        BinaryWriter writer = new(stream, Encoding.UTF8);

        WriteSerializedStreamHeader(writer);
        writer.Write((byte)SerializationRecordType.ArraySinglePrimitive);
        writer.Write(1); // object ID
        writer.Write(2); // length
        writer.Write((byte)PrimitiveType.Boolean); // element type
        writer.Write((byte)0x01);
        writer.Write((byte)0x02);
        writer.Write((byte)SerializationRecordType.MessageEnd);
        stream.Position = 0;

        SZArrayRecord<bool> serializationRecord = (SZArrayRecord<bool>)NrbfDecoder.Decode(stream);

        bool[] bools = serializationRecord.GetArray();
        bool a = bools[0];
        Assert.True(a);
        bool b = bools[1];
        Assert.True(b);
        bool c = a && b;
        Assert.True(c);
    }

    [Fact]
    public void DontCastBytesToDateTimes()
    {
        using MemoryStream stream = new();
        BinaryWriter writer = new(stream, Encoding.UTF8);

        WriteSerializedStreamHeader(writer);
        writer.Write((byte)SerializationRecordType.ArraySinglePrimitive);
        writer.Write(1); // object ID
        writer.Write(1); // length
        writer.Write((byte)PrimitiveType.DateTime); // element type
        writer.Write(ulong.MaxValue); // un-representable DateTime
        writer.Write((byte)SerializationRecordType.MessageEnd);
        stream.Position = 0;

        Assert.Throws<SerializationException>(() => NrbfDecoder.Decode(stream));
    }

    [ConditionalTheory]
    [MemberData(nameof(GetCanReadArrayOfAnySizeArgs))]
    public void CanReadArrayOfAnySize_Bool(int size, bool canSeek) => Test<bool>(size, canSeek);

    [ConditionalTheory]
    [MemberData(nameof(GetCanReadArrayOfAnySizeArgs))]
    public void CanReadArrayOfAnySize_Byte(int size, bool canSeek) => Test<byte>(size, canSeek);

    [ConditionalTheory]
    [MemberData(nameof(GetCanReadArrayOfAnySizeArgs))]
    public void CanReadArrayOfAnySize_SByte(int size, bool canSeek) => Test<sbyte>(size, canSeek);

    [ConditionalTheory]
    [MemberData(nameof(GetCanReadArrayOfAnySizeArgs))]
    public void CanReadArrayOfAnySize_Char(int size, bool canSeek) => Test<char>(size, canSeek);

    [ConditionalTheory]
    [MemberData(nameof(GetCanReadArrayOfAnySizeArgs))]
    public void CanReadArrayOfAnySize_Int16(int size, bool canSeek) => Test<short>(size, canSeek);

    [ConditionalTheory]
    [MemberData(nameof(GetCanReadArrayOfAnySizeArgs))]
    public void CanReadArrayOfAnySize_UInt16(int size, bool canSeek) => Test<ushort>(size, canSeek);

    [ConditionalTheory]
    [MemberData(nameof(GetCanReadArrayOfAnySizeArgs))]
    public void CanReadArrayOfAnySize_Int32(int size, bool canSeek) => Test<int>(size, canSeek);

    [ConditionalTheory]
    [MemberData(nameof(GetCanReadArrayOfAnySizeArgs))]
    public void CanReadArrayOfAnySize_UInt32(int size, bool canSeek) => Test<uint>(size, canSeek);

    [ConditionalTheory]
    [MemberData(nameof(GetCanReadArrayOfAnySizeArgs))]
    public void CanReadArrayOfAnySize_Int64(int size, bool canSeek) => Test<long>(size, canSeek);

    [ConditionalTheory]
    [MemberData(nameof(GetCanReadArrayOfAnySizeArgs))]
    public void CanReadArrayOfAnySize_UInt64(int size, bool canSeek) => Test<ulong>(size, canSeek);

    [ConditionalTheory]
    [MemberData(nameof(GetCanReadArrayOfAnySizeArgs))]
    public void CanReadArrayOfAnySize_Single(int size, bool canSeek) => Test<float>(size, canSeek);

    [ConditionalTheory]
    [MemberData(nameof(GetCanReadArrayOfAnySizeArgs))]
    public void CanReadArrayOfAnySize_Double(int size, bool canSeek) => Test<double>(size, canSeek);

    [ConditionalTheory]
    [MemberData(nameof(GetCanReadArrayOfAnySizeArgs))]
    public void CanReadArrayOfAnySize_TimeSpan(int size, bool canSeek) => Test<TimeSpan>(size, canSeek);

    [ConditionalTheory]
    [MemberData(nameof(GetCanReadArrayOfAnySizeArgs))]
    public void CanReadArrayOfAnySize_DateTime(int size, bool canSeek) => Test<DateTime>(size, canSeek);

    private void Test<T>(int size, bool canSeek) where T : IComparable
    {
        Random constSeed = new Random(27644437);
        T[] input = new T[size];
        for (int i = 0; i < input.Length; i++)
        {
            input[i] = GenerateValue<T>(constSeed);
        }

        TestSZArrayOfT(input, size, canSeek);
        TestSZArrayOfIComparable(input, size, canSeek);
    }

    private void TestSZArrayOfT<T>(T[] input, int size, bool canSeek)
    {
        MemoryStream stream = Serialize(input);
        stream = canSeek ? stream : new NonSeekableStream(stream.ToArray());
        SZArrayRecord<T> arrayRecord = (SZArrayRecord<T>)NrbfDecoder.Decode(stream);

        Assert.Equal(size, arrayRecord.Length);
        T?[] output = arrayRecord.GetArray();
        Assert.Equal(input, output);
        Assert.Same(output, arrayRecord.GetArray());
    }

    private void TestSZArrayOfIComparable<T>(T[] input, int size, bool canSeek) where T : IComparable
    {
        if (!IsPatched)
        {
            throw new SkipTestException("Current machine has not been patched with the most recent BinaryFormatter fix.");
        }

        // Arrays of abstractions that store primitive values (example: new IComparable[1] { int.MaxValue })
        // are represented by BinaryFormatter with a single SystemClassWithMembersAndTypesRecord
        // and multiple ClassWithIdRecord that re-use the information from the system record.
        // This requires some non-trivial mapping and this test is very important as it covers that code path.
        IComparable[] comparables = new IComparable[size];
        for (int i = 0; i < input.Length; i++)
        {
            comparables[i] = input[i];
        }

        TestArrayOfSerializationRecords(input, comparables, canSeek);
    }

    private void TestSZArrayOfObjects<T>(T[] input, int size, bool canSeek)
    {
        // Arrays of objects that store primitive values (example: new object[1] { int.MaxValue })
        // are represented by BinaryFormatter with MemberPrimitiveTypedRecord instances.
        object[] objects = new object[size];
        for (int i = 0; i < input.Length; i++)
        {
            objects[i] = input[i];
        }

        TestArrayOfSerializationRecords(input, objects, canSeek);
    }

    private void TestArrayOfSerializationRecords<T>(T[] values, object input, bool canSeek)
    {
        MemoryStream stream = Serialize(input);

        stream = canSeek ? stream : new NonSeekableStream(stream.ToArray());
        SZArrayRecord<SerializationRecord> arrayRecordOfPrimitiveRecords = (SZArrayRecord<SerializationRecord>)NrbfDecoder.Decode(stream);
        SerializationRecord[] arrayOfPrimitiveRecords = arrayRecordOfPrimitiveRecords.GetArray();
        for (int i = 0; i < values.Length; i++)
        {
            Assert.Equal(values[i], ((PrimitiveTypeRecord)arrayOfPrimitiveRecords[i]).Value);
            Assert.Equal(values[i], ((PrimitiveTypeRecord<T>)arrayOfPrimitiveRecords[i]).Value);
        }
    }

    private static T GenerateValue<T>(Random random)
    {
        if (typeof(T) == typeof(byte))
        {
            return (T)(object)(byte)random.Next(byte.MinValue, byte.MaxValue);
        }
        else if (typeof(T) == typeof(sbyte))
        {
            return (T)(object)(sbyte)random.Next(sbyte.MinValue, sbyte.MaxValue);
        }
        else if (typeof(T) == typeof(char))
        {
            return (T)(object)(char)random.Next(0, 255);
        }
        else if (typeof(T) == typeof(short))
        {
            return (T)(object)(short)random.Next(short.MaxValue);
        }
        else if (typeof(T) == typeof(ushort))
        {
            return (T)(object)(ushort)random.Next(short.MaxValue);
        }
        else if (typeof(T) == typeof(int))
        {
            return (T)(object)NextInt32(random);
        }
        else if (typeof(T) == typeof(uint))
        {
            return (T)(object)(uint)random.Next();
        }
        else if (typeof(T) == typeof(long))
        {
            return (T)(object)(long)random.Next();
        }
        else if (typeof(T) == typeof(ulong))
        {
            return (T)(object)(ulong)random.Next();
        }
        else if (typeof(T) == typeof(float))
        {
            return (T)(object)(float)random.NextDouble();
        }
        else if (typeof(T) == typeof(double))
        {
            return (T)(object)random.NextDouble();
        }
        else if (typeof(T) == typeof(bool))
        {
            return (T)(object)(random.NextDouble() > 0.5);
        }
        else if (typeof(T) == typeof(decimal))
        {
            return (T)(object)GenerateRandomDecimal(random);
        }
        else if (typeof(T) == typeof(TimeSpan))
        {
            return (T)(object)new TimeSpan(random.Next());
        }
        else if (typeof(T) == typeof(DateTime))
        {
            return (T)(object)new DateTime(random.Next());
        }
        else
        {
            throw new NotImplementedException($"{typeof(T).Name} is not implemented");
        }

        static decimal GenerateRandomDecimal(Random random)
        {
            // Decimal values have a sign, a scale, and 96 bits of significance (lo/mid/high)
            // generate those parts randomly and assemble a valid decimal
            byte scale = (byte)random.Next(29);
            bool sign = random.Next(2) == 0;
            return new decimal(NextInt32(random),
                               NextInt32(random),
                               NextInt32(random),
                               sign,
                               scale);
        }

        /// <summary>
        /// Returns a randomly generated int value from the entire range of legal 32-bit integers
        /// Note: using Random.Next will never return negative values so we can't use that where
        /// we want the complete bit-space.
        /// </summary>
        static int NextInt32(Random random)
        {
            int firstBits = random.Next(0, 1 << 4) << 28;
            int lastBits = random.Next(0, 1 << 28);
            return firstBits | lastBits;
        }
    }
}
