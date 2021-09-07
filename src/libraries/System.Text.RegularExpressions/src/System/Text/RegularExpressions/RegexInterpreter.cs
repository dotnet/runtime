// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace System.Text.RegularExpressions
{
    /// <summary>Executes a block of regular expression codes while consuming input.</summary>
    internal sealed class RegexInterpreter : RegexRunner
    {
        private const int LoopTimeoutCheckCount = 2048; // conservative value to provide reasonably-accurate timeout handling.

        private readonly RegexCode _code;
        private readonly TextInfo _textInfo;

        private int _operator;
        private int _codepos;
        private bool _rightToLeft;
        private bool _caseInsensitive;

        public RegexInterpreter(RegexCode code, CultureInfo culture)
        {
            Debug.Assert(code != null, "code must not be null.");
            Debug.Assert(culture != null, "culture must not be null.");

            _code = code;
            _textInfo = culture.TextInfo;
        }

        protected override void InitTrackCount() => runtrackcount = _code.TrackCount;

        private void Advance(int i)
        {
            _codepos += i + 1;
            SetOperator(_code.Codes[_codepos]);
        }

        private void Goto(int newpos)
        {
            // When branching backward, ensure storage.
            if (newpos < _codepos)
            {
                EnsureStorage();
            }

            _codepos = newpos;
            SetOperator(_code.Codes[newpos]);
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
            int newpos = runtrack![runtrackpos];
            runtrackpos++;

#if DEBUG
            if (runmatch!.IsDebug)
            {
                Debug.WriteLine(newpos < 0 ?
                    $"       Backtracking (back2) to code position {-newpos}" :
                    $"       Backtracking to code position {newpos}");
            }
#endif

            int back = RegexCode.Back;
            if (newpos < 0)
            {
                newpos = -newpos;
                back = RegexCode.Back2;
            }
            SetOperator(_code.Codes[newpos] | back);

            // When branching backward, ensure storage.
            if (newpos < _codepos)
            {
                EnsureStorage();
            }

            _codepos = newpos;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SetOperator(int op)
        {
            _operator = op & ~(RegexCode.Rtl | RegexCode.Ci);
            _caseInsensitive = (op & RegexCode.Ci) != 0;
            _rightToLeft = (op & RegexCode.Rtl) != 0;
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

        private int Leftchars() => runtextpos - runtextbeg;

        private int Rightchars() => runtextend - runtextpos;

        private int Bump() => _rightToLeft ? -1 : 1;

        private int Forwardchars() => _rightToLeft ? runtextpos - runtextbeg : runtextend - runtextpos;

        private char Forwardcharnext()
        {
            char ch = _rightToLeft ? runtext![--runtextpos] : runtext![runtextpos++];

            return _caseInsensitive ? _textInfo.ToLower(ch) : ch;
        }

        private bool MatchString(string str)
        {
            int c = str.Length;
            int pos;

            if (!_rightToLeft)
            {
                if (runtextend - runtextpos < c)
                {
                    return false;
                }

                pos = runtextpos + c;
            }
            else
            {
                if (runtextpos - runtextbeg < c)
                {
                    return false;
                }

                pos = runtextpos;
            }

            if (!_caseInsensitive)
            {
                while (c != 0)
                {
                    if (str[--c] != runtext![--pos])
                    {
                        return false;
                    }
                }
            }
            else
            {
                TextInfo ti = _textInfo;
                while (c != 0)
                {
                    if (str[--c] != ti.ToLower(runtext![--pos]))
                    {
                        return false;
                    }
                }
            }

            if (!_rightToLeft)
            {
                pos += str.Length;
            }

            runtextpos = pos;

            return true;
        }

        private bool MatchRef(int index, int length)
        {
            int pos;
            if (!_rightToLeft)
            {
                if (runtextend - runtextpos < length)
                {
                    return false;
                }

                pos = runtextpos + length;
            }
            else
            {
                if (runtextpos - runtextbeg < length)
                {
                    return false;
                }

                pos = runtextpos;
            }

            int cmpos = index + length;
            int c = length;

            if (!_caseInsensitive)
            {
                while (c-- != 0)
                {
                    if (runtext![--cmpos] != runtext[--pos])
                    {
                        return false;
                    }
                }
            }
            else
            {
                TextInfo ti = _textInfo;
                while (c-- != 0)
                {
                    if (ti.ToLower(runtext![--cmpos]) != ti.ToLower(runtext[--pos]))
                    {
                        return false;
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

        protected override bool FindFirstChar()
        {
            // Return early if we know there's not enough input left to match.
            if (!_code.RightToLeft)
            {
                if (runtextpos > runtextend - _code.Tree.MinRequiredLength)
                {
                    runtextpos = runtextend;
                    return false;
                }
            }
            else
            {
                if (runtextpos - _code.Tree.MinRequiredLength < runtextbeg)
                {
                    runtextpos = runtextbeg;
                    return false;
                }
            }

            // If the pattern is anchored, we can update our position appropriately and return immediately.
            // If there's a Boyer-Moore prefix, we can also validate it.
            if ((_code.LeadingAnchor & (RegexPrefixAnalyzer.Beginning | RegexPrefixAnalyzer.Start | RegexPrefixAnalyzer.EndZ | RegexPrefixAnalyzer.End)) != 0)
            {
                if (!_code.RightToLeft)
                {
                    switch (_code.LeadingAnchor)
                    {
                        case RegexPrefixAnalyzer.Beginning when runtextpos > runtextbeg:
                        case RegexPrefixAnalyzer.Start when runtextpos > runtextstart:
                            runtextpos = runtextend;
                            return false;

                        case RegexPrefixAnalyzer.EndZ when runtextpos < runtextend - 1:
                            runtextpos = runtextend - 1;
                            break;

                        case RegexPrefixAnalyzer.End when runtextpos < runtextend:
                            runtextpos = runtextend;
                            break;
                    }
                }
                else
                {
                    switch (_code.LeadingAnchor)
                    {
                        case RegexPrefixAnalyzer.End when runtextpos < runtextend:
                        case RegexPrefixAnalyzer.EndZ when runtextpos < runtextend - 1 || (runtextpos == runtextend - 1 && runtext![runtextpos] != '\n'):
                        case RegexPrefixAnalyzer.Start when runtextpos < runtextstart:
                            runtextpos = runtextbeg;
                            return false;

                        case RegexPrefixAnalyzer.Beginning when runtextpos > runtextbeg:
                            runtextpos = runtextbeg;
                            break;
                    }
                }

                return
                    _code.BoyerMoorePrefix == null || // found a valid start or end anchor
                    _code.BoyerMoorePrefix.IsMatch(runtext!, runtextpos, runtextbeg, runtextend);
            }

            // Optimize the handling of a Beginning-Of-Line (BOL) anchor.  BOL is special, in that unlike
            // other anchors like Beginning, there are potentially multiple places a BOL can match.  So unlike
            // the other anchors, which all skip all subsequent processing if found, with BOL we just use it
            // to boost our position to the next line, and then continue normally with any Boyer-Moore or
            // leading char class searches.
            if (_code.LeadingAnchor == RegexPrefixAnalyzer.Bol &&
                !_code.RightToLeft) // don't bother customizing this optimization for the very niche RTL + Multiline case
            {
                // If we're not currently positioned at the beginning of a line (either
                // the beginning of the string or just after a line feed), find the next
                // newline and position just after it.
                if (runtextpos > runtextbeg && runtext![runtextpos - 1] != '\n')
                {
                    int newline = runtext.IndexOf('\n', runtextpos);
                    if (newline == -1 || newline + 1 > runtextend)
                    {
                        runtextpos = runtextend;
                        return false;
                    }

                    runtextpos = newline + 1;
                }
            }

            if (_code.BoyerMoorePrefix != null)
            {
                runtextpos = _code.BoyerMoorePrefix.Scan(runtext!, runtextpos, runtextbeg, runtextend);

                if (runtextpos == -1)
                {
                    runtextpos = _code.RightToLeft ? runtextbeg : runtextend;
                    return false;
                }

                return true;
            }

            if (_code.LeadingCharClasses is null)
            {
                return true;
            }

            // We now loop through looking for the first matching character.  This is a hot loop, so we lift out as many
            // branches as we can.  Each operation requires knowing whether this is a) right-to-left vs left-to-right, and
            // b) case-sensitive vs case-insensitive, and c) a singleton or not.  So, we split it all out into 8 loops, for
            // each combination of these. It's duplicated code, but it allows the inner loop to be much tighter than if
            // everything were combined with multiple branches on each operation.  We can also then use spans to avoid bounds
            // checks in at least the forward iteration direction where the JIT is able to detect the pattern.

            // TODO https://github.com/dotnet/runtime/issues/1349:
            // LeadingCharClasses may contain multiple sets, one for each of the first N characters in the expression,
            // but the interpreter currently only uses the first set for the first character.  In fact, we currently
            // only run the analysis that can produce multiple sets if RegexOptions.Compiled was set.

            string set = _code.LeadingCharClasses[0].CharClass;
            if (RegexCharClass.IsSingleton(set))
            {
                char ch = RegexCharClass.SingletonChar(set);

                if (!_code.RightToLeft)
                {
                    ReadOnlySpan<char> span = runtext.AsSpan(runtextpos, runtextend - runtextpos);
                    if (!_code.LeadingCharClasses[0].CaseInsensitive)
                    {
                        // singleton, left-to-right, case-sensitive
                        int i = span.IndexOf(ch);
                        if (i >= 0)
                        {
                            runtextpos += i;
                            return true;
                        }
                    }
                    else
                    {
                        // singleton, left-to-right, case-insensitive
                        TextInfo ti = _textInfo;
                        for (int i = 0; i < span.Length; i++)
                        {
                            if (ch == ti.ToLower(span[i]))
                            {
                                runtextpos += i;
                                return true;
                            }
                        }
                    }

                    runtextpos = runtextend;
                }
                else
                {
                    if (!_code.LeadingCharClasses[0].CaseInsensitive)
                    {
                        // singleton, right-to-left, case-sensitive
                        for (int i = runtextpos - 1; i >= runtextbeg; i--)
                        {
                            if (ch == runtext![i])
                            {
                                runtextpos = i + 1;
                                return true;
                            }
                        }
                    }
                    else
                    {
                        // singleton, right-to-left, case-insensitive
                        TextInfo ti = _textInfo;
                        for (int i = runtextpos - 1; i >= runtextbeg; i--)
                        {
                            if (ch == ti.ToLower(runtext![i]))
                            {
                                runtextpos = i + 1;
                                return true;
                            }
                        }
                    }

                    runtextpos = runtextbeg;
                }
            }
            else
            {
                if (!_code.RightToLeft)
                {
                    ReadOnlySpan<char> span = runtext.AsSpan(runtextpos, runtextend - runtextpos);
                    if (!_code.LeadingCharClasses[0].CaseInsensitive)
                    {
                        // set, left-to-right, case-sensitive
                        for (int i = 0; i < span.Length; i++)
                        {
                            if (RegexCharClass.CharInClass(span[i], set, ref _code.LeadingCharClassAsciiLookup))
                            {
                                runtextpos += i;
                                return true;
                            }
                        }
                    }
                    else
                    {
                        // set, left-to-right, case-insensitive
                        TextInfo ti = _textInfo;
                        for (int i = 0; i < span.Length; i++)
                        {
                            if (RegexCharClass.CharInClass(ti.ToLower(span[i]), set, ref _code.LeadingCharClassAsciiLookup))
                            {
                                runtextpos += i;
                                return true;
                            }
                        }
                    }

                    runtextpos = runtextend;
                }
                else
                {
                    if (!_code.LeadingCharClasses[0].CaseInsensitive)
                    {
                        // set, right-to-left, case-sensitive
                        for (int i = runtextpos - 1; i >= runtextbeg; i--)
                        {
                            if (RegexCharClass.CharInClass(runtext![i], set, ref _code.LeadingCharClassAsciiLookup))
                            {
                                runtextpos = i + 1;
                                return true;
                            }
                        }
                    }
                    else
                    {
                        // set, right-to-left, case-insensitive
                        TextInfo ti = _textInfo;
                        for (int i = runtextpos - 1; i >= runtextbeg; i--)
                        {
                            if (RegexCharClass.CharInClass(ti.ToLower(runtext![i]), set, ref _code.LeadingCharClassAsciiLookup))
                            {
                                runtextpos = i + 1;
                                return true;
                            }
                        }
                    }

                    runtextpos = runtextbeg;
                }
            }

            return false;
        }

        protected override void Go()
        {
            SetOperator(_code.Codes[0]);
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
#if DEBUG
                if (runmatch!.IsDebug)
                {
                    DumpState();
                }
#endif
                CheckTimeout();

                switch (_operator)
                {
                    case RegexCode.Stop:
                        return;

                    case RegexCode.Nothing:
                        break;

                    case RegexCode.Goto:
                        Goto(Operand(0));
                        continue;

                    case RegexCode.Testref:
                        if (!IsMatched(Operand(0)))
                        {
                            break;
                        }
                        advance = 1;
                        continue;

                    case RegexCode.Lazybranch:
                        TrackPush(runtextpos);
                        advance = 1;
                        continue;

                    case RegexCode.Lazybranch | RegexCode.Back:
                        TrackPop();
                        runtextpos = TrackPeek();
                        Goto(Operand(0));
                        continue;

                    case RegexCode.Setmark:
                        StackPush(runtextpos);
                        TrackPush();
                        advance = 0;
                        continue;

                    case RegexCode.Nullmark:
                        StackPush(-1);
                        TrackPush();
                        advance = 0;
                        continue;

                    case RegexCode.Setmark | RegexCode.Back:
                    case RegexCode.Nullmark | RegexCode.Back:
                        StackPop();
                        break;

                    case RegexCode.Getmark:
                        StackPop();
                        TrackPush(StackPeek());
                        runtextpos = StackPeek();
                        advance = 0;
                        continue;

                    case RegexCode.Getmark | RegexCode.Back:
                        TrackPop();
                        StackPush(TrackPeek());
                        break;

                    case RegexCode.Capturemark:
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

                    case RegexCode.Capturemark | RegexCode.Back:
                        TrackPop();
                        StackPush(TrackPeek());
                        Uncapture();
                        if (Operand(0) != -1 && Operand(1) != -1)
                        {
                            Uncapture();
                        }
                        break;

                    case RegexCode.Branchmark:
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

                    case RegexCode.Branchmark | RegexCode.Back:
                        TrackPop(2);
                        StackPop();
                        runtextpos = TrackPeek(1); // Recall position
                        TrackPush2(TrackPeek());   // Save old mark
                        advance = 1;               // Straight
                        continue;

                    case RegexCode.Branchmark | RegexCode.Back2:
                        TrackPop();
                        StackPush(TrackPeek()); // Recall old mark
                        break;                  // Backtrack

                    case RegexCode.Lazybranchmark:
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

                    case RegexCode.Lazybranchmark | RegexCode.Back:
                        {
                            // After the first time, Lazybranchmark | RegexCode.Back occurs
                            // with each iteration of the loop, and therefore with every attempted
                            // match of the inner expression.  We'll try to match the inner expression,
                            // then go back to Lazybranchmark if successful.  If the inner expression
                            // fails, we go to Lazybranchmark | RegexCode.Back2
                            TrackPop(2);
                            int pos = TrackPeek(1);
                            TrackPush2(TrackPeek()); // Save old mark
                            StackPush(pos);          // Make new mark
                            runtextpos = pos;        // Recall position
                            Goto(Operand(0));        // Loop
                        }
                        continue;

                    case RegexCode.Lazybranchmark | RegexCode.Back2:
                        // The lazy loop has failed.  We'll do a true backtrack and
                        // start over before the lazy loop.
                        StackPop();
                        TrackPop();
                        StackPush(TrackPeek()); // Recall old mark
                        break;

                    case RegexCode.Setcount:
                        StackPush(runtextpos, Operand(0));
                        TrackPush();
                        advance = 1;
                        continue;

                    case RegexCode.Nullcount:
                        StackPush(-1, Operand(0));
                        TrackPush();
                        advance = 1;
                        continue;

                    case RegexCode.Setcount | RegexCode.Back:
                    case RegexCode.Nullcount | RegexCode.Back:
                    case RegexCode.Setjump | RegexCode.Back:
                        StackPop(2);
                        break;

                    case RegexCode.Branchcount:
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

                    case RegexCode.Branchcount | RegexCode.Back:
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

                    case RegexCode.Branchcount | RegexCode.Back2:
                        // TrackPush:
                        //  0: Previous mark
                        //  1: Previous count
                        TrackPop(2);
                        StackPush(TrackPeek(), TrackPeek(1)); // Recall old mark, old count
                        break;                                // Backtrack

                    case RegexCode.Lazybranchcount:
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

                    case RegexCode.Lazybranchcount | RegexCode.Back:
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

                    case RegexCode.Lazybranchcount | RegexCode.Back2:
                        // TrackPush:
                        //  0: Previous mark
                        // StackPush:
                        //  0: Mark (== current pos, discarded)
                        //  1: Count
                        TrackPop();
                        StackPop(2);
                        StackPush(TrackPeek(), StackPeek(1) - 1); // Recall old mark, count
                        break;                                    // Backtrack

                    case RegexCode.Setjump:
                        StackPush(Trackpos(), Crawlpos());
                        TrackPush();
                        advance = 0;
                        continue;

                    case RegexCode.Backjump:
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

                    case RegexCode.Forejump:
                        // StackPush:
                        //  0: Saved trackpos
                        //  1: Crawlpos
                        StackPop(2);
                        Trackto(StackPeek());
                        TrackPush(StackPeek(1));
                        advance = 0;
                        continue;

                    case RegexCode.Forejump | RegexCode.Back:
                        // TrackPush:
                        //  0: Crawlpos
                        TrackPop();
                        while (Crawlpos() != TrackPeek())
                        {
                            Uncapture();
                        }
                        break;

                    case RegexCode.Bol:
                        if (Leftchars() > 0 && runtext![runtextpos - 1] != '\n')
                        {
                            break;
                        }
                        advance = 0;
                        continue;

                    case RegexCode.Eol:
                        if (Rightchars() > 0 && runtext![runtextpos] != '\n')
                        {
                            break;
                        }
                        advance = 0;
                        continue;

                    case RegexCode.Boundary:
                        if (!IsBoundary(runtextpos, runtextbeg, runtextend))
                        {
                            break;
                        }
                        advance = 0;
                        continue;

                    case RegexCode.NonBoundary:
                        if (IsBoundary(runtextpos, runtextbeg, runtextend))
                        {
                            break;
                        }
                        advance = 0;
                        continue;

                    case RegexCode.ECMABoundary:
                        if (!IsECMABoundary(runtextpos, runtextbeg, runtextend))
                        {
                            break;
                        }
                        advance = 0;
                        continue;

                    case RegexCode.NonECMABoundary:
                        if (IsECMABoundary(runtextpos, runtextbeg, runtextend))
                        {
                            break;
                        }
                        advance = 0;
                        continue;

                    case RegexCode.Beginning:
                        if (Leftchars() > 0)
                        {
                            break;
                        }
                        advance = 0;
                        continue;

                    case RegexCode.Start:
                        if (runtextpos != runtextstart)
                        {
                            break;
                        }
                        advance = 0;
                        continue;

                    case RegexCode.EndZ:
                        if (Rightchars() > 1 || Rightchars() == 1 && runtext![runtextpos] != '\n')
                        {
                            break;
                        }
                        advance = 0;
                        continue;

                    case RegexCode.End:
                        if (Rightchars() > 0)
                        {
                            break;
                        }
                        advance = 0;
                        continue;

                    case RegexCode.One:
                        if (Forwardchars() < 1 || Forwardcharnext() != (char)Operand(0))
                        {
                            break;
                        }
                        advance = 1;
                        continue;

                    case RegexCode.Notone:
                        if (Forwardchars() < 1 || Forwardcharnext() == (char)Operand(0))
                        {
                            break;
                        }
                        advance = 1;
                        continue;

                    case RegexCode.Set:
                        if (Forwardchars() < 1)
                        {
                            break;
                        }
                        else
                        {
                            int operand = Operand(0);
                            if (!RegexCharClass.CharInClass(Forwardcharnext(), _code.Strings[operand], ref _code.StringsAsciiLookup[operand]))
                            {
                                break;
                            }
                        }
                        advance = 1;
                        continue;

                    case RegexCode.Multi:
                        if (!MatchString(_code.Strings[Operand(0)]))
                        {
                            break;
                        }
                        advance = 1;
                        continue;

                    case RegexCode.Ref:
                        {
                            int capnum = Operand(0);
                            if (IsMatched(capnum))
                            {
                                if (!MatchRef(MatchIndex(capnum), MatchLength(capnum)))
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

                    case RegexCode.Onerep:
                        {
                            int c = Operand(1);
                            if (Forwardchars() < c)
                            {
                                break;
                            }

                            char ch = (char)Operand(0);
                            while (c-- > 0)
                            {
                                if (Forwardcharnext() != ch)
                                {
                                    goto BreakBackward;
                                }
                            }
                        }
                        advance = 2;
                        continue;

                    case RegexCode.Notonerep:
                        {
                            int c = Operand(1);
                            if (Forwardchars() < c)
                            {
                                break;
                            }

                            char ch = (char)Operand(0);
                            while (c-- > 0)
                            {
                                if (Forwardcharnext() == ch)
                                {
                                    goto BreakBackward;
                                }
                            }
                        }
                        advance = 2;
                        continue;

                    case RegexCode.Setrep:
                        {
                            int c = Operand(1);
                            if (Forwardchars() < c)
                            {
                                break;
                            }

                            int operand0 = Operand(0);
                            string set = _code.Strings[operand0];
                            ref int[]? setLookup = ref _code.StringsAsciiLookup[operand0];

                            while (c-- > 0)
                            {
                                // Check the timeout every 2048th iteration.
                                if ((uint)c % LoopTimeoutCheckCount == 0)
                                {
                                    CheckTimeout();
                                }

                                if (!RegexCharClass.CharInClass(Forwardcharnext(), set, ref setLookup))
                                {
                                    goto BreakBackward;
                                }
                            }
                        }
                        advance = 2;
                        continue;

                    case RegexCode.Oneloop:
                    case RegexCode.Oneloopatomic:
                        {
                            int len = Math.Min(Operand(1), Forwardchars());
                            char ch = (char)Operand(0);
                            int i;

                            for (i = len; i > 0; i--)
                            {
                                if (Forwardcharnext() != ch)
                                {
                                    Backwardnext();
                                    break;
                                }
                            }

                            if (len > i && _operator == RegexCode.Oneloop)
                            {
                                TrackPush(len - i - 1, runtextpos - Bump());
                            }
                        }
                        advance = 2;
                        continue;

                    case RegexCode.Notoneloop:
                    case RegexCode.Notoneloopatomic:
                        {
                            int len = Math.Min(Operand(1), Forwardchars());
                            char ch = (char)Operand(0);
                            int i;

                            if (!_rightToLeft && !_caseInsensitive)
                            {
                                // We're left-to-right and case-sensitive, so we can employ the vectorized IndexOf
                                // to search for the character.
                                i = runtext!.AsSpan(runtextpos, len).IndexOf(ch);
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
                                    if (Forwardcharnext() == ch)
                                    {
                                        Backwardnext();
                                        break;
                                    }
                                }
                            }

                            if (len > i && _operator == RegexCode.Notoneloop)
                            {
                                TrackPush(len - i - 1, runtextpos - Bump());
                            }
                        }
                        advance = 2;
                        continue;

                    case RegexCode.Setloop:
                    case RegexCode.Setloopatomic:
                        {
                            int len = Math.Min(Operand(1), Forwardchars());
                            int operand0 = Operand(0);
                            string set = _code.Strings[operand0];
                            ref int[]? setLookup = ref _code.StringsAsciiLookup[operand0];
                            int i;

                            for (i = len; i > 0; i--)
                            {
                                // Check the timeout every 2048th iteration.
                                if ((uint)i % LoopTimeoutCheckCount == 0)
                                {
                                    CheckTimeout();
                                }

                                if (!RegexCharClass.CharInClass(Forwardcharnext(), set, ref setLookup))
                                {
                                    Backwardnext();
                                    break;
                                }
                            }

                            if (len > i && _operator == RegexCode.Setloop)
                            {
                                TrackPush(len - i - 1, runtextpos - Bump());
                            }
                        }
                        advance = 2;
                        continue;

                    case RegexCode.Oneloop | RegexCode.Back:
                    case RegexCode.Notoneloop | RegexCode.Back:
                    case RegexCode.Setloop | RegexCode.Back:
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

                    case RegexCode.Onelazy:
                    case RegexCode.Notonelazy:
                    case RegexCode.Setlazy:
                        {
                            int c = Math.Min(Operand(1), Forwardchars());
                            if (c > 0)
                            {
                                TrackPush(c - 1, runtextpos);
                            }
                        }
                        advance = 2;
                        continue;

                    case RegexCode.Onelazy | RegexCode.Back:
                        TrackPop(2);
                        {
                            int pos = TrackPeek(1);
                            runtextpos = pos;

                            if (Forwardcharnext() != (char)Operand(0))
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

                    case RegexCode.Notonelazy | RegexCode.Back:
                        TrackPop(2);
                        {
                            int pos = TrackPeek(1);
                            runtextpos = pos;

                            if (Forwardcharnext() == (char)Operand(0))
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

                    case RegexCode.Setlazy | RegexCode.Back:
                        TrackPop(2);
                        {
                            int pos = TrackPeek(1);
                            runtextpos = pos;

                            int operand0 = Operand(0);
                            if (!RegexCharClass.CharInClass(Forwardcharnext(), _code.Strings[operand0], ref _code.StringsAsciiLookup[operand0]))
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

                    case RegexCode.UpdateBumpalong:
                        // UpdateBumpalong should only exist in the code stream at such a point where the root
                        // of the backtracking stack contains the runtextpos from the start of this Go call. Replace
                        // that tracking value with the current runtextpos value.
                        runtrack![runtrack.Length - 1] = runtextpos;
                        advance = 0;
                        continue;

                    default:
                        Debug.Fail($"Unimplemented state: {_operator:X8}");
                        break;
                }

            BreakBackward:
                Backtrack();
            }
        }

#if DEBUG
        [ExcludeFromCodeCoverage(Justification = "Debug only")]
        internal override void DumpState()
        {
            base.DumpState();
            Debug.WriteLine(
                "       " +
                _code.OpcodeDescription(_codepos) +
                ((_operator & RegexCode.Back) != 0 ? " Back" : "") +
                ((_operator & RegexCode.Back2) != 0 ? " Back2" : ""));
            Debug.WriteLine("");
        }
#endif
    }
}
