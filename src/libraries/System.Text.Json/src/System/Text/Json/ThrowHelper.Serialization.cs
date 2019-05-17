// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace System.Text.Json
{
    internal static partial class ThrowHelper
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ThrowArgumentException_DeserializeWrongType(Type type, object value)
        {
            throw new ArgumentException(SR.Format(SR.DeserializeWrongType, type, value.GetType()));
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static NotSupportedException GetNotSupportedException_SerializationNotSupportedCollection(Type propertyType, Type parentType, MemberInfo memberInfo)
        {
            if (parentType != null && parentType != typeof(object) && memberInfo != null)
            {
                return new NotSupportedException(SR.Format(SR.SerializationNotSupportedCollection, propertyType, $"{parentType}.{memberInfo.Name}"));
            }

            return new NotSupportedException(SR.Format(SR.SerializationNotSupportedCollectionType, propertyType));
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ThrowJsonException_DeserializeUnableToConvertValue(Type propertyType, in Utf8JsonReader reader, string path)
        {
            ThowJsonException(SR.Format(SR.DeserializeUnableToConvertValue, propertyType), in reader, path);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ThrowJsonException_DeserializeUnableToConvertValue(Type propertyType, string path)
        {
            string message = SR.Format(SR.DeserializeUnableToConvertValue, propertyType) + $" Path: {path}.";
            throw new JsonException(message, path, null, null);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ThrowJsonException_DeserializeCannotBeNull(in Utf8JsonReader reader, string path)
        {
            ThowJsonException(SR.DeserializeCannotBeNull, in reader, path);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ThrowInvalidOperationException_SerializerOptionsImmutable()
        {
            throw new InvalidOperationException(SR.SerializerOptionsImmutable);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ThrowInvalidOperationException_SerializerPropertyNameConflict(JsonClassInfo jsonClassInfo, JsonPropertyInfo jsonPropertyInfo)
        {
            throw new InvalidOperationException(SR.Format(SR.SerializerPropertyNameConflict, jsonClassInfo.Type, jsonPropertyInfo.PropertyInfo.Name));
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ThrowInvalidOperationException_SerializerPropertyNameNull(Type parentType, JsonPropertyInfo jsonPropertyInfo)
        {
            throw new InvalidOperationException(SR.Format(SR.SerializerPropertyNameNull, parentType, jsonPropertyInfo.PropertyInfo.Name));
        }

        public static void ThrowJsonException_DeserializeDataRemaining(long length, long bytesRemaining)
        {
            throw new JsonException(SR.Format(SR.DeserializeDataRemaining, length, bytesRemaining), path: null, lineNumber: null, bytePositionInLine: null);
        }

        public static void ThrowJsonException_DeserializeDuplicateKey(string key, in Utf8JsonReader reader, string path)
        {
            ThowJsonException(SR.Format(SR.DeserializeDuplicateKey, key), in reader, path);
        }

        private static void ThowJsonException(string message, in Utf8JsonReader reader, string path)
        {
            long lineNumber = reader.CurrentState._lineNumber;
            long bytePositionInLine = reader.CurrentState._bytePositionInLine;

            message += $" Path: {path} | LineNumber: {lineNumber} | BytePositionInLine: {bytePositionInLine}.";
            throw new JsonException(message, path, lineNumber, bytePositionInLine);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ReThrowWithPath(JsonException exception, string path)
        {
            Debug.Assert(exception.Path == null);

            string message = exception.Message;

            // Insert the "Path" portion before "LineNumber" and "BytePositionInLine".
            int iPos = message.LastIndexOf(" LineNumber: ", StringComparison.InvariantCulture);
            if (iPos >= 0)
            {
                message = $"{message.Substring(0, iPos)} Path: {path} |{message.Substring(iPos)}";
            }
            else
            {
                message += $" Path: {path}.";
            }

            throw new JsonException(message, path, exception.LineNumber, exception.BytePositionInLine, exception);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ThrowInvalidOperationException_SerializationDuplicateAttribute(Type attribute)
        {
            throw new InvalidOperationException(SR.Format(SR.SerializationDuplicateAttribute, attribute));
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ThrowInvalidOperationException_SerializationDataExtensionPropertyInvalid(JsonClassInfo jsonClassInfo, JsonPropertyInfo jsonPropertyInfo)
        {
            throw new InvalidOperationException(SR.Format(SR.SerializationDataExtensionPropertyInvalid, jsonClassInfo.Type, jsonPropertyInfo.PropertyInfo.Name));
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ThrowInvalidOperationException_SerializationDataExtensionPropertyInvalid(JsonClassInfo jsonClassInfo, Type invalidType)
        {
            throw new InvalidOperationException(SR.Format(SR.SerializationDataExtensionPropertyInvalidElement, jsonClassInfo.Type, jsonClassInfo.DataExtensionProperty.PropertyInfo.Name, invalidType));
        }
    }
}
