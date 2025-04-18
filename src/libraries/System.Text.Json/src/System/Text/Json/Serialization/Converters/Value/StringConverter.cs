// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.Json.Nodes;
using System.Text.Json.Schema;
using System.Text.Json.Serialization.Metadata;

namespace System.Text.Json.Serialization.Converters
{
    internal sealed class StringConverter : JsonHybridResumableConverter<string?>
    {
        public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
            {
                return null;
            }

            return reader.GetString();
        }

        // When called without a stack (top level)
        public override void Write(Utf8JsonWriter writer, string? value, JsonSerializerOptions options)
        {
            if (value is null)
            {
                writer.WriteNullValue();
                return;
            }

            writer.WriteStringValue(value);
        }

        internal override bool OnTryRead(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options, scoped ref ReadStack state, out string? value)
        {
            if (!reader._hasPartialStringValue && state.Current.ObjectState == StackFrameObjectState.None)
            {
                value = reader.GetString();
                return true;
            }

            if (state.Current.ObjectState < StackFrameObjectState.CreatedObject)
            {
                // This is the first segment so it can't be the only/last segment since we are on the slow read path.
                Debug.Assert(reader._hasPartialStringValue);

                state.Current.ObjectState = StackFrameObjectState.ReadElements;
                state.Current.ReturnValue = new List<ArraySegment<char>>();

                ReadSegment(ref reader, ref state);

                value = null;
                return false;
            }

            Debug.Assert(state.Current.ObjectState == StackFrameObjectState.ReadElements);

            bool consumedEntireString = reader.ContinueConsumeString();
            if (!consumedEntireString && reader.IsFinalBlock)
            {
                // TODO
                throw new Exception();
            }

            ReadSegment(ref reader, ref state);

            if (consumedEntireString)
            {
                value = GetStringFromChunks(reader, state);
                return true;
            }

            value = null;
            return false;

            static string GetStringFromChunks(Utf8JsonReader reader, ReadStack state)
            {
                Debug.Assert(reader.TokenType == JsonTokenType.String);

                List<ArraySegment<char>> chunks = (List<ArraySegment<char>>)state.Current.ReturnValue!;

                int totalSize = 0;
                foreach (ArraySegment<char> c in chunks)
                {
                    totalSize += c.Count;
                }

                // TODO skip zeroing
                string ret = new string((char)0, totalSize);
                unsafe
                {
                    fixed (char* r = ret)
                    {
                        int idx = 0;
                        foreach (ArraySegment<char> c in chunks)
                        {
                            c.AsSpan().CopyTo(new Span<char>(r + idx, c.Count));
                            idx += c.Count;
                            ArrayPool<char>.Shared.Return(c.Array!);
                        }
                    }
                }

                return ret;
            }
        }

        private static void ReadSegment(ref Utf8JsonReader reader, scoped ref ReadStack state)
        {
            ReadOnlySpan<byte> newSegment = reader.ValueSpan.Slice(state.Current.PropertyIndex);

            if (reader.ValueIsEscaped)
            {
                int idx = newSegment.IndexOf(JsonConstants.BackSlash);
                if (idx >= 0)
                {
                    ReadSegmentEscaped(ref reader, ref state, newSegment, idx);
                    return;
                }
            }

            ReadSegmentCore(ref reader, ref state, newSegment);
        }

        private static void ReadSegmentEscaped(ref Utf8JsonReader reader, scoped ref ReadStack state, ReadOnlySpan<byte> newSegment, int indexOfFirstCharToEscape)
        {
            int additionalByteCount = reader.ValueSpan.Length - state.Current.PropertyIndex;

            byte[] unescaped = ArrayPool<byte>.Shared.Rent(additionalByteCount);
            JsonReaderHelper.Unescape(newSegment, unescaped, indexOfFirstCharToEscape, out int writtenTemp);
            newSegment = unescaped.AsSpan(0, writtenTemp);

            ReadSegmentCore(ref reader, ref state, newSegment);

            ArrayPool<byte>.Shared.Return(unescaped);
        }

