// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Text.Json.Serialization
{
    /// <summary>
    /// When placed on a property or field of type <see cref="System.Text.Json.Nodes.JsonObject"/> or
    /// <see cref="System.Collections.Generic.IDictionary{TKey, TValue}"/>, any properties that do not have a
    /// matching property or field are added during deserialization and written during serialization.
    /// </summary>
    /// <remarks>
    /// When using <see cref="System.Collections.Generic.IDictionary{TKey, TValue}"/>, the TKey value must be <see cref="string"/>
    /// and TValue must be <see cref="JsonElement"/> or <see cref="object"/>.
    ///
    /// During deserializing with a <see cref="System.Collections.Generic.IDictionary{TKey, TValue}"/> extension property with TValue as
    /// <see cref="object"/>, the type of object created will either be a <see cref="System.Text.Json.Nodes.JsonNode"/> or a
    /// <see cref="JsonElement"/> depending on the value of <see cref="System.Text.Json.JsonSerializerOptions.UnknownTypeHandling"/>.
    ///
    /// If a <see cref="JsonElement"/> is created, a "null" JSON value is treated as a JsonElement with <see cref="JsonElement.ValueKind"/>
    /// set to <see cref="JsonValueKind.Null"/>, otherwise a "null" JSON value is treated as a <c>null</c> object reference.
    ///
    /// During serializing, the name of the extension data member is not included in the JSON;
    /// the data contained within the extension data is serialized as properties of the JSON object.
    ///
    /// If there is more than one extension member on a type, or the member is not of the correct type,
    /// an <see cref="InvalidOperationException"/> is thrown during the first serialization or deserialization of that type.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public sealed class JsonExtensionDataAttribute : JsonAttribute
    {
    }
}
