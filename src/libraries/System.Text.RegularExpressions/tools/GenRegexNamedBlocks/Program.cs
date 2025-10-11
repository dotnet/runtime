// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

using static System.FormattableString;

namespace GenRegexNamedBlocks
{
    /// <summary>
    /// This program outputs the named blocks for RegexCharClass.cs
    /// </summary>
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.WriteLine("Usage: dotnet run -- <Blocks.txt>");
                Console.WriteLine("Example: dotnet run -- Blocks.txt");
                return;
            }

            // The input file should be Blocks.txt from the UCD corresponding to the
            // version of the Unicode spec we're consuming.
            // More info: https://www.unicode.org/reports/tr44/
            // Latest Blocks.txt: https://www.unicode.org/Public/UCD/latest/ucd/Blocks.txt

            string[] allInputLines = File.ReadAllLines(args[0]);

            Regex inputLineRegex = new Regex(@"^(?<startCode>[0-9A-F]{4})\.\.(?<endCode>[0-9A-F]{4}); (?<blockName>.+)$");

            var entries = new List<(string name, string startCode, string endCode)>();

            foreach (string inputLine in allInputLines)
            {
                // We only care about lines of the form "XXXX..XXXX; Block name"
                var match = inputLineRegex.Match(inputLine);
                if (match == null || !match.Success)
                {
                    continue;
                }

                string startCode = match.Groups["startCode"].Value;
                string endCode = match.Groups["endCode"].Value;
                string blockName = match.Groups["blockName"].Value;

                // Exclude the surrogate range and everything outside the BMP.
                uint startCodeAsInt = uint.Parse(startCode, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                if (startCodeAsInt >= 0x10000 || (startCodeAsInt >= 0xD800 && startCodeAsInt <= 0xDFFF))
                {
                    continue;
                }

                // Exclude any private use areas
                if (blockName.Contains("Private Use", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                // Convert block name to Regex format (with "Is" prefix)
                string regexBlockName = "Is" + RemoveAllNonAlphanumeric(blockName);

                entries.Add((regexBlockName, startCode, endCode));
            }

            // Sort by start code for consistent output
            entries.Sort((a, b) => string.Compare(a.name, b.name, StringComparison.Ordinal));

            // Generate the output
            foreach (var entry in entries)
            {
                Console.WriteLine($"            [\"{entry.name}\", \"\\u{entry.startCode}\\u{GetNextCodePoint(entry.endCode)}\"],");
            }
        }

        private static string RemoveAllNonAlphanumeric(string blockName)
        {
            // Allow only A-Z a-z 0-9 and hyphens
            // Keep hyphens to preserve naming like "Latin-1" or "Extended-A"
            return new string(blockName.ToCharArray().Where(c => 
                ('A' <= c && c <= 'Z') || 
                ('a' <= c && c <= 'z') || 
                ('0' <= c && c <= '9') ||
                c == '-').ToArray());
        }

        private static string GetNextCodePoint(string hexCode)
        {
            // Regex named blocks use the start of the next block as the end code
            // So we need to add 1 to the end code
            uint code = uint.Parse(hexCode, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            code++;
            return code.ToString("X4");
        }
    }
}
