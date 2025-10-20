// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text.Json.Serialization.Metadata;

namespace System.Text.Json.Serialization.Converters
{
    /// <summary>
    /// Converter for System.Tuple and System.ValueTuple types that serializes them as objects with Item1, Item2, etc. properties.
    /// Handles long tuples (&gt; 7 elements) by flattening the Rest field.
    /// </summary>
    internal sealed class TupleConverter<T> : JsonObjectConverter<T>
    {
        private readonly List<(string Name, Type Type, Func<T, object?> Getter)> _elements;

        internal override bool CanHaveMetadata => false;
        internal override bool SupportsCreateObjectDelegate => false;

        public TupleConverter()
        {
            _elements = new List<(string, Type, Func<T, object?>)>();
            PopulateTupleElements(typeof(T), _elements, 0);
        }

        private static void PopulateTupleElements(Type tupleType, List<(string, Type, Func<T, object?>)> elements, int offset)
        {
            if (tupleType.IsValueTuple())
            {
                PopulateValueTupleElements(tupleType, elements, offset);
            }
            else if (tupleType.IsTuple())
            {
                PopulateReferenceTupleElements(tupleType, elements, offset);
            }
        }

        private static void PopulateValueTupleElements(Type tupleType, List<(string, Type, Func<T, object?>)> elements, int offset)
        {
            FieldInfo[] fields = tupleType.GetFields(BindingFlags.Public | BindingFlags.Instance);
            
            foreach (FieldInfo field in fields)
            {
                if (field.Name == "Rest")
                {
                    // Handle long tuple (> 7 elements) - recursively flatten the Rest field
                    Type restType = field.FieldType;
                    PopulateTupleElements(restType, elements, offset);
                }
                else if (field.Name.StartsWith("Item", StringComparison.Ordinal))
                {
                    string itemName = $"Item{offset + elements.Count + 1}";
                    Type fieldType = field.FieldType;
                    Func<T, object?> getter = (T tuple) => field.GetValue(tuple);
                    elements.Add((itemName, fieldType, getter));
                }
            }
        }

        private static void PopulateReferenceTupleElements(Type tupleType, List<(string, Type, Func<T, object?>)> elements, int offset)
        {
            PropertyInfo[] properties = tupleType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            
            foreach (PropertyInfo property in properties)
            {
                if (property.Name == "Rest")
                {
                    // Handle long tuple (> 7 elements) - recursively flatten the Rest property
                    Type restType = property.PropertyType;
                    PropertyInfo restProp = property;
                    
                    // For System.Tuple, we need to handle Rest differently since it's accessed through properties
                    if (restType.IsValueTuple() || restType.IsTuple())
                    {
                        // Create nested getters for Rest elements
                        PopulateNestedReferenceTupleElements(restType, elements, offset, restProp);
                    }
                }
                else if (property.Name.StartsWith("Item", StringComparison.Ordinal))
                {
                    string itemName = $"Item{offset + elements.Count + 1}";
                    Type propertyType = property.PropertyType;
                    Func<T, object?> getter = (T tuple) => property.GetValue(tuple);
                    elements.Add((itemName, propertyType, getter));
                }
            }
        }

        private static void PopulateNestedReferenceTupleElements(Type tupleType, List<(string, Type, Func<T, object?>)> elements, int offset, PropertyInfo restProperty)
        {
            PropertyInfo[] properties = tupleType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            
            foreach (PropertyInfo property in properties)
            {
                if (property.Name == "Rest")
                {
                    // Further nesting
                    Type nestedRestType = property.PropertyType;
                    if (nestedRestType.IsValueTuple() || nestedRestType.IsTuple())
                    {
                        // Chain the getters
                        PropertyInfo innerRestProp = property;
                        Func<T, object?> baseGetter = (T tuple) => restProperty.GetValue(tuple);
                        PopulateNestedReferenceTupleElementsChained(nestedRestType, elements, offset, baseGetter, innerRestProp);
                    }
                }
                else if (property.Name.StartsWith("Item", StringComparison.Ordinal))
                {
                    string itemName = $"Item{offset + elements.Count + 1}";
                    Type propertyType = property.PropertyType;
                    PropertyInfo prop = property;
                    Func<T, object?> getter = (T tuple) =>
                    {
                        object? restValue = restProperty.GetValue(tuple);
                        return restValue is not null ? prop.GetValue(restValue) : null;
                    };
                    elements.Add((itemName, propertyType, getter));
                }
            }
        }

        private static void PopulateNestedReferenceTupleElementsChained(Type tupleType, List<(string, Type, Func<T, object?>)> elements, int offset, Func<T, object?> baseGetter, PropertyInfo currentRestProp)
        {
            PropertyInfo[] properties = tupleType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            
            foreach (PropertyInfo property in properties)
            {
                if (property.Name == "Rest")
                {
                    // Further nesting - would be very rare
                    Type nestedRestType = property.PropertyType;
                    if (nestedRestType.IsValueTuple() || nestedRestType.IsTuple())
                    {
                        PropertyInfo innerRestProp = property;
                        Func<T, object?> chainedGetter = (T tuple) =>
                        {
                            object? restValue = baseGetter(tuple);
                            return restValue is not null ? currentRestProp.GetValue(restValue) : null;
                        };
                        PopulateNestedReferenceTupleElementsChained(nestedRestType, elements, offset, chainedGetter, innerRestProp);
                    }
                }
                else if (property.Name.StartsWith("Item", StringComparison.Ordinal))
                {
                    string itemName = $"Item{offset + elements.Count + 1}";
                    Type propertyType = property.PropertyType;
                    PropertyInfo prop = property;
                    Func<T, object?> getter = (T tuple) =>
                    {
                        object? restValue = baseGetter(tuple);
                        if (restValue is not null)
                        {
                            object? currentRest = currentRestProp.GetValue(restValue);
                            return currentRest is not null ? prop.GetValue(currentRest) : null;
                        }
                        return null;
                    };
                    elements.Add((itemName, propertyType, getter));
                }
            }
        }

        internal override bool OnTryRead(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options, scoped ref ReadStack state, [MaybeNullWhen(false)] out T value)
        {
            // Deserialization of tuples as objects is not supported
            ThrowHelper.ThrowNotSupportedException_DeserializeNoConstructor(state.Current.JsonTypeInfo, ref reader, ref state);
            value = default;
            return false;
        }

        internal override bool OnTryWrite(Utf8JsonWriter writer, T value, JsonSerializerOptions options, ref WriteStack state)
        {
            writer.WriteStartObject();

            foreach (var (name, elementType, getter) in _elements)
            {
                object? elementValue = getter(value);
                
                writer.WritePropertyName(name);
                
                JsonConverter elementConverter = options.GetConverterInternal(elementType);
                if (elementConverter is null)
                {
                    throw new JsonException($"No converter found for type {elementType}");
                }

                elementConverter.WriteAsObject(writer, elementValue, options, ref state);
            }

            writer.WriteEndObject();
            return true;
        }

        public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            throw new NotSupportedException("Deserialization of tuples is not supported.");
        }

        public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();

            foreach (var (name, elementType, getter) in _elements)
            {
                object? elementValue = getter(value);
                
                writer.WritePropertyName(name);
                
                JsonConverter elementConverter = options.GetConverterInternal(elementType);
                if (elementConverter is null)
                {
                    throw new JsonException($"No converter found for type {elementType}");
                }

                elementConverter.WriteAsObject(writer, elementValue, options);
            }

            writer.WriteEndObject();
        }
    }
}
