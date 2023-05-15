// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// This RegexRunner class is a base class for source-generated regex extensibility
// (and the old CompileToAssembly extensibility).  It's not intended to be used
// by anything else.

// Implementation notes:

// It provides the driver code that call's the subclass's Scan
// method for either scanning or direct execution.
// It also maintains memory allocation for the backtracking stack,
// the grouping stack and the longjump crawlstack, and provides
// methods to push new subpattern match results into (or remove
// backtracked results from) the Match instance.

using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace System.Text.RegularExpressions
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public abstract class RegexRunner
    {
        protected internal int runtextbeg;         // Beginning of text to search. We now always use a sliced span of the input
                                                   // from runtextbeg to runtextend, which means that runtextbeg is now always 0 except
                                                   // for CompiledToAssembly scenario which works over the original input.
        protected internal int runtextend;         // End of text to search. Because we now pass in a sliced span of the input into Scan,
                                                   // the runtextend will always match the length of that passed in span except for CompileToAssembly
                                                   // scenario, which still works over the original input.
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

        private protected RegexRunnerMode _mode;   // the mode in which the runner is currently operating

        private int _timeout;                      // timeout in milliseconds
        private bool _checkTimeout;
        private long _timeoutOccursAt;

        protected RegexRunner() { }

        /// <summary>Used by a <see cref="Regex"/> object to scan the input <paramref name="text"/> looking for the next match.</summary>
        /// <remarks>This API supports the product infrastructure and is not intended to be used directly from your code.</remarks>
        /// <param name="text">The text to scan for a pattern match.</param>
        /// <exception cref="NotSupportedException">
        /// <see cref="ReadOnlySpan{T}"/>-based <see cref="Regex"/> methods are not supported from <see cref="Regex"/>-derived types
        /// generated by Regex.CompileToAssembly.
        /// </exception>
        protected internal virtual void Scan(ReadOnlySpan<char> text)
        {
            // This base implementation is overridden by all of the built-in engines and by all source-generated
            // implementations.  The only time this should end up being used is if someone is using a Regex-derived
            // type created by .NET Framework's Regex.CompileToAssembly, in which case it will have overridden
            // FindFirstChar and Go but not Scan (which didn't exist yet).  This isn't an officially supported configuration,
            // using assemblies built for .NET Framework and targeting .NET Framework surface area against this
            // implementation, but we make a best-effort to keep things functional.
            string? s = runtext;

            // We can assume that the passed in 'text' span is a slice of the original text input runtext. That said we need to calculate
            // what the original beginning was and can't do it by just using the lengths of text and runtext, since we can't guarantee that
            // the passed in beginning and length match the size of the original input. We instead use MemoryExtensions Overlaps to find the
            // offset in memory between them. We intentionally use s.Overlaps(text) since we want to get a positive value.
            s.AsSpan().Overlaps(text, out int beginning);

            // The passed in span is sliced from runtextbeg to runtextend already, but in the precompiled scenario
            // we require to use the complete input and to use the full string instead. We first test to ensure that the
            // passed in span matches the original input by using the original runtextbeg. If that is not the case,
            // then it means the user is calling the new span-based APIs using CompiledToAssembly, so we throw NSE
            // so as to prevent a lot of unexpected allocations.
            if (s == null || text != s.AsSpan(beginning, text.Length))
            {
                // If we landed here then we are dealing with a CompiledToAssembly case where the new Span overloads are being called.
                throw new NotSupportedException(SR.UsingSpanAPIsWithCompiledToAssembly);
            }

            // If the original beginning wasn't zero, then we have to adjust some of the
            // internal fields of RegexRunner to ensure the Precompiled Go and FFC methods
            // will continue to work as expected since they work over the original input, as opposed
            // to using the sliced span.
            if (beginning != 0)
            {
                runtextbeg = beginning;
                runtextstart += beginning;
                runtextend += beginning;
            }

            InternalScan(runregex!, beginning, beginning + text.Length);
        }

        [Obsolete(Obsoletions.RegexExtensibilityImplMessage, DiagnosticId = Obsoletions.RegexExtensibilityDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        protected Match? Scan(Regex regex, string text, int textbeg, int textend, int textstart, int prevlen, bool quick) =>
            Scan(regex, text, textbeg, textend, textstart, prevlen, quick, regex.MatchTimeout);

        /// <summary>
        /// This method's body is only kept since it is a protected member that could be called by someone outside
        /// the assembly.
        /// </summary>
        [Obsolete(Obsoletions.RegexExtensibilityImplMessage, DiagnosticId = Obsoletions.RegexExtensibilityDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        protected internal Match? Scan(Regex regex, string text, int textbeg, int textend, int textstart, int prevlen, bool quick, TimeSpan timeout)
        {
            InitializeTimeout(timeout);

            RegexRunnerMode mode = quick ? RegexRunnerMode.ExistenceRequired : RegexRunnerMode.FullMatchRequired;

            // We set runtext before calling InitializeForScan so that runmatch object is initialized with the text
            runtext = text;

            InitializeForScan(regex, text, textstart, mode);

            // InitializeForScan will default runtextstart and runtextend to 0 and length of string
            // since it is configured to work over a sliced portion of text so we adjust those values.
            runtextstart = textstart;
            runtextend = textend;

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

            // If previous match was empty or failed, advance by one before matching.
            if (prevlen == 0)
            {
                if (textstart == stoppos)
                {
                    return Match.Empty;
                }

                runtextpos += bump;
            }

            Match match = InternalScan(regex, textbeg, textend);
            runtext = null; //drop reference

            if (match.FoundMatch)
            {
                if (quick)
                {
                    return null;
                }

                runmatch = null;
                match.Tidy(runtextpos, 0, mode);
            }
            else
            {
                runmatch!.Text = null;
            }

            return match;

        }

        private Match InternalScan(Regex regex, int textbeg, int textend)
        {
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

            while (true)
            {
                // Find the next potential location for a match in the input.
                if (FindFirstChar())
                {
                    CheckTimeout();

                    // See if there's a match at this position.
                    Go();

                    if (runmatch!.FoundMatch)
                    {
                        return runmatch;
                    }

                    // Reset state for another iteration.
                    runtrackpos = runtrack!.Length;
                    runstackpos = runstack!.Length;
                    runcrawlpos = runcrawl!.Length;
                }

                // We failed to match at this position.  If we're at the stopping point, we're done.
                if (runtextpos == stoppos)
                {
                    return Match.Empty;
                }

                // Bump by one (in whichever direction is appropriate) and loop to go again.
                runtextpos += bump;
            }
        }

        internal void InitializeForScan(Regex regex, ReadOnlySpan<char> text, int textstart, RegexRunnerMode mode)
        {
            // Store remaining arguments into fields now that we're going to start the scan.
            // These are referenced by the derived runner.
            _mode = mode;
            runregex = regex;
            runtextstart = textstart;
            runtextbeg = 0;
            runtextend = text.Length;
            runtextpos = textstart;

            Match? m = runmatch;
            if (m is null)
            {
                // Use a hashtabled Match object if the capture numbers are sparse
                runmatch = runregex!.caps is null ?
                    new Match(runregex, runregex.capsize, runtext, text.Length) :
                    new MatchSparse(runregex, runregex.caps, runregex.capsize, runtext, text.Length);
            }
            else
            {
                m.Reset(runtext, text.Length);
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

        internal void InitializeTimeout(TimeSpan timeout)
        {
            // Handle timeout argument
            _checkTimeout = false;
            if (Regex.InfiniteMatchTimeout != timeout)
            {
                ConfigureTimeout(timeout);

                void ConfigureTimeout(TimeSpan timeout)
                {
                    _checkTimeout = true;
                    _timeout = (int)(timeout.TotalMilliseconds + 0.5); // Round;
                    _timeoutOccursAt = Environment.TickCount64 + _timeout;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected internal void CheckTimeout()
        {
            if (_checkTimeout && Environment.TickCount64 >= _timeoutOccursAt)
            {
                ThrowRegexTimeout();
            }

            void ThrowRegexTimeout() => throw new RegexMatchTimeoutException(runtext ?? string.Empty, runregex!.pattern!, TimeSpan.FromMilliseconds(_timeout));
        }

        /// <summary>
        /// The responsibility of Go() is to run the regular expression at
        /// runtextpos and call Capture() on all the captured subexpressions,
        /// then to leave runtextpos at the ending position. It should leave
        /// runtextpos where it started if there was no match.
        /// </summary>
        protected virtual void Go() => throw new NotImplementedException();

        /// <summary>
        /// The responsibility of FindFirstChar() is to advance runtextpos
        /// until it is at the next position which is a candidate for the
        /// beginning of a successful match.
        /// </summary>
        protected virtual bool FindFirstChar() => throw new NotImplementedException();

        /// <summary>
        /// InitTrackCount must initialize the runtrackcount field; this is
        /// used to know how large the initial runtrack and runstack arrays
        /// must be.
        /// </summary>
        protected virtual void InitTrackCount() { }

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
            return (index > startpos && RegexCharClass.IsBoundaryWordChar(runtext![index - 1])) !=
                   (index < endpos && RegexCharClass.IsBoundaryWordChar(runtext![index]));
        }

        internal static bool IsBoundary(ReadOnlySpan<char> inputSpan, int index)
        {
            int indexM1 = index - 1;
            return ((uint)indexM1 < (uint)inputSpan.Length && RegexCharClass.IsBoundaryWordChar(inputSpan[indexM1])) !=
                   ((uint)index < (uint)inputSpan.Length && RegexCharClass.IsBoundaryWordChar(inputSpan[index]));
        }

        /// <summary>Called to determine a char's inclusion in the \w set.</summary>
        internal static bool IsWordChar(char ch) => RegexCharClass.IsWordChar(ch);

        protected bool IsECMABoundary(int index, int startpos, int endpos)
        {
            return (index > startpos && RegexCharClass.IsECMAWordChar(runtext![index - 1])) !=
                   (index < endpos && RegexCharClass.IsECMAWordChar(runtext![index]));
        }

        internal static bool IsECMABoundary(ReadOnlySpan<char> inputSpan, int index)
        {
            int indexM1 = index - 1;
            return ((uint)indexM1 < (uint)inputSpan.Length && RegexCharClass.IsECMAWordChar(inputSpan[indexM1])) !=
                   ((uint)index < (uint)inputSpan.Length && RegexCharClass.IsECMAWordChar(inputSpan[index]));
        }

        [Obsolete(Obsoletions.RegexExtensibilityImplMessage, DiagnosticId = Obsoletions.RegexExtensibilityDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        protected static bool CharInSet(char ch, string set, string category)
        {
            string charClass = RegexCharClass.ConvertOldStringsToClass(set, category);
            return RegexCharClass.CharInClass(ch, charClass);
        }

        public static bool CharInClass(char ch, string charClass)
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
    }
}
