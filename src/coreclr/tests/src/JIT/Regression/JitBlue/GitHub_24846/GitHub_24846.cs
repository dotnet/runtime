// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.CompilerServices;

public class GitHub_24846
{
    public static void Test(byte[] destination, byte[] source)
    {
        Unsafe.CopyBlockUnaligned(ref destination[0], ref source[0], 0);
    }

    public static int Main(string[] args)
    {
        int returnVal = 100;
        var destination = new byte[1];
        var source = new byte[1];
        try
        {
            Test(destination, source);
        }
        catch (Exception e)
        {
            Console.WriteLine("FAILED: " + e.Message);
            returnVal = -1;
        }
        bool caught = false;
        try
        {
            Test(destination, null);
        }
        catch (NullReferenceException e)
        {
            caught = true;
        }
        catch (Exception e)
        {
            Console.WriteLine("FAILED: Wrong Exception " + e.Message);
            returnVal = -1;
        }
        if (!caught)
        {
            Console.WriteLine("FAILED: null destination didn't throw");
            returnVal = -1;
        }
        caught = false;
        try
        {
            Test(null, source);
        }
        catch (NullReferenceException e)
        {
            caught = true;
        }
        catch (Exception e)
        {
            Console.WriteLine("FAILED: Wrong Exception " + e.Message);
            returnVal = -1;
        }
        if (!caught)
        {
            Console.WriteLine("FAILED: null source didn't throw");
            returnVal = -1;
        }
        return returnVal;
    }
}

