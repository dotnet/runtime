// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// This RegexInterpreter class is internal to the RegularExpression package.
// It executes a block of regular expression codes while consuming
// input.

using System.Collections;
using System.Diagnostics;
using System.Globalization;

namespace System.Text.RegularExpressions
{
    internal sealed class RegexInterpreter : RegexRunner
    {
        internal int _runoperator;
        internal int[] _runcodes;
        internal int _runcodepos;
        internal String[] _runstrings;
        internal RegexCode _runcode;
        internal RegexPrefix _runfcPrefix;
        internal RegexBoyerMoore _runbmPrefix;
        internal int _runanchors;
        internal bool _runrtl;
        internal bool _runci;
        internal CultureInfo _runculture;

        internal RegexInterpreter(RegexCode code, CultureInfo culture)
        {
            _runcode = code;
            _runcodes = code._codes;
            _runstrings = code._strings;
            _runfcPrefix = code._fcPrefix;
            _runbmPrefix = code._bmPrefix;
            _runanchors = code._anchors;
            _runculture = culture;
        }

        protected override void InitTrackCount()
        {
            _runtrackcount = _runcode._trackcount;
        }

        private void Advance()
        {
            Advance(0);
        }

        private void Advance(int i)
        {
            _runcodepos += (i + 1);
            SetOperator(_runcodes[_runcodepos]);
        }

        private void Goto(int newpos)
        {
            // when branching backward, ensure storage
            if (newpos < _runcodepos)
                EnsureStorage();

            SetOperator(_runcodes[newpos]);
            _runcodepos = newpos;
        }

        private void Textto(int newpos)
        {
            _runtextpos = newpos;
        }

        private void Trackto(int newpos)
        {
            _runtrackpos = _runtrack.Length - newpos;
        }

        private int Textstart()
        {
            return _runtextstart;
        }

        private int Textpos()
        {
            return _runtextpos;
        }

        // push onto the backtracking stack
        private int Trackpos()
        {
            return _runtrack.Length - _runtrackpos;
        }

        private void TrackPush()
        {
            _runtrack[--_runtrackpos] = _runcodepos;
        }

        private void TrackPush(int I1)
        {
            _runtrack[--_runtrackpos] = I1;
            _runtrack[--_runtrackpos] = _runcodepos;
        }

        private void TrackPush(int I1, int I2)
        {
            _runtrack[--_runtrackpos] = I1;
            _runtrack[--_runtrackpos] = I2;
            _runtrack[--_runtrackpos] = _runcodepos;
        }

        private void TrackPush(int I1, int I2, int I3)
        {
            _runtrack[--_runtrackpos] = I1;
            _runtrack[--_runtrackpos] = I2;
            _runtrack[--_runtrackpos] = I3;
            _runtrack[--_runtrackpos] = _runcodepos;
        }

        private void TrackPush2(int I1)
        {
            _runtrack[--_runtrackpos] = I1;
            _runtrack[--_runtrackpos] = -_runcodepos;
        }

        private void TrackPush2(int I1, int I2)
        {
            _runtrack[--_runtrackpos] = I1;
            _runtrack[--_runtrackpos] = I2;
            _runtrack[--_runtrackpos] = -_runcodepos;
        }

        private void Backtrack()
        {
            int newpos = _runtrack[_runtrackpos++];
#if DBG
            if (_runmatch.Debug)
            {
                if (newpos < 0)
                    Debug.WriteLine("       Backtracking (back2) to code position " + (-newpos));
                else
                    Debug.WriteLine("       Backtracking to code position " + newpos);
            }
#endif

            if (newpos < 0)
            {
                newpos = -newpos;
                SetOperator(_runcodes[newpos] | RegexCode.Back2);
            }
            else
            {
                SetOperator(_runcodes[newpos] | RegexCode.Back);
            }

            // When branching backward, ensure storage
            if (newpos < _runcodepos)
                EnsureStorage();

            _runcodepos = newpos;
        }

        private void SetOperator(int op)
        {
            _runci = (0 != (op & RegexCode.Ci));
            _runrtl = (0 != (op & RegexCode.Rtl));
            _runoperator = op & ~(RegexCode.Rtl | RegexCode.Ci);
        }

