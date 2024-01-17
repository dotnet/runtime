// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace System.Buffers
{
    [InlineArray(8)]
    internal struct EightPackedReferences
    {
#pragma warning disable CA1823 // Unused field -- https://github.com/dotnet/roslyn-analyzers/issues/6788
        private object? _ref0;
#pragma warning restore CA1823

        public EightPackedReferences(ReadOnlySpan<object> values)
        {
            Debug.Assert(values.Length is > 0 and <= 8, $"Got {values.Length} values");

            values.CopyTo(this!);
        }
    }
}
