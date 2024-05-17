using System.Linq;
using Xunit;

namespace System.Runtime.Serialization.BinaryFormat.Tests;

public class JaggedArraysTests : ReadTests
{
    [Fact]
    public void CanReadJaggedArraysOfPrimitiveTypes_2D()
    {
        int[][] input = new int[7][];
        for (int i = 0; i < input.Length; i++)
        {
            input[i] = [i, i, i];
        }

        var arrayRecord = (ArrayRecord)PayloadReader.Read(Serialize(input));

        Assert.Equal((uint)input.Length, arrayRecord.Length);
        Assert.Equal(ArrayType.Jagged, arrayRecord.ArrayType);
        Assert.Equal(input, arrayRecord.ToArray(input.GetType()));
    }

    [Fact]
    public void CanReadJaggedArraysOfPrimitiveTypes_3D()
    {
        int[][][] input = new int[7][][];
        for (int i = 0; i < input.Length; i++)
        {
            input[i] = new int[1][];
            input[i][0] = [i, i, i];
        }

        var arrayRecord = (ArrayRecord)PayloadReader.Read(Serialize(input));

        Assert.Equal((uint)input.Length, arrayRecord.Length);
        Assert.Equal(ArrayType.Jagged, arrayRecord.ArrayType);
        Assert.Equal(input, arrayRecord.ToArray(input.GetType()));
        Assert.Equal(1, arrayRecord.Rank);
    }

    [Fact]
    public void CanReadJaggedArraysOfStrings()
    {
        string[][] input = new string[5][];
        for (int i = 0; i < input.Length; i++)
        {
            input[i] = ["a", "b", "c"];
        }

        var arrayRecord = (ArrayRecord)PayloadReader.Read(Serialize(input));

        Assert.Equal((uint)input.Length, arrayRecord.Length);
        Assert.Equal(ArrayType.Jagged, arrayRecord.ArrayType);
        Assert.Equal(input, arrayRecord.ToArray(input.GetType()));
    }

    [Fact]
    public void CanReadJaggedArraysOfObjects()
    {
        object[][] input = new object[3][];
        for (int i = 0; i < input.Length; i++)
        {
            input[i] = ["a", 1, DateTime.MaxValue];
        }

        var arrayRecord = (ArrayRecord)PayloadReader.Read(Serialize(input));

        Assert.Equal((uint)input.Length, arrayRecord.Length);
        Assert.Equal(ArrayType.Jagged, arrayRecord.ArrayType);
        Assert.Equal(input, arrayRecord.ToArray(input.GetType()));
    }

    [Serializable]
    public class ComplexType
    {
        public int SomeField;
    }

    [Fact]
    public void CanReadJaggedArraysOfComplexTypes()
    {
        ComplexType[][] input = new ComplexType[3][];
        for (int i = 0; i < input.Length; i++)
        {
            input[i] = Enumerable.Range(0, i + 1).Select(j => new ComplexType { SomeField = j }).ToArray();
        }

        var arrayRecord = (ArrayRecord)PayloadReader.Read(Serialize(input));

        Assert.Equal((uint)input.Length, arrayRecord.Length);
        Assert.Equal(ArrayType.Jagged, arrayRecord.ArrayType);
        var output = (ClassRecord?[][])arrayRecord.ToArray(input.GetType());
        for (int i = 0; i < input.Length; i++)
        {
            for (int j = 0; j < input[i].Length; j++)
            {
                Assert.Equal(input[i][j].SomeField, output[i][j]!.GetInt32(nameof(ComplexType.SomeField)));
            }
        }
    }
}
