// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

namespace GitHub_19910

{
    public class Program
    {
        public struct Bgr { public byte B; public byte G; public byte R; }

        public class BasicReadWriteBenchmark<T>
            where T : struct
        {

            // NOTE: This includes cost of stack alloc
            [MethodImpl(MethodImplOptions.NoInlining)]
            static public void ReadFromStack()
            {
                unsafe
                {
                    void* stackPtr = stackalloc byte[Unsafe.SizeOf<Bgr>()];

                    var value = Unsafe.Read<T>(stackPtr);
                }
            }

            // NOTE: This includes cost of stack alloc
            [MethodImpl(MethodImplOptions.NoInlining)]
            static public void WriteToStack()
            {
                unsafe
                {
                    void* stackPtr = stackalloc byte[Unsafe.SizeOf<Bgr>()];

                    T value = default(T);
                    Unsafe.Write<T>(stackPtr, value);
                }
            }
        }

        public class BasicReadWriteBenchmarkBgr : BasicReadWriteBenchmark<Bgr> { }

        [Fact]
        public static int TestEntryPoint()
        {
            try
            {
                BasicReadWriteBenchmarkBgr.ReadFromStack();
                BasicReadWriteBenchmarkBgr.WriteToStack();
            }
            catch (Exception e)
            {
                Console.WriteLine("Failed with Exception: " + e.Message);
                return -1;
            }
            return 100;
        }
    }
}
