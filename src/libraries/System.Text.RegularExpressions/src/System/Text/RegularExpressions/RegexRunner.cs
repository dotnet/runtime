// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
using System.Diagnostics.CodeAnalysis;

namespace System.Text.RegularExpressions
{
    public abstract class RegexRunner
    {
        protected internal int runtextbeg;         // beginning of text to search
        protected internal int runtextend;         // end of text to search
        protected internal int runtextstart;       // starting point for search

        protected internal string? runtext;        // text to search
        protected internal int runtextpos;         // current position in text

        protected internal int[]? runtrack;        // The backtracking stack.  Opcodes use this to store data regarding
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

        protected internal int[]? runstack;        // This stack is used to track text positions across different opcodes.
        protected internal int runstackpos;        // For example, in /(a*b)+/, the parentheses result in a SetMark/CaptureMark
                                                   // pair. SetMark records the text position before we match a*b.  Then
                                                   // CaptureMark uses that position to figure out where the capture starts.
                                                   // Opcodes which push onto this stack are always paired with other opcodes
                                                   // which will pop the value from it later.  A successful match should mean
                                                   // that this stack is empty.

        protected internal int[]? runcrawl;        // The crawl stack is used to keep track of captures.  Every time a group
        protected internal int runcrawlpos;        // has a capture, we push its group number onto the runcrawl stack.  In
                                                   // the case of a balanced match, we push BOTH groups onto the stack.

        protected internal int runtrackcount;      // count of states that may do backtracking

        protected internal Match? runmatch;        // result object
        protected internal Regex? runregex;        // regex object

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
        protected internal Match? Scan(Regex regex, string text, int textbeg, int textend, int textstart, int prevlen, bool quick) =>
            Scan(regex, text, textbeg, textend, textstart, prevlen, quick, regex.MatchTimeout);

        protected internal Match? Scan(Regex regex, string text, int textbeg, int textend, int textstart, int prevlen, bool quick, TimeSpan timeout)
        {
            // Handle timeout argument
            _timeout = -1; // (int)Regex.InfiniteMatchTimeout.TotalMilliseconds
            bool ignoreTimeout = _ignoreTimeout = Regex.InfiniteMatchTimeout == timeout;
            if (!ignoreTimeout)
            {
                // We are using Environment.TickCount and not Stopwatch for performance reasons.
                // Environment.TickCount is an int that cycles. We intentionally let timeoutOccursAt
                // overflow it will still stay ahead of Environment.TickCount for comparisons made
                // in DoCheckTimeout().
                Regex.ValidateMatchTimeout(timeout); // validate timeout as this could be called from user code due to being protected
                _timeout = (int)(timeout.TotalMilliseconds + 0.5); // Round;
                _timeoutOccursAt = Environment.TickCount + _timeout;
                _timeoutChecksToSkip = TimeoutCheckFrequency;
            }

            // Configure the additional value to "bump" the position along each time we loop around
            // to call FindFirstChar again, as well as the stopping position for the loop.  We generally
            // bump by 1 and stop at textend, but if we're examining right-to-left, we instead bump
            // by -1 and stop at textbeg.
            int bump = 1, stoppos = textend;
            if (regex.RightToLeft)
            {
                bump = -1;
                stoppos = textbeg;
            }

            // Store runtextpos into field, as we may bump it in next check.  The remaining arguments
            // are stored below once we're past the potential return in the next check.
            runtextpos = textstart;

            // If previous match was empty or failed, advance by one before matching.
            if (prevlen == 0)
            {
                if (textstart == stoppos)
                {
                    return Match.Empty;
                }

                runtextpos += bump;
            }

            // Store remaining arguments into fields now that we're going to start the scan.
            // These are referenced by the derived runner.
            runregex = regex;
            runtext = text;
            runtextstart = textstart;
            runtextbeg = textbeg;
            runtextend = textend;

            // Main loop: FindFirstChar/Go + bump until the ending position.
            bool initialized = false;
            while (true)
            {
#if DEBUG
                if (regex.IsDebug)
                {
                    Debug.WriteLine("");
                    Debug.WriteLine($"Search range: from {runtextbeg} to {runtextend}");
                    Debug.WriteLine($"Firstchar search starting at {runtextpos} stopping at {stoppos}");
                }
#endif

                // Find the next potential location for a match in the input.
                if (FindFirstChar())
                {
                    if (!ignoreTimeout)
                    {
                        DoCheckTimeout();
                    }

                    // Ensure that the runner is initialized.  This includes initializing all of the state in the runner
                    // that Go might use, such as the backtracking stack, as well as a Match object for it to populate.
                    if (!initialized)
                    {
                        InitializeForGo();
                        initialized = true;
                    }

#if DEBUG
                    if (regex.IsDebug)
                    {
                        Debug.WriteLine($"Executing engine starting at {runtextpos}");
                        Debug.WriteLine("");
                    }
#endif

                    // See if there's a match at this position.
                    Go();

                    // If we got a match, we're done.
                    Match match = runmatch!;
                    if (match._matchcount[0] > 0)
                    {
                        runtext = null; // drop reference to text to avoid keeping it alive in a cache

                        if (quick)
                        {
                            runmatch!.Text = null!; // drop reference
                            return null;
                        }

                        // Return the match in its canonical form.
                        runmatch = null;
                        match.Tidy(runtextpos);
                        return match;
                    }

                    // Reset state for another iteration.
                    runtrackpos = runtrack!.Length;
                    runstackpos = runstack!.Length;
                    runcrawlpos = runcrawl!.Length;
                }

                // We failed to match at this position.  If we're at the stopping point, we're done.
                if (runtextpos == stoppos)
                {
                    runtext = null; // drop reference to text to avoid keeping it alive in a cache
                    if (runmatch != null) runmatch.Text = null!;
                    return Match.Empty;
                }

                // Bump by one (in whichever direction is appropriate) and loop to go again.
                runtextpos += bump;
            }
        }

