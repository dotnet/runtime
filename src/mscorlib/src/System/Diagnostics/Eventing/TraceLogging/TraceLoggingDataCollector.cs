// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
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
    [SecuritySafeCritical]
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

        /// <summary>
        /// Adds a Boolean value to the event payload.
        /// </summary>
        /// <param name="value">Value to be added.</param>
        public void AddScalar(bool value)
        {
            DataCollector.ThreadInstance.AddScalar(&value, sizeof(bool));
        }

        /// <summary>
        /// Adds an SByte value to the event payload.
        /// </summary>
        /// <param name="value">Value to be added.</param>
        //[CLSCompliant(false)]
        public void AddScalar(sbyte value)
        {
            DataCollector.ThreadInstance.AddScalar(&value, sizeof(sbyte));
        }

        /// <summary>
        /// Adds a Byte value to the event payload.
        /// </summary>
        /// <param name="value">Value to be added.</param>
        public void AddScalar(byte value)
        {
            DataCollector.ThreadInstance.AddScalar(&value, sizeof(byte));
        }

        /// <summary>
        /// Adds an Int16 value to the event payload.
        /// </summary>
        /// <param name="value">Value to be added.</param>
        public void AddScalar(short value)
        {
            DataCollector.ThreadInstance.AddScalar(&value, sizeof(short));
        }

        /// <summary>
        /// Adds a UInt16 value to the event payload.
        /// </summary>
        /// <param name="value">Value to be added.</param>
        //[CLSCompliant(false)]
        public void AddScalar(ushort value)
        {
            DataCollector.ThreadInstance.AddScalar(&value, sizeof(ushort));
        }

        /// <summary>
        /// Adds an Int32 value to the event payload.
        /// </summary>
        /// <param name="value">Value to be added.</param>
        public void AddScalar(int value)
        {
            DataCollector.ThreadInstance.AddScalar(&value, sizeof(int));
        }

        /// <summary>
        /// Adds a UInt32 value to the event payload.
        /// </summary>
        /// <param name="value">Value to be added.</param>
        //[CLSCompliant(false)]
        public void AddScalar(uint value)
        {
            DataCollector.ThreadInstance.AddScalar(&value, sizeof(uint));
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
        /// Adds a UInt64 value to the event payload.
        /// </summary>
        /// <param name="value">Value to be added.</param>
        //[CLSCompliant(false)]
        public void AddScalar(ulong value)
        {
            DataCollector.ThreadInstance.AddScalar(&value, sizeof(ulong));
        }

        /// <summary>
        /// Adds an IntPtr value to the event payload.
        /// </summary>
        /// <param name="value">Value to be added.</param>
        public void AddScalar(IntPtr value)
        {
            DataCollector.ThreadInstance.AddScalar(&value, IntPtr.Size);
        }

        /// <summary>
        /// Adds a UIntPtr value to the event payload.
        /// </summary>
        /// <param name="value">Value to be added.</param>
        //[CLSCompliant(false)]
        public void AddScalar(UIntPtr value)
        {
            DataCollector.ThreadInstance.AddScalar(&value, UIntPtr.Size);
        }

        /// <summary>
        /// Adds a Single value to the event payload.
        /// </summary>
        /// <param name="value">Value to be added.</param>
        public void AddScalar(float value)
        {
            DataCollector.ThreadInstance.AddScalar(&value, sizeof(float));
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
        /// Adds a Char value to the event payload.
        /// </summary>
        /// <param name="value">Value to be added.</param>
        public void AddScalar(char value)
        {
            DataCollector.ThreadInstance.AddScalar(&value, sizeof(char));
        }

        /// <summary>
        /// Adds a Guid value to the event payload.
        /// </summary>
        /// <param name="value">Value to be added.</param>
        public void AddScalar(Guid value)
        {
            DataCollector.ThreadInstance.AddScalar(&value, 16);
        }

        /// <summary>
        /// Adds a counted String value to the event payload.
        /// </summary>
        /// <param name="value">
        /// Value to be added. A null value is treated as a zero-length string.
        /// </param>
        public void AddBinary(string value)
        {
            DataCollector.ThreadInstance.AddBinary(value, value == null ? 0 : value.Length * 2);
        }

        /// <summary>
        /// Adds an array of Byte values to the event payload.
        /// </summary>
        /// <param name="value">
        /// Value to be added. A null value is treated as a zero-length array.
        /// </param>
        public void AddBinary(byte[] value)
        {
            DataCollector.ThreadInstance.AddBinary(value, value == null ? 0 : value.Length);
        }

        /// <summary>
        /// Adds an array of Boolean values to the event payload.
        /// </summary>
        /// <param name="value">
        /// Value to be added. A null value is treated as a zero-length array.
        /// </param>
        public void AddArray(bool[] value)
        {
            DataCollector.ThreadInstance.AddArray(value, value == null ? 0 : value.Length, sizeof(bool));
        }

        /// <summary>
        /// Adds an array of SByte values to the event payload.
        /// </summary>
        /// <param name="value">
        /// Value to be added. A null value is treated as a zero-length array.
        /// </param>
        //[CLSCompliant(false)]
        public void AddArray(sbyte[] value)
        {
            DataCollector.ThreadInstance.AddArray(value, value == null ? 0 : value.Length, sizeof(sbyte));
        }

        /// <summary>
        /// Adds an array of Int16 values to the event payload.
        /// </summary>
        /// <param name="value">
        /// Value to be added. A null value is treated as a zero-length array.
        /// </param>
        public void AddArray(short[] value)
        {
            DataCollector.ThreadInstance.AddArray(value, value == null ? 0 : value.Length, sizeof(short));
        }

        /// <summary>
        /// Adds an array of UInt16 values to the event payload.
        /// </summary>
        /// <param name="value">
        /// Value to be added. A null value is treated as a zero-length array.
        /// </param>
        //[CLSCompliant(false)]
        public void AddArray(ushort[] value)
        {
            DataCollector.ThreadInstance.AddArray(value, value == null ? 0 : value.Length, sizeof(ushort));
        }

        /// <summary>
        /// Adds an array of Int32 values to the event payload.
        /// </summary>
        /// <param name="value">
        /// Value to be added. A null value is treated as a zero-length array.
        /// </param>
        public void AddArray(int[] value)
        {
            DataCollector.ThreadInstance.AddArray(value, value == null ? 0 : value.Length, sizeof(int));
        }

        /// <summary>
        /// Adds an array of UInt32 values to the event payload.
        /// </summary>
        /// <param name="value">
        /// Value to be added. A null value is treated as a zero-length array.
        /// </param>
        //[CLSCompliant(false)]
        public void AddArray(uint[] value)
        {
            DataCollector.ThreadInstance.AddArray(value, value == null ? 0 : value.Length, sizeof(uint));
        }

        /// <summary>
        /// Adds an array of Int64 values to the event payload.
        /// </summary>
        /// <param name="value">
        /// Value to be added. A null value is treated as a zero-length array.
        /// </param>
        public void AddArray(long[] value)
        {
            DataCollector.ThreadInstance.AddArray(value, value == null ? 0 : value.Length, sizeof(long));
        }

        /// <summary>
        /// Adds an array of UInt64 values to the event payload.
        /// </summary>
        /// <param name="value">
        /// Value to be added. A null value is treated as a zero-length array.
        /// </param>
        //[CLSCompliant(false)]
        public void AddArray(ulong[] value)
        {
            DataCollector.ThreadInstance.AddArray(value, value == null ? 0 : value.Length, sizeof(ulong));
        }

        /// <summary>
        /// Adds an array of IntPtr values to the event payload.
        /// </summary>
        /// <param name="value">
        /// Value to be added. A null value is treated as a zero-length array.
        /// </param>
        public void AddArray(IntPtr[] value)
        {
            DataCollector.ThreadInstance.AddArray(value, value == null ? 0 : value.Length, IntPtr.Size);
        }

        /// <summary>
        /// Adds an array of UIntPtr values to the event payload.
        /// </summary>
        /// <param name="value">
        /// Value to be added. A null value is treated as a zero-length array.
        /// </param>
        //[CLSCompliant(false)]
        public void AddArray(UIntPtr[] value)
        {
            DataCollector.ThreadInstance.AddArray(value, value == null ? 0 : value.Length, UIntPtr.Size);
        }

        /// <summary>
        /// Adds an array of Single values to the event payload.
        /// </summary>
        /// <param name="value">
        /// Value to be added. A null value is treated as a zero-length array.
        /// </param>
        public void AddArray(float[] value)
        {
            DataCollector.ThreadInstance.AddArray(value, value == null ? 0 : value.Length, sizeof(float));
        }

        /// <summary>
        /// Adds an array of Double values to the event payload.
        /// </summary>
        /// <param name="value">
        /// Value to be added. A null value is treated as a zero-length array.
        /// </param>
        public void AddArray(double[] value)
        {
            DataCollector.ThreadInstance.AddArray(value, value == null ? 0 : value.Length, sizeof(double));
        }

        /// <summary>
        /// Adds an array of Char values to the event payload.
        /// </summary>
        /// <param name="value">
        /// Value to be added. A null value is treated as a zero-length array.
        /// </param>
        public void AddArray(char[] value)
        {
            DataCollector.ThreadInstance.AddArray(value, value == null ? 0 : value.Length, sizeof(char));
        }

        /// <summary>
        /// Adds an array of Guid values to the event payload.
        /// </summary>
        /// <param name="value">
        /// Value to be added. A null value is treated as a zero-length array.
        /// </param>
        public void AddArray(Guid[] value)
        {
            DataCollector.ThreadInstance.AddArray(value, value == null ? 0 : value.Length, 16);
        }

        /// <summary>
        /// Adds an array of Byte values to the event payload.
        /// </summary>
        /// <param name="value">
        /// Value to be added. A null value is treated as a zero-length array.
        /// </param>
        public void AddCustom(byte[] value)
        {
            DataCollector.ThreadInstance.AddArray(value, value == null ? 0 : value.Length, sizeof(byte));
        }
    }
}
