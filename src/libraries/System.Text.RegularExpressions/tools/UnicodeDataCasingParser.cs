// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;

namespace GenerateRegexCasingTable
{
    /// <summary>
    /// Class that parses UnicodeData.txt file and generates a casing table using lowercase values.
    /// </summary>
    public class UnicodeDataCasingParser
    {
        /// <summary>
        /// Parses UnicodeData.txt file in path <paramref name="unicodeDataTxtFilePath"/> and returns a Dictionary map
        /// with all of the lower-casing info
        /// </summary>
        /// <param name="unicodeDataTxtFilePath">The full path to UnicodeData.txt</param>
        /// <returns>A Dictionary map of chars with lowercasing data parsed from UnicodeData.txt</returns>
        public static Dictionary<char, char> Parse(string unicodeDataTxtFilePath, bool upperCase)
        {
            using FileStream fs = File.OpenRead(unicodeDataTxtFilePath);
            using StreamReader reader = new StreamReader(fs);

            Dictionary<char, char> result = new Dictionary<char, char>();

            string? line;
            // Parse each line. The format has one line per character, with semicolon separated properties. The only
            // values we care about is the one in position 0 which is the hex representation of the character, and the
            // property at position 13 which is the lowercase mapping with a hex value pointing to the lower case character.
            while ((line = reader.ReadLine()) != null)
            {
                string[] split = line.Split(';');
                Debug.Assert(split.Length == 15);

                uint codepoint = uint.Parse(split[0], NumberStyles.HexNumber, CultureInfo.InvariantCulture);

                if (codepoint > 0xFFFF)
                    break;

                if (upperCase)
                {
                    // Skip special cases where string.Compare invariantCultureIgnoreCase returns false
                    if (codepoint == 0x0131)
                        continue;

                    if (int.TryParse(split[12], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int upperCaseCodePoint))
                    {
                        result.Add((char)codepoint, (char)upperCaseCodePoint);
                    }
                }
                else
                {
                    // Skip special cases where string.Compare invariantCultureIgnoreCase returns false
                    if (codepoint == 0x0130)
                        continue;

                    if (int.TryParse(split[13], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int lowerCaseCodePoint))
                    {
                        result.Add((char)codepoint, (char)lowerCaseCodePoint);
                    }
                }
            }

            return result;
        }
    }
}
