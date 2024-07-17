// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.Text.Json.Serialization
{
    public partial class JsonConverter<T>
    {
        internal bool ReadCore(
            ref Utf8JsonReader reader,
            out T? value,
            JsonSerializerOptions options,
            ref ReadStack state)
        {
            try
            {
                if (!state.IsContinuation && !IsRootLevelMultiContentStreamingConverter)
                {
                    // This is first call to the converter -- advance the reader
                    // to the first JSON token and perform a read-ahead if necessary.
                    if (!reader.TryAdvanceWithOptionalReadAhead(RequiresReadAhead))
                    {
                        if (state.SupportContinuation && state.Current.ReturnValue is object result)
                        {
                            // This branch is hit when deserialization has completed in an earlier call
                            // but we're still processing trailing whitespace. Return the result stored in the state machine.
                            Debug.Assert(!reader.AllowMultipleValues, "should not be entered by converters allowing multiple values.");
                            value = (T)result;
                        }
                        else
                        {
                            value = default;
                        }

                        return reader.IsFinalBlock;
                    }
                }

                bool success = TryRead(ref reader, state.Current.JsonTypeInfo.Type, options, ref state, out value, out _);
                if (success)
                {
                    if (!reader.AllowMultipleValues)
                    {
                        // Read any trailing whitespace. This will throw if JsonCommentHandling=Disallow.
                        // Avoiding setting ReturnValue for the final block; reader.Read() returns 'false' even when this is the final block.
                        if (!reader.Read() && !reader.IsFinalBlock)
                        {
                            // This method will re-enter if so set `ReturnValue` which will be returned during re-entry.
                            state.Current.ReturnValue = value;
                            success = false;
                        }
                    }
                }

                return success;
            }
            catch (Exception ex)
            {
                switch (ex)
                {
                    case JsonReaderException jsonReaderEx:
                        ThrowHelper.ReThrowWithPath(ref state, jsonReaderEx);
                        break;

                    case FormatException when ex.Source == ThrowHelper.ExceptionSourceValueToRethrowAsJsonException:
                        ThrowHelper.ReThrowWithPath(ref state, reader, ex);
                        break;

                    case InvalidOperationException when ex.Source == ThrowHelper.ExceptionSourceValueToRethrowAsJsonException:
                        ThrowHelper.ReThrowWithPath(ref state, reader, ex);
                        break;

                    case JsonException jsonEx when jsonEx.Path is null:
                        // JsonExceptions where the Path property is already set
                        // typically originate from nested calls to JsonSerializer;
                        // treat these cases as any other exception type and do not
                        // overwrite any exception information.
                        ThrowHelper.AddJsonExceptionInformation(ref state, reader, jsonEx);
                        break;

                    case NotSupportedException when !ex.Message.Contains(" Path: "):
                        // If the message already contains Path, just re-throw. This could occur in serializer re-entry cases.
                        // To get proper Path semantics in re-entry cases, APIs that take 'state' need to be used.
                        ThrowHelper.ThrowNotSupportedException(ref state, reader, ex);
                        break;
                }

                throw;
            }
        }
    }
}
