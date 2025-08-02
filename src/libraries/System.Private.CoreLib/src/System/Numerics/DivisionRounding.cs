// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Numerics
{
    /// <summary>
    /// Specifies the rounding strategy to use when performing division.
    /// </summary>
    public enum DivisionRounding
    {
        /// <summary>
        /// Truncated division (rounding toward zero) — round the division result towards zero.
        /// </summary>
        Truncate = 0,
        // Graph for truncated division with positive divisor https://www.wolframalpha.com/input?i=Plot%5B%7BIntegerPart%5Bn%5D%2C+n+-+IntegerPart%5Bn%5D%7D%2C+%7Bn%2C+-3%2C+3%7D%5D
        // Graph for truncated division with negative divisor https://www.wolframalpha.com/input?i=Plot%5B%7BIntegerPart%5B-n%5D%2C+n+%2B+IntegerPart%5B-n%5D%7D%2C+%7Bn%2C+-3%2C+3%7D%5D

        /// <summary>
        /// Floor division (rounding down) — round the division result down to the next lower integer.
        /// </summary>
        Floor = 1,
        // Graph for floor division with positive divisor https://www.wolframalpha.com/input?i=Plot%5B%7BFloor%5Bn%5D%2C+n+-+Floor%5Bn%5D%7D%2C+%7Bn%2C+-3%2C+3%7D%5D
        // Graph for floor division with negative divisor https://www.wolframalpha.com/input?i=Plot%5B%7BFloor%5B-n%5D%2C+n+%2B+Floor%5B-n%5D%7D%2C+%7Bn%2C+-3%2C+3%7D%5D

        /// <summary>
        /// Ceiling division (rounding up) — round the division result up to the next higher integer.
        /// </summary>
        Ceiling = 2,
        // Graph for ceiling division with positive divisor https://www.wolframalpha.com/input?i=Plot%5B%7BCeiling%5Bn%5D%2C+n+-+Ceiling%5Bn%5D%7D%2C+%7Bn%2C+-3%2C+3%7D%5D
        // Graph for ceiling division with negative divisor https://www.wolframalpha.com/input?i=Plot%5B%7BCeiling%5B-n%5D%2C+n+%2B+Ceiling%5B-n%5D%7D%2C+%7Bn%2C+-3%2C+3%7D%5D

        /// <summary>
        /// AwayFromZero division (rounding away zero — round the division result away from zero to the nearest integer.
        /// </summary>
        AwayFromZero = 3,
        // Graph for AwayFromZero division with positive divisor https://www.wolframalpha.com/input?i=Plot%5B%7BIntegerPart%5Bn%5D+%2B+Sign%5Bn%5D%2C+n+-+IntegerPart%5Bn%5D+-+Sign%5Bn%5D%7D%2C+%7Bn%2C+-3%2C+3%7D%5D
        // Graph for AwayFromZero division with negative divisor https://www.wolframalpha.com/input?i=Plot%5B%7BIntegerPart%5B-n%5D+%2B+Sign%5B-n%5D%2C+n+%2B+IntegerPart%5B-n%5D+%2B+Sign%5B-n%5D%7D%2C+%7Bn%2C+-3%2C+3%7D%5D

        /// <summary>
        /// Euclidean division ensures a non-negative remainder:
        ///   for positive divisor — round the division result down to the next lower integer (rounding down);
        ///   for negative divisor — round the division result up to the next higher integer  (rounding up);
        /// </summary>
        Euclidean = 4,
        // Graph for Euclidean division with positive divisor https://www.wolframalpha.com/input?i=Plot%5B%7BFloor%5Bn%5D%2C+n+-+Floor%5Bn%5D%7D%2C+%7Bn%2C+-3%2C+3%7D%5D
        // Graph for Euclidean division with negative divisor https://www.wolframalpha.com/input?i=Plot%5B%7BCeiling%5B-n%5D%2C+n+%2B+Ceiling%5B-n%5D%7D%2C+%7Bn%2C+-3%2C+3%7D%5D
    }
}
