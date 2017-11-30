// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.ComponentModel.Composition.Primitives
{
    /// <summary>
    ///     Indicates the cardinality of the <see cref="Export"/> objects required by an <see cref="ImportDefinition"/>.
    /// </summary>
    public enum ImportCardinality
    {
        /// <summary>
        ///     Zero or one <see cref="Export"/> objects are required by an <see cref="ImportDefinition"/>.
        /// </summary>
        ZeroOrOne = 0,

        /// <summary>
        ///     Exactly one <see cref="Export"/> object is required by an <see cref="ImportDefinition"/>.
        /// </summary>
        ExactlyOne = 1,

        /// <summary>
        ///     Zero or more <see cref="Export"/> objects are required by an <see cref="ImportDefinition"/>.
        /// </summary>
        ZeroOrMore = 2,
    }
}
