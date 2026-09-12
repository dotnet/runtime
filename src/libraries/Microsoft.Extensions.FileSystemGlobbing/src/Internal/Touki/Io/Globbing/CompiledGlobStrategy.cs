// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Copyright (c) 2025 Jeremy W Kuhne
// SPDX-License-Identifier: MIT
// See LICENSE file in the project root for full license information

namespace Touki.Io.Globbing;

// <summary>
//  General-purpose glob matcher used for patterns that don't fit a specialized shape.
//  The pattern is encoded into a single program string interpreted at match time;
//  matching is allocation-free.
// </summary>
// <remarks>
//  <para>
//   Program encoding (see <see cref="GlobOpCodes"/>):
//  </para>
//  <para>
//   - <c>AnyRun</c>: matches zero or more characters.<br/>
//   - <c>Literal</c> followed by <c>&lt;len&gt;&lt;chars&gt;</c>: matches the literal run.<br/>
//   - <c>GlobStar</c> followed by its flags: matches zero or more path segments.
//  </para>
//  <para>
//   Matching backtracks over the most recent <c>AnyRun</c> and <c>GlobStar</c>
//   savepoints. The ordinal and ignore-case paths are compiled into separate static
//   methods so the case branch is hoisted out of the hot loop.
//  </para>
// </remarks>
internal sealed partial class CompiledGlobStrategy : GlobStrategy
{
    private readonly string _program;
    private readonly int _nfaProgramLength;
    private readonly int _tailStart;
    private readonly int _tailLength;

    // <summary>
    //  Compile-time globstar presence, used to select the smaller single-wildcard
    //  loop when recursive matching is unnecessary.
    // </summary>
    private readonly bool _hasGlobStar;

    // <summary>
    //  Constructs a matcher with a trailing-literal anchor. <paramref name="nfaProgramLength"/>
    //  is the length of the program portion run by the NFA (excludes the trailing Literal
    //  op header and payload); <paramref name="tailStart"/> and <paramref name="tailLength"/>
    //  identify the tail characters within <paramref name="program"/>.
    //  <paramref name="hasGlobStar"/> records whether the program contains globstar.
    // </summary>
    public CompiledGlobStrategy(
        string program,
        int nfaProgramLength,
        int tailStart,
        int tailLength,
        bool hasGlobStar,
        bool ignoreCase)
        : base(ignoreCase)
    {
        _program = program;
        _nfaProgramLength = nfaProgramLength;
        _tailStart = tailStart;
        _tailLength = tailLength;
        _hasGlobStar = hasGlobStar;
    }

    // <inheritdoc/>
    // <remarks>
    //  <para>
    //   Walks the virtual concatenation <paramref name="directoryPrefix"/> +
    //   <paramref name="fileName"/> without copying. Per-char access goes through an
    //   inline branch; literal-segment comparisons go through
    //   <see cref="LiteralMatchesAt"/>, which splits the literal at the span boundary
    //   so each half uses the vectorized <see cref="MemoryExtensions"/>.<c>SequenceEqual</c>
    //   on a contiguous slice. The caller hands in <paramref name="directoryPrefix"/>
    //   already separator-translated and separator-terminated (per the
    //   <see cref="GlobStrategy.MatchCore"/> contract), so the boundary at
    //   <c>directoryPrefix.Length</c> lines up exactly with the directory/file-name
    //   split in the bytecode program.
    //  </para>
    // </remarks>
    internal override bool MatchCore(
        ReadOnlySpan<char> directoryPrefix,
        ReadOnlySpan<char> fileName)
    {
        ReadOnlySpan<char> first = directoryPrefix;
        ReadOnlySpan<char> second = fileName;
        int firstLength = first.Length;
        int totalLength = firstLength + second.Length;

        // Tail-anchor fast-fail. When the encoded program ends in a Literal op, the
        // factory pre-extracts that literal so we can verify it with a single
        // contiguous compare (vectorized when not straddling) before running the NFA.
        //
        // The Literal is the program's last opcode, so matching the tail also
        // consumes it: trim the tail and run the bytecode loop on the prefix.
        if (_tailLength > 0)
        {
            if (totalLength < _tailLength)
            {
                return false;
            }

            ReadOnlySpan<char> tail = _program.AsSpan(_tailStart, _tailLength);
            int tailStart = totalLength - _tailLength;
            if (!LiteralMatchesAt(first, second, tailStart, tail, IgnoreCase))
            {
                return false;
            }

            int trimmed = totalLength - _tailLength;
            if (trimmed >= firstLength)
            {
                second = second[..(trimmed - firstLength)];
            }
            else
            {
                first = first[..trimmed];
                second = default;
            }

        }

        ReadOnlySpan<char> program = _program.AsSpan(0, _nfaProgramLength);

        if (!IgnoreCase)
        {
            return _hasGlobStar
                ? MatchOrdinal(first, second, program, Separator)
                : MatchOrdinalSimple(first, second, program, Separator);
        }

        return _hasGlobStar
            ? MatchIgnoreCase(first, second, program, Separator)
            : MatchIgnoreCaseSimple(first, second, program, Separator);
    }

