// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Text.Json.Serialization
{
    /// <summary>
    /// Specifies that the JSON type should have its <see cref="OnDeserialized"/> method called after deserialization occurs.
    /// </summary>
    /// <remarks>
    /// This behavior is only supported on types representing JSON objects.
    /// Types that have a custom converter or represent either collections or primitive values do not support this behavior.
    /// </remarks>
    public interface IJsonOnDeserialized
    {
        /// <summary>
        /// The method that is called after deserialization.
        /// </summary>
        void OnDeserialized();
    }
}
