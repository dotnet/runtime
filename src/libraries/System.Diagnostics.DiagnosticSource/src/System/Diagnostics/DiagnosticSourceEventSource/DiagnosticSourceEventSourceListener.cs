// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Runtime.CompilerServices;

namespace System.Diagnostics;

internal sealed class DiagnosticSourceEventSourceListener : IDisposable
{
    private const string c_ActivitySourcePrefix = "[AS]";
    private const string c_ParentRatioSamplerPrefix = "ParentRatioSampler(";

    private DiagnosticSourceEventSourceFilterAndTransform? _specList;
    private DiagnosticSourceEventSourceActivitySourceListener? _activitySourceListener;

    /// <summary>
    /// Parses filterAndPayloadSpecs which is a list of lines each of which has the from
    ///
    ///    DiagnosticSourceName/EventName:PAYLOAD_SPEC
    ///
    /// where PAYLOADSPEC is a semicolon separated list of specifications of the form
    ///
    ///    OutputName=Prop1.Prop2.PropN
    ///
    /// Into linked list of FilterAndTransform that together forward events from the given
    /// DiagnosticSource's to 'eventSource'. Sets the 'specList' variable to this value
    /// (destroying anything that was there previously).
    ///
    /// By default any serializable properties of the payload object are also included
    /// in the output payload, however this feature and be tuned off by prefixing the
    /// PAYLOADSPEC with a '-'.
    /// </summary>
    public static DiagnosticSourceEventSourceListener Create(string? filterAndPayloadSpecs)
    {
        filterAndPayloadSpecs ??= "";

        DiagnosticSourceEventSourceFilterAndTransform? specList = null;
        DiagnosticSourceEventSourceFilterAndTransform? activitySourceSpecList = null;

        // Points just beyond the last point in the string that has yet to be parsed. Thus we start with the whole string.
        int endIdx = filterAndPayloadSpecs.Length;
        while (true)
        {
            // Skip trailing whitespace.
            while (0 < endIdx && char.IsWhiteSpace(filterAndPayloadSpecs[endIdx - 1]))
                --endIdx;

            int newlineIdx = filterAndPayloadSpecs.LastIndexOf('\n', endIdx - 1, endIdx);
            int startIdx = 0;
            if (0 <= newlineIdx)
                startIdx = newlineIdx + 1;  // starts after the newline, or zero if we don't find one.

            // Skip leading whitespace
            while (startIdx < endIdx && char.IsWhiteSpace(filterAndPayloadSpecs[startIdx]))
                startIdx++;

            if (IsActivitySourceEntry(filterAndPayloadSpecs, startIdx, endIdx))
            {
                activitySourceSpecList = CreateNewActivitySourceTransform(filterAndPayloadSpecs, startIdx, endIdx, activitySourceSpecList);
            }
            else
            {
                specList = new DiagnosticSourceEventSourceFilterAndTransform(filterAndPayloadSpecs, startIdx, endIdx, specList);
            }

            endIdx = newlineIdx;
            if (endIdx < 0)
                break;
        }

        return new DiagnosticSourceEventSourceListener
        {
            _specList = specList,
            _activitySourceListener = activitySourceSpecList == null ? null : DiagnosticSourceEventSourceActivitySourceListener.Create(activitySourceSpecList),
        };
    }

    private DiagnosticSourceEventSourceListener()
    {
    }

