// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Converters;

namespace System.Text.Json
{
    [DebuggerDisplay("ClassType.{ClassType}, {Type.Name}")]
    internal sealed partial class JsonClassInfo
    {
        // The length of the property name embedded in the key (in bytes).
        private const int PropertyNameKeyLength = 6;

        // The limit to how many property names from the JSON are cached in _propertyRefsSorted before using PropertyCache.
        private const int PropertyNameCountCacheThreshold = 64;

        // All of the serializable properties on a POCO keyed on property name.
        public volatile Dictionary<string, JsonPropertyInfo> PropertyCache;

        // Fast cache of properties by first JSON ordering; may not contain all properties. Accessed before PropertyCache.
        // Use an array (instead of List<T>) for highest performance.
        private volatile PropertyRef[] _propertyRefsSorted;

        public delegate object ConstructorDelegate();
        public ConstructorDelegate CreateObject { get; private set; }
        public ConstructorDelegate CreateConcreteDictionary { get; private set; }

        public ClassType ClassType { get; private set; }

        public JsonPropertyInfo DataExtensionProperty { get; private set; }

        // If enumerable, the JsonClassInfo for the element type.
        private JsonClassInfo _elementClassInfo;

        /// <summary>
        /// Return the JsonClassInfo for the element type, or null if the type is not an enumerable or dictionary.
        /// </summary>
        /// <remarks>
        /// This should not be called during warm-up (initial creation of JsonClassInfos) to avoid recursive behavior
        /// which could result in a StackOverflowException.
        /// </remarks>
        public JsonClassInfo ElementClassInfo
        {
            get
            {
                if (_elementClassInfo == null && ElementType != null)
                {
                    Debug.Assert(ClassType == ClassType.Enumerable ||
                        ClassType == ClassType.Dictionary ||
                        ClassType == ClassType.IDictionaryConstructible);

                    _elementClassInfo = Options.GetOrAddClass(ElementType);
                }

                return _elementClassInfo;
            }
        }

        public Type ElementType { get; set; }

        public JsonSerializerOptions Options { get; private set; }

        public Type Type { get; private set; }

        public void UpdateSortedPropertyCache(ref ReadStackFrame frame)
        {
            Debug.Assert(frame.PropertyRefCache != null);

            // frame.PropertyRefCache is only read\written by a single thread -- the thread performing
            // the deserialization for a given object instance.

            List<PropertyRef> listToAppend = frame.PropertyRefCache;

            // _propertyRefsSorted can be accessed by multiple threads, so replace the reference when
            // appending to it. No lock() is necessary.

            if (_propertyRefsSorted != null)
            {
                List<PropertyRef> replacementList = new List<PropertyRef>(_propertyRefsSorted);
                Debug.Assert(replacementList.Count <= PropertyNameCountCacheThreshold);

                // Verify replacementList will not become too large.
                while (replacementList.Count + listToAppend.Count > PropertyNameCountCacheThreshold)
                {
                    // This code path is rare; keep it simple by using RemoveAt() instead of RemoveRange() which requires calculating index\count.
                    listToAppend.RemoveAt(listToAppend.Count - 1);
                }

                // Add the new items; duplicates are possible but that is tolerated during property lookup.
                replacementList.AddRange(listToAppend);
                _propertyRefsSorted = replacementList.ToArray();
            }
            else
            {
                _propertyRefsSorted = listToAppend.ToArray();
            }

            frame.PropertyRefCache = null;
        }

