// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using Xunit;

namespace Microsoft.Diagnostics.DataContractReader.DumpTests;

/// <summary>
/// Shared infrastructure for the CallSiteLayout dump tests. Each ABI gets its
/// own subclass (<see cref="CallSiteLayoutDumpTests_WinX64"/>, etc.) that
/// applies <c>[SkipOnOS]</c> / <c>[SkipOnArch]</c> attributes and encodes
/// the expected ABI-specific layout for every frame.
/// </summary>
public abstract class CallSiteLayoutDumpTestsBase : DumpTestBase
{
    protected override string DebuggeeName => "CallSiteLayout";
    protected override string DumpType => "full";

    // Deepest non-FailFast frame on the chain. Used to discover the
    // FailFast thread; resolution then visits every frame above it.
    private const string LeafFrame = "M_Combo_RefStructWithMultipleRefs";

    /// <summary>
    /// Walks the FailFast thread once, recording the MethodDescHandle of
    /// every named frame visited. Frame names beyond the chain are ignored.
    /// </summary>
    protected Dictionary<string, MethodDescHandle> CollectChainMethods()
    {
        ThreadData thread = DumpTestHelpers.FindThreadWithMethod(Target, LeafFrame);
        IStackWalk stackWalk = Target.Contracts.StackWalk;
        IRuntimeTypeSystem rts = Target.Contracts.RuntimeTypeSystem;

        Dictionary<string, MethodDescHandle> result = new();
        foreach (IStackDataFrameHandle frame in stackWalk.CreateStackWalk(thread))
        {
            TargetPointer mdPtr = stackWalk.GetMethodDescPtr(frame);
            if (mdPtr == TargetPointer.Null)
                continue;
            MethodDescHandle md = rts.GetMethodDescHandle(mdPtr);
            string? name = DumpTestHelpers.GetMethodName(Target, md);
            if (name is not null && !result.ContainsKey(name))
                result[name] = md;
        }

        return result;
    }

    /// <summary>
    /// Looks up the named frame on the FailFast thread and computes its layout.
    /// </summary>
    protected CallSiteLayout LayoutFor(string methodName)
    {
        Dictionary<string, MethodDescHandle> methods = CollectChainMethods();
        Assert.True(methods.TryGetValue(methodName, out MethodDescHandle md),
            $"'{methodName}' frame not found on the FailFast thread");
        return Target.Contracts.CallingConvention.ComputeCallSiteLayout(md);
    }

    // ===== Common assertion helpers =====

    /// <summary>Asserts a single-arg signature where the arg is passed by managed/implicit byref.</summary>
    protected void AssertSingleByRef(string frame)
    {
        CallSiteLayout layout = LayoutFor(frame);
        Assert.Single(layout.Arguments);
        ArgLayout arg = layout.Arguments[0];
        Assert.True(arg.IsPassedByRef, $"{frame}: expected IsPassedByRef=true");
        Assert.Null(arg.ValueTypeHandle);
    }

    /// <summary>Asserts a single-arg signature passed by value with the given value-type name.</summary>
    protected void AssertSingleByValueVT(string frame, string typeName)
    {
        CallSiteLayout layout = LayoutFor(frame);
        Assert.Single(layout.Arguments);
        ArgLayout arg = layout.Arguments[0];
        Assert.False(arg.IsPassedByRef, $"{frame}: expected IsPassedByRef=false");
        Assert.NotNull(arg.ValueTypeHandle);
        Assert.Equal(typeName, DumpTestHelpers.GetTypeName(Target, arg.ValueTypeHandle.Value));
    }

    /// <summary>Asserts a single-arg signature for a managed object reference (no VTH, not byref).</summary>
    protected void AssertSingleManagedRef(string frame)
    {
        CallSiteLayout layout = LayoutFor(frame);
        Assert.Single(layout.Arguments);
        ArgLayout arg = layout.Arguments[0];
        Assert.False(arg.IsPassedByRef, $"{frame}: expected IsPassedByRef=false");
        Assert.Null(arg.ValueTypeHandle);
    }
}
