// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using Xunit;

namespace Microsoft.Diagnostics.DataContractReader.TestInfrastructure;

/// <summary>
/// A single resolved stack frame, carrying the method name, the raw
/// MethodDesc pointer, the runtime Frame name (if this is a capital-F Frame),
/// and the underlying <see cref="IStackDataFrameHandle"/>
/// so that callers can perform ad-hoc assertions (e.g. frame type checks).
/// </summary>
/// <param name="Name">The resolved method name, or <c>null</c> if unavailable.</param>
/// <param name="MethodDescPtr">The raw MethodDesc pointer for this frame.</param>
/// <param name="FrameName">
/// The runtime Frame name (e.g. "InterpreterFrame", "InlinedCallFrame") when this
/// frame has a non-null frame address, or <c>null</c> for native/managed code frames.
/// </param>
/// <param name="FrameHandle">The underlying stack data frame handle for raw access.</param>
public readonly record struct ResolvedFrame(string? Name, TargetPointer MethodDescPtr, string? FrameName, IStackDataFrameHandle FrameHandle);

/// <summary>
/// Encapsulates a resolved stack walk for a thread, providing a builder-pattern
/// API to assert frame names, ordering, adjacency, and ad-hoc predicates.
/// </summary>
/// <remarks>
/// <para>
/// Inner-to-outer (callee -> caller, default):
/// <code>
/// DumpTestStackWalker.Walk(Target, threadData)
///     .InnerToOuter()
///     .ExpectFrame("MethodC")
///     .ExpectFrame("MethodB")
///     .ExpectFrame("Main")
///     .Verify();
/// </code>
/// </para>
/// <para>
/// Outer-to-inner (caller -> callee):
/// <code>
/// DumpTestStackWalker.Walk(Target, threadData)
///     .OuterToInner()
///     .ExpectFrame("Main")
///     .ExpectFrame("MethodA")
///     .ExpectFrame("MethodC")
///     .Verify();
/// </code>
/// </para>
/// <para>
/// Strict adjacency (no gaps allowed between the two frames):
/// <code>
///     .ExpectFrame("CrashInVarargPInvoke")
///     .ExpectAdjacentFrame("Main")
/// </code>
/// </para>
/// <para>
/// Ad-hoc predicate on matched frame:
/// <code>
///     .ExpectFrame("IL_STUB_PInvoke", frame =>
///     {
///         var md = rts.GetMethodDescHandle(frame.MethodDescPtr);
///         Assert.True(rts.IsILStub(md));
///     })
/// </code>
/// </para>
/// </remarks>
public sealed class DumpTestStackWalker
{
    private readonly ContractDescriptorTarget _target;
    private readonly List<ResolvedFrame> _frames;
    private readonly List<Expectation> _expectations = [];
    private bool _outerToInner;

    private DumpTestStackWalker(ContractDescriptorTarget target, List<ResolvedFrame> frames)
    {
        _target = target;
        _frames = frames;
    }

    /// <summary>The cDAC target the stack was walked from.</summary>
    public ContractDescriptorTarget Target => _target;

    /// <summary>
    /// Returns <c>true</c> if <paramref name="frame"/> is a stack frame that
    /// the legacy DAC stack walker (<c>ClrDataStackWalk</c>) would surface
    /// through the <c>IXCLRDataStackWalk</c> COM interface. Use this to
    /// filter the raw <see cref="IStackWalk"/> output to match what a debugger
    /// would see via the legacy DAC.
    /// </summary>
    public static bool IsLegacyVisible(IStackDataFrameHandle frame)
        => frame.State is StackWalkState.Frameless
                       or StackWalkState.Frame
                       or StackWalkState.SkippedFrame;

    /// <summary>
    /// Enumerates the frames of <paramref name="thread"/>'s stack that the
    /// legacy DAC stack walker would surface — that is, the same frames a
    /// debugger sees via <c>IXCLRDataStackWalk</c>. Convenience wrapper
    /// around <see cref="IStackWalk.CreateStackWalk"/> + <see cref="IsLegacyVisible"/>.
    /// </summary>
    public static IEnumerable<IStackDataFrameHandle> LegacyVisibleFrames(IStackWalk stackWalk, ThreadData thread)
        => stackWalk.CreateStackWalk(thread).Where(IsLegacyVisible);

    /// <summary>
    /// The fully resolved call stack in inner-to-outer order (callee -> caller).
    /// </summary>
    public IReadOnlyList<ResolvedFrame> Frames => _frames;

