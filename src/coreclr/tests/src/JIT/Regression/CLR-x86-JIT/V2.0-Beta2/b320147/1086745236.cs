// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

using System;
using System.Collections;
using System.Runtime.InteropServices;

public struct AA
{
    public void Method1()
    {
        bool local1 = true;
        for (; local1; )
        {
            if (local1)
                break;
        }
        do
        {
            if (local1)
                break;
        }
        while (local1);
        return;
    }

}

[StructLayout(LayoutKind.Sequential)]
public class App
{
    static int Main()
    {
        try
        {
            Console.WriteLine("Testing AA::Method1");
            new AA().Method1();
        }
        catch (Exception x)
        {
            Console.WriteLine("Exception handled: " + x.ToString());
        }

        // JIT Stress test... if jitted it passes
        Console.WriteLine("Passed.");
        return 100;
    }
}
