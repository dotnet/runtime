// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
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
