// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Drawing;
using System.Runtime.Serialization;
using System.Runtime.Serialization.BinaryFormat;

namespace FormatTests.FormattedObject;

public class ArrayTests : Common.ArrayTests<FormattedObjectSerializer>
{
    public override void Roundtrip_ArrayContainingArrayAtNonZeroLowerBound()
    {
        Action action = base.Roundtrip_ArrayContainingArrayAtNonZeroLowerBound;
        action.Should().Throw<NotSupportedException>();
    }

    [Theory]
    [MemberData(nameof(StringArray_Parse_Data))]
    public void StringArray_Parse(string?[] strings)
    {
        System.Windows.Forms.BinaryFormat.BinaryFormattedObject format = new(Serialize(strings));
        var arrayRecord = (ArrayRecord<string>)format.RootRecord;
        arrayRecord.ToArray().Should().BeEquivalentTo(strings);
    }

    public static TheoryData<string?[]> StringArray_Parse_Data => new()
    {
        new string?[] { "one", "two" },
        new string?[] { "yes", "no", null },
        new string?[] { "same", "same", "same" }
    };

    [Theory]
    [MemberData(nameof(PrimitiveArray_Parse_Data))]
    public void PrimitiveArray_Parse(Array array)
    {
        System.Windows.Forms.BinaryFormat.BinaryFormattedObject format = new(Serialize(array));
        var arrayRecord = (ArrayRecord)format.RootRecord;
        arrayRecord.ToArray(expectedArrayType: array.GetType()).Should().BeEquivalentTo(array);
    }

    public static TheoryData<Array> PrimitiveArray_Parse_Data => new()
    {
        new int[] { 1, 2, 3 },
        new int[] { 1, 2, 1 },
        new float[] { 1.0f, float.NaN, float.PositiveInfinity },
        new DateTime[] { DateTime.MaxValue }
    };

    public static IEnumerable<object[]> Array_TestData => StringArray_Parse_Data.Concat(PrimitiveArray_Parse_Data);

    public static TheoryData<Array> Array_UnsupportedTestData => new()
    {
        new Point[] { new() },
        new object[] { new() },
    };

    public override void BinaryArray_InvalidRank_Positive(int rank, byte arrayType)
    {
        // BinaryFormatter doesn't throw on these.
        Action action = () => base.BinaryArray_InvalidRank_Positive(rank, arrayType);
        action.Should().Throw<SerializationException>();
    }
}
