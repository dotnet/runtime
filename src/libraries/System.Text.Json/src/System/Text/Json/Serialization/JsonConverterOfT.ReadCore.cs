// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Text.Json.Serialization
{
    public partial class JsonConverter<T>
    {
        internal T? ReadCore(
            ref Utf8JsonReader reader,
            JsonSerializerOptions options,
            ref ReadStack state)
        {
            try
            {
                if (!state.IsContinuation)
                {
                    // This is first call to the converter -- advance the reader
                    // to the first JSON token and perform a read-ahead if necessary.
                    if (!reader.TryAdvanceWithOptionalReadAhead(RequiresReadAhead))
                    {
                        if (state.SupportContinuation && state.Current.ReturnValue is object result)
                        {
                            // This branch is hit when deserialization has completed in an earlier call
                            // but we're still processing trailing whitespace. Return the result stored in the state machine.
                            return (T)result;
                        }

                        return default;
                    }
                }

                bool success = TryRead(ref reader, state.Current.JsonTypeInfo.Type, options, ref state, out T? value, out _);
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

                return value;
            }
            catch (JsonReaderException ex)
            {
                ThrowHelper.ReThrowWithPath(ref state, ex);
                return default;
            }
            catch (FormatException ex) when (ex.Source == ThrowHelper.ExceptionSourceValueToRethrowAsJsonException)
            {
                ThrowHelper.ReThrowWithPath(ref state, reader, ex);
                return default;
            }
            catch (InvalidOperationException ex) when (ex.Source == ThrowHelper.ExceptionSourceValueToRethrowAsJsonException)
            {
                ThrowHelper.ReThrowWithPath(ref state, reader, ex);
                return default;
            }
            catch (JsonException ex) when (ex.Path == null)
            {
                // JsonExceptions where the Path property is already set
                // typically originate from nested calls to JsonSerializer;
                // treat these cases as any other exception type and do not
                // overwrite any exception information.

                ThrowHelper.AddJsonExceptionInformation(ref state, reader, ex);
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

                ThrowHelper.ThrowNotSupportedException(ref state, reader, ex);
                return default;
            }
        }
    }
}
