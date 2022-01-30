// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace JIT.HardwareIntrinsics.X86
{
    public static partial class Program
    {
        private static void Sha256TwoRoundsByte()
        {
            if (!Sha.IsSupported)
            {
                return;
            }

            RunSha256TwoRounds(0, 0, 0, 0, 0, 0);
            RunSha256TwoRounds(long.MinValue, long.MinValue, long.MinValue, long.MinValue, long.MinValue, long.MinValue);
            RunSha256TwoRounds(long.MaxValue, long.MaxValue, long.MaxValue, long.MaxValue, long.MaxValue, long.MaxValue);
            RunSha256TwoRounds(10000, 20000, 30000, 40000, 50000, 60000);
        }

        private static void RunSha256TwoRounds(long lowerA, long upperA, long lowerB, long upperB, long lowerK, long upperK)
        {
            var a = Vector128.AsByte(Vector128.Create(lowerA, upperA));
            var b = Vector128.AsByte(Vector128.Create(lowerA, upperA));
            var k = Vector128.AsByte(Vector128.Create(lowerK, upperK));

            Sha.Sha256TwoRounds(a, b, k);
        }
    }
}
