// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Microsoft.DotNet.Build.Tasks
{
    public class TpnSectionHeader
    {
        private static readonly char[] SectionSeparatorChars = { '-', '=' };
        private static readonly Regex NumberListPrefix = new Regex(@"^[0-9]+\.\t(?<name>.*)$");

        public static bool IsSeparatorLine(string line)
        {
            return line.Length > 2 && line.All(c => SectionSeparatorChars.Contains(c));
        }

        public static IEnumerable<TpnSectionHeader> ParseAll(string[] lines)
        {
            // A separator line can't represent a section if it's on the first or last few lines.
            for (int i = 1; i < lines.Length - 2; i++)
            {
                string lineAbove = lines[i - 1].Trim();
                string line = lines[i].Trim();
                string lineBelow = lines[i + 1].Trim();

                if (line.Length > 2 &&
                    IsSeparatorLine(line) &&
                    string.IsNullOrEmpty(lineBelow))
                {
                    // 'line' is a separator line. Check around to see what kind it is.

                    if (string.IsNullOrEmpty(lineAbove))
                    {
                        var header = ParseSeparatedOrNull(lines, i);
                        if (header != null)
                        {
                            yield return header;
                        }
                    }
                    else
                    {
                        var header = ParseUnderlined(lines, i);
                        yield return header;
                    }
                }

                var numberedHeader = ParseNumberedOrNull(lines, i);
                if (numberedHeader != null)
                {
                    yield return numberedHeader;
                }
            }
        }

        public string Name { get; set; }
        public string SeparatorLine { get; set; }

        public TpnSectionHeaderFormat Format { get; set; }

        public int StartLine { get; set; }
        public int LineLength { get; set; }

        public string SingleLineName => Name.Replace('\n', ' ').Replace('\r', ' ');

        public override string ToString()
        {
            switch (Format)
            {
                case TpnSectionHeaderFormat.Separated:
                    return
                        SeparatorLine + Environment.NewLine +
                        Environment.NewLine +
                        Name;

                case TpnSectionHeaderFormat.Underlined:
                    return
                        Name + Environment.NewLine +
                        SeparatorLine;

                case TpnSectionHeaderFormat.Numbered:
                    return SeparatorLine;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private static TpnSectionHeader ParseSeparatedOrNull(string[] lines, int i)
        {
            string[] nameLines = lines
                .Skip(i + 2)
                .TakeWhile(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => s.Trim())
                .ToArray();

            string name = string.Join(Environment.NewLine, nameLines);

            // If there's a separator line as the last line in the name, this line doesn't indicate
            // a section. It needs to be handled by ParseUnderlined instead.
            if (nameLines.Any(IsSeparatorLine))
            {
                if (nameLines.Take(nameLines.Length - 1).Any(IsSeparatorLine))
                {
                    throw new ArgumentException(
                        $"Separator line detected inside name '{name}'");
                }
            }
            else
            {
                return new TpnSectionHeader
                {
                    Name = name,

                    SeparatorLine = lines[i],
                    Format = TpnSectionHeaderFormat.Separated,

                    StartLine = i,
                    LineLength = 2 + nameLines.Length
                };
            }

            return null;
        }

        private static TpnSectionHeader ParseUnderlined(string[] lines, int i)
        {
            string[] nameLines = lines
                .Take(i)
                .Reverse()
                .TakeWhile(s => !string.IsNullOrWhiteSpace(s))
                .Reverse()
                .Select(s => s.Trim())
                .ToArray();

            int nameStartLine = i - nameLines.Length;

            return new TpnSectionHeader
            {
                Name = string.Join(Environment.NewLine, nameLines),

                SeparatorLine = lines[i],
                Format = TpnSectionHeaderFormat.Underlined,

                StartLine = nameStartLine,
                LineLength = nameLines.Length + 1
            };
        }
        private static TpnSectionHeader ParseNumberedOrNull(string[] lines, int i)
        {
            Match numberListMatch;

            if (string.IsNullOrWhiteSpace(lines[i - 1]) &&
                string.IsNullOrWhiteSpace(lines[i + 1]) &&
                (numberListMatch = NumberListPrefix.Match(lines[i])).Success)
            {
                return new TpnSectionHeader
                {
                    Name = numberListMatch.Groups["name"].Value,

                    SeparatorLine = lines[i],
                    Format = TpnSectionHeaderFormat.Numbered,

                    StartLine = i,
                    LineLength = 1
                };
            }

            return null;
        }
    }
}
