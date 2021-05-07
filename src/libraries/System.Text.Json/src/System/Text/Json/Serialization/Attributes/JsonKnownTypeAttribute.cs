// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Text.Json.Serialization
{
    /// <summary>
    /// When placed on a type, indicates that the specified subtype should
    /// be serialized polymorphically using type discriminator identifiers.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = true, Inherited = false)]
    public class JsonKnownTypeAttribute : JsonAttribute
    {
        /// <summary>
        /// Initializes a new attribute with specified parameters.
        /// </summary>
        /// <param name="subtype">The known subtype that should be serialized polymorphically.</param>
        /// <param name="identifier">The string identifier to be used for the serialization of the subtype.</param>
        public JsonKnownTypeAttribute(Type subtype, string identifier)
        {
            Subtype = subtype;
            Identifier = identifier;
        }

        /// <summary>
        /// The known subtype that should be serialized polymorphically.
        /// </summary>
        public Type Subtype { get; }

        /// <summary>
        /// The string identifier to be used for the serialization of the subtype.
        /// </summary>
        public string Identifier { get; }
    }
}
