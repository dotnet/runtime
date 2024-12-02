// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace System.Text.Json.Serialization.Metadata
{
    /// <summary>
    /// Provides JSON serialization-related metadata about a constructor parameter.
    /// </summary>
    public abstract class JsonParameterInfo
    {
        internal JsonParameterInfo(JsonParameterInfoValues parameterInfoValues, JsonPropertyInfo matchingProperty)
        {
            Debug.Assert(matchingProperty.PropertyType == parameterInfoValues.ParameterType);

            Position = parameterInfoValues.Position;
            Name = parameterInfoValues.Name;
            HasDefaultValue = parameterInfoValues.HasDefaultValue;
            DefaultValue = parameterInfoValues.HasDefaultValue ? parameterInfoValues.DefaultValue : null;
            MatchingProperty = matchingProperty;
            IsMemberInitializer = parameterInfoValues.IsMemberInitializer;
        }

        /// <summary>
        /// Gets the declaring type of the constructor.
        /// </summary>
        public Type DeclaringType => MatchingProperty.DeclaringType;

        /// <summary>
        /// Gets the zero-based position of the parameter in the formal parameter list.
        /// </summary>
        public int Position { get; }

        /// <summary>
        /// Gets the type of this parameter.
        /// </summary>
        public Type ParameterType => MatchingProperty.PropertyType;

        /// <summary>
        /// Gets the name of the parameter.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets a value indicating whether the parameter has a default value.
        /// </summary>
        public bool HasDefaultValue { get; }

        /// <summary>
        /// Gets a value indicating the default value if the parameter has a default value.
        /// </summary>
        public object? DefaultValue { get; }

        /// <summary>
        /// The default value to be passed to the constructor argument array, replacing null with default(TParameter).
        /// </summary>
        internal object? EffectiveDefaultValue { get; private protected init; }

        /// <summary>
        /// Gets a value indicating whether the constructor parameter is annotated as nullable.
        /// </summary>
        /// <remarks>
        /// Contracts originating from <see cref="DefaultJsonTypeInfoResolver"/> or <see cref="JsonSerializerContext"/>,
        /// derive the value of this parameter from nullable reference type annotations, including annotations
        /// from attributes such as <see cref="AllowNullAttribute"/> or <see cref="DisallowNullAttribute"/>.
        ///
        /// This property has no effect on deserialization unless the <see cref="JsonSerializerOptions.RespectNullableAnnotations"/>
        /// property has been enabled, in which case the serializer will reject any <see langword="null"/> deserialization results.
        ///
        /// This setting is in sync with the associated <see cref="JsonPropertyInfo.IsSetNullable"/> property.
        /// </remarks>
        public bool IsNullable => MatchingProperty.IsSetNullable;

        /// <summary>
        /// Gets a value indicating whether the parameter represents a required or init-only member initializer.
        /// </summary>
        /// <remarks>
        /// Only returns <see langword="true" /> for source generated metadata which can only access
        /// required or init-only member initializers using object initialize expressions.
        /// </remarks>
        public bool IsMemberInitializer { get; }

        /// <summary>
        /// Gets a custom attribute provider for the current parameter.
        /// </summary>
        /// <remarks>
        /// When resolving metadata via the built-in resolvers this will be populated with
        /// the underlying <see cref="ParameterInfo" /> of the constructor metadata.
        /// </remarks>
        public ICustomAttributeProvider? AttributeProvider
        {
            get
            {
                // Use delayed initialization to ensure that reflection dependencies are pay-for-play.
                Debug.Assert(MatchingProperty.DeclaringTypeInfo != null, "Declaring type metadata must have already been configured.");
                ICustomAttributeProvider? parameterInfo = _attributeProvider;
                if (parameterInfo is null && MatchingProperty.DeclaringTypeInfo.ConstructorAttributeProvider is MethodBase ctorInfo)
                {
                    ParameterInfo[] parameters = ctorInfo.GetParameters();
                    if (Position < parameters.Length)
                    {
                        _attributeProvider = parameterInfo = parameters[Position];
                    }
                }

                return parameterInfo;
            }
        }

        private ICustomAttributeProvider? _attributeProvider;

        internal JsonPropertyInfo MatchingProperty { get; }

        internal JsonConverter EffectiveConverter => MatchingProperty.EffectiveConverter;
        internal bool IgnoreNullTokensOnRead => MatchingProperty.IgnoreNullTokensOnRead;
        internal JsonSerializerOptions Options => MatchingProperty.Options;

        // The effective name of the parameter as UTF-8 bytes.
        internal byte[] JsonNameAsUtf8Bytes => MatchingProperty.NameAsUtf8Bytes;
        internal JsonNumberHandling? NumberHandling => MatchingProperty.EffectiveNumberHandling;
        internal JsonTypeInfo JsonTypeInfo => MatchingProperty.JsonTypeInfo;
        internal bool ShouldDeserialize => !MatchingProperty.IsIgnored;
        internal bool IsRequiredParameter => !HasDefaultValue && !IsMemberInitializer;
    }
}
