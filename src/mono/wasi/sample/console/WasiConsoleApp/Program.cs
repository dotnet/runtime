// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace WasiConsoleApp
{
    public class Program
    {
        public static int Main()
        {
            Console.WriteLine($"Hello from .NET at {DateTime.Now.ToLongTimeString()}");
            return 0;
        }
    }
}
