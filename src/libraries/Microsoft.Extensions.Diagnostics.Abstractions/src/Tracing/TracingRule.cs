// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;

namespace Microsoft.Extensions.Diagnostics.Configuration
{
    /// <summary>
    /// Contains a set of parameters used to determine which activities are enabled for which listeners.
    /// Unspecified parameters match anything.
    /// </summary>
    /// <remarks>
    /// <para>The most specific rule that matches a given activity will be used. The priority of parameters is as follows:</para>
    /// <para>- ListenerName, an exact match. See <see cref="IActivityListener.Name"/>.</para>
    /// <para>- ActivitySourceName, either an exact match, or the longest prefix match. See <see cref="ActivitySource.Name"/>.</para>
    /// </remarks>
    public class TracingRule
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TracingRule"/> class.
        /// </summary>
        /// <param name="activitySourceName">The <see cref="ActivitySource.Name"/> or prefix.</param>
        /// <param name="listenerName">The <see cref="IActivityListener.Name"/>.</param>
        /// <param name="enabled"><see langword="true"/> to enable matched activities for this listener; otherwise, <see langword="false"/>.</param>
        public TracingRule(string? activitySourceName, string? listenerName, bool enabled)
            : this(activitySourceName, listenerName, ActivitySourceScope.Global | ActivitySourceScope.Local, enabled)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TracingRule"/> class.
        /// </summary>
        /// <param name="activitySourceName">The <see cref="ActivitySource.Name"/> or prefix.</param>
        /// <param name="listenerName">The <see cref="IActivityListener.Name"/>.</param>
        /// <param name="scopes">A bitwise combination of the enumeration values that specifies the scopes to consider.</param>
        /// <param name="enabled"><see langword="true"/> to enable matched activities for this listener; otherwise, <see langword="false"/>.</param>
        public TracingRule(string? activitySourceName, string? listenerName, ActivitySourceScope scopes, bool enabled)
        {
            ActivitySourceName = activitySourceName;
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
        /// The activity source name. If <see langword="null"/>, all activity sources are matched.
        /// </value>
        public string? ActivitySourceName { get; }

        /// <summary>
        /// Gets the <see cref="IActivityListener.Name"/>, an exact match.
        /// </summary>
        /// <value>
        /// The listener name. If <see langword="null"/>, all listeners are matched.
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
