// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.CompilerServices;

namespace System.MathBenchmarks
{
    public static class DivideByConst
    {
        public static void Test()
        {
            Verify(ulong.MaxValue, long.MaxValue, uint.MaxValue, int.MaxValue);

            [MethodImpl(MethodImplOptions.NoInlining)]
            static void Verify(ulong u64, long i64, uint u32, int i32)
            {
                if (u64 / 7 != 0x2492492492492492) throw new Exception($"{u64:x}/7={u64 / 7:x}");
                if (i64 / 7 != 0x1249249249249249) throw new Exception($"{i64:x}/7={i64 / 7:x}");
                if (u32 / 7 != 0x24924924) throw new Exception($"{u32:x}/7={u32 / 7:x}");
                if (i32 / 7 != 0x12492492) throw new Exception($"{i32:x}/7={i32 / 7:x}");

                if (u64 / 14 != 0x1249249249249249) throw new Exception($"{u64:x}/14={u64 / 14:x}");
                if (i64 / 14 != 0x924924924924924) throw new Exception($"{i64:x}/14={i64 / 14:x}");
                if (u32 / 14 != 0x12492492) throw new Exception($"{u32:x}/14={u32 / 14:x}");
                if (i32 / 14 != 0x9249249) throw new Exception($"{i32:x}/14={i32 / 14:x}");

                if (u64 / 56 != 0x492492492492492) throw new Exception($"{u64:x}/56={u64 / 56:x}");
                if (i64 / 56 != 0x249249249249249) throw new Exception($"{i64:x}/56={i64 / 56:x}");
                if (u32 / 56 != 0x4924924) throw new Exception($"{u32:x}/56={u32 / 56:x}");
                if (i32 / 56 != 0x2492492) throw new Exception($"{i32:x}/56={i32 / 56:x}");
            }
        }
    }
}
