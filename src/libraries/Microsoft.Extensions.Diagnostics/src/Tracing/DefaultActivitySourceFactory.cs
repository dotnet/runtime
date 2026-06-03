// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Diagnostics.Tracing
{
    internal sealed class DefaultActivitySourceFactory : ActivitySourceFactory
    {
        private readonly Dictionary<string, List<FactoryActivitySource>> _cachedSources = [];
        private readonly ActivityListenerRegistration[] _listenerRegistrations;
        private readonly IDisposable? _changeTokenRegistration;
        private bool _disposed;

        public DefaultActivitySourceFactory(IEnumerable<ActivityListener> listeners, IOptionsMonitor<TracingOptions> options)
        {
            ArgumentNullException.ThrowIfNull(listeners);
            ArgumentNullException.ThrowIfNull(options);

            _listenerRegistrations = [.. listeners.Select(listener => new ActivityListenerRegistration(listener, this, options.CurrentValue))];
            _changeTokenRegistration = options.OnChange(UpdateRules);
        }

        protected override ActivitySource CreateCore(ActivitySourceOptions options)
        {
            Debug.Assert(options is not null);
            Debug.Assert(options.Name is not null);
            Debug.Assert(ReferenceEquals(options.Scope, this));

            lock (_cachedSources)
            {
                if (_disposed)
                {
                    throw new ObjectDisposedException(nameof(DefaultActivitySourceFactory));
                }

                if (_cachedSources.TryGetValue(options.Name, out List<FactoryActivitySource>? sourceList))
                {
                    foreach (ActivitySource source in sourceList)
                    {
                        if (source.Version == options.Version && DiagnosticsHelper.CompareTags(source.Tags as IList<KeyValuePair<string, object?>>, options.Tags))
                        {
                            return source;
                        }
                    }
                }
                else
                {
                    sourceList = new List<FactoryActivitySource>();
                    _cachedSources.Add(options.Name, sourceList);
                }

                FactoryActivitySource activitySource = new FactoryActivitySource(options);
                sourceList.Add(activitySource);
                return activitySource;
            }
        }

        private void UpdateRules(TracingOptions options)
        {
            if (Volatile.Read(ref _disposed))
            {
                return;
            }

            List<TracingRule> rules = options.Rules;
            foreach (ActivityListenerRegistration registration in _listenerRegistrations)
            {
                registration.UpdateRules(rules);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (!disposing)
            {
                return;
            }

            lock (_cachedSources)
            {
                if (_disposed)
                {
                    return;
                }

                Volatile.Write(ref _disposed, true);
                _changeTokenRegistration?.Dispose();
            }

            foreach (ActivityListenerRegistration registration in _listenerRegistrations)
            {
                registration.Dispose();
            }

            foreach (var entry in _cachedSources)
            {
                foreach (var source in entry.Value)
                {
                    source.Release();
                }
            }
        }

        internal sealed class FactoryActivitySource : ActivitySource
        {
            public FactoryActivitySource(ActivitySourceOptions options) : base(options)
            {
            }

            public void Release() => base.Dispose(true); // call the protected Dispose(bool)

            protected override void Dispose(bool disposing)
            {
                // no-op, disallow users from disposing of the activity sources created by the factory.
            }
        }

        private sealed class ActivityListenerRegistration : IDisposable
        {
            private readonly string? _listenerName;
            private readonly DefaultActivitySourceFactory _activitySourceFactory;
            private readonly object _lock = new();
            private readonly ActivityListener _userListener;
            private readonly ActivityListener _activityListener;
            private Dictionary<ActivitySource, SourceFilterState> _sourceFilterStates = new();
            private List<TracingRule> _rules = [];
            private bool _disposed;

            public ActivityListenerRegistration(ActivityListener listener, DefaultActivitySourceFactory activitySourceFactory, TracingOptions options)
            {
                ArgumentNullException.ThrowIfNull(listener);
                _userListener = listener;
                _activitySourceFactory = activitySourceFactory ?? throw new ArgumentNullException(nameof(activitySourceFactory));
                _listenerName = listener.Name;
                _rules = options.Rules;
                _activityListener = new ActivityListener(_listenerName)
                {
                    ShouldListenTo = ShouldListenTo,
                    Sample = WrappedSample,
                    SampleUsingParentId = WrappedSampleUsingParentId,
                    ActivityStarted = WrappedActivityStarted,
                    ActivityStopped = WrappedActivityStopped,
                    ExceptionRecorder = WrappedExceptionRecorder,
                };
                ActivitySource.AddActivityListener(_activityListener);
            }

            public void Dispose()
            {
                lock (_lock)
                {
                    if (_disposed)
                    {
                        return;
                    }

                    _disposed = true;
                    _activityListener.Dispose();
                    _rules = [];
                }
            }

            public void UpdateRules(List<TracingRule> rules)
            {
                ArgumentNullException.ThrowIfNull(rules);

                lock (_lock)
                {
                    if (_disposed)
                    {
                        return;
                    }

                    Volatile.Write(ref _rules, rules);
                    // Drop the per-source decision cache: stale entries would otherwise mis-route notifications.
                    // RefreshSources below will re-invoke ShouldListenTo, which repopulates the dictionary.
                    Volatile.Write(ref _sourceFilterStates, new Dictionary<ActivitySource, SourceFilterState>());
                    _activityListener.RefreshSources();
                }
            }

            private ActivitySamplingResult WrappedSample(ref ActivityCreationOptions<ActivityContext> options)
            {
                if (!IsEnabledFast(options.Source, options.Name))
                {
                    return ActivitySamplingResult.None;
                }

                return _userListener.Sample?.Invoke(ref options) ?? ActivitySamplingResult.None;
            }

            private ActivitySamplingResult WrappedSampleUsingParentId(ref ActivityCreationOptions<string> options)
            {
                if (!IsEnabledFast(options.Source, options.Name))
                {
                    return ActivitySamplingResult.None;
                }

                return _userListener.SampleUsingParentId?.Invoke(ref options) ?? ActivitySamplingResult.None;
            }

            private void WrappedActivityStarted(Activity activity)
            {
                if (IsEnabledFast(activity.Source, activity.OperationName))
                {
                    _userListener.ActivityStarted?.Invoke(activity);
                }
            }

            private void WrappedActivityStopped(Activity activity)
            {
                if (IsEnabledFast(activity.Source, activity.OperationName))
                {
                    _userListener.ActivityStopped?.Invoke(activity);
                }
            }

            private void WrappedExceptionRecorder(Activity activity, Exception exception, ref TagList tags)
            {
                if (IsEnabledFast(activity.Source, activity.OperationName))
                {
                    _userListener.ExceptionRecorder?.Invoke(activity, exception, ref tags);
                }
            }

            private bool IsEnabledFast(ActivitySource source, string operationName)
            {
                Dictionary<ActivitySource, SourceFilterState> states = Volatile.Read(ref _sourceFilterStates);
                if (!states.TryGetValue(source, out SourceFilterState state))
                {
                    // Cache miss is rare (race against UpdateRules clearing the dictionary).
                    // Compute on the fly without caching; the next ShouldListenTo for this source repopulates.
                    state = ComputeFilterState(source);
                }
                bool divergent = state.Divergent is { } d && d.Contains(operationName);
                return divergent ? !state.DefaultEnabled : state.DefaultEnabled;
            }

            private SourceFilterState ComputeFilterState(ActivitySource source)
            {
                bool isLocalScope = ReferenceEquals(_activitySourceFactory, source.Scope);
                IList<TracingRule> rules = Volatile.Read(ref _rules);

                TracingRule? defaultRule = GetMostSpecificRule(source.Name, operationName: null, _listenerName, isLocalScope, considerOperationName: true);
                bool defaultEnabled = defaultRule?.Enable ?? false;

                HashSet<string>? divergent = null;
                HashSet<string>? seen = null;
                foreach (TracingRule rule in rules)
                {
                    if (string.IsNullOrEmpty(rule.OperationName))
                    {
                        continue;
                    }

                    seen ??= new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    if (!seen.Add(rule.OperationName))
                    {
                        continue;
                    }

                    bool enabled = IsOperationEnabled(source, rule.OperationName);
                    if (enabled != defaultEnabled)
                    {
                        divergent ??= new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                        divergent.Add(rule.OperationName);
                    }
                }

                return new SourceFilterState(defaultEnabled, divergent);
            }

            private bool IsOperationEnabled(ActivitySource source, string operationName)
            {
                bool isLocalScope = ReferenceEquals(_activitySourceFactory, source.Scope);
                TracingRule? rule = GetMostSpecificRule(source.Name, operationName, _listenerName, isLocalScope, considerOperationName: true);
                return rule?.Enable ?? false;
            }

            private bool ShouldListenTo(ActivitySource activitySource)
            {
                SourceFilterState state;
                while (true)
                {
                    Dictionary<ActivitySource, SourceFilterState> snapshot = Volatile.Read(ref _sourceFilterStates);
                    if (snapshot.TryGetValue(activitySource, out state))
                    {
                        break;
                    }

                    state = ComputeFilterState(activitySource);
                    // Copy-on-write via CAS so IsEnabledFast readers stay lock-free and
                    // UpdateRules can reset the cache without blocking concurrent ShouldListenTo calls.
                    var newStates = new Dictionary<ActivitySource, SourceFilterState>(snapshot)
                    {
                        [activitySource] = state,
                    };
                    if (Interlocked.CompareExchange(ref _sourceFilterStates, newStates, snapshot) == snapshot)
                    {
                        break;
                    }
                }

                bool rulesAllow = state.DefaultEnabled || state.Divergent is { Count: > 0 };
                if (!rulesAllow)
                {
                    return false;
                }

                return _userListener.ShouldListenTo?.Invoke(activitySource) ?? true;
            }

            private TracingRule? GetMostSpecificRule(string sourceName, string? operationName, string? listenerName, bool isLocalScope, bool considerOperationName)
            {
                TracingRule? best = null;
                IList<TracingRule> rules = Volatile.Read(ref _rules);
                foreach (TracingRule rule in rules)
                {
                    if (RuleMatches(rule, sourceName, listenerName, isLocalScope, considerOperationName, operationName)
                        && IsMoreSpecific(rule, best, isLocalScope, considerOperationName))
                    {
                        best = rule;
                    }
                }

                return best;
            }

            private static bool RuleMatches(TracingRule rule, string sourceName, string? listenerName, bool isLocalScope, bool considerOperationName, string? operationName = null)
            {
                if (!string.IsNullOrEmpty(rule.ListenerName)
                    && !string.Equals(rule.ListenerName, listenerName, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                if (!rule.Scopes.HasFlag(isLocalScope ? ActivitySourceScopes.Local : ActivitySourceScopes.Global))
                {
                    return false;
                }

                if (!Matches(rule.SourceName, sourceName))
                {
                    return false;
                }

                if (considerOperationName && !string.IsNullOrEmpty(rule.OperationName))
                {
                    if (operationName is null
                        || !string.Equals(rule.OperationName, operationName, StringComparison.OrdinalIgnoreCase))
                    {
                        return false;
                    }
                }

                return true;
            }

            private static bool IsMoreSpecific(TracingRule rule, TracingRule? best, bool isLocalScope, bool considerOperationName)
            {
                if (best is null)
                {
                    return true;
                }

                if (!string.IsNullOrEmpty(rule.ListenerName) && string.IsNullOrEmpty(best.ListenerName))
                {
                    return true;
                }
                else if (string.IsNullOrEmpty(rule.ListenerName) && !string.IsNullOrEmpty(best.ListenerName))
                {
                    return false;
                }

                if (!string.IsNullOrEmpty(rule.SourceName))
                {
                    if (string.IsNullOrEmpty(best.SourceName))
                    {
                        return true;
                    }

                    if (rule.SourceName.Length != best.SourceName.Length)
                    {
                        return rule.SourceName.Length > best.SourceName.Length;
                    }
                }
                else if (!string.IsNullOrEmpty(best.SourceName))
                {
                    return false;
                }

                if (considerOperationName)
                {
                    if (!string.IsNullOrEmpty(rule.OperationName) && string.IsNullOrEmpty(best.OperationName))
                    {
                        return true;
                    }
                    else if (string.IsNullOrEmpty(rule.OperationName) && !string.IsNullOrEmpty(best.OperationName))
                    {
                        return false;
                    }
                }

                if (isLocalScope)
                {
                    if (!rule.Scopes.HasFlag(ActivitySourceScopes.Global) && best.Scopes.HasFlag(ActivitySourceScopes.Global))
                    {
                        return true;
                    }
                    else if (rule.Scopes.HasFlag(ActivitySourceScopes.Global) && !best.Scopes.HasFlag(ActivitySourceScopes.Global))
                    {
                        return false;
                    }
                }
                else
                {
                    if (!rule.Scopes.HasFlag(ActivitySourceScopes.Local) && best.Scopes.HasFlag(ActivitySourceScopes.Local))
                    {
                        return true;
                    }
                    else if (rule.Scopes.HasFlag(ActivitySourceScopes.Local) && !best.Scopes.HasFlag(ActivitySourceScopes.Local))
                    {
                        return false;
                    }
                }

                return true;
            }

            private static bool Matches(string? pattern, string name)
            {
                if (string.IsNullOrEmpty(pattern))
                {
                    return true;
                }

                const char WildcardChar = '*';
                int wildcardIndex = pattern.IndexOf(WildcardChar);
                if (wildcardIndex >= 0 &&
                    pattern.IndexOf(WildcardChar, wildcardIndex + 1) >= 0)
                {
                    throw new InvalidOperationException(SR.MoreThanOneWildcardActivitySourceName);
                }

                ReadOnlySpan<char> prefix;
                ReadOnlySpan<char> suffix;
                if (wildcardIndex < 0)
                {
                    prefix = pattern.AsSpan();
                    suffix = default;
                }
                else
                {
                    prefix = pattern.AsSpan(0, wildcardIndex);
                    suffix = pattern.AsSpan(wildcardIndex + 1);
                }

                return name.AsSpan().StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
                    && name.AsSpan().EndsWith(suffix, StringComparison.OrdinalIgnoreCase);
            }

            private readonly struct SourceFilterState
            {
                public SourceFilterState(bool defaultEnabled, HashSet<string>? divergent)
                {
                    DefaultEnabled = defaultEnabled;
                    Divergent = divergent;
                }

                public bool DefaultEnabled { get; }
                public HashSet<string>? Divergent { get; }
            }
        }
    }
}
