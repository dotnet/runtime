// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Text.RegularExpressions;
using RegexTestNamespace;
using Xunit;
using System.Collections.Generic;

namespace System.Text.RegularExpressionsTests
{
    public class PrecompiledRegexScenarioTest
    {
        [Fact]
        public void TestPrecompiledRegex()
        {
            string text = "asdf134success1245something";
            RegexTestClass testClass = new RegexTestClass();

            Assert.Equal(1, testClass.Matches(text).Count);
            Assert.Equal(1, testClass.Match(text).Groups[0].Captures.Count);
            Assert.Equal(text, testClass.Match(text).Groups[0].Value);
            Assert.Equal(new int[] { 0, 1, 2}, testClass.GetGroupNumbers());
            Assert.Equal(new string[] { "0", "1", "output" }, testClass.GetGroupNames());
        }
    }
}

namespace RegexTestNamespace
{
    public class RegexTestClass : Regex
    {
        public RegexTestClass()
        {
            pattern = ".*\\B(\\d+)(?<output>SUCCESS)\\B.*";
            roptions = RegexOptions.IgnoreCase;
            internalMatchTimeout = TimeSpan.FromTicks(-10000L);
            factory = new RegexFactoryTestClass();
            Caps = new Hashtable {{0, 0}, {1, 1}, {2, 2}};
            CapNames = new Hashtable {{"0", 0}, {"1", 1}, {"output", 2}};
            capslist = new string[3];
            capslist[0] = "0";
            capslist[1] = "1";
            capslist[2] = "output";
            capsize = 3;
            base.InitializeReferences();
        }

        public RegexTestClass(TimeSpan timeSpan) : this()
        {
            Regex.ValidateMatchTimeout(timeSpan);
            internalMatchTimeout = timeSpan;
        }
    }

    internal class RegexFactoryTestClass : RegexRunnerFactory
    {
        protected override RegexRunner CreateInstance()
        {
            return new RegexRunnerTestClass();
        }
    }

