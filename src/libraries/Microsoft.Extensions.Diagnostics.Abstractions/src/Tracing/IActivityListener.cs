// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;

namespace Microsoft.Extensions.Diagnostics.Tracing
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
        /// Gets the callback invoked when an activity starts.
        /// </summary>
        /// <value>
        /// The callback to invoke when an activity starts.
        /// If <see langword="null"/>, this listener does not receive activity start notifications.
        /// </value>
        Action<Activity>? ActivityStarted { get; }

        /// <summary>
        /// Gets the callback invoked when an activity stops.
        /// </summary>
        /// <value>
        /// The callback to invoke when an activity stops.
        /// If <see langword="null"/>, this listener does not receive activity stop notifications.
        /// </value>
        Action<Activity>? ActivityStopped { get; }

        /// <summary>
        /// Gets the callback invoked when an exception is recorded on an activity.
        /// </summary>
        /// <value>
        /// The callback to invoke when an exception is recorded on an activity.
        /// If <see langword="null"/>, this listener does not receive exception notifications.
        /// </value>
        ExceptionRecorder? ActivityExceptionRecorded { get; }
    }
}
