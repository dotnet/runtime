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
        protected JsonConverterFactory() { }

        internal sealed override ClassType ClassType
        {
            get
            {
                return ClassType.None;
            }
        }

        /// <summary>
        /// Create a converter for the provided <see cref="Type"/>.
        /// </summary>
        /// <param name="typeToConvert">The <see cref="Type"/> being converted.</param>
        /// <param name="options">The <see cref="JsonSerializerOptions"/> being used.</param>
        /// <returns>
        /// An instance of a <see cref="JsonConverter{T}"/> where T is compatible with <paramref name="typeToConvert"/>.
        /// If <see langword="null"/> is returned, a <see cref="NotSupportedException"/> will be thrown.
        /// </returns>
        public abstract JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options);

        internal override JsonPropertyInfo CreateJsonPropertyInfo()
        {
            // We should never get here.
            Debug.Assert(false);

            throw new InvalidOperationException();
        }

        internal override JsonParameterInfo CreateJsonParameterInfo()
        {
            throw new InvalidOperationException();
        }

        internal sealed override Type? ElementType => null;

        internal JsonConverter GetConverterInternal(Type typeToConvert, JsonSerializerOptions options)
        {
            Debug.Assert(CanConvert(typeToConvert));

            JsonConverter? converter = CreateConverter(typeToConvert, options);
            if (converter == null)
            {
                ThrowHelper.ThrowInvalidOperationException_SerializerConverterFactoryReturnsNull(GetType());
            }

            return converter!;
        }

        internal sealed override object ReadCoreAsObject(
            ref Utf8JsonReader reader,
            JsonSerializerOptions options,
            ref ReadStack state)
        {
            // We should never get here.
            Debug.Assert(false);

            throw new InvalidOperationException();
        }

        internal sealed override bool TryWriteAsObject(
            Utf8JsonWriter writer,
            object? value,
            JsonSerializerOptions options,
            ref WriteStack state)
        {
            // We should never get here.
            Debug.Assert(false);

            throw new InvalidOperationException();
        }

        internal sealed override Type TypeToConvert => null!;

        internal sealed override bool WriteCoreAsObject(
            Utf8JsonWriter writer,
            object? value,
            JsonSerializerOptions options,
            ref WriteStack state)
        {
            // We should never get here.
            Debug.Assert(false);

            throw new InvalidOperationException();
        }
    }
}
