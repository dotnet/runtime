using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Demo
{
    partial class Kernel32
    {
        [GeneratedDllImport(nameof(Kernel32), EntryPoint = "QueryPerformanceCounter")]
        public static partial int Method(ref long t);
    }

    unsafe class Program
    {
        static void Main(string[] args)
        {
            var ts = (long)0;
            int suc = Kernel32.Method(ref ts);
            Console.WriteLine($"{suc}: 0x{ts:x}");
        }
    }
}
