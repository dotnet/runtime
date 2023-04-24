using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;

namespace GenDefinedCharList
{
    /// <summary>
    /// This program outputs the 'UnicodeHelpers.generated.cs' bitmap file.
    /// </summary>
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("Usage: dotnet run -- <inFile> <outFile>");
                return;
            }

            // The input file should be UnicodeData.txt from the UCD corresponding to the
            // version of the Unicode spec we're consuming.
            // More info: https://www.unicode.org/reports/tr44/tr44-14.html#UCD_Files
            // Latest UnicodeData.txt: https://www.unicode.org/Public/UCD/latest/ucd/UnicodeData.txt

            const uint MAX_UNICODE_CHAR = 0x10FFFF; // Unicode range is U+0000 .. U+10FFFF
            bool[] definedChars = new bool[MAX_UNICODE_CHAR + 1];
            Dictionary<string, UnicodeRange> ranges = new Dictionary<string, UnicodeRange>();

            // Read all defined characters from the input file.
            string[] allLines = File.ReadAllLines(args[0]);

            // Each line is a semicolon-delimited list of information:
            // <value>;<name>;<category>;...
            foreach (string line in allLines)
            {
                string[] splitLine = line.Split(new char[] { ';' }, 4);
                uint codepoint = uint.Parse(splitLine[0], NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture);
                string rawName = splitLine[1];
                string category = splitLine[2];

                // ranges go into their own dictionary for later processing
                string rangeName;
                bool isStartOfRange;
                if (IsRangeDefinition(rawName, out rangeName, out isStartOfRange))
                {
                    if (isStartOfRange)
                    {
                        ranges.Add(rangeName, new UnicodeRange() { FirstCodePoint = codepoint, Category = category });
                    }
                    else
                    {
                        var existingRange = ranges[rangeName];
                        Debug.Assert(existingRange.FirstCodePoint != 0, "We should've seen the start of this range already.");
                        Debug.Assert(existingRange.LastCodePoint == 0, "We shouldn't have seen the end of this range already.");
                        Debug.Assert(existingRange.Category == category, "Range start Unicode category doesn't match range end Unicode category.");
                        existingRange.LastCodePoint = codepoint;
                    }
                    continue;
                }

                // We only allow certain categories of code points.
                // Check for special-cased code points before querying whether the overall category is allowed.

                if (!(GetIsCodePointAllowedOverride(codepoint) ?? IsAllowedUnicodeCategory(category)))
                {
                    continue;
                }

                Debug.Assert(codepoint <= MAX_UNICODE_CHAR);
                definedChars[codepoint] = true;
            }

            // Next, populate characters that weren't defined on their own lines
            // but which are instead defined as members of a range.
            foreach (var range in ranges.Values)
            {
                if (IsAllowedUnicodeCategory(range.Category))
                {
                    Debug.Assert(range.FirstCodePoint <= MAX_UNICODE_CHAR);
                    Debug.Assert(range.LastCodePoint <= MAX_UNICODE_CHAR);
                    for (uint i = range.FirstCodePoint; i <= range.LastCodePoint; i++)
                    {
                        definedChars[i] = true;
                    }
                }
            }

            // Finally, write the list of defined characters out as a bitmap. The list
            // will be divided into groups of 32, with each group being represented as
            // a little-endian 32-bit integer. Within each group, the least significant
            // bit will represent the first member of the group, and the most significant
            // bit will represent the last member of the group.
            //
            // Example: To look up the character U+0123,
            // a) First read the 32-bit value at offset 0x0120 (group #9) as little-endian,
            // b) then AND with the value 0x0008 (1 << 3) and see if the result is non-zero.
            //
            // We're only concerned about the BMP (U+0000 .. U+FFFF) for now.

            StringBuilder builder = new StringBuilder();

            builder.AppendLine("// Licensed to the .NET Foundation under one or more agreements.");
            builder.AppendLine("// The .NET Foundation licenses this file to you under the MIT license.");
            builder.AppendLine();
            builder.AppendLine("// This file was generated by a tool.");
            builder.AppendLine("// See src/System.Text.Encodings.Web/tools/GenDefinedCharList");
            builder.AppendLine("// IF YOU NEED TO UPDATE UNICODE VERSION FOLLOW THE GUIDE AT src/libraries/System.Private.CoreLib/Tools/GenUnicodeProp/Updating-Unicode-Versions.md");
            builder.AppendLine();
            builder.AppendLine("namespace System.Text.Unicode");
            builder.AppendLine("{");
            builder.AppendLine("    internal static partial class UnicodeHelpers");
            builder.AppendLine("    {");
            builder.AppendLine("        private static ReadOnlySpan<byte> DefinedCharsBitmapSpan => new byte[0x2000]");
            builder.AppendLine("        {");

            for (int i = 0; i < 0x10000; i += 8)
            {
                int thisByte = 0;
                for (int j = 7; j >= 0; j--)
                {
                    thisByte <<= 1;
                    if (definedChars[i + j])
                    {
                        thisByte |= 0x1;
                    }
                }

                if ((i % 0x80) == 0)
                {
                    builder.Append("            ");
                }

                builder.Append("0x");
                builder.AppendFormat(CultureInfo.InvariantCulture, "{0:X2}", thisByte);
                builder.Append(", ");

                if (((i + 8) % 0x80) == 0)
                {
                    builder.AppendFormat(CultureInfo.InvariantCulture, "// U+{0:X4}..U+{1:X4}", i - 0x78, i + 7);
                    builder.AppendLine();
                }
            }

            builder.AppendLine("        };");
            builder.AppendLine("    }");
            builder.AppendLine("}");

            File.WriteAllText(args[1], builder.ToString());
        }

        private static bool? GetIsCodePointAllowedOverride(uint codepoint)
        {
            switch (codepoint)
            {
                case 0x0020:
                    // ' ' U+0020 SPACE is allowed, even though the Zs category (space separators) is otherwise disallowed.
                    return true;

                case 0xFEFF:
                    // U+FEFF ZERO WIDTH NO-BREAK SPACE is disallowed, even though the Cf category (format characters) is otherwise allowed.
                    // The reason for this is that U+FEFF is also used as the byte order mark (BOM), and some clients don't handle this
                    // code point correctly when it appears in the middle of a string. See https://www.unicode.org/faq/utf_bom.html#BOM.
                    return false;

                default:
                    // No override specified; fall back to whether the category itself is allowed or disallowed.
                    return null;
            }
        }

        private static bool IsAllowedUnicodeCategory(string category)
        {
            // We only allow certain classes of characters
            return category == "Lu" /* letters */
                || category == "Ll"
                || category == "Lt"
                || category == "Lm"
                || category == "Lo"
                || category == "Mn" /* marks */
                || category == "Mc"
                || category == "Me"
                || category == "Nd" /* numbers */
                || category == "Nl"
                || category == "No"
                || category == "Pc" /* punctuation */
                || category == "Pd"
                || category == "Ps"
                || category == "Pe"
                || category == "Pi"
                || category == "Pf"
                || category == "Po"
                || category == "Sm" /* symbols */
                || category == "Sc"
                || category == "Sk"
                || category == "So"
                || category == "Cf"; /* other */
        }

        private static bool IsRangeDefinition(string rawName, out string rangeName, out bool isStartOfRange)
        {
            // Ranges are represented within angle brackets, such as the following:
            // DC00;<Low Surrogate, First>;Cs;0;L;;;;;N;;;;;
            // DFFF;<Low Surrogate, Last>;Cs;0;L;;;;;N;;;;;
            if (rawName.StartsWith("<", StringComparison.Ordinal))
            {
                if (rawName.EndsWith(", First>", StringComparison.Ordinal))
                {
                    rangeName = rawName.Substring(1, rawName.Length - 1 - ", First>".Length);
                    isStartOfRange = true;
                    return true;
                }
                else if (rawName.EndsWith(", Last>", StringComparison.Ordinal))
                {
                    rangeName = rawName.Substring(1, rawName.Length - 1 - ", Last>".Length);
                    isStartOfRange = false;
                    return true;
                }
            }

            // not surrounded by <>, or <control> or some other non-range
            rangeName = null;
            isStartOfRange = false;
            return false;
        }

        // Represents a range of Unicode code points which are all members of a single category.
        // More info: https://www.unicode.org/faq/blocks_ranges.html
        private sealed class UnicodeRange
        {
            public uint FirstCodePoint;
            public uint LastCodePoint;
            public string Category;
        }
    }
}
