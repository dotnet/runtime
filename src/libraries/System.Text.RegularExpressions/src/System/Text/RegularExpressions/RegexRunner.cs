// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// This RegexRunner class is a base class for compiled regex code.

// Implementation notes:

// It provides the driver code that call's the subclass's Go()
// method for either scanning or direct execution.
//
// It also maintains memory allocation for the backtracking stack,
// the grouping stack and the longjump crawlstack, and provides
// methods to push new subpattern match results into (or remove
// backtracked results from) the Match instance.

using System.Diagnostics;
using System.Globalization;

namespace System.Text.RegularExpressions
{
    public abstract class RegexRunner
    {
        protected internal int runtextbeg;         // beginning of text to search
        protected internal int runtextend;         // end of text to search
        protected internal int runtextstart;       // starting point for search

        protected internal string runtext;         // text to search
        protected internal int runtextpos;         // current position in text

        protected internal int[] runtrack;         // The backtracking stack.  Opcodes use this to store data regarding
        protected internal int runtrackpos;        // what they have matched and where to backtrack to.  Each "frame" on
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

        protected internal int[] runstack;         // This stack is used to track text positions across different opcodes.
        protected internal int runstackpos;        // For example, in /(a*b)+/, the parentheses result in a SetMark/CaptureMark
                                           // pair. SetMark records the text position before we match a*b.  Then
                                           // CaptureMark uses that position to figure out where the capture starts.
                                           // Opcodes which push onto this stack are always paired with other opcodes
                                           // which will pop the value from it later.  A successful match should mean
                                           // that this stack is empty.

        protected internal int[] runcrawl;         // The crawl stack is used to keep track of captures.  Every time a group
        protected internal int runcrawlpos;        // has a capture, we push its group number onto the runcrawl stack.  In
                                           // the case of a balanced match, we push BOTH groups onto the stack.

        protected internal int runtrackcount;      // count of states that may do backtracking

        protected internal Match runmatch;         // result object
        protected internal Regex runregex;         // regex object

        private int _timeout;              // timeout in milliseconds (needed for actual)
        private bool _ignoreTimeout;
        private int _timeoutOccursAt;


        // We have determined this value in a series of experiments where x86 retail
        // builds (ono-lab-optimized) were run on different pattern/input pairs. Larger values
        // of TimeoutCheckFrequency did not tend to increase performance; smaller values
        // of TimeoutCheckFrequency tended to slow down the execution.
        private const int TimeoutCheckFrequency = 1000;
        private int _timeoutChecksToSkip;

        protected internal RegexRunner() { }

        /// <summary>
        /// Scans the string to find the first match. Uses the Match object
        /// both to feed text in and as a place to store matches that come out.
        ///
        /// All the action is in the abstract Go() method defined by subclasses. Our
        /// responsibility is to load up the class members (as done here) before
        /// calling Go.
        ///
        /// The optimizer can compute a set of candidate starting characters,
        /// and we could use a separate method Skip() that will quickly scan past
        /// any characters that we know can't match.
        /// </summary>
        protected internal Match Scan(Regex regex, string text, int textbeg, int textend, int textstart, int prevlen, bool quick)
        {
            return Scan(regex, text, textbeg, textend, textstart, prevlen, quick, regex.MatchTimeout);
        }

