// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.DotNet.Build.Tasks
{
    public class TpnDocument
    {
        public static TpnDocument Parse(string[] lines)
        {
            var headers = TpnSectionHeader.ParseAll(lines).ToArray();

            var sections = headers
                .Select((h, i) =>
                {
                    int headerEndLine = h.StartLine + h.LineLength + 1;
                    int linesUntilNext = lines.Length - headerEndLine;

                    if (i + 1 < headers.Length)
                    {
                        linesUntilNext = headers[i + 1].StartLine - headerEndLine;
                    }

                    return new TpnSection
                    {
                        Header = h,
                        Content = string.Join(
                            Environment.NewLine,
                            lines
                                .Skip(headerEndLine)
                                .Take(linesUntilNext)
                                // Skip lines in the content that could be confused for separators.
                                .Where(line => !TpnSectionHeader.IsSeparatorLine(line))
                                // Trim empty line at the end of the section.
                                .Reverse()
                                .SkipWhile(line => string.IsNullOrWhiteSpace(line))
                                .Reverse())
                    };
                })
                .ToArray();

            if (sections.Length == 0)
            {
                throw new ArgumentException($"No sections found.");
            }

            return new TpnDocument
            {
                Preamble = string.Join(
                    Environment.NewLine,
                    lines.Take(sections.First().Header.StartLine)),

                Sections = sections
            };
        }

        public string Preamble { get; set; }

        public IEnumerable<TpnSection> Sections { get; set; }

        public override string ToString() =>
            Preamble + Environment.NewLine +
            string.Join(Environment.NewLine + Environment.NewLine, Sections) +
            Environment.NewLine;
    }
}
