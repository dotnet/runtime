// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Text.Json.Serialization;

namespace System.Text.Json
{
    internal partial class JsonClassInfo
    {
        private void AddPolicyProperty(Type propertyType, JsonSerializerOptions options)
        {
            // A policy property is not a real property on a type; instead it leverages the existing converter
            // logic and generic support to avoid boxing. It is used with values types and elements from collections and
            // dictionaries. Typically it would represent a CLR type such as System.String.
            PolicyProperty = AddProperty(
                propertyType,
                propertyInfo: null,        // Not a real property so this is null.
                classType: typeof(object), // A dummy type (not used).
                options: options);
        }

        private JsonPropertyInfo AddProperty(Type propertyType, PropertyInfo propertyInfo, Type classType, JsonSerializerOptions options)
        {
            JsonPropertyInfo jsonInfo;

            // Get implemented type, if applicable.
            // Will return the propertyType itself if it's a non-enumerable, string, natively supported collection,
            // or if a custom converter has been provided for the type.
            Type implementedType = GetImplementedCollectionType(classType, propertyType, propertyInfo, out JsonConverter converter, options);

            if (implementedType != propertyType)
            {
                jsonInfo = CreateProperty(implementedType, implementedType, implementedType, propertyInfo, typeof(object), converter, options);
            }
            else
            {
                jsonInfo = CreateProperty(propertyType, propertyType, propertyType, propertyInfo, classType, converter, options);
            }

            // Convert non-immutable dictionary interfaces to concrete types.
            if (IsNativelySupportedCollection(propertyType) && implementedType.IsInterface && jsonInfo.ClassType == ClassType.Dictionary)
            {
                JsonPropertyInfo elementPropertyInfo = options.GetJsonPropertyInfoFromClassInfo(jsonInfo.ElementType, options);

                Type newPropertyType = elementPropertyInfo.GetDictionaryConcreteType();
                if (implementedType != newPropertyType)
                {
                    jsonInfo = CreateProperty(propertyType, newPropertyType, implementedType, propertyInfo, classType, converter, options);
                }
                else
                {
                    jsonInfo = CreateProperty(propertyType, implementedType, implementedType, propertyInfo, classType, converter, options);
                }
            }
            else if (jsonInfo.ClassType == ClassType.Enumerable &&
                !implementedType.IsArray &&
                ((IsDeserializedByAssigningFromList(implementedType) && IsNativelySupportedCollection(propertyType)) || IsSetInterface(implementedType)))
            {
                JsonPropertyInfo elementPropertyInfo = options.GetJsonPropertyInfoFromClassInfo(jsonInfo.ElementType, options);

                // Get a runtime type for the implemented property. e.g. ISet<T> -> HashSet<T>, ICollection -> List<object>
                // We use the element's JsonPropertyInfo so we can utilize the generic support.
                Type newPropertyType = elementPropertyInfo.GetConcreteType(implementedType);
                if ((implementedType != newPropertyType) && implementedType.IsAssignableFrom(newPropertyType))
                {
                    jsonInfo = CreateProperty(propertyType, newPropertyType, implementedType, propertyInfo, classType, converter, options);
                }
                else
                {
                    jsonInfo = CreateProperty(propertyType, implementedType, implementedType, propertyInfo, classType, converter, options);
                }
            }
            else if (propertyType != implementedType)
            {
                jsonInfo = CreateProperty(propertyType, implementedType, implementedType, propertyInfo, classType, converter, options);
            }

            return jsonInfo;
        }

