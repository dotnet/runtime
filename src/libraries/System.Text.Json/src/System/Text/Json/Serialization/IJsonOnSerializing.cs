// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Text.Json.Serialization
{
    /// <summary>
    /// Specifies that the JSON object should have its <see cref="OnSerializing"/> method called
    /// before serialization occurs.
    /// </summary>
    /// <remarks>
    /// Only JSON objects using the default custom converter support this behavior; collections, dictionaries and values do not.
    /// </remarks>
    public interface IJsonOnSerializing
    {
        /// <summary>
        /// The method that is called before serialization.
        /// </summary>
        void OnSerializing();
    }
}
