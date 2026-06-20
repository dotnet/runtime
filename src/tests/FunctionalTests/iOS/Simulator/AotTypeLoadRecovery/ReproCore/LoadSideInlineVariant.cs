// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using ReproContracts;

namespace ReproCore;

public static class LoadSideInlineHarness
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Run()
    {
        Consume(LoadThroughInlineCandidate());
        Consume(LoadThroughInlineInstanceCandidate());
        Consume(LoadInsideExceptionClause());
        Consume(new LoadSideTargetNode().ReadTarget());
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int LoadThroughInlineCandidate()
    {
        return InlineableMissingFieldLoad();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int InlineableMissingFieldLoad()
    {
        return MissingFieldOwner.Counter;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int LoadThroughInlineInstanceCandidate()
    {
        return InlineableMissingInstanceFieldLoad(new MissingFieldOwner());
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int InlineableMissingInstanceFieldLoad(MissingFieldOwner owner)
    {
        return owner.InstanceCounter;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int LoadInsideExceptionClause()
    {
        try
        {
            return new MissingFieldOwner().InstanceCounter;
        }
        finally
        {
            Consume(0);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Consume(int value)
    {
        if (Environment.TickCount == int.MinValue)
            GC.KeepAlive(value);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Consume(object? value)
    {
        if (Environment.TickCount == int.MinValue)
            GC.KeepAlive(value);
    }
}

public static class LoadSideStepExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TTarget ReadTarget<TValue, TCurve, TTarget>(
        LoadSideBoundStep<TValue, TCurve, TTarget> step)
        where TCurve : struct
        where TTarget : class
        => step.Target;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LoadSideTargetNode ReadTarget<TCurve>(this LoadSideTargetNode target)
        where TCurve : struct
        => ReadTarget<LoadSidePayload, TCurve, LoadSideTargetNode>(new LoadSideConcreteStep<TCurve>());
}

public abstract class LoadSideBoundStep<TValue, TCurve, TTarget>
    where TCurve : struct
    where TTarget : class
{
    public TTarget Target
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get;
    } = null!;
}

public sealed class LoadSideTargetNode
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public LoadSideTargetNode ReadTarget()
    {
        return this.ReadTarget<LoadSideCurve>();
    }
}

public sealed class LoadSideConcreteStep<TCurve> : LoadSideBoundStep<LoadSidePayload, TCurve, LoadSideTargetNode>
    where TCurve : struct
{
}

public readonly struct LoadSidePayload
{
}

public readonly struct LoadSideCurve
{
}
