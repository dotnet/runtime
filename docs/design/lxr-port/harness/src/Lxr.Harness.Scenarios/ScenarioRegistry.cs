// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Lxr.Harness.Core;

namespace Lxr.Harness.Scenarios;

/// <summary>
/// Constructs the scenarios that need nothing beyond the base shared framework.
///
/// <para>The ASP.NET request-load scenario is deliberately absent: it lives in the ASP.NET worker
/// because a <c>FrameworkReference</c> to <c>Microsoft.AspNetCore.App</c> propagates to every project
/// that references the assembly carrying it, which would make every scenario require a shared
/// framework this repository does not build and would make the whole harness unable to run on
/// <c>corerun</c>.</para>
/// </summary>
public static class ScenarioRegistry
{
    private static readonly Dictionary<string, Func<IScenario>> Factories = new(StringComparer.Ordinal)
    {
        ["low-allocation-compute"] = static () => new LowAllocationComputeScenario(),
        ["allocation-churn"] = static () => new AllocationChurnScenario(),
        ["long-lived-cache"] = static () => new LongLivedCacheScenario(),
        ["cyclic-garbage"] = static () => new CyclicGarbageScenario(),
        ["pointer-chasing"] = static () => new PointerChasingScenario(),
        ["multi-thread-throughput"] = static () => new MultiThreadThroughputScenario(),
        ["pinning-heavy-io"] = static () => new PinningHeavyIoScenario(),
        ["lifecycle-semantics"] = static () => new LifecycleSemanticsScenario(),
        ["large-object-pressure"] = static () => new LargeObjectPressureScenario(),
    };

    public static IReadOnlyCollection<string> Ids => Factories.Keys;

    public static bool TryCreate(string id, out IScenario scenario)
    {
        if (Factories.TryGetValue(id, out Func<IScenario>? factory))
        {
            scenario = factory();
            return true;
        }

        scenario = null!;
        return false;
    }

    public static IScenario Create(string id) =>
        TryCreate(id, out IScenario scenario)
            ? scenario
            : throw new ArgumentException($"Unknown scenario '{id}'. Known: {string.Join(", ", Factories.Keys)}.", nameof(id));
}
