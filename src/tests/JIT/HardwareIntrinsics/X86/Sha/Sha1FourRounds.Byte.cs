// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace JIT.HardwareIntrinsics.X86
{
    public static partial class Program
    {
        private static void Sha1FourRoundsByte()
        {
            if (!Sha.IsSupported)
            {
                return;
            }

            RunSha1FourRounds(0, 0, 0, 0, 0);
            RunSha1FourRounds(long.MinValue, long.MinValue, long.MinValue, long.MinValue, 0);
            RunSha1FourRounds(long.MaxValue, long.MaxValue, long.MaxValue, long.MaxValue, 0);
            RunSha1FourRounds(10000, 20000, 30000, 40000, 0);
            RunSha1FourRounds(10000, 20000, 30000, 40000, 1);
            RunSha1FourRounds(10000, 20000, 30000, 40000, 2);
            RunSha1FourRounds(10000, 20000, 30000, 40000, 3);
            RunSha1FourRounds(10000, 20000, 30000, 40000, 4);
            RunSha1FourRounds(10000, 20000, 30000, 40000, 5);
            RunSha1FourRounds(10000, 20000, 30000, 40000, 6);
            RunSha1FourRounds(10000, 20000, 30000, 40000, 7);
            RunSha1FourRounds(10000, 20000, 30000, 40000, 8);
            RunSha1FourRounds(10000, 20000, 30000, 40000, 9);
            RunSha1FourRounds(10000, 20000, 30000, 40000, 10);
            RunSha1FourRounds(10000, 20000, 30000, 40000, 11);
        }

        private static void RunSha1FourRounds(long lowerA, long upperA, long lowerB, long upperB, byte func)
        {
            var a = Vector128.AsByte(Vector128.Create(lowerA, upperA));
            var b = Vector128.AsByte(Vector128.Create(lowerA, upperA));

            Sha.Sha1FourRounds(a, b, func);
        }
    }
}
