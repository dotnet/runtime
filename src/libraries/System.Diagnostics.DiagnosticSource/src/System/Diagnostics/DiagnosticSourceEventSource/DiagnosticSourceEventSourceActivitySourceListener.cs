// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace System.Diagnostics;

internal sealed class DiagnosticSourceEventSourceActivitySourceListener : IDisposable
{
    private ActivitySourceFilterAndTransformSpecs? _wildcardActivitySourceSpecs;
    private Dictionary<string, ActivitySourceFilterAndTransformSpecs>? _activitySourceSpecsBySourceName;
    private ActivityListener? _activityListener;

    public static DiagnosticSourceEventSourceActivitySourceListener Create(
        DiagnosticSourceEventSourceFilterAndTransform activitySourceSpecs)
    {
        var listener = new DiagnosticSourceEventSourceActivitySourceListener();

        listener.NormalizeActivitySourceSpecsList(activitySourceSpecs);

        listener.CreateActivityListener();

        return listener;
    }

    private DiagnosticSourceEventSourceActivitySourceListener()
    {
    }

    public void Dispose()
    {
        _activityListener?.Dispose();
        _activityListener = null;
        _wildcardActivitySourceSpecs = null;
        _activitySourceSpecsBySourceName = null;
    }

    private void NormalizeActivitySourceSpecsList(
        DiagnosticSourceEventSourceFilterAndTransform activitySourceSpecs)
    {
        Debug.Assert(activitySourceSpecs != null);

        DiagnosticSourceEventSourceFilterAndTransform? currentActivitySourceSpec = activitySourceSpecs;

        while (currentActivitySourceSpec != null)
        {
            Debug.Assert(currentActivitySourceSpec.SourceName != null);
            Debug.Assert(currentActivitySourceSpec.SampleFunc != null);

            DiagnosticSourceEventSourceFilterAndTransform? nextRaw = currentActivitySourceSpec.Next;

            if (currentActivitySourceSpec.SourceName == "*")
            {
                AddCurrentToSpecs(currentActivitySourceSpec, _wildcardActivitySourceSpecs ??= new());
            }
            else
            {
                var sources = _activitySourceSpecsBySourceName ??= new(StringComparer.OrdinalIgnoreCase);

                if (!sources.TryGetValue(currentActivitySourceSpec.SourceName, out ActivitySourceFilterAndTransformSpecs? specs))
                {
                    specs = new();
                    sources[currentActivitySourceSpec.SourceName] = specs;
                }

                AddCurrentToSpecs(currentActivitySourceSpec, specs);
            }

            currentActivitySourceSpec = nextRaw;
        }

        Debug.Assert(_wildcardActivitySourceSpecs != null || _activitySourceSpecsBySourceName != null);

        static void AddCurrentToSpecs(DiagnosticSourceEventSourceFilterAndTransform currentRaw, ActivitySourceFilterAndTransformSpecs specs)
        {
            if (currentRaw.ActivityName != null)
            {
                Dictionary<string, DiagnosticSourceEventSourceFilterAndTransform> specsByActivityName = specs.SpecsByActivityName ??= new(StringComparer.OrdinalIgnoreCase);

                currentRaw.Next = !specsByActivityName.TryGetValue(currentRaw.ActivityName, out DiagnosticSourceEventSourceFilterAndTransform? head)
                    ? null
                    : head;

                specsByActivityName[currentRaw.ActivityName] = currentRaw;
            }
            else
            {
                currentRaw.Next = specs.WildcardSpecs;
                specs.WildcardSpecs = currentRaw;
            }
        }
    }

    private void CreateActivityListener()
    {
        Debug.Assert(_activityListener == null);
        Debug.Assert(_wildcardActivitySourceSpecs != null
            || _activitySourceSpecsBySourceName != null);

        _activityListener = new ActivityListener();

        _activityListener.SampleUsingParentId = OnSampleUsingParentId;
        _activityListener.Sample = OnSample;

        _activityListener.ShouldListenTo = (activitySource) =>
        {
            return _wildcardActivitySourceSpecs != null
                || (_activitySourceSpecsBySourceName != null
                && _activitySourceSpecsBySourceName.ContainsKey(activitySource.Name));
        };

        _activityListener.ActivityStarted = OnActivityStarted;

        _activityListener.ActivityStopped = OnActivityStopped;

        ActivitySource.AddActivityListener(_activityListener);
    }

    private void OnActivityStarted(Activity activity)
    {
        if (_wildcardActivitySourceSpecs?.OnActivityStarted(activity) == true)
        {
            return;
        }

        if (_activitySourceSpecsBySourceName?.TryGetValue(activity.Source.Name, out ActivitySourceFilterAndTransformSpecs? specs) == true)
        {
            specs.OnActivityStarted(activity);
        }
    }

    private void OnActivityStopped(Activity activity)
    {
        if (_wildcardActivitySourceSpecs?.OnActivityStopped(activity) == true)
        {
            return;
        }

        if (_activitySourceSpecsBySourceName?.TryGetValue(activity.Source.Name, out ActivitySourceFilterAndTransformSpecs? specs) == true)
        {
            specs.OnActivityStopped(activity);
        }
    }