    /// <summary>
    /// Creates a <see cref="DumpTestStackWalker"/> by walking the stack for
    /// <paramref name="threadData"/> and resolving every frame's method name.
    /// </summary>
    public static DumpTestStackWalker Walk(ContractDescriptorTarget target, ThreadData threadData)
    {
        IStackWalk stackWalk = target.Contracts.StackWalk;
        List<ResolvedFrame> frames = [];

        foreach (IStackDataFrameHandle frame in stackWalk.CreateStackWalk(threadData).Where(IsLegacyVisible))
        {
            TargetPointer methodDescPtr = stackWalk.GetMethodDescPtr(frame);
            string? name = DumpTestHelpers.GetMethodName(target, methodDescPtr);

            string? frameName = null;
            TargetPointer frameAddress = stackWalk.GetFrameAddress(frame);
            if (frameAddress != TargetPointer.Null)
            {
                TargetPointer frameIdentifier = target.ReadPointer(frameAddress);
                frameName = stackWalk.GetFrameName(frameIdentifier);
            }

            frames.Add(new ResolvedFrame(name, methodDescPtr, frameName, frame));
        }

        return new DumpTestStackWalker(target, frames);
    }

    /// <summary>
    /// Sets the expectation direction to inner-to-outer (callee -> caller).
    /// This is the default and matches the natural stack walk order.
    /// </summary>
    public DumpTestStackWalker InnerToOuter()
    {
        _outerToInner = false;
        return this;
    }

    /// <summary>
    /// Sets the expectation direction to outer-to-inner (caller -> callee).
    /// Expectations are matched starting from the outermost frame (e.g. Main)
    /// toward the innermost (e.g. the crash site).
    /// </summary>
    public DumpTestStackWalker OuterToInner()
    {
        _outerToInner = true;
        return this;
    }

    /// <summary>
    /// Prints the resolved call stack to the provided <paramref name="writer"/>,
    /// or to <see cref="Console.WriteLine(string?)"/> if no writer is given.
    /// Useful for debugging test failures.
    /// </summary>
    public DumpTestStackWalker Print(Action<string>? writer = null)
    {
        writer ??= Console.WriteLine;
        writer($"Call stack ({_frames.Count} frames, {(_outerToInner ? "outer->inner" : "inner->outer")}):");

        for (int i = 0; i < _frames.Count; i++)
        {
            int frameIndex = _outerToInner ? _frames.Count - 1 - i : i;
            ResolvedFrame f = _frames[frameIndex];
            string name = f.Name ?? "<null>";
            string md = f.MethodDescPtr != TargetPointer.Null
                ? $"0x{(ulong)f.MethodDescPtr:X}"
                : "null";
            string frameInfo = f.FrameName is not null ? $" [{f.FrameName}]" : "";
            writer($"  [{i}] {name}{frameInfo} (MethodDesc: {md})");
        }

        return this;
    }

    /// <summary>
    /// Expects a frame with <paramref name="methodName"/> after the previous expectation.
    /// Gaps (unmatched frames) between this and the previous expectation are allowed.
    /// </summary>
    public DumpTestStackWalker ExpectFrame(string methodName, Action<ResolvedFrame>? assert = null)
    {
        _expectations.Add(new Expectation(methodName, adjacent: false, assert));
        return this;
    }

    /// <summary>
    /// Expects a frame with <paramref name="methodName"/> immediately after the
    /// previously matched frame (no gaps allowed).
    /// </summary>
    public DumpTestStackWalker ExpectAdjacentFrame(string methodName, Action<ResolvedFrame>? assert = null)
    {
        Assert.True(_expectations.Count > 0,
            "ExpectAdjacentFrame must follow a prior ExpectFrame or ExpectAdjacentFrame.");
        _expectations.Add(new Expectation(methodName, adjacent: true, assert));
        return this;
    }

    /// <summary>
    /// Expects the next frame (at the current search position) to satisfy
    /// <paramref name="predicate"/>, without matching by name.
    /// Gaps before this frame are allowed.
    /// </summary>
    public DumpTestStackWalker ExpectFrameWhere(Func<ResolvedFrame, bool> predicate, string description, Action<ResolvedFrame>? assert = null)
    {
        _expectations.Add(new Expectation(predicate, description, adjacent: false, assert));
        return this;
    }

    /// <summary>
    /// Expects the frame immediately after the previously matched frame to satisfy
    /// <paramref name="predicate"/> (no gaps allowed).
    /// </summary>
    public DumpTestStackWalker ExpectAdjacentFrameWhere(Func<ResolvedFrame, bool> predicate, string description, Action<ResolvedFrame>? assert = null)
    {
        Assert.True(_expectations.Count > 0,
            "ExpectAdjacentFrameWhere must follow a prior expectation.");
        _expectations.Add(new Expectation(predicate, description, adjacent: true, assert));
        return this;
    }

    /// <summary>
    /// Expects a runtime Frame (capital-F) with the given <paramref name="frameName"/>
    /// (e.g. "InterpreterFrame", "InlinedCallFrame") after the previous expectation.
    /// Gaps between this and the previous expectation are allowed.
    /// </summary>
    public DumpTestStackWalker ExpectRuntimeFrame(string frameName, Action<ResolvedFrame>? assert = null)
    {
        _expectations.Add(new Expectation(
            f => string.Equals(f.FrameName, frameName, StringComparison.Ordinal),
            $"RuntimeFrame:{frameName}",
            adjacent: false,
            assert));
        return this;
    }

