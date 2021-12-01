// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using System;
using System.Threading.Tasks;

namespace SimpleConsole
{
    public class Test
    {
        public static int Main(string[] args)
        {
            Console.WriteLine ($"from pinvoke: {SimpleConsole.Test.print_line(100)}");
            return 0;
        }

        [DllImport("native-lib")]
        public static extern int print_line(int x);
    }
}
