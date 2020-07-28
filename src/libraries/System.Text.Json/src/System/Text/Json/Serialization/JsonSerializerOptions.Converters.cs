// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Converters;

namespace System.Text.Json
{
    /// <summary>
    /// Provides options to be used with <see cref="JsonSerializer"/>.
    /// </summary>
    public sealed partial class JsonSerializerOptions
    {
        // The global list of built-in simple converters.
        private static readonly Dictionary<Type, JsonConverter> s_defaultSimpleConverters = GetDefaultSimpleConverters();

        // The global list of built-in converters that override CanConvert().
        private static readonly JsonConverter[] s_defaultFactoryConverters = new JsonConverter[]
        {
            // Nullable converter should always be first since it forwards to any nullable type.
            new NullableConverterFactory(),
            new EnumConverterFactory(),
            // IEnumerable should always be second to last since they can convert any IEnumerable.
            new IEnumerableConverterFactory(),
            // Object should always be last since it converts any type.
            new ObjectConverterFactory()
        };

        // The cached converters (custom or built-in).
        private readonly ConcurrentDictionary<Type, JsonConverter?> _converters = new ConcurrentDictionary<Type, JsonConverter?>();

        private static Dictionary<Type, JsonConverter> GetDefaultSimpleConverters()
        {
            const int NumberOfSimpleConverters = 23;
            var converters = new Dictionary<Type, JsonConverter>(NumberOfSimpleConverters);

            // Use a dictionary for simple converters.
            // When adding to this, update NumberOfSimpleConverters above.
            Add(new BooleanConverter());
            Add(new ByteConverter());
            Add(new ByteArrayConverter());
            Add(new CharConverter());
            Add(new DateTimeConverter());
            Add(new DateTimeOffsetConverter());
            Add(new DoubleConverter());
            Add(new DecimalConverter());
            Add(new GuidConverter());
            Add(new Int16Converter());
            Add(new Int32Converter());
            Add(new Int64Converter());
            Add(new JsonElementConverter());
            Add(new JsonDocumentConverter());
            Add(new ObjectConverter());
            Add(new SByteConverter());
            Add(new SingleConverter());
            Add(new StringConverter());
            Add(new TypeConverter());
            Add(new UInt16Converter());
            Add(new UInt32Converter());
            Add(new UInt64Converter());
            Add(new UriConverter());

            Debug.Assert(NumberOfSimpleConverters == converters.Count);

            return converters;

            void Add(JsonConverter converter) =>
                converters.Add(converter.TypeToConvert, converter);
        }

        internal JsonConverter GetDictionaryKeyConverter(Type keyType)
        {
            _dictionaryKeyConverters ??= GetDictionaryKeyConverters();

            if (!_dictionaryKeyConverters.TryGetValue(keyType, out JsonConverter? converter))
            {
                if (keyType.IsEnum)
                {
                    converter = GetEnumConverter();
                    _dictionaryKeyConverters[keyType] = converter;
                }
                else
                {
                    ThrowHelper.ThrowNotSupportedException_DictionaryKeyTypeNotSupported(keyType);
                }
            }

            return converter!;

            // Use factory pattern to generate an EnumConverter with AllowStrings and AllowNumbers options for dictionary keys.
            // There will be one converter created for each enum type.
            JsonConverter GetEnumConverter()
                => (JsonConverter)Activator.CreateInstance(
                        typeof(EnumConverter<>).MakeGenericType(keyType),
                        BindingFlags.Instance | BindingFlags.Public,
                        binder: null,
                        new object[] { EnumConverterOptions.AllowStrings | EnumConverterOptions.AllowNumbers, this },
                        culture: null)!;
        }

        private ConcurrentDictionary<Type, JsonConverter>? _dictionaryKeyConverters;

