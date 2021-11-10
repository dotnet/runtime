// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.ComponentModel.Design
{
    /// <summary>
    /// Provides a generic dictionary service that a designer can use
    /// to store user-defined data on the site.
    /// </summary>
    public interface IDictionaryService
    {
        /// <summary>
        /// Gets the key corresponding to the specified value.
        /// </summary>
        object? GetKey(object? value);

        /// <summary>
        /// Gets the value corresponding to the specified key.
        /// </summary>
        object? GetValue(object key);

        /// <summary>
        /// Sets the specified key-value pair.
        /// </summary>
        void SetValue(object key, object? value);
    }
}
