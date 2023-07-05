// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace System.Buffers
{
    [InlineArray(8)]
    internal readonly struct EightPackedReferences
    {
        private readonly object? _ref0;

        public EightPackedReferences(ReadOnlySpan<object> values)
        {
            Debug.Assert(values.Length <= 8, $"Got {values.Length} values");

            for (int i = 0; i < values.Length; i++)
            {
                this[i] = values[i];
            }
        }

        public object this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                Debug.Assert(index is >= 0 and < 8, $"Should be [0, 7], was {index}");
                Debug.Assert(Unsafe.Add(ref Unsafe.AsRef(in _ref0), index) is not null);

                return Unsafe.Add(ref Unsafe.AsRef(in _ref0), index)!;
            }
            private set
            {
                Unsafe.Add(ref Unsafe.AsRef(in _ref0), index) = value;
            }
        }
    }
}
