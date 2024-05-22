// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Serialization;
using System.Windows.Forms.BinaryFormat;

namespace FormatTests.Common;

public abstract class ArrayTests<T> : SerializationTest<T> where T : ISerializer
{
    [Fact]
    public virtual void Roundtrip_ArrayContainingArrayAtNonZeroLowerBound()
    {
        // Not supported by BinaryFormattedObject
        RoundTrip(Array.CreateInstance(typeof(uint[]), [5], [1]));
    }

    [Fact]
    public void ArraySerializableValueType()
    {
        nint[] nints = [42, 43, 44];
        object deserialized = Deserialize(Serialize(nints));
        deserialized.Should().BeEquivalentTo(nints);
    }

    [Fact]
    public void SameObjectRepeatedInArray()
    {
        object o = new();
        object[] arr = [o, o, o, o, o];
        object[] result = (object[])Deserialize(Serialize(arr));

        Assert.Equal(arr.Length, result.Length);
        Assert.NotSame(arr, result);
        Assert.NotSame(arr[0], result[0]);
        for (int i = 1; i < result.Length; i++)
        {
            Assert.Same(result[0], result[i]);
        }
    }

    [Theory]
    [InlineData(-1, (byte)RecordType.ArraySingleObject)]
    [InlineData(int.MinValue, (byte)RecordType.ArraySingleObject)]
    [InlineData(-1, (byte)RecordType.ArraySinglePrimitive)]
    [InlineData(int.MinValue, (byte)RecordType.ArraySinglePrimitive)]
    [InlineData(-1, (byte)RecordType.ArraySingleString)]
    [InlineData(int.MinValue, (byte)RecordType.ArraySingleString)]
    public void NegativeLength(int length, byte recordType)
    {
        MemoryStream stream = new();
        using (BinaryFormatWriterScope scope = new(stream))
        {
            scope.Writer.Write(recordType);

            // Id
            scope.Writer.Write(1);

            // Length
            scope.Writer.Write(length);
        }

        stream.Position = 0;
        Action action = () => Deserialize(stream);
        action.Should().Throw<SerializationException>();
    }

    [Theory]
    [InlineData(-1, (byte)BinaryArrayType.Single)]
    [InlineData(int.MinValue, (byte)BinaryArrayType.Single)]
    [InlineData(-1, (byte)BinaryArrayType.Rectangular)]
    [InlineData(int.MinValue, (byte)BinaryArrayType.Rectangular)]
    [InlineData(-1, (byte)BinaryArrayType.Jagged)]
    [InlineData(int.MinValue, (byte)BinaryArrayType.Jagged)]
    public void BinaryArray_NegativeLength(int length, byte arrayType)
    {
        MemoryStream stream = new();
        using (BinaryFormatWriterScope scope = new(stream))
        {
            scope.Writer.Write((byte)RecordType.BinaryArray);

            // Id
            scope.Writer.Write(1);
            scope.Writer.Write(arrayType);

            // Rank
            scope.Writer.Write(1);

            // Length
            scope.Writer.Write(length);
        }

        stream.Position = 0;
        Action action = () => Deserialize(stream);
        action.Should().Throw<SerializationException>();
    }

    [Theory]
    [InlineData(0, (byte)BinaryArrayType.Single)]
    [InlineData(0, (byte)BinaryArrayType.Rectangular)]
    [InlineData(0, (byte)BinaryArrayType.Jagged)]
    [InlineData(-1, (byte)BinaryArrayType.Single)]
    [InlineData(-1, (byte)BinaryArrayType.Rectangular)]
    [InlineData(-1, (byte)BinaryArrayType.Jagged)]
    [InlineData(int.MinValue, (byte)BinaryArrayType.Single)]
    [InlineData(int.MinValue, (byte)BinaryArrayType.Rectangular)]
    [InlineData(int.MinValue, (byte)BinaryArrayType.Jagged)]
    [InlineData(33, (byte)BinaryArrayType.Rectangular)]
    public void BinaryArray_InvalidRank(int rank, byte arrayType)
    {
        MemoryStream stream = new();
        using (BinaryFormatWriterScope scope = new(stream))
        {
            scope.Writer.Write((byte)RecordType.BinaryArray);

            // Id
            scope.Writer.Write(1);
            scope.Writer.Write(arrayType);

            // Rank
            scope.Writer.Write(rank);

            // Lengths
            int lengths = rank > 0 ? rank : 1;
            for (int i = 0; i < lengths; i++)
            {
                scope.Writer.Write(0);
            }

            scope.Writer.Write((byte)BinaryType.Object);
        }

        stream.Position = 0;
        Action action = () => Deserialize(stream);
        action.Should().Throw<SerializationException>();
    }

    [Theory]
    [InlineData(2, (byte)BinaryArrayType.Single)]
    [InlineData(2, (byte)BinaryArrayType.Jagged)]
    public virtual void BinaryArray_InvalidRank_Positive(int rank, byte arrayType)
    {
        MemoryStream stream = new();
        using (BinaryFormatWriterScope scope = new(stream))
        {
            scope.Writer.Write((byte)RecordType.BinaryArray);

            // Id
            scope.Writer.Write(1);
            scope.Writer.Write(arrayType);

            // Rank
            scope.Writer.Write(rank);

            // Lengths
            int lengths = rank > 0 ? rank : 1;
            for (int i = 0; i < lengths; i++)
            {
                scope.Writer.Write(0);
            }

            scope.Writer.Write((byte)BinaryType.Object);
        }

        // BinaryFormatter doesn't reject these outright.
        stream.Position = 0;
        Deserialize(stream);
    }
}