    /// <summary>
    /// Expects a runtime Frame (capital-F) with the given <paramref name="frameName"/>
    /// immediately after the previously matched frame (no gaps allowed).
    /// </summary>
    public DumpTestStackWalker ExpectAdjacentRuntimeFrame(string frameName, Action<ResolvedFrame>? assert = null)
    {
        Assert.True(_expectations.Count > 0,
            "ExpectAdjacentRuntimeFrame must follow a prior expectation.");
        _expectations.Add(new Expectation(
            f => string.Equals(f.FrameName, frameName, StringComparison.Ordinal),
            $"RuntimeFrame:{frameName}",
            adjacent: true,
            assert));
        return this;
    }

    /// <summary>
    /// Asserts that the call stack contains a runtime Frame (capital-F) with the given
    /// <paramref name="frameName"/>, regardless of position or order.
    /// </summary>
    public DumpTestStackWalker AssertHasRuntimeFrame(string frameName)
    {
        Assert.True(_frames.Any(f => string.Equals(f.FrameName, frameName, StringComparison.Ordinal)),
            $"Expected runtime frame '{frameName}' not found. Call stack: [{FormatCallStack(_frames)}]");
        return this;
    }

    /// <summary>
    /// Asserts that the call stack contains a frame with the given
    /// <paramref name="methodName"/>, regardless of position or order.
    /// </summary>
    public DumpTestStackWalker AssertHasFrame(string methodName)
    {
        Assert.True(_frames.Any(f => string.Equals(f.Name, methodName, StringComparison.Ordinal)),
            $"Expected frame '{methodName}' not found. Call stack: [{FormatCallStack(_frames)}]");
        return this;
    }

    /// <summary>
    /// Verifies all expectations added via <see cref="ExpectFrame"/>,
    /// <see cref="ExpectAdjacentFrame"/>, and <see cref="ExpectFrameWhere"/>.
    /// </summary>
    public void Verify()
    {
        // When outer-to-inner, reverse so expectations match caller -> callee order.
        List<ResolvedFrame> ordered = _outerToInner
            ? new List<ResolvedFrame>(Enumerable.Reverse(_frames))
            : _frames;

        int searchStart = 0;
        int previousMatchIndex = -1;

        foreach (Expectation expectation in _expectations)
        {
            if (expectation.Adjacent)
            {
                int requiredIndex = previousMatchIndex + 1;
                Assert.True(requiredIndex < ordered.Count,
                    $"Expected adjacent frame '{expectation.Description}' but stack ended at index {previousMatchIndex}. " +
                    $"Call stack: [{FormatCallStack(ordered)}]");

                Assert.True(expectation.Matches(ordered[requiredIndex]),
                    $"Expected adjacent frame '{expectation.Description}' at index {requiredIndex}, " +
                    $"but found '{ordered[requiredIndex].Name ?? "<null>"}'. " +
                    $"Call stack: [{FormatCallStack(ordered)}]");

                expectation.RunAssert(ordered[requiredIndex]);
                previousMatchIndex = requiredIndex;
                searchStart = requiredIndex + 1;
            }
            else
            {
                int foundIndex = -1;
                for (int i = searchStart; i < ordered.Count; i++)
                {
                    if (expectation.Matches(ordered[i]))
                    {
                        foundIndex = i;
                        break;
                    }
                }

                Assert.True(foundIndex >= 0,
                    $"Expected frame '{expectation.Description}' not found (searching from index {searchStart}). " +
                    $"Call stack: [{FormatCallStack(ordered)}]");

                expectation.RunAssert(ordered[foundIndex]);
                previousMatchIndex = foundIndex;
                searchStart = foundIndex + 1;
            }
        }
    }

    private static string FormatCallStack(List<ResolvedFrame> frames)
        => string.Join(", ", frames.Select(f =>
        {
            string name = f.Name ?? "<null>";
            return f.FrameName is not null ? $"{name}[{f.FrameName}]" : name;
        }));

    private sealed class Expectation
    {
        private readonly string? _name;
        private readonly Func<ResolvedFrame, bool>? _predicate;
        private readonly Action<ResolvedFrame>? _assert;

        public bool Adjacent { get; }
        public string Description { get; }

        public Expectation(string name, bool adjacent, Action<ResolvedFrame>? assert)
        {
            _name = name;
            _predicate = null;
            _assert = assert;
            Adjacent = adjacent;
            Description = name;
        }

        public Expectation(Func<ResolvedFrame, bool> predicate, string description, bool adjacent, Action<ResolvedFrame>? assert)
        {
            _name = null;
            _predicate = predicate;
            _assert = assert;
            Adjacent = adjacent;
            Description = description;
        }

        public bool Matches(ResolvedFrame frame)
        {
            if (_predicate is not null)
                return _predicate(frame);

            return string.Equals(frame.Name, _name, StringComparison.Ordinal);
        }

        public void RunAssert(ResolvedFrame frame)
        {
            _assert?.Invoke(frame);
        }
    }
}
