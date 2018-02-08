// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Buffers;

namespace System.IO.Pipelines
{
    /// <summary>
    /// The result of a <see cref="PipeReader.ReadAsync"/> call.
    /// </summary>
    public struct ReadResult
    {
        internal ReadOnlySequence<byte> ResultBuffer;
        internal ResultFlags ResultFlags;

        /// <summary>
        /// Creates a new instance of <see cref="ReadResult"/> setting <see cref="IsCanceled"/> and <see cref="IsCompleted"/> flags
        /// </summary>
        public ReadResult(ReadOnlySequence<byte> buffer, bool isCanceled, bool isCompleted)
        {
            ResultBuffer = buffer;
            ResultFlags = ResultFlags.None;

            if (isCompleted)
            {
                ResultFlags |= ResultFlags.Completed;
            }
            if (isCanceled)
            {
                ResultFlags |= ResultFlags.Canceled;
            }
        }

        /// <summary>
        /// The <see cref="ReadOnlyBuffer{Byte}"/> that was read
        /// </summary>
        public ReadOnlySequence<byte> Buffer => ResultBuffer;

        /// <summary>
        /// True if the current <see cref="PipeReader.ReadAsync"/> operation was canceled, otherwise false.
        /// </summary>
        public bool IsCanceled => (ResultFlags & ResultFlags.Canceled) != 0;

        /// <summary>
        /// True if the <see cref="PipeReader"/> is complete
        /// </summary>
        public bool IsCompleted => (ResultFlags & ResultFlags.Completed) != 0;
    }
}
