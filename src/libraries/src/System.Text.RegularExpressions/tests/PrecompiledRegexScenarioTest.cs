using System;
using System.Text.RegularExpressions;
using RegexTestNamespace;
using Xunit;

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
        }
    }
}

namespace RegexTestNamespace
{
    public class RegexTestClass : Regex
    {
        public RegexTestClass()
        {
            this.pattern = @".*\B(SUCCESS)\B.*";
            this.roptions = RegexOptions.IgnoreCase;
            this.internalMatchTimeout = TimeSpan.FromMinutes(1);
            this.factory = new RegexFactoryTestClass();
            this.capsize = 2;
            base.InitializeReferences();
        }

        public RegexTestClass(TimeSpan timeSpan) : this()
        {
            Regex.ValidateMatchTimeout(timeSpan);
            this.internalMatchTimeout = timeSpan;
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
                    if (7 <= runtextend - num && char.ToLower(runtext[num]) == 's' && char.ToLower(runtext[num + 1]) == 'u' && char.ToLower(runtext[num + 2]) == 'c' && char.ToLower(runtext[num + 3]) == 'c' && char.ToLower(runtext[num + 4]) == 'e' && char.ToLower(runtext[num + 5]) == 's' && char.ToLower(runtext[num + 6]) == 's')
                    {
                        num += 7;
                        this.CheckTimeout();
                        num4 = runstack[num3++];
                        this.Capture(1, num4, num);
                        runtrack[--num2] = num4;
                        runtrack[--num2] = 3;
                        this.CheckTimeout();
                        if (!this.IsBoundary(num, runtextbeg, runtextend))
                        {
                            break;
                        }
                    }
                }
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
                            goto IL_39A;
                        case 3:
                            this.CheckTimeout();
                            runstack[--num3] = runtrack[num2++];
                            this.Uncapture();
                            continue;
                        case 4:
                            goto IL_415;
                    }
                    goto IL_371;
                }
                IL_39A:
                this.CheckTimeout();
                num = runtrack[num2++];
                num4 = runtrack[num2++];
                if (num4 > 0)
                {
                    runtrack[--num2] = num4 - 1;
                    runtrack[--num2] = num - 1;
                    runtrack[--num2] = 2;
                }
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
                runtrack[--num2] = 4;
            }
            IL_2D3:
            this.CheckTimeout();
            num4 = runstack[num3++];
            this.Capture(0, num4, num);
            runtrack[--num2] = num4;
            runtrack[num2 - 1] = 3;
            IL_309:
            this.CheckTimeout();
            this.runtextpos = num;
            return;
            IL_371:
            this.CheckTimeout();
            num = runtrack[num2++];
            goto IL_309;
            IL_415:
            this.CheckTimeout();
            num = runtrack[num2++];
            num4 = runtrack[num2++];
            if (num4 > 0)
            {
                runtrack[--num2] = num4 - 1;
                runtrack[--num2] = num - 1;
                runtrack[--num2] = 4;
                goto IL_2D3;
            }
            goto IL_2D3;
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
                    if (RegexRunner.CharInClass(char.ToLower(runtext[num++]), "\0\u0003\0\0\n\v"))
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
            this.runtrackcount = 7;
        }
    }
}
