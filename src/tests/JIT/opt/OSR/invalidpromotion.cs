// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Xunit;

interface IFoo 
{
    public Span<object> AsSpan();
}

public struct ObjectSequence1 : IFoo
{
    public object Value1;
    
    public Span<object> AsSpan()
    {
        return MemoryMarshal.CreateSpan(ref Value1, 1);
    }
}

public struct ObjectSequenceMany : IFoo
{
    public object[] _values;

    public Span<object> AsSpan()
    {
        return _values.AsSpan();
    }

    public ObjectSequenceMany(object[] x)
    {
        _values = x;
    }
}

public class InvalidPromotion
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool G<T>(int n) where T : IFoo
    {
        // OSR cannot safely promote values
        T values = default;

        if (values is ObjectSequenceMany)
        {
            values = (T)(object)new ObjectSequenceMany(new object[5]);
        }

        Span<object> indexedValues = values.AsSpan();

        // For a patchpoint here.
        for (int i = 0; i < n; i++)
        {
            indexedValues[i] = "foo";
        }

        if (values is ObjectSequence1)
        {
            return (indexedValues[0] == ((ObjectSequence1)(object)values).Value1);
        }
        
        return false;
    }

    [Fact]
    public static int TestEntryPoint()
    {
        return G<ObjectSequence1>(1) ? 100 : -1;
    }
}

    
