// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace Internal.TypeSystem
{
    /// <summary>
    /// Type with metadata available that is equivalent to a TypeDef record in an ECMA 335 metadata stream.
    /// A class, an interface, or a value type.
    /// </summary>
    abstract partial class DefType
    {
        public abstract string DiagnosticName { get; }
    }
}
