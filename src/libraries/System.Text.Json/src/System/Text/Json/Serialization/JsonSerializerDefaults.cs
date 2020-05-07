// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Text.Json
{
    /// <summary>
    /// Signifies what default options are instantiated in <see cref="JsonSerializerOptions"/>.
    /// </summary>
    public enum JsonSerializerDefaults : byte
    {
        /// <summary>
        /// General-purpose defaults
        /// </summary>
        General = 0,
        /// <summary>
        /// Web scenarios defaults
        /// </summary>
        Web = 1
    }
}
