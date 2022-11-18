// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


namespace System.Diagnostics.Tracing
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
    internal static unsafe class TraceLoggingDataCollector
    {
        /// <summary>
        /// Marks the start of a non-blittable array or enumerable.
        /// </summary>
        /// <returns>Bookmark to be passed to EndBufferedArray.</returns>
        public static int BeginBufferedArray()
        {
            return DataCollector.ThreadInstance.BeginBufferedArray();
        }

        /// <summary>
        /// Marks the end of a non-blittable array or enumerable.
        /// </summary>
        /// <param name="bookmark">The value returned by BeginBufferedArray.</param>
        /// <param name="count">The number of items in the array.</param>
        public static void EndBufferedArray(int bookmark, int count)
        {
            DataCollector.ThreadInstance.EndBufferedArray(bookmark, count);
        }

        public static void AddScalar(PropertyValue value)
        {
            PropertyValue.Scalar scalar = value.ScalarValue;
            DataCollector.ThreadInstance.AddScalar(&scalar, value.ScalarLength);
        }

        /// <summary>
        /// Adds an Int64 value to the event payload.
        /// </summary>
        /// <param name="value">Value to be added.</param>
        public static void AddScalar(long value)
        {
            DataCollector.ThreadInstance.AddScalar(&value, sizeof(long));
        }

        /// <summary>
        /// Adds a Double value to the event payload.
        /// </summary>
        /// <param name="value">Value to be added.</param>
        public static void AddScalar(double value)
        {
            DataCollector.ThreadInstance.AddScalar(&value, sizeof(double));
        }

        /// <summary>
        /// Adds a Boolean value to the event payload.
        /// </summary>
        /// <param name="value">Value to be added.</param>
        public static void AddScalar(bool value)
        {
            DataCollector.ThreadInstance.AddScalar(&value, sizeof(bool));
        }

        /// <summary>
        /// Adds a null-terminated String value to the event payload.
        /// </summary>
        /// <param name="value">
        /// Value to be added. A null value is treated as a zero-length string.
        /// </param>
        public static void AddNullTerminatedString(string? value)
        {
            DataCollector.ThreadInstance.AddNullTerminatedString(value);
        }

        /// <summary>
        /// Adds a counted String value to the event payload.
        /// </summary>
        /// <param name="value">
        /// Value to be added. A null value is treated as a zero-length string.
        /// </param>
        public static void AddBinary(string? value)
        {
            DataCollector.ThreadInstance.AddBinary(value, value == null ? 0 : value.Length * 2);
        }

        public static void AddArray(PropertyValue value, int elementSize)
        {
            Array? array = (Array?)value.ReferenceValue;
            DataCollector.ThreadInstance.AddArray(array, array == null ? 0 : array.Length, elementSize);
        }
    }
}
