using Xunit;

namespace System.Runtime.Serialization.BinaryFormat.Tests;

public class CustomOffsetArrays : ReadTests
{
    [Fact]
    public void CanReadSingleDimensionalArrayOfIntegersWithCustomOffset()
    {
        const int lowerBound = 1;
        Array input = Array.CreateInstance(typeof(int), lengths: [3], lowerBounds: [lowerBound]);
        for (int i = lowerBound; i < lowerBound + input.Length; i++)
        {
            input.SetValue(value: i, index: i); 
        }

        ArrayRecord arrayRecord = (ArrayRecord)PayloadReader.Read(Serialize(input));

        Assert.Equal(input, arrayRecord.GetArray(input.GetType()));
    }

    [Fact]
    public void CanReadRectangularArrayOfStringsWithCustomOffsets()
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

        ArrayRecord arrayRecord = (ArrayRecord)PayloadReader.Read(Serialize(input));

        Assert.Equal(input, arrayRecord.GetArray(input.GetType()));
    }

    [Serializable]
    public class ComplexType3D
    {
        public int I, J, K;
    }

    [Fact]
    public void CanReadRectangularArraysOfComplexTypes_3D()
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

        ArrayRecord arrayRecord = (ArrayRecord)PayloadReader.Read(Serialize(input));

        RectangularArraysTests.VerifyLength(input, arrayRecord);
        Array output = arrayRecord.GetArray(input.GetType());

        for (int i = 0; i < input.GetLength(0); i++)
        {
            for (int j = 0; j < input.GetLength(1); j++)
            {
                for (int k = 0; k < input.GetLength(2); k++)
                {
                    ComplexType3D expected = (ComplexType3D)input.GetValue(i + lowerBoundI, j + lowerBoundJ, k + lowerBoundK)!;
                    ClassRecord got = (ClassRecord)output.GetValue(i + lowerBoundI, j + lowerBoundJ, k + lowerBoundK)!;

                    Assert.Equal(expected.I, got.GetInt32(nameof(ComplexType3D.I)));
                    Assert.Equal(expected.J, got.GetInt32(nameof(ComplexType3D.J)));
                    Assert.Equal(expected.K, got.GetInt32(nameof(ComplexType3D.K)));
                }
            }
        }
    }

    [Fact]
    [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "Not supported, custom offsets will be soon removed.")]
    public void JaggedCustomOffset()
    {
        Array input = Array.CreateInstance(typeof(uint[]), [5], [1]);

        ArrayRecord arrayRecord = (ArrayRecord)PayloadReader.Read(Serialize(input));

        Array output = arrayRecord.GetArray(expectedArrayType: input.GetType());

        Assert.Equal(input, output);
    }
}
