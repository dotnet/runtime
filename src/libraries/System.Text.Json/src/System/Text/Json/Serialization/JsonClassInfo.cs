// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Converters;

namespace System.Text.Json
{
    [DebuggerDisplay("ClassType.{ClassType}, {Type.Name}")]
    internal sealed partial class JsonClassInfo
    {
        // The length of the property name embedded in the key (in bytes).
        // The key is a ulong (8 bytes) containing the first 7 bytes of the property name
        // followed by a byte representing the length.
        private const int PropertyNameKeyLength = 7;

        // The limit to how many property names from the JSON are cached in _propertyRefsSorted before using PropertyCache.
        private const int PropertyNameCountCacheThreshold = 64;

        // All of the serializable properties on a POCO (except the optional extension property) keyed on property name.
        public volatile Dictionary<string, JsonPropertyInfo> PropertyCache;

        // Serializable runtime/polymorphic properties, keyed on property and runtime type.
        public ConcurrentDictionary<(JsonPropertyInfo, Type), JsonPropertyInfo> RuntimePropertyCache;

        // All of the serializable properties on a POCO including the optional extension property.
        // Used for performance during serialization instead of 'PropertyCache' above.
        public volatile JsonPropertyInfo[] PropertyCacheArray;

        // Fast cache of properties by first JSON ordering; may not contain all properties. Accessed before PropertyCache.
        // Use an array (instead of List<T>) for highest performance.
        private volatile PropertyRef[] _propertyRefsSorted;

        public delegate object ConstructorDelegate();
        public ConstructorDelegate CreateObject { get; private set; }

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
                        ClassType == ClassType.Dictionary);

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

            ClassType = GetClassType(
                type,
                parentClassType: type,
                propertyInfo: null,
                out Type runtimeType,
                out Type elementType,
                out Type nullableUnderlyingType,
                out MethodInfo addMethod,
                out JsonConverter converter,
                checkForAddMethod: true,
                options);