        /// <summary>Enumerates all of the matches with the specified regex, invoking the callback for each.</summary>
        /// <remarks>
        /// This optionally repeatedly hands out the same Match instance, updated with new information.
        /// <paramref name="reuseMatchObject"/> should be set to false if the Match object is handed out to user code.
        /// </remarks>
        internal void Scan<TState>(Regex regex, string text, int textstart, ref TState state, MatchCallback<TState> callback, bool reuseMatchObject, TimeSpan timeout)
        {
            // Handle timeout argument
            _timeout = -1; // (int)Regex.InfiniteMatchTimeout.TotalMilliseconds
            bool ignoreTimeout = _ignoreTimeout = Regex.InfiniteMatchTimeout == timeout;
            if (!ignoreTimeout)
            {
                // We are using Environment.TickCount and not Stopwatch for performance reasons.
                // Environment.TickCount is an int that cycles. We intentionally let timeoutOccursAt
                // overflow it will still stay ahead of Environment.TickCount for comparisons made
                // in DoCheckTimeout().
                _timeout = (int)(timeout.TotalMilliseconds + 0.5); // Round;
                _timeoutOccursAt = Environment.TickCount + _timeout;
                _timeoutChecksToSkip = TimeoutCheckFrequency;
            }

            // Configure the additional value to "bump" the position along each time we loop around
            // to call FindFirstChar again, as well as the stopping position for the loop.  We generally
            // bump by 1 and stop at text.Length, but if we're examining right-to-left, we instead bump
            // by -1 and stop at 0.
            int bump = 1, stoppos = text.Length;
            if (regex.RightToLeft)
            {
                bump = -1;
                stoppos = 0;
            }

            // Store remaining arguments into fields now that we're going to start the scan.
            // These are referenced by the derived runner.
            runregex = regex;
            runtextstart = runtextpos = textstart;
            runtext = text;
            runtextend = text.Length;
            runtextbeg = 0;

            // Main loop: FindFirstChar/Go + bump until the ending position.
            bool initialized = false;
            while (true)
            {
#if DEBUG
                if (regex.IsDebug)
                {
                    Debug.WriteLine("");
                    Debug.WriteLine($"Search range: from {runtextbeg} to {runtextend}");
                    Debug.WriteLine($"Firstchar search starting at {runtextpos} stopping at {stoppos}");
                }
#endif

                // Find the next potential location for a match in the input.
                if (FindFirstChar())
                {
                    if (!ignoreTimeout)
                    {
                        DoCheckTimeout();
                    }

                    // Ensure that the runner is initialized.  This includes initializing all of the state in the runner
                    // that Go might use, such as the backtracking stack, as well as a Match object for it to populate.
                    if (!initialized)
                    {
                        InitializeForGo();
                        initialized = true;
                    }

#if DEBUG
                    if (regex.IsDebug)
                    {
                        Debug.WriteLine($"Executing engine starting at {runtextpos}");
                        Debug.WriteLine("");
                    }
#endif

                    // See if there's a match at this position.
                    Go();

                    // See if we have a match.
                    Match match = runmatch!;
                    if (match._matchcount[0] > 0)
                    {
                        // Hand it out to the callback in canonical form.
                        if (!reuseMatchObject)
                        {
                            // We're not reusing match objects, so null out our field reference to the instance.
                            // It'll be recreated the next time one is needed.
                            runmatch = null;
                        }
                        match.Tidy(runtextpos);
                        initialized = false;
                        if (!callback(ref state, match))
                        {
                            // If the callback returns false, we're done.
                            // Drop reference to text to avoid keeping it alive in a cache.
                            runtext = null!;
                            if (reuseMatchObject)
                            {
                                // We're reusing the single match instance, so clear out its text as well.
                                // We don't do this if we're not reusing instances, as in that case we're
                                // dropping the whole reference to the match, and we no longer own the instance
                                // having handed it out to the callback.
                                match.Text = null!;
                            }
                            return;
                        }

                        // Now that we've matched successfully, update the starting position to reflect
                        // the current position, just as Match.NextMatch() would pass in _textpos as textstart.
                        runtextstart = runtextpos;

                        // Reset state for another iteration.
                        runtrackpos = runtrack!.Length;
                        runstackpos = runstack!.Length;
                        runcrawlpos = runcrawl!.Length;
                        if (match.Length == 0)
                        {
                            if (runtextpos == stoppos)
                            {
                                // Drop reference to text to avoid keeping it alive in a cache.
                                runtext = null!;
                                if (reuseMatchObject)
                                {
                                    // See above comment.
                                    match.Text = null!;
                                }
                                return;
                            }

                            runtextpos += bump;
                        }

                        // Loop around to perform next match from where we left off.
                        continue;
                    }

                    // Ran Go but it didn't find a match. Reset state for another iteration.
                    runtrackpos = runtrack!.Length;
                    runstackpos = runstack!.Length;
                    runcrawlpos = runcrawl!.Length;
                }

                // We failed to match at this position.  If we're at the stopping point, we're done.
                if (runtextpos == stoppos)
                {
                    runtext = null; // drop reference to text to avoid keeping it alive in a cache
                    if (runmatch != null)
                    {
                        runmatch.Text = null!;
                    }
                    return;
                }

                // Bump by one (in whichever direction is appropriate) and loop to go again.
                runtextpos += bump;
            }
        }

