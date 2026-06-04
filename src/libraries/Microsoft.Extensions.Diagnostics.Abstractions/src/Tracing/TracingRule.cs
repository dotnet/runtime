// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;

namespace Microsoft.Extensions.Diagnostics.Tracing
{
    /// <summary>
    /// Contains a set of parameters used to determine which activities are enabled for which listeners.
    /// An unspecified <see cref="SourceName"/> matches all activity sources, an unspecified
    /// <see cref="OperationName"/> matches all activities within the matching sources, and an unspecified
    /// <see cref="ListenerName"/> matches all listeners.
    /// </summary>
    /// <remarks>
    /// <para>The most specific rule that matches a given activity will be used. The priority of parameters is as follows:</para>
    /// <para>- ListenerName, an exact match. See <see cref="ActivityListener.Name"/>.</para>
    /// <para>- SourceName, either an exact match, the longest prefix match, or a wildcard pattern using a single <c>*</c>. See <see cref="ActivitySource.Name"/>.</para>
    /// <para>- OperationName, an exact match. See <see cref="Activity.OperationName"/>.</para>
    /// <para>- Scopes, where a more constrained scope is preferred over <c>Global | Local</c>.</para>
    /// </remarks>
    public class TracingRule
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TracingRule"/> class.
        /// </summary>
        /// <param name="sourceName">The <see cref="ActivitySource.Name"/>, prefix, or pattern with a single <c>*</c> wildcard. A <see langword="null"/> or empty value matches all activity sources.</param>
        /// <param name="operationName">The <see cref="Activity.OperationName"/>, exact match. A <see langword="null"/> or empty value matches all activities within the matching sources.</param>
        /// <param name="listenerName">The <see cref="ActivityListener.Name"/>. A <see langword="null"/> or empty value matches all listeners.</param>
        /// <param name="scopes">A bitwise combination of the enumeration values that specifies the scopes to consider.</param>
        /// <param name="enable"><see langword="true"/> to enable matched activities for this listener; otherwise, <see langword="false"/>.</param>
        /// <exception cref="ArgumentException"><paramref name="sourceName"/> contains more than one <c>*</c> wildcard.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="scopes"/> is <see cref="ActivitySourceScopes.None"/>.</exception>
        public TracingRule(string? sourceName, string? operationName, string? listenerName, ActivitySourceScopes scopes, bool enable)
        {
            // Validate the wildcard pattern eagerly. The equivalent rule type in Microsoft.Extensions.Diagnostics
            // for metrics defers this validation to the per-event matching path, so a malformed rule introduced
            // via IOptionsMonitor reload throws out of arbitrary instrument operations later. We diverge from
            // that here so configuration mistakes surface at bind time (or programmatic-construction call site)
            // and never reach the StartActivity hot path. The metrics behaviour is shipped public surface and
            // can't change without a breaking-change process; tracing is new, so we get the cleaner shape now.
            if (!string.IsNullOrEmpty(sourceName))
            {
                int firstWildcard = sourceName.IndexOf('*');
                if (firstWildcard >= 0 && sourceName.IndexOf('*', firstWildcard + 1) >= 0)
                {
                    throw new ArgumentException("Only one '*' wildcard is allowed in an activity source name pattern.", nameof(sourceName));
                }
            }

            SourceName = sourceName;
            OperationName = operationName;
            ListenerName = listenerName;
            Scopes = scopes == ActivitySourceScopes.None
                ? throw new ArgumentOutOfRangeException(nameof(scopes), scopes, "The ActivitySourceScopes must be Global, Local, or both.")
                : scopes;
            Enable = enable;
        }

        /// <summary>
        /// Gets the <see cref="ActivitySource.Name"/>, either an exact match, the longest prefix match, or a wildcard pattern using a single <c>*</c>.
        /// </summary>
        /// <value>
        /// The activity source name. If <see langword="null"/> or empty, all activity sources are matched.
        /// </value>
        public string? SourceName { get; }

        /// <summary>
        /// Gets the <see cref="Activity.OperationName"/>, an exact match.
        /// </summary>
        /// <value>
        /// The operation name. If <see langword="null"/> or empty, all activities within the matching sources are matched.
        /// </value>
        public string? OperationName { get; }

        /// <summary>
        /// Gets the <see cref="ActivityListener.Name"/>, an exact match.
        /// </summary>
        /// <value>
        /// The listener name. If <see langword="null"/> or empty, all listeners are matched.
        /// </value>
        public string? ListenerName { get; }

        /// <summary>
        /// Gets the <see cref="ActivitySourceScopes"/>.
        /// </summary>
        public ActivitySourceScopes Scopes { get; }

        /// <summary>
        /// Gets a value that indicates whether matched activities are enabled for this listener.
        /// </summary>
        public bool Enable { get; }
    }
}
