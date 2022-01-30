// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace JIT.HardwareIntrinsics.X86
{
    public static partial class Program
    {
        private static void Sha256MessageSchedule2Byte()
        {
            if (!Sha.IsSupported)
            {
                return;
            }

            RunSha256MessageSchedule2(0, 0, 0, 0);
            RunSha256MessageSchedule2(long.MinValue, long.MinValue, long.MinValue, long.MinValue);
            RunSha256MessageSchedule2(long.MaxValue, long.MaxValue, long.MaxValue, long.MaxValue);
            RunSha256MessageSchedule2(10000, 20000, 30000, 40000);
        }

        private static void RunSha256MessageSchedule2(long lowerA, long upperA, long lowerB, long upperB)
        {
            var a = Vector128.AsByte(Vector128.Create(lowerA, upperA));
            var b = Vector128.AsByte(Vector128.Create(lowerA, upperA));

            Sha.Sha256MessageSchedule2(a, b);
        }
    }
}