    // <summary>
    //  Compares the virtual <paramref name="first"/> + <paramref name="second"/> slice
    //  starting at <paramref name="inputIndex"/> against <paramref name="literal"/> under
    //  the matcher's case-fold rule. Splits the literal at the span boundary so each
    //  half stays contiguous on its source span and uses vectorized routines.
    // </summary>
    private static bool LiteralMatchesAt(
        ReadOnlySpan<char> first,
        ReadOnlySpan<char> second,
        int inputIndex,
        ReadOnlySpan<char> literal,
        bool ignoreCase)
    {
        int firstLength = first.Length;
        int literalLength = literal.Length;

        if (inputIndex + literalLength <= firstLength)
        {
            return LiteralMatch(first.Slice(inputIndex, literalLength), literal, ignoreCase);
        }

        if (inputIndex >= firstLength)
        {
            return LiteralMatch(second.Slice(inputIndex - firstLength, literalLength), literal, ignoreCase);
        }

        int leftLength = firstLength - inputIndex;
        return LiteralMatch(first.Slice(inputIndex, leftLength), literal[..leftLength], ignoreCase)
            && LiteralMatch(second[..(literalLength - leftLength)], literal[leftLength..], ignoreCase);
    }

    // <summary>
    //  Ordinal match path for programs without <see cref="GlobOpCodes.GlobStar"/>.
    //  Single AnyRun savepoint, no shared backtrack helper - keeps the JIT happy and
    //  avoids the two-slot backtrack overhead of <see cref="MatchOrdinal"/> on the
    //  globstar-free common case. Walks the virtual
    //  <paramref name="first"/> + <paramref name="second"/> concatenation.
    // </summary>
    private static bool MatchOrdinalSimple(
        ReadOnlySpan<char> first,
        ReadOnlySpan<char> second,
        ReadOnlySpan<char> program,
        char separator)
    {
        int firstLength = first.Length;
        int totalLength = firstLength + second.Length;
        int programIndex = 0;
        int inputIndex = 0;
        int anyRunProgramIndex = -1;
        int anyRunInputIndex = 0;

        while (inputIndex < totalLength)
        {
            if (programIndex < program.Length)
            {
                char opcode = program[programIndex];

                if (opcode == GlobOpCodes.AnyRun)
                {
                    anyRunProgramIndex = programIndex;
                    anyRunInputIndex = inputIndex;
                    programIndex++;
                    continue;
                }

                if (opcode == GlobOpCodes.Literal)
                {
                    int literalLength = program[programIndex + 1];
                    if (inputIndex + literalLength <= totalLength
                        && LiteralMatchesAt(first, second, inputIndex, program.Slice(programIndex + 2, literalLength), ignoreCase: false))
                    {
                        inputIndex += literalLength;
                        programIndex += 2 + literalLength;
                        continue;
                    }
                }
            }

            if (anyRunProgramIndex >= 0)
            {
                // Path-aware AnyRun cannot extend across the separator.
                if (separator != '\0' && anyRunInputIndex < totalLength)
                {
                    char anyRunChar = anyRunInputIndex < firstLength
                        ? first[anyRunInputIndex]
                        : second[anyRunInputIndex - firstLength];
                    if (anyRunChar == separator)
                    {
                        return false;
                    }
                }

                programIndex = anyRunProgramIndex + 1;
                anyRunInputIndex++;
                inputIndex = anyRunInputIndex;
                continue;
            }

            return false;
        }

        while (programIndex < program.Length && program[programIndex] == GlobOpCodes.AnyRun)
        {
            programIndex++;
        }

        return programIndex == program.Length;
    }

