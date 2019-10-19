// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization.Converters;

namespace System.Text.Json
{
    [DebuggerDisplay("ClassType.{JsonClassInfo.ClassType}, {JsonClassInfo.Type.Name}")]
    internal struct ReadStackFrame
    {
        // The object (POCO or IEnumerable) that is being populated
        public object ReturnValue;
        public JsonClassInfo JsonClassInfo;

        // Support Dictionary keys.
        public string KeyName;

        // Support JSON Path on exceptions.
        public byte[] JsonPropertyName;

        // Current property values.
        public JsonPropertyInfo JsonPropertyInfo;

        // Delegate used to add elements to the current property.
        public object AddObjectToEnumerable;

        // Support System.Array and other types that don't implement IList.
        public IList TempEnumerableValues;

        // Has an array or dictionary property been initialized.
        public bool CollectionPropertyInitialized;

        // The current JSON data for a property does not match a given POCO, so ignore the property (recursively).
        public bool Drain;

        // Support IDictionary constructible types, i.e. types that we
        // support by passing and IDictionary to their constructors:
        // immutable dictionaries, Hashtable, SortedList
        public IDictionary TempDictionaryValues;

        // For performance, we order the properties by the first deserialize and PropertyIndex helps find the right slot quicker.
        public int PropertyIndex;
        public List<PropertyRef> PropertyRefCache;

        /// <summary>
        /// Is the current object an Enumerable or Dictionary.
        /// </summary>
        public bool IsProcessingCollectionObject()
        {
            return IsProcessingObject(ClassType.Enumerable | ClassType.Dictionary);
        }

        /// <summary>
        /// Is the current property an Enumerable or Dictionary.
        /// </summary>
        public bool IsProcessingCollectionProperty()
        {
            return IsProcessingProperty(ClassType.Enumerable | ClassType.Dictionary);
        }

        /// <summary>
        /// Is the current object or property an Enumerable or Dictionary.
        /// </summary>
        public bool IsProcessingCollection()
        {
            return IsProcessingObject(ClassType.Enumerable | ClassType.Dictionary) ||
                IsProcessingProperty(ClassType.Enumerable | ClassType.Dictionary);
        }

        /// <summary>
        /// Is the current object or property a Dictionary.
        /// </summary>
        public bool IsProcessingDictionary()
        {
            return IsProcessingObject(ClassType.Dictionary) ||
                IsProcessingProperty(ClassType.Dictionary);
        }

        /// <summary>
        /// Is the current object or property an Enumerable.
        /// </summary>
        public bool IsProcessingEnumerable()
        {
            return IsProcessingObject(ClassType.Enumerable) ||
                IsProcessingProperty(ClassType.Enumerable);
        }

        /// <summary>
        /// Is the current object of the provided <paramref name="classTypes"/>.
        /// </summary>
        public bool IsProcessingObject(ClassType classTypes)
        {
            return (JsonClassInfo.ClassType & classTypes) != 0;
        }

        /// <summary>
        /// Is the current property of the provided <paramref name="classTypes"/>.
        /// </summary>
        public bool IsProcessingProperty(ClassType classTypes)
        {
            return JsonPropertyInfo != null &&
                !JsonPropertyInfo.IsPropertyPolicy &&
                (JsonPropertyInfo.ClassType & classTypes) != 0;
        }

        /// <summary>
        /// Determine whether a StartObject or StartArray token should be treated as a value.
        /// This allows read-ahead functionality required for Streams so that a custom converter
        /// does not run out of data and fail.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsProcessingValue()
        {
            if (SkipProperty)
            {
                return false;
            }

            ClassType classType;

            if (CollectionPropertyInitialized)
            {
                classType = JsonPropertyInfo.ElementClassInfo.ClassType;
            }
            else if (JsonPropertyInfo == null)
            {
                classType = JsonClassInfo.ClassType;
            }
            else
            {
                classType = JsonPropertyInfo.ClassType;
            }

            // A ClassType.Value indicates the object has a converter and ClassType.Unknown indicates the
            // property or element type is System.Object.
            // System.Object is treated as a JsonElement which requires returning true from this
            // method in order to properly read-ahead (since JsonElement has a custom converter).
            return (classType & (ClassType.Value | ClassType.Unknown)) != 0;
        }

        public void Initialize(Type type, JsonSerializerOptions options)
        {
            JsonClassInfo = options.GetOrAddClass(type);
            InitializeJsonPropertyInfo();
        }

        public void InitializeJsonPropertyInfo()
        {
            if (IsProcessingObject(ClassType.Value | ClassType.Enumerable | ClassType.Dictionary))
            {
                JsonPropertyInfo = JsonClassInfo.PolicyProperty;
            }
        }

        public void Reset()
        {
            Drain = false;
            JsonClassInfo = null;
            PropertyRefCache = null;
            ReturnValue = null;
            EndObject();
        }

        public void EndObject()
        {
            PropertyIndex = 0;
            EndProperty();
        }

        public void EndProperty()
        {
            AddObjectToEnumerable = null;
            CollectionPropertyInitialized = false;
            JsonPropertyInfo = null;
            TempEnumerableValues = null;
            TempDictionaryValues = null;
            JsonPropertyName = null;
            KeyName = null;
        }

        public static object CreateEnumerableValue(ref ReadStack state)
        {
            JsonPropertyInfo jsonPropertyInfo = state.Current.JsonPropertyInfo;