        private static ConcurrentDictionary<Type, JsonConverter> GetDictionaryKeyConverters()
        {
            const int NumberOfConverters = 18;
            var converters = new ConcurrentDictionary<Type, JsonConverter>(Environment.ProcessorCount, NumberOfConverters);

            // When adding to this, update NumberOfConverters above.
            Add(s_defaultSimpleConverters[typeof(bool)]);
            Add(s_defaultSimpleConverters[typeof(byte)]);
            Add(s_defaultSimpleConverters[typeof(char)]);
            Add(s_defaultSimpleConverters[typeof(DateTime)]);
            Add(s_defaultSimpleConverters[typeof(DateTimeOffset)]);
            Add(s_defaultSimpleConverters[typeof(double)]);
            Add(s_defaultSimpleConverters[typeof(decimal)]);
            Add(s_defaultSimpleConverters[typeof(Guid)]);
            Add(s_defaultSimpleConverters[typeof(short)]);
            Add(s_defaultSimpleConverters[typeof(int)]);
            Add(s_defaultSimpleConverters[typeof(long)]);
            Add(s_defaultSimpleConverters[typeof(object)]);
            Add(s_defaultSimpleConverters[typeof(sbyte)]);
            Add(s_defaultSimpleConverters[typeof(float)]);
            Add(s_defaultSimpleConverters[typeof(string)]);
            Add(s_defaultSimpleConverters[typeof(ushort)]);
            Add(s_defaultSimpleConverters[typeof(uint)]);
            Add(s_defaultSimpleConverters[typeof(ulong)]);

            Debug.Assert(NumberOfConverters == converters.Count);

            return converters;

            void Add(JsonConverter converter) =>
                converters[converter.TypeToConvert] = converter;
        }

        /// <summary>
        /// The list of custom converters.
        /// </summary>
        /// <remarks>
        /// Once serialization or deserialization occurs, the list cannot be modified.
        /// </remarks>
        public IList<JsonConverter> Converters { get; }

        internal JsonConverter DetermineConverter(Type? parentClassType, Type runtimePropertyType, MemberInfo? memberInfo)
        {
            JsonConverter converter = null!;

            // Priority 1: attempt to get converter from JsonConverterAttribute on property.
            if (memberInfo != null)
            {
                Debug.Assert(parentClassType != null);

                JsonConverterAttribute? converterAttribute = (JsonConverterAttribute?)
                    GetAttributeThatCanHaveMultiple(parentClassType!, typeof(JsonConverterAttribute), memberInfo);

                if (converterAttribute != null)
                {
                    converter = GetConverterFromAttribute(converterAttribute, typeToConvert: runtimePropertyType, classTypeAttributeIsOn: parentClassType!, memberInfo);
                }
            }

            if (converter == null)
            {
                converter = GetConverter(runtimePropertyType);
                Debug.Assert(converter != null);
            }

            if (converter is JsonConverterFactory factory)
            {
                converter = factory.GetConverterInternal(runtimePropertyType, this);

                // A factory cannot return null; GetConverterInternal checked for that.
                Debug.Assert(converter != null);
            }

            return converter;
        }

        /// <summary>
        /// Returns the converter for the specified type.
        /// </summary>
        /// <param name="typeToConvert">The type to return a converter for.</param>
        /// <returns>
        /// The converter for the given type.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// The configured <see cref="JsonConverter"/> for <paramref name="typeToConvert"/> returned an invalid converter.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// There is no compatible <see cref="System.Text.Json.Serialization.JsonConverter"/>
        /// for <paramref name="typeToConvert"/> or its serializable members.
        /// </exception>
        public JsonConverter GetConverter(Type typeToConvert)
        {
            if (_converters.TryGetValue(typeToConvert, out JsonConverter? converter))
            {
                Debug.Assert(converter != null);
                return converter;
            }

            // Priority 2: Attempt to get custom converter added at runtime.
            // Currently there is not a way at runtime to overide the [JsonConverter] when applied to a property.
            foreach (JsonConverter item in Converters)
            {
                if (item.CanConvert(typeToConvert))
                {
                    converter = item;
                    break;
                }
            }

            // Priority 3: Attempt to get converter from [JsonConverter] on the type being converted.
            if (converter == null)
            {
                JsonConverterAttribute? converterAttribute = (JsonConverterAttribute?)
                    GetAttributeThatCanHaveMultiple(typeToConvert, typeof(JsonConverterAttribute));

                if (converterAttribute != null)
                {
                    converter = GetConverterFromAttribute(converterAttribute, typeToConvert: typeToConvert, classTypeAttributeIsOn: typeToConvert, memberInfo: null);
                }
            }

            // Priority 4: Attempt to get built-in converter.
            if (converter == null)
            {
                if (s_defaultSimpleConverters.TryGetValue(typeToConvert, out JsonConverter? foundConverter))
                {
                    Debug.Assert(foundConverter != null);
                    converter = foundConverter;
                }
                else
                {
                    foreach (JsonConverter item in s_defaultFactoryConverters)
                    {
                        if (item.CanConvert(typeToConvert))
                        {
                            converter = item;
                            break;
                        }
                    }

                    // Since the object and IEnumerable converters cover all types, we should have a converter.
                    Debug.Assert(converter != null);
                }
            }

            // Allow redirection for generic types or the enum converter.
            if (converter is JsonConverterFactory factory)
            {
                converter = factory.GetConverterInternal(typeToConvert, this);

                // A factory cannot return null; GetConverterInternal checked for that.
                Debug.Assert(converter != null);
            }

            Type converterTypeToConvert = converter.TypeToConvert;

            if (!converterTypeToConvert.IsAssignableFrom(typeToConvert) &&
                !typeToConvert.IsAssignableFrom(converterTypeToConvert))
            {
                ThrowHelper.ThrowInvalidOperationException_SerializationConverterNotCompatible(converter.GetType(), typeToConvert);
            }

            // Only cache the value once (de)serialization has occurred since new converters can be added that may change the result.
            if (_haveTypesBeenCreated)
            {
                // A null converter is allowed here and cached.

                // Ignore failure case here in multi-threaded cases since the cached item will be equivalent.
                _converters.TryAdd(typeToConvert, converter);
            }

            return converter;
        }

