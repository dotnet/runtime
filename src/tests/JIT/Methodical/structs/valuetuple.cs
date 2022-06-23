// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

// This test is extracted and simplified from the corefx tests for the ValueTuple class.
// It exposed an issue with assertion propagation not validating the assertions
// for a containing struct when a field lclVar is defined.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Xunit;

[StructLayout(LayoutKind.Auto)]
public struct ValueTuple<T1, T2, T3>
{
    public T1 Item1;
    public T2 Item2;
    public T3 Item3;
    public ValueTuple(T1 item1, T2 item2, T3 item3)
    {
        Item1 = item1;
        Item2 = item2;
        Item3 = item3;
    }

    public static ValueTuple<T1, T2, T3> Create(T1 item1, T2 item2, T3 item3) =>
        new ValueTuple<T1, T2, T3>(item1, item2, item3);

    public override bool Equals(object obj)
    {
        return obj is ValueTuple<T1, T2, T3> && Equals((ValueTuple<T1, T2, T3>)obj);
    }

    public bool Equals(ValueTuple<T1, T2, T3> other)
    {
        return Item1.Equals(other.Item1) && Item2.Equals(other.Item2) && Item3.Equals(other.Item3);
    }
    public override int GetHashCode()
    {
        return 0;
    }
}

public static class TupleExtensions
{
    public static ValueTuple<T1, T2, T3>
        ToValueTuple<T1, T2, T3>(
            this Tuple<T1, T2, T3> value)
    {
        return ValueTuple<T1, T2, T3>.Create(value.Item1, value.Item2, value.Item3);
    }
    public static Tuple<T1, T2, T3>
        ToTuple<T1, T2, T3>(
            this ValueTuple<T1, T2, T3> value)
    {
        return Tuple.Create(value.Item1, value.Item2, value.Item3);
    }
}

public class StructOptsTest
{
    const int Pass = 100;
    const int Fail = -1;

    public static int ConvertToRef3()
    {
        var refTuple = Tuple.Create(-1, -1, -1);
        var valueTuple = ValueTuple<int,int,int>.Create(1, 2, 3);

        refTuple = valueTuple.ToTuple();
        if (!String.Equals("(1, 2, 3)", refTuple.ToString()))
        {
            Console.WriteLine("Expected (1, 2, 3); got " + refTuple.ToString());
            return Fail;
        }
        return Pass;
    }

    [Fact]
    public static int TestEntryPoint()
    {
        int returnVal = Fail;
        try
        {
            returnVal = ConvertToRef3();
        }
        catch (Exception e)
        {
            Console.WriteLine("Unexpected exception " + e.Message);
            returnVal = Fail;
        }
        return returnVal;
    }
}
