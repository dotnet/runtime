// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.Text.Json.Serialization.Metadata
{
    /// <summary>
    /// Holds relevant state about a method parameter, like the default value of
    /// the parameter, and the position in the method's parameter list.
    /// </summary>
    internal abstract class JsonParameterInfo
    {
        internal JsonParameterInfo(JsonParameterInfoValues parameterInfoValues, JsonPropertyInfo matchingProperty)
        {
            Debug.Assert(matchingProperty.PropertyType == parameterInfoValues.ParameterType);

            Position = parameterInfoValues.Position;
            Name = parameterInfoValues.Name;
            HasDefaultValue = parameterInfoValues.HasDefaultValue;
            IsNullable = parameterInfoValues.IsNullable;
            MatchingProperty = matchingProperty;
        }

        public int Position { get; }
        public string Name { get; }
        public bool HasDefaultValue { get; }

        // The default value of the parameter. This is `DefaultValue` of the `ParameterInfo`, if specified, or the `default` for the `ParameterType`.
        public object? DefaultValue { get; private protected init; }
        public JsonPropertyInfo MatchingProperty { get; }
        public bool IsNullable { get; internal set; }

        public Type DeclaringType => MatchingProperty.DeclaringType;
        public Type ParameterType => MatchingProperty.PropertyType;
        public JsonConverter EffectiveConverter => MatchingProperty.EffectiveConverter;
        public bool IgnoreNullTokensOnRead => MatchingProperty.IgnoreNullTokensOnRead;
        public JsonSerializerOptions Options => MatchingProperty.Options;

        // The effective name of the parameter as UTF-8 bytes.
        public byte[] JsonNameAsUtf8Bytes => MatchingProperty.NameAsUtf8Bytes;
        public JsonNumberHandling? NumberHandling => MatchingProperty.EffectiveNumberHandling;
        public JsonTypeInfo JsonTypeInfo => MatchingProperty.JsonTypeInfo;
        public bool ShouldDeserialize => !MatchingProperty.IsIgnored;
    }
}