        private void TrackPop()
        {
            _runtrackpos++;
        }

        // pop framesize items from the backtracking stack
        private void TrackPop(int framesize)
        {
            _runtrackpos += framesize;
        }

        // Technically we are actually peeking at items already popped.  So if you want to 
        // get and pop the top item from the stack, you do 
        // TrackPop();
        // TrackPeek();
        private int TrackPeek()
        {
            return _runtrack[_runtrackpos - 1];
        }

        // get the ith element down on the backtracking stack
        private int TrackPeek(int i)
        {
            return _runtrack[_runtrackpos - i - 1];
        }

        // Push onto the grouping stack
        private void StackPush(int I1)
        {
            _runstack[--_runstackpos] = I1;
        }

        private void StackPush(int I1, int I2)
        {
            _runstack[--_runstackpos] = I1;
            _runstack[--_runstackpos] = I2;
        }

        private void StackPop()
        {
            _runstackpos++;
        }

        // pop framesize items from the grouping stack
        private void StackPop(int framesize)
        {
            _runstackpos += framesize;
        }

        // Technically we are actually peeking at items already popped.  So if you want to 
        // get and pop the top item from the stack, you do 
        // StackPop();
        // StackPeek();
        private int StackPeek()
        {
            return _runstack[_runstackpos - 1];
        }

        // get the ith element down on the grouping stack
        private int StackPeek(int i)
        {
            return _runstack[_runstackpos - i - 1];
        }

        private int Operator()
        {
            return _runoperator;
        }

        private int Operand(int i)
        {
            return _runcodes[_runcodepos + i + 1];
        }

        private int Leftchars()
        {
            return _runtextpos - _runtextbeg;
        }

        private int Rightchars()
        {
            return _runtextend - _runtextpos;
        }

        private int Bump()
        {
            return _runrtl ? -1 : 1;
        }

        private int Forwardchars()
        {
            return _runrtl ? _runtextpos - _runtextbeg : _runtextend - _runtextpos;
        }

        private char Forwardcharnext()
        {
            char ch = (_runrtl ? _runtext[--_runtextpos] : _runtext[_runtextpos++]);

            return (_runci ? _runculture.TextInfo.ToLower(ch) : ch);
        }

        private bool Stringmatch(String str)
        {
            int c;
            int pos;

            if (!_runrtl)
            {
                if (_runtextend - _runtextpos < (c = str.Length))
                    return false;

                pos = _runtextpos + c;
            }
            else
            {
                if (_runtextpos - _runtextbeg < (c = str.Length))
                    return false;

                pos = _runtextpos;
            }

            if (!_runci)
            {
                while (c != 0)
                    if (str[--c] != _runtext[--pos])
                        return false;
            }
            else
            {
                while (c != 0)
                    if (str[--c] != _runculture.TextInfo.ToLower(_runtext[--pos]))
                        return false;
            }

            if (!_runrtl)
            {
                pos += str.Length;
            }

            _runtextpos = pos;

            return true;
        }

        private bool Refmatch(int index, int len)
        {
            int c;
            int pos;
            int cmpos;

            if (!_runrtl)
            {
                if (_runtextend - _runtextpos < len)
                    return false;

                pos = _runtextpos + len;
            }
            else
            {
                if (_runtextpos - _runtextbeg < len)
                    return false;

                pos = _runtextpos;
            }
            cmpos = index + len;

            c = len;

            if (!_runci)
            {
                while (c-- != 0)
                    if (_runtext[--cmpos] != _runtext[--pos])
                        return false;
            }
            else
            {
                while (c-- != 0)
                    if (_runculture.TextInfo.ToLower(_runtext[--cmpos]) != _runculture.TextInfo.ToLower(_runtext[--pos]))
                        return false;
            }

            if (!_runrtl)
            {
                pos += len;
            }

            _runtextpos = pos;

            return true;
        }

        private void Backwardnext()
        {
            _runtextpos += _runrtl ? 1 : -1;
        }

        private char CharAt(int j)
        {
            return _runtext[j];
        }

