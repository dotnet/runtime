// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using Xunit;

namespace PropagateThroughNativeTester
{
    public unsafe class Program
    {
        [DllImport("PropagateThroughNative_Native")]
        public static extern void InvokeCallbackCatchAndRethrow(delegate*unmanaged<void> callBack1, delegate*unmanaged<void> callBack2);

        [DllImport("PropagateThroughNative_Native")]
        public static extern void NativeThrow();

        [UnmanagedCallersOnly]
        static void CallPInvoke()
        {
            NativeThrow();
        }

        [UnmanagedCallersOnly]
        static void ThrowAndCatchException()
        {
            try
            {
                throw new Exception("This one is handled");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Caught {ex}");
            }
        }

        [Fact]
        public static void TestEntryPoint()
        {
             try
             {
		        InvokeCallbackCatchAndRethrow(&CallPInvoke, &ThrowAndCatchException);
             }
             catch (Exception ex)
             {
                 Console.WriteLine($"Caught {ex}");
             }
        }
    }
}
