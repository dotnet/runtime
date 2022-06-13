// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

public class NegCollect
{
    public static int Main()
    {
        bool retVal = true;
        GCCollectionMode[] invalidInputs = { (GCCollectionMode)(GCCollectionMode.Default - 1), (GCCollectionMode)(GCCollectionMode.Aggressive + 1) };

        for (int i = 0; i < invalidInputs.Length; i++)
        {
            try
            {
                GC.Collect(2, invalidInputs[i]);
                retVal = false;
                Console.WriteLine("Invalid value for GC.Collect: {0}", invalidInputs[i]);
            }
            catch (ArgumentOutOfRangeException)
            {
            }
        }

        if (retVal)
        {
            Console.WriteLine("Test Passed");
            return 100;
        }

        Console.WriteLine("Test Failed");
        return 1;
    }
}
