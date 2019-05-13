// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

using static System.FormattableString;

namespace GenDefinedCharList
{
    /// <summary>
    /// This program outputs the 'UnicodeBlocks.generated.cs' and 'UnicodeBlocksTests.generated.cs' source files.
    /// </summary>
    class Program
    {
        private const string _codePointFiltersTestsGeneratedFormat = @"[InlineData('\u{1}', '\u{2}', nameof(UnicodeRanges.{0}))]";

        static void Main(string[] args)
        {
            if (args.Length < 3)
            {
                Console.WriteLine("Usage: dotnet run -- <infile> <outfile_runtime> <outfile_test>");
                return;
            }

            // The input file should be Blocks.txt from the UCD corresponding to the
            // version of the Unicode spec we're consuming.
            // More info: http://www.unicode.org/reports/tr44/
            // Latest Blocks.txt: http://www.unicode.org/Public/UCD/latest/ucd/Blocks.txt

            StringBuilder runtimeCodeBuilder = new StringBuilder();
            WriteCopyrightAndHeader(runtimeCodeBuilder);
            runtimeCodeBuilder.AppendLine();
            runtimeCodeBuilder.AppendLine("namespace System.Text.Unicode");
            runtimeCodeBuilder.AppendLine("{");
            runtimeCodeBuilder.AppendLine("    public static partial class UnicodeRanges");
            runtimeCodeBuilder.AppendLine("    {");

            StringBuilder testCodeBuilder = new StringBuilder();
            WriteCopyrightAndHeader(testCodeBuilder);
            testCodeBuilder.AppendLine();
            testCodeBuilder.AppendLine("using System.Collections.Generic;");
            testCodeBuilder.AppendLine();
            testCodeBuilder.AppendLine("namespace System.Text.Unicode.Tests");
            testCodeBuilder.AppendLine("{");
            testCodeBuilder.AppendLine("    public static partial class UnicodeRangesTests");
            testCodeBuilder.AppendLine("    {");
            testCodeBuilder.AppendLine("        public static IEnumerable<object[]> UnicodeRanges_GeneratedData => new[]");
            testCodeBuilder.AppendLine("        {");

            string[] allInputLines = File.ReadAllLines(args[0]);

            Regex inputLineRegex = new Regex(@"^(?<startCode>[0-9A-F]{4})\.\.(?<endCode>[0-9A-F]{4}); (?<blockName>.+)$");
            bool isFirstLine = true;

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
                string blockNameAsProperty = WithDotNetPropertyCasing(RemoveAllNonAlphanumeric(blockName));
                string blockNameAsField = Invariant($"_u{startCode}");

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

                if (!isFirstLine)
                {
                    runtimeCodeBuilder.AppendLine();
                }

                isFirstLine = false;

                runtimeCodeBuilder.AppendLine(Invariant($"        /// <summary>"));
                runtimeCodeBuilder.AppendLine(Invariant($"        /// A <see cref=\"UnicodeRange\"/> corresponding to the '{blockName}' Unicode block (U+{startCode}..U+{endCode})."));
                runtimeCodeBuilder.AppendLine(Invariant($"        /// </summary>"));
                runtimeCodeBuilder.AppendLine(Invariant($"        /// <remarks>"));
                runtimeCodeBuilder.AppendLine(Invariant($"        /// See http://www.unicode.org/charts/PDF/U{startCode}.pdf for the full set of characters in this block."));
                runtimeCodeBuilder.AppendLine(Invariant($"        /// </remarks>"));
                runtimeCodeBuilder.AppendLine(Invariant($"        public static UnicodeRange {blockNameAsProperty} => {blockNameAsField} ?? CreateRange(ref {blockNameAsField}, first: '\\u{startCode}', last: '\\u{endCode}');"));
                runtimeCodeBuilder.AppendLine(Invariant($"        private static UnicodeRange {blockNameAsField};"));

                testCodeBuilder.AppendLine(Invariant($"            new object[] {{ '\\u{startCode}', '\\u{endCode}', nameof(UnicodeRanges.{blockNameAsProperty}) }},"));
            }

            runtimeCodeBuilder.AppendLine("    }");
            runtimeCodeBuilder.AppendLine("}");

            testCodeBuilder.AppendLine("        };");
            testCodeBuilder.AppendLine("    }");
            testCodeBuilder.AppendLine("}");

            File.WriteAllText(args[1], runtimeCodeBuilder.ToString());
            File.WriteAllText(args[2], testCodeBuilder.ToString());
        }

        private static string RemoveAllNonAlphanumeric(string blockName)
        {
            // Allow only A-Z 0-9
            return new String(blockName.ToCharArray().Where(c => ('A' <= c && c <= 'Z') || ('a' <= c && c <= 'z') || ('0' <= c && c <= '9')).ToArray());
        }

        private static string WithDotNetPropertyCasing(string originalInput)
        {
            // Converts "CJKSymbolsandPunctuation" to "CjkSymbolsandPunctunation"
            // (n.b. We don't uppercase 'and' for compatibility with existing property names.)

            char[] chars = originalInput.ToCharArray();
            for (int i = 0; i < chars.Length; i++)
            {
                if (char.IsUpper(chars[i]))
                {
                    // Found an uppercase char - is it followed by a string of uppercase chars
                    // that needs to be converted to lowercase?

                    Span<char> remaining = chars.AsSpan(i + 1);
                    if (remaining.Length < 2)
                    {
                        break;
                    }

                    if (char.IsUpper(remaining[0]) && char.IsUpper(remaining[1]))
                    {
                        int j = i + 1;
                        for (; j < chars.Length; j++)
                        {
                            if (char.IsLower(chars[j]))
                            {
                                chars[j - 1] = originalInput[j - 1]; // restore original case of immediately preceding char
                                break;
                            }
                            else
                            {
                                chars[j] = char.ToLowerInvariant(chars[j]);
                            }
                        }

                        i = j - 1; // found a lowercase char or reached the end of the string
                        continue;
                    }
                }
            }

            return new string(chars);
        }

        private static void WriteCopyrightAndHeader(StringBuilder builder)
        {
            builder.AppendLine("// Licensed to the .NET Foundation under one or more agreements.");
            builder.AppendLine("// The .NET Foundation licenses this file to you under the MIT license.");
            builder.AppendLine("// See the LICENSE file in the project root for more information.");
            builder.AppendLine();
            builder.AppendLine("// This file was generated by a tool.");
            builder.AppendLine("// See src/System.Text.Encodings.Web/tools/GenUnicodeRanges");
        }
    }
}