    // <summary>
    //  Ignore-case companion to <see cref="MatchOrdinalSimple"/>.
    // </summary>
    private static bool MatchIgnoreCaseSimple(
        ReadOnlySpan<char> first,
        ReadOnlySpan<char> second,
        ReadOnlySpan<char> program,
        char separator)
    {
        int firstLength = first.Length;
        int totalLength = firstLength + second.Length;
        int programIndex = 0;
        int inputIndex = 0;
        int anyRunProgramIndex = -1;
        int anyRunInputIndex = 0;

        while (inputIndex < totalLength)
        {
            if (programIndex < program.Length)
            {
                char opcode = program[programIndex];

                if (opcode == GlobOpCodes.AnyRun)
                {
                    anyRunProgramIndex = programIndex;
                    anyRunInputIndex = inputIndex;
                    programIndex++;
                    continue;
                }

                if (opcode == GlobOpCodes.Literal)
                {
                    int literalLength = program[programIndex + 1];
                    if (inputIndex + literalLength <= totalLength
                        && LiteralMatchesAt(first, second, inputIndex, program.Slice(programIndex + 2, literalLength), ignoreCase: true))
                    {
                        inputIndex += literalLength;
                        programIndex += 2 + literalLength;
                        continue;
                    }
                }
            }

            if (anyRunProgramIndex >= 0)
            {
                if (separator != '\0' && anyRunInputIndex < totalLength)
                {
                    char anyRunChar = anyRunInputIndex < firstLength
                        ? first[anyRunInputIndex]
                        : second[anyRunInputIndex - firstLength];
                    if (anyRunChar == separator)
                    {
                        return false;
                    }
                }

                programIndex = anyRunProgramIndex + 1;
                anyRunInputIndex++;
                inputIndex = anyRunInputIndex;
                continue;
            }

            return false;
        }

        while (programIndex < program.Length && program[programIndex] == GlobOpCodes.AnyRun)
        {
            programIndex++;
        }

        return programIndex == program.Length;
    }

    // <summary>
    //  Ordinal match path. Walks the virtual <paramref name="first"/> +
    //  <paramref name="second"/> concatenation; uses <see cref="LiteralMatchesAt"/>
    //  for literal runs (vectorized when not straddling the span boundary).
    // </summary>
    private static bool MatchOrdinal(
        ReadOnlySpan<char> first,
        ReadOnlySpan<char> second,
        ReadOnlySpan<char> program,
        char separator)
    {
        BacktrackState state = default;
        state.AnyRunProgramIndex = -1;
        state.GlobStarProgramIndex = -1;

        int firstLength = first.Length;
        int totalLength = firstLength + second.Length;

        while (state.InputIndex < totalLength)
        {
            if (state.ProgramIndex < program.Length)
            {
                char opcode = program[state.ProgramIndex];

                if (opcode == GlobOpCodes.AnyRun)
                {
                    state.AnyRunProgramIndex = state.ProgramIndex;
                    state.AnyRunInputIndex = state.InputIndex;
                    state.ProgramIndex++;
                    continue;
                }

                if (opcode == GlobOpCodes.GlobStar)
                {
                    int flags = program[state.ProgramIndex + 1];
                    int absorbedLength = FirstValidGlobStarLength(first, second, state.InputIndex, flags, separator);
                    if (absorbedLength >= 0)
                    {
                        if (state.AnyRunProgramIndex > state.ProgramIndex)
                        {
                            state.AnyRunProgramIndex = -1;
                        }

                        state.GlobStarProgramIndex = state.ProgramIndex;
                        state.GlobStarInitialInput = state.InputIndex;
                        state.GlobStarInputIndex = state.InputIndex + absorbedLength;
                        state.GlobStarFlags = flags;
                        state.ProgramIndex += 2;
                        state.InputIndex = state.GlobStarInputIndex;
                        continue;
                    }
                }
                else
                {
                    if (opcode == GlobOpCodes.Literal)
                    {
                        int length = program[state.ProgramIndex + 1];
                        if (state.InputIndex + length <= totalLength
                            && LiteralMatchesAt(first, second, state.InputIndex, program.Slice(state.ProgramIndex + 2, length), ignoreCase: false))
                        {
                            state.InputIndex += length;
                            state.ProgramIndex += 2 + length;
                            continue;
                        }
                    }
                }
            }

            if (!Backtrack(first, second, separator, ref state))
            {
                return false;
            }
        }

        return ConsumeTrailingEmpty(program, state.ProgramIndex);
    }

