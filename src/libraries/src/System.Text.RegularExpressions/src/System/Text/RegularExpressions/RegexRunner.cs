// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// This RegexRunner class is a base class for compiled regex code.

// Implementation notes:

// It provides the driver code that call's the subclass's Go()
// method for either scanning or direct execution.
//
// It also maintains memory allocation for the backtracking stack,
// the grouping stack and the longjump crawlstack, and provides
// methods to push new subpattern match results into (or remove
// backtracked results from) the Match instance.

using System.Collections;
using System.Diagnostics;
using System.ComponentModel;
using System.Globalization;

namespace System.Text.RegularExpressions
{
    abstract internal class RegexRunner
    {
        protected internal int _runtextbeg;         // beginning of text to search
        protected internal int _runtextend;         // end of text to search
        protected internal int _runtextstart;       // starting point for search

        protected internal String _runtext;         // text to search
        protected internal int _runtextpos;         // current position in text

        protected internal int[] _runtrack;         // The backtracking stack.  Opcodes use this to store data regarding 
        protected internal int _runtrackpos;        // what they have matched and where to backtrack to.  Each "frame" on 
                                                    // the stack takes the form of [CodePosition Data1 Data2...], where 
                                                    // CodePosition is the position of the current opcode and 
                                                    // the data values are all optional.  The CodePosition can be negative, and
                                                    // these values (also called "back2") are used by the BranchMark family of opcodes
                                                    // to indicate whether they are backtracking after a successful or failed
                                                    // match.  
                                                    // When we backtrack, we pop the CodePosition off the stack, set the current
                                                    // instruction pointer to that code position, and mark the opcode 
                                                    // with a backtracking flag ("Back").  Each opcode then knows how to 
                                                    // handle its own data. 

        protected internal int[] _runstack;         // This stack is used to track text positions across different opcodes. 
        protected internal int _runstackpos;        // For example, in /(a*b)+/, the parentheses result in a SetMark/CaptureMark 
                                                    // pair. SetMark records the text position before we match a*b.  Then
                                                    // CaptureMark uses that position to figure out where the capture starts.
                                                    // Opcodes which push onto this stack are always paired with other opcodes
                                                    // which will pop the value from it later.  A successful match should mean
                                                    // that this stack is empty. 

        protected internal int[] _runcrawl;         // The crawl stack is used to keep track of captures.  Every time a group 
        protected internal int _runcrawlpos;        // has a capture, we push its group number onto the runcrawl stack.  In 
                                                    // the case of a balanced match, we push BOTH groups onto the stack. 

        protected internal int _runtrackcount;      // count of states that may do backtracking

        protected internal Match _runmatch;         // result object
        protected internal Regex _runregex;         // regex object

        private Int32 _timeout;                     // timeout in millisecs (needed for actual)        
        private bool _ignoreTimeout;
        private Int32 _timeoutOccursAt;


        // We have determined this value in a series of experiments where x86 retail
        // builds (ono-lab-optimised) were run on different pattern/input pairs. Larger values
        // of TimeoutCheckFrequency did not tend to increase performance; smaller values
        // of TimeoutCheckFrequency tended to slow down the execution.
        private const int TimeoutCheckFrequency = 1000;
        private int _timeoutChecksToSkip;

        protected internal RegexRunner() { }

        /*
         * Scans the string to find the first match. Uses the Match object
         * both to feed text in and as a place to store matches that come out.
         *
         * All the action is in the abstract Go() method defined by subclasses. Our
         * responsibility is to load up the class members (as done here) before
         * calling Go.
         *
         * The optimizer can compute a set of candidate starting characters,
         * and we could use a separate method Skip() that will quickly scan past
         * any characters that we know can't match.
         */
        protected internal Match Scan(Regex regex, String text, int textbeg, int textend, int textstart, int prevlen, bool quick)
        {
            return Scan(regex, text, textbeg, textend, textstart, prevlen, quick, regex.MatchTimeout);
        }

