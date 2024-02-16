// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using Xunit;

public struct Data
{
    public long Int;
    public object Obj;
}

public class Test11611
{    
    static bool fail = false;

    public static void handle(Data data, long value)
    {
        Console.WriteLine("handle( ( i:{0}, o:{1} ), {2} )", data.Int, data.Obj, value);

        if (data.Int != 0)
        {
            fail = true;
        }

        if (data.Obj != null)
        {
            fail = true;
        }

        data.Int = value;
        data.Obj = value;
    }

    [Fact]
    public static int TestEntryPoint()
    {
        Action<Data, long> handler = handle;
        handler += handle;

        var data = new Data();
        handler(data, 123);
        
        return fail ? -1 : 100;
    }
}
