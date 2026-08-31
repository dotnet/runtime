// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ------------------------------------------------------------------------------
// Changes to this file must follow the https://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace System.IO.Hashing
{
    public sealed partial class XxHash128 : System.IO.Hashing.NonCryptographicHashAlgorithm
    {
        [System.CLSCompliantAttribute(false)]
        public System.UInt128 GetCurrentHashAsUInt128() { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static System.UInt128 HashToUInt128(System.ReadOnlySpan<byte> source, long seed = 0) { throw null; }
    }
}
