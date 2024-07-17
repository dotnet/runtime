// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Tracing;

namespace System.Diagnostics;

internal sealed class DsesActivitySourceListener : IDisposable
{
    private DsesFilterAndTransform? _wildcardSpec;
    private Dictionary<SpecLookupKey, DsesFilterAndTransform>? _specsBySourceNameAndActivityName;
    private HashSet<string>? _listenToActivitySourceNames;
    private bool _hasActivityNameSpecDefined;
    private ActivityListener? _activityListener;

    public static DsesActivitySourceListener Create(
        DsesFilterAndTransform activitySourceSpecs)
    {
        var listener = new DsesActivitySourceListener();

        listener.NormalizeActivitySourceSpecsList(activitySourceSpecs);

        listener.CreateActivityListener();

        return listener;
    }

    private DsesActivitySourceListener()
    {
    }

    public void Dispose()
    {
        _activityListener?.Dispose();
        _activityListener = null;
        _wildcardSpec = null;
        _specsBySourceNameAndActivityName = null;
        _listenToActivitySourceNames = null;
    }

    private void NormalizeActivitySourceSpecsList(
        DsesFilterAndTransform? activitySourceSpecs)
    {
        Debug.Assert(activitySourceSpecs != null);

        while (activitySourceSpecs != null)
        {
            DsesFilterAndTransform? currentActivitySourceSpec = activitySourceSpecs;

            Debug.Assert(currentActivitySourceSpec.SourceName != null);
            Debug.Assert(currentActivitySourceSpec.SampleFunc != null);

            activitySourceSpecs = activitySourceSpecs.Next;

            if (currentActivitySourceSpec.SourceName == "*")
            {
                if (_wildcardSpec != null)
                {
                    if (DiagnosticSourceEventSource.Log.IsEnabled(EventLevel.Warning, DiagnosticSourceEventSource.Keywords.Messages))
                        DiagnosticSourceEventSource.Log.Message("DiagnosticSource: Ignoring wildcard activity source filterAndPayloadSpec rule because a previous rule was defined");
                    continue;
                }

                _wildcardSpec = currentActivitySourceSpec;
            }
            else
            {
                var specs = _specsBySourceNameAndActivityName ??= new(SpecLookupKeyComparer.Instance);
                var allSources = _listenToActivitySourceNames ??= new(StringComparer.OrdinalIgnoreCase);

                SpecLookupKey key = new(currentActivitySourceSpec.SourceName, currentActivitySourceSpec.ActivityName);

#if NETFRAMEWORK || NETSTANDARD2_0
                if (specs.ContainsKey(key))
                {
                    LogIgnoredSpecRule(currentActivitySourceSpec.SourceName, currentActivitySourceSpec.ActivityName);
                    continue;
                }
                specs[key] = currentActivitySourceSpec;
#else
                if (!specs.TryAdd(key, currentActivitySourceSpec))
                {
                    LogIgnoredSpecRule(currentActivitySourceSpec.SourceName, currentActivitySourceSpec.ActivityName);
                    continue;
                }
#endif
                allSources.Add(key.activitySourceName);
                if (key.activityName != null)
                {
                    _hasActivityNameSpecDefined = true;
                }
            }
        }

        Debug.Assert(_wildcardSpec != null || _specsBySourceNameAndActivityName != null);

        static void LogIgnoredSpecRule(string activitySourceName, string? activityName)
        {
            if (DiagnosticSourceEventSource.Log.IsEnabled(EventLevel.Warning, DiagnosticSourceEventSource.Keywords.Messages))
            {
                if (activityName == null)
                {
                    DiagnosticSourceEventSource.Log.Message($"DiagnosticSource: Ignoring filterAndPayloadSpec rule for '[AS]{activitySourceName}' because a previous rule was defined");
                }
                else
                {
                    DiagnosticSourceEventSource.Log.Message($"DiagnosticSource: Ignoring filterAndPayloadSpec rule for '[AS]{activitySourceName}+{activityName}' because a previous rule was defined");
                }
            }
        }
    }

