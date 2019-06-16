// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Reflection;
using System.Text.Json.Serialization;

namespace System.Text.Json
{
    internal partial class JsonClassInfo
    {
        private JsonPropertyInfo AddPolicyProperty(Type propertyType, JsonSerializerOptions options)
        {
            // A policy property is not a real property on a type; instead it leverages the existing converter
            // logic and generic support to avoid boxing. It is used with values types and elements from collections and
            // dictionaries. Typically it would represent a CLR type such as System.String.
            return AddProperty(
                propertyType,
                propertyInfo : null,        // Not a real property so this is null.
                classType : typeof(object), // A dummy type (not used).
                options : options);

        }
        private JsonPropertyInfo AddProperty(Type propertyType, PropertyInfo propertyInfo, Type classType, JsonSerializerOptions options)
        {
            JsonPropertyInfo jsonInfo = CreateProperty(propertyType, propertyType, propertyInfo, classType, options);

            // Convert non-immutable dictionary interfaces to concrete types.
            if (propertyType.IsInterface && jsonInfo.ClassType == ClassType.Dictionary)
            {
                JsonClassInfo elementClassInfo = jsonInfo.ElementClassInfo;
                JsonPropertyInfo elementPropertyInfo = options.GetJsonPropertyInfoFromClassInfo(elementClassInfo, options);

                Type newPropertyType = elementPropertyInfo.GetDictionaryConcreteType();
                if (propertyType != newPropertyType)
                {
                    jsonInfo = CreateProperty(propertyType, newPropertyType, propertyInfo, classType, options);
                }
            }
            else if (jsonInfo.ClassType == ClassType.Enumerable &&
                !propertyType.IsArray &&
                (IsDeserializedByAssigningFromList(propertyType) || IsSetInterface(propertyType)))
            {
                JsonClassInfo elementClassInfo = jsonInfo.ElementClassInfo;
                JsonPropertyInfo elementPropertyInfo = options.GetJsonPropertyInfoFromClassInfo(elementClassInfo, options);

                // Get a runtime type for the property. e.g. ISet<T> -> HashSet<T>, ICollection -> List<object>
                // We use the element's JsonPropertyInfo so we can utilize the generic support.
                Type newPropertyType = elementPropertyInfo.GetConcreteType(propertyType);
                if ((propertyType != newPropertyType) && propertyType.IsAssignableFrom(newPropertyType))
                {
                    jsonInfo = CreateProperty(propertyType, newPropertyType, propertyInfo, classType, options);
                }
            }

            if (propertyInfo != null)
            {
                _propertyRefs.Add(new PropertyRef(GetKey(jsonInfo.NameUsedToCompare), jsonInfo));
            }
            else
            {
                // A single property or an IEnumerable
                _propertyRefs.Add(new PropertyRef(0, jsonInfo));
            }

            return jsonInfo;
        }

        internal static JsonPropertyInfo CreateProperty(Type declaredPropertyType, Type runtimePropertyType, PropertyInfo propertyInfo, Type parentClassType, JsonSerializerOptions options)
        {
            bool hasIgnoreAttribute = (JsonPropertyInfo.GetAttribute<JsonIgnoreAttribute>(propertyInfo) != null);
            if (hasIgnoreAttribute)
            {
                return JsonPropertyInfo.CreateIgnoredPropertyPlaceholder(propertyInfo, options);
            }

            Type collectionElementType = null;
            switch (GetClassType(runtimePropertyType))
            {
                case ClassType.Enumerable:
                case ClassType.Dictionary:
                case ClassType.IDictionaryConstructible:
                case ClassType.KeyValuePair:
                case ClassType.Unknown:
                    collectionElementType = GetElementType(runtimePropertyType, parentClassType, propertyInfo);
                    break;
            }

            // Create the JsonPropertyInfo<TType, TProperty>
            Type propertyInfoClassType;
            if (runtimePropertyType.IsGenericType && runtimePropertyType.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                Type underlyingPropertyType = Nullable.GetUnderlyingType(runtimePropertyType);
                propertyInfoClassType = typeof(JsonPropertyInfoNullable<,>).MakeGenericType(parentClassType, underlyingPropertyType);
            }
            else
            {
                propertyInfoClassType = typeof(JsonPropertyInfoNotNullable<,,>).MakeGenericType(parentClassType, declaredPropertyType, runtimePropertyType);
            }

            JsonPropertyInfo jsonInfo = (JsonPropertyInfo)Activator.CreateInstance(
                propertyInfoClassType,
                BindingFlags.Instance | BindingFlags.Public,
                binder: null, 
                args: null,
                culture: null);

            jsonInfo.Initialize(parentClassType, declaredPropertyType, runtimePropertyType, propertyInfo, collectionElementType, options);

            return jsonInfo;
        }

        internal JsonPropertyInfo CreateRootObject(JsonSerializerOptions options)
        {
            return CreateProperty(Type, Type, null, Type, options);
        }

        internal JsonPropertyInfo CreatePolymorphicProperty(JsonPropertyInfo property, Type runtimePropertyType, JsonSerializerOptions options)
        {
            JsonPropertyInfo runtimeProperty = CreateProperty(property.DeclaredPropertyType, runtimePropertyType, property?.PropertyInfo, Type, options);
            property.CopyRuntimeSettingsTo(runtimeProperty);

            return runtimeProperty;
        }
    }
}
