// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Diagnostics.Eventing.Reader
{
    /// <summary>
    /// The custom event handler args.
    /// </summary>
    public sealed class EventRecordWrittenEventArgs : EventArgs
    {
        internal EventRecordWrittenEventArgs(EventLogRecord record) { EventRecord = record; }
        internal EventRecordWrittenEventArgs(Exception exception) { EventException = exception; }

        /// <summary>
        /// The EventRecord being notified.
        /// NOTE: If non null, then caller is required to call Dispose().
        /// </summary>
        public EventRecord EventRecord { get; }

        /// <summary>
        /// If any error occurred during subscription, this will be non-null.
        /// After a notification containing an exception, no more notifications will
        /// be made for this subscription.
        /// </summary>
        public Exception EventException { get; }
    }

}
