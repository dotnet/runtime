// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Buffers
{
    /// <summary>
    /// This enum defines the various potential status that can be returned from Span-based operations
    /// that support processing of input contained in multiple discontiguous buffers.
    /// </summary>
    public enum OperationStatus
    {
        /// <summary>
        /// The entire input buffer has been processed and the operation is complete.
        /// </summary>
        Done,
        /// <summary>
        /// The input is partially processed, up to what could fit into the destination buffer.
        /// The caller can enlarge the destination buffer, slice the buffers appropriately, and retry.
        /// </summary>
        DestinationTooSmall,
        /// <summary>
        /// The input is partially processed, up to the last valid chunk of the input that could be consumed.
        /// The caller can stitch the remaining unprocessed input with more data, slice the buffers appropriately, and retry.
        /// </summary>
        NeedMoreData,
        /// <summary>
        /// The input contained invalid bytes which could not be processed. If the input is partially processed,
        /// the destination contains the partial result. This guarantees that no additional data appended to the input
        /// will make the invalid sequence valid.
        /// </summary>
        InvalidData,
    }
}
