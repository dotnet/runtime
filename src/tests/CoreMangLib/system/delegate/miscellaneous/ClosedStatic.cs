// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Reflection;
using Xunit;

public class Program
{
    public int scale;

    public Program(int scale)
    {
        this.scale = scale;
    }
    public static decimal getfunc(Program prog,int constituent)
    {
        return new decimal(constituent/prog.scale);
    }

    [Fact]
    public static int TestEntryPoint()
    {
        int result = -1;
        
        int constituent = 3;
        Program prog = new Program(1);
        2.Equals(3);
        
        MethodInfo info = typeof(Program).GetMethod("getfunc", BindingFlags.Static | BindingFlags.Public);
        
        //Tests closed delegates over static methods with return buffer
        Func<int, decimal> deepThought = (Func<int, decimal>)info.CreateDelegate(typeof(Func<int, decimal>), prog);

        var res1 = deepThought(constituent);
        var res2 = deepThought(constituent);

        if (decimal.Compare(res1, res2) == 0)
            return 100;

        return result;
        
    }
}
