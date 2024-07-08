// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

namespace WindowsSpecific
{
    public static partial class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            Console.WriteLine(string.Join(Environment.NewLine, args));
            Console.WriteLine(RuntimeInformation.FrameworkDescription);

            if (args.Length == 0)
                return;

            switch (args[0])
            {
                case "long_path":
                    LongPath(args[1]);
                    break;
                case "compat_shims":
                    CompatShims();
                    break;
                default:
                    break;
            }
        }
    }
}