        protected void CheckTimeout()
        {
            if (_ignoreTimeout)
                return;

            DoCheckTimeout();
        }

        private void DoCheckTimeout()
        {
            if (--_timeoutChecksToSkip != 0)
                return;

            _timeoutChecksToSkip = TimeoutCheckFrequency;

            // Note that both, Environment.TickCount and timeoutOccursAt are ints and can overflow and become negative.
            // See the comment in StartTimeoutWatch().

            int currentMillis = Environment.TickCount;

            if (currentMillis < _timeoutOccursAt)
                return;

            if (0 > _timeoutOccursAt && 0 < currentMillis)
                return;

#if DEBUG
            if (runregex!.IsDebug)
            {
                Debug.WriteLine("");
                Debug.WriteLine("RegEx match timeout occurred!");
                Debug.WriteLine($"Specified timeout:       {TimeSpan.FromMilliseconds(_timeout)}");
                Debug.WriteLine($"Timeout check frequency: {TimeoutCheckFrequency}");
                Debug.WriteLine($"Search pattern:          {runregex.pattern}");
                Debug.WriteLine($"Input:                   {runtext}");
                Debug.WriteLine("About to throw RegexMatchTimeoutException.");
            }
#endif

            throw new RegexMatchTimeoutException(runtext!, runregex!.pattern!, TimeSpan.FromMilliseconds(_timeout));
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
        private void InitializeForGo()
        {
            if (runmatch is null)
            {
                // Use a hashtabled Match object if the capture numbers are sparse
                runmatch = runregex!.caps is null ?
                    new Match(runregex, runregex.capsize, runtext!, runtextbeg, runtextend - runtextbeg, runtextstart) :
                    new MatchSparse(runregex, runregex.caps, runregex.capsize, runtext!, runtextbeg, runtextend - runtextbeg, runtextstart);
            }
            else
            {
                runmatch.Reset(runregex!, runtext!, runtextbeg, runtextend, runtextstart);
            }

            // Note we test runcrawl, because it is the last one to be allocated
            // If there is an alloc failure in the middle of the three allocations,
            // we may still return to reuse this instance, and we want to behave
            // as if the allocations didn't occur.
            if (runcrawl != null)
            {
                runtrackpos = runtrack!.Length;
                runstackpos = runstack!.Length;
                runcrawlpos = runcrawl.Length;
                return;
            }

            // Everything above runs once per match.
            // Everything below runs once per runner.

            InitTrackCount();

            int stacksize;
            int tracksize = stacksize = runtrackcount * 8;

            if (tracksize < 32)
            {
                tracksize = 32;
            }
            if (stacksize < 16)
            {
                stacksize = 16;
            }

            runtrack = new int[tracksize];
            runtrackpos = tracksize;

            runstack = new int[stacksize];
            runstackpos = stacksize;

            runcrawl = new int[32];
            runcrawlpos = 32;
        }

        /// <summary>
        /// Called by the implementation of Go() to increase the size of storage
        /// </summary>
        protected void EnsureStorage()
        {
            int limit = runtrackcount * 4;

            if (runstackpos < limit)
                DoubleStack();

            if (runtrackpos < limit)
                DoubleTrack();
        }

        /// <summary>
        /// Called by the implementation of Go() to decide whether the pos
        /// at the specified index is a boundary or not. It's just not worth
        /// emitting inline code for this logic.
        /// </summary>
        protected bool IsBoundary(int index, int startpos, int endpos)
        {
            return (index > startpos && RegexCharClass.IsWordChar(runtext![index - 1])) !=
                   (index < endpos && RegexCharClass.IsWordChar(runtext![index]));
        }

        protected bool IsECMABoundary(int index, int startpos, int endpos)
        {
            return (index > startpos && RegexCharClass.IsECMAWordChar(runtext![index - 1])) !=
                   (index < endpos && RegexCharClass.IsECMAWordChar(runtext![index]));
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
            int[] newtrack = new int[runtrack!.Length * 2];

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
            int[] newstack = new int[runstack!.Length * 2];

            Array.Copy(runstack, 0, newstack, runstack.Length, runstack.Length);
            runstackpos += runstack.Length;
            runstack = newstack;
        }

        /// <summary>
        /// Increases the size of the longjump unrolling stack.
        /// </summary>
        protected void DoubleCrawl()
        {
            int[] newcrawl = new int[runcrawl!.Length * 2];

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

            runcrawl![--runcrawlpos] = i;
        }

        /// <summary>
        /// Remove a number from the longjump unrolling stack
        /// </summary>
        protected int Popcrawl()
        {
            return runcrawl![runcrawlpos++];
        }

        /// <summary>
        /// Get the height of the stack
        /// </summary>
        protected int Crawlpos()
        {
            return runcrawl!.Length - runcrawlpos;
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
                int t = end;
                end = start;
                start = t;
            }

            Crawl(capnum);
            runmatch!.AddMatch(capnum, start, end - start);
        }

