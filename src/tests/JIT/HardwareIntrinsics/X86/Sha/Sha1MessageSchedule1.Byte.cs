// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace JIT.HardwareIntrinsics.X86
{
    public static partial class Program
    {
        private static void Sha1MessageSchedule1Byte()
        {
            if (!Sha.IsSupported)
            {
                return;
            }

            RunSha1MessageSchedule1(0, 0, 0, 0);
            RunSha1MessageSchedule1(long.MinValue, long.MinValue, long.MinValue, long.MinValue);
            RunSha1MessageSchedule1(long.MaxValue, long.MaxValue, long.MaxValue, long.MaxValue);
            RunSha1MessageSchedule1(10000, 20000, 30000, 40000);
        }

        private static void RunSha1MessageSchedule1(long lowerA, long upperA, long lowerB, long upperB)
        {
            var a = Vector128.AsByte(Vector128.Create(lowerA, upperA));
            var b = Vector128.AsByte(Vector128.Create(lowerA, upperA));

            Sha.Sha1MessageSchedule1(a, b);
        }
    }
}
