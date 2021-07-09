// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Numerics.Enums
{
    /// <summary>
    /// States of numbers.
    /// </summary>
    internal enum NumberStates
    {
        None = 0b_0000_0000,
        Sign = 0b_0000_0001,
        Parentheses = 0b_0000_0010,
        Digits = 0b_0000_0100,
        NonZero = 0b_0000_1000,
        Decimal = 0b_0001_0000,
        Currency = 0b_0010_0000,
    }
}
