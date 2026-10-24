// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using Xunit;

// Guarded devirtualization must not drop the IL offset of the call it guards.
// Each caller makes a single interface call whose result is used, so the
// devirtualized call needs a return temp. Once Tier1 has guarded-devirtualized
// the call site (the profile only ever sees Impl), the stack frame for the
// caller must still report an IL offset inside the call's statement, as it
// does at Tier0.
public class Runtime_134523
{
    public interface IValue
    {
        int Inlined(int x);
        int NotInlined(int x);
    }

    public sealed class Impl : IValue
    {
        // Inlineable: after inlining, the call to Check ends up in the
        // statement created for the GDV return expression.
        public int Inlined(int x) => Check(x);

        // Not inlineable: GDV still devirtualizes the call and keeps it as a
        // direct call stored to the return temp.
        [MethodImpl(MethodImplOptions.NoInlining)]
        public int NotInlined(int x) => Check(x);
    }

    // The shape reported in the issue: a delegate returns a holder, and the
    // result of an inlined getter is dereferenced, so a null Data faults in
    // this frame (hardware NullReferenceException).
    public interface IHolder
    {
        object Data { get; }
    }

    public sealed class HolderImpl : IHolder
    {
        public object Data { get; set; }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool CallIssue(Func<IHolder> holderFactory, bool flag)
    {
        var holder = holderFactory();
        return flag && holder.Data.ToString() != null;
    }

    // IL range of CallIssue's 'return flag && ...' statement: everything after the
    // first statement (ldarg.0; callvirt Invoke; stloc.0).
    private static (int Lo, int Hi) IssueStatementRange()
    {
        MethodInfo method = typeof(Runtime_134523).GetMethod(nameof(CallIssue), BindingFlags.NonPublic | BindingFlags.Static);
        byte[] il = method.GetMethodBody().GetILAsByteArray();
        Assert.True(il.Length > 7 && il[0] == 0x02 && il[1] == 0x6F && il[6] == 0x0A, "unexpected IL shape in CallIssue");
        return (7, il.Length - 1);
    }

    private static int IssueFrameILOffset(Func<IHolder> holderFactory)
    {
        try
        {
            CallIssue(holderFactory, true);
        }
        catch (NullReferenceException ex)
        {
            foreach (StackFrame frame in new StackTrace(ex, false).GetFrames())
            {
                if (frame.GetMethod()?.Name == nameof(CallIssue))
                {
                    return frame.GetILOffset();
                }
            }
        }

        Assert.Fail("expected a NullReferenceException from CallIssue");
        return -1;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Check(int x)
    {
        if (x < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(x));
        }

        return x;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Consume(int x) { }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int CallInlined(IValue v, int x)
    {
        Consume(x);
        return v.Inlined(x) + 1;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int CallNotInlined(IValue v, int x)
    {
        Consume(x);
        return v.NotInlined(x) + 1;
    }

    // IL range of the statement 'return v.X(x) + 1;': from just after the
    // 'call Consume' instruction up to and including the 'callvirt'.
    private static (int Lo, int Hi) CallStatementRange(string methodName)
    {
        MethodInfo method = typeof(Runtime_134523).GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Static);
        byte[] il = method.GetMethodBody().GetILAsByteArray();
        int lo = -1;
        int hi = -1;
        int i = 0;
        while (i < il.Length)
        {
            switch (il[i])
            {
                case 0x28: // call <token>
                    if (lo < 0)
                    {
                        lo = i + 5;
                    }
                    i += 5;
                    break;
                case 0x6F: // callvirt <token>
                    hi = i;
                    i += 5;
                    break;
                default:
                    // Only one-byte opcodes without operands (ldarg.N, ldc.i4.1,
                    // add, ret, nop) occur otherwise in these two methods.
                    i += 1;
                    break;
            }
        }

        Assert.True(lo > 0 && hi >= lo, $"unexpected IL shape in {methodName}");
        return (lo, hi);
    }

    private static int ThrowingFrameILOffset(Func<IValue, int, int> caller, IValue v, string callerName)
    {
        try
        {
            caller(v, -1);
        }
        catch (ArgumentOutOfRangeException ex)
        {
            StackTrace trace = new StackTrace(ex, false);
            foreach (StackFrame frame in trace.GetFrames())
            {
                if (frame.GetMethod()?.Name == callerName)
                {
                    return frame.GetILOffset();
                }
            }

            Assert.Fail($"no frame for {callerName} in:{Environment.NewLine}{trace}");
        }

        Assert.Fail("expected ArgumentOutOfRangeException");
        return -1;
    }

    [Fact]
    public static void TestEntryPoint()
    {
        IValue v = new Impl();
        (int Lo, int Hi) inlinedRange = CallStatementRange(nameof(CallInlined));
        (int Lo, int Hi) notInlinedRange = CallStatementRange(nameof(CallNotInlined));

        // Tier0 reference: the frame lands inside the call statement.
        Assert.InRange(ThrowingFrameILOffset(CallInlined, v, nameof(CallInlined)), inlinedRange.Lo, inlinedRange.Hi);
        Assert.InRange(ThrowingFrameILOffset(CallNotInlined, v, nameof(CallNotInlined)), notInlinedRange.Lo, notInlinedRange.Hi);

        // The issue's shape faults in CallIssue itself, in its second statement.
        Func<IHolder> holderFactory = () => new HolderImpl();
        (int Lo, int Hi) issueRange = IssueStatementRange();
        Assert.InRange(IssueFrameILOffset(holderFactory), issueRange.Lo, issueRange.Hi);

        // Drive the callers through instrumented Tier0 to Tier1 with a
        // monomorphic class profile, so Tier1 guarded-devirtualizes the calls.
        // If tier-up has not finished by the checks below, they run against
        // Tier0 code and pass, so a slow machine can only make this vacuous.
        for (int i = 0; i < 100; i++)
        {
            for (int j = 0; j < 1000; j++)
            {
                CallInlined(v, j);
                CallNotInlined(v, j);
            }

            // As in the issue, warm CallIssue through the throwing path.
            for (int j = 0; j < 100; j++)
            {
                try
                {
                    CallIssue(holderFactory, true);
                }
                catch (NullReferenceException)
                {
                }
            }

            Thread.Sleep(20);
        }

        Thread.Sleep(200);

        // Check every caller before asserting, so one failure does not hide another.
        string failures = "";
        for (int i = 0; i < 20; i++)
        {
            failures += CheckCaller(CallInlined, v, nameof(CallInlined), inlinedRange);
            failures += CheckCaller(CallNotInlined, v, nameof(CallNotInlined), notInlinedRange);

            int issueOffset = IssueFrameILOffset(holderFactory);
            if ((issueOffset < issueRange.Lo) || (issueOffset > issueRange.Hi))
            {
                failures += $"{nameof(CallIssue)}: frame IL offset 0x{issueOffset:X} outside statement [0x{issueRange.Lo:X}, 0x{issueRange.Hi:X}]{Environment.NewLine}";
            }
        }

        Assert.True(failures.Length == 0, failures);
    }

    private static string CheckCaller(Func<IValue, int, int> caller, IValue v, string callerName, (int Lo, int Hi) range)
    {
        int offset = ThrowingFrameILOffset(caller, v, callerName);
        if ((offset >= range.Lo) && (offset <= range.Hi))
        {
            return "";
        }

        return $"{callerName}: frame IL offset 0x{offset:X} outside call statement [0x{range.Lo:X}, 0x{range.Hi:X}]{Environment.NewLine}";
    }
}
