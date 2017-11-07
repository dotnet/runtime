// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Buffers.Text
{
    internal static partial class Utf8Constants
    {
        public const byte Colon = (byte)':';
        public const byte Comma = (byte)',';
        public const byte Minus = (byte)'-';
        public const byte Period = (byte)'.';
        public const byte Plus = (byte)'+';
        public const byte Slash = (byte)'/';
        public const byte Space = (byte)' ';
        public const byte Hyphen = (byte)'-';

        public const byte Separator = (byte)',';

        // Invariant formatting uses groups of 3 for each number group separated by commas.
        //   ex. 1,234,567,890
        public const int GroupSize = 3;

        public static readonly byte[] s_True = { (byte)'T', (byte)'r', (byte)'u', (byte)'e' };
        public static readonly byte[] s_False = { (byte)'F', (byte)'a', (byte)'l', (byte)'s', (byte)'e' };
        public static readonly byte[] s_true = { (byte)'t', (byte)'r', (byte)'u', (byte)'e' };
        public static readonly byte[] s_false = { (byte)'f', (byte)'a', (byte)'l', (byte)'s', (byte)'e' };

        public static readonly TimeSpan NullUtcOffset = TimeSpan.MinValue;  // Utc offsets must range from -14:00 to 14:00 so this is never a valid offset.

        public const int DateTimeMaxUtcOffsetHours = 14; // The UTC offset portion of a TimeSpan or DateTime can be no more than 14 hours and no less than -14 hours.

        public const int DateTimeNumFractionDigits = 7;  // TimeSpan and DateTime formats allow exactly up to many digits for specifying the fraction after the seconds.
        public const int MaxDateTimeFraction = 9999999;  // ... and hence, the largest fraction expressible is this.
    }
}