        public JsonClassInfo(Type type, JsonSerializerOptions options)
        {
            Type = type;
            Options = options;
            ClassType = GetClassType(type, options);

            CreateObject = options.MemberAccessorStrategy.CreateConstructor(type);

            // Ignore properties on enumerable.
            switch (ClassType)
            {
                case ClassType.Object:
                    {
                        PropertyInfo[] properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                        Dictionary<string, JsonPropertyInfo> cache = CreatePropertyCache(properties.Length);

                        foreach (PropertyInfo propertyInfo in properties)
                        {
                            // Ignore indexers
                            if (propertyInfo.GetIndexParameters().Length > 0)
                            {
                                continue;
                            }

                            // For now we only support public getters\setters
                            if (propertyInfo.GetMethod?.IsPublic == true ||
                                propertyInfo.SetMethod?.IsPublic == true)
                            {
                                JsonPropertyInfo jsonPropertyInfo = AddProperty(propertyInfo.PropertyType, propertyInfo, type, options);
                                Debug.Assert(jsonPropertyInfo != null);

                                // If the JsonPropertyNameAttribute or naming policy results in collisions, throw an exception.
                                if (!JsonHelpers.TryAdd(cache, jsonPropertyInfo.NameAsString, jsonPropertyInfo))
                                {
                                    JsonPropertyInfo other = cache[jsonPropertyInfo.NameAsString];

                                    if (other.ShouldDeserialize == false && other.ShouldSerialize == false)
                                    {
                                        // Overwrite the one just added since it has [JsonIgnore].
                                        cache[jsonPropertyInfo.NameAsString] = jsonPropertyInfo;
                                    }
                                    else if (jsonPropertyInfo.ShouldDeserialize == true || jsonPropertyInfo.ShouldSerialize == true)
                                    {
                                        ThrowHelper.ThrowInvalidOperationException_SerializerPropertyNameConflict(this, jsonPropertyInfo);
                                    }
                                    // else ignore jsonPropertyInfo since it has [JsonIgnore].
                                }
                            }
                        }

                        if (DetermineExtensionDataProperty(cache))
                        {
                            // Remove from cache since it is handled independently.
                            cache.Remove(DataExtensionProperty.NameAsString);
                        }

                        // Set as a unit to avoid concurrency issues.
                        PropertyCache = cache;
                    }
                    break;
                case ClassType.Enumerable:
                case ClassType.Dictionary:
                    {
                        // Add a single property that maps to the class type so we can have policies applied.
                         AddPolicyProperty(type, options);

                        Type objectType;
                        if (IsNativelySupportedCollection(type))
                        {
                            // Use the type from the property policy to get any late-bound concrete types (from an interface like IDictionary).
                            objectType = PolicyProperty.RuntimePropertyType;
                        }
                        else
                        {
                            // We need to create the declared instance for types implementing natively supported collections.
                            objectType = PolicyProperty.DeclaredPropertyType;
                        }

                        CreateObject = options.MemberAccessorStrategy.CreateConstructor(objectType);

                        ElementType = GetElementType(type, parentType: null, memberInfo: null, options: options);
                    }
                    break;
                case ClassType.IDictionaryConstructible:
                    {
                        // Add a single property that maps to the class type so we can have policies applied.
                        AddPolicyProperty(type, options);

                        ElementType = GetElementType(type, parentType: null, memberInfo: null, options: options);

                        CreateConcreteDictionary = options.MemberAccessorStrategy.CreateConstructor(
                           typeof(Dictionary<,>).MakeGenericType(typeof(string), ElementType));

                        CreateObject = options.MemberAccessorStrategy.CreateConstructor(PolicyProperty.DeclaredPropertyType);
                    }
                    break;
                case ClassType.Value:
                    // Add a single property that maps to the class type so we can have policies applied.
                    AddPolicyProperty(type, options);
                    break;
                case ClassType.Unknown:
                    // Add a single property that maps to the class type so we can have policies applied.
                    AddPolicyProperty(type, options);
                    PropertyCache = new Dictionary<string, JsonPropertyInfo>();
                    break;
                default:
                    Debug.Fail($"Unexpected class type: {ClassType}");
                    break;
            }
        }

        private bool DetermineExtensionDataProperty(Dictionary<string, JsonPropertyInfo> cache)
        {
            JsonPropertyInfo jsonPropertyInfo = GetPropertyWithUniqueAttribute(typeof(JsonExtensionDataAttribute), cache);
            if (jsonPropertyInfo != null)
            {
                Type declaredPropertyType = jsonPropertyInfo.DeclaredPropertyType;
                if (!typeof(IDictionary<string, JsonElement>).IsAssignableFrom(declaredPropertyType) &&
                    !typeof(IDictionary<string, object>).IsAssignableFrom(declaredPropertyType))
                {
                    ThrowHelper.ThrowInvalidOperationException_SerializationDataExtensionPropertyInvalid(this, jsonPropertyInfo);
                }

                DataExtensionProperty = jsonPropertyInfo;
                return true;
            }

            return false;
        }

