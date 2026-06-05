// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.ExceptionServices;
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

            // Each ActivityListenerRegistration ctor attaches a wrapper ActivityListener globally
            // (ActivitySource.AddActivityListener). If a later registration throws, the wrappers
            // already attached would leak in s_allListeners/s_activeSources for the process lifetime
            // because the partially-constructed factory never sees Dispose(). Materialise into a
            // local list and dispose what we built before rethrowing.
            TracingOptions initial = options.CurrentValue;
            List<ActivityListenerRegistration> registrations = new();
            try
            {
                foreach (ActivityListener listener in listeners)
                {
                    registrations.Add(new ActivityListenerRegistration(listener, this, initial));
                }

                _listenerRegistrations = registrations.ToArray();
                // TracingOptions is a public type so other code may register named buckets via
                // services.Configure<TracingOptions>("foo", ...). We only own the default bucket,
                // so filter notifications to it. The standard OptionsMonitor pipeline normalises
                // null -> Options.DefaultName ("") before invoking the listener, but the delegate
                // signature permits null, so IsNullOrEmpty is the defensive form that covers both.
                _changeTokenRegistration = options.OnChange((opts, name) =>
                {
                    if (string.IsNullOrEmpty(name))
                    {
                        UpdateRules(opts);
                    }
                });

                // Reconcile: a configuration reload could have fired between the CurrentValue read
                // used to bootstrap the registrations above and the OnChange subscription. Such a
                // reload would not be delivered via the callback (we had no subscription yet) and
                // would be permanently lost without this re-read. Re-applying the latest snapshot
                // covers that window; if no reload happened, OptionsMonitor returns the same
                // instance and the ReferenceEquals guard skips the redundant work. The standard
                // sibling MetricsSubscriptionManager achieves the same property by subscribing
                // first and then calling UpdateRules(CurrentValue) unconditionally.
                TracingOptions current = options.CurrentValue;
                if (!ReferenceEquals(current, initial))
                {
                    UpdateRules(current);
                }
            }
            catch
            {
                _changeTokenRegistration?.Dispose();

                foreach (ActivityListenerRegistration registration in registrations)
                {
                    try
                    {
                        registration.Dispose();
                    }
                    catch
                    {
                        // Suppress secondary failures during cleanup so the original construction
                        // exception is the one observed by the caller.
                    }
                }

                throw;
            }
        }

        protected override ActivitySource CreateCore(ActivitySourceOptions options)
        {
            Debug.Assert(options is not null);
            Debug.Assert(options.Name is not null);
            Debug.Assert(ReferenceEquals(options.Scope, this));

            // Phase 1: lookup under the cache lock. No user code runs here.
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

            // Phase 2: construct outside the cache lock. The base ActivitySource constructor
            // walks ActivitySource.s_allListeners and synchronously invokes each listener's
            // ShouldListenTo predicate, which forwards to user-supplied delegates. Holding
            // _cachedSources across user code would create a lock-order inversion against any
            // user lock taken before calling Create.
            FactoryActivitySource newSource = new FactoryActivitySource(options);

            // Phase 3: re-acquire the cache lock and commit, handling the rare race where a
            // concurrent Create with the same identity won, and a concurrent factory dispose.
            lock (_cachedSources)
            {
                if (_disposed)
                {
                    newSource.Release();
                    throw new ObjectDisposedException(nameof(DefaultActivitySourceFactory));
                }

                if (TryGetCachedMatch(options, out ActivitySource? winner))
                {
                    // Lost the race to another concurrent Create call. Discard our redundant
                    // instance; it was never published to any caller and Release tears down its
                    // BCL bookkeeping (s_activeSources entry, attached listeners).
                    newSource.Release();
                    return winner;
                }

                if (!_cachedSources.TryGetValue(options.Name, out List<FactoryActivitySource>? sourceList))
                {
                    sourceList = new List<FactoryActivitySource>();
                    _cachedSources.Add(options.Name, sourceList);
                }

                sourceList.Add(newSource);
                return newSource;
            }
        }

        private bool TryGetCachedMatch(ActivitySourceOptions options, [NotNullWhen(true)] out ActivitySource? match)
        {
            Debug.Assert(Monitor.IsEntered(_cachedSources));

            if (_cachedSources.TryGetValue(options.Name, out List<FactoryActivitySource>? sourceList))
            {
                foreach (FactoryActivitySource source in sourceList)
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

        private void UpdateRules(TracingOptions options)
        {
            if (Volatile.Read(ref _disposed))
            {
                return;
            }

            List<TracingRule> rules = options.Rules;
            List<Exception>? errors = null;

            foreach (ActivityListenerRegistration registration in _listenerRegistrations)
            {
                try
                {
                    registration.UpdateRules(rules);
                }
                catch (Exception ex)
                {
                    (errors ??= new List<Exception>()).Add(ex);
                }
            }

            if (errors is null)
            {
                return;
            }

            if (errors.Count == 1)
            {
                ExceptionDispatchInfo.Capture(errors[0]).Throw();
            }

            throw new AggregateException(SR.DefaultActivitySourceFactory_UpdateRules_RegistrationThrew, errors);
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
            private readonly string? _listenerName;
            private readonly DefaultActivitySourceFactory _activitySourceFactory;
            private readonly object _lock = new();
            private readonly ActivityListener _userListener;
            private readonly ActivityListener _activityListener;
            private ListenerState _state;
            private bool _disposed;

            public ActivityListenerRegistration(ActivityListener listener, DefaultActivitySourceFactory activitySourceFactory, TracingOptions options)
            {
                ArgumentNullException.ThrowIfNull(listener);
                _userListener = listener;
                _activitySourceFactory = activitySourceFactory ?? throw new ArgumentNullException(nameof(activitySourceFactory));
                _listenerName = listener.Name;
                _state = ListenerState.Create(options.Rules);
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

                // Dispose the user-supplied listener so that, if the caller kept a reference, any
                // RefreshSources() they invoke on it short-circuits on the IsDisposed check and
                // does not register their raw listener in s_allListeners in parallel with our
                // wrapper. We never attached this listener via AddActivityListener, so Dispose
                // has no other effect: the delegate properties (Sample, ActivityStarted, ...)
                // remain readable, which is what our wrappers rely on for live lookup.
                listener.Dispose();
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

            public void UpdateRules(List<TracingRule> rules)
            {
                ArgumentNullException.ThrowIfNull(rules);

                lock (_lock)
                {
                    if (_disposed)
                    {
                        return;
                    }

                    // Single atomic publication of rules + fresh per-source cache. Readers either
                    // see the old snapshot in full or the new one in full.
                    Volatile.Write(ref _state, ListenerState.Create(rules));
                }

                // RefreshSources walks ActivitySource.s_activeSources and synchronously invokes the
                // wrapper's ShouldListenTo (which forwards to the user-supplied predicate) for every
                // active source. Calling it under _lock would expose us to the same lock-order
                // inversion that CreateCore avoids: a caller whose ShouldListenTo grabs a user lock
                // could deadlock against another thread that holds that lock and triggers a reload.
                // The call is safe to make without _lock: RefreshSources short-circuits once the
                // listener is disposed, and concurrent UpdateRules calls converge because every
                // wrapper reads _state via Volatile.Read on each invocation.
                _activityListener.RefreshSources();
            }

            private ActivitySamplingResult WrappedSample(ref ActivityCreationOptions<ActivityContext> options)
            {
                ListenerState state = Volatile.Read(ref _state);
                if (state.HasOperationNameRules && !IsEnabledFast(state, options.Source, options.Name))
                {
                    return ActivitySamplingResult.None;
                }

                return _userListener.Sample?.Invoke(ref options) ?? ActivitySamplingResult.None;
            }

            private ActivitySamplingResult WrappedSampleUsingParentId(ref ActivityCreationOptions<string> options)
            {
                ListenerState state = Volatile.Read(ref _state);
                if (state.HasOperationNameRules && !IsEnabledFast(state, options.Source, options.Name))
                {
                    return ActivitySamplingResult.None;
                }

                return _userListener.SampleUsingParentId?.Invoke(ref options) ?? ActivitySamplingResult.None;
            }

            private void WrappedActivityStarted(Activity activity)
            {
                ListenerState state = Volatile.Read(ref _state);
                if (!state.HasOperationNameRules || IsEnabledFast(state, activity.Source, activity.OperationName))
                {
                    _userListener.ActivityStarted?.Invoke(activity);
                }
            }

            private void WrappedActivityStopped(Activity activity)
            {
                ListenerState state = Volatile.Read(ref _state);
                if (!state.HasOperationNameRules || IsEnabledFast(state, activity.Source, activity.OperationName))
                {
                    _userListener.ActivityStopped?.Invoke(activity);
                }
            }

            private void WrappedExceptionRecorder(Activity activity, Exception exception, ref TagList tags)
            {
                ListenerState state = Volatile.Read(ref _state);
                if (!state.HasOperationNameRules || IsEnabledFast(state, activity.Source, activity.OperationName))
                {
                    _userListener.ExceptionRecorder?.Invoke(activity, exception, ref tags);
                }
            }

            private bool IsEnabledFast(ListenerState state, ActivitySource source, string operationName)
            {
                (string Name, bool IsLocalScope) key = (source.Name, ReferenceEquals(_activitySourceFactory, source.Scope));
                if (!state.SourceFilterStates.TryGetValue(key, out SourceFilterState filter))
                {
                    // Cache miss is rare (race against UpdateRules clearing the dictionary).
                    // Compute on the fly without caching; the next ShouldListenTo for this source repopulates.
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

                bool rulesAllow = filter.DefaultEnabled || filter.Divergent is { Count: > 0 };
                if (!rulesAllow)
                {
                    return false;
                }

                return _userListener.ShouldListenTo?.Invoke(activitySource) ?? true;
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
                // instance was disposed) share the cached filter state. Name and Scope are both
                // immutable once an ActivitySource is constructed, so the key is stable.
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