        internal Match Scan(Regex regex, String text, int textbeg, int textend, int textstart, int prevlen, bool quick, TimeSpan timeout)
        {
            int bump;
            int stoppos;
            bool initted = false;

            // We need to re-validate timeout here because Scan is historically protected and
            // thus there is a possibility it is called from user code:
            Regex.ValidateMatchTimeout(timeout);

            _ignoreTimeout = (Regex.InfiniteMatchTimeout == timeout);
            _timeout = _ignoreTimeout
                                    ? (Int32)Regex.InfiniteMatchTimeout.TotalMilliseconds
                                    : (Int32)(timeout.TotalMilliseconds + 0.5); // Round            

            _runregex = regex;
            _runtext = text;
            _runtextbeg = textbeg;
            _runtextend = textend;
            _runtextstart = textstart;

            bump = _runregex.RightToLeft ? -1 : 1;
            stoppos = _runregex.RightToLeft ? _runtextbeg : _runtextend;

            _runtextpos = textstart;

            // If previous match was empty or failed, advance by one before matching

            if (prevlen == 0)
            {
                if (_runtextpos == stoppos)
                    return Match.Empty;

                _runtextpos += bump;
            }

            StartTimeoutWatch();

            for (; ;)
            {
#if DEBUG
                if (_runregex.Debug)
                {
                    Debug.WriteLine("");
                    Debug.WriteLine("Search range: from " + _runtextbeg.ToString(CultureInfo.InvariantCulture) + " to " + _runtextend.ToString(CultureInfo.InvariantCulture));
                    Debug.WriteLine("Firstchar search starting at " + _runtextpos.ToString(CultureInfo.InvariantCulture) + " stopping at " + stoppos.ToString(CultureInfo.InvariantCulture));
                }
#endif
                if (FindFirstChar())
                {
                    CheckTimeout();

                    if (!initted)
                    {
                        InitMatch();
                        initted = true;
                    }
#if DEBUG
                    if (_runregex.Debug)
                    {
                        Debug.WriteLine("Executing engine starting at " + _runtextpos.ToString(CultureInfo.InvariantCulture));
                        Debug.WriteLine("");
                    }
#endif
                    Go();

                    if (_runmatch._matchcount[0] > 0)
                    {
                        // We'll return a match even if it touches a previous empty match
                        return TidyMatch(quick);
                    }

                    // reset state for another go
                    _runtrackpos = _runtrack.Length;
                    _runstackpos = _runstack.Length;
                    _runcrawlpos = _runcrawl.Length;
                }

                // failure!

                if (_runtextpos == stoppos)
                {
                    TidyMatch(true);
                    return Match.Empty;
                }

                // Recognize leading []* and various anchors, and bump on failure accordingly

                // Bump by one and start again

                _runtextpos += bump;
            }
            // We never get here
        }

        private void StartTimeoutWatch()
        {
            if (_ignoreTimeout)
                return;

            _timeoutChecksToSkip = TimeoutCheckFrequency;

            // We are using Environment.TickCount and not Timewatch for performance reasons.
            // Environment.TickCount is an int that cycles. We intentionally let timeoutOccursAt
            // overflow it will still stay ahead of Environment.TickCount for comparisons made
            // in DoCheckTimeout():
            unchecked
            {
                _timeoutOccursAt = Environment.TickCount + _timeout;
            }
        }

        internal void CheckTimeout()
        {
            if (_ignoreTimeout)
                return;

            if (--_timeoutChecksToSkip != 0)
                return;

            _timeoutChecksToSkip = TimeoutCheckFrequency;
            DoCheckTimeout();
        }

        private void DoCheckTimeout()
        {
            // Note that both, Environment.TickCount and timeoutOccursAt are ints and can overflow and become negative.
            // See the comment in StartTimeoutWatch().

            int currentMillis = Environment.TickCount;

            if (currentMillis < _timeoutOccursAt)
                return;

            if (0 > _timeoutOccursAt && 0 < currentMillis)
                return;

#if DEBUG
            if (_runregex.Debug)
            {
                Debug.WriteLine("");
                Debug.WriteLine("RegEx match timeout occurred!");
                Debug.WriteLine("Specified timeout:       " + TimeSpan.FromMilliseconds(_timeout).ToString());
                Debug.WriteLine("Timeout check frequency: " + TimeoutCheckFrequency);
                Debug.WriteLine("Search pattern:          " + _runregex._pattern);
                Debug.WriteLine("Input:                   " + _runtext);
                Debug.WriteLine("About to throw RegexMatchTimeoutException.");
            }
#endif

            throw new RegexMatchTimeoutException(_runtext, _runregex._pattern, TimeSpan.FromMilliseconds(_timeout));
        }

        /*
         * The responsibility of Go() is to run the regular expression at
         * runtextpos and call Capture() on all the captured subexpressions,
         * then to leave runtextpos at the ending position. It should leave
         * runtextpos where it started if there was no match.
         */
        protected abstract void Go();

        /*
         * The responsibility of FindFirstChar() is to advance runtextpos
         * until it is at the next position which is a candidate for the
         * beginning of a successful match.
         */
        protected abstract bool FindFirstChar();

