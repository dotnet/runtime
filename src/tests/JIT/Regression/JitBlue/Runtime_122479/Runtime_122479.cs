// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Under STRESS_TAILCALL the JIT importer would promote a "call+ret" pattern
// to an explicit (or implicit) tailcall before discovering that the callee
// is a named intrinsic (e.g. GC.KeepAlive, which is imported as a
// GT_KEEPALIVE node, not a CALL). When combined with a [SuppressGCTransition]
// P/Invoke earlier in the same block, the resulting BBJ_RETURN ended with a
// non-CALL/non-RETURN statement and tripped an assert in fgInsertStmtNearEnd
// during "Insert GC Polls".
//
// Run under any jitstress / jitstress_tiered leg to validate.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using Xunit;

public class Runtime_122479
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ReproPattern()
    {
        // Thread.Sleep -> Thread.ClearWaitSleepJoinState which has the
        // exact "SuppressGCTransition QCall; GC.KeepAlive(this); ret" shape
        // that originally triggered the assert.
        Thread.Sleep(0);
    }

    [Fact]
    public static void TestEntryPoint()
    {
        for (int i = 0; i < 100; i++)
        {
            ReproPattern();
        }
    }
}
