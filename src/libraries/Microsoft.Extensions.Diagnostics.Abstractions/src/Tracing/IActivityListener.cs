// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;

namespace Microsoft.Extensions.Diagnostics.Configuration
{
    /// <summary>
    /// Receives activity callbacks from the tracing system.
    /// </summary>
    public interface IActivityListener
    {
        /// <summary>
        /// Gets the listener name used by tracing rules.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets the callback used to decide whether to create and sample an activity when a parent ID is available.
        /// </summary>
        /// <value>
        /// The sampling callback to invoke for parent ID-based creation options.
        /// If <see langword="null"/>, this listener does not participate in sampling for parent ID-based creation.
        /// </value>
        SampleActivity<string>? SampleUsingParentId { get; }

        /// <summary>
        /// Gets the callback used to decide whether to create and sample an activity when a parent context is available.
        /// </summary>
        /// <value>
        /// The sampling callback to invoke for parent context-based creation options.
        /// If <see langword="null"/>, this listener does not participate in sampling for parent context-based creation.
        /// </value>
        SampleActivity<ActivityContext>? Sample { get; }

        /// <summary>
        /// Called when an activity starts.
        /// </summary>
        /// <param name="activity">The started activity.</param>
        void ActivityStarted(Activity activity);

        /// <summary>
        /// Called when an activity stops.
        /// </summary>
        /// <param name="activity">The stopped activity.</param>
        void ActivityStopped(Activity activity);

        /// <summary>
        /// Called when an exception is added to an activity.
        /// </summary>
        /// <param name="activity">The activity receiving the exception.</param>
        /// <param name="exception">The exception being recorded.</param>
        /// <param name="tags">Tags associated with the exception.</param>
        void ActivityExceptionRecorded(Activity activity, Exception exception, ref TagList tags);
    }
}
