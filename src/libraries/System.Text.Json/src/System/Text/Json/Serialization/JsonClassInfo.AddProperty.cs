// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using System.Text.Json.Serialization;

namespace System.Text.Json
{
    internal partial class JsonClassInfo
    {
        private JsonPropertyInfo AddProperty(Type propertyType, PropertyInfo propertyInfo, Type parentClassType, JsonSerializerOptions options)
        {
            bool hasIgnoreAttribute = (JsonPropertyInfo.GetAttribute<JsonIgnoreAttribute>(propertyInfo) != null);
            if (hasIgnoreAttribute)
            {
                return JsonPropertyInfo.CreateIgnoredPropertyPlaceholder(propertyInfo, options);
            }

            JsonConverter? converter;
            ClassType classType = GetClassType(
                propertyType,
                parentClassType,
                propertyInfo,
                out Type? runtimeType,
                out Type? _,
                out converter,
                options);

            if (converter == null)
            {
                ThrowHelper.ThrowNotSupportedException_SerializationNotSupported(propertyType, parentClassType, propertyInfo);
            }

            return CreateProperty(
                declaredPropertyType: propertyType,
                runtimePropertyType: runtimeType,
                propertyInfo,
                parentClassType,
                converter,
                classType,
                options);
        }

        internal static JsonPropertyInfo CreateProperty(
            Type declaredPropertyType,
            Type? runtimePropertyType,
            PropertyInfo? propertyInfo,
            Type parentClassType,
            JsonConverter converter,
            ClassType classType,
            JsonSerializerOptions options)
        {
            // Create the JsonPropertyInfo instance.
            JsonPropertyInfo jsonPropertyInfo = converter.CreateJsonPropertyInfo();

            jsonPropertyInfo.Initialize(
                parentClassType,
                declaredPropertyType,
                runtimePropertyType,
                runtimeClassType: classType,
                propertyInfo,
                converter,
                options);

            return jsonPropertyInfo;
        }

        /// <summary>
        /// Create a <see cref="JsonPropertyInfo"/> for a given Type.
        /// A policy property is not a real property on a type; instead it leverages the existing converter
        /// logic and generic support to avoid boxing. It is used with values types, elements from collections and
        /// dictionaries, and collections themselves. Typically it would represent a CLR type such as System.String.
        /// </summary>
        internal static JsonPropertyInfo CreatePolicyProperty(
            Type declaredPropertyType,
            Type? runtimePropertyType,
            JsonConverter converter,
            ClassType classType,
            JsonSerializerOptions options)
        {
            return CreateProperty(
                declaredPropertyType: declaredPropertyType,
                runtimePropertyType: runtimePropertyType,
                propertyInfo: null, // Not a real property so this is null.
                parentClassType: typeof(object), // a dummy value (not used)
                converter : converter,
                classType : classType,
                options);
        }
    }
}
