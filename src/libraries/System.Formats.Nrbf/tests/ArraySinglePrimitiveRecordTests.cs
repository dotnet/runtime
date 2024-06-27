// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
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
        foreach (int size in new[] { 1, 127, 128, 512_001, 512_001 })
        {
            yield return new object[] { size, true };
            yield return new object[] { size, false };
        }
    }

    [Theory]
    [MemberData(nameof(GetCanReadArrayOfAnySizeArgs))]
    public void CanReadArrayOfAnySize_Bool(int size, bool canSeek) => Test<bool>(size, canSeek);

    [Theory]
    [MemberData(nameof(GetCanReadArrayOfAnySizeArgs))]
    public void CanReadArrayOfAnySize_Byte(int size, bool canSeek) => Test<byte>(size, canSeek);

    [Theory]
    [MemberData(nameof(GetCanReadArrayOfAnySizeArgs))]
    public void CanReadArrayOfAnySize_SByte(int size, bool canSeek) => Test<sbyte>(size, canSeek);

    [Theory]
    [MemberData(nameof(GetCanReadArrayOfAnySizeArgs))]
    public void CanReadArrayOfAnySize_Char(int size, bool canSeek) => Test<char>(size, canSeek);

    [Theory]
    [MemberData(nameof(GetCanReadArrayOfAnySizeArgs))]
    public void CanReadArrayOfAnySize_Int16(int size, bool canSeek) => Test<short>(size, canSeek);

    [Theory]
    [MemberData(nameof(GetCanReadArrayOfAnySizeArgs))]
    public void CanReadArrayOfAnySize_UInt16(int size, bool canSeek) => Test<ushort>(size, canSeek);

    [Theory]
    [MemberData(nameof(GetCanReadArrayOfAnySizeArgs))]
    public void CanReadArrayOfAnySize_Int32(int size, bool canSeek) => Test<int>(size, canSeek);

    [Theory]
    [MemberData(nameof(GetCanReadArrayOfAnySizeArgs))]
    public void CanReadArrayOfAnySize_UInt32(int size, bool canSeek) => Test<uint>(size, canSeek);

    [Theory]
    [MemberData(nameof(GetCanReadArrayOfAnySizeArgs))]
    public void CanReadArrayOfAnySize_Int64(int size, bool canSeek) => Test<long>(size, canSeek);

    [Theory]
    [MemberData(nameof(GetCanReadArrayOfAnySizeArgs))]
    public void CanReadArrayOfAnySize_UInt64(int size, bool canSeek) => Test<ulong>(size, canSeek);

    [Theory]
    [MemberData(nameof(GetCanReadArrayOfAnySizeArgs))]
    public void CanReadArrayOfAnySize_Single(int size, bool canSeek) => Test<float>(size, canSeek);

    [Theory]
    [MemberData(nameof(GetCanReadArrayOfAnySizeArgs))]
    public void CanReadArrayOfAnySize_Double(int size, bool canSeek) => Test<double>(size, canSeek);

    [Theory]
    [MemberData(nameof(GetCanReadArrayOfAnySizeArgs))]
    public void CanReadArrayOfAnySize_TimeSpan(int size, bool canSeek) => Test<TimeSpan>(size, canSeek);

    [Theory]
    [MemberData(nameof(GetCanReadArrayOfAnySizeArgs))]
    public void CanReadArrayOfAnySize_DateTime(int size, bool canSeek) => Test<DateTime>(size, canSeek);

    private void Test<T>(int size, bool canSeek)
    {
        Random constSeed = new Random(27644437);
        T[] input = new T[size];
        for (int i = 0; i < input.Length; i++)
        {
            input[i] = GenerateValue<T>(constSeed);
        }

        MemoryStream stream = Serialize(input);
        stream = canSeek ? stream : new NonSeekableStream(stream.ToArray());
        SZArrayRecord<T> arrayRecord = (SZArrayRecord<T>)NrbfDecoder.Decode(stream);

        Assert.Equal(size, arrayRecord.Length);
        T?[] output = arrayRecord.GetArray();
        Assert.Equal(input, output);
        Assert.Same(output, arrayRecord.GetArray());
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
