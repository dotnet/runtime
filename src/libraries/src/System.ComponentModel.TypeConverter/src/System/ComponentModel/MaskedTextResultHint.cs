// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.ComponentModel
{
    /// <summary>
    /// Enum defining hints about the reason of the result of a particular operation.
    /// </summary>
    public enum MaskedTextResultHint
    {
        // Unknown/Uninitialized
        Unknown = 0,

        ////////// Success (positive values, excluded). /////////
        /// WARNING: Do NOT modify the order of the success enum values, they are ordered the way TestString give precedence 
        /// to the result hint when multiple chars tested giving different success hints.

        // The operation Succeeded because a literal, prompt or space char was escaped.
        CharacterEscaped = 1,
        // The primary operation was not performed because it was not needed and no side effect generated.
        NoEffect = 2,
        // The primary operation was not performed but had a side effect (e.g. Delete at an unassigned edit pos produces left-shifting of chars).
        SideEffect = 3,
        // The primary operation succeeded.
        Success = 4,


        ///////// Failure values (negative values). /////////

        // Failure due to mask violation. (values in the range of [-1, -49]

        // Input character not ascii.
        AsciiCharacterExpected = -1,
        // Input character not alpha-numeric ascii.
        AlphanumericCharacterExpected = -2,
        // Input character not a digit.
        DigitExpected = -3,
        // Input character not a letter.
        LetterExpected = -4,
        // Input character not a signed digit.
        SignedDigitExpected = -5,

        // Other failures. (values < -50)

        // Invalid input
        InvalidInput = -51,
        // Prompt not allowed as input.
        PromptCharNotAllowed = -52,
        // No more room.
        UnavailableEditPosition = -53,
        // Literal or separator position.
        NonEditPosition = -54,
        // Position not in the range of indexes.
        PositionOutOfRange = -55
    }
}
