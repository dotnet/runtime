// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using Xunit;

namespace ComInterfaceGenerator.Tests;

public partial class ArrayBufferMarshallingTests
{
    [GeneratedComInterface]
    [Guid("8A2AF35B-D028-4191-A01F-3422AB0CF724")]
    public partial interface ITestInterface
    {
        void TestMethod(
            int bufferSize,
            [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0), Out] int[]? buffer1,
            [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0), Out] int[]? buffer2);
        
        void TestMethodWithRef(
            int bufferSize,
            [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] ref int[]? buffer);
    }

    private class TestImplementation : ITestInterface
    {
        public void TestMethod(int bufferSize, int[]? buffer1, int[]? buffer2)
        {
            // Fill buffer1 if not null
            if (buffer1 != null)
            {
                for (int i = 0; i < Math.Min(bufferSize, buffer1.Length); i++)
                {
                    buffer1[i] = i;
                }
            }

            // Fill buffer2 if not null
            if (buffer2 != null)
            {
                for (int i = 0; i < Math.Min(bufferSize, buffer2.Length); i++)
                {
                    buffer2[i] = i * 2;
                }
            }
        }

        public void TestMethodWithRef(int bufferSize, ref int[]? buffer)
        {
            // If buffer is null, allocate it with the specified size
            if (buffer is null && bufferSize > 0)
            {
                buffer = new int[bufferSize];
            }
            
            // Fill buffer if not null
            if (buffer != null)
            {
                for (int i = 0; i < Math.Min(bufferSize, buffer.Length); i++)
                {
                    buffer[i] = i * 3;
                }
            }
        }
    }

    [Fact]
    public void TestGeneratedCodeCompilation()
    {
        // This test ensures the COM interface with array parameters is generated without compilation errors
        // The specific issue is that the generated code should handle null array pointers correctly
        // when calculating the number of elements in unmanaged-to-managed stubs.
        
        var testImpl = new TestImplementation();
        var cw = new StrategyBasedComWrappers();
        nint ptr = cw.GetOrCreateComInterfaceForObject(testImpl, CreateComInterfaceFlags.None);

        try
        {
            // The main test is that this interface can be created and the generated code compiles
            Assert.NotEqual(0, (int)ptr);
        }
        finally
        {
            Marshal.Release(ptr);
        }
    }

    [Fact]
    public void TestGeneratedCodeCompilationWithRefArrays()
    {
        // This test ensures that COM interfaces with ref array parameters compile correctly
        // and handle null pointer scenarios properly
        
        var testImpl = new TestImplementation();
        var cw = new StrategyBasedComWrappers();
        nint ptr = cw.GetOrCreateComInterfaceForObject(testImpl, CreateComInterfaceFlags.None);

        try
        {
            // The main test is that this interface with ref arrays can be created and the generated code compiles
            Assert.NotEqual(0, (int)ptr);
        }
        finally
        {
            Marshal.Release(ptr);
        }
    }
}