        protected internal Match Scan(Regex regex, string text, int textbeg, int textend, int textstart, int prevlen, bool quick, TimeSpan timeout)
        {
            int bump;
            int stoppos;
            bool initted = false;

            // We need to re-validate timeout here because Scan is historically protected and
            // thus there is a possibility it is called from user code:
            Regex.ValidateMatchTimeout(timeout);

            _ignoreTimeout = (Regex.InfiniteMatchTimeout == timeout);
            _timeout = _ignoreTimeout
                                    ? (int)Regex.InfiniteMatchTimeout.TotalMilliseconds
                                    : (int)(timeout.TotalMilliseconds + 0.5); // Round

            runregex = regex;
            runtext = text;
            runtextbeg = textbeg;
            runtextend = textend;
            runtextstart = textstart;

            bump = runregex.RightToLeft ? -1 : 1;
            stoppos = runregex.RightToLeft ? runtextbeg : runtextend;

            runtextpos = textstart;

            // If previous match was empty or failed, advance by one before matching

            if (prevlen == 0)
            {
                if (runtextpos == stoppos)
                    return Match.Empty;

                runtextpos += bump;
            }

            StartTimeoutWatch();

            for (; ;)
            {
#if DEBUG
                if (runregex.Debug)
                {
                    Debug.WriteLine("");
                    Debug.WriteLine("Search range: from " + runtextbeg.ToString(CultureInfo.InvariantCulture) + " to " + runtextend.ToString(CultureInfo.InvariantCulture));
                    Debug.WriteLine("Firstchar search starting at " + runtextpos.ToString(CultureInfo.InvariantCulture) + " stopping at " + stoppos.ToString(CultureInfo.InvariantCulture));
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
                    if (runregex.Debug)
                    {
                        Debug.WriteLine("Executing engine starting at " + runtextpos.ToString(CultureInfo.InvariantCulture));
                        Debug.WriteLine("");
                    }
#endif
                    Go();

                    if (runmatch._matchcount[0] > 0)
                    {
                        // We'll return a match even if it touches a previous empty match
                        return TidyMatch(quick);
                    }

                    // reset state for another go
                    runtrackpos = runtrack.Length;
                    runstackpos = runstack.Length;
                    runcrawlpos = runcrawl.Length;
                }

                // failure!

                if (runtextpos == stoppos)
                {
                    TidyMatch(true);
                    return Match.Empty;
                }

                // Recognize leading []* and various anchors, and bump on failure accordingly

                // Bump by one and start again

                runtextpos += bump;
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

        protected void CheckTimeout()
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
            if (runregex.Debug)
            {
                Debug.WriteLine("");
                Debug.WriteLine("RegEx match timeout occurred!");
                Debug.WriteLine("Specified timeout:       " + TimeSpan.FromMilliseconds(_timeout).ToString());
                Debug.WriteLine("Timeout check frequency: " + TimeoutCheckFrequency);
                Debug.WriteLine("Search pattern:          " + runregex.pattern);
                Debug.WriteLine("Input:                   " + runtext);
                Debug.WriteLine("About to throw RegexMatchTimeoutException.");
            }
#endif

            throw new RegexMatchTimeoutException(runtext, runregex.pattern, TimeSpan.FromMilliseconds(_timeout));
        }

        /// <summary>
        /// The responsibility of Go() is to run the regular expression at
        /// runtextpos and call Capture() on all the captured subexpressions,
        /// then to leave runtextpos at the ending position. It should leave
        /// runtextpos where it started if there was no match.
        /// </summary>
        protected abstract void Go();

        /// <summary>
        /// The responsibility of FindFirstChar() is to advance runtextpos
        /// until it is at the next position which is a candidate for the
        /// beginning of a successful match.
        /// </summary>
        protected abstract bool FindFirstChar();

        /// <summary>
        /// InitTrackCount must initialize the runtrackcount field; this is
        /// used to know how large the initial runtrack and runstack arrays
        /// must be.
        /// </summary>
        protected abstract void InitTrackCount();

        /// <summary>
        /// Initializes all the data members that are used by Go()
        /// </summary>
        private void InitMatch()
        {
            // Use a hashtabled Match object if the capture numbers are sparse

            if (runmatch == null)
            {
                if (runregex.caps != null)
                    runmatch = new MatchSparse(runregex, runregex.caps, runregex.capsize, runtext, runtextbeg, runtextend - runtextbeg, runtextstart);
                else
                    runmatch = new Match(runregex, runregex.capsize, runtext, runtextbeg, runtextend - runtextbeg, runtextstart);
            }
            else
            {
                runmatch.Reset(runregex, runtext, runtextbeg, runtextend, runtextstart);
            }

            // note we test runcrawl, because it is the last one to be allocated
            // If there is an alloc failure in the middle of the three allocations,
            // we may still return to reuse this instance, and we want to behave
            // as if the allocations didn't occur. (we used to test _trackcount != 0)

            if (runcrawl != null)
            {
                runtrackpos = runtrack.Length;
                runstackpos = runstack.Length;
                runcrawlpos = runcrawl.Length;
                return;
            }

            InitTrackCount();

            int tracksize = runtrackcount * 8;
            int stacksize = runtrackcount * 8;

            if (tracksize < 32)
                tracksize = 32;
            if (stacksize < 16)
                stacksize = 16;

            runtrack = new int[tracksize];
            runtrackpos = tracksize;

            runstack = new int[stacksize];
            runstackpos = stacksize;

            runcrawl = new int[32];
            runcrawlpos = 32;
        }

        /// <summary>
        /// Put match in its canonical form before returning it.
        /// </summary>
        private Match TidyMatch(bool quick)
        {
            if (!quick)
            {
                Match match = runmatch;

                runmatch = null;

                match.Tidy(runtextpos);
                return match;
            }
            else
            {
                // in quick mode, a successful match returns null, and
                // the allocated match object is left in the cache

                return null;
            }
        }

        /// <summary>
        /// Called by the implementation of Go() to increase the size of storage
        /// </summary>
        protected void EnsureStorage()
        {
            if (runstackpos < runtrackcount * 4)
                DoubleStack();
            if (runtrackpos < runtrackcount * 4)
                DoubleTrack();
        }

        /// <summary>
        /// Called by the implementation of Go() to decide whether the pos
        /// at the specified index is a boundary or not. It's just not worth
        /// emitting inline code for this logic.
        /// </summary>
        protected bool IsBoundary(int index, int startpos, int endpos)
        {
            return (index > startpos && RegexCharClass.IsWordChar(runtext[index - 1])) !=
                   (index < endpos && RegexCharClass.IsWordChar(runtext[index]));
        }

        protected bool IsECMABoundary(int index, int startpos, int endpos)
        {
            return (index > startpos && RegexCharClass.IsECMAWordChar(runtext[index - 1])) !=
                   (index < endpos && RegexCharClass.IsECMAWordChar(runtext[index]));
        }

        protected static bool CharInSet(char ch, string set, string category)
        {
            string charClass = RegexCharClass.ConvertOldStringsToClass(set, category);
            return RegexCharClass.CharInClass(ch, charClass);
        }

        protected static bool CharInClass(char ch, string charClass)
        {
            return RegexCharClass.CharInClass(ch, charClass);
        }

        /// <summary>
        /// Called by the implementation of Go() to increase the size of the
        /// backtracking stack.
        /// </summary>
        protected void DoubleTrack()
        {
            int[] newtrack;

            newtrack = new int[runtrack.Length * 2];

            Array.Copy(runtrack, 0, newtrack, runtrack.Length, runtrack.Length);
            runtrackpos += runtrack.Length;
            runtrack = newtrack;
        }

        /// <summary>
        /// Called by the implementation of Go() to increase the size of the
        /// grouping stack.
        /// </summary>
        protected void DoubleStack()
        {
            int[] newstack;

            newstack = new int[runstack.Length * 2];

            Array.Copy(runstack, 0, newstack, runstack.Length, runstack.Length);
            runstackpos += runstack.Length;
            runstack = newstack;
        }

        /// <summary>
        /// Increases the size of the longjump unrolling stack.
        /// </summary>
        protected void DoubleCrawl()
        {
            int[] newcrawl;

            newcrawl = new int[runcrawl.Length * 2];

            Array.Copy(runcrawl, 0, newcrawl, runcrawl.Length, runcrawl.Length);
            runcrawlpos += runcrawl.Length;
            runcrawl = newcrawl;
        }

        /// <summary>
        /// Save a number on the longjump unrolling stack
        /// </summary>
        protected void Crawl(int i)
        {
            if (runcrawlpos == 0)
                DoubleCrawl();

            runcrawl[--runcrawlpos] = i;
        }

        /// <summary>
        /// Remove a number from the longjump unrolling stack
        /// </summary>
        protected int Popcrawl()
        {
            return runcrawl[runcrawlpos++];
        }

        /// <summary>
        /// Get the height of the stack
        /// </summary>
        protected int Crawlpos()
        {
            return runcrawl.Length - runcrawlpos;
        }

        /// <summary>
        /// Called by Go() to capture a subexpression. Note that the
        /// capnum used here has already been mapped to a non-sparse
        /// index (by the code generator RegexWriter).
        /// </summary>
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
            runmatch.AddMatch(capnum, start, end - start);
        }

