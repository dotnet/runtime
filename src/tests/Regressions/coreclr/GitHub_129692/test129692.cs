// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

internal struct NestedGenericStruct<T>
{
    static readonly object StaticField = new object();
    public T InstanceField;

    public override int GetHashCode()
    {
        Assert.NotNull(StaticField);
        return -InstanceField!.GetHashCode();
    }
}

internal struct OuterGenericStruct<T>
{
    public T Nested;
}

public class Program
{
    [Fact]
    public static void TestEntryPoint()
    {
        const string str = "129692";
        var s = new OuterGenericStruct<NestedGenericStruct<string>>
        {
            Nested = new NestedGenericStruct<string>
            {
                InstanceField = str
            }
        };
        Assert.Equal(HashCode.Combine(typeof(OuterGenericStruct<NestedGenericStruct<string>>).TypeHandle.Value, -str.GetHashCode()), s.GetHashCode());
    }
}
