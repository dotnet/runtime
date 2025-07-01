// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Resources.Extensions.Tests.Common;

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
        Assert.Equal(nints, deserialized);
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
}
