// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Interop
{
    public interface IMarshallingGeneratorResolver
    {
        /// <summary>
        /// Create an <see cref="IMarshallingGenerator"/> instance for marshalling the supplied type in the given position and collect any diagnostics from generator resolution.
        /// </summary>
        /// <param name="info">Type details</param>
        /// <param name="context">Metadata about the stub the type is associated with</param>
        /// <returns>A <see cref="ResolvedGenerator"/> instance.</returns>
        public ResolvedGenerator Create(
            TypePositionInfo info,
            StubCodeContext context);
    }
}
