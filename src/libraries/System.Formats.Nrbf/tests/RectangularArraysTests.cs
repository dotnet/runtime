using System.Formats.Nrbf.Utils;
using System.IO;
using Xunit;

namespace System.Formats.Nrbf.Tests;

public class RectangularArraysTests : ReadTests
{
    [Theory]
    [InlineData(2, 3)]
    // [InlineData(2147483591 /* Array.MaxLength */, 2)] // uint.MaxValue elements
    public void CanReadRectangularArraysOfPrimitiveTypes_2D(int x, int y)
    {
        byte[,] array = new byte[x, y];
        for (int i = 0; i < array.GetLength(0); i++)
        {
            for (int j = 0; j < array.GetLength(1); j++)
            {
                array[i, j] = (byte)(i * j);
            }   
        }
        using FileStream stream = SerializeToFile(array);

        ArrayRecord arrayRecord = (ArrayRecord)NrbfDecoder.Decode(stream);

        Verify(array, arrayRecord);
        Assert.True(arrayRecord.TypeNameMatches(typeof(byte[,])));
        Assert.False(arrayRecord.TypeNameMatches(typeof(string[,])));
        Assert.Equal(array, arrayRecord.GetArray(typeof(byte[,])));
        Assert.Equal(2, arrayRecord.Rank);
    }

    [Fact]
    public void CanReadRectangularArraysOfStrings_2D()
    {
        string[,] array = new string[7, 4];
        for (int i = 0; i < array.GetLength(0); i++)
        {
            for (int j = 0; j < array.GetLength(1); j++)
            {
                array[i, j] = $"{i}, {j}";
            }
        }
        using MemoryStream stream = Serialize(array);

        ArrayRecord arrayRecord = (ArrayRecord)NrbfDecoder.Decode(stream);

        Verify(array, arrayRecord);
        Assert.True(arrayRecord.TypeNameMatches(typeof(string[,])));
        Assert.False(arrayRecord.TypeNameMatches(typeof(int[,])));
        Assert.Equal(array, arrayRecord.GetArray(typeof(string[,])));
    }

    [Fact]
    public void CanReadRectangularArraysOfObjects_2D()
    {
        object?[,] array = new object[6, 3];
        for (int i = 0; i < array.GetLength(0); i++)
        {
            array[i, 0] = i;
            array[i, 1] = $"{i}, 1";
            array[i, 2] = null;
        }
        using MemoryStream stream = Serialize(array);

        ArrayRecord arrayRecord = (ArrayRecord)NrbfDecoder.Decode(stream);

        Verify(array, arrayRecord);
        Assert.True(arrayRecord.TypeNameMatches(typeof(object[,])));
        Assert.False(arrayRecord.TypeNameMatches(typeof(int[,])));
        Assert.Equal(array, arrayRecord.GetArray(typeof(object[,])));
    }

    [Serializable]
    public class ComplexType2D
    {
        public int I, J;
    }

    [Fact]
    public void CanReadRectangularArraysOfComplexTypes_2D()
    {
        ComplexType2D[,] array = new ComplexType2D[3,7];
        for (int i = 0; i < array.GetLength(0); i++)
        {
            for (int j = 0; j < array.GetLength(1); j++)
            {
                array[i, j] = new() { I = i, J = j };
            }
        }
        using MemoryStream stream = Serialize(array);

        ArrayRecord arrayRecord = (ArrayRecord)NrbfDecoder.Decode(stream);

        Verify(array, arrayRecord);
        Assert.True(arrayRecord.TypeNameMatches(typeof(ComplexType2D[,])));
        Assert.False(arrayRecord.TypeNameMatches(typeof(int[,])));

        var inputEnumerator = array.GetEnumerator();
        foreach(ClassRecord classRecord in arrayRecord.GetArray(typeof(ComplexType2D[,])))
        {
            inputEnumerator.MoveNext();
            ComplexType2D current = (ComplexType2D)inputEnumerator.Current;

            Assert.Equal(current.I, classRecord.GetInt32(nameof(ComplexType2D.I)));
            Assert.Equal(current.J, classRecord.GetInt32(nameof(ComplexType2D.J)));
        }
    }

