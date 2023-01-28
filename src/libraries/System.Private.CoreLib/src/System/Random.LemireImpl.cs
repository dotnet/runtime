// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

namespace System
{
    public partial class Random
    {
        /// <summary>
        /// Provides an implementation of Lemire's nearly divisionless method for getting random numbers in an interval
        /// given a source for random uint or random ulong.
        ///
        /// The methods are based on the algorithm from https://arxiv.org/pdf/1805.10941.pdf and https://github.com/lemire/fastrange:
        ///
        /// Written in 2018 by Daniel Lemire.
        /// </summary>

        internal abstract class LemireImpl : ImplBase
        {
            internal abstract uint NextUInt32();
            internal abstract ulong NextUInt64();

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            protected uint NextBoundedUint(uint exclusiveUpperBound)
            {
                ulong randomProduct = (ulong)exclusiveUpperBound * NextUInt32();
                uint lowPart = (uint)randomProduct;

                if (lowPart < exclusiveUpperBound)
                {
                    uint remainder = (0u - exclusiveUpperBound) % exclusiveUpperBound;

                    while (lowPart < remainder)
                    {
                        randomProduct = (ulong)exclusiveUpperBound * NextUInt32();
                        lowPart = (uint)randomProduct;
                    }
                }

                return (uint)(randomProduct >> 32);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            protected ulong NextBoundedUlong(ulong exclusiveUpperBound)
            {
                UInt128 randomProduct = (UInt128)exclusiveUpperBound * NextUInt64();
                ulong lowPart = (ulong)randomProduct;

                if (lowPart < exclusiveUpperBound)
                {
                    ulong remainder = (0ul - exclusiveUpperBound) % exclusiveUpperBound;

                    while (lowPart < remainder)
                    {
                        randomProduct = (UInt128)exclusiveUpperBound * NextUInt64();
                        lowPart = (ulong)randomProduct;
                    }
                }

                return (ulong)(randomProduct >> 64);
            }
        }
    }
}
