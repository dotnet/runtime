// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Diagnostics.Configuration
{
    internal sealed class DefaultActivitySourceFactory : IActivitySourceFactory
    {
        private readonly Dictionary<string, List<FactoryActivitySource>> _cachedSources = [];
        private readonly List<ActivityListenerRegistration> _listenerRegistrations;
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
                throw new InvalidOperationException(SR.InvalidScope);
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
                FactoryActivitySource activitySource = new FactoryActivitySource(options);
                options.Scope = scope;

                sourceList.Add(activitySource);
                return activitySource;
            }
        }

        private void UpdateRules(TracingOptions options)
        {
            lock (_cachedSources)
            {
                if (_disposed)
                {
                    return;
                }

                foreach (ActivityListenerRegistration registration in _listenerRegistrations)
                {
                    registration.UpdateRules(options.Rules);
                }
            }
        }

        public void Dispose()
        {
            List<ActivityListenerRegistration> listenerRegistrations;
            List<FactoryActivitySource> sources;

            lock (_cachedSources)
            {
                if (_disposed)
                {
                    return;
                }

                _disposed = true;
                _changeTokenRegistration?.Dispose();
                listenerRegistrations = [.. _listenerRegistrations];
                _listenerRegistrations.Clear();

                sources = [.. _cachedSources.Values.SelectMany(static sourceList => sourceList)];
                _cachedSources.Clear();
            }

            foreach (ActivityListenerRegistration registration in listenerRegistrations)
            {
                registration.Dispose();
            }

            foreach (FactoryActivitySource source in sources)
            {
                source.Release();
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
                // no-op, disallow users from disposing of the meters created from the factory.
            }
        }

        private sealed class ActivityListenerRegistration : IDisposable
        {
            private readonly IActivityListener _listener;
            private readonly DefaultActivitySourceFactory _activitySourceFactory;
            private readonly object _lock = new();
            private readonly ActivityListener _activityListener;
            private IList<TracingRule> _rules = Array.Empty<TracingRule>();
            private bool _disposed;

            public ActivityListenerRegistration(IActivityListener listener, DefaultActivitySourceFactory activitySourceFactory, TracingOptions options)
            {
                _listener = listener ?? throw new ArgumentNullException(nameof(listener));
                _activitySourceFactory = activitySourceFactory ?? throw new ArgumentNullException(nameof(activitySourceFactory));
                _activityListener = new ActivityListener(_listener.Name)
                {
                    ShouldListenTo = ShouldListenTo,
                    SampleUsingParentId = listener.SampleUsingParentId,
                    Sample = listener.Sample,
                    ActivityStarted = listener.ActivityStarted,
                    ActivityStopped = listener.ActivityStopped,
                    ExceptionRecorder = listener.ActivityExceptionRecorded,
                };
                _rules = options.Rules;
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
                    ActivitySource.UpdateActivityListener(_activityListener);
                }
            }

            private bool ShouldListenTo(ActivitySource activitySource)
            {
                return IsEnabled(activitySource) && IsEnabled(activitySource, _listener.Name ?? string.Empty);
            }

            private bool IsEnabled(ActivitySource activitySource, string listenerName = "")
            {
                TracingRule? rule = GetMostSpecificRule(activitySource.Name, listenerName, ReferenceEquals(_activitySourceFactory, activitySource.Scope));
                return rule?.Enabled ?? false;
            }

            private TracingRule? GetMostSpecificRule(string activitySourceName, string listenerName, bool isLocalScope)
            {
                TracingRule? best = null;
                IList<TracingRule> rules = Volatile.Read(ref _rules);
                foreach (TracingRule rule in rules)
                {
                    if (RuleMatches(rule, activitySourceName, listenerName, isLocalScope)
                        && IsMoreSpecific(rule, best, isLocalScope))
                    {
                        best = rule;
                    }
                }

                return best;
            }

            private static bool RuleMatches(TracingRule rule, string activitySourceName, string listenerName, bool isLocalScope)
            {
                if (!string.Equals(rule.ListenerName ?? string.Empty, listenerName, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                if (!rule.Scopes.HasFlag(isLocalScope ? ActivitySourceScope.Local : ActivitySourceScope.Global))
                {
                    return false;
                }

                return Matches(rule.ActivitySourceName, activitySourceName);
            }

            private static bool IsMoreSpecific(TracingRule rule, TracingRule? best, bool isLocalScope)
            {
                if (best is null)
                {
                    return true;
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
                    throw new InvalidOperationException(SR.MoreThanOneWildcard);
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
