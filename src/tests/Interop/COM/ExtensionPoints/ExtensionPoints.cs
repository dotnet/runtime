// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

using COM;

using TestLibrary;
using Xunit;

public class ExtensionPoints
{
    unsafe class MallocSpy : IMallocSpy
    {
        private int _called = 0;
        public int Called => _called;

        public virtual nuint PreAlloc(nuint cbRequest)
        {
            _called++;
            return cbRequest;
        }

        public virtual unsafe void* PostAlloc(void* pActual) => pActual;
        public virtual unsafe void* PreFree(void* pRequest, [MarshalAs(UnmanagedType.Bool)] bool fSpyed)
        {
            _called++;
            return pRequest;
        }

        public virtual void PostFree([MarshalAs(UnmanagedType.Bool)] bool fSpyed) { }
        public virtual unsafe nuint PreRealloc(void* pRequest, nuint cbRequest, void** ppNewRequest, [MarshalAs(UnmanagedType.Bool)] bool fSpyed) => cbRequest;
        public virtual unsafe void* PostRealloc(void* pActual, [MarshalAs(UnmanagedType.Bool)] bool fSpyed) => pActual;
        public virtual unsafe void* PreGetSize(void* pRequest, [MarshalAs(UnmanagedType.Bool)] bool fSpyed) => pRequest;
        public virtual nuint PostGetSize(nuint cbActual, [MarshalAs(UnmanagedType.Bool)] bool fSpyed) => cbActual;
        public virtual unsafe void* PreDidAlloc(void* pRequest, [MarshalAs(UnmanagedType.Bool)] bool fSpyed) => pRequest;
        public virtual unsafe int PostDidAlloc(void* pRequest, [MarshalAs(UnmanagedType.Bool)] bool fSpyed, int fActual) => fActual;
        public virtual void PreHeapMinimize() { }
        public virtual void PostHeapMinimize() { }
    }

    [Fact]
    public static unsafe void Validate_Managed_IMallocSpy()
    {
        Console.WriteLine($"Running {nameof(Validate_Managed_IMallocSpy)}...");
        var mallocSpy = new MallocSpy();
        int result = Ole32.CoRegisterMallocSpy(mallocSpy);
        Assert.Equal(0, result);
        try
        {
            var arr = new [] { "", "", "", "", null };

            // The goal of this test is to trigger paths in which CoTaskMemAlloc
            // will be implicitly used and validate that the registered managed
            // IMallocSpy can be called successful. The validation is for confirming
            // the transition to Preemptive mode was performed.
            //
            // Casting the function pointer to one in which an IL stub will be
            // used to marshal the string[].
            var fptr = (delegate*unmanaged<string[], int>)(delegate*unmanaged<char**, int>)&ArrayLen;
            int len = fptr(arr);
            Assert.Equal(arr.Length - 1, len);

            // Allocate 1 for the array, 1 for each non-null element, then double it for Free.
            Assert.Equal((1 + (arr.Length - 1)) * 2, mallocSpy.Called);
        }
        finally
        {
            Ole32.CoRevokeMallocSpy();
        }

        [UnmanagedCallersOnly]
        static int ArrayLen(char** ptr)
        {
            char** begin = ptr;
            while (*ptr != null)
                ptr++;
            return (int)(ptr - begin);
        }
    }
}