        /// <summary>
        /// Called by Go() to capture a subexpression. Note that the
        /// capnum used here has already been mapped to a non-sparse
        /// index (by the code generator RegexWriter).
        /// </summary>
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
            runmatch.BalanceMatch(uncapnum);

            if (capnum != -1)
            {
                Crawl(capnum);
                runmatch.AddMatch(capnum, start, end - start);
            }
        }

        /*
         * Called by Go() to revert the last capture
         */
        protected void Uncapture()
        {
            int capnum = Popcrawl();
            runmatch.RemoveMatch(capnum);
        }

        /// <summary>
        /// Call out to runmatch to get around visibility issues
        /// </summary>
        protected bool IsMatched(int cap)
        {
            return runmatch.IsMatched(cap);
        }

        /// <summary>
        /// Call out to runmatch to get around visibility issues
        /// </summary>
        protected int MatchIndex(int cap)
        {
            return runmatch.MatchIndex(cap);
        }

        /// <summary>
        /// Call out to runmatch to get around visibility issues
        /// </summary>
        protected int MatchLength(int cap)
        {
            return runmatch.MatchLength(cap);
        }

#if DEBUG
        /// <summary>
        /// Dump the current state
        /// </summary>
        internal virtual void DumpState()
        {
            Debug.WriteLine("Text:  " + TextposDescription());
            Debug.WriteLine("Track: " + StackDescription(runtrack, runtrackpos));
            Debug.WriteLine("Stack: " + StackDescription(runstack, runstackpos));
        }

