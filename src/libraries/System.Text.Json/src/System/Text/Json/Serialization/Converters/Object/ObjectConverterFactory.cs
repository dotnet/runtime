// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text.Json.Serialization.Metadata;

namespace System.Text.Json.Serialization.Converters
{
    /// <summary>
    /// Converter factory for all object-based types (non-enumerable and non-primitive).
    /// </summary>
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

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2067:UnrecognizedReflectionPattern",
            Justification = "The ctor is marked RequiresUnreferencedCode.")]
        public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
        {
            if (IsKeyValuePair(typeToConvert))
            {
                return CreateKeyValuePairConverter(typeToConvert, options);
            }

            JsonConverter converter;
            Type converterType;

            ConstructorInfo? constructor = GetDeserializationConstructor(typeToConvert);
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
                    converterType = typeof(LargeObjectWithParameterizedConstructorConverter<>).MakeGenericType(typeToConvert);
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

        private bool IsKeyValuePair(Type typeToConvert)
        {
            if (!typeToConvert.IsGenericType)
                return false;

            Type generic = typeToConvert.GetGenericTypeDefinition();
            return (generic == typeof(KeyValuePair<,>));
        }

        private JsonConverter CreateKeyValuePairConverter(Type type, JsonSerializerOptions options)
        {
            Debug.Assert(IsKeyValuePair(type));

            Type keyType = type.GetGenericArguments()[0];
            Type valueType = type.GetGenericArguments()[1];

            JsonConverter converter = (JsonConverter)Activator.CreateInstance(
                typeof(KeyValuePairConverter<,>).MakeGenericType(new Type[] { keyType, valueType }),
                BindingFlags.Instance | BindingFlags.Public,
                binder: null,
                args: null,
                culture: null)!;

            converter.Initialize(options);

            return converter;
        }

        private ConstructorInfo? GetDeserializationConstructor(
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)] Type type)
        {
            ConstructorInfo? ctorWithAttribute = null;
            ConstructorInfo? publicParameterlessCtor = null;
            ConstructorInfo? lonePublicCtor = null;

            ConstructorInfo[] constructors = type.GetConstructors(BindingFlags.Public | BindingFlags.Instance);

            if (constructors.Length == 1)
            {
                lonePublicCtor = constructors[0];
            }

            foreach (ConstructorInfo constructor in constructors)
            {
                if (constructor.GetCustomAttribute<JsonConstructorAttribute>() != null)
                {
                    if (ctorWithAttribute != null)
                    {
                        ThrowHelper.ThrowInvalidOperationException_SerializationDuplicateTypeAttribute<JsonConstructorAttribute>(type);
                    }

                    ctorWithAttribute = constructor;
                }
                else if (constructor.GetParameters().Length == 0)
                {
                    publicParameterlessCtor = constructor;
                }
            }

            // For correctness, throw if multiple ctors have [JsonConstructor], even if one or more are non-public.
            ConstructorInfo? dummyCtorWithAttribute = ctorWithAttribute;

            constructors = type.GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance);
            foreach (ConstructorInfo constructor in constructors)
            {
                if (constructor.GetCustomAttribute<JsonConstructorAttribute>() != null)
                {
                    if (dummyCtorWithAttribute != null)
                    {
                        ThrowHelper.ThrowInvalidOperationException_SerializationDuplicateTypeAttribute<JsonConstructorAttribute>(type);
                    }

                    dummyCtorWithAttribute = constructor;
                }
            }

            // Structs will use default constructor if attribute isn't used.
            if (_useDefaultConstructorInUnannotatedStructs && type.IsValueType && ctorWithAttribute == null)
            {
                return null;
            }

            return ctorWithAttribute ?? publicParameterlessCtor ?? lonePublicCtor;
        }
    }
}
