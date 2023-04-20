// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using Xunit;

namespace System.Text.RegularExpressions.Tests
{
    public class CustomDerivedRegexScenarioTest
    {
        [Fact]
        public void CallProtectedScanMethodFromCustomDerivedRegex()
        {
            CustomDerivedRegex regex = new();
            Assert.True(regex.CallScanDirectly(regex, "3456", 0, 4, 0, -1, false).Success);
            Assert.False(regex.CallScanDirectly(regex, "456", 0, 3, 0, -1, false).Success);
            Assert.Equal("45", regex.CallScanDirectly(regex, "45456", 0, 5, 0, -1, false).Value);
            Assert.Equal("896", regex.CallScanDirectly(regex, "45896456", 0, 8, 2, -1, false).Value);
            Assert.Equal(Match.Empty, regex.CallScanDirectly(regex, "I dont match", 0, 12, 0, -1, false));
            Assert.Null(regex.CallScanDirectly(regex, "3456", 0, 4, 0, -1, true));
        }

    }

    /// <summary>
    /// This type was generated using an earlier version of the Regex Source Generator which still overrides Go and FindFirstChar.
    /// The purpose of this class is to validate that if a derived RegexRunner is invoking the protected Scan methods, they should call
    /// the overridden Go and FindFirstChar methods and return the expected results.
    /// </summary>
    internal class CustomDerivedRegex : Regex
    {
        private CustomRegexRunnerFactory.CustomRegexRunner runner;

        public CustomDerivedRegex()
        {
            pattern = @"\G(\d{1,3})(?=(?:\d{3})+\b)";
            roptions = RegexOptions.Compiled;
            internalMatchTimeout = Timeout.InfiniteTimeSpan;
            factory = new CustomRegexRunnerFactory();
            capsize = 2;
            MethodInfo createRunnerMethod = typeof(Regex).GetMethod("CreateRunner", BindingFlags.Instance | BindingFlags.NonPublic);
            runner = createRunnerMethod.Invoke(this, new object[] { }) as CustomRegexRunnerFactory.CustomRegexRunner;
        }

        public Match? CallScanDirectly(Regex regex, string text, int textbeg, int textend, int textstart, int prevlen, bool quick)
            => runner.CallScanDirectly(regex, text, textbeg, textend, textstart, prevlen, quick);

        internal class CustomRegexRunnerFactory : RegexRunnerFactory
        {
            protected override RegexRunner CreateInstance() => new CustomRegexRunner();

            internal class CustomRegexRunner : RegexRunner
            {
                public Match? CallScanDirectly(Regex regex, string text, int textbeg, int textend, int textstart, int prevlen, bool quick)
#pragma warning disable SYSLIB0052 // Type or member is obsolete
                    => Scan(regex, text, textbeg, textend, textstart, prevlen, quick);
#pragma warning restore SYSLIB0052 // Type or member is obsolete

                protected override void InitTrackCount() => base.runtrackcount = 12;

                // Description:
                // * Match if at the start position.
                // * 1st capture group.
                //     * Match a Unicode digit greedily at least 1 and at most 3 times.
                // * Zero-width positive lookahead assertion.
                //     * Loop greedily at least once.
                //         * Match a Unicode digit exactly 3 times.
                //     * Match if at a word boundary.

                protected override bool FindFirstChar()
                {
                    int pos = runtextpos, end = runtextend;

                    if (pos < end)
                    {
                        // Start \G anchor
                        if (pos > runtextstart)
                        {
                            goto NoStartingPositionFound;
                        }
                        return true;
                    }

                // No starting position found
                NoStartingPositionFound:
                    runtextpos = end;
                    return false;
                }

