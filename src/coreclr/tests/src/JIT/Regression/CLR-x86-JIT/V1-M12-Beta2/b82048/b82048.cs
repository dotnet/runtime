// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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

