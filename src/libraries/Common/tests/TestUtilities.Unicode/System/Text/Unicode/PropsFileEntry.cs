// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Security.Policy;
using System.Text.RegularExpressions;
using Xunit;

namespace System.Text.Unicode
{
    // Represents an entry in a Unicode props file.
    // The expected format is "XXXX[..YYYY] ; <propName> [# <comment>]".
    internal sealed class PropsFileEntry
    {
        private static readonly Regex _regex = new Regex(@"^\s*(?<firstCodePoint>[0-9a-f]{4,})(\.\.(?<lastCodePoint>[0-9a-f]{4,}))?\s*;\s*(?<propName>.+?)\s*(#\s*(?<comment>.*))?$", RegexOptions.IgnoreCase);

        public readonly int FirstCodePoint;
        public readonly int LastCodePoint;
        public readonly string PropName;

        private PropsFileEntry(uint firstCodePoint, uint lastCodePoint, string propName)
        {
            Assert.True(firstCodePoint <= 0x10FFFF, "First code point is out of range.");
            Assert.True(lastCodePoint <= 0x10FFFF, "Last code point is out of range.");
            Assert.True(firstCodePoint <= lastCodePoint, "First code point is after last code point.");

            FirstCodePoint = (int)firstCodePoint;
            LastCodePoint = (int)lastCodePoint;
            PropName = propName;
        }

        public bool IsSingleCodePoint => (FirstCodePoint == LastCodePoint);

        public static bool TryParseLine(string line, out PropsFileEntry value)
        {
            Match match = _regex.Match(line);

            if (!match.Success)
            {
                value = default; // no match
                return false;
            }

            uint firstCodePoint = uint.Parse(match.Groups["firstCodePoint"].Value, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            uint lastCodePoint = firstCodePoint; // assume no "..YYYY" segment for now

            if (match.Groups["lastCodePoint"].Success)
            {
                lastCodePoint = uint.Parse(match.Groups["lastCodePoint"].Value, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            }

            value = new PropsFileEntry(firstCodePoint, lastCodePoint, match.Groups["propName"].Value);
            return true;
        }
    }
}
