// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Diagnostics;

namespace System.Text.Json.Serialization.Metadata
{
    /// <summary>
    /// Provides JSON serialization-related metadata about a type.
    /// </summary>
    /// <typeparam name="T">The generic definition of the type.</typeparam>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public abstract class JsonTypeInfo<T> : JsonTypeInfo
    {
        internal JsonTypeInfo(Type type, JsonSerializerOptions options, ConverterStrategy converterStrategy) :
            base(type, options, converterStrategy)
        { }

        internal JsonTypeInfo()
        {
            Debug.Assert(false, "This constructor should not be called.");
        }

        /// <summary>
        /// A method that serializes an instance of <typeparamref name="T"/> using
        /// <see cref="JsonSerializerOptionsAttribute"/> values specified at design time.
        /// </summary>
        public Action<Utf8JsonWriter, T>? Serialize { get; private protected set; }
    }
}