        /*
         * InitTrackCount must initialize the runtrackcount field; this is
         * used to know how large the initial runtrack and runstack arrays
         * must be.
         */
        protected abstract void InitTrackCount();

        /*
         * Initializes all the data members that are used by Go()
         */
        private void InitMatch()
        {
            // Use a hashtable'ed Match object if the capture numbers are sparse

            if (_runmatch == null)
            {
                if (_runregex._caps != null)
                    _runmatch = new MatchSparse(_runregex, _runregex._caps, _runregex._capsize, _runtext, _runtextbeg, _runtextend - _runtextbeg, _runtextstart);
                else
                    _runmatch = new Match(_runregex, _runregex._capsize, _runtext, _runtextbeg, _runtextend - _runtextbeg, _runtextstart);
            }
            else
            {
                _runmatch.Reset(_runregex, _runtext, _runtextbeg, _runtextend, _runtextstart);
            }

            // note we test runcrawl, because it is the last one to be allocated
            // If there is an alloc failure in the middle of the three allocations,
            // we may still return to reuse this instance, and we want to behave
            // as if the allocations didn't occur. (we used to test _trackcount != 0)

            if (_runcrawl != null)
            {
                _runtrackpos = _runtrack.Length;
                _runstackpos = _runstack.Length;
                _runcrawlpos = _runcrawl.Length;
                return;
            }

            InitTrackCount();

            int tracksize = _runtrackcount * 8;
            int stacksize = _runtrackcount * 8;

            if (tracksize < 32)
                tracksize = 32;
            if (stacksize < 16)
                stacksize = 16;

            _runtrack = new int[tracksize];
            _runtrackpos = tracksize;

            _runstack = new int[stacksize];
            _runstackpos = stacksize;

            _runcrawl = new int[32];
            _runcrawlpos = 32;
        }

        /*
         * Put match in its canonical form before returning it.
         */
        private Match TidyMatch(bool quick)
        {
            if (!quick)
            {
                Match match = _runmatch;

                _runmatch = null;

                match.Tidy(_runtextpos);
                return match;
            }
            else
            {
                // in quick mode, a successful match returns null, and
                // the allocated match object is left in the cache

                return null;
            }
        }

        /*
         * Called by the implemenation of Go() to increase the size of storage
         */
        protected void EnsureStorage()
        {
            if (_runstackpos < _runtrackcount * 4)
                DoubleStack();
            if (_runtrackpos < _runtrackcount * 4)
                DoubleTrack();
        }

        /*
         * Called by the implemenation of Go() to decide whether the pos
         * at the specified index is a boundary or not. It's just not worth
         * emitting inline code for this logic.
         */
        protected bool IsBoundary(int index, int startpos, int endpos)
        {
            return (index > startpos && RegexCharClass.IsWordChar(_runtext[index - 1])) !=
                   (index < endpos && RegexCharClass.IsWordChar(_runtext[index]));
        }

        protected bool IsECMABoundary(int index, int startpos, int endpos)
        {
            return (index > startpos && RegexCharClass.IsECMAWordChar(_runtext[index - 1])) !=
                   (index < endpos && RegexCharClass.IsECMAWordChar(_runtext[index]));
        }

        protected static bool CharInSet(char ch, String set, String category)
        {
            string charClass = RegexCharClass.ConvertOldStringsToClass(set, category);
            return RegexCharClass.CharInClass(ch, charClass);
        }

        protected static bool CharInClass(char ch, String charClass)
        {
            return RegexCharClass.CharInClass(ch, charClass);
        }

        /*
         * Called by the implemenation of Go() to increase the size of the
         * backtracking stack.
         */
        protected void DoubleTrack()
        {
            int[] newtrack;

            newtrack = new int[_runtrack.Length * 2];

            System.Array.Copy(_runtrack, 0, newtrack, _runtrack.Length, _runtrack.Length);
            _runtrackpos += _runtrack.Length;
            _runtrack = newtrack;
        }

        /*
         * Called by the implemenation of Go() to increase the size of the
         * grouping stack.
         */
        protected void DoubleStack()
        {
            int[] newstack;

            newstack = new int[_runstack.Length * 2];

            System.Array.Copy(_runstack, 0, newstack, _runstack.Length, _runstack.Length);
            _runstackpos += _runstack.Length;
            _runstack = newstack;
        }

        /*
         * Increases the size of the longjump unrolling stack.
         */
        protected void DoubleCrawl()
        {
            int[] newcrawl;

            newcrawl = new int[_runcrawl.Length * 2];

            System.Array.Copy(_runcrawl, 0, newcrawl, _runcrawl.Length, _runcrawl.Length);
            _runcrawlpos += _runcrawl.Length;
            _runcrawl = newcrawl;
        }

