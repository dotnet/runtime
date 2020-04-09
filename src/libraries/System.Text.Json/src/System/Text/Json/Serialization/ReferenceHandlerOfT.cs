// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;

namespace System.Text.Json.Serialization
{
    /// <summary>
    /// This class defines how the <see cref="JsonSerializer"/> deals with references on serialization and deserialization.
    /// </summary>
    public sealed class ReferenceHandler<T> : ReferenceHandler
        where T: ReferenceResolver, new()
    {
        /// <inheritdoc/>
        public override ReferenceResolver CreateResolver() => new T();
    }
}