    // <summary>
    //  Ordinal-ignore-case companion to <see cref="MatchOrdinal"/>.
    // </summary>
    private static bool MatchIgnoreCase(
        ReadOnlySpan<char> first,
        ReadOnlySpan<char> second,
        ReadOnlySpan<char> program,
        char separator)
    {
        BacktrackState state = default;
        state.AnyRunProgramIndex = -1;
        state.GlobStarProgramIndex = -1;

        int firstLength = first.Length;
        int totalLength = firstLength + second.Length;

        while (state.InputIndex < totalLength)
        {
            if (state.ProgramIndex < program.Length)
            {
                char opcode = program[state.ProgramIndex];

                if (opcode == GlobOpCodes.AnyRun)
                {
                    state.AnyRunProgramIndex = state.ProgramIndex;
                    state.AnyRunInputIndex = state.InputIndex;
                    state.ProgramIndex++;
                    continue;
                }

                if (opcode == GlobOpCodes.GlobStar)
                {
                    int flags = program[state.ProgramIndex + 1];
                    int absorbedLength = FirstValidGlobStarLength(first, second, state.InputIndex, flags, separator);
                    if (absorbedLength >= 0)
                    {
                        if (state.AnyRunProgramIndex > state.ProgramIndex)
                        {
                            state.AnyRunProgramIndex = -1;
                        }

                        state.GlobStarProgramIndex = state.ProgramIndex;
                        state.GlobStarInitialInput = state.InputIndex;
                        state.GlobStarInputIndex = state.InputIndex + absorbedLength;
                        state.GlobStarFlags = flags;
                        state.ProgramIndex += 2;
                        state.InputIndex = state.GlobStarInputIndex;
                        continue;
                    }
                }
                else
                {
                    if (opcode == GlobOpCodes.Literal)
                    {
                        int length = program[state.ProgramIndex + 1];
                        if (state.InputIndex + length <= totalLength
                            && LiteralMatchesAt(first, second, state.InputIndex, program.Slice(state.ProgramIndex + 2, length), ignoreCase: true))
                        {
                            state.InputIndex += length;
                            state.ProgramIndex += 2 + length;
                            continue;
                        }
                    }
                }
            }

            if (!Backtrack(first, second, separator, ref state))
            {
                return false;
            }
        }

        return ConsumeTrailingEmpty(program, state.ProgramIndex);
    }

    // <summary>
    //  Consumes trailing ops whose empty match is valid (<see cref="GlobOpCodes.AnyRun"/>
    //  and any <see cref="GlobOpCodes.GlobStar"/> that is not <c>GS_LT</c>). Returns
    //  <see langword="true"/> iff the program is fully consumed.
    // </summary>
    private static bool ConsumeTrailingEmpty(ReadOnlySpan<char> program, int programIndex)
    {
        while (programIndex < program.Length)
        {
            char opcode = program[programIndex];
            if (opcode == GlobOpCodes.AnyRun)
            {
                programIndex++;
                continue;
            }

            if (opcode == GlobOpCodes.GlobStar)
            {
                int flags = program[programIndex + 1];
                // GS_LT requires a non-empty absorbed slice; empty match invalid.
                if ((flags & GlobOpCodes.GlobStarFlagLead) != 0
                    && (flags & GlobOpCodes.GlobStarFlagTrail) != 0)
                {
                    return false;
                }

                programIndex += 2;
                continue;
            }

            return false;
        }

        return true;
    }

    // <summary>
    //  Backtracks to whichever savepoint (AnyRun or GlobStar) is more recent in
    //  program flow; on exhaustion, falls through to the other. Returns
    //  <see langword="false"/> when both slots are exhausted.
    // </summary>
    private static bool Backtrack(
        ReadOnlySpan<char> first,
        ReadOnlySpan<char> second,
        char separator,
        ref BacktrackState state)
    {
        int firstLength = first.Length;
        int totalLength = firstLength + second.Length;

        while (true)
        {
            bool tryGlobStar = state.GlobStarProgramIndex >= 0
                && (state.AnyRunProgramIndex < 0 || state.GlobStarProgramIndex >= state.AnyRunProgramIndex);

            if (tryGlobStar)
            {
                int currentAbsorbed = state.GlobStarInputIndex - state.GlobStarInitialInput;
                int nextAbsorbed = NextValidGlobStarLength(
                    first,
                    second,
                    state.GlobStarInitialInput,
                    currentAbsorbed,
                    state.GlobStarFlags,
                    separator);
                if (nextAbsorbed < 0)
                {
                    state.GlobStarProgramIndex = -1;
                    continue;
                }

                state.GlobStarInputIndex = state.GlobStarInitialInput + nextAbsorbed;
                state.ProgramIndex = state.GlobStarProgramIndex + 2;
                state.InputIndex = state.GlobStarInputIndex;
                return true;
            }

            if (state.AnyRunProgramIndex >= 0)
            {
                // Path-aware AnyRun cannot extend across the separator.
                if (separator != '\0' && state.AnyRunInputIndex < totalLength)
                {
                    char anyRunChar = state.AnyRunInputIndex < firstLength
                        ? first[state.AnyRunInputIndex]
                        : second[state.AnyRunInputIndex - firstLength];
                    if (anyRunChar == separator)
                    {
                        state.AnyRunProgramIndex = -1;
                        continue;
                    }
                }

                state.AnyRunInputIndex++;
                if (state.AnyRunInputIndex > totalLength)
                {
                    state.AnyRunProgramIndex = -1;
                    continue;
                }

                if (state.GlobStarProgramIndex > state.AnyRunProgramIndex)
                {
                    state.GlobStarProgramIndex = -1;
                }

                state.ProgramIndex = state.AnyRunProgramIndex + 1;
                state.InputIndex = state.AnyRunInputIndex;
                return true;
            }

            return false;
        }
    }

