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
        public new JsonConverter<T> EffectiveConverter => MatchingProperty.EffectiveConverter;
        public new JsonPropertyInfo<T> MatchingProperty { get; }
        public new T? DefaultValue { get; }

        public JsonParameterInfo(JsonParameterInfoValues parameterInfoValues, JsonPropertyInfo<T> matchingPropertyInfo)
            : base(parameterInfoValues, matchingPropertyInfo)
        {
            Debug.Assert(parameterInfoValues.ParameterType == typeof(T));
            Debug.Assert(matchingPropertyInfo.IsConfigured);

            MatchingProperty = matchingPropertyInfo;
            DefaultValue = parameterInfoValues.HasDefaultValue && parameterInfoValues.DefaultValue is not null
                ? (T)parameterInfoValues.DefaultValue
                : default;

            base.DefaultValue = DefaultValue;
        }
    }
}
