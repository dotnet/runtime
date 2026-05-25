// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;

namespace Microsoft.Extensions.Diagnostics.Tracing
{
    /// <summary>
    /// Contains a set of parameters used to determine which activities are enabled for which listeners.
    /// An unspecified <see cref="ActivitySourceName"/> matches all activity sources, an unspecified
    /// <see cref="ActivityName"/> matches all activities within the matching sources, and an unspecified
    /// <see cref="ListenerName"/> matches all listeners.
    /// </summary>
    /// <remarks>
    /// <para>The most specific rule that matches a given activity will be used. The priority of parameters is as follows:</para>
    /// <para>- ListenerName, an exact match. See <see cref="IActivityListener.Name"/>.</para>
    /// <para>- ActivitySourceName, either an exact match or a wildcard match using a single <c>*</c>. See <see cref="ActivitySource.Name"/>.</para>
    /// <para>- ActivityName, an exact match. See <see cref="Activity.OperationName"/>.</para>
    /// <para>- Scopes, where a more constrained scope is preferred over <c>Global | Local</c>.</para>
    /// </remarks>
    public class TracingRule
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TracingRule"/> class.
        /// </summary>
        /// <param name="activitySourceName">The <see cref="ActivitySource.Name"/> or pattern with a single <c>*</c> wildcard. A <see langword="null"/> or empty value matches all activity sources.</param>
        /// <param name="activityName">The <see cref="Activity.OperationName"/>, exact match. A <see langword="null"/> or empty value matches all activities within the matching sources.</param>
        /// <param name="listenerName">The <see cref="IActivityListener.Name"/>. A <see langword="null"/> or empty value matches all listeners.</param>
        /// <param name="scopes">A bitwise combination of the enumeration values that specifies the scopes to consider.</param>
        /// <param name="enabled"><see langword="true"/> to enable matched activities for this listener; otherwise, <see langword="false"/>.</param>
        public TracingRule(string? activitySourceName, string? activityName, string? listenerName, ActivitySourceScope scopes, bool enabled)
        {
            ActivitySourceName = activitySourceName;
            ActivityName = activityName;
            ListenerName = listenerName;
            Scopes = scopes == ActivitySourceScope.None
                ? throw new ArgumentOutOfRangeException(nameof(scopes), scopes, "The ActivitySourceScope must be Global, Local, or both.")
                : scopes;
            Enabled = enabled;
        }

        /// <summary>
        /// Gets the <see cref="ActivitySource.Name"/>, either an exact match or a wildcard match using a single <c>*</c>.
        /// </summary>
        /// <value>
        /// The activity source name. If <see langword="null"/> or empty, all activity sources are matched.
        /// </value>
        public string? ActivitySourceName { get; }

        /// <summary>
        /// Gets the <see cref="Activity.OperationName"/>, an exact match.
        /// </summary>
        /// <value>
        /// The activity name. If <see langword="null"/> or empty, all activities within the matching sources are matched.
        /// </value>
        public string? ActivityName { get; }

        /// <summary>
        /// Gets the <see cref="IActivityListener.Name"/>, an exact match.
        /// </summary>
        /// <value>
        /// The listener name. If <see langword="null"/> or empty, all listeners are matched.
        /// </value>
        public string? ListenerName { get; }

        /// <summary>
        /// Gets the <see cref="ActivitySourceScope"/>.
        /// </summary>
        public ActivitySourceScope Scopes { get; }

        /// <summary>
        /// Gets a value that indicates whether matched activities are enabled for this listener.
        /// </summary>
        public bool Enabled { get; }
    }
}
