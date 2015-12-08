// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Reflection;

public class repro1
{
    public static int Main()
    {
        Type t = Type.GetType("System.Runtime.InteropServices.Marshal");
        Type[] ts = { };

        MethodInfo mi = t.GetMethod("ReleaseThreadCache", ts);

        Console.WriteLine("Processing method : " + mi.DeclaringType.Name + "  :  " + mi.Name);

        Console.WriteLine("\nInvoking...");

        mi.Invoke(null, null);

        Console.WriteLine("DONE");
        return 100;
    }
}