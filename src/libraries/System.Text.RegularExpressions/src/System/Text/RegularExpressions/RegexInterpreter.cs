// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace System.Text.RegularExpressions
{
    /// <summary>A <see cref="RegexRunnerFactory"/> for creating <see cref="RegexInterpreter"/>s.</summary>
    internal sealed class RegexInterpreterFactory(RegexTree tree) : RegexRunnerFactory
    {
        /// <summary>The RegexInterpretedCode for the RegexTree and the specified culture.</summary>
        private readonly RegexInterpreterCode _code = RegexWriter.Write(tree);
        /// <summary>
        /// CultureInfo field from the tree's culture which will only be set to an actual culture if the
        /// tree contains IgnoreCase backreferences. If the tree doesn't have IgnoreCase backreferences, then we keep _culture as null.
        /// </summary>
        private readonly CultureInfo? _culture = tree.Culture;

        protected internal override RegexRunner CreateInstance() =>
            // Create a new interpreter instance.
            new RegexInterpreter(_code, _culture);
    }

    /// <summary>Executes a block of regular expression codes while consuming input.</summary>
    internal sealed class RegexInterpreter : RegexRunner
    {
        private readonly RegexInterpreterCode _code;
        private readonly CultureInfo? _culture;
        private RegexCaseBehavior _caseBehavior;

        private RegexOpcode _operator;
        private int _codepos;
        private bool _rightToLeft;

        public RegexInterpreter(RegexInterpreterCode code, CultureInfo? culture)
        {
            Debug.Assert(code != null, "code must not be null.");

            _code = code;
            _culture = culture;
        }

        protected override void InitTrackCount() => runtrackcount = _code.TrackCount;

        private void Advance(int i)
        {
            _codepos += i + 1;
            SetOperator((RegexOpcode)_code.Codes[_codepos]);
        }

        private void Goto(int newpos)
        {
            // When branching backward, ensure storage.
            if (newpos < _codepos)
            {
                EnsureStorage();
            }

            _codepos = newpos;
            SetOperator((RegexOpcode)_code.Codes[newpos]);
        }

        private void Trackto(int newpos) => runtrackpos = runtrack!.Length - newpos;

        private int Trackpos() => runtrack!.Length - runtrackpos;

        /// <summary>Push onto the backtracking stack.</summary>
        private void TrackPush() => runtrack![--runtrackpos] = _codepos;

        private void TrackPush(int i1)
        {
            int[] localruntrack = runtrack!;
            int localruntrackpos = runtrackpos;

            localruntrack[--localruntrackpos] = i1;
            localruntrack[--localruntrackpos] = _codepos;

            runtrackpos = localruntrackpos;
        }

        private void TrackPush(int i1, int i2)
        {
            int[] localruntrack = runtrack!;
            int localruntrackpos = runtrackpos;

            localruntrack[--localruntrackpos] = i1;
            localruntrack[--localruntrackpos] = i2;
            localruntrack[--localruntrackpos] = _codepos;

            runtrackpos = localruntrackpos;
        }

        private void TrackPush(int i1, int i2, int i3)
        {
            int[] localruntrack = runtrack!;
            int localruntrackpos = runtrackpos;

            localruntrack[--localruntrackpos] = i1;
            localruntrack[--localruntrackpos] = i2;
            localruntrack[--localruntrackpos] = i3;
            localruntrack[--localruntrackpos] = _codepos;

            runtrackpos = localruntrackpos;
        }

        private void TrackPush2(int i1)
        {
            int[] localruntrack = runtrack!;
            int localruntrackpos = runtrackpos;

            localruntrack[--localruntrackpos] = i1;
            localruntrack[--localruntrackpos] = -_codepos;

            runtrackpos = localruntrackpos;
        }

        private void TrackPush2(int i1, int i2)
        {
            int[] localruntrack = runtrack!;
            int localruntrackpos = runtrackpos;

            localruntrack[--localruntrackpos] = i1;
            localruntrack[--localruntrackpos] = i2;
            localruntrack[--localruntrackpos] = -_codepos;

            runtrackpos = localruntrackpos;
        }

        private void Backtrack()
        {
            CheckTimeout(); // to ensure that any backtracking operation has a timeout check

            int newpos = runtrack![runtrackpos];
            runtrackpos++;

            int back = (int)RegexOpcode.Backtracking;
            if (newpos < 0)
            {
                newpos = -newpos;
                back = (int)RegexOpcode.BacktrackingSecond;
            }
            SetOperator((RegexOpcode)(_code.Codes[newpos] | back));

            // When branching backward, ensure storage.
            if (newpos < _codepos)
            {
                EnsureStorage();
            }

            _codepos = newpos;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SetOperator(RegexOpcode op)
        {
            _operator = op & ~RegexOpcode.RightToLeft;
            _rightToLeft = (op & RegexOpcode.RightToLeft) != 0;
        }

        private void TrackPop() => runtrackpos++;

        /// <summary>Pop framesize items from the backtracking stack.</summary>
        private void TrackPop(int framesize) => runtrackpos += framesize;

        /// <summary>Peek at the item popped from the stack.</summary>
        /// <remarks>
        /// If you want to get and pop the top item from the stack, you do `TrackPop(); TrackPeek();`.
        /// </remarks>
        private int TrackPeek() => runtrack![runtrackpos - 1];

        /// <summary>Get the ith element down on the backtracking stack.</summary>
        private int TrackPeek(int i) => runtrack![runtrackpos - i - 1];

        /// <summary>Push onto the grouping stack.</summary>
        private void StackPush(int i1) => runstack![--runstackpos] = i1;

        private void StackPush(int i1, int i2)
        {
            int[] localrunstack = runstack!;
            int localrunstackpos = runstackpos;

            localrunstack[--localrunstackpos] = i1;
            localrunstack[--localrunstackpos] = i2;

            runstackpos = localrunstackpos;
        }

        private void StackPop() => runstackpos++;

        // pop framesize items from the grouping stack
        private void StackPop(int framesize) => runstackpos += framesize;

        /// <summary>
        /// Technically we are actually peeking at items already popped.  So if you want to
        /// get and pop the top item from the stack, you do `StackPop(); StackPeek();`.
        /// </summary>
        private int StackPeek() => runstack![runstackpos - 1];

        /// <summary>Get the ith element down on the grouping stack.</summary>
        private int StackPeek(int i) => runstack![runstackpos - i - 1];

        private int Operand(int i) => _code.Codes[_codepos + i + 1];

        private int Bump() => _rightToLeft ? -1 : 1;

        private int Forwardchars() => _rightToLeft ? runtextpos : runtextend - runtextpos;

        private char Forwardcharnext(ReadOnlySpan<char> inputSpan)
        {
            int i = _rightToLeft ? --runtextpos : runtextpos++;
            return inputSpan[i];
        }

        private bool MatchString(string str, ReadOnlySpan<char> inputSpan)
        {
            int c = str.Length;
            int pos;

            if (!_rightToLeft)
            {
                if (inputSpan.Length - runtextpos < c)
                {
                    return false;
                }

                pos = runtextpos + c;
            }
            else
            {
                if (runtextpos < c)
                {
                    return false;
                }

                pos = runtextpos;
            }

            while (c != 0)
            {
                if (str[--c] != inputSpan[--pos])
                {
                    return false;
                }
            }

            if (!_rightToLeft)
            {
                pos += str.Length;
            }

            runtextpos = pos;

            return true;
        }

        private bool MatchRef(int index, int length, ReadOnlySpan<char> inputSpan, bool caseInsensitive)
        {
            int pos;
            if (!_rightToLeft)
            {
                if (inputSpan.Length - runtextpos < length)
                {
                    return false;
                }

                pos = runtextpos + length;
            }
            else
            {
                if (runtextpos < length)
                {
                    return false;
                }

                pos = runtextpos;
            }

            int cmpos = index + length;
            int c = length;

            if (!caseInsensitive)
            {
                while (c-- != 0)
                {
                    if (inputSpan[--cmpos] != inputSpan[--pos])
                    {
                        return false;
                    }
                }
            }
            else
            {
                while (c-- != 0)
                {
                    char backreferenceChar = inputSpan[--cmpos];
                    char currentChar = inputSpan[--pos];

                    // If we are evaluating a backreference case-insensitive match, we first check if the characters at the position
                    // are the same character.
                    if (backreferenceChar != currentChar)
                    {
                        // If they are not the same character, then we need to check if the backreference character participates in case conversion
                        // and if so, we need to fetch the case equivalences from our casing tables.
                        Debug.Assert(_culture != null, "If the pattern has backreferences and is IgnoreCase, then _culture must not be null.");
                        if (!RegexCaseEquivalences.TryFindCaseEquivalencesForCharWithIBehavior(backreferenceChar, _culture, ref _caseBehavior, out ReadOnlySpan<char> equivalences) ||
                            equivalences.IndexOf(inputSpan[pos]) < 0)
                        {
                            // The backreference character doesn't participate in case conversions, or it does but the input character
                            // doesn't match any of its equivalents.  Either way, we fail to match.
                            return false;
                        }
                    }
                }
            }

            if (!_rightToLeft)
            {
                pos += length;
            }

            runtextpos = pos;

            return true;
        }

        private void Backwardnext() => runtextpos += _rightToLeft ? 1 : -1;

        protected internal override void Scan(ReadOnlySpan<char> text)
        {
            Debug.Assert(runregex is not null);
            Debug.Assert(runtrack is not null);
            Debug.Assert(runstack is not null);
            Debug.Assert(runcrawl is not null);

            if (runregex.RightToLeft)
            {
                while (_code.FindOptimizations.TryFindNextStartingPositionRightToLeft(text, ref runtextpos, runtextstart))
                {
                    CheckTimeout();

                    if (TryMatchAtCurrentPosition(text) || runtextpos == 0)
                    {
                        return;
                    }

                    // Reset state for another iteration.
                    runtrackpos = runtrack.Length;
                    runstackpos = runstack.Length;
                    runcrawlpos = runcrawl.Length;
                    runtextpos--;
                }
            }
            else
            {
                while (_code.FindOptimizations.TryFindNextStartingPositionLeftToRight(text, ref runtextpos, runtextstart))
                {
                    CheckTimeout();

                    if (TryMatchAtCurrentPosition(text) || runtextpos == text.Length)
                    {
                        return;
                    }

                    // Reset state for another iteration.
                    runtrackpos = runtrack.Length;
                    runstackpos = runstack.Length;
                    runcrawlpos = runcrawl.Length;
                    runtextpos++;
                }
            }
        }

        private bool TryMatchAtCurrentPosition(ReadOnlySpan<char> inputSpan)
        {
            SetOperator((RegexOpcode)_code.Codes[0]);
            _codepos = 0;
            int advance = -1;

            while (true)
            {
                if (advance >= 0)
                {
                    // Single common Advance call to reduce method size; and single method inline point.
                    // Details at https://github.com/dotnet/corefx/pull/25096.
                    Advance(advance);
                    advance = -1;
                }

                switch (_operator)
                {
                    case RegexOpcode.Stop:
                        return runmatch!.FoundMatch;

                    case RegexOpcode.Nothing:
                        break;

                    case RegexOpcode.Goto:
                        Goto(Operand(0));
                        continue;

                    case RegexOpcode.TestBackreference:
                        if (!IsMatched(Operand(0)))
                        {
                            break;
                        }
                        advance = 1;
                        continue;

                    case RegexOpcode.Lazybranch:
                        TrackPush(runtextpos);
                        advance = 1;
                        continue;

                    case RegexOpcode.Lazybranch | RegexOpcode.Backtracking:
                        TrackPop();
                        runtextpos = TrackPeek();
                        Goto(Operand(0));
                        continue;

                    case RegexOpcode.Setmark:
                        StackPush(runtextpos);
                        TrackPush();
                        advance = 0;
                        continue;

                    case RegexOpcode.Nullmark:
                        StackPush(-1);
                        TrackPush();
                        advance = 0;
                        continue;

                    case RegexOpcode.Setmark | RegexOpcode.Backtracking:
                    case RegexOpcode.Nullmark | RegexOpcode.Backtracking:
                        StackPop();
                        break;

                    case RegexOpcode.Getmark:
                        StackPop();
                        TrackPush(StackPeek());
                        runtextpos = StackPeek();
                        advance = 0;
                        continue;

                    case RegexOpcode.Getmark | RegexOpcode.Backtracking:
                        TrackPop();
                        StackPush(TrackPeek());
                        break;

                    case RegexOpcode.Capturemark:
                        if (Operand(1) != -1 && !IsMatched(Operand(1)))
                        {
                            break;
                        }
                        StackPop();
                        if (Operand(1) != -1)
                        {
                            TransferCapture(Operand(0), Operand(1), StackPeek(), runtextpos);
                        }
                        else
                        {
                            Capture(Operand(0), StackPeek(), runtextpos);
                        }
                        TrackPush(StackPeek());
                        advance = 2;
                        continue;

                    case RegexOpcode.Capturemark | RegexOpcode.Backtracking:
                        TrackPop();
                        StackPush(TrackPeek());
                        Uncapture();
                        if (Operand(0) != -1 && Operand(1) != -1)
                        {
                            Uncapture();
                        }
                        break;

                    case RegexOpcode.Branchmark:
                        StackPop();
                        if (runtextpos != StackPeek())
                        {
                            // Nonempty match -> loop now
                            TrackPush(StackPeek(), runtextpos); // Save old mark, textpos
                            StackPush(runtextpos);              // Make new mark
                            Goto(Operand(0));                   // Loop
                        }
                        else
                        {
                            // Empty match -> straight now
                            TrackPush2(StackPeek());            // Save old mark
                            advance = 1;                        // Straight
                        }
                        continue;

                    case RegexOpcode.Branchmark | RegexOpcode.Backtracking:
                        TrackPop(2);
                        StackPop();
                        runtextpos = TrackPeek(1); // Recall position
                        TrackPush2(TrackPeek());   // Save old mark
                        advance = 1;               // Straight
                        continue;

                    case RegexOpcode.Branchmark | RegexOpcode.BacktrackingSecond:
                        TrackPop();
                        StackPush(TrackPeek()); // Recall old mark
                        break;                  // Backtrack

                    case RegexOpcode.Lazybranchmark:
                        // We hit this the first time through a lazy loop and after each
                        // successful match of the inner expression.  It simply continues
                        // on and doesn't loop.
                        StackPop();
                        {
                            int oldMarkPos = StackPeek();
                            if (runtextpos != oldMarkPos)
                            {
                                // Nonempty match -> try to loop again by going to 'back' state
                                if (oldMarkPos != -1)
                                {
                                    TrackPush(oldMarkPos, runtextpos); // Save old mark, textpos
                                }
                                else
                                {
                                    TrackPush(runtextpos, runtextpos);
                                }
                            }
                            else
                            {
                                // The inner expression found an empty match, so we'll go directly to 'back2' if we
                                // backtrack.  In this case, we need to push something on the stack, since back2 pops.
                                // However, in the case of ()+? or similar, this empty match may be legitimate, so push the text
                                // position associated with that empty match.
                                StackPush(oldMarkPos);
                                TrackPush2(StackPeek()); // Save old mark
                            }
                        }
                        advance = 1;
                        continue;

                    case RegexOpcode.Lazybranchmark | RegexOpcode.Backtracking:
                        {
                            // After the first time, Lazybranchmark | RegexOpcode.Back occurs
                            // with each iteration of the loop, and therefore with every attempted
                            // match of the inner expression.  We'll try to match the inner expression,
                            // then go back to Lazybranchmark if successful.  If the inner expression
                            // fails, we go to Lazybranchmark | RegexOpcode.Back2
                            TrackPop(2);
                            int pos = TrackPeek(1);
                            TrackPush2(TrackPeek()); // Save old mark
                            StackPush(pos);          // Make new mark
                            runtextpos = pos;        // Recall position
                            Goto(Operand(0));        // Loop
                        }
                        continue;

                    case RegexOpcode.Lazybranchmark | RegexOpcode.BacktrackingSecond:
                        // The lazy loop has failed.  We'll do a true backtrack and
                        // start over before the lazy loop.
                        StackPop();
                        TrackPop();
                        StackPush(TrackPeek()); // Recall old mark
                        break;

                    case RegexOpcode.Setcount:
                        StackPush(runtextpos, Operand(0));
                        TrackPush();
                        advance = 1;
                        continue;

                    case RegexOpcode.Nullcount:
                        StackPush(-1, Operand(0));
                        TrackPush();
                        advance = 1;
                        continue;

                    case RegexOpcode.Setcount | RegexOpcode.Backtracking:
                    case RegexOpcode.Nullcount | RegexOpcode.Backtracking:
                    case RegexOpcode.Setjump | RegexOpcode.Backtracking:
                        StackPop(2);
                        break;

                    case RegexOpcode.Branchcount:
                        // StackPush:
                        //  0: Mark
                        //  1: Count
                        StackPop(2);
                        {
                            int mark = StackPeek();
                            int count = StackPeek(1);
                            int matched = runtextpos - mark;
                            if (count >= Operand(1) || (matched == 0 && count >= 0))
                            {
                                // Max loops or empty match -> straight now
                                TrackPush2(mark, count); // Save old mark, count
                                advance = 2;             // Straight
                            }
                            else
                            {
                                // Nonempty match -> count+loop now
                                TrackPush(mark);                  // remember mark
                                StackPush(runtextpos, count + 1); // Make new mark, incr count
                                Goto(Operand(0));                 // Loop
                            }
                        }
                        continue;

                    case RegexOpcode.Branchcount | RegexOpcode.Backtracking:
                        // TrackPush:
                        //  0: Previous mark
                        // StackPush:
                        //  0: Mark (= current pos, discarded)
                        //  1: Count
                        TrackPop();
                        StackPop(2);
                        if (StackPeek(1) > 0)
                        {
                            // Positive -> can go straight
                            runtextpos = StackPeek();                  // Zap to mark
                            TrackPush2(TrackPeek(), StackPeek(1) - 1); // Save old mark, old count
                            advance = 2;                               // Straight
                            continue;
                        }
                        StackPush(TrackPeek(), StackPeek(1) - 1);      // Recall old mark, old count
                        break;

                    case RegexOpcode.Branchcount | RegexOpcode.BacktrackingSecond:
                        // TrackPush:
                        //  0: Previous mark
                        //  1: Previous count
                        TrackPop(2);
                        StackPush(TrackPeek(), TrackPeek(1)); // Recall old mark, old count
                        break;                                // Backtrack

                    case RegexOpcode.Lazybranchcount:
                        // StackPush:
                        //  0: Mark
                        //  1: Count
                        StackPop(2);
                        {
                            int mark = StackPeek();
                            int count = StackPeek(1);
                            if (count < 0)
                            {
                                // Negative count -> loop now
                                TrackPush2(mark);                 // Save old mark
                                StackPush(runtextpos, count + 1); // Make new mark, incr count
                                Goto(Operand(0));                 // Loop
                            }
                            else
                            {
                                // Nonneg count -> straight now
                                TrackPush(mark, count, runtextpos); // Save mark, count, position
                                advance = 2;                        // Straight
                            }
                        }
                        continue;

                    case RegexOpcode.Lazybranchcount | RegexOpcode.Backtracking:
                        // TrackPush:
                        //  0: Mark
                        //  1: Count
                        //  2: Textpos
                        TrackPop(3);
                        {
                            int mark = TrackPeek();
                            int textpos = TrackPeek(2);
                            if (TrackPeek(1) < Operand(1) && textpos != mark)
                            {
                                // Under limit and not empty match -> loop
                                runtextpos = textpos;                 // Recall position
                                StackPush(textpos, TrackPeek(1) + 1); // Make new mark, incr count
                                TrackPush2(mark);                     // Save old mark
                                Goto(Operand(0));                     // Loop
                                continue;
                            }
                            else
                            {
                                // Max loops or empty match -> backtrack
                                StackPush(TrackPeek(), TrackPeek(1)); // Recall old mark, count
                                break;                                // backtrack
                            }
                        }

                    case RegexOpcode.Lazybranchcount | RegexOpcode.BacktrackingSecond:
                        // TrackPush:
                        //  0: Previous mark
                        // StackPush:
                        //  0: Mark (== current pos, discarded)
                        //  1: Count
                        TrackPop();
                        StackPop(2);
                        StackPush(TrackPeek(), StackPeek(1) - 1); // Recall old mark, count
                        break;                                    // Backtrack

                    case RegexOpcode.Setjump:
                        CheckTimeout(); // to ensure that positive/negative lookarounds have a timeout check
                        StackPush(Trackpos(), Crawlpos());
                        TrackPush();
                        advance = 0;
                        continue;

                    case RegexOpcode.Backjump:
                        // StackPush:
                        //  0: Saved trackpos
                        //  1: Crawlpos
                        StackPop(2);
                        Trackto(StackPeek());
                        while (Crawlpos() != StackPeek(1))
                        {
                            Uncapture();
                        }
                        break;

                    case RegexOpcode.Forejump:
                        // StackPush:
                        //  0: Saved trackpos
                        //  1: Crawlpos
                        StackPop(2);
                        Trackto(StackPeek());
                        TrackPush(StackPeek(1));
                        advance = 0;
                        continue;

                    case RegexOpcode.Forejump | RegexOpcode.Backtracking:
                        // TrackPush:
                        //  0: Crawlpos
                        TrackPop();
                        while (Crawlpos() != TrackPeek())
                        {
                            Uncapture();
                        }
                        break;

                    case RegexOpcode.Bol:
                        {
                            int m1 = runtextpos - 1;
                            if ((uint)m1 < (uint)inputSpan.Length && inputSpan[m1] != '\n')
                            {
                                break;
                            }
                            advance = 0;
                            continue;
                        }

                    case RegexOpcode.Eol:
                        {
                            int runtextpos = this.runtextpos;
                            if ((uint)runtextpos < (uint)inputSpan.Length && inputSpan[runtextpos] != '\n')
                            {
                                break;
                            }
                            advance = 0;
                            continue;
                        }

                    case RegexOpcode.Boundary:
                        if (!IsBoundary(inputSpan, runtextpos))
                        {
                            break;
                        }
                        advance = 0;
                        continue;

                    case RegexOpcode.NonBoundary:
                        if (IsBoundary(inputSpan, runtextpos))
                        {
                            break;
                        }
                        advance = 0;
                        continue;

                    case RegexOpcode.ECMABoundary:
                        if (!IsECMABoundary(inputSpan, runtextpos))
                        {
                            break;
                        }
                        advance = 0;
                        continue;

                    case RegexOpcode.NonECMABoundary:
                        if (IsECMABoundary(inputSpan, runtextpos))
                        {
                            break;
                        }
                        advance = 0;
                        continue;

                    case RegexOpcode.Beginning:
                        if (runtextpos > 0)
                        {
                            break;
                        }
                        advance = 0;
                        continue;

                    case RegexOpcode.Start:
                        if (runtextpos != runtextstart)
                        {
                            break;
                        }
                        advance = 0;
                        continue;

                    case RegexOpcode.EndZ:
                        {
                            int runtextpos = this.runtextpos;
                            if (runtextpos < inputSpan.Length - 1 || ((uint)runtextpos < (uint)inputSpan.Length && inputSpan[runtextpos] != '\n'))
                            {
                                break;
                            }
                            advance = 0;
                            continue;
                        }

                    case RegexOpcode.End:
                        if (runtextpos < inputSpan.Length)
                        {
                            break;
                        }
                        advance = 0;
                        continue;

                    case RegexOpcode.One:
                        if (Forwardchars() < 1 || Forwardcharnext(inputSpan) != (char)Operand(0))
                        {
                            break;
                        }
                        advance = 1;
                        continue;

                    case RegexOpcode.Notone:
                        if (Forwardchars() < 1 || Forwardcharnext(inputSpan) == (char)Operand(0))
                        {
                            break;
                        }
                        advance = 1;
                        continue;

                    case RegexOpcode.Set:
                        if (Forwardchars() < 1)
                        {
                            break;
                        }
                        else
                        {
                            int operand = Operand(0);
                            if (!RegexCharClass.CharInClass(Forwardcharnext(inputSpan), _code.Strings[operand], ref _code.StringsAsciiLookup[operand]))
                            {
                                break;
                            }
                        }
                        advance = 1;
                        continue;

                    case RegexOpcode.Multi:
                        if (!MatchString(_code.Strings[Operand(0)], inputSpan))
                        {
                            break;
                        }
                        advance = 1;
                        continue;

                    case RegexOpcode.Backreference:
                    case RegexOpcode.Backreference | RegexOpcode.CaseInsensitive:
                        {
                            int capnum = Operand(0);
                            if (IsMatched(capnum))
                            {
                                if (!MatchRef(MatchIndex(capnum), MatchLength(capnum), inputSpan, (_operator & RegexOpcode.CaseInsensitive) != 0))
                                {
                                    break;
                                }
                            }
                            else
                            {
                                if ((runregex!.roptions & RegexOptions.ECMAScript) == 0)
                                {
                                    break;
                                }
                            }
                        }
                        advance = 1;
                        continue;

                    case RegexOpcode.Onerep:
                        {
                            int c = Operand(1);
                            if (Forwardchars() < c)
                            {
                                break;
                            }

                            char ch = (char)Operand(0);
                            while (c-- > 0)
                            {
                                if (Forwardcharnext(inputSpan) != ch)
                                {
                                    goto BreakBackward;
                                }
                            }
                        }
                        advance = 2;
                        continue;

                    case RegexOpcode.Notonerep:
                        {
                            int c = Operand(1);
                            if (Forwardchars() < c)
                            {
                                break;
                            }

                            char ch = (char)Operand(0);
                            while (c-- > 0)
                            {
                                if (Forwardcharnext(inputSpan) == ch)
                                {
                                    goto BreakBackward;
                                }
                            }
                        }
                        advance = 2;
                        continue;

                    case RegexOpcode.Setrep:
                        {
                            int c = Operand(1);
                            if (Forwardchars() < c)
                            {
                                break;
                            }

                            int operand0 = Operand(0);
                            string set = _code.Strings[operand0];
                            ref uint[]? setLookup = ref _code.StringsAsciiLookup[operand0];

                            while (c-- > 0)
                            {
                                if (!RegexCharClass.CharInClass(Forwardcharnext(inputSpan), set, ref setLookup))
                                {
                                    goto BreakBackward;
                                }
                            }
                        }
                        advance = 2;
                        continue;

                    case RegexOpcode.Oneloop:
                    case RegexOpcode.Oneloopatomic:
                        {
                            int len = Math.Min(Operand(1), Forwardchars());
                            char ch = (char)Operand(0);
                            int i;

                            for (i = len; i > 0; i--)
                            {
                                if (Forwardcharnext(inputSpan) != ch)
                                {
                                    Backwardnext();
                                    break;
                                }
                            }

                            if (len > i && _operator == RegexOpcode.Oneloop)
                            {
                                TrackPush(len - i - 1, runtextpos - Bump());
                            }
                        }
                        advance = 2;
                        continue;

                    case RegexOpcode.Notoneloop:
                    case RegexOpcode.Notoneloopatomic:
                        {
                            int len = Math.Min(Operand(1), Forwardchars());
                            char ch = (char)Operand(0);
                            int i;

                            if (!_rightToLeft)
                            {
                                // We're left-to-right, so we can employ the vectorized IndexOf
                                // to search for the character.
                                i = inputSpan.Slice(runtextpos, len).IndexOf(ch);
                                if (i == -1)
                                {
                                    runtextpos += len;
                                    i = 0;
                                }
                                else
                                {
                                    runtextpos += i;
                                    i = len - i;
                                }
                            }
                            else
                            {
                                for (i = len; i > 0; i--)
                                {
                                    if (Forwardcharnext(inputSpan) == ch)
                                    {
                                        Backwardnext();
                                        break;
                                    }
                                }
                            }

                            if (len > i && _operator == RegexOpcode.Notoneloop)
                            {
                                TrackPush(len - i - 1, runtextpos - Bump());
                            }
                        }
                        advance = 2;
                        continue;

                    case RegexOpcode.Setloop:
                    case RegexOpcode.Setloopatomic:
                        {
                            int len = Math.Min(Operand(1), Forwardchars());
                            int operand0 = Operand(0);
                            string set = _code.Strings[operand0];
                            ref uint[]? setLookup = ref _code.StringsAsciiLookup[operand0];
                            int i;

                            for (i = len; i > 0; i--)
                            {
                                if (!RegexCharClass.CharInClass(Forwardcharnext(inputSpan), set, ref setLookup))
                                {
                                    Backwardnext();
                                    break;
                                }
                            }

                            if (len > i && _operator == RegexOpcode.Setloop)
                            {
                                TrackPush(len - i - 1, runtextpos - Bump());
                            }
                        }
                        advance = 2;
                        continue;

                    case RegexOpcode.Oneloop | RegexOpcode.Backtracking:
                    case RegexOpcode.Notoneloop | RegexOpcode.Backtracking:
                    case RegexOpcode.Setloop | RegexOpcode.Backtracking:
                        TrackPop(2);
                        {
                            int i = TrackPeek();
                            int pos = TrackPeek(1);
                            runtextpos = pos;
                            if (i > 0)
                            {
                                TrackPush(i - 1, pos - Bump());
                            }
                        }
                        advance = 2;
                        continue;

                    case RegexOpcode.Onelazy:
                    case RegexOpcode.Notonelazy:
                    case RegexOpcode.Setlazy:
                        {
                            int c = Math.Min(Operand(1), Forwardchars());
                            if (c > 0)
                            {
                                TrackPush(c - 1, runtextpos);
                            }
                        }
                        advance = 2;
                        continue;

                    case RegexOpcode.Onelazy | RegexOpcode.Backtracking:
                        TrackPop(2);
                        {
                            int pos = TrackPeek(1);
                            runtextpos = pos;

                            if (Forwardcharnext(inputSpan) != (char)Operand(0))
                            {
                                break;
                            }

                            int i = TrackPeek();
                            if (i > 0)
                            {
                                TrackPush(i - 1, pos + Bump());
                            }
                        }
                        advance = 2;
                        continue;

                    case RegexOpcode.Notonelazy | RegexOpcode.Backtracking:
                        TrackPop(2);
                        {
                            int pos = TrackPeek(1);
                            runtextpos = pos;

                            if (Forwardcharnext(inputSpan) == (char)Operand(0))
                            {
                                break;
                            }

                            int i = TrackPeek();
                            if (i > 0)
                            {
                                TrackPush(i - 1, pos + Bump());
                            }
                        }
                        advance = 2;
                        continue;

                    case RegexOpcode.Setlazy | RegexOpcode.Backtracking:
                        TrackPop(2);
                        {
                            int pos = TrackPeek(1);
                            runtextpos = pos;

                            int operand0 = Operand(0);
                            if (!RegexCharClass.CharInClass(Forwardcharnext(inputSpan), _code.Strings[operand0], ref _code.StringsAsciiLookup[operand0]))
                            {
                                break;
                            }

                            int i = TrackPeek();
                            if (i > 0)
                            {
                                TrackPush(i - 1, pos + Bump());
                            }
                        }
                        advance = 2;
                        continue;

                    case RegexOpcode.UpdateBumpalong:
                        // UpdateBumpalong should only exist in the code stream at such a point where the root
                        // of the backtracking stack contains the runtextpos from the start of this Go call. Replace
                        // that tracking value with the current runtextpos value if it's greater.
                        {
                            Debug.Assert(!_rightToLeft, "UpdateBumpalongs aren't added for RTL");
                            ref int trackingpos = ref runtrack![runtrack.Length - 1];
                            if (trackingpos < runtextpos)
                            {
                                trackingpos = runtextpos;
                            }
                            advance = 0;
                            continue;
                        }

                    default:
                        Debug.Fail($"Unimplemented state: {_operator:X8}");
                        break;
                }

            BreakBackward:
                Backtrack();
            }
        }
    }
}
