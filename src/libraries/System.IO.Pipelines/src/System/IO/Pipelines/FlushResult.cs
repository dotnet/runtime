// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.IO.Pipelines
{
    /// <summary>
    /// Result returned by <see cref="PipeWriter.FlushAsync"/> call
    /// </summary>
    public struct FlushResult
    {
        internal ResultFlags ResultFlags;

        /// <summary>
        /// Creates a new instance of <see cref="FlushResult"/> setting <see cref="IsCanceled"/> and <see cref="IsCompleted"/> flags
        /// </summary>
        public FlushResult(bool isCanceled, bool isCompleted)
        {
            ResultFlags = ResultFlags.None;

            if (isCanceled)
            {
                ResultFlags |= ResultFlags.Canceled;
            }

            if (isCompleted)
            {
                ResultFlags |= ResultFlags.Completed;
            }
        }

        /// <summary>
        /// True if the current <see cref="PipeWriter.FlushAsync"/> operation was canceled, otherwise false.
        /// </summary>
        public bool IsCanceled => (ResultFlags & ResultFlags.Canceled) != 0;

        /// <summary>
        /// True if the <see cref="PipeWriter"/> is complete otherwise false
        /// </summary>
        public bool IsCompleted => (ResultFlags & ResultFlags.Completed) != 0;
    }
}