    // <summary>
    //  Returns the smallest valid absorbed length at which a
    //  <see cref="GlobOpCodes.GlobStar"/> with the given flag bits may commit, given the
    //  absorbed input slice <c>input[initial..initial+length]</c>. Returns <c>-1</c> when
    //  no valid length exists (e.g., <c>GS_LT</c> at a position where the input character
    //  at <c>initial</c> is not the separator).
    // </summary>
    private static int FirstValidGlobStarLength(
        ReadOnlySpan<char> first,
        ReadOnlySpan<char> second,
        int initialInputIndex,
        int flags,
        char separator)
    {
        bool hasLead = (flags & GlobOpCodes.GlobStarFlagLead) != 0;
        bool hasTrail = (flags & GlobOpCodes.GlobStarFlagTrail) != 0;
        int firstLength = first.Length;
        int totalLength = firstLength + second.Length;

        if (hasLead && hasTrail)
        {
            if (initialInputIndex < totalLength)
            {
                char inputChar = initialInputIndex < firstLength
                    ? first[initialInputIndex]
                    : second[initialInputIndex - firstLength];
                if (inputChar == separator)
                {
                    return 1;
                }
            }

            return -1;
        }

        // GS_None / GS_R / GS_L: empty match (length 0) is always valid.
        return 0;
    }

    // <summary>
    //  Returns the smallest valid absorbed length greater than
    //  <paramref name="currentAbsorbed"/> for a <see cref="GlobOpCodes.GlobStar"/>
    //  backtrack, or <c>-1</c> when exhausted.
    // </summary>
    private static int NextValidGlobStarLength(
        ReadOnlySpan<char> first,
        ReadOnlySpan<char> second,
        int initialInputIndex,
        int currentAbsorbed,
        int flags,
        char separator)
    {
        bool hasLead = (flags & GlobOpCodes.GlobStarFlagLead) != 0;
        bool hasTrail = (flags & GlobOpCodes.GlobStarFlagTrail) != 0;
        int firstLength = first.Length;
        int totalLength = firstLength + second.Length;
        int maxAbsorbed = totalLength - initialInputIndex;

        if (hasTrail)
        {
            // (GS_R or GS_LT.) Need length > currentAbsorbed with the input character at
            // (initial + length - 1) equal to the separator. Scan upward.
            for (int position = initialInputIndex + currentAbsorbed; position < totalLength; position++)
            {
                char inputChar = position < firstLength
                    ? first[position]
                    : second[position - firstLength];
                if (inputChar == separator)
                {
                    return position - initialInputIndex + 1;
                }
            }

            return -1;
        }

        if (hasLead)
        {
            // GS_L. Length 0 was the initial empty match; length >= 1 requires the input
            // character at `initial` to be the separator. Beyond length 1, no further
            // constraint.
            int next = currentAbsorbed + 1;
            if (next > maxAbsorbed)
            {
                return -1;
            }

            if (next == 1)
            {
                if (initialInputIndex < totalLength)
                {
                    char inputChar = initialInputIndex < firstLength
                        ? first[initialInputIndex]
                        : second[initialInputIndex - firstLength];
                    if (inputChar == separator)
                    {
                        return 1;
                    }
                }

                return -1;
            }

            return next;
        }

        // GS_None: no constraint.
        {
            int next = currentAbsorbed + 1;
            return next > maxAbsorbed ? -1 : next;
        }
    }

    // <summary>
    //  Selects the literal-segment compare for the active case-folding mode.
    //  Caller guarantees <c>a.Length == b.Length</c> (the NFA slices both sides to <c>length</c>).
    // </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool LiteralMatch(ReadOnlySpan<char> a, ReadOnlySpan<char> b, bool ignoreCase) =>
        ignoreCase ? a.Equals(b, StringComparison.OrdinalIgnoreCase) : a.SequenceEqual(b);
}
