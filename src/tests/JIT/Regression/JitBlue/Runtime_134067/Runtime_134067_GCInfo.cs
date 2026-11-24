// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Xunit;

public static unsafe class Runtime_134067_GCInfo
{
    [Fact]
    public static void TestEntryPoint()
    {
        C c = new();
        c.T.H = 1;
        InitLarge(c);
        Assert.Equal(0, c.T.H);

        SmallC small = new();
        small.T.H = 1;
        InitSmall(small);
        Assert.Equal(0, small.T.H);

        ByrefOwner owner = new();
        owner.Value.T.H = 1;
        InitByref(ref owner.Value);
        Assert.Equal(0, owner.Value.T.H);

        byte[] array = new byte[40_064];
        array[63] = 1;
        InitNegative(ref array[40_000]);
        Assert.Equal(0, array[63]);

        byte* buffer = (byte*)NativeMemory.Alloc(40_064);
        try
        {
            buffer[40_063] = 1;
            InitNative(buffer);
            Assert.Equal(0, buffer[40_063]);
        }
        finally
        {
            NativeMemory.Free(buffer);
        }

        Assert.Equal(0, InitStack());
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static void InitLarge(C c)
    {
        // ARM64: ; gcrRegs +{{\[}}[[BASE:x[0-9]+]]{{\]}}
        // ARM64-NOT: nogc
        // ARM64-NOT: ; gcrRegs -{{\[}}{{.*}}[[BASE]]{{.*}}{{\]}}
        // ARM64: mov [[ADDR:x[0-9]+]], #0xD1FFAB1E
        // ARM64-NEXT: add [[ADDR]], [[BASE]], [[ADDR]]
        // ARM64-NEXT: ; byrRegs +{{\[}}[[ADDR]]{{\]}}
        // ARM64-NOT: nogc
        // ARM64-NOT: ; byrRegs -{{\[}}{{.*}}[[ADDR]]{{.*}}{{\]}}
        // ARM64: stp {{.*}}, {{\[}}[[ADDR]]{{\]}}
        // ARM64-NOT: nogc
        // ARM64-NOT: ; byrRegs -{{\[}}{{.*}}[[ADDR]]{{.*}}{{\]}}
        // ARM64: stp {{.*}}, {{\[}}[[ADDR]]
        c.T = default;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static void InitSmall(SmallC c)
    {
        // ARM64: ; gcrRegs +{{\[}}[[BASE:x[0-9]+]]{{\]}}
        // ARM64-NOT: nogc
        // ARM64-NOT: ; gcrRegs -{{\[}}{{.*}}[[BASE]]{{.*}}{{\]}}
        // ARM64: add [[ADDR:x[0-9]+]], [[BASE]], #0xD1FFAB1E
        // ARM64-NEXT: ; byrRegs +{{\[}}[[ADDR]]{{\]}}
        // ARM64-NOT: nogc
        // ARM64-NOT: ; byrRegs -{{\[}}{{.*}}[[ADDR]]{{.*}}{{\]}}
        // ARM64: stp {{.*}}, {{\[}}[[ADDR]]{{\]}}
        // ARM64-NOT: nogc
        // ARM64-NOT: ; byrRegs -{{\[}}{{.*}}[[ADDR]]{{.*}}{{\]}}
        // ARM64: stp {{.*}}, {{\[}}[[ADDR]]
        c.T = default;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static void InitByref(ref PaddedPayload value)
    {
        // ARM64: ; byrRegs +{{\[}}[[BASE:x[0-9]+]]{{\]}}
        // ARM64-NOT: nogc
        // ARM64-NOT: ; byrRegs -{{\[}}{{.*}}[[BASE]]{{.*}}{{\]}}
        // ARM64: mov [[ADDR:x[0-9]+]], #0xD1FFAB1E
        // ARM64-NEXT: add [[ADDR]], [[BASE]], [[ADDR]]
        // ARM64-NEXT: ; byrRegs +{{\[}}[[ADDR]]{{\]}}
        // ARM64-NOT: nogc
        // ARM64-NOT: ; byrRegs -{{\[}}{{.*}}[[ADDR]]{{.*}}{{\]}}
        // ARM64: stp {{.*}}, {{\[}}[[ADDR]]{{\]}}
        // ARM64-NOT: nogc
        // ARM64-NOT: ; byrRegs -{{\[}}{{.*}}[[ADDR]]{{.*}}{{\]}}
        // ARM64: stp {{.*}}, {{\[}}[[ADDR]]
        value.T = default;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static void InitNegative(ref byte address)
    {
        // ARM64: ; byrRegs +{{\[}}[[BASE:x[0-9]+]]{{\]}}
        // ARM64-NOT: nogc
        // ARM64-NOT: ; byrRegs -{{\[}}{{.*}}[[BASE]]{{.*}}{{\]}}
        // ARM64: mov [[ADDR:x[0-9]+]], #0xD1FFAB1E
        // ARM64-NEXT: sub [[ADDR]], [[BASE]], [[ADDR]]
        // ARM64-NEXT: ; byrRegs +{{\[}}[[ADDR]]{{\]}}
        // ARM64-NOT: nogc
        // ARM64-NOT: ; byrRegs -{{\[}}{{.*}}[[ADDR]]{{.*}}{{\]}}
        // ARM64: stp {{.*}}, {{\[}}[[ADDR]]{{\]}}
        // ARM64-NOT: nogc
        // ARM64-NOT: ; byrRegs -{{\[}}{{.*}}[[ADDR]]{{.*}}{{\]}}
        // ARM64: stp {{.*}}, {{\[}}[[ADDR]]
        Unsafe.InitBlock(ref Unsafe.Subtract(ref address, 40_000), 0, 64);
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static void InitNative(byte* address)
    {
        // ARM64: mov [[ADDR:x[0-9]+]], #0xD1FFAB1E
        // ARM64-NEXT: add [[ADDR]], {{x[0-9]+}}, [[ADDR]]
        // ARM64-NOT: byrRegs +{{\[}}{{.*}}[[ADDR]]{{.*}}{{\]}}
        // ARM64-NOT: gcrRegs +{{\[}}{{.*}}[[ADDR]]{{.*}}{{\]}}
        // ARM64-NOT: nogc
        // ARM64: stp {{.*}}, {{\[}}[[ADDR]]{{\]}}
        // ARM64-NOT: byrRegs +{{\[}}{{.*}}[[ADDR]]{{.*}}{{\]}}
        // ARM64-NOT: gcrRegs +{{\[}}{{.*}}[[ADDR]]{{.*}}{{\]}}
        // ARM64-NOT: nogc
        // ARM64: stp {{.*}}, {{\[}}[[ADDR]]
        Unsafe.InitBlock(address + 40_000, 0, 64);
    }

    [SkipLocalsInit]
    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static long InitStack()
    {
        // ARM64: {{add|sub}} [[ADDR:x[0-9]+]], {{fp|sp}}, [[ADDR]]
        // ARM64-NOT: byrRegs +{{\[}}{{.*}}[[ADDR]]{{.*}}{{\]}}
        // ARM64-NOT: gcrRegs +{{\[}}{{.*}}[[ADDR]]{{.*}}{{\]}}
        // ARM64-NOT: nogc
        // ARM64: stp {{.*}}, {{\[}}[[ADDR]]{{\]}}
        // ARM64-NOT: byrRegs +{{\[}}{{.*}}[[ADDR]]{{.*}}{{\]}}
        // ARM64-NOT: gcrRegs +{{\[}}{{.*}}[[ADDR]]{{.*}}{{\]}}
        // ARM64-NOT: nogc
        // ARM64: stp {{.*}}, {{\[}}[[ADDR]]
        Unsafe.SkipInit(out StackPayload value);
        value.T = default;
        return ReadStack(ref value);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long ReadStack(ref StackPayload value) => value.T.H;

    [InlineArray(40_000)]
    private struct Pad
    {
        private byte _element0;
    }

    [InlineArray(1_024)]
    private struct SmallPad
    {
        private byte _element0;
    }

    private struct Payload
    {
        public long A, B, C, D, E, F, G, H;
    }

    private struct PaddedPayload
    {
        public Pad P;
        public Payload T;
    }

    private struct StackPayload
    {
        public Pad Before;
        public Payload T;
        public Pad After;
    }

    private sealed class C
    {
        public Pad P;
        public Payload T;
    }

    private sealed class SmallC
    {
        public SmallPad P;
        public Payload T;
    }

    private sealed class ByrefOwner
    {
        public PaddedPayload Value;
    }
}
