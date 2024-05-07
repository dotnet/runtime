// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Numerics;

namespace System.Net
{
    internal static partial class IPAddressParser<TChar>
        where TChar : unmanaged, IBinaryInteger<TChar>
    {
        public const int Octal = 8;
        public const int Decimal = 10;
        public const int Hex = 16;

        internal const int MaxIPv4StringLength = 15; // 4 numbers separated by 3 periods, with up to 3 digits per number
        internal const int MaxIPv6StringLength = 65;

        // Generic constants which are used for trying to parse a single digit as an integer.
        private static readonly TChar NumericRangeStartCharacter = TChar.CreateTruncating('0');

        public static bool IsValidInteger(int numericBase, TChar ch)
        {
            Debug.Assert(numericBase is Octal or Decimal or Hex);

            return numericBase switch
            {
                > 0 and < 10 => ch >= NumericRangeStartCharacter && ch - NumericRangeStartCharacter < TChar.CreateTruncating(numericBase),
                Hex => HexConverter.IsHexChar(int.CreateTruncating(ch)),
                _ => false
            };
        }

        public static bool TryParseInteger(int numericBase, TChar ch, out int parsedNumber)
        {
            bool validNumber = IsValidInteger(numericBase, ch);

            // HexConverter allows digits 1-F to be mapped to integers. The octal/decimal digit range restrictions are performed
            // in IsValidInteger.
            parsedNumber = validNumber
                ? HexConverter.FromChar(int.CreateTruncating(ch))
                : -1;

            return validNumber;
        }
    }
}
