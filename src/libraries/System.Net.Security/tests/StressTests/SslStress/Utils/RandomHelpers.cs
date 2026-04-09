// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace SslStress.Utils
{
    public static class RandomHelpers
    {
        public static Random NextRandom(this Random random) => new Random(Seed: random.Next());

        public static bool NextBoolean(this Random random, double probability = 0.5)
        {
            if (probability < 0 || probability > 1)
                throw new ArgumentOutOfRangeException(nameof(probability));

            return random.NextDouble() < probability;
        }
    }
}