    private void CreateActivityListener()
    {
        Debug.Assert(_activityListener == null);
        Debug.Assert(_wildcardSpec != null
            || _specsBySourceNameAndActivityName != null);

        _activityListener = new ActivityListener();

        _activityListener.SampleUsingParentId = OnSampleUsingParentId;
        _activityListener.Sample = OnSample;

        _activityListener.ShouldListenTo = (activitySource) =>
        {
            return _wildcardSpec != null
                || (_listenToActivitySourceNames != null
                && _listenToActivitySourceNames.Contains(activitySource.Name));
        };

        _activityListener.ActivityStarted = OnActivityStarted;

        _activityListener.ActivityStopped = OnActivityStopped;

        ActivitySource.AddActivityListener(_activityListener);
    }

    private bool TryFindSpecForActivity(
        string activitySourceName,
        string activityName,
        [NotNullWhen(true)] out DsesFilterAndTransform? spec)
    {
        if (_specsBySourceNameAndActivityName != null)
        {
            if (_hasActivityNameSpecDefined &&
                _specsBySourceNameAndActivityName.TryGetValue(new(activitySourceName, activityName), out spec))
            {
                return true;
            }
            if (_specsBySourceNameAndActivityName.TryGetValue(new(activitySourceName, null), out spec))
            {
                return true;
            }
        }

        return (spec = _wildcardSpec) != null;
    }

    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties, typeof(Activity))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties, typeof(ActivityContext))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties, typeof(ActivityEvent))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties, typeof(ActivityLink))]
    [DynamicDependency(nameof(DateTime.Ticks), typeof(DateTime))]
    [DynamicDependency(nameof(TimeSpan.Ticks), typeof(TimeSpan))]
    [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
        Justification = "Activity's properties are being preserved with the DynamicDependencies on OnActivityStarted.")]
    private void OnActivityStarted(Activity activity)
    {
        if (TryFindSpecForActivity(activity.Source.Name, activity.OperationName, out var spec)
            && (spec.Events & DsesActivityEvents.ActivityStart) != 0)
        {
            DiagnosticSourceEventSource.Log.ActivityStart(activity.Source.Name, activity.OperationName, spec.Morph(activity));
        }
    }

    [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
        Justification = "Activity's properties are being preserved with the DynamicDependencies on OnActivityStarted.")]
    private void OnActivityStopped(Activity activity)
    {
        if (TryFindSpecForActivity(activity.Source.Name, activity.OperationName, out var spec)
            && (spec.Events & DsesActivityEvents.ActivityStop) != 0)
        {
            DiagnosticSourceEventSource.Log.ActivityStop(activity.Source.Name, activity.OperationName, spec.Morph(activity));
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
        return TryFindSpecForActivity(activitySourceName, activityName, out var spec)
            ? spec.SampleFunc!(hasActivityContext, ref options)
            : ActivitySamplingResult.None;
    }

    private readonly struct SpecLookupKey
    {
        public SpecLookupKey(
            string activitySourceName,
            string? activityName)
        {
            Debug.Assert(activitySourceName != null);

            this.activitySourceName = activitySourceName;
            this.activityName = activityName;
        }

        public readonly string activitySourceName;
        public readonly string? activityName;
    }

    private sealed class SpecLookupKeyComparer : IEqualityComparer<SpecLookupKey>
    {
        public static readonly SpecLookupKeyComparer Instance = new();

        public bool Equals(SpecLookupKey x, SpecLookupKey y)
            => string.Equals(x.activitySourceName, y.activitySourceName, StringComparison.OrdinalIgnoreCase)
                && string.Equals(x.activityName, y.activityName, StringComparison.OrdinalIgnoreCase);

        public int GetHashCode(SpecLookupKey obj)
        {
            // HashCode.Combine would be the best but we need to compile for the full framework which require adding dependency
            // on the extensions package. Considering this simple type and hashing is not expected to be used much, we are implementing
            // the hashing manually.
            int hash = 5381;
            hash = ((hash << 5) + hash) + StringComparer.OrdinalIgnoreCase.GetHashCode(obj.activitySourceName);
            hash = ((hash << 5) + hash) + (obj.activityName == null ? 0 : StringComparer.OrdinalIgnoreCase.GetHashCode(obj.activityName));

            return hash;
        }
    }
}
