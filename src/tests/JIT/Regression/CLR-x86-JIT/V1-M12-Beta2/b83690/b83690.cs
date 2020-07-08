// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
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