        protected override bool FindFirstChar()
        {
            int i;
            String set;

            if (0 != (_runanchors & (RegexFCD.Beginning | RegexFCD.Start | RegexFCD.EndZ | RegexFCD.End)))
            {
                if (!_runcode._rightToLeft)
                {
                    if ((0 != (_runanchors & RegexFCD.Beginning) && _runtextpos > _runtextbeg) ||
                        (0 != (_runanchors & RegexFCD.Start) && _runtextpos > _runtextstart))
                    {
                        _runtextpos = _runtextend;
                        return false;
                    }
                    if (0 != (_runanchors & RegexFCD.EndZ) && _runtextpos < _runtextend - 1)
                    {
                        _runtextpos = _runtextend - 1;
                    }
                    else if (0 != (_runanchors & RegexFCD.End) && _runtextpos < _runtextend)
                    {
                        _runtextpos = _runtextend;
                    }
                }
                else
                {
                    if ((0 != (_runanchors & RegexFCD.End) && _runtextpos < _runtextend) ||
                        (0 != (_runanchors & RegexFCD.EndZ) && (_runtextpos < _runtextend - 1 ||
                                                               (_runtextpos == _runtextend - 1 && CharAt(_runtextpos) != '\n'))) ||
                        (0 != (_runanchors & RegexFCD.Start) && _runtextpos < _runtextstart))
                    {
                        _runtextpos = _runtextbeg;
                        return false;
                    }
                    if (0 != (_runanchors & RegexFCD.Beginning) && _runtextpos > _runtextbeg)
                    {
                        _runtextpos = _runtextbeg;
                    }
                }

                if (_runbmPrefix != null)
                {
                    return _runbmPrefix.IsMatch(_runtext, _runtextpos, _runtextbeg, _runtextend);
                }

                return true; // found a valid start or end anchor
            }
            else if (_runbmPrefix != null)
            {
                _runtextpos = _runbmPrefix.Scan(_runtext, _runtextpos, _runtextbeg, _runtextend);

                if (_runtextpos == -1)
                {
                    _runtextpos = (_runcode._rightToLeft ? _runtextbeg : _runtextend);
                    return false;
                }

                return true;
            }
            else if (_runfcPrefix == null)
            {
                return true;
            }

            _runrtl = _runcode._rightToLeft;
            _runci = _runfcPrefix.CaseInsensitive;
            set = _runfcPrefix.Prefix;

            if (RegexCharClass.IsSingleton(set))
            {
                char ch = RegexCharClass.SingletonChar(set);

                for (i = Forwardchars(); i > 0; i--)
                {
                    if (ch == Forwardcharnext())
                    {
                        Backwardnext();
                        return true;
                    }
                }
            }
            else
            {
                for (i = Forwardchars(); i > 0; i--)
                {
                    if (RegexCharClass.CharInClass(Forwardcharnext(), set))
                    {
                        Backwardnext();
                        return true;
                    }
                }
            }
            return false;
        }

