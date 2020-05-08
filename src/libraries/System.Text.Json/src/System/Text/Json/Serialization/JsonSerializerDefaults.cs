// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Text.Json
{
    /// <summary>
    /// Signifies what default options are used by <see cref="JsonSerializerOptions"/>.
    /// </summary>
    public enum JsonSerializerDefaults
    {
        /// <summary>
        /// Specifies that general-purpose values should be used. These are the same settings applied if a <see cref="JsonSerializerDefaults"/> isn't specified.
        /// </summary>
        /// <remarks>
        /// This option implies that property names are treated as case-sensitive and that "PascalCase" name formatting should be employed.
        /// </remarks>
        General = 0,
        /// <summary>
        /// Specifies that values should be used more appropriate to web-based scenarios.
        /// </summary>
        /// <remarks>
        /// This option implies that property names are treated as case-insensitive and that "camelCase" name formatting should be employed.
        /// </remarks>
        Web = 1
    }
}
