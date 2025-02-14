// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Text.Json.Serialization.Metadata;

namespace System.Text.Json.Serialization
{
    /// <summary>
    /// Base class for converters that are able to resume after reading or writing to a buffer.
    /// Writes initially start as non-resumable but are able to upgrade to resumable.
    /// This is used when the Stream-based serialization APIs are used.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal abstract class JsonHybridResumableConverter<T> : JsonConverter<T>
    {
        public sealed override bool HandleNull => false;

        private protected sealed override ConverterStrategy GetDefaultConverterStrategy() => ConverterStrategy.SegmentableValue;

        /// <summary>
        /// Writes the value without a stack frame. If the conversion should be done with resumption then <see cref="WriteWithStackFrame"/>
        /// should be called from within this method to push/pop a stack frame. This allows opt-in resumption based on the value.
        /// </summary>
        internal abstract bool WriteWithoutStackFrame(Utf8JsonWriter writer, T? value, JsonSerializerOptions options, ref WriteStack state);

        /// <summary>
        /// Pushes a stack frame, calls <see cref="JsonConverter{T}.OnTryWrite(Utf8JsonWriter, T, JsonSerializerOptions, ref WriteStack)"/> and
        /// pops the frame.
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="value"></param>
        /// <param name="options"></param>
        /// <param name="state"></param>
        /// <returns></returns>
        private protected bool WriteWithStackFrame(Utf8JsonWriter writer, T value, JsonSerializerOptions options, ref WriteStack state)
        {
#if DEBUG
            // DEBUG: ensure push/pop operations preserve stack integrity
            JsonTypeInfo originalJsonTypeInfo = state.Current.JsonTypeInfo;
#endif

            state.Push();

#if DEBUG
            // For performance, only perform validation on internal converters on debug builds.
            Debug.Assert(state.Current.OriginalDepth == 0);
            state.Current.OriginalDepth = writer.CurrentDepth;
#endif

            bool success = OnTryWrite(writer, value, options, ref state);

#if DEBUG
            if (success)
            {
                VerifyWrite(state.Current.OriginalDepth, writer);
            }
#endif

            state.Pop(success);

#if DEBUG
            Debug.Assert(ReferenceEquals(originalJsonTypeInfo, state.Current.JsonTypeInfo));
#endif

            return success;
        }
    }
}
