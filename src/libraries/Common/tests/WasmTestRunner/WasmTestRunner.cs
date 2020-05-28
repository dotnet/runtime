// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using System.Text;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

public class WasmTestRunner
{
    public static int Main(string[] args)
    {
        Console.Write("Args: ");
        foreach (string arg in args)
        {
            Console.Write(arg);
        }
        Console.WriteLine(".");

        return 0;
    }
}
