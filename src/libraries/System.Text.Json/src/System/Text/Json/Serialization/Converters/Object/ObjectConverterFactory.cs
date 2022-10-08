// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text.Json.Reflection;
using System.Text.Json.Serialization.Metadata;

namespace System.Text.Json.Serialization.Converters
{
    /// <summary>
    /// Converter factory for all object-based types (non-enumerable and non-primitive).
    /// </summary>
    [RequiresDynamicCode(JsonSerializer.SerializationRequiresDynamicCodeMessage)]
    internal sealed class ObjectConverterFactory : JsonConverterFactory
    {
        // Need to toggle this behavior when generating converters for F# struct records.
        private readonly bool _useDefaultConstructorInUnannotatedStructs;

        [RequiresUnreferencedCode(JsonSerializer.SerializationUnreferencedCodeMessage)]
        public ObjectConverterFactory(bool useDefaultConstructorInUnannotatedStructs = true)
        {
            _useDefaultConstructorInUnannotatedStructs = useDefaultConstructorInUnannotatedStructs;
        }

        public override bool CanConvert(Type typeToConvert)
        {
            // This is the last built-in factory converter, so if the IEnumerableConverterFactory doesn't
            // support it, then it is not IEnumerable.
            Debug.Assert(!typeof(IEnumerable).IsAssignableFrom(typeToConvert));
            return true;
        }

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
            Justification = "The ctor is marked RequiresUnreferencedCode.")]
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2067:UnrecognizedReflectionPattern",
            Justification = "The ctor is marked RequiresUnreferencedCode.")]
        public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
        {
            if (typeToConvert.IsKeyValuePair())
            {
                return CreateKeyValuePairConverter(typeToConvert);
            }

            JsonConverter converter;
            Type converterType;

            if (!typeToConvert.TryGetDeserializationConstructor(_useDefaultConstructorInUnannotatedStructs, out ConstructorInfo? constructor))
            {
                ThrowHelper.ThrowInvalidOperationException_SerializationDuplicateTypeAttribute<JsonConstructorAttribute>(typeToConvert);
            }

            ParameterInfo[]? parameters = constructor?.GetParameters();

            if (constructor == null || typeToConvert.IsAbstract || parameters!.Length == 0)
            {
                converterType = typeof(ObjectDefaultConverter<>).MakeGenericType(typeToConvert);
            }
            else
            {
                int parameterCount = parameters.Length;

                if (parameterCount <= JsonConstants.UnboxedParameterCountThreshold)
                {
                    Type placeHolderType = JsonTypeInfo.ObjectType;
                    Type[] typeArguments = new Type[JsonConstants.UnboxedParameterCountThreshold + 1];

                    typeArguments[0] = typeToConvert;
                    for (int i = 0; i < JsonConstants.UnboxedParameterCountThreshold; i++)
                    {
                        if (i < parameterCount)
                        {
                            typeArguments[i + 1] = parameters[i].ParameterType;
                        }
                        else
                        {
                            // Use placeholder arguments if there are less args than the threshold.
                            typeArguments[i + 1] = placeHolderType;
                        }
                    }

                    converterType = typeof(SmallObjectWithParameterizedConstructorConverter<,,,,>).MakeGenericType(typeArguments);
                }
                else
                {
                    converterType = typeof(LargeObjectWithParameterizedConstructorConverterWithReflection<>).MakeGenericType(typeToConvert);
                }
            }

            converter = (JsonConverter)Activator.CreateInstance(
                    converterType,
                    BindingFlags.Instance | BindingFlags.Public,
                    binder: null,
                    args: null,
                    culture: null)!;

            converter.ConstructorInfo = constructor!;
            return converter;
        }

        private static JsonConverter CreateKeyValuePairConverter(Type type)
        {
            Debug.Assert(type.IsKeyValuePair());

            Type keyType = type.GetGenericArguments()[0];
            Type valueType = type.GetGenericArguments()[1];

            JsonConverter converter = (JsonConverter)Activator.CreateInstance(
                typeof(KeyValuePairConverter<,>).MakeGenericType(new Type[] { keyType, valueType }),
                BindingFlags.Instance | BindingFlags.Public,
                binder: null,
                args: null,
                culture: null)!;

            return converter;
        }
    }
}
