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
        public static void ThrowJsonException_DeserializeUnableToConvertValue(Type propertyType, in Utf8JsonReader reader, string path, Exception innerException = null)
        {
            ThrowJsonException(SR.Format(SR.DeserializeUnableToConvertValue, propertyType), in reader, path, innerException);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ThrowJsonException_DeserializeUnableToConvertValue(Type propertyType, string path, Exception innerException = null)
        {
            string message = SR.Format(SR.DeserializeUnableToConvertValue, propertyType) + $" Path: {path}.";
            throw new JsonException(message, path, null, null, innerException);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ThrowJsonException_DepthTooLarge(int currentDepth, in WriteStack writeStack, JsonSerializerOptions options)
        {
            var ex = new JsonException(SR.Format(SR.DepthTooLarge, currentDepth, options.EffectiveMaxDepth));
            AddExceptionInformation(writeStack, ex);
            throw ex;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ThrowJsonException_SerializationConverterRead(in Utf8JsonReader reader, string path, string converter)
        {
            ThrowJsonException(SR.Format(SR.SerializationConverterRead, converter), reader, path);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ThrowJsonException_SerializationConverterWrite(string path, string converter)
        {
            ThrowJsonException(SR.Format(SR.SerializationConverterWrite, converter), path);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ThrowJsonException()
        {
            throw new JsonException();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ThrowInvalidOperationException_SerializationConverterNotCompatible(Type converterType, Type type)
        {
            throw new InvalidOperationException(SR.Format(SR.SerializationConverterNotCompatible, converterType, type));
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ThrowInvalidOperationException_SerializationConverterOnAttributeInvalid(Type classType, PropertyInfo propertyInfo)
        {
            string location = classType.ToString();
            if (propertyInfo != null)
            {
                location += $".{propertyInfo.Name}";
            }

            throw new InvalidOperationException(SR.Format(SR.SerializationConverterOnAttributeInvalid, location));
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ThrowInvalidOperationException_SerializationConverterOnAttributeNotCompatible(Type classTypeAttributeIsOn, PropertyInfo propertyInfo, Type typeToConvert)
        {
            string location = classTypeAttributeIsOn.ToString();

            if (propertyInfo != null)
            {
                location += $".{propertyInfo.Name}";
            }

            throw new InvalidOperationException(SR.Format(SR.SerializationConverterOnAttributeNotCompatible, location, typeToConvert));
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

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ThrowInvalidOperationException_SerializerDictionaryKeyNull(Type policyType)
        {
            throw new InvalidOperationException(SR.Format(SR.SerializerDictionaryKeyNull, policyType));
        }

        public static void ThrowJsonException_DeserializeDataRemaining(long length, long bytesRemaining)
        {
            throw new JsonException(SR.Format(SR.DeserializeDataRemaining, length, bytesRemaining), path: null, lineNumber: null, bytePositionInLine: null);
        }

        // todo: since we now catch and re-throw JsonException and add Path etc, we can clean up callers to this to not pass the reader and path.
        private static void ThrowJsonException(string message, in Utf8JsonReader reader, string path, Exception innerException = null)
        {
            long lineNumber = reader.CurrentState._lineNumber;
            long bytePositionInLine = reader.CurrentState._bytePositionInLine;

            message += $" Path: {path} | LineNumber: {lineNumber} | BytePositionInLine: {bytePositionInLine}.";
            throw new JsonException(message, path, lineNumber, bytePositionInLine, innerException);
        }

        private static void ThrowJsonException(string message, string path, Exception innerException = null)
        {
            message += $" Path: {path}.";
            throw new JsonException(message, path, null, null, innerException);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ReThrowWithPath(in ReadStack readStack, JsonReaderException ex)
        {
            Debug.Assert(ex.Path == null);

            string path = readStack.JsonPath;
            string message = ex.Message;

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

            throw new JsonException(message, path, ex.LineNumber, ex.BytePositionInLine, ex);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ReThrowWithPath(in ReadStack readStack, in Utf8JsonReader reader, Exception ex)
        {
            JsonException jsonException = new JsonException(null, ex);
            AddExceptionInformation(readStack, reader, jsonException);
            throw jsonException;
        }

        public static void AddExceptionInformation(in ReadStack readStack, in Utf8JsonReader reader, JsonException ex)
        {
            long lineNumber = reader.CurrentState._lineNumber;
            ex.LineNumber = lineNumber;

            long bytePositionInLine = reader.CurrentState._bytePositionInLine;
            ex.BytePositionInLine = bytePositionInLine;

            string path = readStack.JsonPath;
            ex.Path = path;

            // If the message is empty, use a default message with Path, LineNumber and BytePositionInLine.
            if (string.IsNullOrEmpty(ex.Message))
            {
                Type propertyType = readStack.Current.JsonPropertyInfo?.RuntimePropertyType;
                if (propertyType == null)
                {
                    propertyType = readStack.Current.JsonClassInfo.Type;
                }

                ex.SetMessage($"{SR.Format(SR.DeserializeUnableToConvertValue, propertyType)} Path: {path} | LineNumber: {lineNumber} | BytePositionInLine: {bytePositionInLine}.");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ReThrowWithPath(in WriteStack writeStack, Exception ex)
        {
            JsonException jsonException = new JsonException(null, ex);
            AddExceptionInformation(writeStack, jsonException);
            throw jsonException;
        }

        public static void AddExceptionInformation(in WriteStack writeStack, JsonException ex)
        {
            string path = writeStack.PropertyPath;
            ex.Path = path;

            // If the message is empty, use a default message with the Path.
            if (string.IsNullOrEmpty(ex.Message))
            {
                ex.SetMessage(SR.Format(SR.SerializeUnableToSerialize, path));
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ThrowInvalidOperationException_SerializationDuplicateAttribute(Type attribute, Type classType, PropertyInfo propertyInfo)
        {
            string location = classType.ToString();
            if (propertyInfo != null)
            {
                location += $".{propertyInfo.Name}";
            }

            throw new InvalidOperationException(SR.Format(SR.SerializationDuplicateAttribute, attribute, location));
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ThrowInvalidOperationException_SerializationDuplicateTypeAttribute(Type classType, Type attribute)
        {
            throw new InvalidOperationException(SR.Format(SR.SerializationDuplicateTypeAttribute, classType, attribute));
        }

        

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ThrowInvalidOperationException_SerializationDataExtensionPropertyInvalid(JsonClassInfo jsonClassInfo, JsonPropertyInfo jsonPropertyInfo)
        {
            throw new InvalidOperationException(SR.Format(SR.SerializationDataExtensionPropertyInvalid, jsonClassInfo.Type, jsonPropertyInfo.PropertyInfo.Name));
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ThrowInvalidOperationException_DeserializeMissingParameterlessConstructor(Type invalidType)
        {
            throw new NotSupportedException(SR.Format(SR.DeserializeMissingParameterlessConstructor, invalidType));
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ThrowInvalidOperationException_DeserializePolymorphicInterface(Type invalidType)
        {
            throw new NotSupportedException(SR.Format(SR.DeserializePolymorphicInterface, invalidType));
        }
    }
}
