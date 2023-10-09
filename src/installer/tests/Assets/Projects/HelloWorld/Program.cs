// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

namespace HelloWorld
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            Console.WriteLine(string.Join(Environment.NewLine, args));
            Console.WriteLine(RuntimeInformation.FrameworkDescription);
        }
    }
}
