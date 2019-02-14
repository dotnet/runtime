// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Numerics.Hashing
{
    internal static class HashHelpers
    {
        public static readonly int RandomSeed = new Random().Next(int.MinValue, int.MaxValue);

        public static int Combine(int h1, int h2)
        {
            return ((int)BitOps.RotateLeft((uint)h1, 5) + h1) ^ h2;
        }
    }
}