            // If the property has an EnumerableConverter, then we use tempEnumerableValues.
            if (jsonPropertyInfo.EnumerableConverter != null)
            {
                IList converterList;
                JsonClassInfo elementClassInfo = jsonPropertyInfo.ElementClassInfo;
                if (elementClassInfo.ClassType == ClassType.Value)
                {
                    converterList = elementClassInfo.PolicyProperty.CreateConverterList();
                }
                else
                {
                    converterList = new List<object>();
                }

                state.Current.TempEnumerableValues = converterList;

                // Clear the value if present to ensure we don't confuse tempEnumerableValues with the collection.
                if (!jsonPropertyInfo.IsPropertyPolicy &&
                    !state.Current.JsonPropertyInfo.IsImmutableArray)
                {
                    jsonPropertyInfo.SetValueAsObject(state.Current.ReturnValue, null);
                }

                return null;
            }

            JsonClassInfo runtimeClassInfo = jsonPropertyInfo.RuntimeClassInfo;
            if (runtimeClassInfo.CreateObject != null)
            {
                return runtimeClassInfo.CreateObject();
            }
            else
            {
                // Could not create an instance to be returned. For derived types, this means there is no parameterless ctor.
                throw ThrowHelper.GetNotSupportedException_SerializationNotSupportedCollection(
                    jsonPropertyInfo.DeclaredPropertyType,
                    jsonPropertyInfo.ParentClassType,
                    jsonPropertyInfo.PropertyInfo);
            }
        }

        public static object CreateDictionaryValue(ref ReadStack state)
        {
            JsonPropertyInfo jsonPropertyInfo = state.Current.JsonPropertyInfo;

            // If the property has a DictionaryConverter, then we use tempDictionaryValues.
            if (jsonPropertyInfo.DictionaryConverter != null)
            {
                IDictionary converterDictionary;
                JsonClassInfo elementClassInfo = jsonPropertyInfo.ElementClassInfo;
                if (elementClassInfo.ClassType == ClassType.Value)
                {
                    converterDictionary = elementClassInfo.PolicyProperty.CreateConverterDictionary();
                }
                else
                {
                    converterDictionary = new Dictionary<string, object>();
                }

                state.Current.TempDictionaryValues = converterDictionary;

                // Clear the value if present to ensure we don't confuse tempEnumerableValues with the collection.
                if (!jsonPropertyInfo.IsPropertyPolicy)
                {
                    jsonPropertyInfo.SetValueAsObject(state.Current.ReturnValue, null);
                }

                return null;
            }

            JsonClassInfo runtimeClassInfo = jsonPropertyInfo.RuntimeClassInfo;
            if (runtimeClassInfo.CreateObject != null)
            {
                return runtimeClassInfo.CreateObject();
            }
            else
            {
                // Could not create an instance to be returned. For derived types, this means there is no parameterless ctor.
                throw ThrowHelper.GetNotSupportedException_SerializationNotSupportedCollection(
                    jsonPropertyInfo.DeclaredPropertyType,
                    jsonPropertyInfo.ParentClassType,
                    jsonPropertyInfo.PropertyInfo);
            }
        }

        public Type GetElementType()
        {
            if (IsProcessingCollectionProperty())
            {
                return JsonPropertyInfo.ElementClassInfo.Type;
            }

            if (IsProcessingCollectionObject())
            {
                return JsonClassInfo.ElementClassInfo.Type;
            }

            return JsonPropertyInfo.RuntimePropertyType;
        }

        public static IEnumerable GetEnumerableValue(ref ReadStackFrame current)
        {
            if (current.IsProcessingObject(ClassType.Enumerable))
            {
                if (current.ReturnValue != null)
                {
                    return (IEnumerable)current.ReturnValue;
                }
            }

            // IEnumerable properties are finished (values added inline) unless they are using tempEnumerableValues.
            return current.TempEnumerableValues;
        }

        public void DetermineEnumerablePopulationStrategy(object targetEnumerable)
        {
            if (JsonPropertyInfo.RuntimeClassInfo.AddItemToObject != null)
            {
                if (!JsonPropertyInfo.TryCreateEnumerableAddMethod(targetEnumerable, out object addMethodDelegate))
                {
                    // No "add" method for this collection, hence, not supported for deserialization.
                    throw ThrowHelper.GetNotSupportedException_SerializationNotSupportedCollection(
                        JsonPropertyInfo.DeclaredPropertyType,
                        JsonPropertyInfo.ParentClassType,
                        JsonPropertyInfo.PropertyInfo);
                }

                AddObjectToEnumerable = addMethodDelegate;
            }
            else if (targetEnumerable is IList targetList)
            {
                if (targetList.IsReadOnly)
                {
                    throw ThrowHelper.GetNotSupportedException_SerializationNotSupportedCollection(
                    JsonPropertyInfo.DeclaredPropertyType,
                    JsonPropertyInfo.ParentClassType,
                    JsonPropertyInfo.PropertyInfo);
                }
            }
            // If there's no add method, and we can't cast to IList, this collection is not supported for deserialization.
            else
            {
                throw ThrowHelper.GetNotSupportedException_SerializationNotSupportedCollection(
                    JsonPropertyInfo.DeclaredPropertyType,
                    JsonPropertyInfo.ParentClassType,
                    JsonPropertyInfo.PropertyInfo);
            }
        }

        public void DetermineIfDictionaryCanBePopulated(object targetDictionary)
        {
            if (!JsonPropertyInfo.CanPopulateDictionary(targetDictionary))
            {
                throw ThrowHelper.GetNotSupportedException_SerializationNotSupportedCollection(
                    JsonPropertyInfo.DeclaredPropertyType,
                    JsonPropertyInfo.ParentClassType,
                    JsonPropertyInfo.PropertyInfo);
            }
        }

        public bool SkipProperty => Drain ||
            JsonPropertyInfo != null &&
            JsonPropertyInfo.IsPropertyPolicy == false &&
            JsonPropertyInfo.ShouldDeserialize == false;
    }
}