        internal static JsonPropertyInfo CreateProperty(
            Type declaredPropertyType,
            Type runtimePropertyType,
            Type implementedPropertyType,
            PropertyInfo propertyInfo,
            Type parentClassType,
            JsonConverter converter,
            JsonSerializerOptions options)
        {
            bool hasIgnoreAttribute = (JsonPropertyInfo.GetAttribute<JsonIgnoreAttribute>(propertyInfo) != null);
            if (hasIgnoreAttribute)
            {
                return JsonPropertyInfo.CreateIgnoredPropertyPlaceholder(propertyInfo, options);
            }

            Type collectionElementType = null;
            switch (GetClassType(runtimePropertyType, options))
            {
                case ClassType.Enumerable:
                case ClassType.Dictionary:
                case ClassType.IDictionaryConstructible:
                case ClassType.Unknown:
                    collectionElementType = GetElementType(runtimePropertyType, parentClassType, propertyInfo, options);
                    break;
            }

            // Create the JsonPropertyInfo<TType, TProperty>
            Type propertyInfoClassType;
            if (runtimePropertyType.IsGenericType && runtimePropertyType.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                // First try to find a converter for the Nullable, then if not found use the underlying type.
                // This supports custom converters that want to (de)serialize as null when the value is not null.
                if (converter == null)
                {
                    converter = options.DetermineConverterForProperty(parentClassType, runtimePropertyType, propertyInfo);
                }

                if (converter != null)
                {
                    propertyInfoClassType = typeof(JsonPropertyInfoNotNullable<,,,>).MakeGenericType(
                        parentClassType,
                        declaredPropertyType,
                        runtimePropertyType,
                        runtimePropertyType);
                }
                else
                {
                    Type typeToConvert = Nullable.GetUnderlyingType(runtimePropertyType);
                    converter = options.DetermineConverterForProperty(parentClassType, typeToConvert, propertyInfo);
                    propertyInfoClassType = typeof(JsonPropertyInfoNullable<,>).MakeGenericType(parentClassType, typeToConvert);
                }
            }
            else
            {
                if (converter == null)
                {
                    converter = options.DetermineConverterForProperty(parentClassType, runtimePropertyType, propertyInfo);
                }

                Type typeToConvert = converter?.TypeToConvert;
                if (typeToConvert == null)
                {
                    if (IsNativelySupportedCollection(declaredPropertyType))
                    {
                        typeToConvert = implementedPropertyType;
                    }
                    else
                    {
                        typeToConvert = declaredPropertyType;
                    }
                }

                // For the covariant case, create JsonPropertyInfoNotNullable. The generic constraints are "where TConverter : TDeclaredProperty".
                if (runtimePropertyType.IsAssignableFrom(typeToConvert))
                {
                    propertyInfoClassType = typeof(JsonPropertyInfoNotNullable<,,,>).MakeGenericType(
                        parentClassType,
                        declaredPropertyType,
                        runtimePropertyType,
                        typeToConvert);
                }
                else
                {
                    Debug.Assert(typeToConvert.IsAssignableFrom(runtimePropertyType));

                    // For the contravariant case, create JsonPropertyInfoNotNullableContravariant. The generic constraints are "where TDeclaredProperty : TConverter".
                    propertyInfoClassType = typeof(JsonPropertyInfoNotNullableContravariant<,,,>).MakeGenericType(
                        parentClassType,
                        declaredPropertyType,
                        runtimePropertyType,
                        typeToConvert);
                }
            }

            JsonPropertyInfo jsonInfo = (JsonPropertyInfo)Activator.CreateInstance(
                propertyInfoClassType,
                BindingFlags.Instance | BindingFlags.Public,
                binder: null,
                args: null,
                culture: null);

            jsonInfo.Initialize(parentClassType, declaredPropertyType, runtimePropertyType, implementedPropertyType, propertyInfo, collectionElementType, converter, options);

            return jsonInfo;
        }

        internal JsonPropertyInfo CreateRootObject(JsonSerializerOptions options)
        {
            return CreateProperty(
                declaredPropertyType: Type,
                runtimePropertyType: Type,
                implementedPropertyType: Type,
                propertyInfo: null,
                parentClassType: Type,
                converter: null,
                options: options);
        }

        internal JsonPropertyInfo CreatePolymorphicProperty(JsonPropertyInfo property, Type runtimePropertyType, JsonSerializerOptions options)
        {
            JsonPropertyInfo runtimeProperty = CreateProperty(
                property.DeclaredPropertyType, runtimePropertyType,
                property.ImplementedPropertyType,
                property.PropertyInfo,
                parentClassType: Type,
                converter: null,
                options: options);
            property.CopyRuntimeSettingsTo(runtimeProperty);

            return runtimeProperty;
        }
    }
}
