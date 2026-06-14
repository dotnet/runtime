// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Diagnostics.Tracing
{
    internal sealed class DefaultActivitySourceFactory : ActivitySourceFactory
    {
        private readonly Dictionary<string, FactoryActivitySource[]> _cachedSources = [];
        private readonly ActivityListenerRegistration[] _listenerRegistrations;
        private readonly IDisposable? _changeTokenRegistration;
        private bool _disposed;

        public DefaultActivitySourceFactory(IEnumerable<ActivityListenerBuilder> listenerBuilders, IOptionsMonitor<TracingOptions> options)
        {
            ArgumentNullException.ThrowIfNull(listenerBuilders);
            ArgumentNullException.ThrowIfNull(options);

            _listenerRegistrations = listenerBuilders
                .Select(listenerBuilder => new ActivityListenerRegistration(listenerBuilder, this))
                .ToArray();
            try
            {
                _changeTokenRegistration = options.OnChange((opts, name) =>
                {
                    if (string.IsNullOrEmpty(name))
                    {
                        UpdateRules(opts);
                    }
                });

                UpdateRules(options.CurrentValue, false);
            }
            catch
            {
                Dispose();
                throw;
            }
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

                if (TryGetCachedMatch(options, out ActivitySource? cached))
                {
                    return cached;
                }
            }

            // Construct outside the cache lock since the base ActivitySource constructor
            // walks ActivitySource.s_allListeners and synchronously invokes each listener's
            // ShouldListenTo predicate.
            FactoryActivitySource newSource = new FactoryActivitySource(options);

            lock (_cachedSources)
            {
                if (_disposed)
                {
                    newSource.Release();
                    throw new ObjectDisposedException(nameof(DefaultActivitySourceFactory));
                }

                if (TryGetCachedMatch(options, out ActivitySource? winner))
                {
                    // Lost the race to another concurrent Create call.
                    newSource.Release();
                    return winner;
                }

                if (_cachedSources.TryGetValue(options.Name, out FactoryActivitySource[]? sources))
                {
                    FactoryActivitySource[] grown = new FactoryActivitySource[sources.Length + 1];
                    sources.CopyTo(grown, 0);
                    grown[sources.Length] = newSource;
                    _cachedSources[options.Name] = grown;
                }
                else
                {
                    _cachedSources.Add(options.Name, [newSource]);
                }

                return newSource;
            }
        }

        private bool TryGetCachedMatch(ActivitySourceOptions options, [NotNullWhen(true)] out ActivitySource? match)
        {
            Debug.Assert(Monitor.IsEntered(_cachedSources));

            if (_cachedSources.TryGetValue(options.Name, out FactoryActivitySource[]? sources))
            {
                foreach (FactoryActivitySource source in sources)
                {
                    if (source.Version == options.Version
                        && source.TelemetrySchemaUrl == options.TelemetrySchemaUrl
                        && DiagnosticsHelper.CompareTags(source.Tags as IList<KeyValuePair<string, object?>>, options.Tags))
                    {
                        match = source;
                        return true;
                    }
                }
            }

            match = null;
            return false;
        }

        private void UpdateRules(TracingOptions options, bool overwrite = true)
        {
            if (Volatile.Read(ref _disposed))
            {
                return;
            }

            List<TracingRule> rules = options.Rules;
            foreach (ActivityListenerRegistration registration in _listenerRegistrations)
            {
                registration.UpdateRules(rules, overwrite);
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

            foreach (KeyValuePair<string, FactoryActivitySource[]> entry in _cachedSources)
            {
                foreach (FactoryActivitySource source in entry.Value)
                {
                    source.Release();
                }
            }

            _cachedSources.Clear();
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
            private readonly string _listenerName;
            private readonly DefaultActivitySourceFactory _activitySourceFactory;
            private readonly object _lock = new();
            private readonly SampleActivity<ActivityContext>? _sample;
            private readonly SampleActivity<string>? _sampleUsingParentId;
            private readonly Action<Activity>? _activityStarted;
            private readonly Action<Activity>? _activityStopped;
            private readonly ExceptionRecorder? _exceptionRecorder;
            private readonly ActivityListener _activityListener;
            private ListenerState _state;
            private bool _disposed;

            public ActivityListenerRegistration(ActivityListenerBuilder listenerBuilder, DefaultActivitySourceFactory activitySourceFactory)
            {
                _activitySourceFactory = activitySourceFactory;
                _listenerName = listenerBuilder.Name;
                _sample = listenerBuilder.Sample;
                _sampleUsingParentId = listenerBuilder.SampleUsingParentId;
                _activityStarted = listenerBuilder.ActivityStarted;
                _activityStopped = listenerBuilder.ActivityStopped;
                _exceptionRecorder = listenerBuilder.ExceptionRecorder;
                _state = ListenerState.Empty;
                _activityListener = new ActivityListener { ShouldListenTo = ShouldListenTo };
                ApplyListenerDelegates(false);
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
                    _state = ListenerState.Empty;
                }
            }

            public void UpdateRules(List<TracingRule> rules, bool overwrite = true)
            {
                ArgumentNullException.ThrowIfNull(rules);

                lock (_lock)
                {
                    if (_disposed)
                    {
                        return;
                    }

                    if (!overwrite && !ReferenceEquals(_state, ListenerState.Empty))
                    {
                        return;
                    }

                    ListenerState newState = ListenerState.Create(rules);
                    bool delegatesNeedSwap = newState.HasOperationNameRules != _state.HasOperationNameRules;
                    Volatile.Write(ref _state, newState);
                    if (delegatesNeedSwap)
                    {
                        ApplyListenerDelegates(newState.HasOperationNameRules);
                    }
                }

                _activityListener.RefreshSources();
            }

            private void ApplyListenerDelegates(bool hasOperationNameRules)
            {
                _activityListener.Sample = _sample is null ? null : (hasOperationNameRules ? WrappedSample : _sample);
                _activityListener.SampleUsingParentId = _sampleUsingParentId is null ? null : (hasOperationNameRules ? WrappedSampleUsingParentId : _sampleUsingParentId);
                _activityListener.ActivityStarted = _activityStarted is null ? null : (hasOperationNameRules ? WrappedActivityStarted : _activityStarted);
                _activityListener.ActivityStopped = _activityStopped is null ? null : (hasOperationNameRules ? WrappedActivityStopped : _activityStopped);
                _activityListener.ExceptionRecorder = _exceptionRecorder is null ? null : (hasOperationNameRules ? WrappedExceptionRecorder : _exceptionRecorder);
            }


            private ActivitySamplingResult WrappedSample(ref ActivityCreationOptions<ActivityContext> options)
            {
                ListenerState state = Volatile.Read(ref _state);
                if (state.HasOperationNameRules && !IsEnabledFast(state, options.Source, options.Name))
                {
                    return ActivitySamplingResult.None;
                }

                Debug.Assert(_sample is not null);
                return _sample!.Invoke(ref options);
            }

            private ActivitySamplingResult WrappedSampleUsingParentId(ref ActivityCreationOptions<string> options)
            {
                ListenerState state = Volatile.Read(ref _state);
                if (state.HasOperationNameRules && !IsEnabledFast(state, options.Source, options.Name))
                {
                    return ActivitySamplingResult.None;
                }

                Debug.Assert(_sampleUsingParentId is not null);
                return _sampleUsingParentId!.Invoke(ref options);
            }

            private void WrappedActivityStarted(Activity activity)
            {
                ListenerState state = Volatile.Read(ref _state);
                if (!state.HasOperationNameRules || IsEnabledFast(state, activity.Source, activity.OperationName))
                {
                    Debug.Assert(_activityStarted is not null);
                    _activityStarted!.Invoke(activity);
                }
            }

            private void WrappedActivityStopped(Activity activity)
            {
                ListenerState state = Volatile.Read(ref _state);
                if (!state.HasOperationNameRules || IsEnabledFast(state, activity.Source, activity.OperationName))
                {
                    Debug.Assert(_activityStopped is not null);
                    _activityStopped!.Invoke(activity);
                }
            }

            private void WrappedExceptionRecorder(Activity activity, Exception exception, ref TagList tags)
            {
                ListenerState state = Volatile.Read(ref _state);
                if (!state.HasOperationNameRules || IsEnabledFast(state, activity.Source, activity.OperationName))
                {
                    Debug.Assert(_exceptionRecorder is not null);
                    _exceptionRecorder!.Invoke(activity, exception, ref tags);
                }
            }

            private bool IsEnabledFast(ListenerState state, ActivitySource source, string operationName)
            {
                (string Name, bool IsLocalScope) key = (source.Name, ReferenceEquals(_activitySourceFactory, source.Scope));
                if (!state.SourceFilterStates.TryGetValue(key, out SourceFilterState filter))
                {
                    // Miss only fires in the brief window between UpdateRules swapping in a fresh
                    // (empty) cache and RefreshSources repopulating it for this source. We recompute
                    // without writing back: the extra work per call is preferable to the dictionary
                    // copy a CAS write-back would cost, and the next ShouldListenTo populates the
                    // entry anyway.
                    filter = ComputeFilterState(state.Rules, key.Name, key.IsLocalScope);
                }
                bool divergent = filter.Divergent is { } d && d.Contains(operationName);
                return divergent ? !filter.DefaultEnabled : filter.DefaultEnabled;
            }

            private SourceFilterState ComputeFilterState(IList<TracingRule> rules, string sourceName, bool isLocalScope)
            {
                TracingRule? defaultRule = GetMostSpecificRule(rules, sourceName, operationName: null, _listenerName, isLocalScope, considerOperationName: true);
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

                    bool enabled = IsOperationEnabled(rules, sourceName, isLocalScope, rule.OperationName);
                    if (enabled != defaultEnabled)
                    {
                        divergent ??= new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                        divergent.Add(rule.OperationName);
                    }
                }

                return new SourceFilterState(defaultEnabled, divergent);
            }

            private bool IsOperationEnabled(IList<TracingRule> rules, string sourceName, bool isLocalScope, string operationName)
            {
                TracingRule? rule = GetMostSpecificRule(rules, sourceName, operationName, _listenerName, isLocalScope, considerOperationName: true);
                return rule?.Enable ?? false;
            }

            private bool ShouldListenTo(ActivitySource activitySource)
            {
                if (activitySource.Scope is { } s && !ReferenceEquals(s, _activitySourceFactory))
                {
                    return false;
                }

                (string Name, bool IsLocalScope) key = (activitySource.Name, ReferenceEquals(_activitySourceFactory, activitySource.Scope));

                SourceFilterState filter;
                while (true)
                {
                    ListenerState state = Volatile.Read(ref _state);
                    if (state.SourceFilterStates.TryGetValue(key, out filter))
                    {
                        break;
                    }

                    filter = ComputeFilterState(state.Rules, key.Name, key.IsLocalScope);
                    // Copy-on-write via CAS so IsEnabledFast readers stay lock-free and
                    // UpdateRules can swap the whole state without blocking concurrent ShouldListenTo calls.
                    var newDict = new Dictionary<(string Name, bool IsLocalScope), SourceFilterState>(state.SourceFilterStates)
                    {
                        [key] = filter,
                    };
                    ListenerState newState = state.WithSourceFilterStates(newDict);
                    if (Interlocked.CompareExchange(ref _state, newState, state) == state)
                    {
                        break;
                    }
                }

                return filter.DefaultEnabled || filter.Divergent is { Count: > 0 };
            }

            private static TracingRule? GetMostSpecificRule(IList<TracingRule> rules, string sourceName, string? operationName, string? listenerName, bool isLocalScope, bool considerOperationName)
            {
                TracingRule? best = null;
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
                // TracingRule's constructor validates that at most one '*' is present, so we don't
                // re-check here. If a pattern with multiple wildcards somehow reaches this code,
                // the second wildcard is silently treated as a literal '*' inside the suffix.

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

            private sealed class ListenerState
            {
                public static readonly ListenerState Empty = new([], hasOperationNameRules: false, new Dictionary<(string, bool), SourceFilterState>());

                public ListenerState(IList<TracingRule> rules, bool hasOperationNameRules, Dictionary<(string Name, bool IsLocalScope), SourceFilterState> sourceFilterStates)
                {
                    Rules = rules;
                    HasOperationNameRules = hasOperationNameRules;
                    SourceFilterStates = sourceFilterStates;
                }

                public IList<TracingRule> Rules { get; }
                public bool HasOperationNameRules { get; }

                // Keyed by (Name, IsLocalScope) rather than by ActivitySource instance so that
                // disposed sources do not stay pinned in the cache, and so that two sources
                // sharing the same name and scope (e.g. a source recreated after a previous
                // instance was disposed) share the cached filter state.
                public Dictionary<(string Name, bool IsLocalScope), SourceFilterState> SourceFilterStates { get; }

                public static ListenerState Create(IList<TracingRule> rules)
                    => new(rules, ComputeHasOperationNameRules(rules), new Dictionary<(string, bool), SourceFilterState>());

                public ListenerState WithSourceFilterStates(Dictionary<(string Name, bool IsLocalScope), SourceFilterState> sourceFilterStates)
                    => new(Rules, HasOperationNameRules, sourceFilterStates);

                private static bool ComputeHasOperationNameRules(IList<TracingRule> rules)
                {
                    foreach (TracingRule rule in rules)
                    {
                        if (!string.IsNullOrEmpty(rule.OperationName))
                        {
                            return true;
                        }
                    }

                    return false;
                }
            }
        }
    }
}