        private JsonPropertyInfo GetPropertyWithUniqueAttribute(Type attributeType, Dictionary<string, JsonPropertyInfo> cache)
        {
            JsonPropertyInfo property = null;

            foreach (JsonPropertyInfo jsonPropertyInfo in cache.Values)
            {
                Attribute attribute = jsonPropertyInfo.PropertyInfo.GetCustomAttribute(attributeType);
                if (attribute != null)
                {
                    if (property != null)
                    {
                        ThrowHelper.ThrowInvalidOperationException_SerializationDuplicateTypeAttribute(Type, attributeType);
                    }

                    property = jsonPropertyInfo;
                }
            }

            return property;
        }

        public JsonPropertyInfo GetProperty(ReadOnlySpan<byte> propertyName, ref ReadStackFrame frame)
        {
            JsonPropertyInfo info = null;

            // Keep a local copy of the cache in case it changes by another thread.
            PropertyRef[] localPropertyRefsSorted = _propertyRefsSorted;

            // If there is an existing cache, then use it.
            if (localPropertyRefsSorted != null)
            {
                ulong key = GetKey(propertyName);

                // Start with the current property index, and then go forwards\backwards.
                int propertyIndex = frame.PropertyIndex;

                int count = localPropertyRefsSorted.Length;
                int iForward = Math.Min(propertyIndex, count);
                int iBackward = iForward - 1;

                while (iForward < count || iBackward >= 0)
                {
                    if (iForward < count)
                    {
                        if (TryIsPropertyRefEqual(localPropertyRefsSorted[iForward], propertyName, key, ref info))
                        {
                            return info;
                        }
                        ++iForward;
                    }

                    if (iBackward >= 0)
                    {
                        if (TryIsPropertyRefEqual(localPropertyRefsSorted[iBackward], propertyName, key, ref info))
                        {
                            return info;
                        }
                        --iBackward;
                    }
                }
            }

            // No cached item was found. Try the main list which has all of the properties.

            string stringPropertyName = JsonHelpers.Utf8GetString(propertyName);
            if (PropertyCache.TryGetValue(stringPropertyName, out info))
            {
                // Check if we should add this to the cache.
                // Only cache up to a threshold length and then just use the dictionary when an item is not found in the cache.
                int count;
                if (localPropertyRefsSorted != null)
                {
                    count = localPropertyRefsSorted.Length;
                }
                else
                {
                    count = 0;
                }

                // Do a quick check for the stable (after warm-up) case.
                if (count < PropertyNameCountCacheThreshold)
                {
                    // Do a slower check for the warm-up case.
                    if (frame.PropertyRefCache != null)
                    {
                        count += frame.PropertyRefCache.Count;
                    }

                    // Check again to append the cache up to the threshold.
                    if (count < PropertyNameCountCacheThreshold)
                    {
                        if (frame.PropertyRefCache == null)
                        {
                            frame.PropertyRefCache = new List<PropertyRef>();
                        }

                        ulong key = info.PropertyNameKey;
                        PropertyRef propertyRef = new PropertyRef(key, info);
                        frame.PropertyRefCache.Add(propertyRef);
                    }
                }
            }

            return info;
        }

        private Dictionary<string, JsonPropertyInfo> CreatePropertyCache(int capacity)
        {
            StringComparer comparer;

            if (Options.PropertyNameCaseInsensitive)
            {
                comparer = StringComparer.OrdinalIgnoreCase;
            }
            else
            {
                comparer = StringComparer.Ordinal;
            }

            return new Dictionary<string, JsonPropertyInfo>(capacity, comparer);
        }

        public JsonPropertyInfo PolicyProperty { get; private set; }

        private static bool TryIsPropertyRefEqual(in PropertyRef propertyRef, ReadOnlySpan<byte> propertyName, ulong key, ref JsonPropertyInfo info)
        {
            if (key == propertyRef.Key)
            {
                if (propertyName.Length <= PropertyNameKeyLength ||
                    // We compare the whole name, although we could skip the first 6 bytes (but it's likely not any faster)
                    propertyName.SequenceEqual(propertyRef.Info.Name))
                {
                    info = propertyRef.Info;
                    return true;
                }
            }

            return false;
        }

        private static bool IsPropertyRefEqual(ref PropertyRef propertyRef, PropertyRef other)
        {
            if (propertyRef.Key == other.Key)
            {
                if (propertyRef.Info.Name.Length <= PropertyNameKeyLength ||
                    propertyRef.Info.Name.AsSpan().SequenceEqual(other.Info.Name.AsSpan()))
                {
                    return true;
                }
            }

            return false;
        }

