// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Internal.TypeSystem
{
    /// <summary>
    /// When implemented by a <see cref="TypeDesc"/> or <see cref="MethodDesc"/>, instructs a name mangler to use the same mangled name
    /// as another entity while prepending a specific prefix to that mangled name.
    /// </summary>
    public interface IPrefixMangledMethod
    {
        /// <summary>
        /// Method whose mangled name to use.
        /// </summary>
        MethodDesc BaseMethod { get; }

        /// <summary>
        /// Prefix to apply when mangling.
        /// </summary>
        string Prefix { get; }
    }
}