            // Ignore properties on enumerable.
            switch (ClassType)
            {
                case ClassType.Object:
                    {
                        CreateObject = options.MemberAccessorStrategy.CreateConstructor(type);

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

                        JsonPropertyInfo[] cacheArray;
                        if (DetermineExtensionDataProperty(cache))
                        {
                            // Remove from cache since it is handled independently.
                            cache.Remove(DataExtensionProperty.NameAsString);

                            cacheArray = new JsonPropertyInfo[cache.Count + 1];

                            // Set the last element to the extension property.
                            cacheArray[cache.Count] = DataExtensionProperty;
                        }
                        else
                        {
                            cacheArray = new JsonPropertyInfo[cache.Count];
                        }

                        // Set fields when finished to avoid concurrency issues.
                        PropertyCache = cache;
                        cache.Values.CopyTo(cacheArray, 0);
                        PropertyCacheArray = cacheArray;
                    }
                    break;
                case ClassType.Enumerable:
                case ClassType.Dictionary:
                    {
                        ElementType = elementType;
                        AddItemToObject = addMethod;
                        PolicyProperty = CreatePolicyProperty(type, runtimeType, elementType, nullableUnderlyingType, converter: null, ClassType, options);
                        CreateObject = options.MemberAccessorStrategy.CreateConstructor(PolicyProperty.RuntimePropertyType);
                    }
                    break;
                case ClassType.Value:
                    {
                        CreateObject = options.MemberAccessorStrategy.CreateConstructor(type);
                        PolicyProperty = CreatePolicyProperty(type, runtimeType, elementType: null, nullableUnderlyingType, converter, ClassType, options);
                    }
                    break;
                case ClassType.Unknown:
                    {
                        CreateObject = options.MemberAccessorStrategy.CreateConstructor(type);
                        PolicyProperty = CreatePolicyProperty(type, runtimeType, elementType: null, nullableUnderlyingType, converter, ClassType, options);
                        PropertyCache = new Dictionary<string, JsonPropertyInfo>();
                        PropertyCacheArray = Array.Empty<JsonPropertyInfo>();
                    }
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

        // AggressiveInlining used although a large method it is only called from one location and is on a hot path.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public JsonPropertyInfo GetProperty(ReadOnlySpan<byte> propertyName, ref ReadStackFrame frame)
        {
            JsonPropertyInfo info = null;

            // Keep a local copy of the cache in case it changes by another thread.
            PropertyRef[] localPropertyRefsSorted = _propertyRefsSorted;

            ulong key = GetKey(propertyName);

            // If there is an existing cache, then use it.
            if (localPropertyRefsSorted != null)
            {
                // Start with the current property index, and then go forwards\backwards.
                int propertyIndex = frame.PropertyIndex;

                int count = localPropertyRefsSorted.Length;
                int iForward = Math.Min(propertyIndex, count);
                int iBackward = iForward - 1;

                while (true)
                {
                    if (iForward < count)
                    {
                        PropertyRef propertyRef = localPropertyRefsSorted[iForward];
                        if (TryIsPropertyRefEqual(propertyRef, propertyName, key, ref info))
                        {
                            return info;
                        }

                        ++iForward;

                        if (iBackward >= 0)
                        {
                            propertyRef = localPropertyRefsSorted[iBackward];
                            if (TryIsPropertyRefEqual(propertyRef, propertyName, key, ref info))
                            {
                                return info;
                            }

                            --iBackward;
                        }
                    }
                    else if (iBackward >= 0)
                    {
                        PropertyRef propertyRef = localPropertyRefsSorted[iBackward];
                        if (TryIsPropertyRefEqual(propertyRef, propertyName, key, ref info))
                        {
                            return info;
                        }

                        --iBackward;
                    }
                    else
                    {
                        // Property was not found.
                        break;
                    }
                }
            }

            // No cached item was found. Try the main list which has all of the properties.

            string stringPropertyName = JsonHelpers.Utf8GetString(propertyName);
            if (!PropertyCache.TryGetValue(stringPropertyName, out info))
            {
                info = JsonPropertyInfo.s_missingProperty;
            }

            Debug.Assert(info != null);

            // Three code paths to get here:
            // 1) info == s_missingProperty. Property not found.
            // 2) key == info.PropertyNameKey. Exact match found.
            // 3) key != info.PropertyNameKey. Match found due to case insensitivity.
            Debug.Assert(info == JsonPropertyInfo.s_missingProperty || key == info.PropertyNameKey || Options.PropertyNameCaseInsensitive);

            // Check if we should add this to the cache.
            // Only cache up to a threshold length and then just use the dictionary when an item is not found in the cache.
            int cacheCount = 0;
            if (localPropertyRefsSorted != null)
            {
                cacheCount = localPropertyRefsSorted.Length;
            }

            // Do a quick check for the stable (after warm-up) case.
            if (cacheCount < PropertyNameCountCacheThreshold)
            {
                // Do a slower check for the warm-up case.
                if (frame.PropertyRefCache != null)
                {
                    cacheCount += frame.PropertyRefCache.Count;
                }

                // Check again to append the cache up to the threshold.
                if (cacheCount < PropertyNameCountCacheThreshold)
                {
                    if (frame.PropertyRefCache == null)
                    {
                        frame.PropertyRefCache = new List<PropertyRef>();
                    }

                    PropertyRef propertyRef = new PropertyRef(key, info);
                    frame.PropertyRefCache.Add(propertyRef);
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

        public MethodInfo AddItemToObject { get; private set; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool TryIsPropertyRefEqual(in PropertyRef propertyRef, ReadOnlySpan<byte> propertyName, ulong key, ref JsonPropertyInfo info)
        {
            if (key == propertyRef.Key)
            {
                // We compare the whole name, although we could skip the first 7 bytes (but it's not any faster)
                if (propertyName.Length <= PropertyNameKeyLength ||
                    propertyName.SequenceEqual(propertyRef.Info.Name))
                {
                    info = propertyRef.Info;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Get a key from the property name.
        /// The key consists of the first 7 bytes of the property name and then the length.
        /// </summary>
        public static ulong GetKey(ReadOnlySpan<byte> propertyName)
        {
            const int BitsInByte = 8;
            ulong key;
            int length = propertyName.Length;

            if (length > 7)
            {
                key = MemoryMarshal.Read<ulong>(propertyName);

                // Max out the length byte.
                // This will cause the comparison logic to always test for equality against the full contents
                // when the first 7 bytes are the same.
                key |= 0xFF00000000000000;

                // It is also possible to include the length up to 0xFF in order to prevent false positives
                // when the first 7 bytes match but a different length (up to 0xFF). However the extra logic
                // slows key generation in the majority of cases:
                // key &= 0x00FFFFFFFFFFFFFF;
                // key |= (ulong) 7 << Math.Max(length, 0xFF);
            }
            else if (length > 3)
            {
                key = MemoryMarshal.Read<uint>(propertyName);

                if (length == 7)
                {
                    key |= (ulong)propertyName[6] << (6 * BitsInByte)
                        | (ulong)propertyName[5] << (5 * BitsInByte)
                        | (ulong)propertyName[4] << (4 * BitsInByte)
                        | (ulong)7 << (7 * BitsInByte);
                }
                else if (length == 6)
                {
                    key |= (ulong)propertyName[5] << (5 * BitsInByte)
                        | (ulong)propertyName[4] << (4 * BitsInByte)
                        | (ulong)6 << (7 * BitsInByte);
                }
                else if (length == 5)
                {
                    key |= (ulong)propertyName[4] << (4 * BitsInByte)
                        | (ulong)5 << (7 * BitsInByte);
                }
                else
                {
                    key |= (ulong)4 << (7 * BitsInByte);
                }
            }
            else if (length > 1)
            {
                key = MemoryMarshal.Read<ushort>(propertyName);

                if (length == 3)
                {
                    key |= (ulong)propertyName[2] << (2 * BitsInByte)
                        | (ulong)3 << (7 * BitsInByte);
                }
                else
                {
                    key |= (ulong)2 << (7 * BitsInByte);
                }
            }
            else if (length == 1)
            {
                key = propertyName[0]
                    | (ulong)1 << (7 * BitsInByte);
            }
            else
            {
                // An empty name is valid.
                key = 0;
            }

            // Verify key contains the embedded bytes as expected.
            Debug.Assert(
                (length < 1 || propertyName[0] == ((key & ((ulong)0xFF << 8 * 0)) >> 8 * 0)) &&
                (length < 2 || propertyName[1] == ((key & ((ulong)0xFF << 8 * 1)) >> 8 * 1)) &&
                (length < 3 || propertyName[2] == ((key & ((ulong)0xFF << 8 * 2)) >> 8 * 2)) &&
                (length < 4 || propertyName[3] == ((key & ((ulong)0xFF << 8 * 3)) >> 8 * 3)) &&
                (length < 5 || propertyName[4] == ((key & ((ulong)0xFF << 8 * 4)) >> 8 * 4)) &&
                (length < 6 || propertyName[5] == ((key & ((ulong)0xFF << 8 * 5)) >> 8 * 5)) &&
                (length < 7 || propertyName[6] == ((key & ((ulong)0xFF << 8 * 6)) >> 8 * 6)));

            return key;
        }

        // This method gets the runtime information for a given type or property.
        // The runtime information consists of the following:
        // - class type,
        // - runtime type,
        // - element type (if the type is a collection),
        // - the underlying type (if the type is nullable type e.g. int?),
        // - the "add" method (if the type is a non-dictionary collection which doesn't implement IList
        //   e.g. typeof(Stack<int>), where we retrieve the void Push(string) method), and
        // - the converter (either native or custom), if one exists.
        public static ClassType GetClassType(
            Type type,
            Type parentClassType,
            PropertyInfo propertyInfo,
            out Type runtimeType,
            out Type elementType,
            out Type nullableUnderlyingType,
            out MethodInfo addMethod,
            out JsonConverter converter,
            bool checkForAddMethod,
            JsonSerializerOptions options)
        {
            Debug.Assert(type != null);

            runtimeType = type;

            nullableUnderlyingType = Nullable.GetUnderlyingType(type);

            // Type is nullable e.g. typeof(int?).
            if (nullableUnderlyingType != null)
            {
                // Check if there's a converter for this nullable type, e.g. do we have a converter that implements
                // JsonConverter<int?> if the type is typeof(int?)?
                converter = options.DetermineConverterForProperty(parentClassType, type, propertyInfo);

                if (converter == null)
                {
                    // No converter. We'll check below if there's a converter for the non-nullable type e.g.
                    // one that implements JsonConverter<int>, given the type is typeof(int?).
                    type = nullableUnderlyingType;
                }
                else
                {
                    elementType = default;
                    addMethod = default;
                    // Don't treat the type as a Nullable when creating the property info later on, since we have a converter for it.
                    nullableUnderlyingType = default;
                    return ClassType.Value;
                }
            }

            converter = options.DetermineConverterForProperty(parentClassType, type, propertyInfo);

            if (converter != null)
            {
                elementType = default;
                addMethod = default;
                return type == typeof(object) ? ClassType.Unknown : ClassType.Value;
            }

            runtimeType = type;

            if (!(typeof(IEnumerable)).IsAssignableFrom(type))
            {
                elementType = null;
                addMethod = default;
                return ClassType.Object;
            }

            if (type.IsArray)
            {
                elementType = type.GetElementType();
                addMethod = default;
                return ClassType.Enumerable;
            }

            if (type.FullName.StartsWith("System.Collections.Generic.IEnumerable`1"))
            {
                elementType = type.GetGenericArguments()[0];
                runtimeType = typeof(List<>).MakeGenericType(elementType);
                addMethod = default;
                return ClassType.Enumerable;
            }
            else if (type.FullName.StartsWith("System.Collections.Generic.IDictionary`2") ||
                type.FullName.StartsWith("System.Collections.Generic.IReadOnlyDictionary`2"))
            {
                Type[] genericTypes = type.GetGenericArguments();

                elementType = genericTypes[1];
                runtimeType = typeof(Dictionary<,>).MakeGenericType(genericTypes[0], elementType);
                addMethod = default;
                return ClassType.Dictionary;
            }

            {
                Type genericIDictionaryType = type.GetInterface("System.Collections.Generic.IDictionary`2") ?? type.GetInterface("System.Collections.Generic.IReadOnlyDictionary`2");
                if (genericIDictionaryType != null)
                {
                    Type[] genericTypes = genericIDictionaryType.GetGenericArguments();
                    elementType = genericTypes[1];
                    addMethod = default;

                    if (type.IsInterface)
                    {
                        Type concreteDictionaryType = typeof(Dictionary<,>).MakeGenericType(genericTypes[0], genericTypes[1]);

                        if (type.IsAssignableFrom(concreteDictionaryType))
                        {
                            runtimeType = concreteDictionaryType;
                        }
                    }

                    return ClassType.Dictionary;
                }
            }

            if (typeof(IDictionary).IsAssignableFrom(type))
            {
                elementType = typeof(object);
                addMethod = default;

                if (type.IsInterface)
                {
                    Type concreteDictionaryType = typeof(Dictionary<string, object>);

                    if (type.IsAssignableFrom(concreteDictionaryType))
                    {
                        runtimeType = concreteDictionaryType;
                    }
                }

                return ClassType.Dictionary;
            }

            {
                Type genericIEnumerableType = type.GetInterface("System.Collections.Generic.IEnumerable`1");

                if (genericIEnumerableType != null)
                {
                    elementType = genericIEnumerableType.GetGenericArguments()[0];
                }
                else
                {
                    elementType = typeof(object);
                }
            }

            if (typeof(IList).IsAssignableFrom(type))
            {
                addMethod = default;

                if (type.IsInterface)
                {
                    Type concreteListType = typeof(List<>).MakeGenericType(elementType);
                    if (type.IsAssignableFrom(concreteListType))
                    {
                        runtimeType = concreteListType;
                    }
                }
            }
            else if (type.IsInterface)
            {
                addMethod = default;

                Type concreteType = typeof(List<>).MakeGenericType(elementType);
                if (type.IsAssignableFrom(concreteType))
                {
                    runtimeType = concreteType;
                }
                else
                {
                    concreteType = typeof(HashSet<>).MakeGenericType(elementType);
                    if (type.IsAssignableFrom(concreteType))
                    {
                        runtimeType = concreteType;
                    }
                }
            }
            else
            {
                addMethod = default;

                if (checkForAddMethod)
                {
                    Type genericICollectionType = type.GetInterface("System.Collections.Generic.ICollection`1");
                    if (genericICollectionType != null)
                    {
                        addMethod = genericICollectionType.GetMethod("Add");
                    }
                    else
                    {
                        // Non-immutable stack or queue.
                        MethodInfo methodInfo = type.GetMethod("Push") ?? type.GetMethod("Enqueue");
                        if (methodInfo?.ReturnType == typeof(void))
                        {
                            addMethod = methodInfo;
                        }
                    }
                }
            }

            return ClassType.Enumerable;
        }
    }
}
