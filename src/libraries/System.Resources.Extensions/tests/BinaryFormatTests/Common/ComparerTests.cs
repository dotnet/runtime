// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using static BinaryFormatTests.FormatterTests.BinaryFormatterTests;

namespace System.Resources.Extensions.Tests.Common;

public abstract class ComparerTests<T> : SerializationTest<T> where T : ISerializer
{
    public static TheoryData<string, object> NullableComparersTestData => new()
    {
        { "NullableEqualityComparer`1", EqualityComparer<byte?>.Default },
        { "NullableEqualityComparer`1", EqualityComparer<int?>.Default },
        { "NullableEqualityComparer`1", EqualityComparer<float?>.Default },
        { "NullableEqualityComparer`1", EqualityComparer<Guid?>.Default }, // implements IEquatable<>
        { "ObjectEqualityComparer`1", EqualityComparer<MyStruct?>.Default },  // doesn't implement IEquatable<>
        { "ObjectEqualityComparer`1", EqualityComparer<DayOfWeek?>.Default },
        { "NullableComparer`1", Comparer<byte?>.Default },
        { "NullableComparer`1", Comparer<int?>.Default },
        { "NullableComparer`1", Comparer<float?>.Default },
        { "NullableComparer`1", Comparer<Guid?>.Default },
        { "ObjectComparer`1", Comparer<MyStruct?>.Default },
        { "ObjectComparer`1", Comparer<DayOfWeek?>.Default }
    };

    [Theory]
    [MemberData(nameof(NullableComparersTestData))]
    public void NullableComparers_Roundtrip(string expectedType, object obj)
    {
        object roundTrip = RoundTrip(obj);
        Assert.Equal(expectedType, roundTrip.GetType().Name);
    }
}
