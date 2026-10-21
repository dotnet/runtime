// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Threading;

namespace Lxr.Harness.Core;

/// <summary>
/// Secondary characterization data: garbage collection counts, pause durations, and - separately -
/// how many collections were <em>induced</em>.
///
/// <para>Pause figures are deliberately secondary. P0.2 established from the paper's only comparative
/// pause table (Table 1, arXiv:2210.17175, PDF page 2) that LXR's pauses are longer than G1's at every
/// percentile while its application latency is far better, under the caption "Short GC pauses do not
/// assure low latency". Pause data is therefore recorded to characterize behaviour and must never be
/// used as an acceptance threshold.</para>
///
/// <para>Induced collections are tracked separately because of a concrete confound found in P0.3. The
/// hsqldb p99 discrepancy between the two reference oracles was not an LXR mechanism difference at
/// all: hsqldb's memory watcher calls <c>System.gc()</c>, one oracle services it with a full
/// stop-the-world collection and the other has that path commented out. A .NET harness that let
/// <c>GC.Collect()</c> happen invisibly would reproduce exactly that confound, so induced collections
/// are counted and asserted to be zero for every scenario that does not declare otherwise.</para>
/// </summary>
public sealed class GcTelemetry : EventListener
{
    // src/coreclr/gc/gc.h lines 62-80: reason_induced = 1, reason_induced_noforce = 7,
    // reason_induced_compacting = 10, reason_induced_aggressive = 17.
    private static readonly int[] InducedReasons = [1, 7, 10, 17];

    private const string RuntimeProviderName = "Microsoft-Windows-DotNETRuntime";
    private const EventKeywords GcKeyword = (EventKeywords)0x1;

    private readonly List<double> _pauseMillis = [];
    private readonly object _lock = new();

    private EventSource? _runtimeSource;
    private DateTime _suspendStart;
    private bool _suspended;
    private int _inducedCollections;
    private int _observedPauses;
    private bool _listenerEnabled;

    // Baselines for the always-correct primary path.
    private TimeSpan _pauseBaseline;
    private int[] _collectionBaseline = new int[3];

    protected override void OnEventSourceCreated(EventSource eventSource)
    {
        if (eventSource.Name != RuntimeProviderName)
        {
            return;
        }

        _runtimeSource = eventSource;
        try
        {
            EnableEvents(eventSource, EventLevel.Informational, GcKeyword);
            _listenerEnabled = true;
        }
        catch (ArgumentException)
        {
            _listenerEnabled = false;
        }
    }

    protected override void OnEventWritten(EventWrittenEventArgs eventData)
    {
        string? name = eventData.EventName;
        if (name is null)
        {
            return;
        }

        if (name.StartsWith("GCSuspendEEBegin", StringComparison.Ordinal))
        {
            lock (_lock)
            {
                _suspendStart = eventData.TimeStamp;
                _suspended = true;
            }
        }
        else if (name.StartsWith("GCRestartEEEnd", StringComparison.Ordinal))
        {
            lock (_lock)
            {
                if (_suspended)
                {
                    double ms = (eventData.TimeStamp - _suspendStart).TotalMilliseconds;
                    _suspended = false;
                    if (ms >= 0)
                    {
                        _pauseMillis.Add(ms);
                        _observedPauses++;
                    }
                }
            }
        }
        else if (name.StartsWith("GCStart", StringComparison.Ordinal))
        {
            int reason = ReadPayloadInt32(eventData, "Reason", -1);
            if (Array.IndexOf(InducedReasons, reason) >= 0)
            {
                Interlocked.Increment(ref _inducedCollections);
            }
        }
    }

    private static int ReadPayloadInt32(EventWrittenEventArgs eventData, string payloadName, int fallback)
    {
        if (eventData.PayloadNames is null || eventData.Payload is null)
        {
            return fallback;
        }

        for (int i = 0; i < eventData.PayloadNames.Count && i < eventData.Payload.Count; i++)
        {
            if (!string.Equals(eventData.PayloadNames[i], payloadName, StringComparison.Ordinal))
            {
                continue;
            }

            return eventData.Payload[i] switch
            {
                uint value => (int)value,
                int value => value,
                ushort value => value,
                _ => fallback,
            };
        }

        return fallback;
    }

    /// <summary>Marks the start of the measured region; everything before it is discarded.</summary>
    public void BeginMeasurement()
    {
        lock (_lock)
        {
            _pauseMillis.Clear();
            _observedPauses = 0;
            _suspended = false;
        }

        Interlocked.Exchange(ref _inducedCollections, 0);
        _pauseBaseline = GC.GetTotalPauseDuration();
        _collectionBaseline = [GC.CollectionCount(0), GC.CollectionCount(1), GC.CollectionCount(2)];
    }

