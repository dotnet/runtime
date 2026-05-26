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
    internal sealed class DefaultActivitySourceFactory : IActivitySourceFactory
    {
        private readonly Dictionary<string, List<FactoryActivitySource>> _cachedSources = [];
        private readonly ActivityListenerRegistration[] _listenerRegistrations;
        private readonly IDisposable? _changeTokenRegistration;
        private bool _disposed;

        public DefaultActivitySourceFactory(IEnumerable<IActivityListener> listeners, IOptionsMonitor<TracingOptions> options)
        {
            ArgumentNullException.ThrowIfNull(listeners);
            ArgumentNullException.ThrowIfNull(options);

            _listenerRegistrations = [.. listeners.Select(listener => new ActivityListenerRegistration(listener, this, options.CurrentValue))];
            _changeTokenRegistration = options.OnChange(UpdateRules);
        }

        public ActivitySource Create(ActivitySourceOptions options)
        {
            ArgumentNullException.ThrowIfNull(options);

            if (options.Scope is not null && !ReferenceEquals(options.Scope, this))
            {
                throw new InvalidOperationException(SR.InvalidActivitySourceScope);
            }

            Debug.Assert(options.Name is not null);

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

                object? scope = options.Scope;
                options.Scope = this;
                FactoryActivitySource activitySource;
                try
                {
                    activitySource = new FactoryActivitySource(options);
                }
                finally
                {
                    options.Scope = scope;
                }

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

            IList<TracingRule> rules = options.Rules;
            foreach (ActivityListenerRegistration registration in _listenerRegistrations)
            {
                registration.UpdateRules(rules);
            }
        }

        public void Dispose()
        {
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
            private readonly IActivityListener _listener;
            private readonly DefaultActivitySourceFactory _activitySourceFactory;
            private readonly object _lock = new();
            private readonly ActivityListener _activityListener;
            private readonly SampleActivity<ActivityContext>? _userSample;
            private readonly SampleActivity<string>? _userSampleUsingParentId;
            private readonly Action<Activity>? _userActivityStarted;
            private readonly Action<Activity>? _userActivityStopped;
            private readonly ExceptionRecorder? _userExceptionRecorder;
            private Dictionary<ActivitySource, SourceFilterState> _sourceFilterStates = new();
            private IList<TracingRule> _rules = Array.Empty<TracingRule>();
            private bool _hasActivityNameRules;
            private bool _disposed;

            public ActivityListenerRegistration(IActivityListener listener, DefaultActivitySourceFactory activitySourceFactory, TracingOptions options)
            {
                _listener = listener ?? throw new ArgumentNullException(nameof(listener));
                _activitySourceFactory = activitySourceFactory ?? throw new ArgumentNullException(nameof(activitySourceFactory));
                _userSample = listener.Sample;
                _userSampleUsingParentId = listener.SampleUsingParentId;
                _userActivityStarted = listener.ActivityStarted;
                _userActivityStopped = listener.ActivityStopped;
                _userExceptionRecorder = listener.ActivityExceptionRecorded;
                _activityListener = new ActivityListener()
                {
                    ShouldListenTo = ShouldListenTo,
                };
                _rules = options.Rules;
                _hasActivityNameRules = ComputeHasActivityNameRules(_rules);
                ConfigureDelegates();
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
                    _rules = Array.Empty<TracingRule>();
                }
            }

            public void UpdateRules(IList<TracingRule> rules)
            {
                ArgumentNullException.ThrowIfNull(rules);

                lock (_lock)
                {
                    if (_disposed)
                    {
                        return;
                    }

                    Volatile.Write(ref _rules, rules);
                    bool hadActivityNameRules = _hasActivityNameRules;
                    _hasActivityNameRules = ComputeHasActivityNameRules(rules);
                    // Drop the per-source decision cache: stale entries would otherwise mis-route notifications.
                    // RefreshSources below will re-invoke ShouldListenTo, which repopulates the dictionary.
                    Volatile.Write(ref _sourceFilterStates, new Dictionary<ActivitySource, SourceFilterState>());
                    if (hadActivityNameRules != _hasActivityNameRules)
                    {
                        ConfigureDelegates();
                    }

                    _activityListener.RefreshSources();
                }
            }

            private void ConfigureDelegates()
            {
                if (_hasActivityNameRules)
                {
                    _activityListener.Sample = _userSample is null ? null : WrappedSample;
                    _activityListener.SampleUsingParentId = _userSampleUsingParentId is null ? null : WrappedSampleUsingParentId;
                    _activityListener.ActivityStarted = _userActivityStarted is null ? null : WrappedActivityStarted;
                    _activityListener.ActivityStopped = _userActivityStopped is null ? null : WrappedActivityStopped;
                    _activityListener.ExceptionRecorder = _userExceptionRecorder is null ? null : WrappedExceptionRecorder;
                }
                else
                {
                    _activityListener.Sample = _userSample;
                    _activityListener.SampleUsingParentId = _userSampleUsingParentId;
                    _activityListener.ActivityStarted = _userActivityStarted;
                    _activityListener.ActivityStopped = _userActivityStopped;
                    _activityListener.ExceptionRecorder = _userExceptionRecorder;
                }
            }

            private static bool ComputeHasActivityNameRules(IList<TracingRule> rules)
            {
                foreach (TracingRule rule in rules)
                {
                    if (!string.IsNullOrEmpty(rule.ActivityName))
                    {
                        return true;
                    }
                }

                return false;
            }

            private ActivitySamplingResult WrappedSample(ref ActivityCreationOptions<ActivityContext> options)
            {
                if (!IsEnabledFast(options.Source, options.Name))
                {
                    return ActivitySamplingResult.None;
                }

                return _userSample!(ref options);
            }

            private ActivitySamplingResult WrappedSampleUsingParentId(ref ActivityCreationOptions<string> options)
            {
                if (!IsEnabledFast(options.Source, options.Name))
                {
                    return ActivitySamplingResult.None;
                }

                return _userSampleUsingParentId!(ref options);
            }

            private void WrappedActivityStarted(Activity activity)
            {
                if (IsEnabledFast(activity.Source, activity.OperationName))
                {
                    _userActivityStarted!(activity);
                }
            }

            private void WrappedActivityStopped(Activity activity)
            {
                if (IsEnabledFast(activity.Source, activity.OperationName))
                {
                    _userActivityStopped!(activity);
                }
            }

            private void WrappedExceptionRecorder(Activity activity, Exception exception, ref TagList tags)
            {
                if (IsEnabledFast(activity.Source, activity.OperationName))
                {
                    _userExceptionRecorder!(activity, exception, ref tags);
                }
            }

            private bool IsEnabledFast(ActivitySource source, string activityName)
            {
                Dictionary<ActivitySource, SourceFilterState> states = Volatile.Read(ref _sourceFilterStates);
                if (!states.TryGetValue(source, out SourceFilterState state))
                {
                    // Cache miss is rare (race against UpdateRules clearing the dictionary).
                    // Compute on the fly without caching; the next ShouldListenTo for this source repopulates.
                    state = ComputeFilterState(source);
                }
                bool divergent = state.Divergent is { } d && d.Contains(activityName);
                return divergent ? !state.DefaultEnabled : state.DefaultEnabled;
            }

            private SourceFilterState ComputeFilterState(ActivitySource source)
            {
                bool isLocalScope = ReferenceEquals(_activitySourceFactory, source.Scope);
                IList<TracingRule> rules = Volatile.Read(ref _rules);

                TracingRule? defaultRule = GetMostSpecificRule(source.Name, activityName: null, _listener.Name, isLocalScope, considerActivityName: true);
                bool defaultEnabled = defaultRule?.Enabled ?? false;

                HashSet<string>? divergent = null;
                HashSet<string>? seen = null;
                foreach (TracingRule rule in rules)
                {
                    if (string.IsNullOrEmpty(rule.ActivityName))
                    {
                        continue;
                    }

                    seen ??= new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    if (!seen.Add(rule.ActivityName))
                    {
                        continue;
                    }

                    bool enabled = IsActivityEnabled(source, rule.ActivityName);
                    if (enabled != defaultEnabled)
                    {
                        divergent ??= new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                        divergent.Add(rule.ActivityName);
                    }
                }

                return new SourceFilterState(defaultEnabled, divergent);
            }

            private bool IsActivityEnabled(ActivitySource source, string activityName)
            {
                bool isLocalScope = ReferenceEquals(_activitySourceFactory, source.Scope);
                TracingRule? rule = GetMostSpecificRule(source.Name, activityName, _listener.Name, isLocalScope, considerActivityName: true);
                return rule?.Enabled ?? false;
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

                return state.DefaultEnabled || state.Divergent is { Count: > 0 };
            }

            private TracingRule? GetMostSpecificRule(string activitySourceName, string? activityName, string listenerName, bool isLocalScope, bool considerActivityName)
            {
                TracingRule? best = null;
                IList<TracingRule> rules = Volatile.Read(ref _rules);
                foreach (TracingRule rule in rules)
                {
                    if (RuleMatches(rule, activitySourceName, listenerName, isLocalScope, considerActivityName, activityName)
                        && IsMoreSpecific(rule, best, isLocalScope, considerActivityName))
                    {
                        best = rule;
                    }
                }

                return best;
            }

            private static bool RuleMatches(TracingRule rule, string activitySourceName, string listenerName, bool isLocalScope, bool considerActivityName, string? activityName = null)
            {
                if (!string.IsNullOrEmpty(rule.ListenerName)
                    && !string.Equals(rule.ListenerName, listenerName, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                if (!rule.Scopes.HasFlag(isLocalScope ? ActivitySourceScope.Local : ActivitySourceScope.Global))
                {
                    return false;
                }

                if (!Matches(rule.ActivitySourceName, activitySourceName))
                {
                    return false;
                }

                if (considerActivityName && !string.IsNullOrEmpty(rule.ActivityName))
                {
                    if (activityName is null
                        || !string.Equals(rule.ActivityName, activityName, StringComparison.OrdinalIgnoreCase))
                    {
                        return false;
                    }
                }

                return true;
            }

            private static bool IsMoreSpecific(TracingRule rule, TracingRule? best, bool isLocalScope, bool considerActivityName)
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

                if (!string.IsNullOrEmpty(rule.ActivitySourceName))
                {
                    if (string.IsNullOrEmpty(best.ActivitySourceName))
                    {
                        return true;
                    }

                    if (rule.ActivitySourceName.Length != best.ActivitySourceName.Length)
                    {
                        return rule.ActivitySourceName.Length > best.ActivitySourceName.Length;
                    }
                }
                else if (!string.IsNullOrEmpty(best.ActivitySourceName))
                {
                    return false;
                }

                if (considerActivityName)
                {
                    if (!string.IsNullOrEmpty(rule.ActivityName) && string.IsNullOrEmpty(best.ActivityName))
                    {
                        return true;
                    }
                    else if (string.IsNullOrEmpty(rule.ActivityName) && !string.IsNullOrEmpty(best.ActivityName))
                    {
                        return false;
                    }
                }

                if (isLocalScope)
                {
                    if (!rule.Scopes.HasFlag(ActivitySourceScope.Global) && best.Scopes.HasFlag(ActivitySourceScope.Global))
                    {
                        return true;
                    }
                    else if (rule.Scopes.HasFlag(ActivitySourceScope.Global) && !best.Scopes.HasFlag(ActivitySourceScope.Global))
                    {
                        return false;
                    }
                }
                else
                {
                    if (!rule.Scopes.HasFlag(ActivitySourceScope.Local) && best.Scopes.HasFlag(ActivitySourceScope.Local))
                    {
                        return true;
                    }
                    else if (rule.Scopes.HasFlag(ActivitySourceScope.Local) && !best.Scopes.HasFlag(ActivitySourceScope.Local))
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
