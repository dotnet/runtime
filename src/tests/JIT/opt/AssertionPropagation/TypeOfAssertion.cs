// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//Unit test for typeof assertion.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Xunit;

public class Sample10
{
    private static int s_s = 1;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static T GetMetadataValue<T>(IDictionary<string, object> metadata)
    {
        return metadata.TryGetValue("flag", out object value) && value is T result ? result : default;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int CountFlags(IEnumerable<IDictionary<string, object>> records)
    {
        // ARM64-NOT: CORINFO_HELP_UNBOX
        // X64-NOT: CORINFO_HELP_UNBOX
        int count = 0;
        foreach (var record in records)
        {
            if (GetMetadataValue<bool>(record))
                count++;
        }
        return count;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool CastAfterMutation(ref object value, object replacement)
    {
        if (value is bool)
        {
            value = replacement;
            return (bool)value;
        }
        return false;
    }

    [Fact]
    public static void CapturedTypeChecks()
    {
        var records = new List<IDictionary<string, object>>();
        foreach (object value in new object[] { true, false, null, "text", 1 })
            records.Add(new Dictionary<string, object> { ["flag"] = value });
        records.Add(new Dictionary<string, object>());
        Assert.Equal(1, CountFlags(records));

        object boxed = true;
        Assert.False(CastAfterMutation(ref boxed, false));
        Assert.Throws<InvalidCastException>(() => CastAfterMutation(ref boxed, "text"));
        boxed = true;
        Assert.Throws<NullReferenceException>(() => CastAfterMutation(ref boxed, null));
    }

    [System.Runtime.CompilerServices.MethodImplAttribute(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    private static int AddOne(int i)
    {
        return i + 1;
    }

    [System.Runtime.CompilerServices.MethodImplAttribute(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    private static int func(object o)
    {
        if (o.GetType() == typeof(int))
        {
            return AddOne((int)o);
        }
        return 0;
    }

    [Fact]
    public static int TestEntryPoint()
    {
        try
        {
            int result = (func(s_s));
            if (result == 2)
            {
                Console.WriteLine("Result:" + result);
                Console.WriteLine("Passed");
                return 100;
            }
            else
            {
                Console.WriteLine("Result:" + result);
                Console.WriteLine("Failed");
                return 101;
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            return 666;
        }
    }
}
