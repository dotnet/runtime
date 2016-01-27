// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
