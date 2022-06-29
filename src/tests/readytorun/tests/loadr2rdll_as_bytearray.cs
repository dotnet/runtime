// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using System.Reflection;
using System.IO;

class Program
{
    static int Main()
    {
        Assembly assembly1 = typeof(Program).Assembly;

        byte[] array = File.ReadAllBytes(assembly1.Location);
        Assembly assembly2 = Assembly.Load(array);

        if (assembly2.FullName != assembly1.FullName)
        {
            Console.WriteLine("names do not match");
            return 1;
        }

        if (Object.ReferenceEquals(assembly1, assembly2))
        {
            Console.WriteLine("did not load as a separate assembly");
            return 2;
        }
        return 100;
    }
}