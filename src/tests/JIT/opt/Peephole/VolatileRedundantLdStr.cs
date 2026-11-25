// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Threading;
using Xunit;

namespace CodeGenTests
{
    public static class VolatileRedundantLdStr
    {
        // A volatile LDR must still be present even when part of a redundant STR+LDR pair.
        [MethodImpl(MethodImplOptions.NoInlining)]
        static float StoreThenVolatileRead(ref float location, float value)
        {
            // ARM64:      str [[REG:s[0-9]+]], [x0]
            // ARM64-NEXT: ldr [[REG]], [x0]
            // ARM64-NEXT: dmb ishld
            location = value;
            return Volatile.Read(ref location);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static void Escape(ref long location) { }

        // A volatile LDR must still be present even when part of a redundant LDR+LDR pair.
        [MethodImpl(MethodImplOptions.NoInlining)]
        static long ReadThenVolatileRead(long value)
        {
            // ARM64:      ldr {{x[0-9]+}}, [fp, [[OFFS:#0x[0-9A-F]+]]]
            // ARM64-NEXT: ldr {{x[0-9]+}}, [fp, [[OFFS]]]
            // ARM64-NEXT: dmb ishld
            long local = value;
            Escape(ref local);
            long first = local;
            long second = Volatile.Read(ref local);
            return first + second;
        }

        [Fact]
        public static void TestEntryPoint()
        {
            float location = 0;
            Assert.Equal(1.5f, StoreThenVolatileRead(ref location, 1.5f));
            Assert.Equal(1.5f, location);
            Assert.Equal(6, ReadThenVolatileRead(3));
        }
    }
}
