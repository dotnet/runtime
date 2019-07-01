// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Text.Json.Serialization
{
    /// <summary>
    /// When placed on a property of type <see cref="System.Collections.Generic.IDictionary{TKey, TValue}"/>, any
    /// properties that do not have a matching member are added to that Dictionary during deserialization and written during serialization.
    /// </summary>
    /// <remarks>
    /// The TKey value must be <see cref="string"/> and TValue must be <see cref="JsonElement"/> or <see cref="object"/>.
    /// 
    /// During deserializing, when using <see cref="object"/> a "null" JSON value is treated as a <c>null</c> object reference, and when using
    /// <see cref="JsonElement"/> a "null" is treated as a JsonElement with <see cref="JsonElement.ValueKind"/> set to <see cref="JsonValueKind.Null"/>.
    /// 
    /// During serializing, the name of the extension data property is not included in the JSON;
    /// the data contained within the extension data is serialized as properties of the JSON object.
    /// 
    /// If there is more than one extension property on a type, or it the property is not of the correct type,
    /// an <see cref="InvalidOperationException"/> is thrown during the first serialization or deserialization of that type.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public sealed class JsonExtensionDataAttribute : JsonAttribute
    {
    }
}