        protected override void Go()
        {
            Goto(0);

            for (; ;)
            {
#if DBG
                if (_runmatch.Debug)
                {
                    DumpState();
                }
#endif

                CheckTimeout();

                switch (Operator())
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
                            break;
                        Advance(1);
                        continue;

                    case RegexCode.Lazybranch:
                        TrackPush(Textpos());
                        Advance(1);
                        continue;

                    case RegexCode.Lazybranch | RegexCode.Back:
                        TrackPop();
                        Textto(TrackPeek());
                        Goto(Operand(0));
                        continue;

                    case RegexCode.Setmark:
                        StackPush(Textpos());
                        TrackPush();
                        Advance();
                        continue;

                    case RegexCode.Nullmark:
                        StackPush(-1);
                        TrackPush();
                        Advance();
                        continue;

                    case RegexCode.Setmark | RegexCode.Back:
                    case RegexCode.Nullmark | RegexCode.Back:
                        StackPop();
                        break;

                    case RegexCode.Getmark:
                        StackPop();
                        TrackPush(StackPeek());
                        Textto(StackPeek());
                        Advance();
                        continue;

                    case RegexCode.Getmark | RegexCode.Back:
                        TrackPop();
                        StackPush(TrackPeek());
                        break;

                    case RegexCode.Capturemark:
                        if (Operand(1) != -1 && !IsMatched(Operand(1)))
                            break;
                        StackPop();
                        if (Operand(1) != -1)
                            TransferCapture(Operand(0), Operand(1), StackPeek(), Textpos());
                        else
                            Capture(Operand(0), StackPeek(), Textpos());
                        TrackPush(StackPeek());

                        Advance(2);

                        continue;

                    case RegexCode.Capturemark | RegexCode.Back:
                        TrackPop();
                        StackPush(TrackPeek());
                        Uncapture();
                        if (Operand(0) != -1 && Operand(1) != -1)
                            Uncapture();

                        break;

                    case RegexCode.Branchmark:
                        {
                            int matched;
                            StackPop();

                            matched = Textpos() - StackPeek();

                            if (matched != 0)
                            {                     // Nonempty match -> loop now
                                TrackPush(StackPeek(), Textpos());  // Save old mark, textpos
                                StackPush(Textpos());               // Make new mark
                                Goto(Operand(0));                   // Loop
                            }
                            else
                            {                                  // Empty match -> straight now
                                TrackPush2(StackPeek());            // Save old mark
                                Advance(1);                         // Straight
                            }
                            continue;
                        }

                    case RegexCode.Branchmark | RegexCode.Back:
                        TrackPop(2);
                        StackPop();
                        Textto(TrackPeek(1));                       // Recall position
                        TrackPush2(TrackPeek());                    // Save old mark
                        Advance(1);                                 // Straight
                        continue;

                    case RegexCode.Branchmark | RegexCode.Back2:
                        TrackPop();
                        StackPush(TrackPeek());                     // Recall old mark
                        break;                                      // Backtrack

                    case RegexCode.Lazybranchmark:
                        {
                            // We hit this the first time through a lazy loop and after each 
                            // successful match of the inner expression.  It simply continues
                            // on and doesn't loop. 
                            StackPop();

                            int oldMarkPos = StackPeek();

                            if (Textpos() != oldMarkPos)
                            {              // Nonempty match -> try to loop again by going to 'back' state
                                if (oldMarkPos != -1)
                                    TrackPush(oldMarkPos, Textpos());   // Save old mark, textpos
                                else
                                    TrackPush(Textpos(), Textpos());
                            }
                            else
                            {
                                // The inner expression found an empty match, so we'll go directly to 'back2' if we
                                // backtrack.  In this case, we need to push something on the stack, since back2 pops.
                                // However, in the case of ()+? or similar, this empty match may be legitimate, so push the text 
                                // position associated with that empty match.
                                StackPush(oldMarkPos);

                                TrackPush2(StackPeek());                // Save old mark
                            }
                            Advance(1);
                            continue;
                        }

                    case RegexCode.Lazybranchmark | RegexCode.Back:
                        {
                            // After the first time, Lazybranchmark | RegexCode.Back occurs
                            // with each iteration of the loop, and therefore with every attempted
                            // match of the inner expression.  We'll try to match the inner expression, 
                            // then go back to Lazybranchmark if successful.  If the inner expression 
                            // failes, we go to Lazybranchmark | RegexCode.Back2
                            int pos;

                            TrackPop(2);
                            pos = TrackPeek(1);
                            TrackPush2(TrackPeek());                // Save old mark
                            StackPush(pos);                         // Make new mark
                            Textto(pos);                            // Recall position
                            Goto(Operand(0));                       // Loop
                            continue;
                        }

                    case RegexCode.Lazybranchmark | RegexCode.Back2:
                        // The lazy loop has failed.  We'll do a true backtrack and 
                        // start over before the lazy loop. 
                        StackPop();
                        TrackPop();
                        StackPush(TrackPeek());                      // Recall old mark
                        break;

                    case RegexCode.Setcount:
                        StackPush(Textpos(), Operand(0));
                        TrackPush();
                        Advance(1);
                        continue;

                    case RegexCode.Nullcount:
                        StackPush(-1, Operand(0));
                        TrackPush();
                        Advance(1);
                        continue;

                    case RegexCode.Setcount | RegexCode.Back:
                        StackPop(2);
                        break;

                    case RegexCode.Nullcount | RegexCode.Back:
                        StackPop(2);
                        break;

                    case RegexCode.Branchcount:
                        // StackPush:
                        //  0: Mark
                        //  1: Count
                        {
                            StackPop(2);
                            int mark = StackPeek();
                            int count = StackPeek(1);
                            int matched = Textpos() - mark;

                            if (count >= Operand(1) || (matched == 0 && count >= 0))
                            {                                   // Max loops or empty match -> straight now
                                TrackPush2(mark, count);            // Save old mark, count
                                Advance(2);                         // Straight
                            }
                            else
                            {                                  // Nonempty match -> count+loop now
                                TrackPush(mark);                    // remember mark
                                StackPush(Textpos(), count + 1);    // Make new mark, incr count
                                Goto(Operand(0));                   // Loop
                            }
                            continue;
                        }

                    case RegexCode.Branchcount | RegexCode.Back:
                        // TrackPush:
                        //  0: Previous mark
                        // StackPush:
                        //  0: Mark (= current pos, discarded)
                        //  1: Count
                        TrackPop();
                        StackPop(2);
                        if (StackPeek(1) > 0)
                        {                         // Positive -> can go straight
                            Textto(StackPeek());                        // Zap to mark
                            TrackPush2(TrackPeek(), StackPeek(1) - 1);  // Save old mark, old count
                            Advance(2);                                 // Straight
                            continue;
                        }
                        StackPush(TrackPeek(), StackPeek(1) - 1);       // recall old mark, old count
                        break;

                    case RegexCode.Branchcount | RegexCode.Back2:
                        // TrackPush:
                        //  0: Previous mark
                        //  1: Previous count
                        TrackPop(2);
                        StackPush(TrackPeek(), TrackPeek(1));           // Recall old mark, old count
                        break;                                          // Backtrack


                    case RegexCode.Lazybranchcount:
                        // StackPush:
                        //  0: Mark
                        //  1: Count
                        {
                            StackPop(2);
                            int mark = StackPeek();
                            int count = StackPeek(1);

                            if (count < 0)
                            {                        // Negative count -> loop now
                                TrackPush2(mark);                   // Save old mark
                                StackPush(Textpos(), count + 1);    // Make new mark, incr count
                                Goto(Operand(0));                   // Loop
                            }
                            else
                            {                                  // Nonneg count -> straight now
                                TrackPush(mark, count, Textpos());  // Save mark, count, position
                                Advance(2);                         // Straight
                            }
                            continue;
                        }

                    case RegexCode.Lazybranchcount | RegexCode.Back:
                        // TrackPush:
                        //  0: Mark
                        //  1: Count
                        //  2: Textpos
                        {
                            TrackPop(3);
                            int mark = TrackPeek();
                            int textpos = TrackPeek(2);

                            if (TrackPeek(1) < Operand(1) && textpos != mark)
                            { // Under limit and not empty match -> loop
                                Textto(textpos);                            // Recall position
                                StackPush(textpos, TrackPeek(1) + 1);       // Make new mark, incr count
                                TrackPush2(mark);                           // Save old mark
                                Goto(Operand(0));                           // Loop
                                continue;
                            }
                            else
                            {                                          // Max loops or empty match -> backtrack
                                StackPush(TrackPeek(), TrackPeek(1));       // Recall old mark, count
                                break;                                      // backtrack
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
                        StackPush(TrackPeek(), StackPeek(1) - 1);   // Recall old mark, count
                        break;                                      // Backtrack

                    case RegexCode.Setjump:
                        StackPush(Trackpos(), Crawlpos());
                        TrackPush();
                        Advance();
                        continue;

                    case RegexCode.Setjump | RegexCode.Back:
                        StackPop(2);
                        break;

                    case RegexCode.Backjump:
                        // StackPush:
                        //  0: Saved trackpos
                        //  1: Crawlpos
                        StackPop(2);
                        Trackto(StackPeek());

                        while (Crawlpos() != StackPeek(1))
                            Uncapture();

                        break;

                    case RegexCode.Forejump:
                        // StackPush:
                        //  0: Saved trackpos
                        //  1: Crawlpos
                        StackPop(2);
                        Trackto(StackPeek());
                        TrackPush(StackPeek(1));
                        Advance();
                        continue;

                    case RegexCode.Forejump | RegexCode.Back:
                        // TrackPush:
                        //  0: Crawlpos
                        TrackPop();

                        while (Crawlpos() != TrackPeek())
                            Uncapture();

                        break;

                    case RegexCode.Bol:
                        if (Leftchars() > 0 && CharAt(Textpos() - 1) != '\n')
                            break;
                        Advance();
                        continue;

                    case RegexCode.Eol:
                        if (Rightchars() > 0 && CharAt(Textpos()) != '\n')
                            break;
                        Advance();
                        continue;

                    case RegexCode.Boundary:
                        if (!IsBoundary(Textpos(), _runtextbeg, _runtextend))
                            break;
                        Advance();
                        continue;

                    case RegexCode.Nonboundary:
                        if (IsBoundary(Textpos(), _runtextbeg, _runtextend))
                            break;
                        Advance();
                        continue;

                    case RegexCode.ECMABoundary:
                        if (!IsECMABoundary(Textpos(), _runtextbeg, _runtextend))
                            break;
                        Advance();
                        continue;

                    case RegexCode.NonECMABoundary:
                        if (IsECMABoundary(Textpos(), _runtextbeg, _runtextend))
                            break;
                        Advance();
                        continue;

                    case RegexCode.Beginning:
                        if (Leftchars() > 0)
                            break;
                        Advance();
                        continue;

                    case RegexCode.Start:
                        if (Textpos() != Textstart())
                            break;
                        Advance();
                        continue;

                    case RegexCode.EndZ:
                        if (Rightchars() > 1 || Rightchars() == 1 && CharAt(Textpos()) != '\n')
                            break;
                        Advance();
                        continue;

                    case RegexCode.End:
                        if (Rightchars() > 0)
                            break;
                        Advance();
                        continue;

                    case RegexCode.One:
                        if (Forwardchars() < 1 || Forwardcharnext() != (char)Operand(0))
                            break;

                        Advance(1);
                        continue;

                    case RegexCode.Notone:
                        if (Forwardchars() < 1 || Forwardcharnext() == (char)Operand(0))
                            break;

                        Advance(1);
                        continue;

                    case RegexCode.Set:
                        if (Forwardchars() < 1 || !RegexCharClass.CharInClass(Forwardcharnext(), _runstrings[Operand(0)]))
                            break;

                        Advance(1);
                        continue;

                    case RegexCode.Multi:
                        {
                            if (!Stringmatch(_runstrings[Operand(0)]))
                                break;

                            Advance(1);
                            continue;
                        }

                    case RegexCode.Ref:
                        {
                            int capnum = Operand(0);

                            if (IsMatched(capnum))
                            {
                                if (!Refmatch(MatchIndex(capnum), MatchLength(capnum)))
                                    break;
                            }
                            else
                            {
                                if ((_runregex._roptions & RegexOptions.ECMAScript) == 0)
                                    break;
                            }

                            Advance(1);
                            continue;
                        }

                    case RegexCode.Onerep:
                        {
                            int c = Operand(1);

                            if (Forwardchars() < c)
                                break;

                            char ch = (char)Operand(0);

                            while (c-- > 0)
                                if (Forwardcharnext() != ch)
                                    goto BreakBackward;

                            Advance(2);
                            continue;
                        }

                    case RegexCode.Notonerep:
                        {
                            int c = Operand(1);

                            if (Forwardchars() < c)
                                break;

                            char ch = (char)Operand(0);

                            while (c-- > 0)
                                if (Forwardcharnext() == ch)
                                    goto BreakBackward;

                            Advance(2);
                            continue;
                        }

                    case RegexCode.Setrep:
                        {
                            int c = Operand(1);

                            if (Forwardchars() < c)
                                break;

                            String set = _runstrings[Operand(0)];

                            while (c-- > 0)
                                if (!RegexCharClass.CharInClass(Forwardcharnext(), set))
                                    goto BreakBackward;

                            Advance(2);
                            continue;
                        }

                    case RegexCode.Oneloop:
                        {
                            int c = Operand(1);

                            if (c > Forwardchars())
                                c = Forwardchars();

                            char ch = (char)Operand(0);
                            int i;

                            for (i = c; i > 0; i--)
                            {
                                if (Forwardcharnext() != ch)
                                {
                                    Backwardnext();
                                    break;
                                }
                            }

                            if (c > i)
                                TrackPush(c - i - 1, Textpos() - Bump());

                            Advance(2);
                            continue;
                        }

                    case RegexCode.Notoneloop:
                        {
                            int c = Operand(1);

                            if (c > Forwardchars())
                                c = Forwardchars();

                            char ch = (char)Operand(0);
                            int i;

                            for (i = c; i > 0; i--)
                            {
                                if (Forwardcharnext() == ch)
                                {
                                    Backwardnext();
                                    break;
                                }
                            }

                            if (c > i)
                                TrackPush(c - i - 1, Textpos() - Bump());

                            Advance(2);
                            continue;
                        }

                    case RegexCode.Setloop:
                        {
                            int c = Operand(1);

                            if (c > Forwardchars())
                                c = Forwardchars();

                            String set = _runstrings[Operand(0)];
                            int i;

                            for (i = c; i > 0; i--)
                            {
                                if (!RegexCharClass.CharInClass(Forwardcharnext(), set))
                                {
                                    Backwardnext();
                                    break;
                                }
                            }

                            if (c > i)
                                TrackPush(c - i - 1, Textpos() - Bump());

                            Advance(2);
                            continue;
                        }

                    case RegexCode.Oneloop | RegexCode.Back:
                    case RegexCode.Notoneloop | RegexCode.Back:
                        {
                            TrackPop(2);
                            int i = TrackPeek();
                            int pos = TrackPeek(1);

                            Textto(pos);

                            if (i > 0)
                                TrackPush(i - 1, pos - Bump());

                            Advance(2);
                            continue;
                        }

                    case RegexCode.Setloop | RegexCode.Back:
                        {
                            TrackPop(2);
                            int i = TrackPeek();
                            int pos = TrackPeek(1);

                            Textto(pos);

                            if (i > 0)
                                TrackPush(i - 1, pos - Bump());

                            Advance(2);
                            continue;
                        }

                    case RegexCode.Onelazy:
                    case RegexCode.Notonelazy:
                        {
                            int c = Operand(1);

                            if (c > Forwardchars())
                                c = Forwardchars();

                            if (c > 0)
                                TrackPush(c - 1, Textpos());

                            Advance(2);
                            continue;
                        }

                    case RegexCode.Setlazy:
                        {
                            int c = Operand(1);

                            if (c > Forwardchars())
                                c = Forwardchars();

                            if (c > 0)
                                TrackPush(c - 1, Textpos());

                            Advance(2);
                            continue;
                        }

                    case RegexCode.Onelazy | RegexCode.Back:
                        {
                            TrackPop(2);
                            int pos = TrackPeek(1);
                            Textto(pos);

                            if (Forwardcharnext() != (char)Operand(0))
                                break;

                            int i = TrackPeek();

                            if (i > 0)
                                TrackPush(i - 1, pos + Bump());

                            Advance(2);
                            continue;
                        }

                    case RegexCode.Notonelazy | RegexCode.Back:
                        {
                            TrackPop(2);
                            int pos = TrackPeek(1);
                            Textto(pos);

                            if (Forwardcharnext() == (char)Operand(0))
                                break;

                            int i = TrackPeek();

                            if (i > 0)
                                TrackPush(i - 1, pos + Bump());

                            Advance(2);
                            continue;
                        }

                    case RegexCode.Setlazy | RegexCode.Back:
                        {
                            TrackPop(2);
                            int pos = TrackPeek(1);
                            Textto(pos);

                            if (!RegexCharClass.CharInClass(Forwardcharnext(), _runstrings[Operand(0)]))
                                break;

                            int i = TrackPeek();

                            if (i > 0)
                                TrackPush(i - 1, pos + Bump());

                            Advance(2);
                            continue;
                        }

                    default:
                        throw NotImplemented.ByDesignWithMessage(SR.UnimplementedState);
                }

            BreakBackward:
                ;

                // "break Backward" comes here:
                Backtrack();
            }
        }
#if DBG
        internal override void DumpState()
        {
            base.DumpState();
            Debug.WriteLine("       " + _runcode.OpcodeDescription(_runcodepos) +
                              ((_runoperator & RegexCode.Back) != 0 ? " Back" : "") +
                              ((_runoperator & RegexCode.Back2) != 0 ? " Back2" : ""));
            Debug.WriteLine("");
        }
#endif
    }
}
