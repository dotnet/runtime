// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//


using System;
using System.Runtime.CompilerServices;
public class BringUpTest
{
    const int Pass = 100;
    const int Fail = -1;

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static int JTrueNeFP(float x)
    {
        int returnValue = -1;

        if (x != -1f)
        {
            if (x != 0f)
            {
                if (x != 1f)
                {
                    returnValue = 4;
                }
                returnValue = 3;
            }
            else returnValue = 2;
        }
        else returnValue = 1;


        return returnValue;
    }

    public static int Main()
    {
        int returnValue = Pass;

        if (JTrueNeFP(-1f) != 1) { Console.WriteLine("1"); returnValue = Fail; }
        if (JTrueNeFP(0f) != 2) { Console.WriteLine("2"); returnValue = Fail; }
        if (JTrueNeFP(1f) != 3) { Console.WriteLine("3"); returnValue = Fail; }

        return returnValue;
    }
}