    public GcSummary EndMeasurement()
    {
        // The primary path cannot fail: GC.GetTotalPauseDuration (System.Private.CoreLib
        // GC.CoreCLR.cs line 897) and GC.CollectionCount are direct runtime queries rather than an
        // event stream that can drop buffers.
        TimeSpan totalPause = GC.GetTotalPauseDuration() - _pauseBaseline;
        int[] counts =
        [
            GC.CollectionCount(0) - _collectionBaseline[0],
            GC.CollectionCount(1) - _collectionBaseline[1],
            GC.CollectionCount(2) - _collectionBaseline[2],
        ];

        double[] pauses;
        lock (_lock)
        {
            pauses = [.. _pauseMillis];
        }

        // The event stream is trusted for the pause distribution only when it produced about as many
        // pauses as there were collections. Materially fewer means buffers were dropped or the
        // listener attached late, and a percentile computed from a lossy sample would be wrong in an
        // unknowable direction.
        //
        // Two exact allowances, both measured rather than assumed:
        //
        // * A measured region containing no collections at all is a complete observation of nothing,
        //   not an incomplete one. Conflating the two would label the low-allocation scenario - whose
        //   whole point is that it collects rarely - as untrustworthy.
        // * At most one collection can straddle each boundary: one whose GCRestartEEEnd arrives after
        //   the counters are read, and one whose GCSuspendEEBegin preceded the clear. Observed
        //   directly - allocation-churn reported 139 gen0 collections against 138 pauses, and
        //   cyclic-garbage 357 against 355. A shortfall inside that bound is arithmetic, not loss, and
        //   is labelled distinctly so a reader can still see it happened.
        //
        // More pauses than collections is expected and not a defect: a background gen2 collection
        // suspends the EE twice, which is why large-object-pressure reported 4726 pauses against 3195
        // collections of which 1584 were gen2.
        const int BoundaryAllowance = 2;
        int totalCollections = counts[0] + counts[1] + counts[2];
        bool nothingToObserve = totalCollections == 0 && pauses.Length == 0;
        int shortfall = counts[0] - pauses.Length;
        bool distributionTrusted = _listenerEnabled && (nothingToObserve || (pauses.Length > 0 && shortfall <= BoundaryAllowance));

        return new GcSummary
        {
            Gen0Collections = counts[0],
            Gen1Collections = counts[1],
            Gen2Collections = counts[2],
            InducedCollections = Volatile.Read(ref _inducedCollections),
            TotalPauseMs = totalPause.TotalMilliseconds,
            ObservedPauseCount = pauses.Length,
            PauseShortfall = shortfall,
            PauseSamplesMs = distributionTrusted ? pauses : [],
            PauseSource = nothingToObserve
                ? "no-collections-in-measured-region"
                : distributionTrusted
                    ? shortfall > 0
                        ? "eventlistener-suspend-restart (boundary-truncated)"
                        : "eventlistener-suspend-restart"
                    : _listenerEnabled
                        ? "total-pause-duration-only (event stream incomplete)"
                        : "total-pause-duration-only (listener unavailable)",
        };
    }

    public override void Dispose()
    {
        if (_runtimeSource is not null && _listenerEnabled)
        {
            DisableEvents(_runtimeSource);
        }

        base.Dispose();
    }
}

public sealed class GcSummary
{
    /// <remarks>
    /// <c>GC.CollectionCount(0)</c> counts every collection of generation 0 <em>or higher</em>, so this
    /// is the cumulative total across all generations, not the gen0-only figure its name suggests.
    /// <see cref="Gen1Collections"/> and <see cref="Gen2Collections"/> are subsets of it. Summing the
    /// three therefore counts gen1 twice and gen2 three times.
    /// </remarks>
    public required int Gen0Collections { get; init; }

    public required int Gen1Collections { get; init; }

    public required int Gen2Collections { get; init; }

    /// <summary>Collections triggered by an explicit request rather than by allocation pressure.</summary>
    public required int InducedCollections { get; init; }

    public required double TotalPauseMs { get; init; }

    public required int ObservedPauseCount { get; init; }

    /// <summary>
    /// Collections counted minus pauses observed. Zero or negative is normal; a small positive value
    /// is the measurement-boundary effect; a large one means the event stream lost data.
    /// </summary>
    public required int PauseShortfall { get; init; }

    /// <summary>Empty when the event stream was not complete enough to trust for percentiles.</summary>
    public required IReadOnlyList<double> PauseSamplesMs { get; init; }

    /// <summary>Which mechanism produced the pause figures, so a reader knows what they are.</summary>
    public required string PauseSource { get; init; }

    /// <summary>Mean suspension, or null when nothing was observed to average.</summary>
    /// <remarks>
    /// <para><see cref="TotalPauseMs"/> is a <c>GC.GetTotalPauseDuration</c> delta: the total time the
    /// EE spent suspended. The matching denominator is therefore the number of <em>suspensions</em>,
    /// which <see cref="ObservedPauseCount"/> gives directly whenever the event stream was usable. It
    /// is the better denominator even than a corrected collection count, because a background gen2
    /// collection suspends the EE twice - the case already documented where large-object-pressure
    /// reported 4726 pauses against 3195 collections.</para>
    /// <para>Falling back to <see cref="Gen0Collections"/> uses the cumulative all-generation total.
    /// Adding the three generation counts would inflate the denominator and deflate the mean by a
    /// factor that varies with the gen1/gen2 mix, so it would not even cancel between two arms being
    /// compared.</para>
    /// </remarks>
    public double? MeanPauseMs =>
        (ObservedPauseCount, Gen0Collections) switch
        {
            ( > 0, _) => TotalPauseMs / ObservedPauseCount,
            (_, > 0) => TotalPauseMs / Gen0Collections,
            _ => null,
        };
}
