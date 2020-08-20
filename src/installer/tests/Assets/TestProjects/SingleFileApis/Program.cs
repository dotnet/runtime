// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;

namespace StandaloneApp
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            try
            {
                #pragma warning disable SYSLIB0012
                Debugger.Launch();
                _ = typeof(Program).Assembly.CodeBase;
                #pragma warning restore SYSLIB0012
            }
            catch
            {
                Console.WriteLine("CodeBase PlatformNotSupported");
                return;
            }

            Console.WriteLine("test failure");
        }
    }
}
