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
        deserialized.Should().BeEquivalentTo(twoDimensions);
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

        deserialized.Should().BeEquivalentTo(twoDimensions);

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
        deserialized.Should().BeEquivalentTo(threeDimensions);
    }

    [Fact]
    public void EmptyDimensions()
    {
        // Didn't even know this was possible.
        int[,] twoDimensionOneEmpty = new int[1, 0];
        object deserialized = Deserialize(Serialize(twoDimensionOneEmpty));
        deserialized.Should().BeEquivalentTo(twoDimensionOneEmpty);

        int[,] twoDimensionEmptyOne = new int[0, 1];
        deserialized = Deserialize(Serialize(twoDimensionEmptyOne));
        deserialized.Should().BeEquivalentTo(twoDimensionEmptyOne);

        int[,] twoDimensionEmpty = new int[0, 0];
        deserialized = Deserialize(Serialize(twoDimensionEmpty));
        deserialized.Should().BeEquivalentTo(twoDimensionEmpty);

        int[,,] threeDimension = new int[1, 0, 1];
        deserialized = Deserialize(Serialize(threeDimension));
        deserialized.Should().BeEquivalentTo(threeDimension);
    }

    [Theory]
    [MemberData(nameof(DimensionLengthsTestData))]
    public void IntegerArrays(int[] lengths)
    {
        Array array = Array.CreateInstance(typeof(int), lengths);

        InitArray(array);

        Array deserialized = (Array)Deserialize(Serialize(array));
        deserialized.Should().BeEquivalentTo(deserialized);
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
        deserialized.Should().BeEquivalentTo(deserialized);
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
