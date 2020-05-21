// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace System.Text.Json
{
    internal static partial class ThrowHelper
    {
        [DoesNotReturn]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ThrowOutOfMemoryException_BufferMaximumSizeExceeded(uint capacity)
        {
            throw new OutOfMemoryException(SR.Format(SR.BufferMaximumSizeExceeded, capacity));
        }

        [DoesNotReturn]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ThrowArgumentException_DeserializeWrongType(Type type, object value)
        {
            throw new ArgumentException(SR.Format(SR.DeserializeWrongType, type, value.GetType()));
        }

        [DoesNotReturn]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ThrowNotSupportedException_SerializationNotSupported(Type propertyType)
        {
            throw new NotSupportedException(SR.Format(SR.SerializationNotSupportedType, propertyType));
        }

        [DoesNotReturn]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ThrowNotSupportedException_ConstructorMaxOf64Parameters(ConstructorInfo constructorInfo, Type type)
        {
            throw new NotSupportedException(SR.Format(SR.ConstructorMaxOf64Parameters, constructorInfo, type));
        }

        [DoesNotReturn]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ThrowJsonException_DeserializeUnableToConvertValue(Type propertyType)
        {
            var ex = new JsonException(SR.Format(SR.DeserializeUnableToConvertValue, propertyType));
            ex.AppendPathInformation = true;
            throw ex;
        }

        [DoesNotReturn]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ThrowJsonException_SerializationConverterRead(JsonConverter? converter)
        {
            var ex = new JsonException(SR.Format(SR.SerializationConverterRead, converter));
            ex.AppendPathInformation = true;
            throw ex;
        }

        [DoesNotReturn]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ThrowJsonException_SerializationConverterWrite(JsonConverter? converter)
        {
            var ex = new JsonException(SR.Format(SR.SerializationConverterWrite, converter));
            ex.AppendPathInformation = true;
            throw ex;
        }

        [DoesNotReturn]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ThrowJsonException_SerializerCycleDetected(int maxDepth)
        {
            throw new JsonException(SR.Format(SR.SerializerCycleDetected, maxDepth));
        }

        [DoesNotReturn]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ThrowJsonException(string? message = null)
        {
            JsonException ex;
            if (string.IsNullOrEmpty(message))
            {
                ex = new JsonException();
            }
            else
            {
                ex = new JsonException(message);
                ex.AppendPathInformation = true;
            }

            throw ex;
        }

        [DoesNotReturn]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ThrowInvalidOperationException_CannotSerializeInvalidType(Type type, Type? parentClassType, PropertyInfo? propertyInfo)
        {
            if (parentClassType == null)
            {
                Debug.Assert(propertyInfo == null);
                throw new InvalidOperationException(SR.Format(SR.CannotSerializeInvalidType, type));
            }

            Debug.Assert(propertyInfo != null);
            throw new InvalidOperationException(SR.Format(SR.CannotSerializeInvalidMember, type, propertyInfo.Name, parentClassType));
        }

        [DoesNotReturn]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ThrowInvalidOperationException_SerializationConverterNotCompatible(Type converterType, Type type)
        {
            throw new InvalidOperationException(SR.Format(SR.SerializationConverterNotCompatible, converterType, type));
        }

        [DoesNotReturn]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ThrowInvalidOperationException_SerializationConverterOnAttributeInvalid(Type classType, PropertyInfo? propertyInfo)
        {
            string location = classType.ToString();
            if (propertyInfo != null)
            {
                location += $".{propertyInfo.Name}";
            }

            throw new InvalidOperationException(SR.Format(SR.SerializationConverterOnAttributeInvalid, location));
        }

        [DoesNotReturn]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ThrowInvalidOperationException_SerializationConverterOnAttributeNotCompatible(Type classTypeAttributeIsOn, PropertyInfo? propertyInfo, Type typeToConvert)
        {
            string location = classTypeAttributeIsOn.ToString();

            if (propertyInfo != null)
            {
                location += $".{propertyInfo.Name}";
            }

            throw new InvalidOperationException(SR.Format(SR.SerializationConverterOnAttributeNotCompatible, location, typeToConvert));
        }

        [DoesNotReturn]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ThrowInvalidOperationException_SerializerOptionsImmutable()
        {
            throw new InvalidOperationException(SR.SerializerOptionsImmutable);
        }

        [DoesNotReturn]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ThrowInvalidOperationException_SerializerPropertyNameConflict(Type type, JsonPropertyInfo jsonPropertyInfo)
        {
            throw new InvalidOperationException(SR.Format(SR.SerializerPropertyNameConflict, type, jsonPropertyInfo.PropertyInfo?.Name));
        }

        [DoesNotReturn]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ThrowInvalidOperationException_SerializerPropertyNameNull(Type parentType, JsonPropertyInfo jsonPropertyInfo)
        {
            throw new InvalidOperationException(SR.Format(SR.SerializerPropertyNameNull, parentType, jsonPropertyInfo.PropertyInfo?.Name));
        }

        [DoesNotReturn]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ThrowInvalidOperationException_NamingPolicyReturnNull(JsonNamingPolicy namingPolicy)
        {
            throw new InvalidOperationException(SR.Format(SR.NamingPolicyReturnNull, namingPolicy));
        }

        [DoesNotReturn]
        public static void ThrowInvalidOperationException_SerializerConverterFactoryReturnsNull(Type converterType)
        {
            throw new InvalidOperationException(SR.Format(SR.SerializerConverterFactoryReturnsNull, converterType));
        }

        [DoesNotReturn]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ThrowInvalidOperationException_MultiplePropertiesBindToConstructorParameters(
            Type parentType,
            ParameterInfo parameterInfo,
            PropertyInfo firstMatch,
            PropertyInfo secondMatch,
            ConstructorInfo constructorInfo)
        {
            throw new InvalidOperationException(
                SR.Format(
                    SR.MultipleMembersBindWithConstructorParameter,
                    firstMatch.Name,
                    secondMatch.Name,
                    parentType,
                    parameterInfo.Name,
                    constructorInfo));
        }

        [DoesNotReturn]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ThrowInvalidOperationException_ConstructorParameterIncompleteBinding(ConstructorInfo constructorInfo, Type parentType)
        {
            throw new InvalidOperationException(SR.Format(SR.ConstructorParamIncompleteBinding, constructorInfo, parentType));
        }

        [DoesNotReturn]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ThrowInvalidOperationException_ExtensionDataCannotBindToCtorParam(
            PropertyInfo propertyInfo,
            Type classType,
            ConstructorInfo constructorInfo)
        {
            throw new InvalidOperationException(SR.Format(SR.ExtensionDataCannotBindToCtorParam, propertyInfo, classType, constructorInfo));
        }

        [DoesNotReturn]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ThrowInvalidOperationException_JsonIncludeOnNonPublicInvalid(PropertyInfo propertyInfo, Type parentType)
        {
            throw new InvalidOperationException(SR.Format(SR.JsonIncludeOnNonPublicInvalid, propertyInfo.Name, parentType));
        }

        [DoesNotReturn]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ThrowNotSupportedException_ObjectWithParameterizedCtorRefMetadataNotHonored(
            ReadOnlySpan<byte> propertyName,
            ref Utf8JsonReader reader,
            ref ReadStack state)
        {
            state.Current.JsonPropertyName = propertyName.ToArray();

            NotSupportedException ex = new NotSupportedException(
                SR.Format(SR.ObjectWithParameterizedCtorRefMetadataNotHonored, state.Current.JsonClassInfo.Type));
            ThrowNotSupportedException(state, reader, ex);
        }

        [DoesNotReturn]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ReThrowWithPath(in ReadStack state, JsonReaderException ex)
        {
            Debug.Assert(ex.Path == null);

            string path = state.JsonPath();
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

        [DoesNotReturn]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ReThrowWithPath(in ReadStack state, in Utf8JsonReader reader, Exception ex)
        {
            JsonException jsonException = new JsonException(null, ex);
            AddJsonExceptionInformation(state, reader, jsonException);
            throw jsonException;
        }

        public static void AddJsonExceptionInformation(in ReadStack state, in Utf8JsonReader reader, JsonException ex)
        {
            long lineNumber = reader.CurrentState._lineNumber;
            ex.LineNumber = lineNumber;

            long bytePositionInLine = reader.CurrentState._bytePositionInLine;
            ex.BytePositionInLine = bytePositionInLine;

            string path = state.JsonPath();
            ex.Path = path;

            string? message = ex._message;

            if (string.IsNullOrEmpty(message))
            {
                // Use a default message.
                Type? propertyType = state.Current.JsonPropertyInfo?.RuntimePropertyType;
                if (propertyType == null)
                {
                    propertyType = state.Current.JsonClassInfo?.Type;
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

        [DoesNotReturn]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ReThrowWithPath(in WriteStack state, Exception ex)
        {
            JsonException jsonException = new JsonException(null, ex);
            AddJsonExceptionInformation(state, jsonException);
            throw jsonException;
        }

        public static void AddJsonExceptionInformation(in WriteStack state, JsonException ex)
        {
            string path = state.PropertyPath();
            ex.Path = path;

            string? message = ex._message;
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

        [DoesNotReturn]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ThrowInvalidOperationException_SerializationDuplicateAttribute(Type attribute, Type classType, PropertyInfo? propertyInfo)
        {
            string location = classType.ToString();
            if (propertyInfo != null)
            {
                location += $".{propertyInfo.Name}";
            }

            throw new InvalidOperationException(SR.Format(SR.SerializationDuplicateAttribute, attribute, location));
        }

        [DoesNotReturn]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ThrowInvalidOperationException_SerializationDuplicateTypeAttribute(Type classType, Type attribute)
        {
            throw new InvalidOperationException(SR.Format(SR.SerializationDuplicateTypeAttribute, classType, attribute));
        }

        [DoesNotReturn]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ThrowInvalidOperationException_SerializationDuplicateTypeAttribute<TAttribute>(Type classType)
        {
            throw new InvalidOperationException(SR.Format(SR.SerializationDuplicateTypeAttribute, classType, typeof(Attribute)));
        }

        [DoesNotReturn]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ThrowInvalidOperationException_SerializationDataExtensionPropertyInvalid(Type type, JsonPropertyInfo jsonPropertyInfo)
        {
            throw new InvalidOperationException(SR.Format(SR.SerializationDataExtensionPropertyInvalid, type, jsonPropertyInfo.PropertyInfo?.Name));
        }

        [DoesNotReturn]
        public static void ThrowNotSupportedException(in ReadStack state, in Utf8JsonReader reader, NotSupportedException ex)
        {
            string message = ex.Message;

            // The caller should check to ensure path is not already set.
            Debug.Assert(!message.Contains(" Path: "));

            // Obtain the type to show in the message.
            Type? propertyType = state.Current.JsonPropertyInfo?.RuntimePropertyType;
            if (propertyType == null)
            {
                propertyType = state.Current.JsonClassInfo.Type;
            }

            if (!message.Contains(propertyType.ToString()))
            {
                if (message.Length > 0)
                {
                    message += " ";
                }

                message += SR.Format(SR.SerializationNotSupportedParentType, propertyType);
            }

            long lineNumber = reader.CurrentState._lineNumber;
            long bytePositionInLine = reader.CurrentState._bytePositionInLine;
            message += $" Path: {state.JsonPath()} | LineNumber: {lineNumber} | BytePositionInLine: {bytePositionInLine}.";

            throw new NotSupportedException(message, ex);
        }

        [DoesNotReturn]
        public static void ThrowNotSupportedException(in WriteStack state, NotSupportedException ex)
        {
            string message = ex.Message;

            // The caller should check to ensure path is not already set.
            Debug.Assert(!message.Contains(" Path: "));

            // Obtain the type to show in the message.
            Type? propertyType = state.Current.DeclaredJsonPropertyInfo?.RuntimePropertyType;
            if (propertyType == null)
            {
                propertyType = state.Current.JsonClassInfo.Type;
            }

            if (!message.Contains(propertyType.ToString()))
            {
                if (message.Length > 0)
                {
                    message += " ";
                }

                message += SR.Format(SR.SerializationNotSupportedParentType, propertyType);
            }

            message += $" Path: {state.PropertyPath()}.";

            throw new NotSupportedException(message, ex);
        }

        [DoesNotReturn]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ThrowNotSupportedException_DeserializeNoConstructor(Type type, ref Utf8JsonReader reader, ref ReadStack state)
        {
            string message;

            if (type.IsInterface)
            {
                message = SR.Format(SR.DeserializePolymorphicInterface, type);
            }
            else
            {
                message = SR.Format(SR.DeserializeNoConstructor, nameof(JsonConstructorAttribute), type);
            }

            ThrowNotSupportedException(state, reader, new NotSupportedException(message));
        }

        [DoesNotReturn]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ThrowNotSupportedException_CannotPopulateCollection(Type type, ref Utf8JsonReader reader, ref ReadStack state)
        {
            ThrowNotSupportedException(state, reader, new NotSupportedException(SR.Format(SR.CannotPopulateCollection, type)));
        }

        [DoesNotReturn]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ThrowJsonException_MetadataValuesInvalidToken(JsonTokenType tokenType)
        {
            ThrowJsonException(SR.Format(SR.MetadataInvalidTokenAfterValues, tokenType));
        }

        [DoesNotReturn]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ThrowJsonException_MetadataReferenceNotFound(string id)
        {
            ThrowJsonException(SR.Format(SR.MetadataReferenceNotFound, id));
        }

        [DoesNotReturn]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ThrowJsonException_MetadataValueWasNotString(JsonTokenType tokenType)
        {
            ThrowJsonException(SR.Format(SR.MetadataValueWasNotString, tokenType));
        }

        [DoesNotReturn]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ThrowJsonException_MetadataReferenceObjectCannotContainOtherProperties(ReadOnlySpan<byte> propertyName, ref ReadStack state)
        {
            state.Current.JsonPropertyName = propertyName.ToArray();
            ThrowJsonException(SR.MetadataReferenceCannotContainOtherProperties);
        }

        [DoesNotReturn]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ThrowJsonException_MetadataIdIsNotFirstProperty(ReadOnlySpan<byte> propertyName, ref ReadStack state)
        {
            state.Current.JsonPropertyName = propertyName.ToArray();
            ThrowJsonException(SR.MetadataIdIsNotFirstProperty);
        }

        [DoesNotReturn]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ThrowJsonException_MetadataMissingIdBeforeValues()
        {
            ThrowJsonException(SR.MetadataPreservedArrayPropertyNotFound);
        }

        [DoesNotReturn]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ThrowJsonException_MetadataInvalidPropertyWithLeadingDollarSign(ReadOnlySpan<byte> propertyName, ref ReadStack state, in Utf8JsonReader reader)
        {
            // Set PropertyInfo or KeyName to write down the conflicting property name in JsonException.Path
            if (state.Current.IsProcessingDictionary())
            {
                state.Current.JsonPropertyNameAsString = reader.GetString();
            }
            else
            {
                state.Current.JsonPropertyName = propertyName.ToArray();
            }

            ThrowJsonException(SR.MetadataInvalidPropertyWithLeadingDollarSign);
        }

        [DoesNotReturn]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ThrowJsonException_MetadataDuplicateIdFound(string id, ref ReadStack state)
        {
            // Set so JsonPath throws exception with $id in it.
            state.Current.JsonPropertyName = JsonSerializer.s_metadataId.EncodedUtf8Bytes.ToArray();

            ThrowJsonException(SR.Format(SR.MetadataDuplicateIdFound, id));
        }

        [DoesNotReturn]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ThrowJsonException_MetadataInvalidReferenceToValueType(Type propertyType)
        {
            ThrowJsonException(SR.Format(SR.MetadataInvalidReferenceToValueType, propertyType));
        }

        [DoesNotReturn]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ThrowJsonException_MetadataPreservedArrayInvalidProperty(Type propertyType, in Utf8JsonReader reader)
        {
            string propertyName = reader.GetString()!;

            ThrowJsonException(SR.Format(SR.MetadataPreservedArrayFailed,
                SR.Format(SR.MetadataPreservedArrayInvalidProperty, propertyName),
                SR.Format(SR.DeserializeUnableToConvertValue, propertyType)));
        }

        [DoesNotReturn]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ThrowJsonException_MetadataPreservedArrayValuesNotFound(Type propertyType)
        {
            ThrowJsonException(SR.Format(SR.MetadataPreservedArrayFailed,
                SR.MetadataPreservedArrayPropertyNotFound,
                SR.Format(SR.DeserializeUnableToConvertValue, propertyType)));
        }

        [DoesNotReturn]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ThrowJsonException_MetadataCannotParsePreservedObjectIntoImmutable(Type propertyType)
        {
            ThrowJsonException(SR.Format(SR.MetadataCannotParsePreservedObjectToImmutable, propertyType));
        }

        [DoesNotReturn]
        internal static void ThrowUnexpectedMetadataException(
            ReadOnlySpan<byte> propertyName,
            ref Utf8JsonReader reader,
            ref ReadStack state)
        {
            if (state.Current.JsonClassInfo.PropertyInfoForClassInfo.ConverterBase.ConstructorIsParameterized)
            {
                ThrowNotSupportedException_ObjectWithParameterizedCtorRefMetadataNotHonored(propertyName, ref reader, ref state);
            }

            MetadataPropertyName name = JsonSerializer.GetMetadataPropertyName(propertyName);
            if (name == MetadataPropertyName.Id)
            {
                ThrowJsonException_MetadataIdIsNotFirstProperty(propertyName, ref state);
            }
            else if (name == MetadataPropertyName.Ref)
            {
                ThrowJsonException_MetadataReferenceObjectCannotContainOtherProperties(propertyName, ref state);
            }
            else
            {
                ThrowJsonException_MetadataInvalidPropertyWithLeadingDollarSign(propertyName, ref state, reader);
            }
        }
    }
}
