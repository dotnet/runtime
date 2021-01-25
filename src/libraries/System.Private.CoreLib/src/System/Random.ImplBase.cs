// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
        }
    }
}
