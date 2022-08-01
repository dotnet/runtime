// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

namespace Demo
{
    internal static partial class NativeExportsNE
    {
        public const string NativeExportsNE_Binary = "Microsoft.Interop.Tests." + nameof(NativeExportsNE);

        [LibraryImport(NativeExportsNE_Binary, EntryPoint = "sumi")]
        public static partial int Sum(int a, int b);

        [LibraryImport(NativeExportsNE_Binary, EntryPoint = "sumouti")]
        public static partial void Sum(int a, int b, out int c);

        [LibraryImport(NativeExportsNE_Binary, EntryPoint = "sumrefi")]
        public static partial void Sum(int a, ref int b);
    }

    internal static class Program
    {
        public static void Main(string[] args)
        {
            int a = 12;
            int b = 13;
            int c = NativeExportsNE.Sum(a, b);
            Console.WriteLine($"{a} + {b} = {c}");

            NativeExportsNE.Sum(a, b, out c);
            Console.WriteLine($"{a} + {b} = {c}");

            c = b;
            NativeExportsNE.Sum(a, ref c);
            Console.WriteLine($"{a} + {b} = {c}");
        }
    }
}
