// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/*
This is a potential security exploit. 
If the result of a cast is stored into a local the JIT incorrectly optimizes it such that the local gets set to the new object 
reference before throwing any exception.  Thus if the cast is in a try block, and the cast fails and the exception is caught, 
the code can still use the local as if the cast had succeeded.

Fix: Use an intermediate temporary, just like for other patterns, when the cast is inside a try block.

*/

using System;
using System.Runtime.CompilerServices;

internal static class Repro
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

    private static int Main()
    {
        int exploit = reinterpret_cast<IntPtr, string>("Hello World!");
        Console.WriteLine(exploit);
        return exploit;
    }
}

