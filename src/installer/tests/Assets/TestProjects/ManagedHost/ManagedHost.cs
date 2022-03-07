// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

namespace ComClient
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                throw new Exception("Invalid number of arguments passed");
            }

            switch (args[0])
            {
                case "ijwhost":
                {
                    // args: ... <ijw_library_path> <entry_point>
                    if (args.Length != 3)
                    {
                        throw new Exception("Invalid number of arguments passed");
                    }

                    IjwTest(args[1], args[2]);
                    break;
                }
                default:
                    throw new ArgumentException("Unknown scenario");
            }
        }

        private static unsafe void IjwTest(string libraryPath, string entryPointName)
        {
            Console.WriteLine($"Invoking {entryPointName} in '{libraryPath}'");

            IntPtr library = NativeLibrary.Load(libraryPath);
            IntPtr export = NativeLibrary.GetExport(library, entryPointName);

            // Test is assuming __cdecl, no argument,s and void return for simplicity
            delegate* unmanaged[Cdecl]<void> entryPoint = (delegate* unmanaged[Cdecl]<void>)export;
            entryPoint();
        }
    }
}