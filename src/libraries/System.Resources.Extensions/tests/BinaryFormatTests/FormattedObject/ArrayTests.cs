// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Drawing;
using System.Linq;
using System.Resources.Extensions.BinaryFormat;
using System.Formats.Nrbf;

namespace System.Resources.Extensions.Tests.FormattedObject;

public class ArrayTests : Common.ArrayTests<FormattedObjectSerializer>
{
    public override void Roundtrip_ArrayContainingArrayAtNonZeroLowerBound()
    {
        Assert.Throws<NotSupportedException>(base.Roundtrip_ArrayContainingArrayAtNonZeroLowerBound);
    }

    [Theory]
    [MemberData(nameof(StringArray_Parse_Data))]
    public void StringArray_Parse(string?[] strings)
    {
        BinaryFormattedObject format = new(Serialize(strings));
        var arrayRecord = (SZArrayRecord<string>)format.RootRecord;
        Assert.Equal(strings, arrayRecord.GetArray());
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
        BinaryFormattedObject format = new(Serialize(array));
        var arrayRecord = (ArrayRecord)format.RootRecord;
        Assert.Equal(array, arrayRecord.GetArray(expectedArrayType: array.GetType()));
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
}
