// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Text.Json
{
    /// <summary>
    /// Specifies how duplicate property names are handled when added to a JSON object.
    /// </summary>
    public enum DuplicatePropertyNameHandlingStrategy
    {
        /// <summary>
        /// Replace the existing value when there is a duplicate property. The value of the last property in the JSON object will be used.
        /// </summary>
        Replace,
        /// <summary>
        /// Ignore the new value when there is a duplicate property. The value of the first property in the JSON object will be used.
        /// </summary>
        Ignore,
        /// <summary>
        /// Throw an <exception cref="ArgumentException"/> when a duplicate property is encountered.
        /// </summary>
        Error,
    }
}
