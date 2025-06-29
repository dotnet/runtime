// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Text.Json.Serialization
{
    /// <summary>
    /// When placed on a type, property, or field, indicates what <see cref="JsonNamingPolicy"/>
    /// should be used when serializing or deserializing properties.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Struct | AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public class JsonNamingPolicyAttribute : JsonAttribute
    {
        /// <summary>
        /// Initializes a new instance of <see cref="JsonNamingPolicyAttribute"/>.
        /// </summary>
        public JsonNamingPolicyAttribute(JsonKnownNamingPolicy namingPolicy)
        {
            NamingPolicy = namingPolicy switch
            {
                JsonKnownNamingPolicy.CamelCase => JsonNamingPolicy.CamelCase,
                JsonKnownNamingPolicy.SnakeCaseLower => JsonNamingPolicy.SnakeCaseLower,
                JsonKnownNamingPolicy.SnakeCaseUpper => JsonNamingPolicy.SnakeCaseUpper,
                JsonKnownNamingPolicy.KebabCaseLower => JsonNamingPolicy.KebabCaseLower,
                _ => JsonNamingPolicy.CamelCase,
            };
        }

        /// <summary>
        /// Initializes a new instance of <see cref="JsonNamingPolicyAttribute"/>.
        /// Should be used for user-based overrides
        /// </summary>
        protected JsonNamingPolicyAttribute(JsonNamingPolicy namingPolicy)
        {
            NamingPolicy = namingPolicy;
        }

        /// <summary>
        /// Indicates which naming policy should be used when serializing or deserializing properties.
        /// </summary>
        public JsonNamingPolicy NamingPolicy { get; }
    }
}
