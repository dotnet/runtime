// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Text.Json.Serialization.Metadata;

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
        /// When overridden, constructs a new <see cref="JsonConverterFactory"/> instance.
        /// </summary>
        protected JsonConverterFactory() { }

        private protected override ConverterStrategy GetDefaultConverterStrategy() => ConverterStrategy.None;

        /// <summary>
        /// Create a converter for the provided <see cref="System.Type"/>.
        /// </summary>
        /// <param name="typeToConvert">The <see cref="System.Type"/> being converted.</param>
        /// <param name="options">The <see cref="JsonSerializerOptions"/> being used.</param>
        /// <returns>
        /// An instance of a <see cref="JsonConverter{T}"/> where T is compatible with <paramref name="typeToConvert"/>.
        /// If <see langword="null"/> is returned, a <see cref="NotSupportedException"/> will be thrown.
        /// </returns>
        public abstract JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options);

        internal sealed override Type? KeyType => null;

        internal sealed override Type? ElementType => null;

        internal JsonConverter GetConverterInternal(Type typeToConvert, JsonSerializerOptions options)
        {
            Debug.Assert(CanConvert(typeToConvert));

            JsonConverter? converter = CreateConverter(typeToConvert, options);
            switch (converter)
            {
                case null:
                    ThrowHelper.ThrowInvalidOperationException_SerializerConverterFactoryReturnsNull(GetType());
                    break;
                case JsonConverterFactory:
                    ThrowHelper.ThrowInvalidOperationException_SerializerConverterFactoryReturnsJsonConverterFactorty(GetType());
                    break;
            }

            return converter;
        }

        internal sealed override object? ReadAsObject(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            Debug.Fail("We should never get here.");

            throw new InvalidOperationException();
        }

        internal sealed override bool OnTryReadAsObject(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options,
            scoped ref ReadStack state,
            out object? value)
        {
            Debug.Fail("We should never get here.");

            throw new InvalidOperationException();
        }

        internal sealed override bool TryReadAsObject(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options,
            scoped ref ReadStack state,
            out object? value)
        {
            Debug.Fail("We should never get here.");

            throw new InvalidOperationException();
        }

        internal sealed override object? ReadAsPropertyNameAsObject(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            Debug.Fail("We should never get here.");

            throw new InvalidOperationException();
        }

        internal sealed override object? ReadAsPropertyNameCoreAsObject(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            Debug.Fail("We should never get here.");

            throw new InvalidOperationException();
        }

        internal sealed override object? ReadNumberWithCustomHandlingAsObject(ref Utf8JsonReader reader, JsonNumberHandling handling, JsonSerializerOptions options)
        {
            Debug.Fail("We should never get here.");

            throw new InvalidOperationException();
        }

        internal sealed override void WriteAsObject(Utf8JsonWriter writer, object? value, JsonSerializerOptions options)
        {
            Debug.Fail("We should never get here.");

            throw new InvalidOperationException();
        }

        internal sealed override bool OnTryWriteAsObject(
            Utf8JsonWriter writer,
            object? value,
            JsonSerializerOptions options,
            ref WriteStack state)
        {
            Debug.Fail("We should never get here.");

            throw new InvalidOperationException();
        }

        internal sealed override bool TryWriteAsObject(
            Utf8JsonWriter writer,
            object? value,
            JsonSerializerOptions options,
            ref WriteStack state)
        {
            Debug.Fail("We should never get here.");

            throw new InvalidOperationException();
        }

        internal sealed override void WriteAsPropertyNameAsObject(Utf8JsonWriter writer, object? value, JsonSerializerOptions options)
        {
            Debug.Fail("We should never get here.");

            throw new InvalidOperationException();
        }

        /// <inheritdoc/>
        public sealed override Type? Type => null;

        internal sealed override void WriteAsPropertyNameCoreAsObject(
            Utf8JsonWriter writer,
            object? value,
            JsonSerializerOptions options,
            bool isWritingExtensionDataProperty)
        {
            Debug.Fail("We should never get here.");

            throw new InvalidOperationException();
        }

        internal sealed override void WriteNumberWithCustomHandlingAsObject(Utf8JsonWriter writer, object? value, JsonNumberHandling handling)
        {
            Debug.Fail("We should never get here.");

            throw new InvalidOperationException();
        }
    }
}
