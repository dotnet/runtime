// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.Text.Json.Serialization.Metadata
{
    /// <summary>
    /// Represents a strongly-typed parameter to prevent boxing where have less than 4 parameters.
    /// Holds relevant state like the default value of the parameter, and the position in the method's parameter list.
    /// </summary>
    internal sealed class JsonParameterInfo<T> : JsonParameterInfo
    {
        internal override object? ClrDefaultValue => default(T);

        public T TypedDefaultValue { get; private set; } = default!;

        public override void Initialize(JsonParameterInfoValues parameterInfo, JsonPropertyInfo matchingProperty, JsonSerializerOptions options)
        {
            base.Initialize(parameterInfo, matchingProperty, options);
            InitializeDefaultValue(matchingProperty);
        }

        protected override void InitializeDefaultValue(JsonPropertyInfo matchingProperty)
        {
            Debug.Assert(ClrInfo.ParameterType == matchingProperty.DeclaredPropertyType);

            if (ClrInfo.HasDefaultValue)
            {
                object? defaultValue = ClrInfo.DefaultValue;

                if (defaultValue == null && !matchingProperty.PropertyTypeCanBeNull)
                {
                    DefaultValue = TypedDefaultValue;
                }
                else
                {
                    DefaultValue = defaultValue;
                    TypedDefaultValue = (T)defaultValue!;
                }
            }
            else
            {
                DefaultValue = TypedDefaultValue;
            }
        }
    }
}
