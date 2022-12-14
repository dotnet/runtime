// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization.Converters;
using System.Text.Json.Serialization.Metadata;

namespace System.Text.Json.Serialization
{
    /// <summary>
    /// Base class for non-enumerable, non-primitive objects where public properties
    /// are (de)serialized as a JSON object.
    /// </summary>
    internal abstract class JsonObjectConverter<T> : JsonAdvancedConverter<T, object>
    {
        private protected sealed override ConverterStrategy GetDefaultConverterStrategy() => ConverterStrategy.Object;
        internal sealed override Type? ElementType => null;
    }
}
