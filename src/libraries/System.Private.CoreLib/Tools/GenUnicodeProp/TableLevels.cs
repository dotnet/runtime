// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace GenUnicodeProp
{
    internal sealed class TableLevels
    {
        public readonly int Level1Bits;
        public readonly int Level2Bits;
        public readonly int Level3Bits;

        public TableLevels(int level2Bits, int level3Bits)
        {
            ArgumentOutOfRangeException.ThrowIfGreaterThan((uint)level2Bits, 20);
            ArgumentOutOfRangeException.ThrowIfGreaterThan((uint)level3Bits, 20);

            Level1Bits = 20 - level2Bits - level3Bits;
            if (Level1Bits < 0) { throw new Exception("Level2Bits + Level3Bits cannot exceed 20."); }

            Level2Bits = level2Bits;
            Level3Bits = level3Bits;
        }

        public override string ToString()
        {
            return FormattableString.Invariant($"{Level1Bits}:{Level2Bits}:{Level3Bits}");
        }
    }
}
