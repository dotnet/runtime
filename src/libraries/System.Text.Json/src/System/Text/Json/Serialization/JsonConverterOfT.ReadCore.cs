// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace System.Text.Json.Serialization
{
    public partial class JsonConverter<T>
    {
        internal sealed override object? ReadCoreAsObject(
            ref Utf8JsonReader reader,
            JsonSerializerOptions options,
            ref ReadStack state)
        {
            return ReadCore(ref reader, options, ref state);
        }

        internal T? ReadCore(
            ref Utf8JsonReader reader,
            JsonSerializerOptions options,
            ref ReadStack state)
        {
            try
            {
                if (!state.IsContinuation)
                {
                    if (!SingleValueReadWithReadAhead(ClassType, ref reader, ref state))
                    {
                        if (state.SupportContinuation)
                        {
                            // If a Stream-based scenaio, return the actual value previously found;
                            // this may or may not be the final pass through here.
                            state.BytesConsumed += reader.BytesConsumed;
                            if (state.Current.ReturnValue == null)
                            {
                                // Avoid returning null for value types.
                                return default;
                            }

                            return (T)state.Current.ReturnValue!;
                        }
                        else
                        {
                            // Read more data until we have the full element.
                            state.BytesConsumed += reader.BytesConsumed;
                            return default;
                        }
                    }
                }
                else
                {
                    // For a continuation, read ahead here to avoid having to build and then tear
                    // down the call stack if there is more than one buffer fetch necessary.
                    if (!SingleValueReadWithReadAhead(ClassType.Value, ref reader, ref state))
                    {
                        state.BytesConsumed += reader.BytesConsumed;
                        return default;
                    }
                }

                JsonPropertyInfo jsonPropertyInfo = state.Current.JsonClassInfo.PropertyInfoForClassInfo;
                bool success = TryRead(ref reader, jsonPropertyInfo.RuntimePropertyType!, options, ref state, out T value);
                if (success)
                {
                    // Read any trailing whitespace. This will throw if JsonCommentHandling=Disallow.
                    // Avoiding setting ReturnValue for the final block; reader.Read() returns 'false' even when this is the final block.
                    if (!reader.Read() && !reader.IsFinalBlock)
                    {
                        // This method will re-enter if so set `ReturnValue` which will be returned during re-entry.
                        state.Current.ReturnValue = value;
                    }
                }

                state.BytesConsumed += reader.BytesConsumed;
                return value;
            }
            catch (JsonReaderException ex)
            {
                ThrowHelper.ReThrowWithPath(state, ex);
                return default;
            }
            catch (FormatException ex) when (ex.Source == ThrowHelper.ExceptionSourceValueToRethrowAsJsonException)
            {
                ThrowHelper.ReThrowWithPath(state, reader, ex);
                return default;
            }
            catch (InvalidOperationException ex) when (ex.Source == ThrowHelper.ExceptionSourceValueToRethrowAsJsonException)
            {
                ThrowHelper.ReThrowWithPath(state, reader, ex);
                return default;
            }
            catch (JsonException ex)
            {
                ThrowHelper.AddJsonExceptionInformation(state, reader, ex);
                throw;
            }
            catch (NotSupportedException ex)
            {
                // If the message already contains Path, just re-throw. This could occur in serializer re-entry cases.
                // To get proper Path semantics in re-entry cases, APIs that take 'state' need to be used.
                if (ex.Message.Contains(" Path: "))
                {
                    throw;
                }

                ThrowHelper.ThrowNotSupportedException(state, reader, ex);
                return default;
            }
        }
    }
}
