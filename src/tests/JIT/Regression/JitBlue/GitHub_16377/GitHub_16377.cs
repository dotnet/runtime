// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

public class GitHub_16377
{
    public static ValueTuple<T1, T2, T3, T4, T5, T6, T7, TRest> CreateLong<T1, T2, T3, T4, T5, T6, T7, TRest>(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6, T7 item7, TRest rest) where TRest : struct
    {
        return new ValueTuple<T1, T2, T3, T4, T5, T6, T7, TRest>(item1, item2, item3, item4, item5, item6, item7, rest);
    }
    
    public static Tuple<T1, T2, T3, T4, T5, T6, T7, TRest> CreateLongRef<T1, T2, T3, T4, T5, T6, T7, TRest>(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6, T7 item7, TRest rest)
    {
        return new Tuple<T1, T2, T3, T4, T5, T6, T7, TRest>(item1, item2, item3, item4, item5, item6, item7, rest);
    }

    internal static void AssertEqual(string s1, string s2)
    {
        if (!s1.Equals(s2))
        {
            throw new Exception();
        }
    }

    internal static void Test()
    {
        {
            var vtWithNull = CreateLong(1, 2, 3, 4, 5, 6, 7, new ValueTuple<string>(null));
            var tupleWithNull = CreateLongRef(1, 2, 3, 4, 5, 6, 7, new Tuple<string>(null));
            AssertEqual("(1, 2, 3, 4, 5, 6, 7, )", vtWithNull.ToString());
            AssertEqual(tupleWithNull.ToString(), vtWithNull.ToString());
        }
        
        {
            var vtWithNull = CreateLong(1, 2, 3, 4, 5, 6, 7, new ValueTuple<string>(null));
            var tupleWithNull = CreateLongRef(1, 2, 3, 4, 5, 6, 7, new Tuple<string>(null));
            AssertEqual("(1, 2, 3, 4, 5, 6, 7, )", vtWithNull.ToString());
            AssertEqual(tupleWithNull.ToString(), vtWithNull.ToString());
        }
        
        {
            var vtWithNull = CreateLong(1, 2, 3, 4, 5, 6, 7, new ValueTuple<string, string>(null, null));
            var tupleWithNull = CreateLongRef(1, 2, 3, 4, 5, 6, 7, new Tuple<string, string>(null, null));
            AssertEqual("(1, 2, 3, 4, 5, 6, 7, , )", vtWithNull.ToString());
            AssertEqual(tupleWithNull.ToString(), vtWithNull.ToString());
        }
        
        {
            var vtWithNull = CreateLong(1, 2, 3, 4, 5, 6, 7, new ValueTuple<string, string, string>(null, null, null));
            var tupleWithNull = CreateLongRef(1, 2, 3, 4, 5, 6, 7, new Tuple<string, string, string>(null, null, null));
            AssertEqual("(1, 2, 3, 4, 5, 6, 7, , , )", vtWithNull.ToString());
            AssertEqual(tupleWithNull.ToString(), vtWithNull.ToString());
        }
        
        {
            var vtWithNull = CreateLong(1, 2, 3, 4, 5, 6, 7, new ValueTuple<string, string, string, string>(null, null, null, null));
            var tupleWithNull = CreateLongRef(1, 2, 3, 4, 5, 6, 7, new Tuple<string, string, string, string>(null, null, null, null));
            AssertEqual("(1, 2, 3, 4, 5, 6, 7, , , , )", vtWithNull.ToString());
            AssertEqual(tupleWithNull.ToString(), vtWithNull.ToString());
        }
        
        {
            var vtWithNull = CreateLong(1, 2, 3, 4, 5, 6, 7, new ValueTuple<string, string, string, string, string>(null, null, null, null, null));
            var tupleWithNull = CreateLongRef(1, 2, 3, 4, 5, 6, 7, new Tuple<string, string, string, string, string>(null, null, null, null, null));
            AssertEqual("(1, 2, 3, 4, 5, 6, 7, , , , , )", vtWithNull.ToString());
            AssertEqual(tupleWithNull.ToString(), vtWithNull.ToString());
        }
        
        {
            var vtWithNull = CreateLong(1, 2, 3, 4, 5, 6, 7, new ValueTuple<string, string, string, string, string, string>(null, null, null, null, null, null));
            var tupleWithNull = CreateLongRef(1, 2, 3, 4, 5, 6, 7, new Tuple<string, string, string, string, string, string>(null, null, null, null, null, null));
            AssertEqual("(1, 2, 3, 4, 5, 6, 7, , , , , , )", vtWithNull.ToString());
            AssertEqual(tupleWithNull.ToString(), vtWithNull.ToString());
        }
        
        {
            var vtWithNull = CreateLong(1, 2, 3, 4, 5, 6, 7, new ValueTuple<string, string, string, string, string, string, string>(null, null, null, null, null, null, null));
            var tupleWithNull = CreateLongRef(1, 2, 3, 4, 5, 6, 7, new Tuple<string, string, string, string, string, string, string>(null, null, null, null, null, null, null));
            AssertEqual("(1, 2, 3, 4, 5, 6, 7, , , , , , , )", vtWithNull.ToString());
            AssertEqual(tupleWithNull.ToString(), vtWithNull.ToString());
        }
    }
    
    [Fact]
    public static int TestEntryPoint()
    {
        int result = 0;
        try
        {
            Test();
            result = 100;
        }
        catch (Exception)
        {
            result = -1;
        }

        return result;
    }
}
