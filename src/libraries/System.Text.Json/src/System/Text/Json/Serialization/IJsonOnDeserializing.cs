// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Text.Json.Serialization
{
    /// <summary>
    /// Specifies that the JSON object should have its <see cref="OnDeserializing"/> method called
    /// before deserialization occurs.
    /// </summary>
    /// <remarks>
    /// Only JSON objects using the default custom converter support this behavior; collections, dictionaries and values do not.
    /// </remarks>
    public interface IJsonOnDeserializing
    {
        /// <summary>
        /// The method that is called before deserialization.
        /// </summary>
        void OnDeserializing();
    }
}
