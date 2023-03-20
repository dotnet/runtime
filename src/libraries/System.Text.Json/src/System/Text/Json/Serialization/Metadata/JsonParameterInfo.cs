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
        public JsonConverter EffectiveConverter => MatchingProperty.EffectiveConverter;

        // The default value of the parameter. This is `DefaultValue` of the `ParameterInfo`, if specified, or the `default` for the `ParameterType`.
        public object? DefaultValue { get; private protected init; }

        public bool IgnoreNullTokensOnRead { get; }

        public JsonSerializerOptions Options { get; }

        // The name of the parameter as UTF-8 bytes.
        public byte[] NameAsUtf8Bytes { get; }

        public JsonNumberHandling? NumberHandling { get; }

        public int Position { get; }

        public JsonTypeInfo JsonTypeInfo => MatchingProperty.JsonTypeInfo;

        public Type ParameterType { get; }

        public bool ShouldDeserialize { get; }

        public JsonPropertyInfo MatchingProperty { get; }

        public JsonParameterInfo(JsonParameterInfoValues parameterInfoValues, JsonPropertyInfo matchingProperty)
        {
            Debug.Assert(matchingProperty.IsConfigured);

            MatchingProperty = matchingProperty;
            ShouldDeserialize = !matchingProperty.IsIgnored;
            Options = matchingProperty.Options;
            Position = parameterInfoValues.Position;

            ParameterType = matchingProperty.PropertyType;
            NameAsUtf8Bytes = matchingProperty.NameAsUtf8Bytes;
            IgnoreNullTokensOnRead = matchingProperty.IgnoreNullTokensOnRead;
            NumberHandling = matchingProperty.EffectiveNumberHandling;
        }
    }
}