    private ActivitySamplingResult OnSampleUsingParentId(ref ActivityCreationOptions<string> options)
    {
        ActivityCreationOptions<ActivityContext> activityContextOptions = default;

        return OnSample(options.Source.Name, options.Name, hasActivityContext: false, ref activityContextOptions);
    }

    private ActivitySamplingResult OnSample(ref ActivityCreationOptions<ActivityContext> options)
    {
        return OnSample(options.Source.Name, options.Name, hasActivityContext: true, ref options);
    }

    private ActivitySamplingResult OnSample(
        string activitySourceName,
        string activityName,
        bool hasActivityContext,
        ref ActivityCreationOptions<ActivityContext> options)
    {
        return (_activitySourceSpecsBySourceName?.TryGetValue(activitySourceName, out ActivitySourceFilterAndTransformSpecs? specs) == true
            ? specs.Sample(activityName, hasActivityContext, ref options)
            : _wildcardActivitySourceSpecs?.Sample(activityName, hasActivityContext, ref options))
            ?? ActivitySamplingResult.None;
    }

    private sealed class ActivitySourceFilterAndTransformSpecs
    {
        public Dictionary<string, DiagnosticSourceEventSourceFilterAndTransform>? SpecsByActivityName;
        public DiagnosticSourceEventSourceFilterAndTransform? WildcardSpecs;

        public bool OnActivityStarted(Activity activity)
        {
            return OnActivityStarted(activity, WildcardSpecs)
                || (SpecsByActivityName != null
                    && SpecsByActivityName.TryGetValue(activity.OperationName, out DiagnosticSourceEventSourceFilterAndTransform? specs)
                    && OnActivityStarted(activity, specs));
        }

        public bool OnActivityStopped(Activity activity)
        {
            return OnActivityStopped(activity, WildcardSpecs)
                || (SpecsByActivityName != null
                    && SpecsByActivityName.TryGetValue(activity.OperationName, out DiagnosticSourceEventSourceFilterAndTransform? specs)
                    && OnActivityStopped(activity, specs));
        }

        public ActivitySamplingResult? Sample(
            string activityName,
            bool hasActivityContext,
            ref ActivityCreationOptions<ActivityContext> options)
        {
            return SpecsByActivityName?.TryGetValue(activityName, out DiagnosticSourceEventSourceFilterAndTransform? specs) == true
                ? Sample(hasActivityContext, ref options, specs)
                : Sample(hasActivityContext, ref options, WildcardSpecs);
        }

        [DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties, typeof(Activity))]
        [DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties, typeof(ActivityContext))]
        [DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties, typeof(ActivityEvent))]
        [DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties, typeof(ActivityLink))]
        [DynamicDependency(nameof(DateTime.Ticks), typeof(DateTime))]
        [DynamicDependency(nameof(TimeSpan.Ticks), typeof(TimeSpan))]
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
            Justification = "Activity's properties are being preserved with the DynamicDependencies on OnActivityStarted.")]
        private static bool OnActivityStarted(Activity activity, DiagnosticSourceEventSourceFilterAndTransform? list)
        {
            while (list != null)
            {
                if ((list.Events & DiagnosticSourceEventSourceFilterAndTransform.ActivityEvents.ActivityStart) != 0)
                {
                    DiagnosticSourceEventSource.Log.ActivityStart(activity.Source.Name, activity.OperationName, list.Morph(activity));
                    return true;
                }
                list = list.Next;
            }

            return false;
        }

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
            Justification = "Activity's properties are being preserved with the DynamicDependencies on OnActivityStarted.")]
        private static bool OnActivityStopped(Activity activity, DiagnosticSourceEventSourceFilterAndTransform? list)
        {
            while (list != null)
            {
                if ((list.Events & DiagnosticSourceEventSourceFilterAndTransform.ActivityEvents.ActivityStop) != 0)
                {
                    DiagnosticSourceEventSource.Log.ActivityStop(activity.Source.Name, activity.OperationName, list.Morph(activity));
                    return true;
                }
                list = list.Next;
            }

            return false;
        }

        private static ActivitySamplingResult? Sample(
            bool hasActivityContext,
            ref ActivityCreationOptions<ActivityContext> options,
            DiagnosticSourceEventSourceFilterAndTransform? list)
        {
            ActivitySamplingResult? finalSamplingResult = null;

            while (list != null)
            {
                ActivitySamplingResult samplingResult = list.SampleFunc!(hasActivityContext, ref options);

                if (!finalSamplingResult.HasValue || samplingResult > finalSamplingResult)
                {
                    finalSamplingResult = samplingResult;
                }

                if (finalSamplingResult >= ActivitySamplingResult.AllDataAndRecorded)
                {
                    return finalSamplingResult.Value; // highest possible value
                }

                list = list.Next;
            }

            return finalSamplingResult;
        }
    }
}
