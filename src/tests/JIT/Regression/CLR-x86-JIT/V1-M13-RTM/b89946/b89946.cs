// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using Xunit;

public class AA
{
    static Array[,] m_ax;
    static bool m_bFlag;

    static void Static3(int param1)
    {
        if (m_bFlag)
            TestEntryPoint();
        else
            m_ax[param1, param1] = null;
    }

    [Fact]
    public static int TestEntryPoint()
    {
        try
        {
            Static3(0);
            return 101;
        }
        catch (NullReferenceException)
        {
            return 100;
        }
    }
}
