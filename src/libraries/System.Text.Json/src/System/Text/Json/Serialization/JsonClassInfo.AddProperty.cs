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

            JsonConverter converter = GetConverter(
                propertyType,
                parentClassType,
                propertyInfo,
                out Type runtimeType,
                options);

            return CreateProperty(
                declaredPropertyType: propertyType,
                runtimePropertyType: runtimeType,
                propertyInfo,
                parentClassType,
                converter,
                options);
        }

        internal static JsonPropertyInfo CreateProperty(
            Type declaredPropertyType,
            Type? runtimePropertyType,
            PropertyInfo? propertyInfo,
            Type parentClassType,
            JsonConverter converter,
            JsonSerializerOptions options)
        {
            // Create the JsonPropertyInfo instance.
            JsonPropertyInfo jsonPropertyInfo = converter.CreateJsonPropertyInfo();

            jsonPropertyInfo.Initialize(
                parentClassType,
                declaredPropertyType,
                runtimePropertyType,
                runtimeClassType: converter.ClassType,
                propertyInfo,
                converter,
                options);

            return jsonPropertyInfo;
        }

        /// <summary>
        /// Create a <see cref="JsonPropertyInfo"/> for a given Type.
        /// See <seealso cref="JsonClassInfo.PropertyInfoForClassInfo"/>.
        /// </summary>
        internal static JsonPropertyInfo CreatePropertyInfoForClassInfo(
            Type declaredPropertyType,
            Type runtimePropertyType,
            JsonConverter converter,
            JsonSerializerOptions options)
        {
            return CreateProperty(
                declaredPropertyType: declaredPropertyType,
                runtimePropertyType: runtimePropertyType,
                propertyInfo: null, // Not a real property so this is null.
                parentClassType: typeof(object), // a dummy value (not used)
                converter : converter,
                options);
        }
    }
}
