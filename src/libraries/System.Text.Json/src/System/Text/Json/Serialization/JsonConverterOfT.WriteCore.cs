// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;

namespace System.Text.Json
{
    public static partial class JsonSerializer
    {
        // There are three conditions to consider for an object (primitive value, enumerable or object) being processed here:
        // 1) The object type was specified as the root-level return type to a Deserialize method.
        // 2) The object is a property on a parent object.
        // 3) The object is an element in an enumerable.
        private static bool WriteCore(
            Utf8JsonWriter writer,
            object? value,
            JsonSerializerOptions options,
            ref WriteStack state,
            JsonConverter jsonConverter)
        {
            try
            {
                return jsonConverter.TryWriteAsObject(writer, value, options, ref state);
            }
            catch (InvalidOperationException ex) when (ex.Source == ThrowHelper.ExceptionSourceValueToRethrowAsJsonException)
            {
                ThrowHelper.ReThrowWithPath(state, ex);
                throw;
            }
            catch (JsonException ex)
            {
                ThrowHelper.AddExceptionInformation(state, ex);
                throw;
            }
        }
    }
}