        private static void ReadSegmentCore(ref Utf8JsonReader reader, scoped ref ReadStack state, ReadOnlySpan<byte> newSegment)
        {
            int additionalByteCount = reader.ValueSpan.Length - state.Current.PropertyIndex;
            char[] chunk = ArrayPool<char>.Shared.Rent(additionalByteCount);

            int written = JsonReaderHelper.TranscodeHelper(newSegment, chunk);
            state.Current.PropertyIndex = reader.ValueSpan.Length;

            List<ArraySegment<char>> chunks = (List<ArraySegment<char>>)state.Current.ReturnValue!;
            chunks.Add(new ArraySegment<char>(chunk, 0, written));
        }

        internal override bool WriteWithoutStackFrame(Utf8JsonWriter writer, string? value, JsonSerializerOptions options, ref WriteStack state)
        {
            if (value == null)
            {
                writer.WriteNullValue();
                return true;
            }
            else if (state.FlushThreshold == 0 || value.Length < state.FlushThreshold)
            {
                // Fast write for small strings. Note that previous unflushed data may still be in the
                // writer but we can let the enclosing container handle the flushing in this case.
                writer.WriteStringValue(value);
                return true;
            }

            return WriteWithStackFrame(writer, value, options, ref state);
        }

        // When called with a stack.
        internal override bool OnTryWrite(Utf8JsonWriter writer, string? value, JsonSerializerOptions options, ref WriteStack state)
        {
            if (!state.Current.ProcessedStartToken)
            {
                state.Current.ProcessedStartToken = true;
                if (ShouldFlush(ref state, writer))
                {
                    return false;
                }
            }

            bool isFinal = GetNextWriteSegment(value, ref state, out int writeIndex, out int writeLength);
            writer.WriteStringValueSegment(value.AsSpan(writeIndex, writeLength), isFinal);
            state.Current.EnumeratorIndex += writeLength;

            // We either wrote the entire input or hit the flush threshold.
            Debug.Assert(ShouldFlush(ref state, writer) || isFinal);

            return isFinal;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool GetNextWriteSegment(ReadOnlySpan<char> value, ref WriteStack state, out int start, out int length)
        {
            Debug.Assert(state.Current.EnumeratorIndex >= 0);
            Debug.Assert(state.Current.EnumeratorIndex < value.Length);

            int writeIndex = state.Current.EnumeratorIndex;

            // Write enough to guarantee a flush. Base64 encoding expands the data by 4/3, so we can write less and still hit the threshold,
            // but we don't need to be exact because the threshold is set very conservatively.
            int writeLength = state.FlushThreshold == 0 ? int.MaxValue : state.FlushThreshold + 1;

            // If the input isn't large enough to hit the flush threshold, write the entire input as the final segment.
            bool isFinal = false;
            int remainingInputBytes = value.Length - state.Current.EnumeratorIndex;
            if (remainingInputBytes <= writeLength)
            {
                writeLength = remainingInputBytes;
                isFinal = true;
            }

            start = writeIndex;
            length = writeLength;

            return isFinal;
        }

        internal override string ReadAsPropertyNameCore(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            Debug.Assert(reader.TokenType == JsonTokenType.PropertyName);
            return reader.GetString()!;
        }

        internal override void WriteAsPropertyNameCore(Utf8JsonWriter writer, string value, JsonSerializerOptions options, bool isWritingExtensionDataProperty)
        {
            if (value is null)
            {
                ThrowHelper.ThrowArgumentNullException(nameof(value));
            }

            if (options.DictionaryKeyPolicy != null && !isWritingExtensionDataProperty)
            {
                value = options.DictionaryKeyPolicy.ConvertName(value);

                if (value == null)
                {
                    ThrowHelper.ThrowInvalidOperationException_NamingPolicyReturnNull(options.DictionaryKeyPolicy);
                }
            }

            writer.WritePropertyName(value);
        }

        internal override JsonSchema? GetSchema(JsonNumberHandling _) => new() { Type = JsonSchemaType.String };

        public sealed override void WriteAsPropertyName(Utf8JsonWriter writer, string? value, JsonSerializerOptions options)
        {
            if (value is null)
            {
                ThrowHelper.ThrowArgumentNullException(nameof(value));
            }

            WriteAsPropertyNameCore(writer, value, options, isWritingExtensionDataProperty: false);
        }

        public sealed override string? ReadAsPropertyName(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                ThrowHelper.ThrowInvalidOperationException_ExpectedPropertyName(reader.TokenType);
            }

            return ReadAsPropertyNameCore(ref reader, typeToConvert, options);
        }
    }
}
