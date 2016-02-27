// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Reflection;

public class Test
{
    public static int Main()
    {
        try
        {
            AssemblyName an = new AssemblyName("noname,PublicKeyToken=null");
            int expected = 0;

            if (an.GetPublicKeyToken() == null)
            {
                Console.WriteLine("!!!ERROR-001: Public key token unexpectedly null. Expected length: " + expected.ToString());
                Console.WriteLine("FAIL");
                return 98;
            }

            if (an.GetPublicKeyToken().Length != expected)
            {
                Console.WriteLine("!!!ERROR-002: Public key token length not as expected. Expected: " + expected.ToString() + ", Actual: " + an.GetPublicKeyToken().Length.ToString());
                Console.WriteLine("FAIL");
                return 99;
            }
        }
        catch (Exception e)
        {
            Console.WriteLine("!!!ERROR-XXX: Unexpected exception : " + e);
            Console.WriteLine("FAIL");
            return 101;
        }
        Console.WriteLine("Pass");
        return 100;
    }
}