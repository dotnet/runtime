// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;

public class AA
{
    public static sbyte Static2()
    { return (new sbyte[1])[0]; }
    public static int Static4(sbyte param1)
    { return (((byte)9u) - AA.Static2()); }
    public static byte Static5()
    { return ((byte[])((Array)null))[AA.Static4(AA.Static2())]; }
    static void Main1()
    { Static5(); }
    public static int Main()
    {
        try
        {
            Main1();
        }
        catch (NullReferenceException)
        {
            return 100;
        }
        return 101;
    }
}
