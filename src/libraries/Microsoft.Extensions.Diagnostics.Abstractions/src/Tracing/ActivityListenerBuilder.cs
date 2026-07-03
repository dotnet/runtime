// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;

namespace Microsoft.Extensions.Diagnostics.Tracing
{
    /// <summary>
    /// Describes the user-configurable surface of an <see cref="ActivityListener"/> registered with an
    /// <see cref="ITracingBuilder"/>. The tracing infrastructure consumes the values set on the builder and
    /// constructs the underlying <see cref="ActivityListener"/>; subscription to <see cref="ActivitySource"/>
    /// instances is controlled entirely by the configuration-driven <see cref="TracingRule"/> set.
    /// </summary>
    /// <remarks>
    /// The builder intentionally does not expose <c>ShouldListenTo</c>, <see cref="ActivityListener.RefreshSources"/>
    /// or <see cref="IDisposable.Dispose"/>: subscription filtering is owned by the rule set, and the lifetime of the
    /// underlying listener is owned by the tracing builder. The delegate properties are snapshotted by the
    /// infrastructure at registration time; mutating the builder afterwards has no effect on the registered listener.
    /// Instances are constructed by the tracing infrastructure when an <c>AddListener</c> overload runs; user code
    /// configures an instance through the <c>configure</c> callback passed to <c>AddListener</c>.
    /// </remarks>
    public sealed class ActivityListenerBuilder
    {
        internal ActivityListenerBuilder(string name)
        {
            Debug.Assert(name is not null);
            Name = name;
        }

        /// <summary>
        /// Gets the name used by configuration-based filtering to target rules at this listener.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets or sets the callback invoked when an <see cref="Activity"/> is sampled from an <see cref="ActivityContext"/>.
        /// </summary>
        public SampleActivity<ActivityContext>? Sample { get; set; }

        /// <summary>
        /// Gets or sets the callback invoked when an <see cref="Activity"/> is sampled from a parent identifier string.
        /// </summary>
        public SampleActivity<string>? SampleUsingParentId { get; set; }

        /// <summary>
        /// Gets or sets the callback invoked when a sampled <see cref="Activity"/> starts.
        /// </summary>
        public Action<Activity>? ActivityStarted { get; set; }

        /// <summary>
        /// Gets or sets the callback invoked when a sampled <see cref="Activity"/> stops.
        /// </summary>
        public Action<Activity>? ActivityStopped { get; set; }

        /// <summary>
        /// Gets or sets the callback invoked when an exception is recorded on a sampled <see cref="Activity"/>.
        /// </summary>
        public ExceptionRecorder? ExceptionRecorder { get; set; }
    }
}
