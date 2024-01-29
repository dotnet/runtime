// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;

namespace System
{
    public partial class Random
    {
        /// <summary>Base type for all generator implementations that plug into the base Random.</summary>
        internal abstract class ImplBase
        {
            public abstract double Sample();

            public abstract int Next();

            public abstract int Next(int maxValue);

            public abstract int Next(int minValue, int maxValue);

            public abstract long NextInt64();

            public abstract long NextInt64(long maxValue);

            public abstract long NextInt64(long minValue, long maxValue);

            public abstract float NextSingle();

            public abstract double NextDouble();

            public abstract void NextBytes(byte[] buffer);

            public abstract void NextBytes(Span<byte> buffer);

            // NextUInt32/64 algorithms based on https://arxiv.org/pdf/1805.10941.pdf and https://github.com/lemire/fastrange.

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal static uint NextUInt32(uint maxValue, XoshiroImpl xoshiro)
            {
                ulong randomProduct = (ulong)maxValue * xoshiro.NextUInt32();
                uint lowPart = (uint)randomProduct;

                if (lowPart < maxValue)
                {
                    uint remainder = (0u - maxValue) % maxValue;

                    while (lowPart < remainder)
                    {
                        randomProduct = (ulong)maxValue * xoshiro.NextUInt32();
                        lowPart = (uint)randomProduct;
                    }
                }

                return (uint)(randomProduct >> 32);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal static ulong NextUInt64(ulong maxValue, XoshiroImpl xoshiro)
            {
                ulong randomProduct = Math.BigMul(maxValue, xoshiro.NextUInt64(), out ulong lowPart);

                if (lowPart < maxValue)
                {
                    ulong remainder = (0ul - maxValue) % maxValue;

                    while (lowPart < remainder)
                    {
                        randomProduct = Math.BigMul(maxValue, xoshiro.NextUInt64(), out lowPart);
                    }
                }

                return randomProduct;
            }
        }
    }
}
