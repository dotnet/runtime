// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using ReproContracts;

namespace ReproCore;

public static class StorePathHarness
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Run()
    {
        // Keep the swapped-contract typeload reachable while the generic Target setter
        // still drives the store-path field import we want AOT to compile.
        StorePathContractProbe.Touch();
        new StorePathTargetNode().Queue();
    }
}

public static class StorePathContractProbe
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Touch()
    {
        var reference = ContractBridge.FromPointer<MissingReference>(0);
        _ = $"{reference}".Length;
    }
}

public static class StorePathStepExtensions
{
    public static StorePathBoundStep<TValue, TCurve, TTarget> Attach<TValue, TCurve, TTarget>(
        TTarget target,
        StorePathBoundStep<TValue, TCurve, TTarget> step,
        TValue value,
        double duration,
        TCurve curve)
        where TCurve : struct
        where TTarget : class
    {
        if (step.Target is not null)
            throw new InvalidOperationException("A step can only be attached once.");

        step.Target = target;
        step.Value = value;
        step.Curve = curve;
        step.EndTime = duration;
        return step;
    }

    public static void Schedule<TCurve>(this StorePathTargetNode target, StorePathPayload value, double duration, TCurve curve)
        where TCurve : struct
        => Attach(target, new StorePathConcreteStep<TCurve>(), value, duration, curve);
}

public abstract class StorePathStep<TValue>
{
    public TValue Value { get; internal set; } = default!;
}

public abstract class StorePathBoundStep<TValue, TCurve, TTarget> : StorePathStep<TValue>
    where TCurve : struct
    where TTarget : class
{
    public TCurve Curve { get; internal set; }

    public double EndTime { get; internal set; }

    public TTarget Target { get; internal set; } = null!;
}

public sealed class StorePathTargetNode
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public void Queue()
    {
        this.Schedule(default, 100, default(StorePathCurve));
    }
}

public sealed class StorePathConcreteStep<TCurve> : StorePathBoundStep<StorePathPayload, TCurve, StorePathTargetNode>
    where TCurve : struct
{
}

public readonly struct StorePathPayload
{
}

public readonly struct StorePathCurve
{
}