        public static ulong GetKey(ReadOnlySpan<byte> propertyName)
        {
            ulong key;
            int length = propertyName.Length;

            // Embed the propertyName in the first 6 bytes of the key.
            if (length > 3)
            {
                key = MemoryMarshal.Read<uint>(propertyName);
                if (length > 4)
                {
                    key |= (ulong)propertyName[4] << 32;
                }
                if (length > 5)
                {
                    key |= (ulong)propertyName[5] << 40;
                }
            }
            else if (length > 1)
            {
                key = MemoryMarshal.Read<ushort>(propertyName);
                if (length > 2)
                {
                    key |= (ulong)propertyName[2] << 16;
                }
            }
            else if (length == 1)
            {
                key = propertyName[0];
            }
            else
            {
                // An empty name is valid.
                key = 0;
            }

            // Embed the propertyName length in the last two bytes.
            key |= (ulong)propertyName.Length << 48;
            return key;
        }

        // Return the element type of the IEnumerable or return null if not an IEnumerable.
        public static Type GetElementType(Type propertyType, Type parentType, MemberInfo memberInfo, JsonSerializerOptions options)
        {
            // We want to handle as the implemented collection type, if applicable.
            Type implementedType = GetImplementedCollectionType(parentType, propertyType, propertyInfo: null, out _, options);

            if (!typeof(IEnumerable).IsAssignableFrom(implementedType))
            {
                return null;
            }

            // Check for Array.
            Type elementType = implementedType.GetElementType();
            if (elementType != null)
            {
                return elementType;
            }

            // Check for Dictionary<TKey, TValue> or IEnumerable<T>
            if (implementedType.IsGenericType)
            {
                Type[] args = implementedType.GetGenericArguments();
                ClassType classType = GetClassType(implementedType, options);

                if ((classType == ClassType.Dictionary || classType == ClassType.IDictionaryConstructible) &&
                    args.Length >= 2 && // It is >= 2 in case there is a IDictionary<TKey, TValue, TSomeExtension>.
                    args[0].UnderlyingSystemType == typeof(string))
                {
                    return args[1];
                }

                if (classType == ClassType.Enumerable && args.Length >= 1) // It is >= 1 in case there is an IEnumerable<T, TSomeExtension>.
                {
                    return args[0];
                }
            }

            if (implementedType.IsAssignableFrom(typeof(IList)) ||
                implementedType.IsAssignableFrom(typeof(IDictionary)) ||
                IsDeserializedByConstructingWithIList(implementedType) ||
                IsDeserializedByConstructingWithIDictionary(implementedType))
            {
                return typeof(object);
            }

            throw ThrowHelper.GetNotSupportedException_SerializationNotSupportedCollection(propertyType, parentType, memberInfo);
        }

        public static ClassType GetClassType(Type type, JsonSerializerOptions options)
        {
            Debug.Assert(type != null);

            // We want to handle as the implemented collection type, if applicable.
            Type implementedType = GetImplementedCollectionType(typeof(object), type, propertyInfo: null, out _, options);

            if (implementedType.IsGenericType && implementedType.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                implementedType = Nullable.GetUnderlyingType(implementedType);
            }

            if (implementedType == typeof(object))
            {
                return ClassType.Unknown;
            }

            if (options.HasConverter(implementedType))
            {
                return ClassType.Value;
            }

            if (DefaultImmutableDictionaryConverter.IsImmutableDictionary(implementedType) ||
                IsDeserializedByConstructingWithIDictionary(implementedType))
            {
                return ClassType.IDictionaryConstructible;
            }

            if (typeof(IDictionary).IsAssignableFrom(implementedType) || IsDictionaryClassType(implementedType))
            {
                // Special case for immutable dictionaries
                if (type != implementedType && !IsNativelySupportedCollection(type))
                {
                    return ClassType.IDictionaryConstructible;
                }

                return ClassType.Dictionary;
            }

            if (typeof(IEnumerable).IsAssignableFrom(implementedType))
            {
                return ClassType.Enumerable;
            }

            return ClassType.Object;
        }

        public static bool IsDictionaryClassType(Type type)
        {
            return (type.IsGenericType && (type.GetGenericTypeDefinition() == typeof(IDictionary<,>) ||
                type.GetGenericTypeDefinition() == typeof(IReadOnlyDictionary<,>)));
        }
    }
}
