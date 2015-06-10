// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
public struct CC
{
    static sbyte m_su;
    static byte[] m_asi;

    static int Main()
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
        bool local4 = true;
        while (local4)
        {
            do
            {
                byte local6 = m_asi[0];
                String local8 = "62";
            } while (new object[1] == new object[] { });
            if (local4)
                for (; (new bool[1])[0]; m_su++) { }
        }
    }
}
