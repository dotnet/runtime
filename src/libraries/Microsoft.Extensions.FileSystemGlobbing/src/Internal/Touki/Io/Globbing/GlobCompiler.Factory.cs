// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Copyright (c) 2025 Jeremy W Kuhne
// SPDX-License-Identifier: MIT
// See LICENSE file in the project root for full license information

namespace Touki.Io.Globbing;

internal static partial class GlobCompiler
{
    private static partial class Factory
    {
        internal const int MaxOpcodeBodyLength = char.MaxValue;

        public static bool TryCreate(
            string pattern,
            bool ignoreCase,
            [NotNullWhen(true)] out GlobStrategy? result)
        {
            ReadOnlySpan<char> source = pattern;

            if (source.IndexOf('*') < 0)
            {
                result = new LiteralGlobStrategy(source.ToString(), ignoreCase);
                return true;
            }

            if (source.IndexOf('/') < 0
                && TryCreateSegmentMatcher(source, ignoreCase, out result))
            {
                if (!source.SequenceEqual("**"))
                {
                    result = new RootFileNameStrategy(result);
                }

                return true;
            }

            if (TryCreateRecursiveDirectoryStrategy(source, ignoreCase, out result)
                || TryCreateLiteralDirectoryStrategy(source, ignoreCase, out result))
            {
                return true;
            }

            if (TryCreateGlobStarFileNameStrategy(source, ignoreCase, out result))
            {
                return true;
            }

            if (!TryEncodeProgram(source, out string program, out bool hasGlobStar))
            {
                result = null;
                return false;
            }

            FindTrailingLiteral(program, out int nfaProgramLength, out int tailStart, out int tailLength);
            result = new CompiledGlobStrategy(
                program,
                nfaProgramLength,
                tailStart,
                tailLength,
                hasGlobStar,
                ignoreCase);
            return true;
        }

        private static bool TryCreateRecursiveDirectoryStrategy(
            ReadOnlySpan<char> pattern,
            bool ignoreCase,
            [NotNullWhen(true)] out GlobStrategy? result)
        {
            const string Prefix = "**/";
            const string Suffix = "/*/**";

            if (pattern.StartsWith(Prefix)
                && pattern.EndsWith(Suffix)
                && pattern.Length > Prefix.Length + Suffix.Length)
            {
                ReadOnlySpan<char> directoryPath = pattern[Prefix.Length..^Suffix.Length];
                if (directoryPath.IndexOf('*') < 0)
                {
                    result = new RecursiveDirectoryPathStrategy(directoryPath.ToString(), ignoreCase);
                    return true;
                }
            }

            result = null;
            return false;
        }

        private static bool TryCreateLiteralDirectoryStrategy(
            ReadOnlySpan<char> pattern,
            bool ignoreCase,
            [NotNullWhen(true)] out GlobStrategy? result)
        {
            int separator = pattern.LastIndexOf('/');
            if (separator > 0
                && pattern[..separator].IndexOf('*') < 0
                && TryCreateSegmentMatcher(pattern[(separator + 1)..], ignoreCase, out GlobStrategy? fileMatcher))
            {
                result = new LiteralDirectoryPathStrategy(pattern[..(separator + 1)].ToString(), fileMatcher, ignoreCase);
                return true;
            }

            result = null;
            return false;
        }

        private static bool TryCreateGlobStarFileNameStrategy(
            ReadOnlySpan<char> pattern,
            bool ignoreCase,
            [NotNullWhen(true)] out GlobStrategy? result)
        {
            result = null;
            if (pattern.Length < 4
                || pattern[0] != '*'
                || pattern[1] != '*'
                || pattern[2] != '/')
            {
                return false;
            }

            ReadOnlySpan<char> segment = pattern[3..];
            if (segment.IndexOf('/') >= 0
                || !TryCreateSegmentMatcher(segment, ignoreCase, out GlobStrategy? segmentMatcher))
            {
                return false;
            }

            result = new GlobStarFileNameStrategy(segmentMatcher);
            return true;
        }

        private static bool TryCreateSegmentMatcher(
            ReadOnlySpan<char> segment,
            bool ignoreCase,
            [NotNullWhen(true)] out GlobStrategy? result)
        {
            int firstStar = segment.IndexOf('*');
            if (firstStar < 0)
            {
                result = new LiteralGlobStrategy(segment.ToString(), ignoreCase);
                return true;
            }

            int starCount = 0;
            for (int index = firstStar; index < segment.Length; index++)
            {
                if (segment[index] == '*')
                {
                    starCount++;
                }
            }

            if (starCount == segment.Length)
            {
                result = new AnyGlobStrategy();
                return true;
            }

            if (starCount == 1)
            {
                if (firstStar == 0)
                {
                    result = new SuffixGlobStrategy(segment[1..].ToString(), ignoreCase);
                }
                else if (firstStar == segment.Length - 1)
                {
                    result = new PrefixGlobStrategy(segment[..firstStar].ToString(), ignoreCase);
                }
                else
                {
                    result = new PrefixSuffixGlobStrategy(
                        segment[..firstStar].ToString(),
                        segment[(firstStar + 1)..].ToString(),
                        ignoreCase);
                }

                return true;
            }

            if (starCount == 2 && firstStar == 0 && segment[^1] == '*')
            {
                result = new ContainsGlobStrategy(segment[1..^1].ToString(), ignoreCase);
                return true;
            }

            result = null;
            return false;
        }

