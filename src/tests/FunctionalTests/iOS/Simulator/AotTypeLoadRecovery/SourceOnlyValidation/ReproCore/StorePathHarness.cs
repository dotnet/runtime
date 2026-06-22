using System;
using System.Runtime.CompilerServices;
using ReproContracts;

namespace ReproCore;

public static class StorePathHarness
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Arm()
    {
        ContractProbe.Touch();
        new TargetNode().Queue();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void RootWithoutRunning()
    {
        if (Environment.TickCount == int.MinValue)
            Arm();
    }
}

public static class ContractProbe
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Touch()
    {
        var reference = ContractBridge.FromPointer<MissingReference>(0);
        _ = $"{reference}".Length;
    }
}

public static class StepExtensions
{
    public static BoundStep<TValue, TCurve, TTarget> Attach<TValue, TCurve, TTarget>(
        TTarget target,
        BoundStep<TValue, TCurve, TTarget> step,
        TValue value,
        double duration,
        TCurve curve)
        where TCurve : struct
        where TTarget : class
    {
        if (step.Target != null)
            throw new InvalidOperationException("A step can only be attached once.");

        step.Target = target;
        step.Value = value;
        step.Curve = curve;
        step.EndTime = duration;
        return step;
    }

    public static void Schedule<TCurve>(this TargetNode target, Payload value, double duration, TCurve curve)
        where TCurve : struct
        => Attach(target, new ConcreteStep<TCurve>(), value, duration, curve);
}

public abstract class Step<TValue>
{
    public TValue Value { get; internal set; } = default!;
}

public abstract class BoundStep<TValue, TCurve, TTarget> : Step<TValue>
    where TCurve : struct
    where TTarget : class
{
    public TCurve Curve { get; internal set; }

    public double EndTime { get; internal set; }

    public TTarget Target { get; internal set; } = null!;
}

public sealed class TargetNode
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public void Queue()
    {
        this.Schedule(default, 100, default(Curve));
    }
}

public sealed class ConcreteStep<TCurve> : BoundStep<Payload, TCurve, TargetNode>
    where TCurve : struct
{
}

public readonly struct Payload
{
}

public readonly struct Curve
{
}
