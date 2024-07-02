// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Diagnostics;

internal static class DiagnosticSourceEventSourceSamplerBuilder
{
    internal delegate ActivitySamplingResult SampleActivityFunc(
        bool hasActivityContext,
        ref ActivityCreationOptions<ActivityContext> options);

    public static SampleActivityFunc CreateParentRatioSampler(double ratio)
    {
        long idUpperBound = ratio <= 0.0
            ? long.MinValue
            : ratio >= 1.0
                ? long.MaxValue
                : (long)(ratio * long.MaxValue);

        return (bool hasActivityContext, ref ActivityCreationOptions<ActivityContext> options) =>
        {
            if (hasActivityContext && options.TraceId != default)
            {
                ActivityContext parentContext = options.Parent;

                ActivitySamplingResult samplingDecision = ParentRatioSampler(idUpperBound, in parentContext, options.TraceId);

                return samplingDecision == ActivitySamplingResult.None
                    && (parentContext == default || parentContext.IsRemote)
                    ? ActivitySamplingResult.PropagationData // If it is the root span or the parent is remote select PropagationData so the trace ID is preserved
                                                             // even if no activity of the trace is recorded
                    : samplingDecision;
            }

            return ActivitySamplingResult.None;
        };
    }

    public static ActivitySamplingResult ParentRatioSampler(long idUpperBound, in ActivityContext parentContext, ActivityTraceId traceId)
    {
        if (parentContext.TraceId != default)
        {
            return parentContext.TraceFlags.HasFlag(ActivityTraceFlags.Recorded)
                ? ActivitySamplingResult.AllDataAndRecorded
                : ActivitySamplingResult.None;
        }

        Span<byte> traceIdBytes = stackalloc byte[16];
        traceId.CopyTo(traceIdBytes);

        return Math.Abs(GetLowerLong(traceIdBytes)) < idUpperBound
            ? ActivitySamplingResult.AllDataAndRecorded
            : ActivitySamplingResult.None;

        static long GetLowerLong(ReadOnlySpan<byte> bytes)
        {
            long result = 0;
            for (int i = 0; i < 8; i++)
            {
                result <<= 8;
#pragma warning disable CS0675 // Bitwise-or operator used on a sign-extended operand
                result |= bytes[i] & 0xff;
#pragma warning restore CS0675 // Bitwise-or operator used on a sign-extended operand
            }

            return result;
        }
    }
}
