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
    /// If there is more than one extension property on a type, or it the property is not of the correct type,
    /// an <see cref="InvalidOperationException"/> is thrown during the first serialization or deserialization of that type.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public sealed class JsonExtensionDataAttribute : JsonAttribute
    {
    }
}
