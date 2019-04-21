// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Reflection;

namespace System.Text.Json.Serialization
{
    internal partial class JsonClassInfo
    {
        private JsonPropertyInfo AddProperty(Type propertyType, PropertyInfo propertyInfo, Type classType, JsonSerializerOptions options)
        {
            JsonPropertyInfo jsonInfo = CreateProperty(propertyType, propertyType, propertyInfo, classType, options);

            // Convert interfaces to concrete types.
            if (propertyType.IsInterface && jsonInfo.ClassType == ClassType.Dictionary)
            {
                Type newPropertyType = jsonInfo.ElementClassInfo.GetPolicyProperty().GetConcreteType(propertyType);
                if (propertyType != newPropertyType)
                {
                    jsonInfo = CreateProperty(propertyType, newPropertyType, propertyInfo, classType, options);
                }
            }

            if (propertyInfo != null)
            {
                _propertyRefs.Add(new PropertyRef(GetKey(jsonInfo.CompareName), jsonInfo));
            }
            else
            {
                // A single property or an IEnumerable
                _propertyRefs.Add(new PropertyRef(0, jsonInfo));
            }

            return jsonInfo;
        }

        internal JsonPropertyInfo CreateProperty(Type declaredPropertyType, Type runtimePropertyType, PropertyInfo propertyInfo, Type parentClassType, JsonSerializerOptions options)
        {
            Type collectionElementType = null;
            ClassType propertyClassType = GetClassType(runtimePropertyType);
            if (propertyClassType == ClassType.Enumerable || propertyClassType == ClassType.Dictionary)
            {
                collectionElementType = GetElementType(runtimePropertyType);
                // todo: if collectionElementType is object, create loosely-typed collection (JsonArray).
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
                BindingFlags.Instance | BindingFlags.NonPublic,
                binder: null,
                new object[] { parentClassType, declaredPropertyType, runtimePropertyType, propertyInfo, collectionElementType, options },
                culture: null);

            return jsonInfo;
        }

        internal JsonPropertyInfo CreatePolymorphicProperty(JsonPropertyInfo property, Type runtimePropertyType, JsonSerializerOptions options)
        {
            if (property == null)
            {
                // Used with root objects which are not really a property.
                return CreateProperty(runtimePropertyType, runtimePropertyType, null, runtimePropertyType, options);
            }

            JsonPropertyInfo runtimeProperty = CreateProperty(property.DeclaredPropertyType, runtimePropertyType, property?.PropertyInfo, Type, options);
            property.CopyRuntimeSettingsTo(runtimeProperty);

            return runtimeProperty;
        }
    }
}
