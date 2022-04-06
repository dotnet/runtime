// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text.Json.Reflection;
using System.Threading;

namespace System.Text.Json.Serialization.Metadata
{
    /// <summary>
    /// Provides JSON serialization-related metadata about a type.
    /// </summary>
    /// <remarks>This API is for use by the output of the System.Text.Json source generator and should not be called directly.</remarks>
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public partial class JsonTypeInfo
    {
        internal const string JsonObjectTypeName = "System.Text.Json.Nodes.JsonObject";

        internal delegate object? ConstructorDelegate();

        internal delegate T ParameterizedConstructorDelegate<T, TArg0, TArg1, TArg2, TArg3>(TArg0 arg0, TArg1 arg1, TArg2 arg2, TArg3 arg3);

        internal ConstructorDelegate? CreateObject { get; set; }

        internal object? CreateObjectWithArgs { get; set; }

        // Add method delegate for non-generic Stack and Queue; and types that derive from them.
        internal object? AddMethodDelegate { get; set; }

        internal JsonPropertyInfo? DataExtensionProperty { get; private set; }

        // If enumerable or dictionary, the JsonTypeInfo for the element type.
        private JsonTypeInfo? _elementTypeInfo;

        // Avoids having to perform an expensive cast to JsonTypeInfo<T> to check if there is a Serialize method.
        internal bool HasSerialize { get; set; }

        /// <summary>
        /// Return the JsonTypeInfo for the element type, or null if the type is not an enumerable or dictionary.
        /// </summary>
        /// <remarks>
        /// This should not be called during warm-up (initial creation of JsonTypeInfos) to avoid recursive behavior
        /// which could result in a StackOverflowException.
        /// </remarks>
        internal JsonTypeInfo? ElementTypeInfo
        {
            get
            {
                if (_elementTypeInfo == null && ElementType != null)
                {
                    _elementTypeInfo = Options.GetOrAddJsonTypeInfo(ElementType);
                }

                return _elementTypeInfo;
            }
            set
            {
                // Set by JsonMetadataServices.
                Debug.Assert(_elementTypeInfo == null);
                _elementTypeInfo = value;
            }
        }

        internal Type? ElementType { get; set; }

        // If dictionary, the JsonTypeInfo for the key type.
        private JsonTypeInfo? _keyTypeInfo;

        /// <summary>
        /// Return the JsonTypeInfo for the key type, or null if the type is not a dictionary.
        /// </summary>
        /// <remarks>
        /// This should not be called during warm-up (initial creation of JsonTypeInfos) to avoid recursive behavior
        /// which could result in a StackOverflowException.
        /// </remarks>
        internal JsonTypeInfo? KeyTypeInfo
        {
            get
            {
                if (_keyTypeInfo == null && KeyType != null)
                {
                    Debug.Assert(PropertyInfoForTypeInfo.ConverterStrategy == ConverterStrategy.Dictionary);

                    _keyTypeInfo = Options.GetOrAddJsonTypeInfo(KeyType);
                }

                return _keyTypeInfo;
            }
            set
            {
                // Set by JsonMetadataServices.
                Debug.Assert(_keyTypeInfo == null);
                _keyTypeInfo = value;
            }
        }

        internal Type? KeyType { get; set; }

        internal JsonSerializerOptions Options { get; set; }

        internal Type Type { get; private set; }

        /// <summary>
        /// The JsonPropertyInfo for this JsonTypeInfo. It is used to obtain the converter for the TypeInfo.
        /// </summary>
        /// <remarks>
        /// The returned JsonPropertyInfo does not represent a real property; instead it represents either:
        /// a collection element type,
        /// a generic type parameter,
        /// a property type (if pushed to a new stack frame),
        /// or the root type passed into the root serialization APIs.
        /// For example, for a property returning <see cref="Collections.Generic.List{T}"/> where T is a string,
        /// a JsonTypeInfo will be created with .Type=typeof(string) and .PropertyInfoForTypeInfo=JsonPropertyInfo{string}.
        /// Without this property, a "Converter" property would need to be added to JsonTypeInfo and there would be several more
        /// `if` statements to obtain the converter from either the actual JsonPropertyInfo (for a real property) or from the
        /// TypeInfo (for the cases mentioned above). In addition, methods that have a JsonPropertyInfo argument would also likely
        /// need to add an argument for JsonTypeInfo.
        /// </remarks>
        internal JsonPropertyInfo PropertyInfoForTypeInfo { get; set; }

        internal bool IsObjectWithParameterizedCtor => PropertyInfoForTypeInfo.ConverterBase.ConstructorIsParameterized;


        /// <summary>
        /// Returns a helper class used for computing the default value.
        /// </summary>
        internal DefaultValueHolder DefaultValueHolder => _defaultValueHolder ??= DefaultValueHolder.CreateHolder(Type);
        private DefaultValueHolder? _defaultValueHolder;

        internal JsonNumberHandling? NumberHandling { get; set; }

        internal JsonTypeInfo()
        {
            Debug.Assert(false, "This constructor should not be called.");
        }

        internal JsonTypeInfo(Type type, JsonSerializerOptions options!!, bool dummy)
        {
            Type = type;
            Options = options;
            // Setting this option is deferred to the initialization methods of the various metadada info types.
            PropertyInfoForTypeInfo = null!;
        }

        [RequiresUnreferencedCode(JsonSerializer.SerializationUnreferencedCodeMessage)]
        internal JsonTypeInfo(Type type, JsonSerializerOptions options) :
            this(
                type,
                GetConverter(
                    type,
                    parentClassType: null, // A TypeInfo never has a "parent" class.
                    memberInfo: null, // A TypeInfo never has a "parent" property.
                    options),
                options)
        {
        }

        [RequiresUnreferencedCode(JsonSerializer.SerializationUnreferencedCodeMessage)]
        internal JsonTypeInfo(Type type, JsonConverter converter, JsonSerializerOptions options)
        {
            Type = type;
            Options = options;
            NumberHandling = GetNumberHandlingForType(Type);
            PropertyInfoForTypeInfo = CreatePropertyInfoForTypeInfo(Type, converter, NumberHandling, Options);
            ElementType = converter.ElementType;

            switch (PropertyInfoForTypeInfo.ConverterStrategy)
            {
                case ConverterStrategy.Object:
                    {
                        AddPropertiesAndParametersUsingReflection();
                    }
                    break;
                case ConverterStrategy.Dictionary:
                    {
                        KeyType = converter.KeyType;
                    }
                    break;
                case ConverterStrategy.Enumerable:
                case ConverterStrategy.Value:
                    break;
                case ConverterStrategy.None:
                    {
                        ThrowHelper.ThrowNotSupportedException_SerializationNotSupported(type);
                    }
                    break;
                default:
                    Debug.Fail($"Unexpected class type: {PropertyInfoForTypeInfo.ConverterStrategy}");
                    throw new InvalidOperationException();
            }

            CreateObject = Options.MemberAccessorStrategy.CreateConstructor(type);

            // These two method overrides are expected to perform
            // orthogonal changes, so we can invoke them both safely.
            converter.ConfigureJsonTypeInfo(this, options);
            converter.ConfigureJsonTypeInfoUsingReflection(this, options);
        }

        [RequiresUnreferencedCode(JsonSerializer.SerializationUnreferencedCodeMessage)]
        private void AddPropertiesAndParametersUsingReflection()
        {
            Debug.Assert(PropertyInfoForTypeInfo.ConverterStrategy == ConverterStrategy.Object);

            const BindingFlags bindingFlags =
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic |
                BindingFlags.DeclaredOnly;
            Dictionary<string, JsonPropertyInfo>? ignoredMembers = null;

            PropertyInfo[] properties = Type.GetProperties(bindingFlags);

            bool propertyOrderSpecified = false;

            // PropertyCache is not accessed by other threads until the current JsonTypeInfo instance
            //  is finished initializing and added to the cache on JsonSerializerOptions.
            // Default 'capacity' to the common non-polymorphic + property case.
            PropertyCache = new JsonPropertyDictionary<JsonPropertyInfo>(Options.PropertyNameCaseInsensitive, capacity: properties.Length);

            // We start from the most derived type.
            Type? currentType = Type;

            while (true)
            {
                foreach (PropertyInfo propertyInfo in properties)
                {
                    bool isVirtual = propertyInfo.IsVirtual();
                    string propertyName = propertyInfo.Name;

                    // Ignore indexers and virtual properties that have overrides that were [JsonIgnore]d.
                    if (propertyInfo.GetIndexParameters().Length > 0 ||
                        PropertyIsOverridenAndIgnored(propertyName, propertyInfo.PropertyType, isVirtual, ignoredMembers))
                    {
                        continue;
                    }

                    // For now we only support public properties (i.e. setter and/or getter is public).
                    if (propertyInfo.GetMethod?.IsPublic == true ||
                        propertyInfo.SetMethod?.IsPublic == true)
                    {
                        CacheMember(
                            currentType,
                            propertyInfo.PropertyType,
                            propertyInfo,
                            isVirtual,
                            NumberHandling,
                            ref propertyOrderSpecified,
                            ref ignoredMembers);
                    }
                    else
                    {
                        if (JsonPropertyInfo.GetAttribute<JsonIncludeAttribute>(propertyInfo) != null)
                        {
                            ThrowHelper.ThrowInvalidOperationException_JsonIncludeOnNonPublicInvalid(propertyName, currentType);
                        }

                        // Non-public properties should not be included for (de)serialization.
                    }
                }

                foreach (FieldInfo fieldInfo in currentType.GetFields(bindingFlags))
                {
                    string fieldName = fieldInfo.Name;

                    if (PropertyIsOverridenAndIgnored(fieldName, fieldInfo.FieldType, currentMemberIsVirtual: false, ignoredMembers))
                    {
                        continue;
                    }

                    bool hasJsonInclude = JsonPropertyInfo.GetAttribute<JsonIncludeAttribute>(fieldInfo) != null;

                    if (fieldInfo.IsPublic)
                    {
                        if (hasJsonInclude || Options.IncludeFields)
                        {
                            CacheMember(
                                currentType,
                                fieldInfo.FieldType,
                                fieldInfo,
                                isVirtual: false,
                                NumberHandling,
                                ref propertyOrderSpecified,
                                ref ignoredMembers);
                        }
                    }
                    else
                    {
                        if (hasJsonInclude)
                        {
                            ThrowHelper.ThrowInvalidOperationException_JsonIncludeOnNonPublicInvalid(fieldName, currentType);
                        }

                        // Non-public fields should not be included for (de)serialization.
                    }
                }

                currentType = currentType.BaseType;
                if (currentType == null)
                {
                    break;
                }

                properties = currentType.GetProperties(bindingFlags);
            };

            if (propertyOrderSpecified)
            {
                PropertyCache.List.Sort((p1, p2) => p1.Value!.Order.CompareTo(p2.Value!.Order));
            }

            JsonConverter converter = PropertyInfoForTypeInfo.ConverterBase;
            if (converter.ConstructorIsParameterized)
            {
                ParameterInfo[] parameters = converter.ConstructorInfo!.GetParameters();
                int parameterCount = parameters.Length;

                JsonParameterInfoValues[] jsonParameters = GetParameterInfoArray(parameters);
                InitializeConstructorParameters(jsonParameters);
            }
        }

        private void CacheMember(
            Type declaringType,
            Type memberType,
            MemberInfo memberInfo,
            bool isVirtual,
            JsonNumberHandling? typeNumberHandling,
            ref bool propertyOrderSpecified,
            ref Dictionary<string, JsonPropertyInfo>? ignoredMembers)
        {
            bool hasExtensionAttribute = memberInfo.GetCustomAttribute(typeof(JsonExtensionDataAttribute)) != null;
            if (hasExtensionAttribute && DataExtensionProperty != null)
            {
                ThrowHelper.ThrowInvalidOperationException_SerializationDuplicateTypeAttribute(Type, typeof(JsonExtensionDataAttribute));
            }

            JsonPropertyInfo jsonPropertyInfo = AddProperty(memberInfo, memberType, declaringType, isVirtual, typeNumberHandling, Options);
            Debug.Assert(jsonPropertyInfo.NameAsString != null);

            if (hasExtensionAttribute)
            {
                Debug.Assert(DataExtensionProperty == null);
                ValidateAndAssignDataExtensionProperty(jsonPropertyInfo);
                Debug.Assert(DataExtensionProperty != null);
            }
            else
            {
                CacheMember(jsonPropertyInfo, PropertyCache, ref ignoredMembers);
                propertyOrderSpecified |= jsonPropertyInfo.Order != 0;
            }
        }

        private void CacheMember(JsonPropertyInfo jsonPropertyInfo, JsonPropertyDictionary<JsonPropertyInfo>? propertyCache, ref Dictionary<string, JsonPropertyInfo>? ignoredMembers)
        {
            string memberName = jsonPropertyInfo.ClrName!;

            // The JsonPropertyNameAttribute or naming policy resulted in a collision.
            if (!propertyCache!.TryAdd(jsonPropertyInfo.NameAsString, jsonPropertyInfo))
            {
                JsonPropertyInfo other = propertyCache[jsonPropertyInfo.NameAsString]!;

                if (other.IsIgnored)
                {
                    // Overwrite previously cached property since it has [JsonIgnore].
                    propertyCache[jsonPropertyInfo.NameAsString] = jsonPropertyInfo;
                }
                else if (
                    // Does the current property have `JsonIgnoreAttribute`?
                    !jsonPropertyInfo.IsIgnored &&
                    // Is the current property hidden by the previously cached property
                    // (with `new` keyword, or by overriding)?
                    other.ClrName != memberName &&
                    // Was a property with the same CLR name was ignored? That property hid the current property,
                    // thus, if it was ignored, the current property should be ignored too.
                    ignoredMembers?.ContainsKey(memberName) != true)
                {
                    // We throw if we have two public properties that have the same JSON property name, and neither have been ignored.
                    ThrowHelper.ThrowInvalidOperationException_SerializerPropertyNameConflict(Type, jsonPropertyInfo);
                }
                // Ignore the current property.
            }

            if (jsonPropertyInfo.IsIgnored)
            {
                (ignoredMembers ??= new Dictionary<string, JsonPropertyInfo>()).Add(memberName, jsonPropertyInfo);
            }
        }

        private sealed class ParameterLookupKey
        {
            public ParameterLookupKey(string name, Type type)
            {
                Name = name;
                Type = type;
            }

            public string Name { get; }
            public Type Type { get; }

            public override int GetHashCode()
            {
                return StringComparer.OrdinalIgnoreCase.GetHashCode(Name);
            }

            public override bool Equals([NotNullWhen(true)] object? obj)
            {
                Debug.Assert(obj is ParameterLookupKey);

                ParameterLookupKey other = (ParameterLookupKey)obj;
                return Type == other.Type && string.Equals(Name, other.Name, StringComparison.OrdinalIgnoreCase);
            }
        }

        private sealed class ParameterLookupValue
        {
            public ParameterLookupValue(JsonPropertyInfo jsonPropertyInfo)
            {
                JsonPropertyInfo = jsonPropertyInfo;
            }

            public string? DuplicateName { get; set; }
            public JsonPropertyInfo JsonPropertyInfo { get; }
        }

        private void InitializeConstructorParameters(JsonParameterInfoValues[] jsonParameters, bool sourceGenMode = false)
        {
            var parameterCache = new JsonPropertyDictionary<JsonParameterInfo>(Options.PropertyNameCaseInsensitive, jsonParameters.Length);

            // Cache the lookup from object property name to JsonPropertyInfo using a case-insensitive comparer.
            // Case-insensitive is used to support both camel-cased parameter names and exact matches when C#
            // record types or anonymous types are used.
            // The property name key does not use [JsonPropertyName] or PropertyNamingPolicy since we only bind
            // the parameter name to the object property name and do not use the JSON version of the name here.
            var nameLookup = new Dictionary<ParameterLookupKey, ParameterLookupValue>(PropertyCache!.Count);

            foreach (KeyValuePair<string, JsonPropertyInfo?> kvp in PropertyCache.List)
            {
                JsonPropertyInfo jsonProperty = kvp.Value!;
                string propertyName = jsonProperty.ClrName!;

                ParameterLookupKey key = new(propertyName, jsonProperty.PropertyType);
                ParameterLookupValue value = new(jsonProperty);

                if (!JsonHelpers.TryAdd(nameLookup, key, value))
                {
                    // More than one property has the same case-insensitive name and Type.
                    // Remember so we can throw a nice exception if this property is used as a parameter name.
                    ParameterLookupValue existing = nameLookup[key];
                    existing.DuplicateName = propertyName;
                }
            }

            foreach (JsonParameterInfoValues parameterInfo in jsonParameters)
            {
                ParameterLookupKey paramToCheck = new(parameterInfo.Name, parameterInfo.ParameterType);

                if (nameLookup.TryGetValue(paramToCheck, out ParameterLookupValue? matchingEntry))
                {
                    if (matchingEntry.DuplicateName != null)
                    {
                        // Multiple object properties cannot bind to the same constructor parameter.
                        ThrowHelper.ThrowInvalidOperationException_MultiplePropertiesBindToConstructorParameters(
                            Type,
                            parameterInfo.Name!,
                            matchingEntry.JsonPropertyInfo.NameAsString,
                            matchingEntry.DuplicateName);
                    }

                    Debug.Assert(matchingEntry.JsonPropertyInfo != null);
                    JsonPropertyInfo jsonPropertyInfo = matchingEntry.JsonPropertyInfo;
                    JsonParameterInfo jsonParameterInfo = CreateConstructorParameter(parameterInfo, jsonPropertyInfo, sourceGenMode, Options);
                    parameterCache.Add(jsonPropertyInfo.NameAsString, jsonParameterInfo);
                }
                // It is invalid for the extension data property to bind with a constructor argument.
                else if (DataExtensionProperty != null &&
                    StringComparer.OrdinalIgnoreCase.Equals(paramToCheck.Name, DataExtensionProperty.NameAsString))
                {
                    ThrowHelper.ThrowInvalidOperationException_ExtensionDataCannotBindToCtorParam(DataExtensionProperty);
                }
            }

            ParameterCount = jsonParameters.Length;
            Volatile.Write(ref ParameterCache, parameterCache);
        }

        private static JsonParameterInfoValues[] GetParameterInfoArray(ParameterInfo[] parameters)
        {
            int parameterCount = parameters.Length;
            JsonParameterInfoValues[] jsonParameters = new JsonParameterInfoValues[parameterCount];

            for (int i = 0; i < parameterCount; i++)
            {
                ParameterInfo reflectionInfo = parameters[i];

                JsonParameterInfoValues jsonInfo = new()
                {
                    Name = reflectionInfo.Name!,
                    ParameterType = reflectionInfo.ParameterType,
                    Position = reflectionInfo.Position,
                    HasDefaultValue = reflectionInfo.HasDefaultValue,
                    DefaultValue = reflectionInfo.GetDefaultValue()
                };

                jsonParameters[i] = jsonInfo;
            }

            return jsonParameters;
        }

        private static bool PropertyIsOverridenAndIgnored(
                string currentMemberName,
                Type currentMemberType,
                bool currentMemberIsVirtual,
                Dictionary<string, JsonPropertyInfo>? ignoredMembers)
        {
            if (ignoredMembers == null || !ignoredMembers.TryGetValue(currentMemberName, out JsonPropertyInfo? ignoredMember))
            {
                return false;
            }

            return currentMemberType == ignoredMember.PropertyType &&
                currentMemberIsVirtual &&
                ignoredMember.IsVirtual;
        }

        private void ValidateAndAssignDataExtensionProperty(JsonPropertyInfo jsonPropertyInfo)
        {
            if (!IsValidDataExtensionProperty(jsonPropertyInfo))
            {
                ThrowHelper.ThrowInvalidOperationException_SerializationDataExtensionPropertyInvalid(Type, jsonPropertyInfo);
            }

            DataExtensionProperty = jsonPropertyInfo;
        }

        private bool IsValidDataExtensionProperty(JsonPropertyInfo jsonPropertyInfo)
        {
            Type memberType = jsonPropertyInfo.PropertyType;

            bool typeIsValid = typeof(IDictionary<string, object>).IsAssignableFrom(memberType) ||
                typeof(IDictionary<string, JsonElement>).IsAssignableFrom(memberType) ||
                // Avoid a reference to typeof(JsonNode) to support trimming.
                (memberType.FullName == JsonObjectTypeName && ReferenceEquals(memberType.Assembly, GetType().Assembly));

            return typeIsValid && Options.GetConverterInternal(memberType) != null;
        }

        private static JsonParameterInfo CreateConstructorParameter(
            JsonParameterInfoValues parameterInfo,
            JsonPropertyInfo jsonPropertyInfo,
            bool sourceGenMode,
            JsonSerializerOptions options)
        {
            if (jsonPropertyInfo.IsIgnored)
            {
                return JsonParameterInfo.CreateIgnoredParameterPlaceholder(parameterInfo, jsonPropertyInfo, sourceGenMode);
            }

            JsonConverter converter = jsonPropertyInfo.ConverterBase;
            JsonParameterInfo jsonParameterInfo = converter.CreateJsonParameterInfo();

            jsonParameterInfo.Initialize(parameterInfo, jsonPropertyInfo, options);

            return jsonParameterInfo;
        }

        // This method gets the runtime information for a given type or property.
        // The runtime information consists of the following:
        // - class type,
        // - element type (if the type is a collection),
        // - the converter (either native or custom), if one exists.
        private static JsonConverter GetConverter(
            Type type,
            Type? parentClassType,
            MemberInfo? memberInfo,
            JsonSerializerOptions options)
        {
            Debug.Assert(type != null);
            ValidateType(type, parentClassType, memberInfo, options);
            return options.GetConverterFromMember(parentClassType, type, memberInfo);
        }

        private static void ValidateType(Type type, Type? parentClassType, MemberInfo? memberInfo, JsonSerializerOptions options)
        {
            if (!options.IsJsonTypeInfoCached(type) && IsInvalidForSerialization(type))
            {
                ThrowHelper.ThrowInvalidOperationException_CannotSerializeInvalidType(type, parentClassType, memberInfo);
            }
        }

        private static bool IsInvalidForSerialization(Type type)
        {
            return type.IsPointer || IsByRefLike(type) || type.ContainsGenericParameters;
        }

        private static bool IsByRefLike(Type type)
        {
#if BUILDING_INBOX_LIBRARY
            return type.IsByRefLike;
#else
            if (!type.IsValueType)
            {
                return false;
            }

            object[] attributes = type.GetCustomAttributes(inherit: false);

            for (int i = 0; i < attributes.Length; i++)
            {
                if (attributes[i].GetType().FullName == "System.Runtime.CompilerServices.IsByRefLikeAttribute")
                {
                    return true;
                }
            }

            return false;
#endif
        }

        private static JsonNumberHandling? GetNumberHandlingForType(Type type)
        {
            var numberHandlingAttribute =
                (JsonNumberHandlingAttribute?)JsonSerializerOptions.GetAttributeThatCanHaveMultiple(type, typeof(JsonNumberHandlingAttribute));

            return numberHandlingAttribute?.Handling;
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private string DebuggerDisplay => $"ConverterStrategy.{PropertyInfoForTypeInfo.ConverterStrategy}, {Type.Name}";
    }
}
