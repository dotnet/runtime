// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace System.Text.RegularExpressions
{
    /// <summary>
    /// This class will perform fast lookups for case-insensitive searches in order to find which characters should
    /// be considered equivalent. The mappings are generated based on Unicode ToLower() mappings and we generate the table
    /// using the GenerateRegexCasingTable program located under the Tools folder for this library.
    /// </summary>
    internal static partial class RegexCaseEquivalences
    {
        public const int CharactersPerRange = 1024;

        private static readonly char[] s_specialCasingSetBehaviors = new char[5]
        {
            'I', 'i', '\u0130', 'I', '\u0131'
        };

        /// <summary>
        /// Performs a fast lookup which determines if a character is involved in case conversion, as well as
        /// returns the characters that should be considered equivalent in case it does participate in case conversion.
        /// This method, in contrast to <see cref="TryFindCaseEquivalencesForChar(char, out ReadOnlySpan{char})"/> does take in
        /// culture and will also factor in the current culture in order to handle the special cases which are different between cultures.
        /// </summary>
        /// <param name="c">The character being analyzed</param>
        /// <param name="culture">The <see cref="CultureInfo"/> to be used to determine the equivalences.</param>
        /// <param name="equivalences">If <paramref name="c"/> is involved in case conversion, then equivalences will contain the
        /// span of character which should be considered equal to <paramref name="c"/> in a case-insensitive comparison.</param>
        /// <returns><see langword="true"/> if <paramref name="c"/> is involved in case conversion; otherwise, <see langword="false"/></returns>
        public static bool TryFindCaseEquivalencesForCharWithIBehavior(char c, CultureInfo culture, out ReadOnlySpan<char> equivalences)
        {
            if ((c | 0x20) == 'i' || (c | 0x01) == '\u0131')
            {
                RegexCaseBehavior mappingBehavior = GetRegexBehavior(culture);
                equivalences = c switch
                {
                    // Invariant mappings
                    'i' or 'I' when mappingBehavior is RegexCaseBehavior.Invariant => s_specialCasingSetBehaviors.AsSpan(0, 2), // 'I' and 'i'

                    // Non-Turkish mappings
                    'i' or 'I' or '\u0130' when mappingBehavior is RegexCaseBehavior.NonTurkish => s_specialCasingSetBehaviors.AsSpan(0, 3), // 'I', 'i', and '\u0130'

                    // Turkish mappings
                    'I' or '\u0131' when mappingBehavior is RegexCaseBehavior.Turkish => s_specialCasingSetBehaviors.AsSpan(3, 2), // 'I' and '\u0131'
                    'i' or '\u0130' when mappingBehavior is RegexCaseBehavior.Turkish => s_specialCasingSetBehaviors.AsSpan(1, 2), // 'i' and '\u0130'

                    // Default
                    _ => default
                };
                return equivalences != default;
            }
            else
            {
                return TryFindCaseEquivalencesForChar(c, out equivalences);
            }
        }

        /// <summary>
        /// Returns which <see cref="RegexCaseBehavior"/> should be used based on the passed in <paramref name="culture"/>.
        /// </summary>
        /// <param name="culture">The <see cref="CultureInfo"/> to be used to determine the behavior.</param>
        /// <returns>The <see cref="RegexCaseBehavior"/> that should be used.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static RegexCaseBehavior GetRegexBehavior(CultureInfo culture)
        {
            return
                culture.Name.Length == 0 ? RegexCaseBehavior.Invariant :
                IsTurkishOrAzeri(culture.Name) ? RegexCaseBehavior.Turkish :
                RegexCaseBehavior.NonTurkish;

            static bool IsTurkishOrAzeri(string cultureName)
            {
                if (cultureName.Length >= 2)
                {
                    // Assert that the first two characters in culture name are between a-z lowercase
                    Debug.Assert(cultureName[0] is >= 'a' and <= 'z');
                    Debug.Assert(cultureName[1] is >= 'a' and <= 'z');
                    switch (cultureName[0])
                    {
                        case 't':
                            return cultureName[1] == 'r' && (cultureName.Length == 2 || cultureName[2] == '-');
                        case 'a':
                            return cultureName[1] == 'z' && (cultureName.Length == 2 || cultureName[2] == '-');
                    }
                }
                return false;
            }
        }

        /// <summary>
        /// Performs a fast lookup which determines if a character is involved in case conversion, as well as
        /// returns the characters that should be considered equivalent in case it does participate in case conversion.
        /// </summary>
        /// <param name="c">The character being analyzed</param>
        /// <param name="equivalences">If <paramref name="c"/> is involved in case conversion, then equivalences will contain the
        /// span of character which should be considered equal to <paramref name="c"/> in a case-insensitive comparison.</param>
        /// <returns><see langword="true"/> if <paramref name="c"/> is involved in case conversion; otherwise, <see langword="false"/></returns>
        /// <remarks>
        /// The casing equivalence data is saved in three different lookup tables:
        ///   EquivalenceFirstLevelLookup => This is a ushort array which contains an index to be used for searching on the next lookup table 'EquivalenceCasingMap'.
        ///                                  We first grab the passed in <paramref name="c"/>, and divide it by CharactersPerRange and save it to index. We then use this index to
        ///                                  perform a lookup in 'EquivalenceFirstLevelLookup' table. If the value at index is 0xFFFF, then <paramref name="c"/>
        ///                                  isn't involved in case conversion so we keep equivalences as default, and return false. If the value at index is not 0xFFFF
        ///                                  then we use that value to search in 'EquivalenceCasingMap'.
        ///          EquivalenceCasingMap => This is a ushort array which contains a ushort for each character in a given range. The 3 highest bits of the ushort represent
        ///                                  the number of characters that are considered equivalent to <paramref name="c"/>. The rest of the 13 bits of the ushort represent
        ///                                  the index that should be used to get those equivalent characters in the 'EquivalenceCasingValues' table. We first calculate the
        ///                                  index2 based on the value obtained from the first level lookup table and adding <paramref name="c"/> modulo CharactersPerRange. If the value of
        ///                                  EquivalenceCasingMap[index2] is 0xFFFF then <paramref name="c"/> isn't involved in case conversion so we return false. Otherwise,
        ///                                  we decompose the ushort into the highest 3 bits, and the other 13 to compute two different numbers: the number of equivalence characters
        ///                                  to grab from the third table (highest 3 bits and save it as count), and the index (aka. index3) to grab them from (other 13 bits).
        ///       EquivalenceCasingValues => The final table contains ushort representing characters. We grab the index3 computed in the previous table, and we use it
        ///                                  to search on this table and grab the next 'count' items which are the equivalence mappings for <paramref name="c"/>.
        ///
        /// Example: using character 'A' (0x0041). We caluclate index by doing `index = 0x0041 / CharactersPerRange` which results in 0. We then look on the first lookup table using the
        /// calculated index `EquivalenceFirstLevelLookup[index]` which results in the value 0x0000. Because this value is not 0xFFFF, character 'A' may be participating in case
        /// conversion, so we continue our search by looking into the second lookup table. We calculate index2 by doing `index2 = (0x0041 % CharactersPerRange) + 0x0000` which results in
        /// index2 = 0x0041. We then use that index to search in the second lookup table `EquivalenceCasingMap[0x0041]` and get a value of 0x4000 back. Because that value isn't
        /// 0xFFFF then we now know that the character 'A' participates in case conversion. We decompose the value we got from the second table 0x4000 (0b_0100_0000_0000_0000 in binary)
        /// into the 3 highest bits (0b_010) and the rest of the 13 bits (0b_0_0000_0000_0000) resulting in: count = 2 and index3 = 0. This means that we finally must go into the
        /// third lookup table EquivalenceFirstLevelLookup at index 0, and grab the next 2 items which gives us 0x0041 ('A') and 0x0061 ('a'), which are the two characters considered
        /// equivalent in a case comparison for 'A'.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool TryFindCaseEquivalencesForChar(char c, out ReadOnlySpan<char> equivalences)
        {
            // Dividing by CharactersPerRange, in order to get the range index for c
            Debug.Assert((c / CharactersPerRange) < 0xFF);
            byte index = (byte)((uint)c / CharactersPerRange);
            ushort firstLevelLookupValue = EquivalenceFirstLevelLookup[index];

            // If character belongs to a range that doesn't participate in casing, then just return false
            if (firstLevelLookupValue == 0xFFFF)
            {
                equivalences = default;
                return false;
            }

            // Using mod CharactersPerRange operator to get the offset of c in the range, and add the firstLevelLookupValue
            Debug.Assert(((c % CharactersPerRange) + firstLevelLookupValue) < 0xFFFF);
            ushort index2 = (ushort)(((uint)c % CharactersPerRange) + firstLevelLookupValue);
            ushort mappingValue = EquivalenceCasingMap[index2];
            if (mappingValue == 0xFFFF)
            {
                equivalences = default;
                return false;
            }

            byte count = (byte)((mappingValue >> 13) & 0b111);
            ushort index3 = (ushort)(mappingValue & 0x1FFF);
            equivalences = EquivalenceCasingValues.AsSpan(index3, count);

            return true;
        }
    }
}
