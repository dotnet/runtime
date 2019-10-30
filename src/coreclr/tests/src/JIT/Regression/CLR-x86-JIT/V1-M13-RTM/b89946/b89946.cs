// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

using System;

public class AA
{
    static Array[,] m_ax;
    static bool m_bFlag;

    static void Static3(int param1)
    {
        if (m_bFlag)
            Main();
        else
            m_ax[param1, param1] = null;
    }

    static int Main()
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
