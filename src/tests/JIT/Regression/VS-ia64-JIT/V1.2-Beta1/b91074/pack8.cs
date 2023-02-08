// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using System.Runtime.InteropServices;
using Xunit;

[StructLayoutAttribute(LayoutKind.Sequential, Pack = 8)]
internal sealed class tagDBPROPSET
{
    public IntPtr rgProperties;
    public Int32 cProperties;
    public Guid guidPropertySet;

    internal tagDBPROPSET()
    {
    }

    internal tagDBPROPSET(int propertyCount, Guid propertySet)
    {
        cProperties = propertyCount;
        guidPropertySet = propertySet;
    }
}

public class a
{
    [Fact]
    static public int TestEntryPoint()
    {
        try
        {
            tagDBPROPSET p = new tagDBPROPSET();
            Console.WriteLine(p.rgProperties);
            Console.WriteLine(p.cProperties);
            Console.WriteLine(p.guidPropertySet);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            Console.WriteLine("FAILED");
            return 1;
        }
        Console.WriteLine("PASSED");
        return 100;
    }
}
