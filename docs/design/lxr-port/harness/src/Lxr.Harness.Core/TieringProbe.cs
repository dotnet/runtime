// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Lxr.Harness.Core;

/// <summary>
/// Reads back the JIT tiering, PGO and ReadyToRun configuration.
///
/// <para><strong>A negative result that shapes this whole type.</strong> The runtime can state its own
/// effective tiering configuration through the <c>TieredCompilationSettings</c> ETW event
/// (src/coreclr/vm/ClrEtwAll.man line 4247, flag map at lines 701-706:
/// <c>0x1</c> QuickJit, <c>0x2</c> QuickJitForLoops, <c>0x4</c> TieredPGO, <c>0x8</c> ReadyToRun).
/// That event is unreachable from inside the process under test. It is fired from
/// <c>EEConfig::sync</c> (src/coreclr/vm/eeconfig.cpp lines 801-804) guarded by
/// <c>ETW::CompilationLog::TieredCompilation::Runtime::IsEnabled()</c>, and <c>EEConfig::sync</c> runs
/// during EE startup, long before any managed <c>EventListener</c> exists. With no listener attached
/// the guard is false and the event is never emitted at all - so a harness that waited for it would
/// wait forever. The rundown variant (eventtrace.cpp lines 1934-1936) needs an external ETW session
/// to request a rundown, which would not work on the <c>corerun</c> host and would be an external
/// tracing dependency.</para>
///
/// <para>The harness therefore does not claim <see cref="ConfigEvidence.RuntimeReported"/> for these
/// knobs. It reads the host property the runtime actually consumes, which is
/// <see cref="ConfigEvidence.HostReported"/>, and says so.</para>
///
/// <para><strong>A second negative result, and a probe that was deleted because of it.</strong> An
/// earlier revision carried a behavioural probe that called a method several hundred times across
/// timed rounds and watched <c>RuntimeMethodHandle.GetFunctionPointer()</c> for the tier-0 to tier-1
/// transition, intending to upgrade tiering specifically to <see cref="ConfigEvidence.Behavioural"/>.
/// It was run in both directions - default tiering, and <c>DOTNET_TieredCompilation=0</c> - and
/// returned <c>Inconclusive</c> in both, because the function pointer is a stable precode address
/// that is backpatched underneath rather than replaced. 400 calls per round over 4 rounds is well
/// past the call-counting threshold and the tiered background delay, so the method was certainly
/// promoted; the probe simply could not see it. A probe that cannot distinguish the two
/// configurations it exists to distinguish is indistinguishable from no probe, and reads as evidence
/// while being none, so it was removed rather than left in reporting <c>Inconclusive</c> forever.</para>
///
/// <para>What remains is honest and small: tiering, QuickJit and PGO are <c>host-reported</c>;
/// ReadyToRun is <c>requested-only</c>. These are the knobs listed in the result's
/// <c>unverifiedKnobs</c> array.</para>
/// </summary>
public static class TieringProbe
{
    public const string TieredCompilationProperty = "System.Runtime.TieredCompilation";
    public const string QuickJitProperty = "System.Runtime.TieredCompilation.QuickJit";
    public const string QuickJitForLoopsProperty = "System.Runtime.TieredCompilation.QuickJitForLoops";
    public const string TieredPgoProperty = "System.Runtime.TieredPGO";

    /// <summary>
    /// The host property the runtime read, or null when the host never set it. The runtime consumes
    /// exactly these names: <c>eeconfig.cpp</c> lines 664, 688, 694 and 759 call
    /// <c>Configuration::GetKnobBooleanValue(W("System.Runtime.TieredCompilation"), ...)</c> and
    /// friends, so a property present here is one the runtime saw.
    /// </summary>
    public static string? ReadHostProperty(string name) =>
        AppContext.GetData(name) switch
        {
            null => null,
            bool flag => flag ? "true" : "false",
            object value => value.ToString(),
        };

    /// <summary>
    /// ReadyToRun has no <c>System.Runtime.*</c> knob at all - <c>eeconfig.cpp</c> line 482 reads it
    /// only through <c>CLRConfig::EXTERNAL_ReadyToRun</c>, which is the environment channel. Its one
    /// runtime-reported readback is the <c>0x8</c> ETW flag proven unreachable above, so the harness
    /// records the requested environment value and marks the evidence honestly rather than implying
    /// it was confirmed.
    /// </summary>
    public static string? ReadReadyToRunEnvironment() =>
        Environment.GetEnvironmentVariable("DOTNET_ReadyToRun");

    /// <summary>
    /// Produces the observation set for every tiering-related knob, given what was requested.
    /// </summary>
    public static List<KnobObservation> Observe(IReadOnlyDictionary<string, string> requested)
    {
        var observations = new List<KnobObservation>();

        foreach (string property in new[] { TieredCompilationProperty, QuickJitProperty, QuickJitForLoopsProperty, TieredPgoProperty })
        {
            string? observed = ReadHostProperty(property);
            requested.TryGetValue(property, out string? request);

            observations.Add(new KnobObservation
            {
                Name = property,
                Requested = request,
                Observed = observed,
                Evidence = observed is null ? ConfigEvidence.RequestedOnly : ConfigEvidence.HostReported,
                Note = observed is null
                    ? "Host never set this property; the runtime used its default. The TieredCompilationSettings ETW event that would report the effective value is emitted during EE startup under an IsEnabled() guard (eeconfig.cpp:801-804) and is unreachable from an in-process listener."
                    : "Read back from AppContext; this is the property the runtime consumed (eeconfig.cpp:664/688/694/759). It is not a statement of the effective value.",
            });
        }

        string? readyToRun = ReadReadyToRunEnvironment();
        observations.Add(new KnobObservation
        {
            Name = "DOTNET_ReadyToRun",
            Requested = requested.GetValueOrDefault("DOTNET_ReadyToRun"),
            Observed = readyToRun,
            Evidence = ConfigEvidence.RequestedOnly,
            Note = "ReadyToRun has no System.Runtime.* knob (eeconfig.cpp:482 reads only CLRConfig::EXTERNAL_ReadyToRun) and its only effective-value readback is the 0x8 flag of an ETW event unreachable in-process. Echoing our own environment variable is not confirmation.",
        });

        return observations;
    }

    internal static string FormatFlags(uint flags)
    {
        var parts = new List<string>();
        if ((flags & 0x1) != 0)
        {
            parts.Add("QuickJit");
        }

        if ((flags & 0x2) != 0)
        {
            parts.Add("QuickJitForLoops");
        }

        if ((flags & 0x4) != 0)
        {
            parts.Add("TieredPGO");
        }

        if ((flags & 0x8) != 0)
        {
            parts.Add("ReadyToRun");
        }

        return parts.Count == 0 ? "None" : string.Join("|", parts);
    }

    internal static string ToInvariant(int value) => value.ToString(CultureInfo.InvariantCulture);
}