        internal static string StackDescription(int[] a, int index)
        {
            var sb = new StringBuilder();

            sb.Append(a.Length - index);
            sb.Append('/');
            sb.Append(a.Length);

            if (sb.Length < 8)
                sb.Append(' ', 8 - sb.Length);

            sb.Append('(');

            for (int i = index; i < a.Length; i++)
            {
                if (i > index)
                    sb.Append(' ');
                sb.Append(a[i]);
            }

            sb.Append(')');

            return sb.ToString();
        }

        internal virtual string TextposDescription()
        {
            var sb = new StringBuilder();
            int remaining;

            sb.Append(runtextpos);

            if (sb.Length < 8)
                sb.Append(' ', 8 - sb.Length);

            if (runtextpos > runtextbeg)
                sb.Append(RegexCharClass.CharDescription(runtext[runtextpos - 1]));
            else
                sb.Append('^');

            sb.Append('>');

            remaining = runtextend - runtextpos;

            for (int i = runtextpos; i < runtextend; i++)
            {
                sb.Append(RegexCharClass.CharDescription(runtext[i]));
            }
            if (sb.Length >= 64)
            {
                sb.Length = 61;
                sb.Append("...");
            }
            else
            {
                sb.Append('$');
            }

            return sb.ToString();
        }
#endif
    }
}
