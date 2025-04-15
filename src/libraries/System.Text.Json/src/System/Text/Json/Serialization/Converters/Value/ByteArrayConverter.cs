// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Buffers.Text;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.Json.Schema;

namespace System.Text.Json.Serialization.Converters
{
    internal sealed class ByteArrayConverter : JsonHybridResumableConverter<byte[]?>
    {
        public override byte[]? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
            {
                return null;
            }

            return reader.GetBytesFromBase64();
        }

        public override void Write(Utf8JsonWriter writer, byte[]? value, JsonSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNullValue();
            }
            else
            {
                writer.WriteBase64StringValue(value);
            }
        }

        internal override bool OnTryRead(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options, scoped ref ReadStack state, out byte[]? value)
        {
            if (!reader._hasPartialStringValue && state.Current.ObjectState == StackFrameObjectState.None)
            {
                value = reader.GetBytesFromBase64();
                return true;
            }

            if (state.Current.ObjectState < StackFrameObjectState.CreatedObject)
            {
                // This is the first segment so it can't be the only/last segment since we are on the slow read path.
                Debug.Assert(reader._hasPartialStringValue);

                state.Current.ObjectState = StackFrameObjectState.ReadElements;

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

            static byte[] GetStringFromChunks(Utf8JsonReader reader, ReadStack state)
            {
                Debug.Assert(reader.TokenType == JsonTokenType.String);

                List<ArraySegment<byte>>? chunks = (List<ArraySegment<byte>>?)state.Current.ReturnValue!;
                if (chunks == null)
                {
                    // Nothing escaped, so just use the raw value.
                    return reader.ValueSpan.ToArray();
                }

                int totalSize = 0;
                foreach (ArraySegment<byte> c in chunks)
                {
                    totalSize += c.Count;
                }

                byte[] ret = new byte[totalSize];
                int idx = 0;
                foreach (ArraySegment<byte> c in chunks)
                {
                    c.AsSpan().CopyTo(ret.AsSpan(idx, c.Count));
                    idx += c.Count;
                    ArrayPool<byte>.Shared.Return(c.Array!);
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

            state.Current.PropertyIndex = reader.ValueSpan.Length;
        }

        private static void ReadSegmentEscaped(ref Utf8JsonReader reader, scoped ref ReadStack state, ReadOnlySpan<byte> newSegment, int indexOfFirstCharToEscape)
        {
            List<ArraySegment<byte>>? chunks = (List<ArraySegment<byte>>?)state.Current.ReturnValue;
            if (chunks == null)
            {
                // First time we are encountering an escaped character.
                chunks = new List<ArraySegment<byte>>();
                state.Current.ReturnValue = chunks;

                // The chunk must include all the segments skipped so far.
                indexOfFirstCharToEscape += state.Current.PropertyIndex;
                newSegment = reader.ValueSpan;
            }

            byte[] unescaped = ArrayPool<byte>.Shared.Rent(newSegment.Length);
            JsonReaderHelper.Unescape(newSegment, unescaped, indexOfFirstCharToEscape, out int written);
            state.Current.PropertyIndex = reader.ValueSpan.Length;

            chunks.Add(new ArraySegment<byte>(unescaped, 0, written));
        }

        internal override bool WriteWithoutStackFrame(Utf8JsonWriter writer, byte[]? value, JsonSerializerOptions options, ref WriteStack state)
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
                writer.WriteBase64StringValue(value);
                return true;
            }

            return WriteWithStackFrame(writer, value, options, ref state);
        }

        internal override bool OnTryWrite(Utf8JsonWriter writer, byte[]? value, JsonSerializerOptions options, ref WriteStack state)
        {
            if (!state.Current.ProcessedStartToken)
            {
                state.Current.ProcessedStartToken = true;
                if (ShouldFlush(ref state, writer))
                {
                    return false;
                }
            }

            Debug.Assert(value != null);
            bool isFinal = GetNextWriteSegment(value, ref state, out int writeIndex, out int writeLength);
            writer.WriteBase64StringSegment(value.AsSpan(writeIndex, writeLength), isFinal);
            state.Current.EnumeratorIndex += writeLength;

            // We either wrote the entire input or hit the flush threshold.
            Debug.Assert(ShouldFlush(ref state, writer) || isFinal);

            return isFinal;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool GetNextWriteSegment(byte[] value, ref WriteStack state, out int start, out int length)
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

        internal override JsonSchema? GetSchema(JsonNumberHandling _) => new() { Type = JsonSchemaType.String };
    }
}
