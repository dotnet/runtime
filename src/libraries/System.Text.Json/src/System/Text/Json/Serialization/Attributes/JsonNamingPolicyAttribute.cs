// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Text.Json.Serialization
{
    /// <summary>
    /// When placed on a type, property, or field, indicates what <see cref="Json.JsonNamingPolicy"/>
    /// should be used to convert property names.
    /// </summary>
    /// <remarks>
    /// When placed on a property or field, the naming policy specified by this attribute
    /// takes precedence over the type-level attribute and the <see cref="JsonSerializerOptions.PropertyNamingPolicy"/>.
    /// When placed on a type, the naming policy specified by this attribute takes precedence
    /// over the <see cref="JsonSerializerOptions.PropertyNamingPolicy"/>.
    /// The <see cref="JsonPropertyNameAttribute"/> takes precedence over this attribute.
    /// </remarks>
    [AttributeUsage(
        AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface |
        AttributeTargets.Property | AttributeTargets.Field,
        AllowMultiple = false)]
    public class JsonNamingPolicyAttribute : JsonAttribute
    {
        /// <summary>
        /// Initializes a new instance of <see cref="JsonNamingPolicyAttribute"/>
        /// with the specified known naming policy.
        /// </summary>
        /// <param name="namingPolicy">The known naming policy to use for name conversion.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// The specified <paramref name="namingPolicy"/> is not a valid known naming policy value.
        /// </exception>
        public JsonNamingPolicyAttribute(JsonKnownNamingPolicy namingPolicy)
        {
            NamingPolicy = ResolveNamingPolicy(namingPolicy);
        }

        /// <summary>
        /// Initializes a new instance of <see cref="JsonNamingPolicyAttribute"/>
        /// with a custom naming policy.
        /// </summary>
        /// <param name="namingPolicy">The naming policy to use for name conversion.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="namingPolicy"/> is <see langword="null"/>.
        /// </exception>
        protected JsonNamingPolicyAttribute(JsonNamingPolicy namingPolicy)
        {
            if (namingPolicy is null)
            {
                ThrowHelper.ThrowArgumentNullException(nameof(namingPolicy));
            }

            NamingPolicy = namingPolicy;
        }

        /// <summary>
        /// Gets the naming policy to use for name conversion.
        /// </summary>
        public JsonNamingPolicy NamingPolicy { get; }

        internal static JsonNamingPolicy ResolveNamingPolicy(JsonKnownNamingPolicy namingPolicy)
        {
            return namingPolicy switch
            {
                JsonKnownNamingPolicy.CamelCase => JsonNamingPolicy.CamelCase,
                JsonKnownNamingPolicy.SnakeCaseLower => JsonNamingPolicy.SnakeCaseLower,
                JsonKnownNamingPolicy.SnakeCaseUpper => JsonNamingPolicy.SnakeCaseUpper,
                JsonKnownNamingPolicy.KebabCaseLower => JsonNamingPolicy.KebabCaseLower,
                JsonKnownNamingPolicy.KebabCaseUpper => JsonNamingPolicy.KebabCaseUpper,
                _ => throw new ArgumentOutOfRangeException(nameof(namingPolicy)),
            };
        }
    }
}
