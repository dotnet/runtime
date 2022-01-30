// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace JIT.HardwareIntrinsics.X86
{
    public static partial class Program
    {
        private static void Sha1NextEByte()
        {
            if (!Sha.IsSupported)
            {
                return;
            }

            RunSha1NextE(0, 0, 0, 0);
            RunSha1NextE(long.MinValue, long.MinValue, long.MinValue, long.MinValue);
            RunSha1NextE(long.MaxValue, long.MaxValue, long.MaxValue, long.MaxValue);
            RunSha1NextE(10000, 20000, 30000, 40000);
        }

        private static void RunSha1NextE(long lowerA, long upperA, long lowerB, long upperB)
        {
            var a = Vector128.AsByte(Vector128.Create(lowerA, upperA));
            var b = Vector128.AsByte(Vector128.Create(lowerA, upperA));

            Sha.Sha1NextE(a, b);
        }
    }
}