        [SkipLocalsInit]
        private static bool TryEncodeProgram(
            ReadOnlySpan<char> pattern,
            out string program,
            out bool hasGlobStar)
        {
            ValueStringBuilder builder = new(stackalloc char[256]);
            LiteralCursor lastLiteral = LiteralCursor.None;
            hasGlobStar = false;
            int overflowPosition = -1;

            int index = 0;
            while (index < pattern.Length)
            {
                if (pattern[index] == '*')
                {
                    int runEnd = index + 1;
                    while (runEnd < pattern.Length && pattern[runEnd] == '*')
                    {
                        runEnd++;
                    }

                    if (TryEmitGlobStar(pattern, index, runEnd, ref builder, ref lastLiteral, out int next))
                    {
                        hasGlobStar = true;
                        index = next;
                        continue;
                    }

                    builder.Append(GlobOpCodes.AnyRun);
                    lastLiteral = LiteralCursor.None;
                    index = runEnd;
                    continue;
                }

                int literalStart = index;
                if (!TryEmitLiteralRun(pattern, ref index, ref builder, out lastLiteral))
                {
                    overflowPosition = literalStart;
                    break;
                }
            }

            if (overflowPosition >= 0)
            {
                builder.Dispose();
                program = string.Empty;
                return false;
            }

            program = builder.ToString();
            return true;
        }

        private static bool TryEmitGlobStar(
            ReadOnlySpan<char> pattern,
            int index,
            int runEnd,
            ref ValueStringBuilder builder,
            ref LiteralCursor lastLiteral,
            out int next)
        {
            next = runEnd;
            if (runEnd - index < 2
                || (index != 0 && pattern[index - 1] != '/')
                || (runEnd != pattern.Length && pattern[runEnd] != '/'))
            {
                return false;
            }

            int flags = 0;
            if (index > 0)
            {
                flags |= GlobOpCodes.GlobStarFlagLead;
            }

            if (runEnd < pattern.Length)
            {
                flags |= GlobOpCodes.GlobStarFlagTrail;
                next++;
            }

            if (index > 0)
            {
                StripTrailingSeparatorFromLastLiteral(ref builder, ref lastLiteral);
            }

            builder.Append(GlobOpCodes.GlobStar);
            builder.Append((char)flags);
            lastLiteral = LiteralCursor.None;
            return true;
        }

        private static void StripTrailingSeparatorFromLastLiteral(
            ref ValueStringBuilder builder,
            ref LiteralCursor lastLiteral)
        {
            Debug.Assert(lastLiteral.Start >= 0 && lastLiteral.Length > 0);
            Debug.Assert(builder[^1] == '/');

            if (lastLiteral.Length == 1)
            {
                builder.Length = lastLiteral.Start;
                lastLiteral = LiteralCursor.None;
            }
            else
            {
                builder.Length--;
                lastLiteral.Length--;
                builder[lastLiteral.Start + 1] = (char)lastLiteral.Length;
            }
        }

        private static bool TryEmitLiteralRun(
            ReadOnlySpan<char> pattern,
            ref int index,
            ref ValueStringBuilder builder,
            out LiteralCursor lastLiteral)
        {
            int literalStart = builder.Length;
            builder.Append(GlobOpCodes.Literal);
            builder.Append('\0');
            int literalLength = 0;

            while (index < pattern.Length && pattern[index] != '*')
            {
                builder.Append(pattern[index]);
                literalLength++;
                index++;
            }

            if (literalLength > MaxOpcodeBodyLength)
            {
                lastLiteral = LiteralCursor.None;
                return false;
            }

            builder[literalStart + 1] = (char)literalLength;
            lastLiteral = new LiteralCursor { Start = literalStart, Length = literalLength };
            return true;
        }

        private static void FindTrailingLiteral(
            string program,
            out int nfaProgramLength,
            out int tailStart,
            out int tailLength)
        {
            ReadOnlySpan<char> span = program;
            int index = 0;
            int lastOpcodeStart = -1;
            char lastOpcode = '\0';
            int lastOpcodeLength = 0;

            while (index < span.Length)
            {
                lastOpcodeStart = index;
                lastOpcode = span[index];
                if (lastOpcode == GlobOpCodes.AnyRun)
                {
                    index++;
                }
                else if (lastOpcode == GlobOpCodes.GlobStar)
                {
                    index += 2;
                }
                else
                {
                    Debug.Assert(lastOpcode == GlobOpCodes.Literal);
                    lastOpcodeLength = span[index + 1];
                    index += 2 + lastOpcodeLength;
                }
            }

            if (lastOpcode == GlobOpCodes.Literal)
            {
                nfaProgramLength = lastOpcodeStart;
                tailStart = lastOpcodeStart + 2;
                tailLength = lastOpcodeLength;
            }
            else
            {
                nfaProgramLength = program.Length;
                tailStart = -1;
                tailLength = 0;
            }
        }
    }
}
