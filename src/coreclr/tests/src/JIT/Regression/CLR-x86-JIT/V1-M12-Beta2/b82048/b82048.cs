// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

public struct AA
{
    public static sbyte m_suFwd3;

    public static int Main()
    {
        bool local9 = false;
        sbyte local11 = m_suFwd3;
        if (local9)
        {
            while (local9)
            {
                while (local9)
                    m_suFwd3 = 0;
            }
        }
        else
        {
            while (local9)
                throw new Exception();
            return 100;
        }
        try
        {
        }
        finally
        {
            if (local9)
                throw new IndexOutOfRangeException();
        }
        return 102;
    }
}

