// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Resources.Extensions.Tests.Common;

public abstract class JaggedArrayTests<T> : SerializationTest<T> where T : ISerializer
{
    [Fact]
    public void IntegerArraysTwoLevels()
    {
        int[][] jaggedArray = [[0, 1], [10, 11]];

        Stream stream = Serialize(jaggedArray);
        object deserialized = Deserialize(stream);

        deserialized.Should().BeEquivalentTo(jaggedArray);
    }

    [Fact]
    public void IntegerArraysThreeLevels()
    {
        int[][][] jaggedArray = [[[0, 1], [10, 11]], [[100, 101], [110, 111]]];

        Stream stream = Serialize(jaggedArray);
        object deserialized = Deserialize(stream);
        deserialized.Should().BeEquivalentTo(jaggedArray);
    }

    [Fact]
    public void JaggedEmpty()
    {
        int[][] jaggedEmpty = new int[1][];

        object deserialized = Deserialize(Serialize(jaggedEmpty));
        deserialized.Should().BeEquivalentTo(jaggedEmpty);
    }
}
