// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Xunit;

public sealed class Stackalloc
{
    private static int s_size = 33;

    [Fact]
    public static void TestEntryPoint()
    {
        if (MarshalToStackalloc() != 1 || StackallocFromArithmetic(8) != 7 || StackallocFromField() != 7)
        {
            throw new Exception("Stack allocation returned an unexpected result.");
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static unsafe int MarshalToStackalloc()
    {
        // The dynamic size is returned in w0. Reuse its register for the
        // stackalloc result instead of copying the size twice.
        // ARM64: {{(bl|blr).*Marshal:SizeOf}}
        // ARM64: mov {{w[0-9]+}}, w0
        // ARM64-NOT: mov {{x[0-9]+}}, x0
        // ARM64: tst
        NonBlittableStruct value = new NonBlittableStruct { I = 1, S = 42, B = true };
        byte* stackSpace = stackalloc byte[Marshal.SizeOf<NonBlittableStruct>()];
        Marshal.StructureToPtr(value, (IntPtr)stackSpace, false);
        return *(int*)stackSpace;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static unsafe int StackallocFromArithmetic(int length)
    {
        // The computed size should need just one move into the stackalloc result register.
        // ARM64: add {{w[0-9]+}}, {{w[0-9]+}}, #1
        // ARM64: mov {{w[0-9]+}}, w0
        // ARM64-NOT: mov {{x[0-9]+}}, x0
        // ARM64: tst
        int size = (length << 1) + 1;
        byte* stackSpace = stackalloc byte[size];
        WriteFirstByte(stackSpace);
        return *stackSpace;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static unsafe int StackallocFromField()
    {
        // Load the size directly into the register used for the result.
        // ARM64: ldr {{w[0-9]+}}, [{{x[0-9]+}}]
        // ARM64-NOT: mov {{x[0-9]+}}, x0
        // ARM64: tst
        int size = s_size;
        byte* stackSpace = stackalloc byte[size];
        WriteFirstByte(stackSpace);
        return *stackSpace;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static unsafe void WriteFirstByte(byte* pointer) => *pointer = 7;

    public struct NonBlittableStruct
    {
        public int I;
        public short S;

        [MarshalAs(UnmanagedType.U1)]
        public bool B;
    }
}
