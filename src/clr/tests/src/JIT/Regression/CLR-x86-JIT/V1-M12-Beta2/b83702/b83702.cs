// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
internal struct BB
{
    public static object m_xStatic1;
    public static byte m_bFwd10;

    public static void Static2(bool[] param1, object param2, BB[] param3, bool[] param4, char param6)
    {
        while (((bool)m_xStatic1)) break;
        do
        {
            for (m_bFwd10 *= (new byte[22u])[46];
            new uint[] { 34u, 4u, 30u } != m_xStatic1;
            param6 *= '\x06')
                ;
        } while (param2 != param3);
    }
    private static int Main()
    {
        try
        {
            Console.WriteLine("Testing BB::Static2");
            BB.Static2(null, null, null, null, '0');
            return 101;
        }
        catch (NullReferenceException)
        {
            return 100;
        }
    }
}
