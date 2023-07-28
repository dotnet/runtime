// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using Xunit;
public class CC
{
    [Fact]
    public static int TestEntryPoint()
    {
        try
        {
            Main1();
            return 101;
        }
        catch (NullReferenceException)
        {
            return 100;
        }
    }
    static void Main1()
    {
        object b = null;
        while ((bool)b)
            return;
        while ((bool)b)
        {
            while (b == null)
            {
                do { } while ((bool)b);
                while ((bool)b) { }
                GC.Collect();
            }
        }
    }
}
