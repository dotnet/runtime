using System.IO;
using Xunit;

namespace System.Formats.Nrbf.Tests;

public class CustomOffsetArrays : ReadTests
{
    [Fact]
    public void SingleDimensionalArrayOfIntegersWithCustomOffset_ThrowsNSE()
    {
        const int lowerBound = 1;
        Array input = Array.CreateInstance(typeof(int), lengths: [3], lowerBounds: [lowerBound]);
        for (int i = lowerBound; i < lowerBound + input.Length; i++)
        {
            input.SetValue(value: i, index: i); 
        }

        using MemoryStream stream = Serialize(input);

        Assert.Throws<NotSupportedException>(() => NrbfDecoder.Decode(stream));
    }

    [Fact]
    public void RectangularArrayOfStringsWithCustomOffsets_ThrowsNSE()
    {
        const int lowerBound = 10;
        Array input = Array.CreateInstance(typeof(string), lengths: [7, 5], lowerBounds: [lowerBound, lowerBound]);
        for (int i = lowerBound; i < lowerBound + input.GetLength(0); i++)
        {
            for (int j = lowerBound; j < lowerBound + input.GetLength(1); j++)
            {
                input.SetValue(value: $"{i}. {j}", index1: i, index2: j);
            }
        }

        using MemoryStream stream = Serialize(input);

        Assert.Throws<NotSupportedException>(() => NrbfDecoder.Decode(stream));
    }

    [Serializable]
    public class ComplexType3D
    {
        public int I, J, K;
    }

    [Fact]
    public void RectangularArraysOfComplexTypes_ThrowsNSE()
    {
        const int lowerBoundI = 1, lowerBoundJ = 2, lowerBoundK = 2;
        Array input = Array.CreateInstance(typeof(ComplexType3D), 
            lengths: [17, 5, 3], lowerBounds: [lowerBoundI, lowerBoundJ, lowerBoundK]);

        for (int i = 0; i < input.GetLength(0); i++)
        {
            for (int j = 0; j < input.GetLength(1); j++)
            {
                for (int k = 0; k < input.GetLength(2); k++)
                {
                    input.SetValue(
                        new ComplexType3D() { I = i, J = j, K = k },
                        i + lowerBoundI, j + lowerBoundJ, k + lowerBoundK);
                }
            }
        }

        using MemoryStream stream = Serialize(input);

        Assert.Throws<NotSupportedException>(() => NrbfDecoder.Decode(stream));
    }

    [Fact]
    [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "BinaryFormatter fails to serialize the input.")]
    public void JaggedCustomOffset_ThrowsNSE()
    {
        Array input = Array.CreateInstance(typeof(uint[]), [5], [1]);

        using MemoryStream stream = Serialize(input);

        Assert.Throws<NotSupportedException>(() => NrbfDecoder.Decode(stream));
    }
}