    internal class RegexRunnerTestClass : RegexRunner
    {
        protected override void Go()
        {
            string runtext = this.runtext;
            int runtextstart = this.runtextstart;
            int runtextbeg = this.runtextbeg;
            int runtextend = this.runtextend;
            int num = this.runtextpos;
            int[] runtrack = this.runtrack;
            int num2 = this.runtrackpos;
            int[] runstack = this.runstack;
            int num3 = this.runstackpos;
            this.CheckTimeout();
            runtrack[--num2] = num;
            runtrack[--num2] = 0;
            this.CheckTimeout();
            runstack[--num3] = num;
            runtrack[--num2] = 1;
            this.CheckTimeout();
            int num5;
            int num4 = (num5 = runtextend - num) + 1;
            while (--num4 > 0)
            {
                if (char.ToLower(runtext[num++]) == '\n')
                {
                    num--;
                    break;
                }
            }
            if (num5 > num4)
            {
                runtrack[--num2] = num5 - num4 - 1;
                runtrack[--num2] = num - 1;
                runtrack[--num2] = 2;
            }
            while (true)
            {
                this.CheckTimeout();
                if (!this.IsBoundary(num, runtextbeg, runtextend))
                {
                    this.CheckTimeout();
                    runstack[--num3] = num;
                    runtrack[--num2] = 1;
                    this.CheckTimeout();
                    if (1 <= runtextend - num)
                    {
                        num++;
                        num4 = 1;
                        while (RegexRunner.CharInClass(char.ToLower(runtext[num - num4--]), "\0\0\u0001\t"))
                        {
                            if (num4 <= 0)
                            {
                                this.CheckTimeout();
                                num4 = (num5 = runtextend - num) + 1;
                                while (--num4 > 0)
                                {
                                    if (!RegexRunner.CharInClass(char.ToLower(runtext[num++]), "\0\0\u0001\t"))
                                    {
                                        num--;
                                        break;
                                    }
                                }
                                if (num5 > num4)
                                {
                                    runtrack[--num2] = num5 - num4 - 1;
                                    runtrack[--num2] = num - 1;
                                    runtrack[--num2] = 3;
                                    goto IL_204;
                                }
                                goto IL_204;
                            }
                        }
                    }
                }
                IL_441:
                while (true)
                {
                    this.runtrackpos = num2;
                    this.runstackpos = num3;
                    this.EnsureStorage();
                    num2 = this.runtrackpos;
                    num3 = this.runstackpos;
                    runtrack = this.runtrack;
                    runstack = this.runstack;
                    switch (runtrack[num2++])
                    {
                        case 1:
                            this.CheckTimeout();
                            num3++;
                            continue;
                        case 2:
                            goto IL_4C7;
                        case 3:
                            goto IL_51D;
                        case 4:
                            this.CheckTimeout();
                            runstack[--num3] = runtrack[num2++];
                            this.Uncapture();
                            continue;
                        case 5:
                            goto IL_598;
                    }
                    goto IL_49E;
                }
                IL_4C7:
                this.CheckTimeout();
                num = runtrack[num2++];
                num4 = runtrack[num2++];
                if (num4 > 0)
                {
                    runtrack[--num2] = num4 - 1;
                    runtrack[--num2] = num - 1;
                    runtrack[--num2] = 2;
                    continue;
                }
                continue;
                IL_51D:
                this.CheckTimeout();
                num = runtrack[num2++];
                num4 = runtrack[num2++];
                if (num4 > 0)
                {
                    runtrack[--num2] = num4 - 1;
                    runtrack[--num2] = num - 1;
                    runtrack[--num2] = 3;
                }
                IL_204:
                this.CheckTimeout();
                num4 = runstack[num3++];
                this.Capture(1, num4, num);
                runtrack[--num2] = num4;
                runtrack[--num2] = 4;
                this.CheckTimeout();
                runstack[--num3] = num;
                runtrack[--num2] = 1;
                this.CheckTimeout();
                if (7 > runtextend - num || char.ToLower(runtext[num]) != 's' || char.ToLower(runtext[num + 1]) != 'u' || char.ToLower(runtext[num + 2]) != 'c' || char.ToLower(runtext[num + 3]) != 'c' || char.ToLower(runtext[num + 4]) != 'e' || char.ToLower(runtext[num + 5]) != 's' || char.ToLower(runtext[num + 6]) != 's')
                {
                    goto IL_441;
                }
                num += 7;
                this.CheckTimeout();
                num4 = runstack[num3++];
                this.Capture(2, num4, num);
                runtrack[--num2] = num4;
                runtrack[--num2] = 4;
                this.CheckTimeout();
                if (!this.IsBoundary(num, runtextbeg, runtextend))
                {
                    break;
                }
                goto IL_441;
            }
            this.CheckTimeout();
            num4 = (num5 = runtextend - num) + 1;
            while (--num4 > 0)
            {
                if (char.ToLower(runtext[num++]) == '\n')
                {
                    num--;
                    break;
                }
            }
            if (num5 > num4)
            {
                runtrack[--num2] = num5 - num4 - 1;
                runtrack[--num2] = num - 1;
                runtrack[--num2] = 5;
            }
            IL_3FC:
            this.CheckTimeout();
            num4 = runstack[num3++];
            this.Capture(0, num4, num);
            runtrack[--num2] = num4;
            runtrack[num2 - 1] = 4;
            IL_432:
            this.CheckTimeout();
            this.runtextpos = num;
            return;
            IL_49E:
            this.CheckTimeout();
            num = runtrack[num2++];
            goto IL_432;
            IL_598:
            this.CheckTimeout();
            num = runtrack[num2++];
            num4 = runtrack[num2++];
            if (num4 > 0)
            {
                runtrack[--num2] = num4 - 1;
                runtrack[--num2] = num - 1;
                runtrack[--num2] = 5;
                goto IL_3FC;
            }
            goto IL_3FC;
        }

        protected override bool FindFirstChar()
        {
            int num = this.runtextpos;
            string runtext = this.runtext;
            int num2 = this.runtextend - num;
            if (num2 > 0)
            {
                do
                {
                    num2--;
                    if (RegexRunner.CharInClass(char.ToLower(runtext[num++]), "\0\u0003\u0001\0\n\v\t"))
                    {
                        goto IL_63;
                    }
                }
                while (num2 > 0);
                bool arg_74_0 = false;
                goto IL_6C;
                IL_63:
                num--;
                arg_74_0 = true;
                IL_6C:
                this.runtextpos = num;
                return arg_74_0;
            }
            return false;
        }

        protected override void InitTrackCount()
        {
            this.runtrackcount = 10;
        }
    }
}