        private JsonConverter GetConverterFromAttribute(JsonConverterAttribute converterAttribute, Type typeToConvert, Type classTypeAttributeIsOn, MemberInfo? memberInfo)
        {
            JsonConverter? converter;

            Type? type = converterAttribute.ConverterType;
            if (type == null)
            {
                // Allow the attribute to create the converter.
                converter = converterAttribute.CreateConverter(typeToConvert);
                if (converter == null)
                {
                    ThrowHelper.ThrowInvalidOperationException_SerializationConverterOnAttributeNotCompatible(classTypeAttributeIsOn, memberInfo, typeToConvert);
                }
            }
            else
            {
                ConstructorInfo? ctor = type.GetConstructor(Type.EmptyTypes);
                if (!typeof(JsonConverter).IsAssignableFrom(type) || ctor == null || !ctor.IsPublic)
                {
                    ThrowHelper.ThrowInvalidOperationException_SerializationConverterOnAttributeInvalid(classTypeAttributeIsOn, memberInfo);
                }

                converter = (JsonConverter)Activator.CreateInstance(type)!;
            }

            Debug.Assert(converter != null);
            if (!converter.CanConvert(typeToConvert))
            {
                Type? underlyingType = Nullable.GetUnderlyingType(typeToConvert);
                if (underlyingType != null && converter.CanConvert(underlyingType))
                {
                    // Allow nullable handling to forward to the underlying type's converter.
                    return NullableConverterFactory.CreateValueConverter(underlyingType, converter);
                }

                ThrowHelper.ThrowInvalidOperationException_SerializationConverterOnAttributeNotCompatible(classTypeAttributeIsOn, memberInfo, typeToConvert);
            }

            return converter;
        }

        private static Attribute? GetAttributeThatCanHaveMultiple(Type classType, Type attributeType, MemberInfo memberInfo)
        {
            object[] attributes = memberInfo.GetCustomAttributes(attributeType, inherit: false);
            return GetAttributeThatCanHaveMultiple(attributeType, classType, memberInfo, attributes);
        }

        internal static Attribute? GetAttributeThatCanHaveMultiple(Type classType, Type attributeType)
        {
            object[] attributes = classType.GetCustomAttributes(attributeType, inherit: false);
            return GetAttributeThatCanHaveMultiple(attributeType, classType, null, attributes);
        }

        private static Attribute? GetAttributeThatCanHaveMultiple(Type attributeType, Type classType, MemberInfo? memberInfo, object[] attributes)
        {
            if (attributes.Length == 0)
            {
                return null;
            }

            if (attributes.Length == 1)
            {
                return (Attribute)attributes[0];
            }

            ThrowHelper.ThrowInvalidOperationException_SerializationDuplicateAttribute(attributeType, classType, memberInfo);
            return default;
        }
    }
}