    [Fact]
    public void CanReadRectangularArraysOfPrimitiveTypes_3D()
    {
        int[,,] array = new int[2, 3, 4];
        for (int i = 0; i < array.GetLength(0); i++)
        {
            for (int j = 0; j < array.GetLength(1); j++)
            {
                for (int k = 0; k < array.GetLength(2); k++)
                {
                    array[i, j, k] = i * j * k;
                }
            }
        }
        using MemoryStream stream = Serialize(array);

        ArrayRecord arrayRecord = (ArrayRecord)NrbfDecoder.Decode(stream);

        Verify(array, arrayRecord);
        Assert.True(arrayRecord.TypeNameMatches(typeof(int[,,])));
        Assert.False(arrayRecord.TypeNameMatches(typeof(int[,])));
        Assert.False(arrayRecord.TypeNameMatches(typeof(string[,,])));
        Assert.Equal(array, arrayRecord.GetArray(typeof(int[,,])));
        Assert.Equal(3, arrayRecord.Rank);
    }

    [Fact]
    public void CanReadRectangularArraysOfStrings_3D()
    {
        string[,,] array = new string[9, 6, 3];
        for (int i = 0; i < array.GetLength(0); i++)
        {
            for (int j = 0; j < array.GetLength(1); j++)
            {
                for (int k = 0; k < array.GetLength(2); k++)
                {
                    array[i, j, k] = $"{i}, {j}, {k}";
                }
            }
        }
        using MemoryStream stream = Serialize(array);

        ArrayRecord arrayRecord = (ArrayRecord)NrbfDecoder.Decode(stream);

        Verify(array, arrayRecord);
        Assert.True(arrayRecord.TypeNameMatches(typeof(string[,,])));
        Assert.False(arrayRecord.TypeNameMatches(typeof(string[,])));
        Assert.False(arrayRecord.TypeNameMatches(typeof(int[,,])));
        Assert.Equal(array, arrayRecord.GetArray(typeof(string[,,])));
    }

    [Fact]
    public void CanReadRectangularArraysOfObjects_3D()
    {
        object?[,,] array = new object[6, 3, 1];
        for (int i = 0; i < array.GetLength(0); i++)
        {
            array[i, 0, 0] = i;
            array[i, 1, 0] = $"{i}, 1";
            array[i, 2, 0] = null;
        } 
        using MemoryStream stream = Serialize(array);

        ArrayRecord arrayRecord = (ArrayRecord)NrbfDecoder.Decode(stream);

        Verify(array, arrayRecord);
        Assert.True(arrayRecord.TypeNameMatches(typeof(object[,,])));
        Assert.False(arrayRecord.TypeNameMatches(typeof(object[,])));
        Assert.False(arrayRecord.TypeNameMatches(typeof(int[,,])));
        Assert.Equal(array, arrayRecord.GetArray(typeof(object[,,])));
    }

    [Serializable]
    public class ComplexType3D
    {
        public int I, J, K;
    }

    [Fact]
    public void CanReadRectangularArraysOfComplexTypes_3D()
    {
        ComplexType3D[,,] array = new ComplexType3D[3, 7, 11];
        for (int i = 0; i < array.GetLength(0); i++)
        {
            for (int j = 0; j < array.GetLength(1); j++)
            {
                for (int k = 0; k < array.GetLength(2); k++)
                {
                    array[i, j, k] = new() { I = i, J = j, K = k };
                }
            }
        }
        using MemoryStream stream = Serialize(array);

        ArrayRecord arrayRecord = (ArrayRecord)NrbfDecoder.Decode(stream);

        Verify(array, arrayRecord);
        Assert.True(arrayRecord.TypeNameMatches(typeof(ComplexType3D[,,])));
        Assert.False(arrayRecord.TypeNameMatches(typeof(ComplexType3D[,])));
        Assert.False(arrayRecord.TypeNameMatches(typeof(int[,,])));

        var inputEnumerator = array.GetEnumerator();
        foreach (ClassRecord classRecord in arrayRecord.GetArray(typeof(ComplexType3D[,,])))
        {
            inputEnumerator.MoveNext();
            ComplexType3D current = (ComplexType3D)inputEnumerator.Current;

            Assert.Equal(current.I, classRecord.GetInt32(nameof(ComplexType3D.I)));
            Assert.Equal(current.J, classRecord.GetInt32(nameof(ComplexType3D.J)));
            Assert.Equal(current.K, classRecord.GetInt32(nameof(ComplexType3D.K)));
        }
    }

    internal static void Verify(Array input, ArrayRecord arrayRecord)
    {
        Assert.Equal(input.Rank, arrayRecord.Lengths.Length);
        for (int i = 0; i < input.Rank; i++)
        {
            Assert.Equal(input.GetLength(i), arrayRecord.Lengths[i]);
        }
        Assert.Equal(input.GetType().FullName, arrayRecord.TypeName.FullName);
        Assert.Equal(input.GetType().GetAssemblyNameIncludingTypeForwards(), arrayRecord.TypeName.AssemblyName!.FullName);
    }
}
