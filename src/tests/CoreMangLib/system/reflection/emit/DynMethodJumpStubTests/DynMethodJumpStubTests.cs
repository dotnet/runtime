// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Threading;
using Xunit;

public static class DynamicMethodJumpStubTests
{
    [Fact]
    public static void TestEntryPoint()
    {
        DynamicMethodJumpStubTest();
    }

    public static void DynamicMethodJumpStubTest()
    {
        if (!Environment.Is64BitProcess)
        {
            return;
        }

        // Reserve memory around framework libraries. This is just a best attempt, it typically doesn't help since the
        // precode allocator may have already committed pages it can allocate from, or it may commit reserved pages close to
        // framework libraries.
        ReserveMemoryAround(new Action(ExecutionContext.RestoreFlow).Method.MethodHandle);

        var dynamicMethodDelegates = new Action[64];
        for (int i = 0; i < dynamicMethodDelegates.Length; ++i)
        {
            DynamicMethod dynamicMethod = CreateDynamicMethod("DynMethod" + i);
            dynamicMethodDelegates[i] = (Action)dynamicMethod.CreateDelegate(typeof(Action));

            // Before compiling the dynamic method, reserve memory around its current entry point, which should be its
            // precode. Then, when compiling the method, there would be a good chance that the code will be located far from
            // the precode, forcing the use of a jump stub.
            ReserveMemoryAround(
                (RuntimeMethodHandle)
                typeof(DynamicMethod).InvokeMember(
                    "GetMethodDescriptor",
                    BindingFlags.Instance | BindingFlags.InvokeMethod | BindingFlags.NonPublic,
                    null,
                    dynamicMethod,
                    null));
        }

        // Call each dynamic method concurrently from several threads to validate jump stub usage
        int threadCount = 64;
        var barrier = new Barrier(threadCount);
        ThreadStart threadStart = () =>
        {
            var dynamicMethodDelegatesLocal = dynamicMethodDelegates;
            for (int i = 0; i < dynamicMethodDelegatesLocal.Length; ++i)
            {
                var dynamicMethodDelegate = dynamicMethodDelegatesLocal[i];
                barrier.SignalAndWait();
                dynamicMethodDelegate();
            }
        };
        var threads = new Thread[threadCount];
        for (int i = 0; i < threads.Length; ++i)
        {
            threads[i] = new Thread(threadStart);
            threads[i].IsBackground = true;
            threads[i].Start();
        }
        foreach (var t in threads)
            t.Join();

        // This test does not release reserved pages because they may have been committed by other components on the system
    }

    private static DynamicMethod CreateDynamicMethod(string name)
    {
        var dynamicMethod = new DynamicMethod(name, null, null);
        ILGenerator ilGenerator = dynamicMethod.GetILGenerator();
        ilGenerator.Emit(OpCodes.Ret);
        return dynamicMethod;
    }

    private const uint AllocationGranularity = (uint)64 << 10;
    private const ulong ReserveRangeRadius = (ulong)4 << 30; // reserve 4 GB before and after the base address

    private static void ReserveMemoryAround(RuntimeMethodHandle methodHandle)
    {
        ulong baseAddress = (ulong)methodHandle.Value.ToInt64();

        ulong low = baseAddress - ReserveRangeRadius;
        if (low > baseAddress)
        {
            low = ulong.MinValue;
        }
        else
        {
            low &= ~((ulong)AllocationGranularity - 1);
        }

        ulong high = baseAddress + ReserveRangeRadius;
        if (high < baseAddress)
        {
            high = ulong.MaxValue;
        }

        ulong address = low;
        while (address <= high)
        {
            VirtualAlloc(
                new UIntPtr(address),
                new UIntPtr(AllocationGranularity),
                AllocationType.RESERVE,
                MemoryProtection.NOACCESS);

            if (address + AllocationGranularity < address)
            {
                break;
            }
            address += AllocationGranularity;
        }
    }

    [Flags]
    private enum AllocationType : uint
    {
        COMMIT = 0x1000,
        RESERVE = 0x2000,
        RESET = 0x80000,
        LARGE_PAGES = 0x20000000,
        PHYSICAL = 0x400000,
        TOP_DOWN = 0x100000,
        WRITE_WATCH = 0x200000
    }

    [Flags]
    private enum MemoryProtection : uint
    {
        EXECUTE = 0x10,
        EXECUTE_READ = 0x20,
        EXECUTE_READWRITE = 0x40,
        EXECUTE_WRITECOPY = 0x80,
        NOACCESS = 0x01,
        READONLY = 0x02,
        READWRITE = 0x04,
        WRITECOPY = 0x08,
        GUARD_Modifierflag = 0x100,
        NOCACHE_Modifierflag = 0x200,
        WRITECOMBINE_Modifierflag = 0x400
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern UIntPtr VirtualAlloc(
        UIntPtr lpAddress,
        UIntPtr dwSize,
        AllocationType flAllocationType,
        MemoryProtection flProtect);
}
