// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using Xunit;
public class AA
{
    static void f(ref Array param)
    {
        try
        {

        }
        finally
        {
            for (int i = 0; i < 3; i++)
            {
            }
#pragma warning disable 1718
            while ((param != param))
#pragma warning restore 1718
            {
            }
        }
    }

    [Fact]
    public static int TestEntryPoint()
    {
        f(ref m_arr);
        Console.WriteLine("Passed.");
        return 100;
    }

    static Array m_arr;

}
