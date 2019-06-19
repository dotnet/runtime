// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace System.Text.Json.Serialization
{
    /// <summary>
    /// Supports converting several types by using a factory pattern.
    /// </summary>
    /// <remarks>
    /// This is useful for converters supporting generics, such as a converter for <see cref="System.Collections.Generic.List{T}"/>.
    /// </remarks>
    public abstract class JsonConverterFactory : JsonConverter
    {
        /// <summary>
        /// When overidden, constructs a new <see cref="JsonConverterFactory"/> instance.
        /// </summary>
        protected internal JsonConverterFactory() { }

        internal JsonConverter GetConverterInternal(Type typeToConvert)
        {
            Debug.Assert(CanConvert(typeToConvert));
            return CreateConverter(typeToConvert);
        }

        /// <summary>
        /// Create a converter for the provided <see cref="Type"/>.
        /// </summary>
        /// <param name="typeToConvert">The type to convert.</param>
        /// <returns>
        /// An instance of a <see cref="JsonConverter{T}"/> where T is compatible with <paramref name="typeToConvert"/>.
        /// </returns>        
        protected abstract JsonConverter CreateConverter(Type typeToConvert);
    }
}