        /// <summary>
        /// Called by Go() to capture a subexpression. Note that the
        /// capnum used here has already been mapped to a non-sparse
        /// index (by the code generator RegexWriter).
        /// </summary>
        protected void TransferCapture(int capnum, int uncapnum, int start, int end)
        {
            // these are the two intervals that are canceling each other

            if (end < start)
            {
                int t = end;
                end = start;
                start = t;
            }

            int start2 = MatchIndex(uncapnum);
            int end2 = start2 + MatchLength(uncapnum);

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
            runmatch!.BalanceMatch(uncapnum);

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
            runmatch!.RemoveMatch(capnum);
        }

        /// <summary>
        /// Call out to runmatch to get around visibility issues
        /// </summary>
        protected bool IsMatched(int cap)
        {
            return runmatch!.IsMatched(cap);
        }

        /// <summary>
        /// Call out to runmatch to get around visibility issues
        /// </summary>
        protected int MatchIndex(int cap)
        {
            return runmatch!.MatchIndex(cap);
        }

        /// <summary>
        /// Call out to runmatch to get around visibility issues
        /// </summary>
        protected int MatchLength(int cap)
        {
            return runmatch!.MatchLength(cap);
        }

#if DEBUG
        /// <summary>
        /// Dump the current state
        /// </summary>
        [ExcludeFromCodeCoverage(Justification = "Debug only")]
        internal virtual void DumpState()
        {
            Debug.WriteLine($"Text:  {TextposDescription()}");
            Debug.WriteLine($"Track: {StackDescription(runtrack!, runtrackpos)}");
            Debug.WriteLine($"Stack: {StackDescription(runstack!, runstackpos)}");
        }

        [ExcludeFromCodeCoverage(Justification = "Debug only")]
        private static string StackDescription(int[] a, int index)
        {
            var sb = new StringBuilder();

            sb.Append(a.Length - index);
            sb.Append('/');
            sb.Append(a.Length);

            if (sb.Length < 8)
            {
                sb.Append(' ', 8 - sb.Length);
            }

            sb.Append('(');

            for (int i = index; i < a.Length; i++)
            {
                if (i > index)
                {
                    sb.Append(' ');
                }
                sb.Append(a[i]);
            }

            sb.Append(')');

            return sb.ToString();
        }

        [ExcludeFromCodeCoverage(Justification = "Debug only")]
        internal virtual string TextposDescription()
        {
            var sb = new StringBuilder();

            sb.Append(runtextpos);

            if (sb.Length < 8)
            {
                sb.Append(' ', 8 - sb.Length);
            }

            if (runtextpos > runtextbeg)
            {
                sb.Append(RegexCharClass.CharDescription(runtext![runtextpos - 1]));
            }
            else
            {
                sb.Append('^');
            }

            sb.Append('>');

            for (int i = runtextpos; i < runtextend; i++)
            {
                sb.Append(RegexCharClass.CharDescription(runtext![i]));
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
