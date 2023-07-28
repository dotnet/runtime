// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.Text.Json.Serialization.Metadata
{
    /// <summary>
    /// Represents a supported derived type defined in the metadata of a polymorphic type.
    /// </summary>
    public readonly struct JsonDerivedType
    {
        /// <summary>
        /// Specifies a supported derived type without a type discriminator.
        /// </summary>
        /// <param name="derivedType">The derived type to be supported by the polymorphic type metadata.</param>
        public JsonDerivedType(Type derivedType)
        {
            DerivedType = derivedType;
            TypeDiscriminator = null;
        }

        /// <summary>
        /// Specifies a supported derived type with an integer type discriminator.
        /// </summary>
        /// <param name="derivedType">The derived type to be supported by the polymorphic type metadata.</param>
        /// <param name="typeDiscriminator">The type discriminator to be associated with the derived type.</param>
        public JsonDerivedType(Type derivedType, int typeDiscriminator)
        {
            DerivedType = derivedType;
            TypeDiscriminator = typeDiscriminator;
        }

        /// <summary>
        /// Specifies a supported derived type with a string type discriminator.
        /// </summary>
        /// <param name="derivedType">The derived type to be supported by the polymorphic type metadata.</param>
        /// <param name="typeDiscriminator">The type discriminator to be associated with the derived type.</param>
        public JsonDerivedType(Type derivedType, string typeDiscriminator)
        {
            DerivedType = derivedType;
            TypeDiscriminator = typeDiscriminator;
        }

        internal JsonDerivedType(Type derivedType, object? typeDiscriminator)
        {
            Debug.Assert(typeDiscriminator is null or int or string);
            DerivedType = derivedType;
            TypeDiscriminator = typeDiscriminator;
        }

        /// <summary>
        /// A derived type that should be supported in polymorphic serialization of the declared base type.
        /// </summary>
        public Type DerivedType { get; }

        /// <summary>
        /// The type discriminator identifier to be used for the serialization of the subtype.
        /// </summary>
        public object? TypeDiscriminator { get; }

        internal void Deconstruct(out Type derivedType, out object? typeDiscriminator)
        {
            derivedType = DerivedType;
            typeDiscriminator = TypeDiscriminator;
        }
    }
}