                protected override void Go()
                {
                    ReadOnlySpan<char> inputSpan = runtext.AsSpan();
                    int pos = base.runtextpos, end = base.runtextend;
                    int original_pos = pos;
                    int charloop_starting_pos = 0, charloop_ending_pos = 0;
                    int loop_iteration = 0, loop_starting_pos = 0;
                    int stackpos = 0;
                    int start = base.runtextstart;
                    ReadOnlySpan<char> slice = inputSpan.Slice(pos, end - pos);

                    // Match if at the start position.
                    {
                        if (pos != start)
                        {
                            goto NoMatch;
                        }
                    }

                    // 1st capture group.
                    //{
                    int capture_starting_pos = pos;

                    // Match a Unicode digit greedily at least 1 and at most 3 times.
                    //{
                    charloop_starting_pos = pos;

                    int iteration = 0;
                    while (iteration < 3 && (uint)iteration < (uint)slice.Length && char.IsDigit(slice[iteration]))
                    {
                        iteration++;
                    }

                    if (iteration == 0)
                    {
                        goto NoMatch;
                    }

                    slice = slice.Slice(iteration);
                    pos += iteration;

                    charloop_ending_pos = pos;
                    charloop_starting_pos++;
                    goto CharLoopEnd;

                CharLoopBacktrack:
                    UncaptureUntil(base.runstack![--stackpos]);
                    StackPop2(base.runstack, ref stackpos, out charloop_ending_pos, out charloop_starting_pos);

                    if (charloop_starting_pos >= charloop_ending_pos)
                    {
                        goto NoMatch;
                    }
                    pos = --charloop_ending_pos;
                    slice = inputSpan.Slice(pos, end - pos);

                CharLoopEnd:
                    StackPush3(ref base.runstack!, ref stackpos, charloop_starting_pos, charloop_ending_pos, base.Crawlpos());
                    //}

                    base.Capture(1, capture_starting_pos, pos);

                    StackPush1(ref base.runstack!, ref stackpos, capture_starting_pos);
                    goto SkipBacktrack;

                CaptureBacktrack:
                    capture_starting_pos = base.runstack![--stackpos];
                    goto CharLoopBacktrack;

                SkipBacktrack:;
                    //}

                    // Zero-width positive lookahead assertion.
                    {
                        int positivelookahead_starting_pos = pos;

                        // Loop greedily at least once.
                        //{
                        loop_iteration = 0;
                        loop_starting_pos = pos;

                    LoopBody:
                        StackPush3(ref base.runstack!, ref stackpos, base.Crawlpos(), loop_starting_pos, pos);

                        loop_starting_pos = pos;
                        loop_iteration++;

                        // Match a Unicode digit exactly 3 times.
                        {
                            if ((uint)slice.Length < 3 ||
                                !char.IsDigit(slice[0]) ||
                                !char.IsDigit(slice[1]) ||
                                !char.IsDigit(slice[2]))
                            {
                                goto LoopIterationNoMatch;
                            }
                        }

                        pos += 3;
                        slice = slice.Slice(3);
                        if (pos != loop_starting_pos || loop_iteration == 0)
                        {
                            goto LoopBody;
                        }
                        goto LoopEnd;

                    LoopIterationNoMatch:
                        loop_iteration--;
                        if (loop_iteration < 0)
                        {
                            goto CaptureBacktrack;
                        }
                        StackPop2(base.runstack, ref stackpos, out pos, out loop_starting_pos);
                        UncaptureUntil(base.runstack![--stackpos]);
                        slice = inputSpan.Slice(pos, end - pos);
                        if (loop_iteration == 0)
                        {
                            goto CaptureBacktrack;
                        }
                        if (loop_iteration == 0)
                        {
                            goto CaptureBacktrack;
                        }
                    LoopEnd:;
                        //}

                        // Match if at a word boundary.
                        {
                            if (!base.IsBoundary(pos, base.runtextbeg, end))
                            {
                                goto LoopIterationNoMatch;
                            }
                        }

                        pos = positivelookahead_starting_pos;
                        slice = inputSpan.Slice(pos, end - pos);
                    }

                    // The input matched.
                    base.runtextpos = pos;
                    base.Capture(0, original_pos, pos);
                    return;

                // The input didn't match.
                NoMatch:
                    UncaptureUntil(0);
                    return;

                    // <summary>Pop 2 values from the backtracking stack.</summary>
                    [MethodImpl(MethodImplOptions.AggressiveInlining)]
                    static void StackPop2(int[] stack, ref int pos, out int arg0, out int arg1)
                    {
                        arg0 = stack[--pos];
                        arg1 = stack[--pos];
                    }

                    // <summary>Push 1 value onto the backtracking stack.</summary>
                    [MethodImpl(MethodImplOptions.AggressiveInlining)]
                    static void StackPush1(ref int[] stack, ref int pos, int arg0)
                    {
                        // If there's space available for the value, store it.
                        int[] s = stack;
                        int p = pos;
                        if ((uint)p < (uint)s.Length)
                        {
                            s[p] = arg0;
                            pos++;
                            return;
                        }

                        // Otherwise, resize the stack to make room and try again.
                        WithResize(ref stack, ref pos, arg0);

                        // <summary>Resize the backtracking stack array and push 1 value onto the stack.</summary>
                        [MethodImpl(MethodImplOptions.NoInlining)]
                        static void WithResize(ref int[] stack, ref int pos, int arg0)
                        {
                            Array.Resize(ref stack, (pos + 0) * 2);
                            StackPush1(ref stack, ref pos, arg0);
                        }
                    }

                    // <summary>Push 3 values onto the backtracking stack.</summary>
                    [MethodImpl(MethodImplOptions.AggressiveInlining)]
                    static void StackPush3(ref int[] stack, ref int pos, int arg0, int arg1, int arg2)
                    {
                        // If there's space available for all 3 values, store them.
                        int[] s = stack;
                        int p = pos;
                        if ((uint)(p + 2) < (uint)s.Length)
                        {
                            s[p] = arg0;
                            s[p + 1] = arg1;
                            s[p + 2] = arg2;
                            pos += 3;
                            return;
                        }

                        // Otherwise, resize the stack to make room and try again.
                        WithResize(ref stack, ref pos, arg0, arg1, arg2);

                        // <summary>Resize the backtracking stack array and push 3 values onto the stack.</summary>
                        [MethodImpl(MethodImplOptions.NoInlining)]
                        static void WithResize(ref int[] stack, ref int pos, int arg0, int arg1, int arg2)
                        {
                            Array.Resize(ref stack, (pos + 2) * 2);
                            StackPush3(ref stack, ref pos, arg0, arg1, arg2);
                        }
                    }

                    // <summary>Undo captures until we reach the specified capture position.</summary>
                    [MethodImpl(MethodImplOptions.AggressiveInlining)]
                    void UncaptureUntil(int capturepos)
                    {
                        while (base.Crawlpos() > capturepos)
                        {
                            base.Uncapture();
                        }
                    }
                }
            }
        }
    }
}
