// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using Xunit;

public class Program
{
    public virtual void VirtualMethod()
    {
    }

    public void NonVirtualMethod()
    {
    }

    [Fact]
    public static int TestEntryPoint()
    {
        Program p = new Program();

        Action d1 = p.VirtualMethod;
        Action d2 = p.VirtualMethod;

        if (!d1.Equals(d2))
        {
            Console.WriteLine("FAILED: d1.Equals(d2) is not true");
            return 200;
        }

        if (d1.GetHashCode() != d2.GetHashCode())
        {
            Console.WriteLine("FAILED: d1.GetHashCode() != d2.GetHashCode()");
            return 201;
        }

        Action d3 = p.NonVirtualMethod;
        Action d4 = p.NonVirtualMethod;

        if (!d3.Equals(d4))
        {
            Console.WriteLine("FAILED: d3.Equals(d4) is not true");
            return 202;
        }

        if (d3.GetHashCode() != d4.GetHashCode())
        {
            Console.WriteLine("FAILED: d3.GetHashCode() != d4.GetHashCode()");
            return 203;
        }

        return 100;
    }
}
