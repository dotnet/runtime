// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Resources.Extensions.Tests.Common;

public abstract class MultidimensionalArrayTests<T> : SerializationTest<T> where T : ISerializer
{
    [Fact]
    public void StringArrays()
    {
        string[,] twoDimensions = new string[2, 2];
        twoDimensions[0, 0] = "00";
        twoDimensions[0, 1] = "01";
        twoDimensions[1, 0] = "10";
        twoDimensions[1, 1] = "11";

        // Raw data will be 0, 1, 10, 11 in memory and in the binary stream
        Stream stream = Serialize(twoDimensions);

        object deserialized = Deserialize(stream);
        Assert.Equal(twoDimensions, deserialized);
    }

    [Fact]
    public void IntegerArrays_Basic()
    {
        int[,] twoDimensions = new int[2, 2];
        twoDimensions[0, 0] = 0;
        twoDimensions[0, 1] = 1;
        twoDimensions[1, 0] = 10;
        twoDimensions[1, 1] = 11;

        // Raw data will be 0, 1, 10, 11 in memory and in the binary stream
        object deserialized = Deserialize(Serialize(twoDimensions));

        Assert.Equal(twoDimensions, deserialized);

        int[,,] threeDimensions = new int[2, 2, 2];
        threeDimensions[0, 0, 0] = 888;
        threeDimensions[0, 0, 1] = 881;
        threeDimensions[0, 1, 0] = 810;
        threeDimensions[0, 1, 1] = 811;
        threeDimensions[1, 0, 0] = 100;
        threeDimensions[1, 0, 1] = 101;
        threeDimensions[1, 1, 0] = 110;
        threeDimensions[1, 1, 1] = 111;

        deserialized = Deserialize(Serialize(threeDimensions));
        Assert.Equal(threeDimensions, deserialized);
    }

    [Serializable]
    public class CustomComparable : IComparable, IEquatable<CustomComparable>
    {
        public int Integer;

        public int CompareTo(object? obj)
        {
            CustomComparable other = (CustomComparable)obj;

            return other.Integer.CompareTo(other.Integer);
        }

        public bool Equals(CustomComparable? other) => Integer == other.Integer;

        public override int GetHashCode() => Integer;

        public override bool Equals(object? obj) => obj is CustomComparable other && Equals(other);
    }

    [Fact]
    public void MultiDimensionalArrayOfMultiDimensionalArrays_Integers()
        => MultiDimensionalArrayOfMultiDimensionalArrays<int>(static (x, y) => x * y);

    [Fact]
    public void MultiDimensionalArrayOfMultiDimensionalArrays_Doubles()
        => MultiDimensionalArrayOfMultiDimensionalArrays<double>(static (x, y) => x * y / 10);

    [Fact]
    public void MultiDimensionalArrayOfMultiDimensionalArrays_Strings()
        => MultiDimensionalArrayOfMultiDimensionalArrays<string>(static (x, y) => $"{x},{y}");

    [Fact]
    public void MultiDimensionalArrayOfMultiDimensionalArrays_Abstraction()
        => MultiDimensionalArrayOfMultiDimensionalArrays<IComparable>(static (x, y) => x switch
        {
            0 => x * y, // int
            1 => x + (double)y / 10, // double
            2 => $"{x},{y}", // string
            _ => new CustomComparable() { Integer = x * y }
        });

    [Fact]
    public void MultiDimensionalArrayOfMultiDimensionalArrays_Objects()
        => MultiDimensionalArrayOfMultiDimensionalArrays<object>(static (x, y) => x switch
        {
            0 => x * y, // int
            1 => x + (double)y / 10, // double
            2 => $"{x},{y}", // string
            _ => new CustomComparable() { Integer = x * y }
        });

    private static void MultiDimensionalArrayOfMultiDimensionalArrays<TValue>(Func<int, int, TValue> valueFactory)
    {
        TValue[,][,] input = new TValue[3, 3][,];
        for (int i = 0; i < input.GetLength(0); i++)
        {
            for (int j = 0; j < input.GetLength(1); j++)
            {
                TValue[,] contained = new TValue[i + 1, j + 1];
                for (int k = 0; k < contained.GetLength(0); k++)
                {
                    for (int l = 0; l < contained.GetLength(1); l++)
                    {
                        contained[k, l] = valueFactory(k, l);
                    }
                }

                input[i, j] = contained;

                object deserializedMd = Deserialize(Serialize(contained));
                Assert.Equal(contained, deserializedMd);
            }
        }

        object deserializedJagged = Deserialize(Serialize(input));
        Assert.Equal(input, deserializedJagged);
    }

    [Fact]
    public void EmptyDimensions()
    {
        // Didn't even know this was possible.
        int[,] twoDimensionOneEmpty = new int[1, 0];
        object deserialized = Deserialize(Serialize(twoDimensionOneEmpty));
        Assert.Equal(twoDimensionOneEmpty, deserialized);

        int[,] twoDimensionEmptyOne = new int[0, 1];
        deserialized = Deserialize(Serialize(twoDimensionEmptyOne));
        Assert.Equal(twoDimensionEmptyOne, deserialized);

        int[,] twoDimensionEmpty = new int[0, 0];
        deserialized = Deserialize(Serialize(twoDimensionEmpty));
        Assert.Equal(twoDimensionEmpty, deserialized);

        int[,,] threeDimension = new int[1, 0, 1];
        deserialized = Deserialize(Serialize(threeDimension));
        Assert.Equal(threeDimension, deserialized);
    }

    [Theory]
    [MemberData(nameof(DimensionLengthsTestData))]
    public void IntegerArrays(int[] lengths)
    {
        Array array = Array.CreateInstance(typeof(int), lengths);

        InitArray(array);

        Array deserialized = (Array)Deserialize(Serialize(array));
        Assert.Equal(array, deserialized);
    }

    public static TheoryData<int[]> DimensionLengthsTestData { get; } = new()
    {
        new int[] { 2, 2 },
        new int[] { 3, 2 },
        new int[] { 2, 3 },
        new int[] { 3, 3, 3 },
        new int[] { 2, 3, 4 },
        new int[] { 4, 3, 2 },
        new int[] { 2, 3, 4, 5 }
    };

    [Fact]
    public void MaxDimensions()
    {
        int[] lengths = new int[32];

        // Even at 2 in every dimension it would be uint.MaxValue in LongLength and 16GB of memory.
        lengths.AsSpan().Fill(1);
        Array array = Array.CreateInstance(typeof(int), lengths);

        InitArray(array);

        Array deserialized = (Array)Deserialize(Serialize(array));
        Assert.Equal(array, deserialized);
    }

    private static void InitArray(Array array)
    {
        ref byte arrayDataRef = ref MemoryMarshal.GetArrayDataReference(array);
        ref int elementRef = ref Unsafe.As<byte, int>(ref arrayDataRef);
        nuint flattenedIndex = 0;

        for (int i = 0; i < array.LongLength; i++)
        {
            ref int offsetElementRef = ref Unsafe.Add(ref elementRef, flattenedIndex);
            offsetElementRef = i;
            flattenedIndex++;
        }
    }
}
