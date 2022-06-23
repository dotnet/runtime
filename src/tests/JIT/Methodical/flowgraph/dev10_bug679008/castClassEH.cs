// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*
This is a potential security exploit. 
If the result of a cast is stored into a local the JIT incorrectly optimizes it such that the local gets set to the new object 
reference before throwing any exception.  Thus if the cast is in a try block, and the cast fails and the exception is caught, 
the code can still use the local as if the cast had succeeded.

Fix: Use an intermediate temporary, just like for other patterns, when the cast is inside a try block.

*/

using System;
using System.Runtime.CompilerServices;
using Xunit;

namespace Test_castClassEH_cs
{
public static class Repro
{
    private class Helper<T>
    {
        public Helper(T s) { t = s; }
        public T t;
    }
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int reinterpret_cast<DestType, SrcType>(SrcType s)
    {
        Helper<DestType> d = null;
        int ReturnVal = 101;
        try
        {
            Helper<SrcType> hs = new Helper<SrcType>(s);
            d = (Helper<DestType>)(object)hs;
        }
        catch (InvalidCastException)
        {
        }
        try
        {
            DestType r = d.t;
        }
        catch (System.NullReferenceException)
        {
            ReturnVal = 100;
        }

        return ReturnVal;
    }

    [Fact]
    public static int TestEntryPoint()
    {
        int exploit = reinterpret_cast<IntPtr, string>("Hello World!");
        Console.WriteLine(exploit);
        return exploit;
    }
}

}