    /// <summary>
    /// This destroys (turns off) the FilterAndTransform stopping the forwarding started with CreateFilterAndTransformList
    /// </summary>
    public void Dispose()
    {
        _activitySourceListener?.Dispose();
        _activitySourceListener = null;

        var curSpec = _specList;
        _specList = null;            // Null out the list
        while (curSpec != null)     // Dispose everything in the list.
        {
            curSpec.Dispose();
            curSpec = curSpec.Next;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsActivitySourceEntry(string filterAndPayloadSpec, int startIdx, int endIdx) =>
        filterAndPayloadSpec.AsSpan(startIdx, endIdx - startIdx).StartsWith(c_ActivitySourcePrefix.AsSpan(), StringComparison.Ordinal);

    private static DiagnosticSourceEventSourceFilterAndTransform? CreateNewActivitySourceTransform(string filterAndPayloadSpec, int startIdx, int endIdx, DiagnosticSourceEventSourceFilterAndTransform? next)
    {
        Debug.Assert(endIdx - startIdx >= 4);
        Debug.Assert(IsActivitySourceEntry(filterAndPayloadSpec, startIdx, endIdx));

        ReadOnlySpan<char> eventName;
        ReadOnlySpan<char> activitySourceName;

        DiagnosticSourceEventSourceFilterAndTransform.ActivityEvents supportedEvent = DiagnosticSourceEventSourceFilterAndTransform.ActivityEvents.All; // Default events
        DiagnosticSourceEventSourceSamplerBuilder.SampleActivityFunc sampleFunc = static (bool hasActivityContext, ref ActivityCreationOptions<ActivityContext> options)
            => ActivitySamplingResult.AllDataAndRecorded; // Default sampler

        int colonIdx = filterAndPayloadSpec.IndexOf(':', startIdx + c_ActivitySourcePrefix.Length, endIdx - startIdx - c_ActivitySourcePrefix.Length);

        ReadOnlySpan<char> entry = filterAndPayloadSpec.AsSpan(
            startIdx + c_ActivitySourcePrefix.Length,
            (colonIdx >= 0 ? colonIdx : endIdx) - startIdx - c_ActivitySourcePrefix.Length)
            .Trim();

        int eventNameIndex = entry.IndexOf('/');
        if (eventNameIndex >= 0)
        {
            activitySourceName = entry.Slice(0, eventNameIndex).Trim();

            ReadOnlySpan<char> suffixPart = entry.Slice(eventNameIndex + 1).Trim();
            int samplingResultIndex = suffixPart.IndexOf('-');
            if (samplingResultIndex >= 0)
            {
                // We have the format "[AS]SourceName/[EventName]-[SamplingResult]
                eventName = suffixPart.Slice(0, samplingResultIndex).Trim();
                suffixPart = suffixPart.Slice(samplingResultIndex + 1).Trim();

                if (suffixPart.Length > 0)
                {
                    if (suffixPart.Equals("Propagate".AsSpan(), StringComparison.OrdinalIgnoreCase))
                    {
                        sampleFunc = static (bool hasActivityContext, ref ActivityCreationOptions<ActivityContext> options) => ActivitySamplingResult.PropagationData;
                    }
                    else if (suffixPart.Equals("Record".AsSpan(), StringComparison.OrdinalIgnoreCase))
                    {
                        sampleFunc = static (bool hasActivityContext, ref ActivityCreationOptions<ActivityContext> options) => ActivitySamplingResult.AllData;
                    }
                    else if (suffixPart.StartsWith(c_ParentRatioSamplerPrefix.AsSpan(), StringComparison.OrdinalIgnoreCase))
                    {
                        int endingLocation = suffixPart.IndexOf(')');
                        if (endingLocation < 0
#if NETFRAMEWORK || NETSTANDARD
                            || !double.TryParse(suffixPart.Slice(c_ParentRatioSamplerPrefix.Length, endingLocation - c_ParentRatioSamplerPrefix.Length).ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out double ratio))
#else
                            || !double.TryParse(suffixPart.Slice(c_ParentRatioSamplerPrefix.Length, endingLocation - c_ParentRatioSamplerPrefix.Length), NumberStyles.Float, CultureInfo.InvariantCulture, out double ratio))
#endif
                        {
                            // Invalid format
                            return next;
                        }

                        sampleFunc = DiagnosticSourceEventSourceSamplerBuilder.CreateParentRatioSampler(ratio);
                    }
                    else
                    {
                        // Invalid format
                        return next;
                    }
                }
            }
            else
            {
                // We have the format "[AS]SourceName/[EventName]
                eventName = suffixPart;
            }

            if (eventName.Length > 0)
            {
                if (eventName.Equals("Start".AsSpan(), StringComparison.OrdinalIgnoreCase))
                {
                    supportedEvent = DiagnosticSourceEventSourceFilterAndTransform.ActivityEvents.ActivityStart;
                }
                else if (eventName.Equals("Stop".AsSpan(), StringComparison.OrdinalIgnoreCase))
                {
                    supportedEvent = DiagnosticSourceEventSourceFilterAndTransform.ActivityEvents.ActivityStop;
                }
                else
                {
                    // Invalid format
                    return next;
                }
            }
        }
        else
        {
            // We have the format "[AS]SourceName"
            activitySourceName = entry;
        }

        string? activityName = null;
        int plusSignIndex = activitySourceName.IndexOf('+');
        if (plusSignIndex >= 0)
        {
            activityName = activitySourceName.Slice(plusSignIndex + 1).Trim().ToString();
            activitySourceName = activitySourceName.Slice(0, plusSignIndex).Trim();
        }

        return new DiagnosticSourceEventSourceFilterAndTransform(filterAndPayloadSpec, endIdx, colonIdx, activitySourceName.ToString(), activityName, supportedEvent, sampleFunc, next);
    }
}
