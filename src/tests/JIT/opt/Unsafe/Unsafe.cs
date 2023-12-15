// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

namespace CodeGenTests
{
    public class UnsafeTests
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        static byte UnsafeAsNarrowCast_Short(short value)
        {
            // X64-NOT: dword ptr
            return Unsafe.As<short, byte>(ref value);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static byte UnsafeAsNarrowCast_Int(int value)
        {
            // X64-NOT: dword ptr
            return Unsafe.As<int, byte>(ref value);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static byte UnsafeAsNarrowCast_Long(long value)
        {
            // X64-NOT: qword ptr
            return Unsafe.As<long, byte>(ref value);
        }

        [Fact]
        public static int TestEntryPoint()
        {
            if (UnsafeAsNarrowCast_Short(255) != 255)
                return 0;

            if (UnsafeAsNarrowCast_Int(255) != 255)
                return 0;

            if (UnsafeAsNarrowCast_Long(255) != 255)
                return 0;

            return 100;
        }
    }
}
