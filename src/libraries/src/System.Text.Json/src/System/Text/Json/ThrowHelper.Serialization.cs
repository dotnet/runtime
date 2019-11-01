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

        public static void ThrowInvalidOperationException_SerializerCycleDetected(int maxDepth)
        {
            throw new JsonException(SR.Format(SR.SerializerCycleDetected, maxDepth));
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ThrowJsonException_DeserializeUnableToConvertValue(Type propertyType)
        {
            var ex = new JsonException(SR.Format(SR.DeserializeUnableToConvertValue, propertyType));
            ex.AppendPathInformation = true;
            throw ex;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ThrowJsonException_DeserializeUnableToConvertValue(Type propertyType, string path, Exception innerException = null)
        {
            string message = SR.Format(SR.DeserializeUnableToConvertValue, propertyType) + $" Path: {path}.";
            throw new JsonException(message, path, null, null, innerException);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ThrowJsonException_DepthTooLarge(int currentDepth, JsonSerializerOptions options)
        {
            throw new JsonException(SR.Format(SR.DepthTooLarge, currentDepth, options.EffectiveMaxDepth));
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ThrowJsonException_SerializationConverterRead(JsonConverter converter)
        {
            var ex = new JsonException(SR.Format(SR.SerializationConverterRead, converter));
            ex.AppendPathInformation = true;
            throw ex;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ThrowJsonException_SerializationConverterWrite(JsonConverter converter)
        {
            var ex = new JsonException(SR.Format(SR.SerializationConverterWrite, converter));
            ex.AppendPathInformation = true;
            throw ex;
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

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ReThrowWithPath(in ReadStack readStack, JsonReaderException ex)
        {
            Debug.Assert(ex.Path == null);

            string path = readStack.JsonPath();
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

            string path = readStack.JsonPath();
            ex.Path = path;

            string message = ex.Message;

            if (string.IsNullOrEmpty(message))
            {
                // Use a default message.
                Type propertyType = readStack.Current.JsonPropertyInfo?.RuntimePropertyType;
                if (propertyType == null)
                {
                    propertyType = readStack.Current.JsonClassInfo.Type;
                }

                message = SR.Format(SR.DeserializeUnableToConvertValue, propertyType);
                ex.AppendPathInformation = true;
            }

            if (ex.AppendPathInformation)
            {
                message += $" Path: {path} | LineNumber: {lineNumber} | BytePositionInLine: {bytePositionInLine}.";
                ex.SetMessage(message);
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
            string path = writeStack.PropertyPath();
            ex.Path = path;

            string message = ex.Message;
            if (string.IsNullOrEmpty(message))
            {
                // Use a default message.
                message = SR.Format(SR.SerializeUnableToSerialize);
                ex.AppendPathInformation = true;
            }

            if (ex.AppendPathInformation)
            {
                message += $" Path: {path}.";
                ex.SetMessage(message);
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
