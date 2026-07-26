// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Regression test for a WebAssembly R2R (crossgen) codegen bug in object stack
// allocation. When a non-escaping box is stack allocated, ObjectAllocator
// rewrites the CORINFO_HELP_UNBOX call to CORINFO_HELP_UNBOX_TYPETEST. Unbox
// returns a byref while Unbox_TypeTest returns void, but the call node kept its
// byref type, so wasm emitted a value-returning call_indirect for a void helper
// and the engine trapped with "function signature mismatch".
//
// The mismatched call_indirect is guarded by an inline method table check that
// short-circuits the helper whenever the unbox type matches, so a same-type
// unbox compiles the bad instruction but never executes it. Driving a
// non-matching type through the same unbox site is what makes the slow path --
// and the trap -- actually run.
//
// Reproduces only under crossgen wasm R2R (TargetOS=browser), where the unfixed
// JIT aborts the module before InvalidCastException can be thrown. Passes
// trivially on all other targets.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class Runtime_131377
{
    [Fact]
    public static void TestEntryPoint()
    {
        // Matching type: the box is stack allocated and the inline method table
        // check succeeds, so the type-test helper is skipped.
        Unbox(null);

        // Non-matching type: the inline check fails, so the type-test helper
        // runs and reaches the call_indirect that trapped under the unfixed JIT.
        Assert.Throws<InvalidCastException>(() => Unbox("not a guid"));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Unbox(object o)
    {
        if (o is null)
        {
            // Non-escaping box, so this is stack allocated.
            o = new Guid();
        }

        Consume((Guid)o);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Consume<T>(T _)
    {
    }
}
