// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Diagnostics
{
    /// <summary>
    /// ActivitySourceEventOperation define a possible values included inside the <see cref="ActivitySourceEventArgs"/> object when firing
    /// different <see cref="ActivitySource"/> events.
    /// </summary>
    public enum ActivitySourceEventOperation
    {
        /// <summary>
        /// SourceCreated is used when a new <see cref="ActivitySource"/> object is created.
        /// </summary>
        SourceCreated = 0,

        /// <summary>
        /// ActivityStarted is used when a new <see cref="Activity"/> object is created.
        /// </summary>
        ActivityStarted = 1,

        /// <summary>
        /// ActivityStopped is used when dispossing <see cref="Activity"/> object.
        /// </summary>
        ActivityStopped = 2
    }

    /// <summary>
    /// ActivitySourceEventArgs represent the event argument used when notifying any listener
    /// about <see cref="ActivitySource"/> and <see cref="Activity"/> objects creation and disposing.
    /// </summary>
    public sealed class ActivitySourceEventArgs : EventArgs
    {
        internal static readonly ActivitySourceEventArgs s_sourceCreated = new ActivitySourceEventArgs(ActivitySourceEventOperation.SourceCreated);
        internal static readonly ActivitySourceEventArgs s_activityStarted = new ActivitySourceEventArgs(ActivitySourceEventOperation.ActivityStarted);
        internal static readonly ActivitySourceEventArgs s_activityStopped = new ActivitySourceEventArgs(ActivitySourceEventOperation.ActivityStopped);

        /// <summary>
        /// Operation tells about the event operation.
        /// </summary>
        public ActivitySourceEventOperation Operation { get; }

        internal ActivitySourceEventArgs(ActivitySourceEventOperation operation)
        {
            Operation = operation;
        }

        private ActivitySourceEventArgs() { throw new InvalidOperationException(); }
    }
}