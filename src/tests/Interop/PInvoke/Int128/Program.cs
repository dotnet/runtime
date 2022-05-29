// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

unsafe partial class Int128Native
{
    public static int Main(string[] args)
    {
        try
        {
            Console.WriteLine("Testing Int128");
            TestInt128();

            Console.WriteLine("Testing UInt128");
            TestUInt128();
        }
        catch (System.Exception ex)
        {
            Console.WriteLine(ex);
            return 0;
        }
        return 100;
    }
}
