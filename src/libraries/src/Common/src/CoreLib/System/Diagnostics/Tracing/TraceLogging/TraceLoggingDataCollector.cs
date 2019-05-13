// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Security;

#if ES_BUILD_STANDALONE
namespace Microsoft.Diagnostics.Tracing
#else
namespace System.Diagnostics.Tracing
#endif
{
    /// <summary>
    /// TraceLogging: Used when implementing a custom TraceLoggingTypeInfo.
    /// The instance of this type is provided to the TypeInfo.WriteData method.
    /// All operations are forwarded to the current thread's DataCollector.
    /// Note that this abstraction would allow us to expose the custom
    /// serialization system to partially-trusted code. If we end up not
    /// making custom serialization public, or if we only expose it to
    /// full-trust code, this abstraction is unnecessary (though it probably
    /// doesn't hurt anything).
    /// </summary>
    internal unsafe class TraceLoggingDataCollector
    {
        internal static readonly TraceLoggingDataCollector Instance = new TraceLoggingDataCollector();

        private TraceLoggingDataCollector()
        {
            return;
        }

        /// <summary>
        /// Marks the start of a non-blittable array or enumerable.
        /// </summary>
        /// <returns>Bookmark to be passed to EndBufferedArray.</returns>
        public int BeginBufferedArray()
        {
            return DataCollector.ThreadInstance.BeginBufferedArray();
        }

        /// <summary>
        /// Marks the end of a non-blittable array or enumerable.
        /// </summary>
        /// <param name="bookmark">The value returned by BeginBufferedArray.</param>
        /// <param name="count">The number of items in the array.</param>
        public void EndBufferedArray(int bookmark, int count)
        {
            DataCollector.ThreadInstance.EndBufferedArray(bookmark, count);
        }

        /// <summary>
        /// Adds the start of a group to the event.
        /// This has no effect on the event payload, but is provided to allow
        /// WriteMetadata and WriteData implementations to have similar
        /// sequences of calls, allowing for easier verification of correctness.
        /// </summary>
        public TraceLoggingDataCollector AddGroup()
        {
            return this;
        }

        public void AddScalar(PropertyValue value)
        {
            var scalar = value.ScalarValue;
            DataCollector.ThreadInstance.AddScalar(&scalar, value.ScalarLength);
        }

        /// <summary>
        /// Adds an Int64 value to the event payload.
        /// </summary>
        /// <param name="value">Value to be added.</param>
        public void AddScalar(long value)
        {
            DataCollector.ThreadInstance.AddScalar(&value, sizeof(long));
        }

        /// <summary>
        /// Adds a Double value to the event payload.
        /// </summary>
        /// <param name="value">Value to be added.</param>
        public void AddScalar(double value)
        {
            DataCollector.ThreadInstance.AddScalar(&value, sizeof(double));
        }

        /// <summary>
        /// Adds a Boolean value to the event payload.
        /// </summary>
        /// <param name="value">Value to be added.</param>
        public void AddScalar(bool value)
        {
            DataCollector.ThreadInstance.AddScalar(&value, sizeof(bool));
        }

        /// <summary>
        /// Adds a null-terminated String value to the event payload.
        /// </summary>
        /// <param name="value">
        /// Value to be added. A null value is treated as a zero-length string.
        /// </param>
        public void AddNullTerminatedString(string? value)
        {
            DataCollector.ThreadInstance.AddNullTerminatedString(value);
        }

        /// <summary>
        /// Adds a counted String value to the event payload.
        /// </summary>
        /// <param name="value">
        /// Value to be added. A null value is treated as a zero-length string.
        /// </param>
        public void AddBinary(string? value)
        {
            DataCollector.ThreadInstance.AddBinary(value, value == null ? 0 : value.Length * 2);
        }

        public void AddArray(PropertyValue value, int elementSize)
        {
            Array? array = (Array?)value.ReferenceValue;
            DataCollector.ThreadInstance.AddArray(array, array == null ? 0 : array.Length, elementSize);
        }
    }
}
