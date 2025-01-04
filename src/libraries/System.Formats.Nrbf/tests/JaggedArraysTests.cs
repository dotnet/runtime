using System.Formats.Nrbf.Utils;
using System.IO;
using System.Linq;
using Xunit;

namespace System.Formats.Nrbf.Tests;

public class JaggedArraysTests : ReadTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void CanReadJaggedArraysOfPrimitiveTypes_2D(bool useReferences)
    {
        int[][] input = new int[7][];
        int[] same = [1, 2, 3];
        for (int i = 0; i < input.Length; i++)
        {
            input[i] = useReferences
                ? same // reuse the same object (represented as a single record that is referenced multiple times)
                : [i, i, i]; // create new array
        }

        var arrayRecord = (ArrayRecord)NrbfDecoder.Decode(Serialize(input));

        Verify(input, arrayRecord);
        ArrayRecord?[] output = (ArrayRecord?[])arrayRecord.GetArray(input.GetType());
        for (int i = 0; i < input.Length; i++)
        {
            Assert.Equal(input[i], ((SZArrayRecord<int>)output[i]).GetArray());
            if (useReferences)
            {
                Assert.Same(((SZArrayRecord<int>)output[0]).GetArray(), ((SZArrayRecord<int>)output[i]).GetArray());
            }
        }
    }

    [Theory]
    [InlineData(1)] // SerializationRecordType.ObjectNull
    [InlineData(200)] // SerializationRecordType.ObjectNullMultiple256
    [InlineData(10_000)] // SerializationRecordType.ObjectNullMultiple
    public void NullRecordsOfAllKindsAreHandledProperly(int nullCount)
    {
        int[][] input = new int[nullCount][];

        var arrayRecord = (ArrayRecord)NrbfDecoder.Decode(Serialize(input));

        Verify(input, arrayRecord);
        ArrayRecord?[] output = (ArrayRecord?[])arrayRecord.GetArray(input.GetType());
        Assert.All(output, Assert.Null);
    }

    [Fact]
    public void ItIsPossibleToHaveBinaryArrayRecordsHaveAnElementTypeOfArrayWithoutBeingMarkedAsJagged()
    {
        int[][][] input = new int[3][][];
        for (int i = 0; i < input.Length; i++)
        {
            input[i] = new int[4][];

            for (int j = 0; j < input[i].Length; j++)
            {
                input[i][j] = [i, j, 0, 1, 2];
            }
        }

        byte[] serialized = Serialize(input).ToArray();
        const int ArrayTypeByteIndex =
            sizeof(byte) + sizeof(int) * 4 + // stream header
            sizeof(byte) + // SerializationRecordType.BinaryArray
            sizeof(int); // SerializationRecordId

        Assert.Equal((byte)BinaryArrayType.Jagged, serialized[ArrayTypeByteIndex]);

        // change the reported array type 
        serialized[ArrayTypeByteIndex] = (byte)BinaryArrayType.Single;

        var arrayRecord = (ArrayRecord)NrbfDecoder.Decode(new MemoryStream(serialized));

        Verify(input, arrayRecord);
        ArrayRecord?[] output = (ArrayRecord?[])arrayRecord.GetArray(input.GetType());
        for (int i = 0; i < input.Length; i++)
        {
            ArrayRecord[] firstLevel = (ArrayRecord[])output[i].GetArray(typeof(int[][]));

            for (int j = 0; j < input[i].Length; j++)
            {
                Assert.Equal(input[i][j], (int[])firstLevel[j].GetArray(typeof(int[])));
            }
        }
    }

    [Fact]
    public void CanReadSZJaggedArrayOfMDArrays()
    {
        int[][,] input = new int[7][,];
        for (int i = 0; i < input.Length; i++)
        {
            input[i] = new int[3, 3];

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
        ArrayRecord[] output = (ArrayRecord[])arrayRecord.GetArray(input.GetType());
        for (int i = 0; i < input.Length; i++)
        {
            Assert.Equal(input[i], output[i].GetArray(typeof(int[,])));
        }
    }

    [Fact]
    public void CanReadMDJaggedArrayOfSZArrays()
    {
        int[,][] input = new int[2,2][];
        input[0, 0] = [1, 2, 3];

        var arrayRecord = (ArrayRecord)NrbfDecoder.Decode(Serialize(input));

        Verify(input, arrayRecord);
        ArrayRecord[,] output = (ArrayRecord[,])arrayRecord.GetArray(input.GetType());
        Assert.Equal(input[0, 0], output[0, 0].GetArray(typeof(int[])));
        Assert.Null(output[0, 1]);
        Assert.Null(output[1, 0]);
        Assert.Null(output[1, 1]);
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

    static void MultiDimensionalArrayOfMultiDimensionalArrays<T>(Func<int, int, T> valueFactory)
    {
        T[,][,] input = new T[2, 2][,];
        for (int i = 0; i < input.GetLength(0); i++)
        {
            for (int j = 0; j < input.GetLength(1); j++)
            {
                T[,] contained = new T[i + 1, j + 1];
                for (int k = 0; k < contained.GetLength(0); k++)
                {
                    for (int l = 0; l < contained.GetLength(1); l++)
                    {
                        contained[k, l] = valueFactory(k, l);
                    }
                }

                input[i, j] = contained;
            }
        }

        var arrayRecord = (ArrayRecord)NrbfDecoder.Decode(Serialize(input));

        Verify(input, arrayRecord);

        ArrayRecord[,] output = (ArrayRecord[,])arrayRecord.GetArray(input.GetType());
        for (int i = 0; i < input.GetLength(0); i++)
        {
            for (int j = 0; j < input.GetLength(1); j++)
            {
                Assert.Equal(input[i, j], output[i, j].GetArray(typeof(T[,])));
            }
        }
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
        ArrayRecord[] output = (ArrayRecord[])arrayRecord.GetArray(input.GetType());
        for (int i = 0; i < input.Length; i++)
        {
            Assert.Equal(input[i], ((SZArrayRecord<string>)output[i]).GetArray());
        }
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
        ArrayRecord[] output = (ArrayRecord[])arrayRecord.GetArray(input.GetType());

        for (int i = 0; i < input.Length; i++)
        {
            SerializationRecord[] row = (SerializationRecord[])output[i].GetArray(typeof(object[]));
            for (int j = 0; j < input[i].Length; j++)
            {
                Assert.Equal(input[i][j], ((PrimitiveTypeRecord)row[j]).Value);
            }
        }
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
        ArrayRecord[] output = (ArrayRecord[])arrayRecord.GetArray(input.GetType());
        for (int i = 0; i < input.Length; i++)
        {
            SerializationRecord[] row = ((SZArrayRecord<SerializationRecord>)output[i]).GetArray();
            for (int j = 0; j < input[i].Length; j++)
            {
                Assert.Equal(input[i][j].SomeField, ((ClassRecord)row[j]!).GetInt32(nameof(ComplexType.SomeField)));
            }
        }
    }

    private static void Verify(Array input, ArrayRecord arrayRecord)
    {
        Assert.Equal(input.Rank, arrayRecord.Rank);
        Assert.True(arrayRecord.TypeName.GetElementType().IsArray); // true only for Jagged arrays
        Assert.Equal(input.GetType().FullName, arrayRecord.TypeName.FullName);
        Assert.Equal(input.GetType().GetAssemblyNameIncludingTypeForwards(), arrayRecord.TypeName.AssemblyName!.FullName);
    }
}
