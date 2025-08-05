// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Text.Json.Serialization
{
    public partial class JsonConverter<T>
    {
        internal bool WriteCore(
            Utf8JsonWriter writer,
            in T? value,
            JsonSerializerOptions options,
            ref WriteStack state)
        {
            try
            {
                return TryWrite(writer, value, options, ref state);
            }
            catch (Exception ex)
            {
                if (!state.SupportAsync)
                {
                    // Async serializers should dispose sync and
                    // async disposables from the async root method.
                    state.DisposePendingDisposablesOnException();
                }

                switch (ex)
                {
                    case InvalidOperationException when ex.Source == ThrowHelper.ExceptionSourceValueToRethrowAsJsonException:
                        ThrowHelper.ReThrowWithPath(ref state, ex);
                        break;

                    case JsonException { Path: null } jsonException:
                        // JsonExceptions where the Path property is already set
                        // typically originate from nested calls to JsonSerializer;
                        // treat these cases as any other exception type and do not
                        // overwrite any exception information.
                        ThrowHelper.AddJsonExceptionInformation(ref state, jsonException);
                        break;

                    case NotSupportedException when !ex.Message.Contains(" Path: "):
                        // If the message already contains Path, just re-throw. This could occur in serializer re-entry cases.
                        // To get proper Path semantics in re-entry cases, APIs that take 'state' need to be used.
                        ThrowHelper.ThrowNotSupportedException(ref state, ex);
                        break;
                }

                throw;
            }
        }
    }
}
