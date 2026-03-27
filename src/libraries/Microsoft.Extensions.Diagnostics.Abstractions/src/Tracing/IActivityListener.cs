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
        string? Name { get; }

        /// <summary>
        /// Decides whether to create and sample an activity when a parent ID is available.
        /// </summary>
        /// <param name="options">The activity creation options.</param>
        /// <returns>The sampling decision.</returns>
        ActivitySamplingResult SampleUsingParentId(ref ActivityCreationOptions<string> options);

        /// <summary>
        /// Decides whether to create and sample an activity when a parent context is available.
        /// </summary>
        /// <param name="options">The activity creation options.</param>
        /// <returns>The sampling decision.</returns>
        ActivitySamplingResult Sample(ref ActivityCreationOptions<ActivityContext> options);

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

        /// <summary>
        /// Called when the listener is detached from an activity source, either because the source is being disposed or the listener is being removed.
        /// </summary>
        /// <param name="activitySource">The activity source from which the listener is detached.</param>
        void ListenerDetached(ActivitySource activitySource);
    }
}
