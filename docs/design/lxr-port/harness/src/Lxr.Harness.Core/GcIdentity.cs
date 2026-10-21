// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime;

namespace Lxr.Harness.Core;

/// <summary>
/// How strongly a configuration value was actually confirmed. The harness records this per knob and
/// never claims more than it can show: "setting an environment variable is not evidence that the
/// setting took".
/// </summary>
public enum ConfigEvidence
{
    /// <summary>We only know what we asked for. Listed in <c>unverifiedKnobs</c>.</summary>
    RequestedOnly,

    /// <summary>Confirmed indirectly, by a probe whose outcome differs by configuration.</summary>
    Behavioural,

    /// <summary>The host handed the property to the runtime and we read it back from AppContext.</summary>
    HostReported,

    /// <summary>The runtime itself stated its effective value.</summary>
    RuntimeReported,
}

public sealed class KnobObservation
{
    public required string Name { get; init; }

    public string? Requested { get; init; }

    public string? Observed { get; init; }

    public required ConfigEvidence Evidence { get; init; }

    /// <summary>Why the evidence is no stronger than it is, when that needs explaining.</summary>
    public string? Note { get; init; }

    /// <summary>
    /// False only when a requested value is contradicted by an observed one. A knob we could not
    /// observe is not "honoured" - it is unverified, which <see cref="Evidence"/> records.
    /// </summary>
    public bool Contradicted =>
        Requested is not null &&
        Observed is not null &&
        !string.Equals(Normalize(Requested), Normalize(Observed), StringComparison.OrdinalIgnoreCase);

    internal static string Normalize(string value) =>
        value switch
        {
            "1" => "true",
            "0" => "false",
            _ => value.Trim(),
        };
}

/// <summary>
/// Reads the collector's own account of its effective configuration.
///
/// <para>The dictionary returned by <see cref="GC.GetConfigurationVariables"/> is keyed by the GC's
/// internal C++ identifier - <c>ServerGC</c>, <c>HeapCount</c>, <c>GCHeapHardLimit</c> - and not by
/// the public <c>System.GC.*</c> name, because <c>gcconfig.cpp</c> passes <c>#name</c> as the key
/// (src/coreclr/gc/gcconfig.cpp lines 49-61). Values are the <c>s_Updated*</c> fields, which are the
/// effective values rather than the requested ones.</para>
/// </summary>
public static class GcIdentity
{
    public const string ServerGcKey = "ServerGC";
    public const string ConcurrentGcKey = "ConcurrentGC";
    public const string HeapCountKey = "HeapCount";
    public const string HeapHardLimitKey = "GCHeapHardLimit";
    public const string DynamicAdaptationModeKey = "GCDynamicAdaptationMode";
    public const string GcNameKey = "GCName";
    public const string GcPathKey = "GCPath";

    public static Dictionary<string, string> ReadConfiguration()
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (KeyValuePair<string, object> entry in GC.GetConfigurationVariables())
        {
            result[entry.Key] = entry.Value switch
            {
                bool flag => flag ? "true" : "false",
                long number => number.ToString(CultureInfo.InvariantCulture),
                null => string.Empty,
                _ => entry.Value.ToString() ?? string.Empty,
            };
        }

        return result;
    }

    /// <summary>
    /// The collector flavour actually in use. <c>gcenv.ee.cpp</c> line 1249 answers the
    /// <c>gcServer</c> config from <c>g_heap_type == GC_HEAP_SVR</c>, so this reflects the heap type
    /// the runtime really built, not the value that was requested.
    /// </summary>
    public static bool ObservedServerGc(IReadOnlyDictionary<string, string> configuration) =>
        configuration.TryGetValue(ServerGcKey, out string? value) && string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Whether <c>GCDynamicAdaptationMode</c> can be believed at all.
    ///
    /// <para>The effective value is written back by <c>SetGCDynamicAdaptationMode</c>
    /// (src/coreclr/gc/interface.cpp line 752), but that call sits inside <c>#ifdef
    /// DYNAMIC_HEAP_COUNT</c>, which <c>gcpriv.h</c> lines 158-162 define only when both
    /// <c>USE_REGIONS</c> and <c>MULTIPLE_HEAPS</c> are defined. In a Workstation GC there is no
    /// write-back, so the reported number is simply the value that was requested and proves
    /// nothing.</para>
    /// </summary>
    public static bool DatasReadbackIsMeaningful(IReadOnlyDictionary<string, string> configuration) =>
        ObservedServerGc(configuration);

    /// <summary>
    /// Cross-checks the GC's self-report against a second, independent source. Disagreement means
    /// one of the two is lying and the run must not be trusted either way.
    /// </summary>
    public static bool AgreesWithGcSettings(IReadOnlyDictionary<string, string> configuration) =>
        ObservedServerGc(configuration) == GCSettings.IsServerGC;
}
