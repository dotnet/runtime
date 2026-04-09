// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Reflection;
using Xunit;

public class ClosedStatic
{
    public int scale;

    private ClosedStatic(int scale)
    {
        this.scale = scale;
    }
    public static decimal getfunc(ClosedStatic prog,int constituent)
    {
        return new decimal(constituent/prog.scale);
    }

    [OuterLoop]
    [Fact]
    public static int TestEntryPoint()
    {
        int result = -1;
        
        int constituent = 3;
        ClosedStatic prog = new ClosedStatic(1);
        2.Equals(3);
        
        MethodInfo info = typeof(ClosedStatic).GetMethod("getfunc", BindingFlags.Static | BindingFlags.Public);
        
        //Tests closed delegates over static methods with return buffer
        Func<int, decimal> deepThought = (Func<int, decimal>)info.CreateDelegate(typeof(Func<int, decimal>), prog);

        var res1 = deepThought(constituent);
        var res2 = deepThought(constituent);

        if (decimal.Compare(res1, res2) == 0)
            return 100;

        return result;
        
    }
}
