// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics.Arm;
using System.Runtime.Intrinsics.X86;

namespace System.Buffers
{
    internal sealed class AsciiStringSearchValuesTeddyBucketizedN2<TStartCaseSensitivity, TCaseSensitivity> : AsciiStringSearchValuesTeddyBase<SearchValues.TrueConst, TStartCaseSensitivity, TCaseSensitivity>
        where TStartCaseSensitivity : struct, StringSearchValuesHelper.ICaseSensitivity
        where TCaseSensitivity : struct, StringSearchValuesHelper.ICaseSensitivity
    {
        public AsciiStringSearchValuesTeddyBucketizedN2(string[][] buckets, ReadOnlySpan<string> values, HashSet<string> uniqueValues)
            : base(buckets, values, uniqueValues, n: 2)
        { }

        [CompExactlyDependsOn(typeof(Ssse3))]
        [CompExactlyDependsOn(typeof(AdvSimd.Arm64))]
        internal override int IndexOfAnyMultiString(ReadOnlySpan<char> span) =>
            IndexOfAnyN2(span);
    }
}
