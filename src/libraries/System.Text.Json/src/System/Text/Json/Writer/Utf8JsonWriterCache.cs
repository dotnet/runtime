// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;

namespace System.Text.Json
{
    /// <summary>
    /// Defines a thread-local cache for JsonSerializer to store reusable Utf8JsonWriter/IBufferWriter instances.
    /// </summary>
    internal static class Utf8JsonWriterCache
    {
        [ThreadStatic]
        private static ThreadLocalState? t_threadLocalState;

        public static Utf8JsonWriter RentWriterAndBuffer(JsonSerializerOptions options, out PooledByteBufferWriter bufferWriter) =>
            RentWriterAndBuffer(options.GetWriterOptions(), options.DefaultBufferSize, out bufferWriter);

        public static Utf8JsonWriter RentWriterAndBuffer(JsonWriterOptions options, int defaultBufferSize, out PooledByteBufferWriter bufferWriter)
        {
            ThreadLocalState state = t_threadLocalState ??= new();
            Utf8JsonWriter writer;

            if (state.RentedWriters++ == 0)
            {
                // First JsonSerializer call in the stack -- initialize & return the cached instances.
                bufferWriter = state.BufferWriter;
                writer = state.Writer;

                bufferWriter.InitializeEmptyInstance(defaultBufferSize);
                writer.Reset(bufferWriter, options);
            }
            else
            {
                // We're in a recursive JsonSerializer call -- return fresh instances.
                bufferWriter = new PooledByteBufferWriter(defaultBufferSize);
                writer = new Utf8JsonWriter(bufferWriter, options);
            }

            return writer;
        }

        public static Utf8JsonWriter RentWriter(JsonSerializerOptions options, IBufferWriter<byte> bufferWriter)
        {
            ThreadLocalState state = t_threadLocalState ??= new();
            Utf8JsonWriter writer;

            if (state.RentedWriters++ == 0)
            {
                // First JsonSerializer call in the stack -- initialize & return the cached instance.
                writer = state.Writer;
                writer.Reset(bufferWriter, options.GetWriterOptions());
            }
            else
            {
                // We're in a recursive JsonSerializer call -- return a fresh instance.
                writer = new Utf8JsonWriter(bufferWriter, options.GetWriterOptions());
            }

            return writer;
        }

        public static void ReturnWriterAndBuffer(Utf8JsonWriter writer, PooledByteBufferWriter bufferWriter)
        {
            Debug.Assert(t_threadLocalState != null);
            ThreadLocalState state = t_threadLocalState;

            writer.ResetAllStateForCacheReuse();
            bufferWriter.ClearAndReturnBuffers();

            int rentedWriters = --state.RentedWriters;
            Debug.Assert((rentedWriters == 0) == (ReferenceEquals(state.BufferWriter, bufferWriter) && ReferenceEquals(state.Writer, writer)));
        }

        public static void ReturnWriter(Utf8JsonWriter writer)
        {
            Debug.Assert(t_threadLocalState != null);
            ThreadLocalState state = t_threadLocalState;

            writer.ResetAllStateForCacheReuse();

            int rentedWriters = --state.RentedWriters;
            Debug.Assert((rentedWriters == 0) == ReferenceEquals(state.Writer, writer));
        }

        private sealed class ThreadLocalState
        {
            public readonly PooledByteBufferWriter BufferWriter;
            public readonly Utf8JsonWriter Writer;
            public int RentedWriters;

            public ThreadLocalState()
            {
                BufferWriter = PooledByteBufferWriter.CreateEmptyInstanceForCaching();
                Writer = Utf8JsonWriter.CreateEmptyInstanceForCaching();
            }
        }
    }
}