        /*
         * Save a number on the longjump unrolling stack
         */
        protected void Crawl(int i)
        {
            if (_runcrawlpos == 0)
                DoubleCrawl();

            _runcrawl[--_runcrawlpos] = i;
        }

        /*
         * Remove a number from the longjump unrolling stack
         */
        protected int Popcrawl()
        {
            return _runcrawl[_runcrawlpos++];
        }

        /*
         * Get the height of the stack
         */
        protected int Crawlpos()
        {
            return _runcrawl.Length - _runcrawlpos;
        }

        /*
         * Called by Go() to capture a subexpression. Note that the
         * capnum used here has already been mapped to a non-sparse
         * index (by the code generator RegexWriter).
         */
        protected void Capture(int capnum, int start, int end)
        {
            if (end < start)
            {
                int T;

                T = end;
                end = start;
                start = T;
            }

            Crawl(capnum);
            _runmatch.AddMatch(capnum, start, end - start);
        }

        /*
         * Called by Go() to capture a subexpression. Note that the
         * capnum used here has already been mapped to a non-sparse
         * index (by the code generator RegexWriter).
         */
        protected void TransferCapture(int capnum, int uncapnum, int start, int end)
        {
            int start2;
            int end2;

            // these are the two intervals that are cancelling each other

            if (end < start)
            {
                int T;

                T = end;
                end = start;
                start = T;
            }

            start2 = MatchIndex(uncapnum);
            end2 = start2 + MatchLength(uncapnum);

            // The new capture gets the innermost defined interval

            if (start >= end2)
            {
                end = start;
                start = end2;
            }
            else if (end <= start2)
            {
                start = start2;
            }
            else
            {
                if (end > end2)
                    end = end2;
                if (start2 > start)
                    start = start2;
            }

            Crawl(uncapnum);
            _runmatch.BalanceMatch(uncapnum);

            if (capnum != -1)
            {
                Crawl(capnum);
                _runmatch.AddMatch(capnum, start, end - start);
            }
        }

        /*
         * Called by Go() to revert the last capture
         */
        protected void Uncapture()
        {
            int capnum = Popcrawl();
            _runmatch.RemoveMatch(capnum);
        }

        /*
         * Call out to runmatch to get around visibility issues
         */
        protected bool IsMatched(int cap)
        {
            return _runmatch.IsMatched(cap);
        }

        /*
         * Call out to runmatch to get around visibility issues
         */
        protected int MatchIndex(int cap)
        {
            return _runmatch.MatchIndex(cap);
        }

        /*
         * Call out to runmatch to get around visibility issues
         */
        protected int MatchLength(int cap)
        {
            return _runmatch.MatchLength(cap);
        }

#if DEBUG
        /*
         * Dump the current state
         */
        internal virtual void DumpState()
        {
            Debug.WriteLine("Text:  " + TextposDescription());
            Debug.WriteLine("Track: " + StackDescription(_runtrack, _runtrackpos));
            Debug.WriteLine("Stack: " + StackDescription(_runstack, _runstackpos));
        }

        internal static String StackDescription(int[] A, int Index)
        {
            StringBuilder Sb = new StringBuilder();

            Sb.Append(A.Length - Index);
            Sb.Append('/');
            Sb.Append(A.Length);

            if (Sb.Length < 8)
                Sb.Append(' ', 8 - Sb.Length);

            Sb.Append("(");

            for (int i = Index; i < A.Length; i++)
            {
                if (i > Index)
                    Sb.Append(' ');
                Sb.Append(A[i]);
            }

            Sb.Append(')');

            return Sb.ToString();
        }

        internal virtual String TextposDescription()
        {
            StringBuilder Sb = new StringBuilder();
            int remaining;

            Sb.Append(_runtextpos);

            if (Sb.Length < 8)
                Sb.Append(' ', 8 - Sb.Length);

            if (_runtextpos > _runtextbeg)
                Sb.Append(RegexCharClass.CharDescription(_runtext[_runtextpos - 1]));
            else
                Sb.Append('^');

            Sb.Append('>');

            remaining = _runtextend - _runtextpos;

            for (int i = _runtextpos; i < _runtextend; i++)
            {
                Sb.Append(RegexCharClass.CharDescription(_runtext[i]));
            }
            if (Sb.Length >= 64)
            {
                Sb.Length = 61;
                Sb.Append("...");
            }
            else
            {
                Sb.Append('$');
            }

            return Sb.ToString();
        }
#endif
    }
}
