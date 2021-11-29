// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Binary;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;

// Implemented from the specification at
// https://github.com/Cyan4973/xxHash/blob/f9155bd4c57e2270a4ffbb176485e5d713de1c9b/doc/xxhash_spec.md

namespace System.IO.Hashing
{
    public sealed partial class XxHash32
    {
        private struct State
        {
            private const uint Prime32_1 = 0x9E3779B1;
            private const uint Prime32_2 = 0x85EBCA77;
            private const uint Prime32_3 = 0xC2B2AE3D;
            private const uint Prime32_4 = 0x27D4EB2F;
            private const uint Prime32_5 = 0x165667B1;

            private uint _acc1;
            private uint _acc2;
            private uint _acc3;
            private uint _acc4;
            private readonly uint _smallAcc;
            private bool _hadFullStripe;

            internal State(uint seed)
            {
                _acc1 = seed + unchecked(Prime32_1 + Prime32_2);
                _acc2 = seed + Prime32_2;
                _acc3 = seed;
                _acc4 = seed - Prime32_1;

                _smallAcc = seed + Prime32_5;
                _hadFullStripe = false;
            }

            internal void ProcessStripe(ReadOnlySpan<byte> source)
            {
                Debug.Assert(source.Length >= StripeSize);
                source = source.Slice(0, StripeSize);

                _acc1 = ApplyRound(_acc1, source);
                _acc2 = ApplyRound(_acc2, source.Slice(sizeof(uint)));
                _acc3 = ApplyRound(_acc3, source.Slice(2 * sizeof(uint)));
                _acc4 = ApplyRound(_acc4, source.Slice(3 * sizeof(uint)));

                _hadFullStripe = true;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private readonly uint Converge()
            {
                return
                    BitOperations.RotateLeft(_acc1, 1) +
                    BitOperations.RotateLeft(_acc2, 7) +
                    BitOperations.RotateLeft(_acc3, 12) +
                    BitOperations.RotateLeft(_acc4, 18);
            }

            private static uint ApplyRound(uint acc, ReadOnlySpan<byte> lane)
            {
                acc += BinaryPrimitives.ReadUInt32LittleEndian(lane) * Prime32_2;
                acc = BitOperations.RotateLeft(acc, 13);
                acc *= Prime32_1;

                return acc;
            }

            internal readonly uint Complete(int length, ReadOnlySpan<byte> remaining)
            {
                uint acc = _hadFullStripe ? Converge() : _smallAcc;

                acc += (uint)length;

                while (remaining.Length >= sizeof(uint))
                {
                    uint lane = BinaryPrimitives.ReadUInt32LittleEndian(remaining);
                    acc += lane * Prime32_3;
                    acc = BitOperations.RotateLeft(acc, 17);
                    acc *= Prime32_4;

                    remaining = remaining.Slice(sizeof(uint));
                }

                for (int i = 0; i < remaining.Length; i++)
                {
                    uint lane = remaining[i];
                    acc += lane * Prime32_5;
                    acc = BitOperations.RotateLeft(acc, 11);
                    acc *= Prime32_1;
                }

                acc ^= (acc >> 15);
                acc *= Prime32_2;
                acc ^= (acc >> 13);
                acc *= Prime32_3;
                acc ^= (acc >> 16);

                return acc;
            }
        }
    }
}
