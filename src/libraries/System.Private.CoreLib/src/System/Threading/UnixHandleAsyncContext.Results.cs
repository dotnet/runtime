// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Threading
{
    public sealed partial class UnixHandleAsyncContext
    {
        /// <summary>
        /// Describes the result of an asynchronous I/O operation.
        /// </summary>
        public enum AsyncResult
        {
            /// <summary>The operation is pending.</summary>
            Pending = 0,

            /// <summary>The operation completed successfully.</summary>
            Completed = 1,
            /// <summary>The operation was aborted.</summary>
            Aborted = 2,
        }

        /// <summary>
        /// Describes the result of a completed asynchronous I/O operation.
        /// </summary>
        public enum OnCompletedResult
        {
            /// <summary>The operation completed successfully.</summary>
            Completed = 1,
            /// <summary>The operation was aborted (handle closed).</summary>
            Aborted = 2,

            /// <summary>The operation was canceled (CancellationToken triggered).</summary>
            Canceled = 3,
        }

        /// <summary>
        /// Describes the result of a synchronous I/O operation.
        /// </summary>
        public enum SyncResult
        {
            /// <summary>The operation completed successfully.</summary>
            Completed = 1,
            /// <summary>The operation was aborted (handle closed).</summary>
            Aborted = 2,

            /// <summary>The operation timed out.</summary>
            TimedOut = 4,
        }
    }
}
