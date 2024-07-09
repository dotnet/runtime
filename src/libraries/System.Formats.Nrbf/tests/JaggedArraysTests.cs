using System.Formats.Nrbf.Utils;
using System.Linq;
using Xunit;

namespace System.Formats.Nrbf.Tests;

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

        var arrayRecord = (ArrayRecord)NrbfDecoder.Decode(Serialize(input));

        Verify(input, arrayRecord);
        Assert.Equal(input, arrayRecord.GetArray(input.GetType()));
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

        var arrayRecord = (ArrayRecord)NrbfDecoder.Decode(Serialize(input));

        Verify(input, arrayRecord);
        Assert.Equal(input, arrayRecord.GetArray(input.GetType()));
        Assert.Equal(1, arrayRecord.Rank);
    }

    [Fact]
    public void CanReadJaggedArrayOfRectangularArrays()
    {
        int[][,] input = new int[7][,];
        for (int i = 0; i < input.Length; i++)
        {
            input[i] = new int[3,3];

            for (int j = 0; j < input[i].GetLength(0); j++)
            {
                for (int k = 0; k < input[i].GetLength(1); k++)
                {
                    input[i][j, k] = i * j * k;
                }
            }
        }

        var arrayRecord = (ArrayRecord)NrbfDecoder.Decode(Serialize(input));

        Verify(input, arrayRecord);
        Assert.Equal(input, arrayRecord.GetArray(input.GetType()));
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

        var arrayRecord = (ArrayRecord)NrbfDecoder.Decode(Serialize(input));

        Verify(input, arrayRecord);
        Assert.Equal(input, arrayRecord.GetArray(input.GetType()));
    }

    [Fact]
    public void CanReadJaggedArraysOfObjects()
    {
        object[][] input = new object[3][];
        for (int i = 0; i < input.Length; i++)
        {
            input[i] = ["a", 1, DateTime.MaxValue];
        }

        var arrayRecord = (ArrayRecord)NrbfDecoder.Decode(Serialize(input));

        Verify(input, arrayRecord);
        Assert.Equal(input, arrayRecord.GetArray(input.GetType()));
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

        var arrayRecord = (ArrayRecord)NrbfDecoder.Decode(Serialize(input));

        Verify(input, arrayRecord);
        var output = (ClassRecord?[][])arrayRecord.GetArray(input.GetType());
        for (int i = 0; i < input.Length; i++)
        {
            for (int j = 0; j < input[i].Length; j++)
            {
                Assert.Equal(input[i][j].SomeField, output[i][j]!.GetInt32(nameof(ComplexType.SomeField)));
            }
        }
    }

    private static void Verify(Array input, ArrayRecord arrayRecord)
    {
        Assert.Equal(1, arrayRecord.Lengths.Length);
        Assert.Equal(input.Length, arrayRecord.Lengths[0]);
        Assert.True(arrayRecord.TypeName.GetElementType().IsArray); // true only for Jagged arrays
        Assert.Equal(input.GetType().FullName, arrayRecord.TypeName.FullName);
        Assert.Equal(input.GetType().GetAssemblyNameIncludingTypeForwards(), arrayRecord.TypeName.AssemblyName!.FullName);
    }
}
