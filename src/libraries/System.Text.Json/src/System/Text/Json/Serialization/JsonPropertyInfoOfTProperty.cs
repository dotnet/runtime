// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using System.Text.Json.Serialization.Converters;
using System.Text.Json.Serialization.Policies;

namespace System.Text.Json.Serialization
{
    internal abstract class JsonPropertyInfo<TProperty> : JsonPropertyInfo
    {
        // For now, just a global converter.
        private static readonly JsonValueConverter<TProperty> s_enumConverter = new DefaultEnumConverter<TProperty>(false);

        // Constructor used for internal identifiers
        internal JsonPropertyInfo() { }

        internal JsonPropertyInfo(Type classType, Type propertyType, PropertyInfo propertyInfo, Type elementType, JsonSerializerOptions options) :
            base(classType, propertyType, propertyInfo, elementType, options)
        { }

        public JsonValueConverter<TProperty> ValueConverter { get; internal set; }

        internal override void GetPolicies(JsonSerializerOptions options)
        {
            Type propertyType = PropertyType;
            bool isNullable = propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() == typeof(Nullable<>);
            if (isNullable)
            {
                propertyType = Nullable.GetUnderlyingType(propertyType);
            }

            // For Enums, support both the type Enum plus strongly-typed Enums.
            if (propertyType.IsEnum || propertyType == typeof(Enum))
            {
                ValueConverter = s_enumConverter;
            }
            else
            {
                ValueConverter = (JsonValueConverter<TProperty>)DefaultConverters.GetDefaultPropertyValueConverter(propertyType, isNullable);
            }

            base.GetPolicies(options);
        }
    }
}
