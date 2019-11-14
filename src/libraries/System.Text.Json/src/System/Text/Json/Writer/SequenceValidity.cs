// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Buffers.Text
{
    /// <summary>
    /// Represents the validity of a UTF code unit sequence.
    /// </summary>
    internal enum SequenceValidity
    {
        /// <summary>
        /// The sequence is empty.
        /// </summary>
        Empty = 0,

        /// <summary>
        /// The sequence is well-formed and unambiguously represents a proper Unicode scalar value.
        /// </summary>
        /// <remarks>
        /// [ 20 ] (U+0020 SPACE) is a well-formed UTF-8 sequence.
        /// [ C3 A9 ] (U+00E9 LATIN SMALL LETTER E WITH ACUTE) is a well-formed UTF-8 sequence.
        /// [ F0 9F 98 80 ] (U+1F600 GRINNING FACE) is a well-formed UTF-8 sequence.
        /// [ D83D DE00 ] (U+1F600 GRINNING FACE) is a well-formed UTF-16 sequence.
        /// </remarks>
        WellFormed = 1,

        /// <summary>
        /// The sequence is not well-formed on its own, but it could appear as a prefix
        /// of a longer well-formed sequence. More code units are needed to make a proper
        /// determination as to whether this sequence is well-formed. Incomplete sequences
        /// can only appear at the end of a string.
        /// </summary>
        /// <remarks>
        /// [ C2 ] is an incomplete UTF-8 sequence if it is followed by nothing.
        /// [ F0 9F ] is an incomplete UTF-8 sequence if it is followed by nothing.
        /// [ D83D ] is an incomplete UTF-16 sequence if it is followed by nothing.
        /// </remarks>
        Incomplete = 2,

        /// <summary>
        /// The sequence is never well-formed anywhere, or this sequence can never appear as a prefix
        /// of a longer well-formed sequence, or the sequence was improperly terminated by the code
        /// unit which appeared immediately after this sequence.
        /// </summary>
        /// <remarks>
        /// [ 80 ] is an invalid UTF-8 sequence (code unit cannot appear at start of sequence).
        /// [ FE ] is an invalid UTF-8 sequence (sequence is never well-formed anywhere in UTF-8 string).
        /// [ C2 ] is an invalid UTF-8 sequence if it is followed by [ 20 ] (sequence improperly terminated).
        /// [ ED A0 ] is an invalid UTF-8 sequence (sequence is never well-formed anywhere in UTF-8 string).
        /// [ DE00 ] is an invalid UTF-16 sequence (code unit cannot appear at start of sequence).
        /// </remarks>
        Invalid = 3
    }
}
