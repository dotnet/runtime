// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Internal.TypeSystem
{
    /// <summary>
    /// When implemented by a <see cref="MethodDesc"/>, instructs a name mangler to use the same mangled name
    /// as another entity while prepending a specific prefix to that mangled name.
    /// </summary>
    public interface IPrefixMangledSignature
    {
        /// <summary>
        /// Signature whose mangled name to use.
        /// </summary>
        MethodSignature BaseSignature { get; }

        /// <summary>
        /// Prefix to apply when mangling.
        /// </summary>
        string Prefix { get; }
    }
}
