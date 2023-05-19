// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

namespace CodeGenTests
{
    public class IntCast
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        static long Cast_Short_To_Long(short value)
        {
            // X64-NOT: cdqe
            return (long)value;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static long Cast_Short_To_Long_Add(short value1, short value2)
        {
            // X64:     movsx
            // X64-NOT: cdqe
            // X64:     movsx
            // X64-NOT: movsxd

            return (long)value1 + (long)value2;
        }

        [Fact]
        public static int TestEntryPoint()
        {
            if (Cast_Short_To_Long(Int16.MaxValue) != 32767)
                return 0;

            if (Cast_Short_To_Long_Add(Int16.MaxValue, Int16.MaxValue) != 65534)
                return 0;

            return 100;
        }
    }
}