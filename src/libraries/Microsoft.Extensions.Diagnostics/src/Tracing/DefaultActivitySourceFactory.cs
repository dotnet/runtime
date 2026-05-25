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
            private IList<TracingRule> _rules = Array.Empty<TracingRule>();
            private bool _hasActivityNameRules;
            private bool _disposed;

            public ActivityListenerRegistration(IActivityListener listener, DefaultActivitySourceFactory activitySourceFactory, TracingOptions options)
            {
                _listener = listener ?? throw new ArgumentNullException(nameof(listener));
                _activitySourceFactory = activitySourceFactory ?? throw new ArgumentNullException(nameof(activitySourceFactory));
                _activityListener = new ActivityListener()
                {
                    ShouldListenTo = ShouldListenTo,
                    ActivityStarted = listener.ActivityStarted,
                    ActivityStopped = listener.ActivityStopped,
                    ExceptionRecorder = listener.ActivityExceptionRecorded,
                };
                _rules = options.Rules;
                _hasActivityNameRules = ComputeHasActivityNameRules(_rules);
                ConfigureSampleDelegates();
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
                    if (hadActivityNameRules != _hasActivityNameRules)
                    {
                        ConfigureSampleDelegates();
                    }

                    _activityListener.RefreshSources();
                }
            }

            private void ConfigureSampleDelegates()
            {
                if (_hasActivityNameRules)
                {
                    _activityListener.Sample = WrappedSample;
                    _activityListener.SampleUsingParentId = WrappedSampleUsingParentId;
                }
                else
                {
                    _activityListener.Sample = _listener.Sample;
                    _activityListener.SampleUsingParentId = _listener.SampleUsingParentId;
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
                if (!IsActivityEnabled(options.Source, options.Name))
                {
                    return ActivitySamplingResult.None;
                }

                SampleActivity<ActivityContext>? userSample = _listener.Sample;
                return userSample is null ? ActivitySamplingResult.AllData : userSample(ref options);
            }

            private ActivitySamplingResult WrappedSampleUsingParentId(ref ActivityCreationOptions<string> options)
            {
                if (!IsActivityEnabled(options.Source, options.Name))
                {
                    return ActivitySamplingResult.None;
                }

                SampleActivity<string>? userSample = _listener.SampleUsingParentId;
                return userSample is null ? ActivitySamplingResult.AllData : userSample(ref options);
            }

            private bool IsActivityEnabled(ActivitySource source, string activityName)
            {
                bool isLocalScope = ReferenceEquals(_activitySourceFactory, source.Scope);
                TracingRule? rule = GetMostSpecificRule(source.Name, activityName, _listener.Name, isLocalScope, considerActivityName: true);
                return rule?.Enabled ?? false;
            }

            private bool ShouldListenTo(ActivitySource activitySource)
            {
                bool isLocalScope = ReferenceEquals(_activitySourceFactory, activitySource.Scope);
                IList<TracingRule> rules = Volatile.Read(ref _rules);

                TracingRule? bestSourceLevel = null;
                bool anyActivityScopedEnable = false;

                foreach (TracingRule rule in rules)
                {
                    if (!RuleMatches(rule, activitySource.Name, _listener.Name, isLocalScope, considerActivityName: false))
                    {
                        continue;
                    }

                    if (string.IsNullOrEmpty(rule.ActivityName))
                    {
                        if (IsMoreSpecific(rule, bestSourceLevel, isLocalScope, considerActivityName: false))
                        {
                            bestSourceLevel = rule;
                        }
                    }
                    else if (rule.Enabled)
                    {
                        anyActivityScopedEnable = true;
                    }
                }

                return (bestSourceLevel?.Enabled ?? false) || anyActivityScopedEnable;
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
        }
    }
}
